using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;
using System.Text.Json;

namespace Restaurant.Api.Controllers.Admin;

public class KitchenDecisionDto
{
    public long AuditLogId { get; set; }
    public string ActionName { get; set; } = string.Empty;
    public string DecisionType { get; set; } = string.Empty; // sold_out | reopened | cancelled | undo | ready_all | portion
    public string DecisionTitle { get; set; } = string.Empty;
    public string Severity { get; set; } = "info"; // danger | warning | success | info
    public int? ChefId { get; set; }
    public string ChefName { get; set; } = "Hệ thống";
    public string ChefRole { get; set; } = "chef";
    public string? ChefEmail { get; set; }
    public int? FoodId { get; set; }
    public string? FoodName { get; set; }
    public int? VariantId { get; set; }
    public string? VariantName { get; set; }
    public int? OrderId { get; set; }
    public int? OrderItemId { get; set; }
    public string? TableDisplay { get; set; }
    public string? Reason { get; set; }
    public string? DetailsSummary { get; set; }
    public DateTime CreatedAt { get; set; }
    public object? RawData { get; set; }
}

public class KitchenHistorySummaryDto
{
    public int TotalDecisions { get; set; }
    public int SoldOutCount { get; set; }
    public int ReopenedCount { get; set; }
    public int CancelledCount { get; set; }
    public int UndoCount { get; set; }
    public int ReadyAllCount { get; set; }
    public int TodayCount { get; set; }
    public List<ReasonStatDto> TopReasons { get; set; } = new();
}

public class ReasonStatDto
{
    public string Reason { get; set; } = string.Empty;
    public int Count { get; set; }
    public string ActionType { get; set; } = string.Empty;
}

[ApiController]
[Route("api/admin/kitchen-history")]
[Authorize(Roles = "admin,manager")]
public class AdminKitchenHistoryController : ControllerBase
{
    private readonly RestaurantDbContext _dbContext;

    public AdminKitchenHistoryController(RestaurantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// [GET] Lấy danh sách lịch sử các quyết định của Quản lý Bếp / Bếp trưởng
    /// URL: /api/admin/kitchen-history/decisions
    /// </summary>
    [HttpGet("decisions")]
    public async Task<IActionResult> GetKitchenDecisions(
        [FromQuery] string? actionType = "all",
        [FromQuery] int? chefId = null,
        [FromQuery] string? search = null,
        [FromQuery] string? fromDate = null,
        [FromQuery] string? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var validKitchenActions = new[]
        {
            "food_sold_out",
            "food_reopened",
            "kitchen_item_cancelled",
            "kitchen_item_undo",
            "kitchen_ready_all",
            "order_cancelled"
        };

        var query = _dbContext.AuditLogs
            .Include(a => a.User)
                .ThenInclude(u => u!.Role)
            .Where(a => validKitchenActions.Contains(a.ActionName))
            .AsQueryable();

        // 1. Lọc theo loại quyết định
        if (!string.IsNullOrWhiteSpace(actionType) && actionType != "all")
        {
            switch (actionType.ToLower().Trim())
            {
                case "sold_out":
                    query = query.Where(a => a.ActionName == "food_sold_out");
                    break;
                case "reopened":
                    query = query.Where(a => a.ActionName == "food_reopened");
                    break;
                case "cancelled":
                    query = query.Where(a => a.ActionName == "kitchen_item_cancelled" || a.ActionName == "order_cancelled");
                    break;
                case "undo":
                    query = query.Where(a => a.ActionName == "kitchen_item_undo");
                    break;
                case "ready_all":
                    query = query.Where(a => a.ActionName == "kitchen_ready_all");
                    break;
            }
        }

        // 2. Lọc theo Bếp trưởng / Người quyết định
        if (chefId.HasValue && chefId.Value > 0)
        {
            query = query.Where(a => a.UserId == chefId.Value);
        }

        // 3. Lọc theo khoảng ngày
        if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParse(fromDate, out var fromD))
        {
            query = query.Where(a => a.CreatedAt >= fromD.Date);
        }

        if (!string.IsNullOrWhiteSpace(toDate) && DateTime.TryParse(toDate, out var toD))
        {
            query = query.Where(a => a.CreatedAt < toD.Date.AddDays(1));
        }

        // 4. Lọc tìm kiếm từ khóa
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(a =>
                (a.NewData != null && a.NewData.Contains(s)) ||
                (a.User != null && a.User.FullName.Contains(s)) ||
                a.EntityId == s);
        }

        int totalItems = await query.CountAsync();

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Để lấy thông tin bàn ăn cho các món bị hủy/hoàn tác
        var orderIds = new HashSet<int>();
        foreach (var log in logs)
        {
            if (!string.IsNullOrWhiteSpace(log.NewData))
            {
                try
                {
                    using var doc = JsonDocument.Parse(log.NewData);
                    if (doc.RootElement.TryGetProperty("orderId", out var oProp) && oProp.TryGetInt32(out var oid))
                    {
                        orderIds.Add(oid);
                    }
                }
                catch { }
            }
            if (log.EntityName == "Order" && int.TryParse(log.EntityId, out var oIdFromEntity))
            {
                orderIds.Add(oIdFromEntity);
            }
        }

        var orderTablesMap = new Dictionary<int, string>();
        if (orderIds.Count > 0)
        {
            var orders = await _dbContext.Orders
                .Where(o => orderIds.Contains(o.OrderId))
                .Include(o => o.OrderTables)
                    .ThenInclude(ot => ot.Table)
                .Select(o => new
                {
                    o.OrderId,
                    TableDisplay = o.OrderTables.FirstOrDefault(ot => ot.IsPrimary) != null
                        ? o.OrderTables.FirstOrDefault(ot => ot.IsPrimary)!.Table.TableName
                        : (o.OrderTables.FirstOrDefault() != null ? o.OrderTables.FirstOrDefault()!.Table.TableName : "Mang về")
                })
                .ToListAsync();

            orderTablesMap = orders.ToDictionary(o => o.OrderId, o => o.TableDisplay);
        }

        // Parse NewData JSON và ánh xạ sang DTO chuẩn
        var items = logs.Select(log =>
        {
            var dto = new KitchenDecisionDto
            {
                AuditLogId = log.AuditLogId,
                ActionName = log.ActionName,
                CreatedAt = log.CreatedAt,
                ChefId = log.UserId,
                ChefName = log.User?.FullName ?? "Bếp viên",
                ChefRole = log.User?.Role?.RoleName ?? "Bếp",
                ChefEmail = log.User?.Email
            };

            // Phân loại quyết định
            switch (log.ActionName)
            {
                case "food_sold_out":
                    dto.DecisionType = "sold_out";
                    dto.DecisionTitle = "Báo Hết Món";
                    dto.Severity = "warning";
                    break;
                case "food_reopened":
                    dto.DecisionType = "reopened";
                    dto.DecisionTitle = "Mở Bán Lại Món";
                    dto.Severity = "success";
                    break;
                case "kitchen_item_cancelled":
                    dto.DecisionType = "cancelled";
                    dto.DecisionTitle = "Bếp Từ Chối / Hủy Món";
                    dto.Severity = "danger";
                    break;
                case "kitchen_item_undo":
                    dto.DecisionType = "undo";
                    dto.DecisionTitle = "Hoàn Tác / Nấu Lại";
                    dto.Severity = "warning";
                    break;
                case "kitchen_ready_all":
                    dto.DecisionType = "ready_all";
                    dto.DecisionTitle = "Nấu Xong Toàn Bộ Bàn";
                    dto.Severity = "success";
                    break;
                case "order_cancelled":
                    dto.DecisionType = "cancelled";
                    dto.DecisionTitle = "Hủy Đơn Hàng";
                    dto.Severity = "danger";
                    break;
                default:
                    dto.DecisionType = "other";
                    dto.DecisionTitle = log.ActionName;
                    dto.Severity = "info";
                    break;
            }

            // Parse NewData JSON
            if (!string.IsNullOrWhiteSpace(log.NewData))
            {
                try
                {
                    using var doc = JsonDocument.Parse(log.NewData);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("reason", out var rProp))
                        dto.Reason = rProp.GetString();

                    if (root.TryGetProperty("foodName", out var fnProp))
                        dto.FoodName = fnProp.GetString();

                    if (root.TryGetProperty("variantName", out var vnProp))
                        dto.VariantName = vnProp.GetString();

                    if (root.TryGetProperty("foodId", out var fidProp) && fidProp.TryGetInt32(out var fid))
                        dto.FoodId = fid;

                    if (root.TryGetProperty("variantId", out var vidProp) && vidProp.TryGetInt32(out var vid))
                        dto.VariantId = vid;

                    if (root.TryGetProperty("orderId", out var oidProp) && oidProp.TryGetInt32(out var oid))
                    {
                        dto.OrderId = oid;
                        if (orderTablesMap.TryGetValue(oid, out var tbl))
                            dto.TableDisplay = tbl;
                    }

                    if (root.TryGetProperty("itemId", out var itmProp) && itmProp.TryGetInt32(out var iid))
                        dto.OrderItemId = iid;

                    if (root.TryGetProperty("orderItemId", out var oitmProp) && oitmProp.TryGetInt32(out var oiid))
                        dto.OrderItemId = oiid;

                    if (root.TryGetProperty("itemsCount", out var icProp) && icProp.TryGetInt32(out var ic))
                        dto.DetailsSummary = $"Nấu xong {ic} món ăn cùng lúc";

                    if (root.TryGetProperty("previousStatus", out var psProp) && root.TryGetProperty("targetStatus", out var tsProp))
                        dto.DetailsSummary = $"Chuyển từ '{psProp.GetString()}' về '{tsProp.GetString()}'";

                    dto.RawData = JsonSerializer.Deserialize<object>(log.NewData);
                }
                catch { }
            }

            // Fallback hiển thị bàn nếu log lưu OrderId ở EntityId
            if (!dto.OrderId.HasValue && log.EntityName == "Order" && int.TryParse(log.EntityId, out var directOid))
            {
                dto.OrderId = directOid;
                if (orderTablesMap.TryGetValue(directOid, out var tbl))
                    dto.TableDisplay = tbl;
            }

            return dto;
        }).ToList();

        return Ok(new
        {
            items,
            page,
            pageSize,
            totalItems,
            totalPages = (int)Math.Ceiling((double)totalItems / pageSize)
        });
    }

    /// <summary>
    /// [GET] Thống kê tổng quan các quyết định của bếp (Summary KPI Cards)
    /// URL: /api/admin/kitchen-history/summary
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetKitchenSummary(
        [FromQuery] string? fromDate = null,
        [FromQuery] string? toDate = null)
    {
        var validKitchenActions = new[]
        {
            "food_sold_out",
            "food_reopened",
            "kitchen_item_cancelled",
            "kitchen_item_undo",
            "kitchen_ready_all",
            "order_cancelled"
        };

        var query = _dbContext.AuditLogs
            .Where(a => validKitchenActions.Contains(a.ActionName))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParse(fromDate, out var fromD))
        {
            query = query.Where(a => a.CreatedAt >= fromD.Date);
        }

        if (!string.IsNullOrWhiteSpace(toDate) && DateTime.TryParse(toDate, out var toD))
        {
            query = query.Where(a => a.CreatedAt < toD.Date.AddDays(1));
        }

        var today = DateTime.UtcNow.Date;

        var logs = await query
            .Select(a => new { a.ActionName, a.NewData, a.CreatedAt })
            .ToListAsync();

        var summary = new KitchenHistorySummaryDto
        {
            TotalDecisions = logs.Count,
            SoldOutCount = logs.Count(l => l.ActionName == "food_sold_out"),
            ReopenedCount = logs.Count(l => l.ActionName == "food_reopened"),
            CancelledCount = logs.Count(l => l.ActionName == "kitchen_item_cancelled" || l.ActionName == "order_cancelled"),
            UndoCount = logs.Count(l => l.ActionName == "kitchen_item_undo"),
            ReadyAllCount = logs.Count(l => l.ActionName == "kitchen_ready_all"),
            TodayCount = logs.Count(l => l.CreatedAt >= today)
        };

        // Trích xuất các lý do phổ biến nhất
        var reasonsMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var log in logs)
        {
            if (string.IsNullOrWhiteSpace(log.NewData)) continue;
            try
            {
                using var doc = JsonDocument.Parse(log.NewData);
                if (doc.RootElement.TryGetProperty("reason", out var rProp))
                {
                    var r = rProp.GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(r))
                    {
                        reasonsMap[r] = reasonsMap.GetValueOrDefault(r, 0) + 1;
                    }
                }
            }
            catch { }
        }

        summary.TopReasons = reasonsMap
            .OrderByDescending(kv => kv.Value)
            .Take(5)
            .Select(kv => new ReasonStatDto
            {
                Reason = kv.Key,
                Count = kv.Value,
                ActionType = "Lý do bếp"
            })
            .ToList();

        return Ok(summary);
    }

    /// <summary>
    /// [GET] Lấy danh sách các nhân sự bếp đã từng ra quyết định để chọn lọc
    /// URL: /api/admin/kitchen-history/chefs
    /// </summary>
    [HttpGet("chefs")]
    public async Task<IActionResult> GetKitchenChefs()
    {
        var validKitchenActions = new[]
        {
            "food_sold_out",
            "food_reopened",
            "kitchen_item_cancelled",
            "kitchen_item_undo",
            "kitchen_ready_all"
        };

        var userIds = await _dbContext.AuditLogs
            .Where(a => validKitchenActions.Contains(a.ActionName) && a.UserId.HasValue)
            .Select(a => a.UserId!.Value)
            .Distinct()
            .ToListAsync();

        var chefs = await _dbContext.Users
            .Where(u => userIds.Contains(u.UserId))
            .Include(u => u.Role)
            .Select(u => new
            {
                u.UserId,
                u.FullName,
                u.Email,
                RoleName = u.Role.RoleName
            })
            .ToListAsync();

        return Ok(chefs);
    }
}
