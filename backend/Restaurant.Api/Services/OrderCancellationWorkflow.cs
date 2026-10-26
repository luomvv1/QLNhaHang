using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;

namespace Restaurant.Api.Services;

public record CancelOrderRequest(string? Reason);
public record CancellationOutcome(int StatusCode, string Message, string? FoodName = null, string? VariantName = null);

public static class OrderCancellationWorkflow
{
    public static bool IsOpen(string status) => status is "open" or "preparing" or "serving";
    public static bool CanCancelItem(OrderItem item) => (item.ItemStatus is "pending" or "confirmed") &&
        item.ChefId == null && item.ReadyAt == null;

    public static async Task<CancellationOutcome> CancelAsync(RestaurantDbContext db, int orderId,
        int? itemId, string? reason, int userId, bool fromKitchen = false)
    {
        reason = reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length < 3 || reason.Length > 500)
            return new(400, "Vui lòng nhập lý do hủy từ 3 đến 500 ký tự.");

        // Share the same isolation level as kitchen transitions so a cancellation cannot race cooking/payment.
        await using var transaction = db.Database.CurrentTransaction == null
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        if (!await PortionAvailabilityService.TryLockAsync(db))
            return new(409, "Đang cập nhật suất ăn. Vui lòng thử lại.");
        var order = await db.Orders.Include(o => o.OrderItems).Include(o => o.OrderTables)
            .ThenInclude(ot => ot.Table).FirstOrDefaultAsync(o => o.OrderId == orderId);
        if (order == null) return new(404, "Không tìm thấy đơn hàng.");
        if (!IsOpen(order.OrderStatus) || await db.Invoices.AnyAsync(i => i.OrderId == orderId))
            return new(409, "Đơn đã đóng, đã lập hóa đơn hoặc đã thanh toán; không thể hủy trực tiếp.");

        var items = itemId.HasValue
            ? order.OrderItems.Where(i => i.OrderItemId == itemId).ToList()
            : order.OrderItems.Where(i => i.ItemStatus != "cancelled").ToList();
        if (itemId.HasValue && items.Count == 0) return new(404, "Không tìm thấy món trong đơn.");
        if (itemId.HasValue && items[0].ItemStatus == "cancelled")
            return new(409, "Món này đã được hủy trước đó.");
        if (items.Any(i => i.ItemStatus == "served"))
            return new(409, "Món đã phục vụ không thể hủy trực tiếp. Cần xử lý với quản lý.");
        if (items.Any(i => !CanCancelItem(i)))
            return new(409, "Món đã bắt đầu chế biến hoặc đã làm xong, không được hủy kể cả sau khi hoàn tác trạng thái.");
        if (fromKitchen && items.Any(i => i.ItemStatus != "confirmed"))
            return new(409, "Bếp chỉ được từ chối món đang chờ nấu.");

        var now = DateTime.UtcNow;
        foreach (var item in items)
        {
            item.CancelledFromStatus = item.ItemStatus;
            item.ItemStatus = "cancelled";
            item.CancelledAt = now;
            item.CancelledBy = userId;
            item.CancellationReason = reason;
            item.UpdatedAt = now;
        }
        if (!itemId.HasValue)
        {
            order.OrderStatus = "cancelled";
            order.CancelledAt = now;
            order.CancelledBy = userId;
            order.CancellationReason = reason;
            foreach (var linked in order.OrderTables)
            {
                linked.Table.TableStatus = "empty";
                linked.Table.UpdatedAt = now;
            }
        }
        order.Subtotal = order.OrderItems.Where(i => i.ItemStatus != "cancelled").Sum(i => i.UnitPrice * i.Quantity);
        order.TotalAmount = Math.Max(0, order.Subtotal - order.DiscountAmount);
        order.UpdatedAt = now;
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId, ActionName = fromKitchen ? "kitchen_item_cancelled" : itemId.HasValue ? "order_item_cancelled" : "order_cancelled",
            EntityName = itemId.HasValue ? "OrderItem" : "Order", EntityId = (itemId ?? orderId).ToString(),
            NewData = JsonSerializer.Serialize(new {
                orderId,
                itemId,
                reason,
                foodName = items.FirstOrDefault()?.FoodNameSnapshot,
                variantName = items.FirstOrDefault()?.VariantSnapshot,
                items = items.Select(i => new { i.OrderItemId, i.Quantity, i.CancelledFromStatus, i.FoodNameSnapshot })
            }),
            CreatedAt = now
        });
        await db.SaveChangesAsync();
        if (transaction != null) await transaction.CommitAsync();
        return new(200, itemId.HasValue ? "Đã hủy món và cập nhật tiền đơn hàng." : "Đã hủy đơn hàng và giải phóng bàn.",
            items.FirstOrDefault()?.FoodNameSnapshot, items.FirstOrDefault()?.VariantSnapshot);
    }
}
