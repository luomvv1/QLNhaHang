using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;
using Restaurant.Api.Hubs;
using System.Data;
using System.Security.Claims;

namespace Restaurant.Api.Controllers.Staff;

// ==============================================================
// DTOs & Request Models cho Staff Order
// ==============================================================
public class CreateTableOrderRequest
{
    public int TableId { get; set; }
    public int? CustomerId { get; set; }
    public string? Note { get; set; }
    public List<OrderItemInputDto>? InitialItems { get; set; }
}

public class OrderItemInputDto
{
    public int FoodId { get; set; }
    public int VariantId { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Note { get; set; }
}

public class AddOrderItemRequest
{
    public int FoodId { get; set; }
    public int VariantId { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Note { get; set; }
}

public class UpdateOrderItemRequest
{
    public int? VariantId { get; set; }
    public int? Quantity { get; set; }
    public string? Note { get; set; }
}

public class SwitchTableRequest
{
    public int FromTableId { get; set; }
    public int ToTableId { get; set; }
}

public class MergeTablesRequest
{
    public int CurrentTableId { get; set; }
    public int TargetTableId { get; set; }
    public string ActionType { get; set; } = "link_empty";
    public bool KeepSourceTableLinked { get; set; }
}

public class UnlinkTableRequest
{
    public int TableId { get; set; }
}

[Route("api/staff/orders")]
[Authorize(Roles = "staff,manager,admin")]
[ApiController]
public class StaffOrderController : ControllerBase
{
    private readonly RestaurantDbContext _dbContext;
    private readonly IHubContext<RestaurantHub> _hubContext;

    public StaffOrderController(RestaurantDbContext dbContext, IHubContext<RestaurantHub> hubContext)
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

        // Fallback: Tìm nhân viên staff hoặc manager đầu tiên trong hệ thống
        var defaultUser = _dbContext.Users
            .Include(u => u.Role)
            .FirstOrDefault(u => u.Role.RoleCode == "staff" || u.Role.RoleCode == "manager");

        return defaultUser?.UserId ?? 1;
    }

    // 1. Lấy danh sách sơ đồ bàn kèm order hiện tại
    [HttpGet("tables")]
    public async Task<IActionResult> GetTableMap()
    {
        var tables = await _dbContext.DiningTables
            .Where(t => t.IsActive)
            .OrderBy(t => t.AreaName).ThenBy(t => t.TableName)
            .Select(t => new
            {
                t.TableId,
                t.TableCode,
                t.TableName,
                t.AreaName,
                t.Capacity,
                t.TableStatus,
                ActiveOrder = _dbContext.OrderTables
                    .Where(ot => ot.TableId == t.TableId && 
                                 (ot.Order.OrderStatus == "open" || ot.Order.OrderStatus == "preparing" || ot.Order.OrderStatus == "serving"))
                    .Select(ot => new
                    {
                        ot.IsPrimary,
                        ot.Order.OrderId,
                        ot.Order.OrderStatus,
                        ot.Order.Subtotal,
                        ot.Order.TotalAmount,
                        ot.Order.CreatedAt,
                        ItemCount = ot.Order.OrderItems.Count(i => i.ItemStatus != "cancelled"),
                        ReadyCount = ot.Order.OrderItems.Count(i => i.ItemStatus == "ready"),
                        ServedCount = ot.Order.OrderItems.Count(i => i.ItemStatus == "served"),
                        CookingCount = ot.Order.OrderItems.Count(i => i.ItemStatus == "confirmed" || i.ItemStatus == "cooking")
                    })
                    .FirstOrDefault(),
                Reservation = _dbContext.Reservations
                    .Where(r => r.TableId == t.TableId && 
                                (r.ReservationStatus == "confirmed" || r.ReservationStatus == "pending" || r.ReservationStatus == "checked_in"))
                    .OrderByDescending(r => r.ReservationDate)
                    .Select(r => new
                    {
                        r.ReservationId,
                        r.CustomerName,
                        r.CustomerPhone,
                        r.GuestCount,
                        r.ReservationDate,
                        r.ReservationStatus,
                        r.Note
                    })
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(tables);
    }

    // 2. Lấy chi tiết order đang mở của một bàn
    [HttpGet("table/{tableId}")]
    public async Task<IActionResult> GetOrderByTable(int tableId)
    {
        var table = await _dbContext.DiningTables.FindAsync(tableId);
        if (table == null)
            return NotFound(new { message = "Không tìm thấy bàn ăn này." });

        var orderTable = await _dbContext.OrderTables
            .Include(ot => ot.Order)
                .ThenInclude(o => o.CreatedByNavigation)
            .Include(ot => ot.Order)
                .ThenInclude(o => o.OrderItems)
            .Include(ot => ot.Order)
                .ThenInclude(o => o.OrderTables)
                    .ThenInclude(ot => ot.Table)
            .FirstOrDefaultAsync(ot => ot.TableId == tableId && 
                                      (ot.Order.OrderStatus == "open" || ot.Order.OrderStatus == "preparing" || ot.Order.OrderStatus == "serving"));

        if (orderTable == null)
        {
            return Ok(new
            {
                Table = new
                {
                    table.TableId,
                    table.TableCode,
                    table.TableName,
                    table.AreaName,
                    table.Capacity,
                    table.TableStatus
                },
                Order = (object?)null
            });
        }

        var order = orderTable.Order;

        return Ok(new
        {
            Table = new
            {
                table.TableId,
                table.TableCode,
                table.TableName,
                table.AreaName,
                table.Capacity,
                table.TableStatus
            },
            Order = new
            {
                order.OrderId,
                order.OrderStatus,
                order.Subtotal,
                order.DiscountAmount,
                order.TotalAmount,
                order.Note,
                order.CreatedAt,
                CreatedByName = order.CreatedByNavigation?.FullName ?? "Nhân viên",
                IsPrimary = order.OrderTables.FirstOrDefault(ot => ot.TableId == tableId)?.IsPrimary ?? false,
                LinkedTables = order.OrderTables
                    .OrderByDescending(ot => ot.IsPrimary)
                    .ThenBy(ot => ot.AssignedAt)
                    .Select(ot => new
                    {
                        ot.TableId,
                        ot.IsPrimary,
                        ot.Table.TableName,
                        ot.Table.AreaName
                    })
                    .ToList(),
                Items = order.OrderItems
                    .OrderBy(i => i.OrderItemId)
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
                        i.Note,
                        i.ItemStatus,
                        i.CreatedAt,
                        i.ConfirmedAt,
                        i.ReadyAt
                    })
                    .ToList()
            }
        });
    }

    // Giải phóng bàn đặt trước về trạng thái trống
    [HttpPost("tables/{tableId}/release-reserved")]
    public async Task<IActionResult> ReleaseReservedTable(int tableId)
    {
        var table = await _dbContext.DiningTables.FindAsync(tableId);
        if (table == null)
            return NotFound(new { message = "Không tìm thấy bàn này." });

        if (table.TableStatus != "reserved")
            return BadRequest(new { message = "Bàn này không ở trạng thái đặt trước." });

        table.TableStatus = "empty";
        table.UpdatedAt = DateTime.UtcNow;

        var reservations = await _dbContext.Reservations
            .Where(r => r.TableId == tableId && r.ReservationStatus == "confirmed")
            .ToListAsync();

        foreach (var r in reservations)
        {
            r.TableId = null;
            r.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();

        try
        {
            await _hubContext.Clients.All.SendAsync("StaffOrderUpdated");
        }
        catch { }

        return Ok(new { success = true, message = $"Đã giải phóng {table.TableName} về trạng thái bàn trống." });
    }

    // 3. Mở Order mới cho bàn trống
    [HttpPost("create")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateTableOrderRequest request)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var table = await _dbContext.DiningTables.FindAsync(request.TableId);
        if (table == null || !table.IsActive)
            return BadRequest(new { message = "Bàn ăn không tồn tại hoặc đã ngừng sử dụng." });

        if (table.TableStatus != "empty")
            return BadRequest(new { message = $"Bàn '{table.TableName}' đang ở trạng thái '{table.TableStatus}', không thể mở order mới." });

        // Kiểm tra xem bàn đã có order đang mở chưa
        var existingOrderTable = await _dbContext.OrderTables
            .Include(ot => ot.Order)
            .FirstOrDefaultAsync(ot => ot.TableId == request.TableId && 
                                      (ot.Order.OrderStatus == "open" || ot.Order.OrderStatus == "preparing" || ot.Order.OrderStatus == "serving"));

        if (existingOrderTable != null)
        {
            return BadRequest(new { message = $"Bàn '{table.TableName}' đang có đơn phục vụ số #{existingOrderTable.OrderId}. Vui lòng tiếp tục đơn này." });
        }

        int currentUserId = GetCurrentUserId();
        var now = DateTime.UtcNow;

        var order = new Order
        {
            CustomerId = request.CustomerId,
            CreatedBy = currentUserId,
            OrderStatus = "open",
            Subtotal = 0,
            DiscountAmount = 0,
            TotalAmount = 0,
            Note = request.Note,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        // Gán bàn vào order
        var orderTable = new OrderTable
        {
            OrderId = order.OrderId,
            TableId = table.TableId,
            IsPrimary = true,
            AssignedAt = now
        };
        _dbContext.OrderTables.Add(orderTable);

        // Chuyển trạng thái bàn sang 'serving'
        table.TableStatus = "serving";
        table.UpdatedAt = now;

        // Thêm món ăn ban đầu nếu có
        if (request.InitialItems != null && request.InitialItems.Count > 0)
        {
            foreach (var itemDto in request.InitialItems)
            {
                var food = await _dbContext.Foods.FindAsync(itemDto.FoodId);
                var variant = await _dbContext.FoodVariants.FindAsync(itemDto.VariantId);

                if (itemDto.Quantity <= 0 || food == null || !food.IsActive || food.FoodStatus != "available" ||
                    variant == null || !variant.IsActive || variant.FoodId != food.FoodId)
                    return BadRequest(new { message = "Món hoặc biến thể trong order ban đầu không hợp lệ hoặc đã hết món." });

                var orderItem = new OrderItem
                {
                    OrderId = order.OrderId,
                    FoodId = food.FoodId,
                    VariantId = variant.VariantId,
                    FoodNameSnapshot = food.FoodName,
                    VariantSnapshot = variant.VariantName,
                    UnitPrice = variant.Price,
                    Quantity = itemDto.Quantity,
                    Note = itemDto.Note,
                    ItemStatus = "pending",
                    CreatedBy = currentUserId,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _dbContext.OrderItems.Add(orderItem);
                order.Subtotal += variant.Price * itemDto.Quantity;
            }
            order.TotalAmount = order.Subtotal - order.DiscountAmount;
        }

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        try
        {
            await _hubContext.Clients.All.SendAsync("StaffOrderUpdated", new { orderId = order.OrderId, tableId = table.TableId, eventType = "order_created" });
            if (request.InitialItems != null && request.InitialItems.Count > 0)
            {
                await _hubContext.Clients.All.SendAsync("KitchenUpdated", new { orderId = order.OrderId, eventType = "order_created" });
            }
        }
        catch { }

        return Ok(new
        {
            message = $"Đã mở order #{order.OrderId} cho bàn '{table.TableName}' thành công!",
            orderId = order.OrderId,
            tableId = table.TableId
        });
    }

    // 4. Thêm món vào Order
    [HttpPost("{orderId}/items")]
    public async Task<IActionResult> AddItemToOrder(int orderId, [FromBody] AddOrderItemRequest request)
    {
        if (request.Quantity <= 0)
            return BadRequest(new { message = "Số lượng món phải lớn hơn 0." });

        var order = await _dbContext.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
            return NotFound(new { message = "Không tìm thấy Order này." });

        if (order.OrderStatus != "open" && order.OrderStatus != "preparing" && order.OrderStatus != "serving")
            return BadRequest(new { message = $"Không thể thêm món vào order có trạng thái '{order.OrderStatus}'." });

        var food = await _dbContext.Foods.FindAsync(request.FoodId);
        var variant = await _dbContext.FoodVariants.FindAsync(request.VariantId);

        if (food == null || !food.IsActive || food.FoodStatus != "available")
            return BadRequest(new { message = "Món ăn không tồn tại hoặc đã ngừng bán." });

        if (variant == null || !variant.IsActive || variant.FoodId != food.FoodId)
            return BadRequest(new { message = "Biến thể món không hợp lệ hoặc đã ngừng bán." });

        int currentUserId = GetCurrentUserId();
        var now = DateTime.UtcNow;

        // Nếu đã có món cùng variant và cùng ghi chú ở trạng thái 'pending', ta cộng dồn số lượng
        var existingPendingItem = order.OrderItems
            .FirstOrDefault(i => i.FoodId == request.FoodId && 
                                 i.VariantId == request.VariantId && 
                                 (i.Note ?? "") == (request.Note ?? "") && 
                                 i.ItemStatus == "pending");

        if (existingPendingItem != null)
        {
            existingPendingItem.Quantity += request.Quantity;
            existingPendingItem.UpdatedAt = now;
        }
        else
        {
            var newItem = new OrderItem
            {
                OrderId = order.OrderId,
                FoodId = food.FoodId,
                VariantId = variant.VariantId,
                FoodNameSnapshot = food.FoodName,
                VariantSnapshot = variant.VariantName,
                UnitPrice = variant.Price,
                Quantity = request.Quantity,
                Note = request.Note,
                ItemStatus = "pending",
                CreatedBy = currentUserId,
                CreatedAt = now,
                UpdatedAt = now
            };
            _dbContext.OrderItems.Add(newItem);
        }

        // Tính lại tổng tiền order
        await _dbContext.SaveChangesAsync();
        await RecalculateOrderTotals(order.OrderId);

        try
        {
            await _hubContext.Clients.All.SendAsync("StaffOrderUpdated", new { orderId = order.OrderId, eventType = "item_added" });
        }
        catch { }

        return Ok(new { message = $"Đã thêm món '{food.FoodName} ({variant.VariantName})' vào order." });
    }

    // 5. Cập nhật số lượng / ghi chú món (chỉ khi còn 'pending')
    [HttpPatch("{orderId}/items/{itemId}")]
    public async Task<IActionResult> UpdateOrderItem(int orderId, int itemId, [FromBody] UpdateOrderItemRequest request)
    {
        var order = await _dbContext.Orders.FindAsync(orderId);
        if (order == null)
            return NotFound(new { message = "Không tìm thấy Order này." });
        if (order.OrderStatus is not ("open" or "preparing" or "serving"))
            return BadRequest(new { message = $"Không thể sửa món trong order có trạng thái '{order.OrderStatus}'." });

        var item = await _dbContext.OrderItems
            .FirstOrDefaultAsync(i => i.OrderId == orderId && i.OrderItemId == itemId);

        if (item == null)
            return NotFound(new { message = "Không tìm thấy món ăn trong đơn hàng." });

        if (item.ItemStatus != "pending")
            return BadRequest(new { message = $"Món này đã ở trạng thái '{item.ItemStatus}', không thể tự do chỉnh sửa. Hãy liên hệ Bếp nếu cần đổi món." });

        var now = DateTime.UtcNow;

        if (request.VariantId.HasValue && request.VariantId.Value != item.VariantId)
        {
            var newVariant = await _dbContext.FoodVariants.FindAsync(request.VariantId.Value);
            if (newVariant == null || !newVariant.IsActive || newVariant.FoodId != item.FoodId)
                return BadRequest(new { message = "Biến thể món không hợp lệ hoặc đã ngừng bán." });

            item.VariantId = newVariant.VariantId;
            item.VariantSnapshot = newVariant.VariantName;
            item.UnitPrice = newVariant.Price;
            item.UpdatedAt = now;
        }

        if (request.Quantity.HasValue)
        {
            if (request.Quantity.Value <= 0)
            {
                _dbContext.OrderItems.Remove(item);
            }
            else
            {
                item.Quantity = request.Quantity.Value;
                item.UpdatedAt = now;
            }
        }

        if (request.Note != null)
        {
            item.Note = request.Note;
            item.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync();
        await RecalculateOrderTotals(orderId);

        try
        {
            await _hubContext.Clients.All.SendAsync("StaffOrderUpdated", new { orderId, eventType = "item_updated" });
        }
        catch { }

        return Ok(new { message = "Đã cập nhật món ăn thành công." });
    }

    // 6. Xóa món khỏi order (chỉ khi còn 'pending')
    [HttpDelete("{orderId}/items/{itemId}")]
    public async Task<IActionResult> DeleteOrderItem(int orderId, int itemId)
    {
        var order = await _dbContext.Orders.FindAsync(orderId);
        if (order == null)
            return NotFound(new { message = "Không tìm thấy Order này." });
        if (order.OrderStatus is not ("open" or "preparing" or "serving"))
            return BadRequest(new { message = $"Không thể xóa món trong order có trạng thái '{order.OrderStatus}'." });

        var item = await _dbContext.OrderItems
            .FirstOrDefaultAsync(i => i.OrderId == orderId && i.OrderItemId == itemId);

        if (item == null)
            return NotFound(new { message = "Không tìm thấy món ăn trong đơn hàng." });

        if (item.ItemStatus != "pending")
            return BadRequest(new { message = $"Món này đã gửi bếp ('{item.ItemStatus}'), không thể xóa." });

        _dbContext.OrderItems.Remove(item);
        await _dbContext.SaveChangesAsync();
        await RecalculateOrderTotals(orderId);

        try
        {
            await _hubContext.Clients.All.SendAsync("StaffOrderUpdated", new { orderId, eventType = "item_deleted" });
        }
        catch { }

        return Ok(new { message = "Đã xóa món khỏi đơn hàng." });
    }

    // 7. Xác nhận gửi các món pending sang Bếp (chuyển sang 'confirmed')
    [HttpPost("{orderId}/confirm")]
    public async Task<IActionResult> ConfirmItemsToKitchen(int orderId)
    {
        var order = await _dbContext.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
            return NotFound(new { message = "Không tìm thấy đơn hàng." });

        var pendingItems = order.OrderItems.Where(i => i.ItemStatus == "pending").ToList();
        if (pendingItems.Count == 0)
            return BadRequest(new { message = "Không có món nào đang chờ gửi bếp." });

        int currentUserId = GetCurrentUserId();
        var now = DateTime.UtcNow;

        foreach (var item in pendingItems)
        {
            item.ItemStatus = "confirmed";
            item.ConfirmedAt = now;
            item.ConfirmedBy = currentUserId;
            item.UpdatedAt = now;
        }

        if (order.OrderStatus == "open")
        {
            order.OrderStatus = "preparing";
        }
        order.UpdatedAt = now;

        await _dbContext.SaveChangesAsync();

        try
        {
            await _hubContext.Clients.All.SendAsync("KitchenUpdated", new { orderId, eventType = "items_confirmed" });
            await _hubContext.Clients.All.SendAsync("StaffOrderUpdated", new { orderId, eventType = "items_confirmed" });
        }
        catch { }

        return Ok(new
        {
            message = $"Đã xác nhận gửi {pendingItems.Count} món xuống Bếp thành công!",
            confirmedCount = pendingItems.Count
        });
    }

    // 8. Chuyển bàn (Đổi bàn)
    // Nhân viên xác nhận đã nhận món từ quầy và bưng ra bàn.
    [HttpPatch("{orderId}/items/{itemId}/served")]
    public async Task<IActionResult> MarkItemServed(int orderId, int itemId)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var item = await _dbContext.OrderItems
            .Include(i => i.Order)
            .FirstOrDefaultAsync(i => i.OrderId == orderId && i.OrderItemId == itemId);

        if (item == null)
            return NotFound(new { message = "Khong tim thay mon trong order." });

        if (item.ItemStatus != "ready")
            return BadRequest(new { message = "Chi duoc phuc vu mon da san sang tai quay." });

        var now = DateTime.UtcNow;
        var previousStatus = item.ItemStatus;

        item.ItemStatus = "served";
        item.UpdatedAt = now;

        var remaining = await _dbContext.OrderItems.AnyAsync(i =>
            i.OrderId == orderId && i.ItemStatus != "cancelled" && i.ItemStatus != "served");

        if (!remaining)
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

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        try
        {
            await _hubContext.Clients.All.SendAsync("KitchenUpdated", new { orderId, itemId, status = "served" });
            await _hubContext.Clients.All.SendAsync("StaffOrderUpdated", new { orderId, itemId, status = "served" });
        }
        catch { }

        return Ok(new { message = "Da xac nhan mon duoc phuc vu tai ban.", itemStatus = item.ItemStatus });
    }

    [HttpPost("switch-table")]
    public async Task<IActionResult> SwitchTable([FromBody] SwitchTableRequest request)
    {
        if (request.FromTableId == request.ToTableId)
            return BadRequest(new { message = "Bàn chuyển đến phải khác bàn hiện tại." });

        var fromTable = await _dbContext.DiningTables.FindAsync(request.FromTableId);
        var toTable = await _dbContext.DiningTables.FindAsync(request.ToTableId);

        if (fromTable == null || toTable == null)
            return NotFound(new { message = "Không tìm thấy thông tin bàn ăn." });

        if (!toTable.IsActive)
            return BadRequest(new { message = "Bàn chuyển đến đã ngừng hoạt động." });

        if (toTable.TableStatus != "empty")
            return BadRequest(new { message = $"Bàn '{toTable.TableName}' đang ở trạng thái '{toTable.TableStatus}', không thể chuyển đến." });

        var orderTable = await _dbContext.OrderTables
            .Include(ot => ot.Order)
            .FirstOrDefaultAsync(ot => ot.TableId == request.FromTableId && 
                                      (ot.Order.OrderStatus == "open" || ot.Order.OrderStatus == "preparing" || ot.Order.OrderStatus == "serving"));

        if (orderTable == null)
            return BadRequest(new { message = $"Bàn '{fromTable.TableName}' hiện không có đơn hàng nào đang mở." });

        var now = DateTime.UtcNow;

        // Xóa liên kết bàn cũ, tạo liên kết bàn mới
        _dbContext.OrderTables.Remove(orderTable);
        _dbContext.OrderTables.Add(new OrderTable
        {
            OrderId = orderTable.OrderId,
            TableId = toTable.TableId,
            IsPrimary = true,
            AssignedAt = now
        });

        fromTable.TableStatus = "empty";
        fromTable.UpdatedAt = now;

        toTable.TableStatus = "serving";
        toTable.UpdatedAt = now;

        orderTable.Order.UpdatedAt = now;

        await _dbContext.SaveChangesAsync();

        try
        {
            await _hubContext.Clients.All.SendAsync("KitchenUpdated", new { orderId = orderTable.OrderId, eventType = "table_switched" });
            await _hubContext.Clients.All.SendAsync("StaffOrderUpdated", new { orderId = orderTable.OrderId, eventType = "table_switched" });
        }
        catch { }

        return Ok(new
        {
            message = $"Đã chuyển toàn bộ đơn hàng #{orderTable.OrderId} từ '{fromTable.TableName}' sang '{toTable.TableName}' thành công!"
        });
    }

    [HttpPost("merge-tables")]
    public async Task<IActionResult> MergeTables([FromBody] MergeTablesRequest request)
    {
        if (request.CurrentTableId == request.TargetTableId)
            return BadRequest(new { message = "Bàn thao tác và bàn đích phải khác nhau." });

        if (request.ActionType is not ("link_empty" or "merge_orders"))
            return BadRequest(new { message = "Kiểu ghép bàn không hợp lệ." });

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var currentLink = await _dbContext.OrderTables
            .Include(ot => ot.Order)
                .ThenInclude(o => o.OrderItems)
            .Include(ot => ot.Order)
                .ThenInclude(o => o.OrderTables)
            .Include(ot => ot.Table)
            .FirstOrDefaultAsync(ot => ot.TableId == request.CurrentTableId &&
                (ot.Order.OrderStatus == "open" || ot.Order.OrderStatus == "preparing" || ot.Order.OrderStatus == "serving"));

        var targetTable = await _dbContext.DiningTables.FindAsync(request.TargetTableId);
        if (currentLink == null || targetTable == null || !targetTable.IsActive)
            return BadRequest(new { message = "Không tìm thấy order đang phục vụ hoặc bàn đích không hợp lệ." });

        var currentOrder = currentLink.Order;
        var now = DateTime.UtcNow;
        var currentArea = currentLink.Table.AreaName?.Trim() ?? string.Empty;

        if (!string.Equals(targetTable.AreaName?.Trim() ?? string.Empty, currentArea, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Chỉ được ghép hoặc gộp bàn trong cùng khu vực." });

        if (request.ActionType == "link_empty")
        {
            if (targetTable.TableStatus != "empty")
                return BadRequest(new { message = $"Bàn '{targetTable.TableName}' không còn trống." });

            var alreadyLinked = await _dbContext.OrderTables.AnyAsync(ot =>
                ot.OrderId == currentOrder.OrderId && ot.TableId == targetTable.TableId);
            if (alreadyLinked)
                return BadRequest(new { message = "Bàn này đã được ghép vào order hiện tại." });

            _dbContext.OrderTables.Add(new OrderTable
            {
                OrderId = currentOrder.OrderId,
                TableId = targetTable.TableId,
                IsPrimary = false,
                AssignedAt = now
            });
            targetTable.TableStatus = "serving";
            targetTable.UpdatedAt = now;
            currentOrder.UpdatedAt = now;

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            await BroadcastOrderUpdate(currentOrder.OrderId, "table_linked");
            return Ok(new { message = $"Đã ghép bàn '{targetTable.TableName}' vào order #{currentOrder.OrderId}.", orderId = currentOrder.OrderId });
        }

        var sourceLink = await _dbContext.OrderTables
            .Include(ot => ot.Order)
                .ThenInclude(o => o.OrderItems)
            .Include(ot => ot.Order)
                .ThenInclude(o => o.OrderTables)
            .FirstOrDefaultAsync(ot => ot.TableId == request.TargetTableId &&
                (ot.Order.OrderStatus == "open" || ot.Order.OrderStatus == "preparing" || ot.Order.OrderStatus == "serving"));

        if (sourceLink == null || sourceLink.OrderId == currentOrder.OrderId)
            return BadRequest(new { message = "Bàn order nguồn không hợp lệ hoặc trùng order hiện tại." });

        var sourceOrder = sourceLink.Order;
        var sourceTableLinks = sourceOrder.OrderTables.ToList();
        var sourceTables = await _dbContext.DiningTables
            .Where(t => sourceTableLinks.Select(link => link.TableId).Contains(t.TableId))
            .ToListAsync();

        if (sourceTables.Any(table => !string.Equals(table.AreaName?.Trim() ?? string.Empty, currentArea, StringComparison.OrdinalIgnoreCase)))
            return BadRequest(new { message = "Không thể gộp order khác khu vực. Hãy chuyển các bàn về cùng khu vực trước." });

        foreach (var item in sourceOrder.OrderItems.Where(i => i.ItemStatus != "cancelled"))
            item.OrderId = currentOrder.OrderId;

        if (!currentOrder.CustomerId.HasValue)
            currentOrder.CustomerId = sourceOrder.CustomerId;

        sourceOrder.MergedIntoOrderId = currentOrder.OrderId;
        sourceOrder.OrderStatus = "merged";
        sourceOrder.UpdatedAt = now;
        currentOrder.UpdatedAt = now;

        foreach (var link in sourceTableLinks)
        {
            _dbContext.OrderTables.Remove(link);
            if (request.KeepSourceTableLinked)
            {
                _dbContext.OrderTables.Add(new OrderTable
                {
                    OrderId = currentOrder.OrderId,
                    TableId = link.TableId,
                    IsPrimary = false,
                    AssignedAt = now
                });
            }
        }

        foreach (var table in sourceTables)
        {
            table.TableStatus = request.KeepSourceTableLinked ? "serving" : "empty";
            table.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync();
        await RecalculateOrderTotals(currentOrder.OrderId);
        await transaction.CommitAsync();
        await BroadcastOrderUpdate(currentOrder.OrderId, "orders_merged");

        return Ok(new
        {
            message = request.KeepSourceTableLinked
                ? $"Đã gộp order #{sourceOrder.OrderId} vào order #{currentOrder.OrderId} và giữ các bàn ghép."
                : $"Đã gộp order #{sourceOrder.OrderId} vào order #{currentOrder.OrderId} và trả bàn nguồn về trống.",
            orderId = currentOrder.OrderId
        });
    }

    [HttpPost("unlink-table")]
    public async Task<IActionResult> UnlinkTable([FromBody] UnlinkTableRequest request)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var link = await _dbContext.OrderTables
            .Include(ot => ot.Order)
            .Include(ot => ot.Table)
            .FirstOrDefaultAsync(ot => ot.TableId == request.TableId &&
                (ot.Order.OrderStatus == "open" || ot.Order.OrderStatus == "preparing" || ot.Order.OrderStatus == "serving"));

        if (link == null)
            return NotFound(new { message = "Không tìm thấy bàn đang được ghép vào order." });
        if (link.IsPrimary)
            return BadRequest(new { message = "Không thể tháo bàn chính. Hãy đổi bàn hoặc hủy order." });

        _dbContext.OrderTables.Remove(link);
        link.Table.TableStatus = "empty";
        link.Table.UpdatedAt = DateTime.UtcNow;
        link.Order.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        await BroadcastOrderUpdate(link.OrderId, "table_unlinked");

        return Ok(new { message = $"Đã tháo bàn '{link.Table.TableName}' khỏi order #{link.OrderId}.", orderId = link.OrderId });
    }

    // 9. Hủy Order (khi chưa có món nào cooking/ready/served)
    [HttpPost("{orderId}/cancel")]
    public async Task<IActionResult> CancelOrder(int orderId)
    {
        var order = await _dbContext.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.OrderTables)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
            return NotFound(new { message = "Không tìm thấy đơn hàng này." });

        if (order.OrderStatus == "paid" || order.OrderStatus == "cancelled")
            return BadRequest(new { message = $"Đơn hàng đã ở trạng thái '{order.OrderStatus}', không thể hủy." });

        // Kiểm tra xem đã có món nào đang nấu hoặc nấu xong chưa
        bool hasInProcessItems = order.OrderItems.Any(i => i.ItemStatus == "cooking" || i.ItemStatus == "ready" || i.ItemStatus == "served");
        if (hasInProcessItems)
            return BadRequest(new { message = "Đơn hàng đã có món đang chế biến hoặc đã phục vụ, không thể hủy đơn tùy tiện." });

        var now = DateTime.UtcNow;
        order.OrderStatus = "cancelled";
        order.CancelledAt = now;
        order.UpdatedAt = now;

        // Chuyển toàn bộ món pending/confirmed sang cancelled
        foreach (var item in order.OrderItems)
        {
            item.ItemStatus = "cancelled";
            item.UpdatedAt = now;
        }

        // Giải phóng bàn ăn về 'empty'
        foreach (var ot in order.OrderTables)
        {
            var table = await _dbContext.DiningTables.FindAsync(ot.TableId);
            if (table != null)
            {
                table.TableStatus = "empty";
                table.UpdatedAt = now;
            }
        }

        await _dbContext.SaveChangesAsync();

        try
        {
            await _hubContext.Clients.All.SendAsync("KitchenUpdated", new { orderId = order.OrderId, eventType = "order_cancelled" });
            await _hubContext.Clients.All.SendAsync("StaffOrderUpdated", new { orderId = order.OrderId, eventType = "order_cancelled" });
        }
        catch { }

        return Ok(new { message = $"Đã hủy đơn hàng #{order.OrderId} và giải phóng bàn ăn về trạng thái trống thành công." });
    }

    // 10. API lấy nhanh thực đơn (Menu) cho giao diện POS phục vụ
    [HttpGet("menu")]
    public async Task<IActionResult> GetPosMenu()
    {
        var categories = await _dbContext.FoodCategories
            .OrderBy(c => c.SortOrder)
            .Select(c => new
            {
                c.CategoryId,
                c.CategoryName,
                c.Description
            })
            .ToListAsync();

        var foods = await _dbContext.Foods
            .Where(f => f.IsActive && f.FoodStatus == "available")
            .OrderBy(f => f.FoodName)
            .Select(f => new
            {
                f.FoodId,
                f.CategoryId,
                f.FoodName,
                f.ImageUrl,
                f.MinPrice,
                Variants = f.FoodVariants
                    .Where(v => v.IsActive)
                    .OrderBy(v => v.SortOrder)
                    .Select(v => new
                    {
                        v.VariantId,
                        v.VariantName,
                        v.Price,
                        v.Unit
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(new { categories, foods });
    }

    private async Task RecalculateOrderTotals(int orderId)
    {
        var order = await _dbContext.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order != null)
        {
            order.Subtotal = order.OrderItems
                .Where(i => i.ItemStatus != "cancelled")
                .Sum(i => i.UnitPrice * i.Quantity);

            order.TotalAmount = Math.Max(0, order.Subtotal - order.DiscountAmount);
            order.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
        }
    }

    private async Task BroadcastOrderUpdate(int orderId, string eventType)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("StaffOrderUpdated", new { orderId, eventType });
            await _hubContext.Clients.All.SendAsync("KitchenUpdated", new { orderId, eventType });
        }
        catch { }
    }
}
