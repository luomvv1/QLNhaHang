using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;
using Restaurant.Api.Hubs;
using System.Security.Claims;

namespace Restaurant.Api.Controllers.Staff;

public class CheckoutRequest
{
    public int OrderId { get; set; }
    public int? CustomerId { get; set; }
    public string? PromotionCode { get; set; }
    public int PointsToUse { get; set; } = 0;
    public string PaymentMethod { get; set; } = "cash"; // "cash" | "transfer" | "card" | "e_wallet"
    public decimal AmountPaidByCustomer { get; set; } = 0;
    public string? TransactionCode { get; set; }
}

public class ValidateVoucherRequest
{
    public string VoucherCode { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
}

public class QuickCreateCustomerRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
}

public class BankConfigRequest
{
    public string BankId { get; set; } = "MB";
    public string AccountNo { get; set; } = "0333888999";
    public string AccountName { get; set; } = "NHA HANG HUONG SEN";
}

[Route("api/staff/payment")]
[Authorize(Roles = "staff,manager,admin")]
[ApiController]
public class StaffPaymentController : ControllerBase
{
    private readonly RestaurantDbContext _dbContext;
    private readonly IHubContext<RestaurantHub> _hubContext;

    public StaffPaymentController(RestaurantDbContext dbContext, IHubContext<RestaurantHub> hubContext)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    private static readonly string BankConfigFilePath = Path.Combine(AppContext.BaseDirectory, "bank_config.json");

    private BankConfigRequest GetBankConfig()
    {
        try
        {
            string[] possiblePaths = new[]
            {
                BankConfigFilePath,
                Path.Combine(Directory.GetCurrentDirectory(), "bank_config.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "backend", "Restaurant.Api", "bank_config.json")
            };

            foreach (var path in possiblePaths)
            {
                if (System.IO.File.Exists(path))
                {
                    var json = System.IO.File.ReadAllText(path);
                    var cfg = System.Text.Json.JsonSerializer.Deserialize<BankConfigRequest>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (cfg != null && !string.IsNullOrWhiteSpace(cfg.AccountNo))
                    {
                        return cfg;
                    }
                }
            }
        }
        catch { }

        return new BankConfigRequest
        {
            BankId = "MB",
            AccountNo = "0333888999",
            AccountName = "NHA HANG HUONG SEN"
        };
    }

    private void SaveBankConfig(BankConfigRequest config)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        string[] possiblePaths = new[]
        {
            BankConfigFilePath,
            Path.Combine(Directory.GetCurrentDirectory(), "bank_config.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "backend", "Restaurant.Api", "bank_config.json")
        };

        foreach (var path in possiblePaths)
        {
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (Directory.Exists(dir))
                {
                    System.IO.File.WriteAllText(path, json, System.Text.Encoding.UTF8);
                }
            }
            catch { }
        }
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim != null && int.TryParse(claim.Value, out int userId))
        {
            return userId;
        }

        var defaultUser = _dbContext.Users
            .Include(u => u.Role)
            .FirstOrDefault(u => u.Role.RoleCode == "staff" || u.Role.RoleCode == "manager");

        return defaultUser?.UserId ?? 1;
    }

    /// <summary>
    /// [GET] Lấy thông tin tạm tính hóa đơn trước khi thanh toán (subtotal, bàn liên kết, mã VietQR)
    /// URL: /api/staff/payment/checkout-preview/{orderId}
    /// </summary>
    /// <param name="orderId">Mã đơn hàng</param>
    [HttpGet("checkout-preview/{orderId}")]
    public async Task<IActionResult> GetCheckoutPreview(int orderId)
    {
        var order = await _dbContext.Orders
            .Include(o => o.OrderTables)
                .ThenInclude(ot => ot.Table)
            .Include(o => o.OrderItems)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
        {
            return NotFound(new { message = $"Không tìm thấy đơn hàng #{orderId}." });
        }

        if (order.OrderStatus == "paid" || order.OrderStatus == "cancelled" || order.OrderStatus == "merged")
        {
            return BadRequest(new { message = $"Đơn hàng #{orderId} đang ở trạng thái '{order.OrderStatus}', không thể thanh toán." });
        }

        var activeItems = order.OrderItems
            .Where(i => i.ItemStatus != "cancelled")
            .Select(i => new
            {
                i.OrderItemId,
                i.FoodId,
                i.VariantId,
                i.FoodNameSnapshot,
                i.VariantSnapshot,
                i.UnitPrice,
                i.Quantity,
                LineTotal = i.UnitPrice * i.Quantity,
                i.ItemStatus,
                i.Note
            })
            .ToList();

        decimal subtotal = activeItems.Sum(i => i.LineTotal);

        var primaryTable = order.OrderTables.FirstOrDefault(ot => ot.IsPrimary)?.Table 
                           ?? order.OrderTables.FirstOrDefault()?.Table;

        var linkedTables = order.OrderTables
            .Select(ot => new
            {
                ot.TableId,
                ot.Table.TableName,
                ot.Table.AreaName,
                ot.IsPrimary
            })
            .ToList();

        string tableDisplay = primaryTable?.TableName ?? "Bàn không xác định";
        var secondaries = linkedTables.Where(t => !t.IsPrimary).Select(t => t.TableName).ToList();
        if (secondaries.Count > 0)
        {
            tableDisplay += " (+ " + string.Join(", ", secondaries) + ")";
        }

        return Ok(new
        {
            order.OrderId,
            order.OrderStatus,
            OrderCode = $"ORD-{order.OrderId:D4}",
            TableDisplay = tableDisplay,
            PrimaryTableId = primaryTable?.TableId,
            LinkedTables = linkedTables,
            order.CreatedAt,
            Subtotal = subtotal,
            Items = activeItems,
            Customer = order.Customer != null ? new
            {
                order.Customer.CustomerId,
                order.Customer.FullName,
                order.Customer.Phone,
                order.Customer.PointsBalance,
                order.Customer.TotalSpent
            } : null,
            // Đề xuất tích lũy: 1 điểm cho mỗi 10.000 VNĐ
            EstimatedEarnPoints = (int)(subtotal / 10000),
            // Quy ước quy đổi điểm: 1 điểm = 1.000 VNĐ
            PointConversionRate = 1000,
            // Thông tin ngân hàng sinh mã VietQR động
            VietQr = new
            {
                GetBankConfig().BankId,
                GetBankConfig().AccountNo,
                GetBankConfig().AccountName,
                Memo = $"ORD{order.OrderId:D4}"
            }
        });
    }

    /// <summary>
    /// [GET] Tìm kiếm khách hàng thành viên theo số điện thoại để tích/tiêu điểm
    /// URL: /api/staff/payment/customers/search?phone=...
    /// </summary>
    /// <param name="phone">Số điện thoại cần tìm</param>
    [HttpGet("customers/search")]
    public async Task<IActionResult> SearchCustomers([FromQuery] string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return BadRequest(new { message = "Vui lòng nhập số điện thoại cần tìm." });
        }

        string cleanPhone = phone.Trim();
        var customers = await _dbContext.Customers
            .Where(c => c.IsActive && (c.Phone.Contains(cleanPhone) || c.FullName.Contains(cleanPhone)))
            .OrderByDescending(c => c.Phone.StartsWith(cleanPhone))
            .ThenByDescending(c => c.PointsBalance)
            .Take(10)
            .Select(c => new
            {
                c.CustomerId,
                c.FullName,
                c.Phone,
                c.Email,
                c.PointsBalance,
                c.TotalSpent,
                c.VisitCount,
                PointValueInVnd = c.PointsBalance * 1000
            })
            .ToListAsync();

        return Ok(customers);
    }

    /// <summary>
    /// [POST] Tạo nhanh hồ sơ khách hàng thành viên mới ngay tại quầy thu ngân
    /// URL: /api/staff/payment/customers/quick-create
    /// </summary>
    /// <param name="request">Thông tin họ tên và số điện thoại của khách</param>
    [HttpPost("customers/quick-create")]
    public async Task<IActionResult> QuickCreateCustomer([FromBody] QuickCreateCustomerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(new { message = "Vui lòng nhập họ tên khách hàng." });

        if (string.IsNullOrWhiteSpace(request.Phone))
            return BadRequest(new { message = "Vui lòng nhập số điện thoại khách hàng." });

        string cleanPhone = request.Phone.Trim();

        var existing = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Phone == cleanPhone);
        if (existing != null)
        {
            return Ok(new
            {
                message = "Khách hàng đã có tài khoản thành viên trong hệ thống!",
                customer = new
                {
                    existing.CustomerId,
                    existing.FullName,
                    existing.Phone,
                    existing.Email,
                    existing.PointsBalance,
                    existing.TotalSpent,
                    existing.VisitCount,
                    PointValueInVnd = existing.PointsBalance * 1000
                }
            });
        }

        var now = DateTime.UtcNow;
        var newCustomer = new Customer
        {
            FullName = request.FullName.Trim(),
            Phone = cleanPhone,
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            PointsBalance = 0,
            TotalSpent = 0,
            VisitCount = 1,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Customers.Add(newCustomer);
        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = "Tạo tài khoản thành viên mới thành công!",
            customer = new
            {
                newCustomer.CustomerId,
                newCustomer.FullName,
                newCustomer.Phone,
                newCustomer.Email,
                newCustomer.PointsBalance,
                newCustomer.TotalSpent,
                newCustomer.VisitCount,
                PointValueInVnd = 0
            }
        });
    }

    /// <summary>
    /// [POST] Kiểm tra tính hợp lệ và số tiền giảm giá của mã Voucher / Khuyến mãi
    /// URL: /api/staff/payment/validate-voucher
    /// </summary>
    /// <param name="request">Mã voucher và tổng tiền đơn hàng</param>
    [HttpPost("validate-voucher")]
    public async Task<IActionResult> ValidateVoucher([FromBody] ValidateVoucherRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.VoucherCode))
            return BadRequest(new { message = "Vui lòng nhập mã voucher." });

        string code = request.VoucherCode.Trim().ToUpper();
        var now = DateTime.UtcNow;

        var promo = await _dbContext.Promotions
            .FirstOrDefaultAsync(p => p.PromotionCode.ToUpper() == code);

        if (promo == null)
            return NotFound(new { message = $"Mã khuyến mãi '{code}' không tồn tại." });

        if (!promo.IsActive)
            return BadRequest(new { message = $"Mã '{code}' hiện đang bị tạm khóa." });

        if (now < promo.StartAt)
            return BadRequest(new { message = $"Chương trình ưu đãi '{code}' chưa đến thời gian áp dụng." });

        if (now > promo.EndAt)
            return BadRequest(new { message = $"Mã ưu đãi '{code}' đã hết hạn sử dụng." });

        if (promo.UsageLimit.HasValue && promo.UsedCount >= promo.UsageLimit.Value)
            return BadRequest(new { message = $"Mã ưu đãi '{code}' đã hết lượt sử dụng." });

        if (request.Subtotal < promo.MinimumOrderValue)
        {
            return BadRequest(new
            {
                message = $"Đơn hàng chưa đạt giá trị tối thiểu để áp dụng mã này (Tối thiểu: {promo.MinimumOrderValue:N0} đ)."
            });
        }

        decimal discount = 0;
        if (promo.PromotionType == "percent")
        {
            discount = (request.Subtotal * promo.PromotionValue) / 100m;
        }
        else if (promo.PromotionType == "fixed")
        {
            discount = Math.Min(promo.PromotionValue, request.Subtotal);
        }

        return Ok(new
        {
            isValid = true,
            promo.PromotionId,
            promo.PromotionCode,
            promo.PromotionName,
            promo.PromotionType,
            promo.PromotionValue,
            DiscountAmount = discount,
            NewTotal = Math.Max(0, request.Subtotal - discount)
        });
    }

    /// <summary>
    /// [POST] Thực hiện thanh toán đơn hàng, áp dụng voucher/điểm, xuất hóa đơn bất biến và tự động giải phóng bàn
    /// URL: /api/staff/payment/checkout
    /// </summary>
    /// <param name="request">Thông tin thanh toán, voucher, điểm tích lũy và số tiền nhận</param>
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
    {
        var validMethods = new[] { "cash", "transfer", "card", "e_wallet" };
        var paymentMethod = request.PaymentMethod.ToLower().Trim();

        if (!validMethods.Contains(paymentMethod))
        {
            return BadRequest(new { message = $"Phương thức thanh toán '{request.PaymentMethod}' không hợp lệ. Chỉ chấp nhận: cash, transfer, card, e_wallet." });
        }

        var currentStaffId = GetCurrentUserId();
        var now = DateTime.UtcNow;

        // BẮT ĐẦU TRANSACTION TOÀN VẸN
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            // 1. Kiểm tra đơn hàng
            var order = await _dbContext.Orders
                .Include(o => o.OrderTables)
                    .ThenInclude(ot => ot.Table)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId);

            if (order == null)
            {
                return NotFound(new { message = $"Không tìm thấy đơn hàng #{request.OrderId}." });
            }

            if (order.OrderStatus == "paid")
            {
                return BadRequest(new { message = "Đơn hàng này đã được thanh toán trước đó." });
            }

            if (order.OrderStatus == "cancelled" || order.OrderStatus == "merged")
            {
                return BadRequest(new { message = $"Không thể thanh toán đơn hàng có trạng thái '{order.OrderStatus}'." });
            }

            var activeItems = order.OrderItems
                .Where(i => i.ItemStatus != "cancelled")
                .ToList();

            if (activeItems.Count == 0)
            {
                return BadRequest(new { message = "Đơn hàng không có món ăn nào hợp lệ để thanh toán." });
            }

            // 2. Tính Subtotal
            decimal subtotal = activeItems.Sum(i => i.UnitPrice * i.Quantity);
            decimal discountAmount = 0;
            Promotion? appliedPromotion = null;

            // 3. Áp dụng Voucher (nếu có)
            if (!string.IsNullOrWhiteSpace(request.PromotionCode))
            {
                string code = request.PromotionCode.Trim().ToUpper();
                appliedPromotion = await _dbContext.Promotions
                    .FirstOrDefaultAsync(p => p.PromotionCode.ToUpper() == code && p.IsActive);

                if (appliedPromotion != null && now >= appliedPromotion.StartAt && now <= appliedPromotion.EndAt)
                {
                    if (subtotal >= appliedPromotion.MinimumOrderValue)
                    {
                        if (appliedPromotion.PromotionType == "percent")
                        {
                            discountAmount = (subtotal * appliedPromotion.PromotionValue) / 100m;
                        }
                        else if (appliedPromotion.PromotionType == "fixed")
                        {
                            discountAmount = Math.Min(appliedPromotion.PromotionValue, subtotal);
                        }

                        appliedPromotion.UsedCount += 1;
                        appliedPromotion.UpdatedAt = now;
                    }
                }
            }

            // 4. Khách hàng & Điểm thưởng
            Customer? customer = null;
            int pointsUsed = 0;
            decimal pointDiscount = 0;

            if (request.CustomerId.HasValue && request.CustomerId.Value > 0)
            {
                customer = await _dbContext.Customers.FindAsync(request.CustomerId.Value);
            }

            if (customer != null && request.PointsToUse > 0)
            {
                int maxUsablePoints = customer.PointsBalance;
                pointsUsed = Math.Min(request.PointsToUse, maxUsablePoints);

                // Quy đổi: 1 điểm = 1.000 VNĐ
                pointDiscount = pointsUsed * 1000m;

                // Không cho giảm vượt quá số tiền còn lại sau voucher
                decimal remaining = Math.Max(0, subtotal - discountAmount);
                if (pointDiscount > remaining)
                {
                    pointDiscount = remaining;
                    pointsUsed = (int)Math.Ceiling(pointDiscount / 1000m);
                }

                // Trừ điểm của khách
                customer.PointsBalance -= pointsUsed;
                customer.UpdatedAt = now;
            }

            decimal totalDiscount = discountAmount + pointDiscount;
            decimal finalTotal = Math.Max(0, subtotal - totalDiscount);

            if (paymentMethod == "cash" && request.AmountPaidByCustomer < finalTotal)
            {
                return BadRequest(new
                {
                    message = $"Số tiền khách đưa chưa đủ. Còn thiếu {(finalTotal - request.AmountPaidByCustomer):N0} đ."
                });
            }

            // 5. Tích điểm mới cho khách: 1 điểm mỗi 10.000 đ
            int pointsEarned = 0;
            if (customer != null)
            {
                pointsEarned = (int)(finalTotal / 10000m);
                customer.PointsBalance += pointsEarned;
                customer.TotalSpent += finalTotal;
                customer.VisitCount += 1;
                customer.UpdatedAt = now;
            }

            // 6. Tạo InvoiceCode duy nhất: INV-yyyyMMdd-OrderId
            string invoiceCode = $"INV-{DateTime.UtcNow:yyyyMMdd}-{order.OrderId:D4}";
            var existingInvoice = await _dbContext.Invoices.FirstOrDefaultAsync(inv => inv.InvoiceCode == invoiceCode);
            if (existingInvoice != null)
            {
                invoiceCode = $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}-{order.OrderId}";
            }

            // 7. Tạo bản ghi Hóa Đơn (Invoices)
            var invoice = new Invoice
            {
                InvoiceCode = invoiceCode,
                OrderId = order.OrderId,
                CustomerId = customer?.CustomerId,
                IssuedBy = currentStaffId,
                Subtotal = subtotal,
                DiscountAmount = totalDiscount,
                PointsUsed = pointsUsed,
                PointsEarned = pointsEarned,
                TotalAmount = finalTotal,
                InvoiceStatus = "paid",
                IssuedAt = now
            };

            _dbContext.Invoices.Add(invoice);
            await _dbContext.SaveChangesAsync(); // Lưu để sinh InvoiceId

            // 8. Lưu Snapshot bất biến vào InvoiceItems
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
                    LineTotal = item.UnitPrice * item.Quantity,
                    Note = item.Note
                };
                _dbContext.InvoiceItems.Add(invoiceItem);

                // Cập nhật trạng thái OrderItem sang served nếu chưa hoàn tất
                if (item.ItemStatus != "served")
                {
                    item.ItemStatus = "served";
                    item.UpdatedAt = now;
                }
            }

            // 9. Ghi nhận giao dịch thanh toán (Payments)
            var payment = new Payment
            {
                InvoiceId = invoice.InvoiceId,
                PaymentMethod = paymentMethod,
                Amount = finalTotal,
                TransactionCode = string.IsNullOrWhiteSpace(request.TransactionCode) 
                    ? $"{paymentMethod.ToUpper()}-{DateTime.UtcNow:yyyyMMddHHmmss}" 
                    : request.TransactionCode.Trim(),
                PaymentStatus = "completed",
                PaidAt = now
            };
            _dbContext.Payments.Add(payment);

            // 10. Ghi vết PromotionUsage (nếu có dùng voucher)
            if (appliedPromotion != null && discountAmount > 0)
            {
                var promoUsage = new PromotionUsage
                {
                    PromotionId = appliedPromotion.PromotionId,
                    InvoiceId = invoice.InvoiceId,
                    CustomerId = customer?.CustomerId,
                    DiscountAmount = discountAmount,
                    UsedAt = now
                };
                _dbContext.PromotionUsages.Add(promoUsage);
            }

            // 11. Ghi vết PointTransactions (nếu có trừ điểm hoặc cộng điểm)
            if (customer != null)
            {
                if (pointsUsed > 0)
                {
                    _dbContext.PointTransactions.Add(new PointTransaction
                    {
                        CustomerId = customer.CustomerId,
                        InvoiceId = invoice.InvoiceId,
                        TransactionType = "redeem",
                        Points = -pointsUsed,
                        BalanceAfter = customer.PointsBalance - pointsEarned, // Số dư trước khi cộng mới
                        Note = $"Đổi {pointsUsed} điểm trừ {pointDiscount:N0} đ khi thanh toán đơn #{order.OrderId}",
                        CreatedBy = currentStaffId,
                        CreatedAt = now
                    });
                }

                if (pointsEarned > 0)
                {
                    _dbContext.PointTransactions.Add(new PointTransaction
                    {
                        CustomerId = customer.CustomerId,
                        InvoiceId = invoice.InvoiceId,
                        TransactionType = "earn",
                        Points = pointsEarned,
                        BalanceAfter = customer.PointsBalance,
                        Note = $"Tích {pointsEarned} điểm từ hóa đơn {invoice.InvoiceCode}",
                        CreatedBy = currentStaffId,
                        CreatedAt = now
                    });
                }
            }

            // 12. Cập nhật Order trạng thái paid
            order.OrderStatus = "paid";
            order.Subtotal = subtotal;
            order.DiscountAmount = totalDiscount;
            order.TotalAmount = finalTotal;
            order.CustomerId = customer?.CustomerId;
            order.PaidAt = now;
            order.UpdatedAt = now;

            // 13. TỰ ĐỘNG GIẢI PHÓNG TOÀN BỘ CÁC BÀN LIÊN KẾT (Bàn chính + Bàn ghép phụ)
            var freedTableNames = new List<string>();
            foreach (var ot in order.OrderTables)
            {
                var table = await _dbContext.DiningTables.FindAsync(ot.TableId);
                if (table != null)
                {
                    table.TableStatus = "empty"; // Chuyển về trạng thái Trống sẵn sàng đón khách mới
                    table.UpdatedAt = now;
                    freedTableNames.Add(table.TableName);
                }
            }

            await _dbContext.SaveChangesAsync();

            // COMMIT TOÀN BỘ TRANSACTION AN TOÀN
            await transaction.CommitAsync();

            // PHÁT SÓNG SIGNALR REALTIME TỨC THÌ (Giải phóng bàn & cập nhật màn hình phục vụ + bếp)
            try
            {
                await _hubContext.Clients.All.SendAsync("StaffOrderUpdated");
                await _hubContext.Clients.All.SendAsync("KitchenUpdated");
            }
            catch (Exception hubEx)
            {
                Console.WriteLine($"[SignalR StaffPayment] Broadcast error: {hubEx.Message}");
            }

            decimal changeToReturn = 0;
            if (paymentMethod == "cash" && request.AmountPaidByCustomer > finalTotal)
            {
                changeToReturn = request.AmountPaidByCustomer - finalTotal;
            }

            return Ok(new
            {
                message = $"Thanh toán thành công đơn hàng #{order.OrderId}! Đã xuất hóa đơn {invoice.InvoiceCode} và giải phóng {freedTableNames.Count} bàn ăn.",
                invoice = new
                {
                    invoice.InvoiceId,
                    invoice.InvoiceCode,
                    invoice.OrderId,
                    invoice.Subtotal,
                    invoice.DiscountAmount,
                    VoucherDiscount = discountAmount,
                    PointDiscount = pointDiscount,
                    invoice.PointsUsed,
                    invoice.PointsEarned,
                    invoice.TotalAmount,
                    invoice.InvoiceStatus,
                    invoice.IssuedAt,
                    PaymentMethod = paymentMethod,
                    AmountPaidByCustomer = request.AmountPaidByCustomer,
                    ChangeToReturn = changeToReturn,
                    FreedTables = freedTableNames,
                    Customer = customer != null ? new
                    {
                        customer.CustomerId,
                        customer.FullName,
                        customer.Phone,
                        NewPointsBalance = customer.PointsBalance
                    } : null
                }
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new
            {
                message = "Đã xảy ra lỗi trong quá trình thực hiện transaction thanh toán: " + ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// [GET] Lấy chi tiết hóa đơn đầy đủ (danh sách món, bàn ăn, khách hàng, phương thức thanh toán) để hiển thị và in bill K80
    /// URL: /api/staff/payment/invoices/{invoiceId}
    /// </summary>
    /// <param name="invoiceId">Mã hóa đơn</param>
    [HttpGet("invoices/{invoiceId}")]
    public async Task<IActionResult> GetInvoiceDetail(int invoiceId)
    {
        var invoice = await _dbContext.Invoices
            .Include(inv => inv.Order)
                .ThenInclude(o => o.OrderTables)
                    .ThenInclude(ot => ot.Table)
            .Include(inv => inv.Customer)
            .Include(inv => inv.IssuedByNavigation)
            .Include(inv => inv.InvoiceItems)
            .Include(inv => inv.Payments)
            .FirstOrDefaultAsync(inv => inv.InvoiceId == invoiceId);

        if (invoice == null)
            return NotFound(new { message = $"Không tìm thấy hóa đơn #{invoiceId}." });

        var tables = invoice.Order.OrderTables
            .Select(ot => new
            {
                ot.TableId,
                ot.Table.TableName,
                ot.Table.AreaName,
                ot.IsPrimary
            })
            .ToList();

        var primaryTable = tables.FirstOrDefault(t => t.IsPrimary) ?? tables.FirstOrDefault();
        string tableDisplay = primaryTable?.TableName ?? "Bàn ?";
        var secondaries = tables.Where(t => !t.IsPrimary).Select(t => t.TableName).ToList();
        if (secondaries.Count > 0)
        {
            tableDisplay += " (+ " + string.Join(", ", secondaries) + ")";
        }

        return Ok(new
        {
            invoice.InvoiceId,
            invoice.InvoiceCode,
            invoice.OrderId,
            invoice.IssuedAt,
            CashierName = invoice.IssuedByNavigation?.FullName ?? "Thu Ngân",
            TableDisplay = tableDisplay,
            Tables = tables,
            Customer = invoice.Customer != null ? new
            {
                invoice.Customer.FullName,
                invoice.Customer.Phone,
                invoice.Customer.PointsBalance
            } : null,
            invoice.Subtotal,
            invoice.DiscountAmount,
            invoice.PointsUsed,
            invoice.PointsEarned,
            invoice.TotalAmount,
            Payments = invoice.Payments.Select(p => new
            {
                p.PaymentMethod,
                p.Amount,
                p.TransactionCode,
                p.PaidAt
            }).ToList(),
            Items = invoice.InvoiceItems.Select(i => new
            {
                i.InvoiceItemId,
                i.FoodNameSnapshot,
                i.VariantSnapshot,
                i.UnitPrice,
                i.Quantity,
                i.LineTotal,
                i.Note
            }).ToList()
        });
    }

    /// <summary>
    /// [GET] Lấy danh sách các hóa đơn thanh toán gần đây phục vụ tra cứu và in lại bill
    /// URL: /api/staff/payment/recent-invoices?limit=20
    /// </summary>
    /// <param name="limit">Số lượng hóa đơn tối đa</param>
    [HttpGet("recent-invoices")]
    public async Task<IActionResult> GetRecentInvoices([FromQuery] int limit = 20)
    {
        var invoices = await _dbContext.Invoices
            .Include(inv => inv.Order)
                .ThenInclude(o => o.OrderTables)
                    .ThenInclude(ot => ot.Table)
            .Include(inv => inv.Customer)
            .Include(inv => inv.IssuedByNavigation)
            .Include(inv => inv.Payments)
            .OrderByDescending(inv => inv.IssuedAt)
            .Take(limit)
            .Select(inv => new
            {
                inv.InvoiceId,
                inv.InvoiceCode,
                inv.OrderId,
                TableDisplay = string.Join(", ", inv.Order.OrderTables.Select(ot => ot.Table.TableName)),
                inv.Subtotal,
                inv.DiscountAmount,
                inv.PointsUsed,
                inv.PointsEarned,
                inv.TotalAmount,
                inv.InvoiceStatus,
                inv.IssuedAt,
                CashierName = inv.IssuedByNavigation.FullName,
                CustomerName = inv.Customer != null ? inv.Customer.FullName : "Khách vãng lai",
                CustomerPhone = inv.Customer != null ? inv.Customer.Phone : null,
                PaymentMethod = inv.Payments.FirstOrDefault() != null ? inv.Payments.FirstOrDefault()!.PaymentMethod : "cash"
            })
            .ToListAsync();

        return Ok(invoices);
    }

    /// <summary>
    /// [GET] Lấy cấu hình tài khoản ngân hàng nhận tiền VietQR hiện tại
    /// URL: /api/staff/payment/bank-config
    /// </summary>
    [HttpGet("bank-config")]
    public IActionResult GetBankConfigEndpoint()
    {
        return Ok(GetBankConfig());
    }

    /// <summary>
    /// [POST] Cập nhật số tài khoản ngân hàng nhận tiền VietQR
    /// URL: /api/staff/payment/bank-config
    /// </summary>
    /// <param name="request">Thông tin tài khoản ngân hàng</param>
    [HttpPost("bank-config")]
    public IActionResult UpdateBankConfigEndpoint([FromBody] BankConfigRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AccountNo))
            return BadRequest(new { message = "Vui lòng nhập số tài khoản ngân hàng." });
        if (string.IsNullOrWhiteSpace(request.AccountName))
            return BadRequest(new { message = "Vui lòng nhập tên chủ tài khoản." });

        var config = new BankConfigRequest
        {
            BankId = string.IsNullOrWhiteSpace(request.BankId) ? "MB" : request.BankId.Trim().ToUpper(),
            AccountNo = request.AccountNo.Trim(),
            AccountName = request.AccountName.Trim().ToUpper()
        };

        SaveBankConfig(config);
        return Ok(new { message = "Đã cập nhật thông tin tài khoản ngân hàng nhận tiền thành công!", config });
    }
}
