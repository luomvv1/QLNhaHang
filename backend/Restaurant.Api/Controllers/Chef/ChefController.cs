using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;
using Restaurant.Api.Hubs;
using Restaurant.Api.Services;
using System.Text.Json;
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
    public bool? IsSoldOut { get; set; }
    public int? VariantId { get; set; }
    public string? Reason { get; set; }
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
                    CanCancel = OrderCancellationWorkflow.CanCancelItem(i),
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
                CanCancel = OrderCancellationWorkflow.CanCancelItem(i),
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
    [Authorize(Roles = "chef,manager,admin")]
    public async Task<IActionResult> UpdateItemStatus(int orderItemId, [FromBody] UpdateItemStatusRequest request)
    {
        var validStatuses = new[] { "confirmed", "cooking", "ready", "served" };
        var targetStatus = request.Status.ToLower().Trim();

        if (!validStatuses.Contains(targetStatus))
        {
            return BadRequest(new { message = $"Trạng thái '{request.Status}' không hợp lệ. Chỉ chấp nhận: confirmed, cooking, ready, served." });
        }

        await using var transaction = _dbContext.Database.CurrentTransaction == null
            ? await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        if (!await PortionAvailabilityService.TryLockAsync(_dbContext))
            return Conflict(new { message = "Đang cập nhật suất ăn. Vui lòng thử lại." });

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

        if (targetStatus == "served") await ServedPortionRecorder.RecordAsync(_dbContext, new[] { item }, now);
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

        }

        await _dbContext.SaveChangesAsync();
        if (transaction != null) await transaction.CommitAsync();

        // PHÁT TÍN HIỆU REALTIME QUA SIGNALR NGAY LẬP TỨC
        try
        {
            await _hubContext.PublishAsync(_dbContext, "KitchenUpdated", new { orderItemId, orderId = item.OrderId, status = targetStatus });
            await _hubContext.PublishAsync(_dbContext, "StaffOrderUpdated", new { orderItemId, orderId = item.OrderId, status = targetStatus });
            await _hubContext.PublishAsync(_dbContext, "PortionAvailabilityChanged", new { orderItemId, eventType = "progress" });
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
    [Authorize(Roles = "chef,manager,admin")]
    public async Task<IActionResult> ReadyAllItems(int orderId, [FromQuery] int? categoryId = null)
    {
        await using var transaction = _dbContext.Database.CurrentTransaction == null
            ? await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        var order = await _dbContext.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.Food)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
            return NotFound(new { message = "Không tìm thấy đơn hàng này." });
        if (!OrderCancellationWorkflow.IsOpen(order.OrderStatus))
            return Conflict(new { message = "Đơn đã đóng; không thể thay đổi món." });

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

        _dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = chefId,
            ActionName = "kitchen_ready_all",
            EntityName = "Order",
            EntityId = orderId.ToString(),
            NewData = JsonSerializer.Serialize(new
            {
                orderId,
                itemsCount = pendingItems.Count,
                items = pendingItems.Select(p => p.FoodNameSnapshot).ToList(),
                reason = "Bếp trưởng xác nhận hoàn tất chế biến toàn bộ món của đơn"
            }),
            CreatedAt = now
        });

        await _dbContext.SaveChangesAsync();
        if (transaction != null) await transaction.CommitAsync();

        // PHÁT TÍN HIỆU REALTIME QUA SIGNALR
        try
        {
            await _hubContext.PublishAsync(_dbContext, "KitchenUpdated", new { orderId, eventType = "ready_all" });
            await _hubContext.PublishAsync(_dbContext, "StaffOrderUpdated", new { orderId, eventType = "ready_all" });
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
    [Authorize(Roles = "chef,manager,admin")]
    public async Task<IActionResult> UndoItemStatus(int orderItemId)
    {
        await using var transaction = _dbContext.Database.CurrentTransaction == null
            ? await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        if (!await PortionAvailabilityService.TryLockAsync(_dbContext))
            return Conflict(new { message = "Đang cập nhật suất ăn. Vui lòng thử lại." });

        var item = await _dbContext.OrderItems
            .Include(i => i.Order)
            .FirstOrDefaultAsync(i => i.OrderItemId == orderItemId);

        if (item == null)
            return NotFound(new { message = "Không tìm thấy món ăn này trong hệ thống." });
        if (!OrderCancellationWorkflow.IsOpen(item.Order.OrderStatus) || item.ItemStatus is not ("cooking" or "ready" or "served"))
            return Conflict(new { message = "Chỉ được hoàn tác món đã bắt đầu chế biến trong đơn còn mở. Món đã hủy không thể khôi phục." });

        var now = DateTime.UtcNow;
        string previousStatus = item.ItemStatus;
        string targetStatus = "confirmed";
        if (previousStatus == "served") await ServedPortionRecorder.UndoAsync(_dbContext, item, now);

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

        if (item.Order.OrderStatus == "serving" && targetStatus != "served")
        {
            item.Order.OrderStatus = "preparing";
            item.Order.UpdatedAt = now;
        }

        _dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = GetCurrentUserId(),
            ActionName = "kitchen_item_undo",
            EntityName = "OrderItem",
            EntityId = orderItemId.ToString(),
            OldData = JsonSerializer.Serialize(new { status = previousStatus }),
            NewData = JsonSerializer.Serialize(new
            {
                orderId = item.OrderId,
                orderItemId,
                foodName = item.FoodNameSnapshot,
                variantName = item.VariantSnapshot,
                previousStatus,
                targetStatus,
                reason = "Bếp hoàn tác trạng thái để nấu lại hoặc điều chỉnh món"
            }),
            CreatedAt = now
        });

        await _dbContext.SaveChangesAsync();
        if (transaction != null) await transaction.CommitAsync();

        // PHÁT TÍN HIỆU REALTIME QUA SIGNALR
        try
        {
            await _hubContext.PublishAsync(_dbContext, "KitchenUpdated", new { orderItemId, orderId = item.OrderId, status = targetStatus, eventType = "undo" });
            await _hubContext.PublishAsync(_dbContext, "StaffOrderUpdated", new { orderItemId, orderId = item.OrderId, status = targetStatus, eventType = "undo" });
            await _hubContext.PublishAsync(_dbContext, "PortionAvailabilityChanged", new { orderItemId, eventType = "undo_served" });
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
            .Where(f => f.IsActive && (f.FoodStatus == "available" || f.FoodStatus == "sold_out"))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(f => f.FoodName.Contains(search));
        }

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(f => f.CategoryId == categoryId.Value);
        }

        var records = await query
            .OrderBy(f => f.Category.SortOrder)
            .ThenBy(f => f.FoodName)
            .ToListAsync();
        var availability = await PortionAvailabilityService.LoadAsync(_dbContext, PortionAvailabilityService.Today);
        var foods = records.Select(f =>
        {
            var variants = f.FoodVariants.Where(v => v.IsActive).OrderBy(v => v.SortOrder).Select(v =>
            {
                var state = availability.For(f, v);
                return new { v.VariantId, v.VariantName, v.Price, IsSoldOut = v.IsSoldOut || f.FoodStatus == "sold_out",
                    state.CanOrder, AvailabilityStatus = state.Status, AvailabilityText = state.Label, state.Plan };
            }).ToList();
            return new
            {
                f.FoodId,
                f.CategoryId,
                CategoryName = f.Category.CategoryName,
                f.FoodName,
                f.ImageUrl,
                f.IsActive,
                f.FoodStatus,
                IsSoldOut = f.FoodStatus == "sold_out" || !f.FoodVariants.Any(v => v.IsActive && !v.IsSoldOut),
                Variants = variants,
                CanOrder = variants.Any(v => v.CanOrder),
                VariantCount = f.FoodVariants.Count,
                MinPrice = f.FoodVariants.Any() ? f.FoodVariants.Min(v => v.Price) : 0
            };
        }).ToList();

        return Ok(foods);
    }

    // ==============================================================
    // 6. BẬT / TẮT TRẠNG THÁI HẾT MÓN (Toggle Sold Out)
    // ==============================================================
    [HttpPatch("foods/{foodId}/toggle-sold-out")]
    [Authorize(Roles = "chef,manager,admin")]
    public async Task<IActionResult> ToggleFoodSoldOut(int foodId, [FromBody] ToggleSoldOutRequest? request)
    {
        await using var transaction = _dbContext.Database.CurrentTransaction == null
            ? await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        var food = await _dbContext.Foods.Include(f => f.FoodVariants).FirstOrDefaultAsync(f => f.FoodId == foodId);
        if (food == null)
            return NotFound(new { message = "Không tìm thấy món ăn này." });
        if (!food.IsActive || food.FoodStatus is not ("available" or "sold_out"))
            return Conflict(new { message = "Món đã ngừng kinh doanh. Hãy liên hệ quản trị danh mục." });
        var variant = request?.VariantId is int id ? food.FoodVariants.FirstOrDefault(v => v.VariantId == id && v.IsActive) : null;
        if (request?.VariantId != null && variant == null)
            return BadRequest(new { message = "Size không thuộc món hoặc đã ngừng kinh doanh." });
        var soldOut = request?.IsSoldOut ?? (request?.IsActive.HasValue == true ? !request.IsActive.Value :
            variant != null ? !variant.IsSoldOut : food.FoodStatus != "sold_out");
        var reason = request?.Reason?.Trim();
        if (soldOut && (string.IsNullOrWhiteSpace(reason) || reason.Length < 3 || reason.Length > 500))
            return BadRequest(new { message = "Vui lòng nhập lý do hết món từ 3 đến 500 ký tự." });
        var now = DateTime.UtcNow;
        // A catalog-level sold_out flag also blocks sizes created before per-size availability existed.
        if (food.FoodStatus == "sold_out")
            foreach (var size in food.FoodVariants.Where(v => v.IsActive)) size.IsSoldOut = true;
        if (variant == null)
        {
            foreach (var size in food.FoodVariants.Where(v => v.IsActive))
            {
                size.IsSoldOut = soldOut;
                size.UpdatedAt = now;
            }
        }
        else
        {
            variant.IsSoldOut = soldOut;
            variant.UpdatedAt = now;
        }
        food.FoodStatus = food.FoodVariants.Any(v => v.IsActive && !v.IsSoldOut) ? "available" : "sold_out";
        food.UpdatedAt = now;
        _dbContext.AuditLogs.Add(new AuditLog { UserId = GetCurrentUserId(), ActionName = soldOut ? "food_sold_out" : "food_reopened",
            EntityName = variant == null ? "Food" : "FoodVariant", EntityId = (variant?.VariantId ?? foodId).ToString(),
            NewData = JsonSerializer.Serialize(new { foodId, foodName = food.FoodName, variantId = variant?.VariantId, variantName = variant?.VariantName, soldOut, reason }), CreatedAt = now });
        await _dbContext.SaveChangesAsync();
        if (transaction != null) await transaction.CommitAsync();

        // PHÁT TÍN HIỆU REALTIME QUA SIGNALR
        try
        {
            var availability = await PortionAvailabilityService.LoadAsync(_dbContext, PortionAvailabilityService.Today);
            var quotas = food.FoodVariants.Where(v => v.IsActive && (variant == null || v.VariantId == variant.VariantId))
                .Select(v => availability.Resolve(foodId, v.VariantId)).Where(q => q?.IsManaged == true)
                .Select(q => q!).DistinctBy(q => q.Key).ToList();
            var payload = new { foodId, variantId = variant?.VariantId, foodName = food.FoodName,
                variantName = variant?.VariantName, isSoldOut = soldOut, reason, eventType = "toggle_sold_out",
                issuedPortions = quotas.Count > 0 ? (decimal?)quotas.Sum(q => q.IssuedPortions) : null,
                servedPortions = quotas.Count > 0 ? (decimal?)quotas.Sum(q => q.ServedPortions) : null,
                reservedPortions = quotas.Count > 0 ? (decimal?)quotas.Sum(q => q.ReservedPortions) : null };
            await _hubContext.PublishAsync(_dbContext, "FoodAvailabilityChanged", payload);
            await _hubContext.PublishAsync(_dbContext, "KitchenUpdated", payload);
            await _hubContext.PublishAsync(_dbContext, "StaffOrderUpdated", payload);
        }
        catch { }

        string statusText = soldOut ? "Hết món" : "Đang phục vụ";
        return Ok(new
        {
            message = $"Món '{food.FoodName}' đã chuyển sang trạng thái: {statusText}.",
            foodId = food.FoodId,
            foodName = food.FoodName,
            isSoldOut = soldOut,
            variantId = variant?.VariantId
        });
    }

    [HttpPost("orders/{orderId}/items/{itemId}/cancel")]
    [Authorize(Roles = "chef,manager,admin")]
    public async Task<IActionResult> CancelKitchenItem(int orderId, int itemId, [FromBody] CancelOrderRequest request)
    {
        var result = await OrderCancellationWorkflow.CancelAsync(_dbContext, orderId, itemId, request.Reason,
            GetCurrentUserId(), fromKitchen: true);
        if (result.StatusCode == 200)
        {
            try
            {
                var payload = new { orderId, itemId, reason = request.Reason, result.FoodName, result.VariantName,
                    source = "kitchen", eventType = "item_cancelled" };
                await _hubContext.PublishAsync(_dbContext, "OrderCancellationUpdated", payload);
                await _hubContext.PublishAsync(_dbContext, "StaffOrderUpdated", payload);
                await _hubContext.PublishAsync(_dbContext, "KitchenUpdated", payload);
                await _hubContext.PublishAsync(_dbContext, "PortionAvailabilityChanged", new { orderId, itemId, eventType = "released" });
            }
            catch { }
        }
        return StatusCode(result.StatusCode, new { message = result.Message });
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
        int soldOutFoodsCount = await _dbContext.Foods.CountAsync(f => f.IsActive &&
            (f.FoodStatus == "sold_out" || f.FoodVariants.Any(v => v.IsActive && v.IsSoldOut)));

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
