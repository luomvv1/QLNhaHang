using Microsoft.EntityFrameworkCore;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;

namespace Restaurant.Api.Services;

public sealed record KitchenBatchPick(int BatchId, string BatchCode, DateTime ExpiryDate,
    decimal Quantity, decimal UnitCost, decimal Subtotal);
public sealed record KitchenIssuePreviewLine(int DetailId, int InventoryItemId, decimal RequiredQuantity,
    decimal ShortageQuantity, IReadOnlyList<KitchenBatchPick> Batches);
public sealed record KitchenIssuePreview(bool CanApprove, DateTime CheckedAt,
    decimal TotalAmount, IReadOnlyList<KitchenIssuePreviewLine> Lines, IReadOnlyList<string> Errors);

public static class KitchenBatchAllocation
{
    public static IOrderedQueryable<InventoryBatch> AvailableBatches(IQueryable<InventoryBatch> batches, DateTime localDate) =>
        batches.Where(b => b.Status == "active" && b.QuantityRemaining > 0 && b.ExpiryDate >= localDate.Date)
            .OrderByDescending(b => b.BatchCode.StartsWith("TH-BEP-"))
            .ThenBy(b => b.ExpiryDate).ThenBy(b => b.ReceivedDate).ThenBy(b => b.BatchId);

    // Simulate in approval order without changing tracked batches or reserving stock.
    public static async Task<KitchenIssuePreview> PreviewAsync(RestaurantDbContext db, StockReceipt receipt, DateTime now)
    {
        var itemIds = receipt.StockReceiptDetails.Select(d => d.InventoryItemId).Distinct().ToArray();
        var localDate = now.AddHours(7).Date;
        var batches = await AvailableBatches(db.InventoryBatches.AsNoTracking()
            .Where(b => itemIds.Contains(b.InventoryItemId)), localDate).ToListAsync();
        var remaining = batches.ToDictionary(b => b.BatchId, b => b.QuantityRemaining);
        var lines = new List<KitchenIssuePreviewLine>();
        var errors = new List<string>();
        foreach (var group in receipt.StockReceiptDetails.GroupBy(d => d.InventoryItemId))
        {
            var item = group.First().InventoryItem;
            if (item.CurrentQuantity < group.Sum(d => d.Quantity))
                errors.Add($"Tồn tổng của '{item.ItemName}' không đủ cho phiếu này.");
            foreach (var detail in group.OrderBy(d => d.DetailId))
            {
                var picks = new List<KitchenBatchPick>();
                var need = detail.Quantity;
                var candidates = batches.Where(b => b.InventoryItemId == group.Key &&
                    (!detail.BatchId.HasValue || b.BatchId == detail.BatchId));
                foreach (var batch in candidates)
                {
                    if (need <= 0) break;
                    var quantity = Math.Min(need, remaining[batch.BatchId]);
                    if (quantity <= 0) continue;
                    remaining[batch.BatchId] -= quantity;
                    need -= quantity;
                    var rate = item.ConversionRate > 0 ? item.ConversionRate : 1m;
                    picks.Add(new(batch.BatchId, batch.BatchCode, batch.ExpiryDate, quantity, batch.UnitCost,
                        Math.Round(quantity * batch.UnitCost / rate, 2)));
                }
                if (need > 0)
                    errors.Add($"'{item.ItemName}' thiếu {need:N4} {item.BaseUnit} trong các lô được phép xuất.");
                lines.Add(new(detail.DetailId, group.Key, detail.Quantity, Math.Max(need, 0), picks));
            }
        }
        return new(lines.Count > 0 && errors.Count == 0, now,
            lines.Sum(l => l.Batches.Sum(b => b.Subtotal)), lines, errors);
    }
}
