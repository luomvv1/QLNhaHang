using System.Globalization;
using System.Text.Json;
using Restaurant.Api.Entities;

namespace Restaurant.Api.Reporting;

public sealed record ReportIssueLine(int ReceiptId, string ReceiptCode, DateTime ApprovedAt,
    int InventoryItemId, decimal Quantity, decimal Subtotal, string? BomSnapshotJson);

public sealed record ProductRecipeCost(decimal UnitCost, bool HasCost);

public static class ReportCalculations
{
    public static bool IsEarnedPointType(string? type)
        => string.Equals(type, "earn", StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, "earned", StringComparison.OrdinalIgnoreCase);

    public static bool IsRedeemedPointType(string? type)
        => string.Equals(type, "redeem", StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, "used", StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, "redeemed", StringComparison.OrdinalIgnoreCase);

    public static bool IsPositiveAdjustment(string movementType, string? reason)
        => movementType == "adjustment" && !string.IsNullOrWhiteSpace(reason)
            && CultureInfo.InvariantCulture.CompareInfo.IndexOf(reason, "thua",
                CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

    public static decimal MovementDelta(string movementType, decimal quantity, string? reason)
        => movementType == "import" || IsPositiveAdjustment(movementType, reason) ? quantity : -quantity;

    public static decimal ProportionalAmount(decimal sourceQuantity, decimal sourceAmount, decimal quantity)
        => sourceQuantity > 0 ? sourceAmount * quantity / sourceQuantity : 0m;

    public static DateOnly IssueDate(string receiptCode, DateTime approvedAt)
    {
        var offset = receiptCode.StartsWith("PXK-BS-", StringComparison.Ordinal) ? 7
            : receiptCode.StartsWith("PXK-", StringComparison.Ordinal) ? 4 : -1;
        if (offset >= 0 && receiptCode.Length >= offset + 8
            && DateOnly.TryParseExact(receiptCode.Substring(offset, 8), "yyyyMMdd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;
        return DateOnly.FromDateTime(approvedAt.AddHours(7));
    }

    public static Dictionary<(DateOnly Date, int FoodId, int? VariantId), ProductRecipeCost> RecipeCosts(
        IEnumerable<ReportIssueLine> lines, DateOnly fromDate, DateOnly toDate)
    {
        var entries = new List<(int ReceiptId, DateOnly Date, int FoodId, int? VariantId,
            int ItemId, decimal Portions, decimal Amount, bool HasCost)>();
        var incompleteDates = new HashSet<DateOnly>();
        foreach (var line in lines)
        {
            var date = IssueDate(line.ReceiptCode, line.ApprovedAt);
            if (date < fromDate || date > toDate) continue;
            if (string.IsNullOrWhiteSpace(line.BomSnapshotJson))
            {
                incompleteDates.Add(date);
                continue;
            }
            List<StockReceiptBomSnapshotEntry>? snapshot;
            try
            {
                snapshot = JsonSerializer.Deserialize<List<StockReceiptBomSnapshotEntry>>(line.BomSnapshotJson);
            }
            catch (JsonException)
            {
                incompleteDates.Add(date);
                continue;
            }
            if (snapshot == null || snapshot.Count == 0)
            {
                incompleteDates.Add(date);
                continue;
            }
            if (snapshot.Any(entry => entry == null || entry.FoodId <= 0 || entry.Portions <= 0 || entry.QuantityPerUnit <= 0)
                || Math.Abs(snapshot.Sum(entry => entry.Portions * entry.QuantityPerUnit) - line.Quantity) > 0.001m)
                incompleteDates.Add(date);
            foreach (var entry in snapshot.Where(entry => entry != null && entry.FoodId > 0 && entry.Portions > 0 && entry.QuantityPerUnit > 0))
            {
                entries.Add((line.ReceiptId, date, entry.FoodId, entry.VariantId, line.InventoryItemId,
                    entry.Portions, ProportionalAmount(line.Quantity, line.Subtotal,
                        entry.Portions * entry.QuantityPerUnit), line.Quantity > 0 && line.Subtotal > 0));
            }
        }

        // Each ingredient repeats a dish's portions; FEFO splits those portions
        // between batches. Count portions once per receipt, not once per ingredient.
        var recipes = entries.GroupBy(entry => (entry.ReceiptId, entry.Date, entry.FoodId, entry.VariantId))
            .Select(group => new
            {
                group.Key.Date, group.Key.FoodId, group.Key.VariantId,
                Portions = group.GroupBy(entry => entry.ItemId).Max(items => items.Sum(entry => entry.Portions)),
                Amount = group.Sum(entry => entry.Amount),
                // Missing allocations may belong to any dish issued that day.
                HasCost = !incompleteDates.Contains(group.Key.Date) && group.All(entry => entry.HasCost)
            });
        return recipes.GroupBy(recipe => (recipe.Date, recipe.FoodId, recipe.VariantId))
            .ToDictionary(group => group.Key, group => new ProductRecipeCost(
                group.Sum(recipe => recipe.Amount) / group.Sum(recipe => recipe.Portions),
                group.All(recipe => recipe.HasCost)));
    }
}
