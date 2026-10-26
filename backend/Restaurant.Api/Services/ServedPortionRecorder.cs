using Microsoft.EntityFrameworkCore;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;

namespace Restaurant.Api.Services;

public static class ServedPortionRecorder
{
    // Call only for an actual serving action, before changing ready -> served.
    public static async Task RecordAsync(RestaurantDbContext db, IEnumerable<OrderItem> items, DateTime now)
    {
        foreach (var group in items.Where(item => item.ItemStatus != "served" && item.ItemStatus != "cancelled")
            .GroupBy(item => new { Date = PortionAvailabilityService.ServiceDate(item, now), item.FoodId, item.VariantId }))
        {
            var state = await PortionAvailabilityService.LoadAsync(db, group.Key.Date);
            var quota = state.Resolve(group.Key.FoodId, group.Key.VariantId);
            var variantId = quota?.IsManaged == true ? quota.Key.VariantId : group.Key.VariantId;
            var portion = await db.DailyPortionControls.FirstOrDefaultAsync(control => control.ControlDate == group.Key.Date
                && control.FoodId == group.Key.FoodId && control.VariantId == variantId);
            if (portion == null)
            {
                portion = new DailyPortionControl
                {
                    ControlDate = group.Key.Date, FoodId = group.Key.FoodId, VariantId = variantId,
                    Status = "in_service", CreatedAt = now,
                    Note = "Ghi nhận khi xác nhận phục vụ món"
                };
                db.DailyPortionControls.Add(portion);
            }
            portion.SoldPortions += group.Sum(item => item.Quantity);
            portion.UpdatedAt = now;
            foreach (var item in group)
            {
                item.PortionDate = group.Key.Date;
                item.ItemStatus = "served";
                item.UpdatedAt = now;
            }
        }
    }

    public static async Task UndoAsync(RestaurantDbContext db, OrderItem item, DateTime now)
    {
        var date = PortionAvailabilityService.ServiceDate(item, now);
        var state = await PortionAvailabilityService.LoadAsync(db, date);
        var quota = state.Resolve(item.FoodId, item.VariantId);
        var variantId = quota?.IsManaged == true ? quota.Key.VariantId : item.VariantId;
        var portion = await db.DailyPortionControls.FirstOrDefaultAsync(p => p.ControlDate == date &&
            p.FoodId == item.FoodId && p.VariantId == variantId);
        if (portion != null) { portion.SoldPortions = Math.Max(0, portion.SoldPortions - item.Quantity); portion.UpdatedAt = now; }
    }
}
