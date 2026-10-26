using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Restaurant.Api.Controllers.Webhook;
using Restaurant.Api.Data;
using Restaurant.Api.Hubs;
using System.Text.Json;

namespace Restaurant.Api.Controllers.Admin;

public class AdminPaymentSettingsModel
{
    public string BankId { get; set; } = "MB";
    public string AccountNo { get; set; } = "0333888999";
    public string AccountName { get; set; } = "NHA HANG HUONG SEN";
    public string Branch { get; set; } = "Hồ Chí Minh";
    public string WebhookSecret { get; set; } = string.Empty;
    public bool IsAutoConfirmEnabled { get; set; } = true;
    public string GatewayProvider { get; set; } = "SePay / VietQR Auto";
}

public class SimulateWebhookRequest
{
    public int OrderId { get; set; }
    public decimal? Amount { get; set; }
    public string? Content { get; set; }
}

[ApiController]
[Route("api/admin/payment-settings")]
[Authorize(Roles = "admin")]
public class AdminPaymentController : ControllerBase
{
    private readonly RestaurantDbContext _dbContext;
    private readonly IHubContext<RestaurantHub> _hubContext;

    public AdminPaymentController(RestaurantDbContext dbContext, IHubContext<RestaurantHub> hubContext)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    private static readonly string BankConfigFilePath = Path.Combine(AppContext.BaseDirectory, "bank_config.json");

    private AdminPaymentSettingsModel LoadConfig()
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
                    var cfg = JsonSerializer.Deserialize<AdminPaymentSettingsModel>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (cfg != null && !string.IsNullOrWhiteSpace(cfg.AccountNo))
                    {
                        return cfg;
                    }
                }
            }
        }
        catch { }

        return new AdminPaymentSettingsModel();
    }

    private void SaveConfig(AdminPaymentSettingsModel config)
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
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

    /// <summary>
    /// [GET] Lấy cấu hình tài khoản nhận tiền VietQR và tham số Webhook dành cho Quản lý
    /// URL: /api/admin/payment-settings
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPaymentSettings()
    {
        var config = LoadConfig();
        string webhookUrl = $"{Request.Scheme}://{Request.Host}/api/payment/bank-webhook";

        // Lấy danh sách các đơn hàng đang phục vụ chưa thanh toán để phục vụ giả lập Webhook test
        var activeOrders = await _dbContext.Orders
            .Where(o => o.OrderStatus == "open" || o.OrderStatus == "preparing" || o.OrderStatus == "serving")
            .Include(o => o.OrderTables)
                .ThenInclude(ot => ot.Table)
            .OrderByDescending(o => o.OrderId)
            .Take(10)
            .Select(o => new
            {
                o.OrderId,
                o.OrderStatus,
                TableDisplay = o.OrderTables.FirstOrDefault(ot => ot.IsPrimary) != null 
                    ? o.OrderTables.FirstOrDefault(ot => ot.IsPrimary)!.Table.TableName 
                    : (o.OrderTables.FirstOrDefault() != null ? o.OrderTables.FirstOrDefault()!.Table.TableName : "Bàn không tên"),
                o.TotalAmount,
                o.Subtotal,
                o.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            config,
            webhookUrl,
            activeOrders
        });
    }

    /// <summary>
    /// [POST] Cập nhật thông tin tài khoản ngân hàng nhận tiền và khóa bảo mật Webhook
    /// URL: /api/admin/payment-settings
    /// </summary>
    /// <param name="request">Thông tin cấu hình ngân hàng</param>
    [HttpPost]
    public IActionResult UpdatePaymentSettings([FromBody] AdminPaymentSettingsModel request)
    {
        if (string.IsNullOrWhiteSpace(request.AccountNo))
            return BadRequest(new { message = "Vui lòng nhập số tài khoản ngân hàng." });
        if (string.IsNullOrWhiteSpace(request.AccountName))
            return BadRequest(new { message = "Vui lòng nhập tên chủ tài khoản." });
        if (string.IsNullOrWhiteSpace(request.WebhookSecret) || request.WebhookSecret.Trim().Length < 8)
            return BadRequest(new { message = "Khóa webhook phải có ít nhất 8 ký tự." });

        var config = new AdminPaymentSettingsModel
        {
            BankId = string.IsNullOrWhiteSpace(request.BankId) ? "MB" : request.BankId.Trim().ToUpper(),
            AccountNo = request.AccountNo.Trim(),
            AccountName = request.AccountName.Trim().ToUpper(),
            Branch = request.Branch?.Trim() ?? string.Empty,
            WebhookSecret = request.WebhookSecret.Trim(),
            IsAutoConfirmEnabled = request.IsAutoConfirmEnabled,
            GatewayProvider = string.IsNullOrWhiteSpace(request.GatewayProvider) ? "SePay / VietQR Auto" : request.GatewayProvider.Trim()
        };

        SaveConfig(config);
        return Ok(new { message = "Đã lưu cấu hình tài khoản ngân hàng và Webhook thành công!", config });
    }

    /// <summary>
    /// [POST] Giả lập Webhook ngân hàng báo có biến động số dư phục vụ kiểm thử hệ thống
    /// URL: /api/admin/payment-settings/simulate-webhook
    /// </summary>
    /// <param name="request">Thông tin đơn hàng và số tiền giả lập</param>
    [HttpPost("simulate-webhook")]
    public async Task<IActionResult> SimulateBankWebhook([FromBody] SimulateWebhookRequest request)
    {
        if (request.OrderId <= 0)
        {
            return BadRequest(new { message = "Vui lòng chọn hoặc nhập mã đơn hàng hợp lệ." });
        }

        var order = await _dbContext.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.OrderId == request.OrderId);

        if (order == null)
        {
            return NotFound(new { message = $"Không tìm thấy đơn hàng #{request.OrderId}." });
        }

        decimal subtotal = order.OrderItems.Where(i => i.ItemStatus != "cancelled").Sum(i => i.UnitPrice * i.Quantity);
        decimal finalTotal = order.TotalAmount > 0 ? order.TotalAmount : subtotal;
        decimal payAmount = request.Amount ?? finalTotal;

        var payload = new BankWebhookPayload
        {
            Gateway = "MBBank",
            TransferAmount = payAmount,
            Content = string.IsNullOrWhiteSpace(request.Content) ? $"ORD{order.OrderId:D4}" : request.Content.Trim(),
            ReferenceCode = $"SIM-{DateTime.UtcNow:yyyyMMddHHmmss}-{order.OrderId}",
            TransactionDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        var webhookCtrl = new BankWebhookController(_dbContext, _hubContext);
        return await webhookCtrl.ReceiveBankWebhook(payload);
    }

    /// <summary>
    /// [GET] Lấy lịch sử các giao dịch chuyển khoản VietQR gần đây
    /// URL: /api/admin/payment-settings/recent-transactions?limit=20
    /// </summary>
    /// <param name="limit">Số lượng giao dịch tối đa</param>
    [HttpGet("recent-transactions")]
    public async Task<IActionResult> GetRecentBankTransactions([FromQuery] int limit = 20)
    {
        var payments = await _dbContext.Payments
            .Where(p => p.PaymentMethod == "transfer")
            .Include(p => p.Invoice)
                .ThenInclude(i => i.Order)
                    .ThenInclude(o => o.OrderTables)
                        .ThenInclude(ot => ot.Table)
            .Include(p => p.Invoice.Customer)
            .OrderByDescending(p => p.PaidAt)
            .Take(limit)
            .Select(p => new
            {
                p.PaymentId,
                p.InvoiceId,
                p.Invoice.InvoiceCode,
                p.Invoice.OrderId,
                p.Amount,
                p.TransactionCode,
                p.PaymentStatus,
                p.PaidAt,
                TableDisplay = p.Invoice.Order.OrderTables.FirstOrDefault(ot => ot.IsPrimary) != null 
                    ? p.Invoice.Order.OrderTables.FirstOrDefault(ot => ot.IsPrimary)!.Table.TableName 
                    : (p.Invoice.Order.OrderTables.FirstOrDefault() != null ? p.Invoice.Order.OrderTables.FirstOrDefault()!.Table.TableName : "Bàn"),
                CustomerName = p.Invoice.Customer != null ? p.Invoice.Customer.FullName : "Khách vãng lai"
            })
            .ToListAsync();

        return Ok(payments);
    }
}
