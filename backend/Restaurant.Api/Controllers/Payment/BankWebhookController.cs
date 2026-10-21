using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;
using Restaurant.Api.Hubs;
using System.Text.RegularExpressions;

namespace Restaurant.Api.Controllers.Webhook;

public class BankWebhookPayload
{
    // Mã giao dịch từ cổng/ngân hàng (SePay, Casso, PayOS, Open Banking)
    public long? Id { get; set; }
    public string? Gateway { get; set; }
    public string? TransactionDate { get; set; }
    public string? AccountNumber { get; set; }
    public string? SubAccount { get; set; }
    public string? TransferType { get; set; } // "in"
    public decimal? TransferAmount { get; set; }
    public decimal? Amount { get; set; }
    public string? Content { get; set; }
    public string? Description { get; set; }
    public string? Memo { get; set; }
    public string? ReferenceCode { get; set; }
    public string? Reference { get; set; }
    public string? TransactionCode { get; set; }
    public string? ApiKey { get; set; }
}

[ApiController]
[Route("api/payment/bank-webhook")]
public class BankWebhookController : ControllerBase
{
    private readonly RestaurantDbContext _dbContext;
    private readonly IHubContext<RestaurantHub> _hubContext;

    public BankWebhookController(RestaurantDbContext dbContext, IHubContext<RestaurantHub> hubContext)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    /// <summary>
    /// [POST] Nhận tín hiệu Webhook biến động số dư tài khoản ngân hàng (SePay, Casso, MBBank) để tự động đối soát và thanh toán đơn hàng
    /// URL: /api/payment/bank-webhook
    /// </summary>
    /// <param name="payload">Nội dung chi tiết giao dịch chuyển khoản</param>
    [HttpPost]
    public async Task<IActionResult> ReceiveBankWebhook([FromBody] BankWebhookPayload payload)
    {
        if (payload == null)
        {
            return BadRequest(new { success = false, message = "Payload webhook không hợp lệ." });
        }

        // 1. Lấy số tiền chuyển khoản
        decimal transferAmount = payload.TransferAmount ?? payload.Amount ?? 0;
        if (transferAmount <= 0)
        {
            return BadRequest(new { success = false, message = "Số tiền chuyển khoản phải lớn hơn 0." });
        }

        // 2. Lấy nội dung chuyển khoản
        string text = payload.Content ?? payload.Description ?? payload.Memo ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return BadRequest(new { success = false, message = "Nội dung chuyển khoản không được để trống." });
        }

        // 3. Trích xuất mã đơn hàng từ nội dung (ORDxxxx hoặc ORD-xxxx hoặc #xxxx hoặc ORD xxxx)
        int orderId = ExtractOrderIdFromContent(text);
        if (orderId <= 0)
        {
            return BadRequest(new
            {
                success = false,
                message = $"Không nhận diện được mã đơn hàng (ví dụ: ORD0005) trong nội dung chuyển khoản: '{text}'"
            });
        }

        // 4. Tìm đơn hàng trong cơ sở dữ liệu
        var order = await _dbContext.Orders
            .Include(o => o.OrderTables)
                .ThenInclude(ot => ot.Table)
            .Include(o => o.OrderItems)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
        {
            return NotFound(new { success = false, message = $"Không tìm thấy đơn hàng #{orderId} trong hệ thống." });
        }

        if (order.OrderStatus == "paid")
        {
            return Ok(new
            {
                success = true,
                message = $"Đơn hàng #{orderId} đã được thanh toán trước đó. Bỏ qua webhook trùng lặp."
            });
        }

        if (order.OrderStatus == "cancelled" || order.OrderStatus == "merged")
        {
            return BadRequest(new
            {
                success = false,
                message = $"Đơn hàng #{orderId} đang ở trạng thái '{order.OrderStatus}', không thể thanh toán."
            });
        }

        var activeItems = order.OrderItems
            .Where(i => i.ItemStatus != "cancelled")
            .ToList();

        if (activeItems.Count == 0)
        {
            return BadRequest(new { success = false, message = $"Đơn hàng #{orderId} không có món ăn hợp lệ." });
        }

        decimal subtotal = activeItems.Sum(i => i.UnitPrice * i.Quantity);
        decimal finalTotal = order.TotalAmount > 0 ? order.TotalAmount : subtotal;

        // 5. Kiểm tra số tiền chuyển khoản (cho phép nếu chuyển đủ hoặc nhiều hơn)
        if (transferAmount < finalTotal)
        {
            return BadRequest(new
            {
                success = false,
                message = $"Số tiền chuyển ({transferAmount:N0} đ) chưa đủ so với tổng tiền cần thanh toán ({finalTotal:N0} đ)."
            });
        }

        var now = DateTime.UtcNow;

        // 6. THỰC HIỆN CHECKOUT TỰ ĐỘNG TRONG DATABASE TRANSACTION
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            // Cập nhật trạng thái món ăn sang served
            foreach (var item in activeItems)
            {
                if (item.ItemStatus != "served")
                {
                    item.ItemStatus = "served";
                    item.UpdatedAt = now;
                }
            }

            // Tạo mã hóa đơn duy nhất
            string invoiceCode = $"INV-{now:yyyyMMdd}-{order.OrderId:D4}";
            var existingInvoice = await _dbContext.Invoices.FirstOrDefaultAsync(inv => inv.InvoiceCode == invoiceCode);
            if (existingInvoice != null)
            {
                invoiceCode = $"INV-{now:yyyyMMdd}-{order.OrderId:D4}-{now:HHmmss}";
            }

            // Thu ngân mặc định
            var defaultStaff = await _dbContext.Users.FirstOrDefaultAsync(u => u.Role.RoleCode == "staff" || u.Role.RoleCode == "admin");
            int staffId = defaultStaff?.UserId ?? 1;

            var invoice = new Invoice
            {
                OrderId = order.OrderId,
                InvoiceCode = invoiceCode,
                IssuedBy = staffId,
                CustomerId = order.CustomerId,
                Subtotal = subtotal,
                DiscountAmount = order.DiscountAmount,
                PointsUsed = 0,
                PointsEarned = (int)(finalTotal / 10000m),
                TotalAmount = finalTotal,
                InvoiceStatus = "paid",
                IssuedAt = now
            };
            _dbContext.Invoices.Add(invoice);
            await _dbContext.SaveChangesAsync();

            // Lưu snapshot từng món ăn vào InvoiceItems
            foreach (var item in activeItems)
            {
                var invoiceItem = new InvoiceItem
                {
                    InvoiceId = invoice.InvoiceId,
                    FoodId = item.FoodId,
                    VariantId = item.VariantId,
                    FoodNameSnapshot = item.FoodNameSnapshot,
                    VariantSnapshot = item.VariantSnapshot,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    LineTotal = item.UnitPrice * item.Quantity
                };
                _dbContext.InvoiceItems.Add(invoiceItem);
            }

            // Ghi nhận Payment
            string refCode = payload.ReferenceCode 
                ?? payload.Reference 
                ?? payload.TransactionCode 
                ?? $"BANK-{now:yyyyMMddHHmmss}";

            var payment = new Entities.Payment
            {
                InvoiceId = invoice.InvoiceId,
                PaymentMethod = "transfer",
                Amount = transferAmount,
                TransactionCode = refCode,
                PaymentStatus = "completed",
                PaidAt = now
            };
            _dbContext.Payments.Add(payment);

            // Cập nhật trạng thái Order
            order.OrderStatus = "paid";
            order.TotalAmount = finalTotal;
            order.UpdatedAt = now;

            // Giải phóng bàn ăn
            var freedTables = new List<string>();
            foreach (var ot in order.OrderTables)
            {
                var table = await _dbContext.DiningTables.FindAsync(ot.TableId);
                if (table != null)
                {
                    table.TableStatus = "empty";
                    table.UpdatedAt = now;
                    freedTables.Add(table.TableName);
                }
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            // Chuẩn bị thông tin bàn hiển thị
            var primaryTable = order.OrderTables.FirstOrDefault(ot => ot.IsPrimary)?.Table
                               ?? order.OrderTables.FirstOrDefault()?.Table;
            string tableDisplay = primaryTable?.TableName ?? (freedTables.Count > 0 ? string.Join(", ", freedTables) : "Bàn đã thanh toán");

            // Đối tượng hóa đơn hoàn chỉnh trả về và phát realtime
            var invoiceDto = new
            {
                invoice.InvoiceId,
                invoice.InvoiceCode,
                invoice.OrderId,
                invoice.Subtotal,
                invoice.DiscountAmount,
                VoucherDiscount = order.DiscountAmount,
                PointDiscount = 0m,
                invoice.PointsUsed,
                invoice.PointsEarned,
                invoice.TotalAmount,
                invoice.InvoiceStatus,
                invoice.IssuedAt,
                PaymentMethod = "transfer",
                AmountPaidByCustomer = transferAmount,
                ChangeToReturn = 0m,
                TableDisplay = tableDisplay,
                FreedTables = freedTables,
                Items = activeItems.Select(i => new
                {
                    i.OrderItemId,
                    i.FoodId,
                    i.VariantId,
                    i.FoodNameSnapshot,
                    i.VariantSnapshot,
                    i.UnitPrice,
                    i.Quantity,
                    LineTotal = i.UnitPrice * i.Quantity
                }).ToList(),
                Customer = order.Customer != null ? new
                {
                    order.Customer.CustomerId,
                    order.Customer.FullName,
                    order.Customer.Phone,
                    NewPointsBalance = order.Customer.PointsBalance
                } : null
            };

            // 7. PHÁT SÓNG SIGNALR WEBSOCKET NGAY LẬP TỨC (0s Delay)
            try
            {
                await _hubContext.Clients.All.SendAsync("PaymentReceived", new
                {
                    orderId = order.OrderId,
                    tableDisplay,
                    tableName = primaryTable?.TableName,
                    amount = transferAmount,
                    invoice = invoiceDto,
                    transactionCode = refCode,
                    message = $"🎉 Bàn {tableDisplay} đã thanh toán VietQR thành công {transferAmount:N0} VNĐ!"
                });

                await _hubContext.Clients.All.SendAsync("StaffOrderUpdated");
                await _hubContext.Clients.All.SendAsync("KitchenUpdated");
            }
            catch (Exception hubEx)
            {
                Console.WriteLine($"[SignalR Webhook] Broadcast error: {hubEx.Message}");
            }

            return Ok(new
            {
                success = true,
                message = $"Đã xác nhận thanh toán ngân hàng thành công cho đơn hàng #{order.OrderId} ({tableDisplay})!",
                invoice = invoiceDto
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new
            {
                success = false,
                message = "Lỗi trong quá trình xử lý xác nhận thanh toán tự động: " + ex.Message
            });
        }
    }

    private static int ExtractOrderIdFromContent(string text)
    {
        // 1. Khớp ORD0005, ord5, ORD-5, ORD_0005, ORD 5
        var match = Regex.Match(text, @"(?i)ORD[-_ ]?0*([1-9]\d*)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int id1))
        {
            return id1;
        }

        // 2. Khớp #0005 hoặc #5
        match = Regex.Match(text, @"#0*([1-9]\d*)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int id2))
        {
            return id2;
        }

        // 3. Khớp bill 5, don 5, order 5
        match = Regex.Match(text, @"(?i)(?:bill|don|order|donhang)[-_ ]?0*([1-9]\d*)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int id3))
        {
            return id3;
        }

        return 0;
    }
}
