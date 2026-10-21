using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;
using Restaurant.Api.Hubs;
using System.Data;
using System.Security.Claims;

namespace Restaurant.Api.Controllers.Chef;

// ==============================================================
// DTOs & Models cho Chef KDS
// ==============================================================
public class UpdateItemStatusRequest
{
    public string Status { get; set; } = string.Empty; // "cooking" | "ready" | "served"
    public string? Note { get; set; }
}

public class ToggleSoldOutRequest
{
    public bool? IsActive { get; set; }
}

[Route("api/chef")]
[Authorize(Roles = "chef,manager,admin,staff")]
[ApiController]
public class ChefController : ControllerBase
{
    private readonly RestaurantDbContext _dbContext;
    private readonly IHubContext<RestaurantHub> _hubContext;

    public ChefController(RestaurantDbContext dbContext, IHubContext<RestaurantHub> hubContext)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim != null && int.TryParse(claim.Value, out int userId))
        {
            return userId;
        }

        // Fallback: Tìm nhân viên chef hoặc manager đầu tiên trong hệ thống
        var defaultUser = _dbContext.Users
            .Include(u => u.Role)
            .FirstOrDefault(u => u.Role.RoleCode == "chef" || u.Role.RoleCode == "manager");

        return defaultUser?.UserId ?? 1;
    }

    // ==============================================================
    // 0. LẤY DANH SÁCH 3 QUẦY BẾP (Kitchen Stations / Categories)
    // ==============================================================
    [HttpGet("categories")]
    public async Task<IActionResult> GetKitchenCategories()
    {
        var validStatuses = new[] { "confirmed", "cooking", "ready" };
        var activeItems = await _dbContext.OrderItems
            .Include(i => i.Food)
            .Include(i => i.Order)
            .Where(i => validStatuses.Contains(i.ItemStatus) &&
                        (i.Order.OrderStatus == "open" || i.Order.OrderStatus == "preparing" || i.Order.OrderStatus == "serving"))
            .ToListAsync();

        var categories = await _dbContext.FoodCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new
            {
                c.CategoryId,
                c.CategoryName,
                c.SortOrder
            })
            .ToListAsync();

        var result = categories.Select(c => new
        {
            c.CategoryId,
            c.CategoryName,
            c.SortOrder,
            ItemCount = activeItems.Count(i => i.Food != null && i.Food.CategoryId == c.CategoryId),
            TotalItems = activeItems.Count(i => i.Food != null && i.Food.CategoryId == c.CategoryId),
            PendingCount = activeItems.Count(i => i.Food != null && i.Food.CategoryId == c.CategoryId && i.ItemStatus == "confirmed"),
            CookingCount = activeItems.Count(i => i.Food != null && i.Food.CategoryId == c.CategoryId && i.ItemStatus == "cooking"),
            ReadyCount = activeItems.Count(i => i.Food != null && i.Food.CategoryId == c.CategoryId && i.ItemStatus == "ready")
        }).ToList();

        return Ok(result);
    }

    // ==============================================================
    // 1. LẤY DANH SÁCH MÓN ĂN THEO THẺ ORDER BÀN (KDS Orders)
    // ==============================================================
    [HttpGet("orders")]
    public async Task<IActionResult> GetKitchenOrders([FromQuery] int? categoryId = null)
    {
        var validItemStatuses = new[] { "confirmed", "cooking", "ready" };

        var query = _dbContext.Orders
            .Where(o => o.OrderStatus == "open" || o.OrderStatus == "preparing" || o.OrderStatus == "serving")
            .Where(o => o.OrderItems.Any(i => validItemStatuses.Contains(i.ItemStatus)));

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(o => o.OrderItems.Any(i => validItemStatuses.Contains(i.ItemStatus) && i.Food.CategoryId == categoryId.Value));
        }

        var activeOrders = await query
            .Include(o => o.OrderTables)
                .ThenInclude(ot => ot.Table)
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.Food)
                    .ThenInclude(f => f.Category)
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.Chef)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

        var now = DateTime.UtcNow;

        var result = activeOrders.Select(order =>
        {
            var itemQuery = order.OrderItems.Where(i => validItemStatuses.Contains(i.ItemStatus));
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                itemQuery = itemQuery.Where(i => i.Food.CategoryId == categoryId.Value);
            }

            var kitchenItems = itemQuery
                .OrderBy(i => i.ConfirmedAt ?? i.CreatedAt)
                .Select(i => new
                {
                    i.OrderItemId,
                    i.OrderId,
                    i.FoodId,
                    i.VariantId,
                    i.FoodNameSnapshot,
                    i.VariantSnapshot,
                    i.Quantity,
                    i.Note,
                    i.ItemStatus,
                    i.CreatedAt,
                    i.ConfirmedAt,
                    i.ReadyAt,
                    CategoryId = i.Food != null ? i.Food.CategoryId : 0,
                    CategoryName = i.Food?.Category?.CategoryName ?? "Bếp",
                    ChefId = i.ChefId,
                    ChefName = i.Chef?.FullName,
                    WaitMinutes = i.ConfirmedAt.HasValue 
                        ? (int)Math.Max(0, (now - i.ConfirmedAt.Value).TotalMinutes)
                        : (int)Math.Max(0, (now - i.CreatedAt).TotalMinutes)
                })
                .ToList();

            var otherStationItems = order.OrderItems
                .Where(i => validItemStatuses.Contains(i.ItemStatus) && (categoryId.HasValue && categoryId.Value > 0 && i.Food.CategoryId != categoryId.Value))
                .Select(i => new
                {
                    i.OrderItemId,
                    i.FoodId,
                    i.FoodNameSnapshot,
                    i.VariantSnapshot,
                    i.Quantity,
                    i.ItemStatus,
                    CategoryId = i.Food != null ? i.Food.CategoryId : 0,
                    CategoryName = i.Food != null && i.Food.Category != null ? i.Food.Category.CategoryName : "Quầy khác"
                })
                .ToList();

            var primaryTable = order.OrderTables.FirstOrDefault(ot => ot.IsPrimary)?.Table 
                               ?? order.OrderTables.FirstOrDefault()?.Table;

            var secondaryTables = order.OrderTables
                .Where(ot => !ot.IsPrimary && ot.TableId != primaryTable?.TableId)
                .Select(ot => ot.Table.TableName)
                .ToList();

            string tableDisplay = primaryTable?.TableName ?? "Bàn không xác định";
            if (secondaryTables.Count > 0)
            {
                tableDisplay += " (+ " + string.Join(", ", secondaryTables) + ")";
            }

            var oldestConfirmedAt = kitchenItems.Any() 
                ? kitchenItems.Min(i => i.ConfirmedAt ?? i.CreatedAt) 
                : order.CreatedAt;
            int elapsedMinutes = (int)Math.Max(0, (now - oldestConfirmedAt).TotalMinutes);

            return new
            {
                order.OrderId,
                order.OrderStatus,
                TableDisplay = tableDisplay,
                AreaName = primaryTable?.AreaName ?? "Sảnh chung",
                PrimaryTableId = primaryTable?.TableId ?? 0,
                LinkedTables = order.OrderTables.Select(ot => new { ot.TableId, ot.Table.TableName, ot.IsPrimary }).ToList(),
                order.CreatedAt,
                OldestConfirmedAt = oldestConfirmedAt,
                ElapsedMinutes = elapsedMinutes,
                OrderNote = order.Note,
                TotalKitchenItems = kitchenItems.Count,
                PendingCookingCount = kitchenItems.Count(i => i.ItemStatus == "confirmed"),
                CookingCount = kitchenItems.Count(i => i.ItemStatus == "cooking"),
                ReadyCount = kitchenItems.Count(i => i.ItemStatus == "ready"),
                Items = kitchenItems,
                OtherStationItems = otherStationItems
            };
        }).Where(o => o.TotalKitchenItems > 0).ToList();

        return Ok(result);
    }

    // ==============================================================
    // 2. LẤY HÀNG ĐỢI MÓN ĂN PHẲNG (FIFO Item Queue)
    // ==============================================================
    [HttpGet("queue")]
    public async Task<IActionResult> GetKitchenQueue([FromQuery] int? categoryId = null)
    {
        var validStatuses = new[] { "confirmed", "cooking", "ready" };
        var now = DateTime.UtcNow;

        var query = _dbContext.OrderItems
            .Include(i => i.Food)
                .ThenInclude(f => f.Category)
            .Include(i => i.Order)
                .ThenInclude(o => o.OrderTables)
                    .ThenInclude(ot => ot.Table)
            .Include(i => i.Chef)
            .Where(i => validStatuses.Contains(i.ItemStatus) && 
                        (i.Order.OrderStatus == "open" || i.Order.OrderStatus == "preparing" || i.Order.OrderStatus == "serving"));

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(i => i.Food.CategoryId == categoryId.Value);
        }

        var rawItems = await query
            .OrderBy(i => i.ConfirmedAt ?? i.CreatedAt)
            .ToListAsync();

        var items = rawItems.Select(i =>
        {
            var primaryTable = i.Order.OrderTables.FirstOrDefault(ot => ot.IsPrimary)?.Table 
                               ?? i.Order.OrderTables.FirstOrDefault()?.Table;
            var secondaryTables = i.Order.OrderTables
                .Where(ot => !ot.IsPrimary && ot.TableId != primaryTable?.TableId)
                .Select(ot => ot.Table.TableName)
                .ToList();
            string tableDisplay = primaryTable?.TableName ?? "Bàn không xác định";
            if (secondaryTables.Count > 0)
            {
                tableDisplay += " (+ " + string.Join(", ", secondaryTables) + ")";
            }

            return new
            {
                i.OrderItemId,
                i.OrderId,
                i.FoodId,
                i.VariantId,
                i.FoodNameSnapshot,
                i.VariantSnapshot,
                i.Quantity,
                i.Note,
                i.ItemStatus,
                CategoryId = i.Food.CategoryId,
                CategoryName = i.Food.Category?.CategoryName ?? "Bếp",
                i.CreatedAt,
                i.ConfirmedAt,
                i.ReadyAt,
                ChefId = i.ChefId,
                ChefName = i.Chef != null ? i.Chef.FullName : null,
                TableDisplay = tableDisplay,
                Tables = i.Order.OrderTables.Select(ot => new { ot.TableId, ot.Table.TableName, ot.IsPrimary }).ToList(),
                WaitMinutes = i.ConfirmedAt.HasValue 
                    ? (int)Math.Max(0, (now - i.ConfirmedAt.Value).TotalMinutes)
                    : (int)Math.Max(0, (now - i.CreatedAt).TotalMinutes)
            };
        }).ToList();

        return Ok(items);
    }

    // ==============================================================
    // 3. CẬP NHẬT TIẾN ĐỘ CHẾ BIẾN CỦA MỘT MÓN ĂN (1-touch update)
    // ==============================================================
    [HttpPatch("items/{orderItemId}/status")]
    public async Task<IActionResult> UpdateItemStatus(int orderItemId, [FromBody] UpdateItemStatusRequest request)
    {
        var validStatuses = new[] { "confirmed", "cooking", "ready", "served" };
        var targetStatus = request.Status.ToLower().Trim();

        if (!validStatuses.Contains(targetStatus))
        {
            return BadRequest(new { message = $"Trạng thái '{request.Status}' không hợp lệ. Chỉ chấp nhận: confirmed, cooking, ready, served." });
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var item = await _dbContext.OrderItems
            .Include(i => i.Order)
            .FirstOrDefaultAsync(i => i.OrderItemId == orderItemId);

        if (item == null)
        {
            return NotFound(new { message = "Không tìm thấy món ăn này trong hệ thống." });
        }

        var isAllowedTransition = targetStatus == item.ItemStatus ||
            (item.ItemStatus == "confirmed" && targetStatus == "cooking") ||
            (item.ItemStatus == "cooking" && targetStatus == "ready") ||
            (item.ItemStatus == "ready" && targetStatus == "served");

        if (!isAllowedTransition)
        {
            return BadRequest(new
            {
                message = $"Không thể chuyển món từ '{item.ItemStatus}' sang '{targetStatus}'. Hãy đi theo thứ tự confirmed → cooking → ready → served."
            });
        }

        var now = DateTime.UtcNow;
        var currentChefId = GetCurrentUserId();
        var previousStatus = item.ItemStatus;

        item.ItemStatus = targetStatus;
        item.UpdatedAt = now;

        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            item.Note = request.Note;
        }

        if (targetStatus == "cooking")
        {
            item.ChefId = currentChefId;
            if (item.Order.OrderStatus == "open")
            {
                item.Order.OrderStatus = "preparing";
                item.Order.UpdatedAt = now;
            }
        }
        else if (targetStatus == "ready")
        {
            item.ChefId = item.ChefId ?? currentChefId;
            item.ReadyAt = now;
        }
        else if (targetStatus == "served")
        {
            // Kiểm tra xem tất cả các món trong order đã served chưa
            var remainingUnserved = await _dbContext.OrderItems
                .AnyAsync(i => i.OrderId == item.OrderId && 
                               i.OrderItemId != item.OrderItemId && 
                               i.ItemStatus != "cancelled" && 
                               i.ItemStatus != "served");

            if (!remainingUnserved)
            {
                item.Order.OrderStatus = "serving";
                item.Order.UpdatedAt = now;
            }

            // Đồng bộ SoldPortions vào DailyPortionControls (chỉ khi chuyển từ trạng thái khác sang served)
            if (previousStatus != "served")
            {
                var controlDate = now.AddHours(7).Date;
                var portion = await _dbContext.DailyPortionControls
                    .FirstOrDefaultAsync(p => p.ControlDate == controlDate &&
                                              p.FoodId == item.FoodId &&
                                              p.VariantId == item.VariantId);

                if (portion != null)
                {
                    portion.SoldPortions += item.Quantity;
                    portion.UpdatedAt = now;
                }
                else
                {
                    portion = new DailyPortionControl
                    {
                        ControlDate = controlDate,
                        FoodId = item.FoodId,
                        VariantId = item.VariantId,
                        PlannedPortions = 0,
                        CarryOverPortions = 0,
                        CookedPortions = 0,
                        SoldPortions = item.Quantity,
                        DisposedPortions = 0,
                        NextCarryOverPortions = 0,
                        Status = "in_service",
                        Note = "Tự động ghi nhận từ đơn gọi món",
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    _dbContext.DailyPortionControls.Add(portion);
                }
            }
        }
        else if (previousStatus == "served" && targetStatus != "served")
        {
            // Hoàn lại SoldPortions nếu chuyển ngược từ served sang trạng thái khác
            // UpdatedAt vẫn là thời điểm chuyển sang served trước khi đổi trạng thái,
            // giúp hoàn lại đúng ngày bán ngay cả khi undo qua ngày hôm sau.
            var controlDate = item.UpdatedAt.AddHours(7).Date;
            var portion = await _dbContext.DailyPortionControls
                .FirstOrDefaultAsync(p => p.ControlDate == controlDate &&
                                          p.FoodId == item.FoodId &&
                                          p.VariantId == item.VariantId);

            if (portion != null)
            {
                portion.SoldPortions = Math.Max(0, portion.SoldPortions - item.Quantity);
                portion.UpdatedAt = now;
            }
        }

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        // PHÁT TÍN HIỆU REALTIME QUA SIGNALR NGAY LẬP TỨC
        try
        {
            await _hubContext.Clients.All.SendAsync("KitchenUpdated", new { orderItemId, orderId = item.OrderId, status = targetStatus });
            await _hubContext.Clients.All.SendAsync("StaffOrderUpdated", new { orderItemId, orderId = item.OrderId, status = targetStatus });
        }
        catch { }

        return Ok(new
        {
            message = $"Đã cập nhật trạng thái món '{item.FoodNameSnapshot}' sang '{targetStatus}'.",
            item = new
            {
                item.OrderItemId,
                item.OrderId,
                item.ItemStatus,
                item.ChefId,
                item.ReadyAt,
                item.UpdatedAt
            }
        });
    }

    // ==============================================================
    // 4. NẤU XONG TOÀN BỘ MÓN CỦA MỘT BÀN (Ready all items)
    // ==============================================================
    [HttpPatch("orders/{orderId}/ready-all")]
    public async Task<IActionResult> ReadyAllItems(int orderId, [FromQuery] int? categoryId = null)
    {
        var order = await _dbContext.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.Food)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
            return NotFound(new { message = "Không tìm thấy đơn hàng này." });

        var now = DateTime.UtcNow;
        var chefId = GetCurrentUserId();

        var query = order.OrderItems
            .Where(i => i.ItemStatus == "confirmed" || i.ItemStatus == "cooking");

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(i => i.Food.CategoryId == categoryId.Value);
        }

        var pendingItems = query.ToList();

        if (pendingItems.Count == 0)
        {
            return BadRequest(new { message = "Không có món nào đang chờ chế biến trong đơn hàng này (theo quầy đã chọn)." });
        }

        foreach (var item in pendingItems)
        {
            item.ItemStatus = "ready";
            item.ReadyAt = now;
            item.ChefId = item.ChefId ?? chefId;
            item.UpdatedAt = now;
        }

        order.UpdatedAt = now;

        await _dbContext.SaveChangesAsync();

        // PHÁT TÍN HIỆU REALTIME QUA SIGNALR
        try
        {
            await _hubContext.Clients.All.SendAsync("KitchenUpdated", new { orderId, eventType = "ready_all" });
            await _hubContext.Clients.All.SendAsync("StaffOrderUpdated", new { orderId, eventType = "ready_all" });
        }
        catch { }

        return Ok(new
        {
            message = $"Đã đánh dấu hoàn tất toàn bộ {pendingItems.Count} món của đơn #{order.OrderId} thành công!"
        });
    }

    // ==============================================================
    // 4b. HOÀN TÁC TRẠNG THÁI MÓN (Undo Item Status)
    // ==============================================================
    [HttpPatch("items/{orderItemId}/undo")]
    public async Task<IActionResult> UndoItemStatus(int orderItemId)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var item = await _dbContext.OrderItems
            .Include(i => i.Order)
            .FirstOrDefaultAsync(i => i.OrderItemId == orderItemId);

        if (item == null)
            return NotFound(new { message = "Không tìm thấy món ăn này trong hệ thống." });

        var now = DateTime.UtcNow;
        string previousStatus = item.ItemStatus;
        string targetStatus = "confirmed";
        var servedControlDate = item.UpdatedAt.AddHours(7).Date;

        if (item.ItemStatus == "served")
        {
            targetStatus = "ready";
        }
        else if (item.ItemStatus == "ready")
        {
            targetStatus = "confirmed";
            item.ReadyAt = null;
        }
        else if (item.ItemStatus == "cooking")
        {
            targetStatus = "confirmed";
        }

        item.ItemStatus = targetStatus;
        item.UpdatedAt = now;

        if (previousStatus == "served")
        {
            var portion = await _dbContext.DailyPortionControls
                .FirstOrDefaultAsync(p => p.ControlDate == servedControlDate &&
                                          p.FoodId == item.FoodId &&
                                          p.VariantId == item.VariantId);

            if (portion != null)
            {
                portion.SoldPortions = Math.Max(0, portion.SoldPortions - item.Quantity);
                portion.UpdatedAt = now;
            }
        }

        if (item.Order.OrderStatus == "serving" && targetStatus != "served")
        {
            item.Order.OrderStatus = "preparing";
            item.Order.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        // PHÁT TÍN HIỆU REALTIME QUA SIGNALR
        try
        {
            await _hubContext.Clients.All.SendAsync("KitchenUpdated", new { orderItemId, orderId = item.OrderId, status = targetStatus, eventType = "undo" });
            await _hubContext.Clients.All.SendAsync("StaffOrderUpdated", new { orderItemId, orderId = item.OrderId, status = targetStatus, eventType = "undo" });
        }
        catch { }

        return Ok(new
        {
            message = $"Đã hoàn tác món '{item.FoodNameSnapshot}' từ '{previousStatus}' về '{targetStatus}'.",
            item = new
            {
                item.OrderItemId,
                item.OrderId,
                item.ItemStatus,
                item.UpdatedAt
            }
        });
    }

    // ==============================================================
    // 4c. LẤY DANH SÁCH MÓN VỪA HOÀN THÀNH GẦN ĐÂY (Recent completed items)
    // ==============================================================
    [HttpGet("recent-completed")]
    public async Task<IActionResult> GetRecentCompleted([FromQuery] int limit = 25)
    {
        var rawItems = await _dbContext.OrderItems
            .Include(i => i.Order)
                .ThenInclude(o => o.OrderTables)
                    .ThenInclude(ot => ot.Table)
            .Where(i => i.ItemStatus == "ready" || i.ItemStatus == "served")
            .OrderByDescending(i => i.UpdatedAt)
            .Take(limit)
            .ToListAsync();

        var result = rawItems.Select(i =>
        {
            var primaryTable = i.Order.OrderTables.FirstOrDefault(ot => ot.IsPrimary)?.Table 
                               ?? i.Order.OrderTables.FirstOrDefault()?.Table;
            var secondaryTables = i.Order.OrderTables
                .Where(ot => !ot.IsPrimary && ot.TableId != primaryTable?.TableId)
                .Select(ot => ot.Table.TableName)
                .ToList();
            string tableDisplay = primaryTable?.TableName ?? "Bàn không xác định";
            if (secondaryTables.Count > 0)
            {
                tableDisplay += " (+ " + string.Join(", ", secondaryTables) + ")";
            }

            return new
            {
                i.OrderItemId,
                i.OrderId,
                i.FoodNameSnapshot,
                i.VariantSnapshot,
                i.Quantity,
                i.ItemStatus,
                i.UpdatedAt,
                i.ReadyAt,
                TableDisplay = tableDisplay
            };
        }).ToList();

        return Ok(result);
    }

    // ==============================================================
    // 5. LẤY DANH SÁCH MÓN ĂN ĐỂ BẾP QUẢN LÝ BÁO HẾT MÓN (Sold Out)
    // ==============================================================
    [HttpGet("foods")]
    public async Task<IActionResult> GetKitchenFoods([FromQuery] string? search, [FromQuery] int? categoryId)
    {
        var query = _dbContext.Foods
            .Include(f => f.Category)
            .Include(f => f.FoodVariants)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(f => f.FoodName.Contains(search));
        }

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(f => f.CategoryId == categoryId.Value);
        }

        var foods = await query
            .OrderBy(f => f.Category.SortOrder)
            .ThenBy(f => f.FoodName)
            .Select(f => new
            {
                f.FoodId,
                f.CategoryId,
                CategoryName = f.Category.CategoryName,
                f.FoodName,
                f.ImageUrl,
                f.IsActive,
                VariantCount = f.FoodVariants.Count,
                MinPrice = f.FoodVariants.Any() ? f.FoodVariants.Min(v => v.Price) : 0
            })
            .ToListAsync();

        return Ok(foods);
    }

    // ==============================================================
    // 6. BẬT / TẮT TRẠNG THÁI HẾT MÓN (Toggle Sold Out)
    // ==============================================================
    [HttpPatch("foods/{foodId}/toggle-sold-out")]
    public async Task<IActionResult> ToggleFoodSoldOut(int foodId, [FromBody] ToggleSoldOutRequest? request)
    {
        var food = await _dbContext.Foods.FindAsync(foodId);
        if (food == null)
            return NotFound(new { message = "Không tìm thấy món ăn này." });

        if (request?.IsActive.HasValue == true)
        {
            food.IsActive = request.IsActive.Value;
        }
        else
        {
            food.IsActive = !food.IsActive;
        }

        food.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        // PHÁT TÍN HIỆU REALTIME QUA SIGNALR
        try
        {
            await _hubContext.Clients.All.SendAsync("KitchenUpdated", new { foodId, isActive = food.IsActive, eventType = "toggle_sold_out" });
            await _hubContext.Clients.All.SendAsync("StaffOrderUpdated", new { foodId, isActive = food.IsActive, eventType = "toggle_sold_out" });
        }
        catch { }

        string statusText = food.IsActive ? "Đang phục vụ (Còn hàng)" : "Tạm hết món (Sold out)";
        return Ok(new
        {
            message = $"Món '{food.FoodName}' đã chuyển sang trạng thái: {statusText}.",
            foodId = food.FoodId,
            foodName = food.FoodName,
            isActive = food.IsActive
        });
    }

    // ==============================================================
    // 7. THỐNG KÊ NHANH CHO BẾP (KDS Metrics)
    // ==============================================================
    [HttpGet("metrics")]
    public async Task<IActionResult> GetKitchenMetrics()
    {
        var validStatuses = new[] { "confirmed", "cooking", "ready" };
        var activeOrderItems = await _dbContext.OrderItems
            .Where(i => validStatuses.Contains(i.ItemStatus) && 
                        (i.Order.OrderStatus == "open" || i.Order.OrderStatus == "preparing" || i.Order.OrderStatus == "serving"))
            .ToListAsync();

        int confirmedCount = activeOrderItems.Count(i => i.ItemStatus == "confirmed");
        int cookingCount = activeOrderItems.Count(i => i.ItemStatus == "cooking");
        int readyCount = activeOrderItems.Count(i => i.ItemStatus == "ready");
        int soldOutFoodsCount = await _dbContext.Foods.CountAsync(f => !f.IsActive);

        return Ok(new
        {
            confirmedCount,
            cookingCount,
            readyCount,
            soldOutFoodsCount,
            totalActiveCount = activeOrderItems.Count
        });
    }
}
