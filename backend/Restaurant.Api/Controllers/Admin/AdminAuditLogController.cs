using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;
using System.Text.Json;

namespace Restaurant.Api.Controllers.Admin;

public class AuditLogResponseItem
{
    public long AuditLogId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? UserId { get; set; }
    public string UserName { get; set; } = "Hệ thống";
    public string UserEmail { get; set; } = string.Empty;
    public string UserRole { get; set; } = "Hệ thống";
    
    // 3 DẠNG CHÍNH THEO ROADMAP & YÊU CẦU:
    // 1. system: Hệ thống & Quản trị con người (Tài khoản, quyền, bảo mật do Admin thực hiện)
    // 2. inventory: Kho & Giá trị tài sản (Phiếu kho, duyệt nhập xuất, hao hụt kho, tiền hàng)
    // 3. customer: Khách hàng, Thực đơn & Vận hành Bếp (Đặt bàn, giá món, quyết định của Bếp trưởng)
    public string Category { get; set; } = "system"; // system | inventory | customer
    public string CategoryLabel { get; set; } = "Hệ thống & Con người";
    public string CategoryIcon { get; set; } = "⚙️";
    
    public string ActionName { get; set; } = string.Empty;
    public string ActionGroup { get; set; } = "update"; // create | update | delete_lock | approve | cancel | kitchen
    public string ActionBadge { get; set; } = "Thao tác";
    public string ActionSeverity { get; set; } = "info"; // success | warning | danger | info | purple
    
    public string EntityName { get; set; } = string.Empty;
    public string EntityLabel { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    
    public string Summary { get; set; } = string.Empty;
    
    // Nổi bật giá trị tiền / hành động nhân sự / quyết định bếp
    public string? ValueHighlight { get; set; }
    public string? ValueHighlightType { get; set; } // money | human | alert | info
    
    public string? OldData { get; set; }
    public string? NewData { get; set; }
}

[ApiController]
[Route("api/admin/audit-logs")]
[Authorize(Roles = "admin,manager")]
public class AdminAuditLogController : ControllerBase
{
    private readonly RestaurantDbContext _dbContext;

    public AdminAuditLogController(RestaurantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// [GET] Lấy danh sách lịch sử thao tác & kiểm toán nhà hàng (Audit Trail - UC-I04)
    /// Chia thành 3 dạng chính:
    /// - system: Hệ thống & Quản trị con người (Nhân viên, phân quyền, khóa tài khoản)
    /// - inventory: Kho hàng & Giá trị tài sản (Nhập/xuất kho, duyệt kho, hủy hao hụt)
    /// - customer: Khách hàng, Thực đơn & Quyết định Bếp (Đặt bàn, đổi giá món, KDS Bếp)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? category = null,
        [FromQuery] string? actionGroup = null,
        [FromQuery] string? entityName = null,
        [FromQuery] int? userId = null,
        [FromQuery] string? fromDate = null,
        [FromQuery] string? toDate = null,
        [FromQuery] string? search = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = _dbContext.AuditLogs
            .Include(a => a.User)
                .ThenInclude(u => u!.Role)
            .AsNoTracking()
            .AsQueryable();

        // 1. Lọc theo 3 DẠNG CHÍNH
        if (!string.IsNullOrWhiteSpace(category) && category != "all")
        {
            var cat = category.Trim().ToLowerInvariant();
            switch (cat)
            {
                case "system":
                case "he_thong":
                    // Dạng 1: Hệ thống & Quản trị con người
                    query = query.Where(a => a.EntityName == "User" || a.EntityName == "Role" || a.EntityName == "System" || a.ActionName.StartsWith("user_"));
                    break;
                case "inventory":
                case "kho":
                    // Dạng 2: Liên quan đến kho & Giá trị tiền hàng hóa
                    query = query.Where(a => a.EntityName == "StockReceipt" || a.EntityName == "Inventory" || a.EntityName == "DailyPortionControl" || a.EntityName == "FoodIngredient" || a.ActionName.StartsWith("receipt_") || a.ActionName.StartsWith("stock_"));
                    break;
                case "customer":
                case "customer_kitchen":
                case "khach_hang":
                case "khach_hang_bep":
                    // Dạng 3: Quản trị khách hàng, thực đơn, giá món & Vận hành Bếp
                    query = query.Where(a => a.EntityName == "Reservation" || a.EntityName == "Food" || a.EntityName == "DiningTable" || a.EntityName == "OrderItem" || a.EntityName == "Order" || a.ActionName.StartsWith("kitchen_") || a.ActionName.StartsWith("food_") || a.ActionName.StartsWith("table_") || a.ActionName.StartsWith("reservation_"));
                    break;
            }
        }

        // 2. Lọc theo nhân sự
        if (userId.HasValue && userId.Value > 0)
        {
            query = query.Where(a => a.UserId == userId.Value);
        }

        // 3. Lọc theo thực thể cụ thể
        if (!string.IsNullOrWhiteSpace(entityName) && entityName != "all")
        {
            var en = entityName.Trim().ToLowerInvariant();
            query = query.Where(a => a.EntityName.ToLower() == en);
        }

        // 4. Lọc theo nhóm hành động chi tiết
        if (!string.IsNullOrWhiteSpace(actionGroup) && actionGroup != "all")
        {
            var ag = actionGroup.Trim().ToLowerInvariant();
            switch (ag)
            {
                case "create":
                    query = query.Where(a => a.ActionName.Contains("create") || a.ActionName == "insert");
                    break;
                case "update":
                    query = query.Where(a => (a.ActionName.Contains("update") || a.ActionName.Contains("edit") || a.ActionName.Contains("change")) && !a.ActionName.Contains("lock"));
                    break;
                case "delete_lock":
                    query = query.Where(a => a.ActionName.Contains("delete") || a.ActionName.Contains("lock") || a.ActionName.Contains("deactivate"));
                    break;
                case "approve":
                    query = query.Where(a => a.ActionName.Contains("approve") || a.ActionName.Contains("confirm") || a.ActionName.Contains("check_in"));
                    break;
                case "cancel":
                    query = query.Where(a => a.ActionName.Contains("cancel") || a.ActionName.Contains("reject"));
                    break;
                case "kitchen":
                    query = query.Where(a => a.ActionName.StartsWith("kitchen_") || a.ActionName.StartsWith("food_sold") || a.ActionName.StartsWith("food_reopened"));
                    break;
            }
        }

        // 5. Lọc theo khoảng ngày
        if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParse(fromDate, out var fromD))
        {
            query = query.Where(a => a.CreatedAt >= fromD.Date);
        }
        if (!string.IsNullOrWhiteSpace(toDate) && DateTime.TryParse(toDate, out var toD))
        {
            query = query.Where(a => a.CreatedAt < toD.Date.AddDays(1));
        }

        // 6. Tìm kiếm từ khóa (tên nhân viên, email, tên món, action, entityId, số tiền, nội dung)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            query = query.Where(a =>
                (a.User != null && (a.User.FullName.ToLower().Contains(s) || a.User.Email.ToLower().Contains(s))) ||
                a.ActionName.ToLower().Contains(s) ||
                a.EntityName.ToLower().Contains(s) ||
                (a.EntityId != null && a.EntityId.Contains(s)) ||
                (a.NewData != null && a.NewData.ToLower().Contains(s)) ||
                (a.OldData != null && a.OldData.ToLower().Contains(s))
            );
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var rawLogs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = rawLogs.Select(log => MapAuditLog(log)).ToList();

        return Ok(new
        {
            success = true,
            data = items,
            pagination = new
            {
                currentPage = page,
                pageSize,
                totalItems,
                totalPages
            }
        });
    }

    /// <summary>
    /// [GET] Lấy số liệu thống kê KPI theo 3 dạng chính
    /// URL: /api/admin/audit-logs/summary
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetAuditSummary()
    {
        var totalCount = await _dbContext.AuditLogs.CountAsync();

        var today = DateTime.UtcNow.Date;
        var todayCount = await _dbContext.AuditLogs
            .Where(a => a.CreatedAt >= today)
            .CountAsync();

        // 1. Dạng Hệ thống & Quản trị con người
        var systemCount = await _dbContext.AuditLogs
            .Where(a => a.EntityName == "User" || a.EntityName == "Role" || a.EntityName == "System" || a.ActionName.StartsWith("user_"))
            .CountAsync();

        // 2. Dạng Kho hàng & Giá trị tài sản
        var inventoryCount = await _dbContext.AuditLogs
            .Where(a => a.EntityName == "StockReceipt" || a.EntityName == "Inventory" || a.EntityName == "DailyPortionControl" || a.EntityName == "FoodIngredient" || a.ActionName.StartsWith("receipt_") || a.ActionName.StartsWith("stock_"))
            .CountAsync();

        // 3. Dạng Khách hàng, Thực đơn & Quyết định Bếp
        var customerCount = await _dbContext.AuditLogs
            .Where(a => a.EntityName == "Reservation" || a.EntityName == "Food" || a.EntityName == "DiningTable" || a.EntityName == "OrderItem" || a.EntityName == "Order" || a.ActionName.StartsWith("kitchen_") || a.ActionName.StartsWith("food_") || a.ActionName.StartsWith("table_") || a.ActionName.StartsWith("reservation_"))
            .CountAsync();

        // Top nhân sự thao tác (Admin, Bếp trưởng, Quản lý)
        var topActorsRaw = await _dbContext.AuditLogs
            .Where(a => a.UserId.HasValue)
            .GroupBy(a => a.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(5)
            .ToListAsync();

        var userIds = topActorsRaw.Select(t => t.UserId!.Value).ToList();
        var usersMap = await _dbContext.Users
            .Include(u => u.Role)
            .Where(u => userIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId, u => new { u.FullName, u.Email, RoleName = u.Role != null ? u.Role.RoleName : "Nhân viên" });

        var topActors = topActorsRaw.Select(t => new
        {
            userId = t.UserId,
            fullName = usersMap.ContainsKey(t.UserId!.Value) ? usersMap[t.UserId.Value].FullName : "Nhân sự",
            email = usersMap.ContainsKey(t.UserId!.Value) ? usersMap[t.UserId.Value].Email : "",
            roleName = usersMap.ContainsKey(t.UserId!.Value) ? usersMap[t.UserId.Value].RoleName : "",
            count = t.Count
        }).ToList();

        return Ok(new
        {
            success = true,
            totalCount,
            todayCount,
            systemCount,
            inventoryCount,
            customerCount,
            topActors
        });
    }

    /// <summary>
    /// [GET] Lấy danh sách nhân viên đã thực hiện thao tác (cho bộ lọc nhân sự)
    /// URL: /api/admin/audit-logs/actors
    /// </summary>
    [HttpGet("actors")]
    public async Task<IActionResult> GetAuditActors()
    {
        var distinctUserIds = await _dbContext.AuditLogs
            .Where(a => a.UserId.HasValue)
            .Select(a => a.UserId!.Value)
            .Distinct()
            .ToListAsync();

        var users = await _dbContext.Users
            .Include(u => u.Role)
            .Where(u => distinctUserIds.Contains(u.UserId))
            .OrderBy(u => u.FullName)
            .Select(u => new
            {
                userId = u.UserId,
                fullName = u.FullName,
                email = u.Email,
                roleName = u.Role != null ? u.Role.RoleName : "Nhân viên"
            })
            .ToListAsync();

        return Ok(new { success = true, data = users });
    }

    /// <summary>
    /// [GET] Chi tiết một bản ghi Audit Log
    /// URL: /api/admin/audit-logs/{id}
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAuditLogDetail(long id)
    {
        var log = await _dbContext.AuditLogs
            .Include(a => a.User)
                .ThenInclude(u => u!.Role)
            .FirstOrDefaultAsync(a => a.AuditLogId == id);

        if (log == null)
            return NotFound(new { success = false, message = "Không tìm thấy bản ghi kiểm toán." });

        return Ok(new { success = true, data = MapAuditLog(log) });
    }

    // ==============================================================
    // TIỆN ÍCH ÁNH XẠ LOG THÀNH 3 DẠNG TIẾNG VIỆT CHO QUẢN LÝ
    // ==============================================================
    private static AuditLogResponseItem MapAuditLog(AuditLog log)
    {
        string category = "system";
        string categoryLabel = "Hệ thống & Con người";
        string categoryIcon = "⚙️";

        string actionGroup = "update";
        string actionBadge = "Chỉnh sửa";
        string actionSeverity = "info";
        string entityLabel = log.EntityName;
        string summary = string.Empty;
        string? valueHighlight = null;
        string? valueHighlightType = null; // money | human | alert | info

        // 1. Phân loại 3 DẠNG CHÍNH
        var enLower = (log.EntityName ?? "").ToLowerInvariant();
        var actLower = (log.ActionName ?? "").ToLowerInvariant();

        if (enLower == "user" || enLower == "role" || enLower == "system" || actLower.StartsWith("user_"))
        {
            category = "system";
            categoryLabel = "Hệ Thống & Nhân Sự";
            categoryIcon = "⚙️";
        }
        else if (enLower == "stockreceipt" || enLower == "inventory" || enLower == "dailyportioncontrol" || enLower == "foodingredient" || actLower.StartsWith("receipt_") || actLower.StartsWith("stock_"))
        {
            category = "inventory";
            categoryLabel = "Kho & Giá Trị Hàng";
            categoryIcon = "📦";
        }
        else
        {
            category = "customer";
            categoryLabel = "Khách Hàng & Bếp";
            categoryIcon = "🍳";
        }

        // 2. Tên hiển thị thực thể
        switch (enLower)
        {
            case "food":
                entityLabel = "Món ăn";
                break;
            case "diningtable":
            case "table":
                entityLabel = "Bàn ăn";
                break;
            case "user":
                entityLabel = "Tài khoản nhân sự";
                break;
            case "stockreceipt":
                entityLabel = "Phiếu kho";
                break;
            case "reservation":
                entityLabel = "Đặt bàn";
                break;
            case "orderitem":
                entityLabel = "Món trong đơn";
                break;
            case "order":
                entityLabel = "Đơn hàng";
                break;
        }

        // 3. Đọc dữ liệu JSON bổ trợ nếu có
        string? nameValue = null;
        string? reasonValue = null;
        decimal? oldPrice = null;
        decimal? newPrice = null;
        decimal? totalAmount = null;
        int? guestCount = null;

        if (!string.IsNullOrWhiteSpace(log.OldData))
        {
            try
            {
                using var odoc = JsonDocument.Parse(log.OldData);
                if (odoc.RootElement.TryGetProperty("minPrice", out var op) && op.TryGetDecimal(out var opVal)) oldPrice = opVal;
                else if (odoc.RootElement.TryGetProperty("price", out var op2) && op2.TryGetDecimal(out var op2Val)) oldPrice = op2Val;
            }
            catch { }
        }

        if (!string.IsNullOrWhiteSpace(log.NewData))
        {
            try
            {
                using var doc = JsonDocument.Parse(log.NewData);
                var root = doc.RootElement;
                if (root.TryGetProperty("foodName", out var fnp)) nameValue = fnp.GetString();
                else if (root.TryGetProperty("tableName", out var tnp)) nameValue = tnp.GetString();
                else if (root.TryGetProperty("fullName", out var unp)) nameValue = unp.GetString();
                else if (root.TryGetProperty("receiptCode", out var rcp)) nameValue = rcp.GetString();
                else if (root.TryGetProperty("customerName", out var cnp)) nameValue = cnp.GetString();

                if (root.TryGetProperty("reason", out var rp)) reasonValue = rp.GetString();
                else if (root.TryGetProperty("note", out var notep)) reasonValue = notep.GetString();

                if (root.TryGetProperty("minPrice", out var np) && np.TryGetDecimal(out var npVal)) newPrice = npVal;
                else if (root.TryGetProperty("price", out var np2) && np2.TryGetDecimal(out var np2Val)) newPrice = np2Val;

                if (root.TryGetProperty("totalAmount", out var tap) && tap.TryGetDecimal(out var taVal)) totalAmount = taVal;
                if (root.TryGetProperty("guestCount", out var gcp) && gcp.TryGetInt32(out var gcVal)) guestCount = gcVal;
            }
            catch { }
        }

        var actorName = log.User?.FullName ?? "Hệ thống";
        var actorRole = log.User?.Role?.RoleName ?? (log.UserId.HasValue ? "Nhân viên" : "Hệ thống");

        // 4. Phân tích chi tiết hành động và làm nổi bật giá trị / quản trị con người / quyết định bếp
        switch (actLower)
        {
            // === NHÓM 1: HỆ THỐNG & QUẢN TRỊ CON NGƯỜI ===
            case "user_create":
            case "user_created":
                actionGroup = "create";
                actionBadge = "Cấp tài khoản";
                actionSeverity = "success";
                valueHighlight = "Cấp mới nhân sự";
                valueHighlightType = "human";
                summary = $"{actorName} (Admin) đã khởi tạo tài khoản nhân sự mới: '{nameValue ?? $"ID #{log.EntityId}"}'";
                break;
            case "user_update":
            case "user_updated":
                actionGroup = "update";
                actionBadge = "Sửa nhân sự";
                actionSeverity = "info";
                valueHighlight = "Đổi thông tin / quyền";
                valueHighlightType = "human";
                summary = $"{actorName} (Admin) đã cập nhật thông tin / quyền hạn của nhân viên: '{nameValue ?? $"ID #{log.EntityId}"}'";
                break;
            case "user_locked":
                actionGroup = "delete_lock";
                actionBadge = "Khóa nhân viên";
                actionSeverity = "danger";
                valueHighlight = "Tạm khóa quyền";
                valueHighlightType = "human";
                summary = $"{actorName} (Admin) đã TẠM KHÓA tài khoản nhân viên: '{nameValue ?? $"ID #{log.EntityId}"}'";
                break;
            case "user_unlocked":
                actionGroup = "update";
                actionBadge = "Mở khóa nhân viên";
                actionSeverity = "success";
                valueHighlight = "Mở khóa hoạt động";
                valueHighlightType = "human";
                summary = $"{actorName} (Admin) đã MỞ KHÓA cho tài khoản nhân viên: '{nameValue ?? $"ID #{log.EntityId}"}'";
                break;

            // === NHÓM 2: LIÊN QUAN ĐẾN KHO & GIÁ TRỊ VẬT TƯ ===
            case "receipt_approved":
                actionGroup = "approve";
                actionBadge = "Duyệt phiếu kho";
                actionSeverity = "purple";
                if (totalAmount.HasValue && totalAmount.Value > 0)
                {
                    valueHighlight = $"{totalAmount.Value:N0} đ";
                    valueHighlightType = "money";
                    summary = $"{actorName} đã phê duyệt phiếu kho '{nameValue ?? $"#{log.EntityId}"}' với tổng giá trị {totalAmount.Value:N0} đ, cập nhật tồn kho.";
                }
                else
                {
                    valueHighlight = "Duyệt tồn kho";
                    valueHighlightType = "info";
                    summary = $"{actorName} đã phê duyệt phiếu kho '{nameValue ?? $"#{log.EntityId}"}' và cập nhật tồn kho.";
                }
                break;
            case "receipt_waste_approved":
                actionGroup = "approve";
                actionBadge = "Duyệt xuất hủy kho";
                actionSeverity = "warning";
                if (totalAmount.HasValue && totalAmount.Value > 0)
                {
                    valueHighlight = $"Hao hụt: {totalAmount.Value:N0} đ";
                    valueHighlightType = "money";
                }
                else
                {
                    valueHighlight = "Hủy nguyên liệu";
                    valueHighlightType = "alert";
                }
                summary = $"{actorName} đã duyệt phiếu xuất hủy hao hụt nguyên liệu '{nameValue ?? $"#{log.EntityId}"}'. {(string.IsNullOrWhiteSpace(reasonValue) ? "" : $"Ghi chú: {reasonValue}")}";
                break;
            case "receipt_cancelled":
                actionGroup = "cancel";
                actionBadge = "Hủy phiếu kho";
                actionSeverity = "danger";
                valueHighlight = "Hủy chứng từ";
                valueHighlightType = "alert";
                summary = $"{actorName} đã hủy phiếu kho #{log.EntityId}. {(string.IsNullOrWhiteSpace(reasonValue) ? "" : $"Lý do: {reasonValue}")}";
                break;

            // === NHÓM 3: KHÁCH HÀNG, THỰC ĐƠN & VẬN HÀNH BẾP ===
            case "food_create":
            case "food_created":
                actionGroup = "create";
                actionBadge = "Thêm món mới";
                actionSeverity = "success";
                if (newPrice.HasValue && newPrice.Value > 0)
                {
                    valueHighlight = $"Giá: {newPrice.Value:N0} đ";
                    valueHighlightType = "money";
                    summary = $"{actorName} đã tạo món ăn mới: '{nameValue ?? $"ID #{log.EntityId}"}' với giá {newPrice.Value:N0} đ";
                }
                else
                {
                    valueHighlight = "Món mới";
                    valueHighlightType = "info";
                    summary = $"{actorName} đã tạo món ăn mới: '{nameValue ?? $"ID #{log.EntityId}"}'";
                }
                break;
            case "food_update":
            case "food_updated":
                actionGroup = "update";
                actionBadge = "Đổi giá / sửa món";
                actionSeverity = "info";
                if (oldPrice.HasValue && newPrice.HasValue && oldPrice.Value != newPrice.Value)
                {
                    valueHighlight = $"{oldPrice.Value:N0} đ → {newPrice.Value:N0} đ";
                    valueHighlightType = "money";
                    summary = $"{actorName} đã ĐIỀU CHỈNH GIÁ món '{nameValue ?? $"ID #{log.EntityId}"}' từ {oldPrice.Value:N0} đ thành {newPrice.Value:N0} đ";
                }
                else if (newPrice.HasValue)
                {
                    valueHighlight = $"Giá: {newPrice.Value:N0} đ";
                    valueHighlightType = "money";
                    summary = $"{actorName} đã cập nhật thông tin món ăn: '{nameValue ?? $"ID #{log.EntityId}"}'";
                }
                else
                {
                    valueHighlight = "Cập nhật món";
                    valueHighlightType = "info";
                    summary = $"{actorName} đã cập nhật thông tin món ăn: '{nameValue ?? $"ID #{log.EntityId}"}'";
                }
                break;
            case "food_delete":
            case "food_deleted":
                actionGroup = "delete_lock";
                actionBadge = "Xóa món ăn";
                actionSeverity = "danger";
                valueHighlight = "Ngừng bán / Xóa";
                valueHighlightType = "alert";
                summary = $"{actorName} đã ngừng bán / xóa món ăn: '{nameValue ?? $"ID #{log.EntityId}"}'";
                break;

            case "table_create":
            case "table_created":
                actionGroup = "create";
                actionBadge = "Thêm bàn mới";
                actionSeverity = "success";
                valueHighlight = "Thêm bàn";
                valueHighlightType = "info";
                summary = $"{actorName} đã tạo bàn ăn mới: '{nameValue ?? $"ID #{log.EntityId}"}'";
                break;
            case "table_update":
            case "table_updated":
                actionGroup = "update";
                actionBadge = "Sửa bàn ăn";
                actionSeverity = "info";
                valueHighlight = "Sửa thông tin bàn";
                valueHighlightType = "info";
                summary = $"{actorName} đã cập nhật thông tin bàn: '{nameValue ?? $"ID #{log.EntityId}"}'";
                break;
            case "table_status_changed":
                actionGroup = "update";
                actionBadge = "Đổi trạng thái bàn";
                actionSeverity = "warning";
                valueHighlight = "Trạng thái bàn";
                valueHighlightType = "alert";
                summary = $"{actorName} đã đổi trạng thái hoạt động của bàn #{log.EntityId}";
                break;

            case "reservation_confirmed":
                actionGroup = "approve";
                actionBadge = "Duyệt đặt bàn";
                actionSeverity = "success";
                valueHighlight = guestCount.HasValue ? $"Bàn {guestCount} khách" : "Duyệt đặt bàn";
                valueHighlightType = "info";
                summary = $"{actorName} đã duyệt & xác nhận lịch đặt bàn của khách: '{nameValue ?? $"#{log.EntityId}"}'{(guestCount.HasValue ? $" ({guestCount} khách)" : "")}";
                break;
            case "reservation_table_assigned":
                actionGroup = "approve";
                actionBadge = "Xếp / Gộp bàn";
                actionSeverity = "info";
                valueHighlight = "Phân vị trí bàn";
                valueHighlightType = "info";
                summary = $"{actorName} đã phân xếp bàn cho khách: '{nameValue ?? $"#{log.EntityId}"}'";
                break;
            case "reservation_checked_in":
                actionGroup = "approve";
                actionBadge = "Check-in khách";
                actionSeverity = "success";
                valueHighlight = "Khách vào bàn";
                valueHighlightType = "info";
                summary = $"{actorName} đã đón khách check-in vào bàn thành công: '{nameValue ?? $"#{log.EntityId}"}'";
                break;
            case "reservation_cancelled":
                actionGroup = "cancel";
                actionBadge = "Hủy đặt bàn";
                actionSeverity = "danger";
                valueHighlight = "Hủy lịch hẹn";
                valueHighlightType = "alert";
                summary = $"{actorName} đã hủy phiếu đặt bàn của khách '{nameValue ?? $"#{log.EntityId}"}'. {(string.IsNullOrWhiteSpace(reasonValue) ? "" : $"Lý do: {reasonValue}")}";
                break;

            // Quyết định của Bếp trưởng
            case "food_sold_out":
                actionGroup = "kitchen";
                actionBadge = "Bếp báo hết";
                actionSeverity = "warning";
                valueHighlight = "Hết món";
                valueHighlightType = "alert";
                summary = $"Bếp ({actorName}) báo hết món '{nameValue ?? $"ID #{log.EntityId}"}'. {(string.IsNullOrWhiteSpace(reasonValue) ? "" : $"Lý do: {reasonValue}")}";
                break;
            case "food_reopened":
                actionGroup = "kitchen";
                actionBadge = "Bếp mở bán lại";
                actionSeverity = "success";
                valueHighlight = "Mở lại món";
                valueHighlightType = "info";
                summary = $"Bếp ({actorName}) đã mở bán lại món '{nameValue ?? $"ID #{log.EntityId}"}'";
                break;
            case "kitchen_item_cancelled":
                actionGroup = "kitchen";
                actionBadge = "Bếp từ chối món";
                actionSeverity = "danger";
                valueHighlight = "Từ chối món";
                valueHighlightType = "alert";
                summary = $"Bếp ({actorName}) từ chối phục vụ món '{nameValue ?? $"ID #{log.EntityId}"}'. {(string.IsNullOrWhiteSpace(reasonValue) ? "" : $"Lý do: {reasonValue}")}";
                break;
            case "kitchen_item_undo":
                actionGroup = "kitchen";
                actionBadge = "Bếp nấu lại";
                actionSeverity = "warning";
                valueHighlight = "Yêu cầu nấu lại";
                valueHighlightType = "alert";
                summary = $"Bếp ({actorName}) yêu cầu nấu lại món '{nameValue ?? $"ID #{log.EntityId}"}'";
                break;
            case "kitchen_ready_all":
                actionGroup = "kitchen";
                actionBadge = "Bếp xong cả bàn";
                actionSeverity = "success";
                valueHighlight = "Xong cả bàn";
                valueHighlightType = "info";
                summary = $"Bếp ({actorName}) đã hoàn thành chế biến tất cả món cho Order #{log.EntityId}";
                break;

            default:
                actionGroup = "update";
                actionBadge = log.ActionName;
                actionSeverity = "info";
                summary = $"{actorName} đã thực hiện thao tác '{log.ActionName}' trên đối tượng {entityLabel} #{log.EntityId}";
                break;
        }

        return new AuditLogResponseItem
        {
            AuditLogId = log.AuditLogId,
            CreatedAt = log.CreatedAt,
            UserId = log.UserId,
            UserName = actorName,
            UserEmail = log.User?.Email ?? string.Empty,
            UserRole = actorRole,
            Category = category,
            CategoryLabel = categoryLabel,
            CategoryIcon = categoryIcon,
            ActionName = log.ActionName,
            ActionGroup = actionGroup,
            ActionBadge = actionBadge,
            ActionSeverity = actionSeverity,
            EntityName = log.EntityName,
            EntityLabel = entityLabel,
            EntityId = log.EntityId,
            Summary = summary,
            ValueHighlight = valueHighlight,
            ValueHighlightType = valueHighlightType,
            OldData = log.OldData,
            NewData = log.NewData
        };
    }
}
