using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Restaurant.Api.Hubs;

namespace Restaurant.Api.Controllers.Admin;

public class ManagerNotificationItem
{
    public string Id { get; set; } = string.Empty;
    public string Category { get; set; } = "general"; // kitchen | payment | reservation | inventory
    public string Icon { get; set; } = "🔔";
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Severity { get; set; } = "info"; // info | success | warning | danger
    public string? Link { get; set; }
    public string Actor { get; set; } = "Hệ thống";
    public DateTime CreatedAt { get; set; }
    public bool Read { get; set; } = false;
}

[ApiController]
[Route("api/admin/notifications")]
[Authorize(Roles = "admin,manager")]
public class AdminNotificationController : ControllerBase
{
    private readonly RestaurantDbContext _dbContext;
    private readonly IHubContext<RestaurantHub> _hubContext;

    public AdminNotificationController(RestaurantDbContext dbContext, IHubContext<RestaurantHub> hubContext)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    /// <summary>
    /// [GET] Lấy danh sách thông báo các sự kiện hoạt động của nhà hàng dành cho Quản lý
    /// URL: /api/admin/notifications
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetManagerNotifications(
        [FromQuery] string category = "all",
        [FromQuery] int limit = 30)
    {
        if (limit < 1 || limit > 100) limit = 30;
        var notifications = new List<ManagerNotificationItem>();

        var cat = category.ToLower().Trim();

        // 1. SỰ KIỆN TỪ BẾP & QUYẾT ĐỊNH BẾP TRƯỞNG (KDS & Sold Out / Cancellation)
        if (cat == "all" || cat == "kitchen")
        {
            var validKitchenActions = new[] { "food_sold_out", "food_reopened", "kitchen_item_cancelled", "kitchen_item_undo", "kitchen_ready_all" };
            var kitchenLogs = await _dbContext.AuditLogs
                .Include(a => a.User)
                .Where(a => validKitchenActions.Contains(a.ActionName))
                .OrderByDescending(a => a.CreatedAt)
                .Take(limit)
                .ToListAsync();

            foreach (var log in kitchenLogs)
            {
                string title = "Sự kiện bếp";
                string icon = "🍳";
                string severity = "info";
                string detail = string.Empty;
                string? foodName = null;
                string? reason = null;

                if (!string.IsNullOrWhiteSpace(log.NewData))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(log.NewData);
                        if (doc.RootElement.TryGetProperty("foodName", out var fnProp))
                            foodName = fnProp.GetString();
                        if (doc.RootElement.TryGetProperty("reason", out var rProp))
                            reason = rProp.GetString();
                    }
                    catch { }
                }

                switch (log.ActionName)
                {
                    case "food_sold_out":
                        title = "Bếp báo hết món";
                        icon = "🛑";
                        severity = "warning";
                        detail = $"Món '{foodName ?? "Món ăn"}' vừa được báo hết hàng. {(string.IsNullOrWhiteSpace(reason) ? "" : $"Lý do: {reason}")}";
                        break;
                    case "food_reopened":
                        title = "Bếp mở bán lại món";
                        icon = "✅";
                        severity = "success";
                        detail = $"Món '{foodName ?? "Món ăn"}' đã chuẩn bị xong nguyên liệu và mở phục vụ trở lại.";
                        break;
                    case "kitchen_item_cancelled":
                        title = "Bếp từ chối / hủy món";
                        icon = "❌";
                        severity = "danger";
                        detail = $"Món '{foodName ?? "Món ăn"}' bị bếp từ chối phục vụ. {(string.IsNullOrWhiteSpace(reason) ? "" : $"Lý do: {reason}")}";
                        break;
                    case "kitchen_item_undo":
                        title = "Bếp yêu cầu nấu lại món";
                        icon = "🔄";
                        severity = "warning";
                        detail = $"Món '{foodName ?? "Món ăn"}' được hoàn tác trạng thái để chế biến lại.";
                        break;
                    case "kitchen_ready_all":
                        title = "Hoàn tất bàn ăn";
                        icon = "✨";
                        severity = "success";
                        detail = "Bếp đã hoàn thành nấu toàn bộ món cho bàn ăn.";
                        break;
                }

                notifications.Add(new ManagerNotificationItem
                {
                    Id = $"kitchen-{log.AuditLogId}",
                    Category = "kitchen",
                    Icon = icon,
                    Title = title,
                    Detail = detail,
                    Severity = severity,
                    Link = "/admin/kitchen-decisions",
                    Actor = log.User?.FullName ?? "Bếp",
                    CreatedAt = log.CreatedAt
                });
            }
        }

        // 2. SỰ KIỆN ĐẶT BÀN (RESERVATIONS)
        if (cat == "all" || cat == "reservation")
        {
            var recentReservations = await _dbContext.Reservations
                .OrderByDescending(r => r.CreatedAt)
                .Take(limit)
                .ToListAsync();

            foreach (var r in recentReservations)
            {
                string icon = "📅";
                string severity = "info";
                string title = "Đặt bàn mới";
                string detail = $"Khách {r.CustomerName} ({r.CustomerPhone}) đặt bàn {r.GuestCount} khách vào lúc {r.ReservationDate:yyyy-MM-dd HH:mm}.";

                if (r.ReservationStatus == "confirmed")
                {
                    icon = "✓";
                    title = "Đã nhận cọc / xác nhận đặt bàn";
                    severity = "success";
                }
                else if (r.ReservationStatus == "cancelled")
                {
                    icon = "↩";
                    title = "Khách hủy đặt bàn";
                    severity = "danger";
                }

                notifications.Add(new ManagerNotificationItem
                {
                    Id = $"res-{r.ReservationId}",
                    Category = "reservation",
                    Icon = icon,
                    Title = title,
                    Detail = detail,
                    Severity = severity,
                    Link = "/admin/reservations",
                    Actor = r.CustomerName,
                    CreatedAt = r.CreatedAt
                });
            }
        }

        // Sắp xếp thời gian giảm dần
        var sorted = notifications
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToList();

        return Ok(sorted);
    }

    /// <summary>
    /// [POST] Bắn thông báo test tới quản lý qua SignalR
    /// URL: /api/admin/notifications/broadcast-test
    /// </summary>
    [HttpPost("broadcast-test")]
    public async Task<IActionResult> BroadcastTestNotification([FromBody] JsonElement body)
    {
        string title = body.TryGetProperty("title", out var tProp) ? tProp.GetString() ?? "Thông báo quản lý" : "Thông báo kiểm tra";
        string detail = body.TryGetProperty("detail", out var dProp) ? dProp.GetString() ?? "Hệ thống thông báo hoạt động bình thường." : "Test thành công.";
        string category = body.TryGetProperty("category", out var cProp) ? cProp.GetString() ?? "kitchen" : "kitchen";

        var payload = new
        {
            title,
            detail,
            category,
            icon = category == "kitchen" ? "🍳" : "🔔",
            severity = "info",
            createdAt = DateTime.UtcNow
        };

        try
        {
            await _hubContext.PublishAsync(_dbContext, "ManagerNotification", payload);
            return Ok(new { success = true, message = "Đã phát thông báo thử nghiệm thành công tới toàn bộ quản lý!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi phát thông báo: " + ex.Message });
        }
    }
}
