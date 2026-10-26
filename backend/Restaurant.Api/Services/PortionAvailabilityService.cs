using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;

namespace Restaurant.Api.Services;

public readonly record struct PortionKey(int FoodId, int? VariantId);
public sealed record IssuedPortions(int ReceiptId, PortionKey Key, decimal Portions, bool Additional, string Status);

public sealed class PortionQuota
{
    public PortionKey Key { get; init; }
    public int? ControlId { get; init; }
    public bool IsManaged { get; init; }
    public bool IsClosed { get; init; }
    public decimal InitialApprovedPortions { get; init; }
    public decimal AdditionalApprovedPortions { get; init; }
    public decimal PendingAdditionalPortions { get; init; }
    public decimal IssuedPortions => InitialApprovedPortions + AdditionalApprovedPortions;
    public decimal ServedPortions { get; init; }
    public decimal ReservedPortions { get; set; }
    public decimal RemainingPortions => IsClosed ? 0 : Math.Max(0, IssuedPortions - ServedPortions - ReservedPortions);
    public string Status => !IsManaged ? "unplanned" : IsClosed ? "closed" : IssuedPortions <= 0 ? "awaiting_issue" : RemainingPortions <= 0 ? "plan_full" : "available";
}

public sealed record OrderingAvailability(bool CanOrder, string Status, string Label, PortionQuota? Plan);
public sealed record PortionShortage(int FoodId, int? VariantId, string FoodName, decimal RequestedPortions,
    decimal AvailablePortions, string Status);

public sealed class DayPortionAvailability
{
    public DateTime Date { get; init; }
    public Dictionary<PortionKey, PortionQuota> Quotas { get; init; } = new();
    public List<IssuedPortions> Allocations { get; init; } = new();

    public PortionQuota? Resolve(int foodId, int? variantId)
    {
        Quotas.TryGetValue(new(foodId, variantId), out var exact);
        if (exact?.IsManaged == true) return exact;
        Quotas.TryGetValue(new(foodId, null), out var common);
        return common?.IsManaged == true ? common : exact ?? common;
    }

    public OrderingAvailability For(Food food, FoodVariant variant)
    {
        var quota = Resolve(food.FoodId, variant.VariantId);
        var plan = quota?.IsManaged == true ? quota : null;
        if (!food.IsActive || !variant.IsActive || food.FoodStatus is not ("available" or "sold_out"))
            return new(false, "inactive", "Ngừng kinh doanh", plan);
        if (food.FoodStatus == "sold_out" || variant.IsSoldOut)
            return new(false, "kitchen_sold_out", "Bếp tạm hết món", plan);
        return plan?.Status switch
        {
            "closed" => new(false, "closed", "Đã chốt nhận món", plan),
            "awaiting_issue" => new(false, "awaiting_issue", "Chờ duyệt xuất bếp", plan),
            "plan_full" => new(false, "plan_full", "Đã nhận đủ suất", plan),
            _ => new(true, "available", plan == null ? "Đang phục vụ" : $"Còn nhận {plan.RemainingPortions:0.####} suất", plan)
        };
    }
}

public static class PortionAvailabilityService
{
    public static DateTime Today => DateTime.UtcNow.AddHours(7).Date;
    public static DateTime ServiceDate(OrderItem item, DateTime now) =>
        item.PortionDate?.Date ?? (item.ConfirmedAt ?? now).AddHours(7).Date;

    public static List<PortionShortage> Check(DayPortionAvailability availability, IEnumerable<OrderItem> items) =>
        items.Select(item => new { Item = item, Quota = availability.Resolve(item.FoodId, item.VariantId) })
            .Where(x => x.Quota?.IsManaged == true).GroupBy(x => x.Quota!.Key)
            .Where(g => g.First().Quota!.IsClosed || g.Sum(x => x.Item.Quantity) > g.First().Quota!.RemainingPortions)
            .Select(g => new PortionShortage(g.Key.FoodId, g.Key.VariantId, g.First().Item.FoodNameSnapshot,
                g.Sum(x => x.Item.Quantity), g.First().Quota!.IsClosed ? 0 : g.First().Quota!.RemainingPortions, g.First().Quota!.Status)).ToList();

    public static object MenuFood(Food food, DayPortionAvailability availability)
    {
        var variants = food.FoodVariants.Where(v => v.IsActive).OrderBy(v => v.SortOrder).Select(v =>
        {
            var state = availability.For(food, v);
            return new { v.VariantId, v.VariantName, v.Price, v.Unit, v.IsSoldOut, state.CanOrder,
                AvailabilityStatus = state.Status, AvailabilityText = state.Label, state.Plan };
        }).ToList();
        var canOrder = variants.Any(v => v.CanOrder);
        var label = canOrder ? variants.Any(v => !v.CanOrder) ? "Một số size tạm ngừng nhận" : "Đang phục vụ"
            : variants.Any(v => v.AvailabilityStatus == "kitchen_sold_out") ? "Bếp tạm hết món"
            : variants.Any(v => v.AvailabilityStatus == "awaiting_issue") ? "Chờ duyệt xuất bếp" : "Đã nhận đủ suất";
        return new { food.FoodId, food.CategoryId, CategoryName = food.Category?.CategoryName, food.FoodName,
            food.Description, food.ImageUrl, food.MinPrice, Price = food.MinPrice, food.FoodStatus, CanOrder = canOrder,
            AvailabilityText = label, HasSoldOutVariants = variants.Any(v => v.IsSoldOut),
            HasUnavailableVariants = variants.Any(v => !v.CanOrder), Variants = variants };
    }

    // A short database-owned mutex serializes quota mutations across API instances.
    // It is acquired before reading order rows, avoiding lock-order cycles between serving and confirming.
    public static async Task<bool> TryLockAsync(RestaurantDbContext db)
    {
        var transaction = db.Database.CurrentTransaction ?? throw new InvalidOperationException("Portion allocation requires a transaction.");
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = "DECLARE @r int; EXEC @r = sys.sp_getapplock @Resource=N'restaurant:portion-allocation', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=4000; SELECT @r;";
        return Convert.ToInt32(await command.ExecuteScalarAsync()) >= 0;
    }

    public static async Task<DayPortionAvailability> LoadAsync(RestaurantDbContext db, DateTime date)
    {
        await using var readTransaction = db.Database.CurrentTransaction == null
            ? await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted) : null;
        if (readTransaction != null && !await TryLockAsync(db))
            throw new TimeoutException("Đang cập nhật suất ăn. Vui lòng tải lại.");
        date = date.Date;
        var controls = await db.DailyPortionControls.AsNoTracking().Include(p => p.Food).Include(p => p.Variant)
            .Where(p => p.ControlDate == date).ToListAsync();
        var initialPrefix = $"PXK-{date:yyyyMMdd}-";
        var additionalPrefix = $"PXK-BS-{date:yyyyMMdd}-";
        var receipts = await db.StockReceipts.AsNoTracking().Where(r => r.ReceiptType == "export_kitchen" &&
            (r.ReceiptCode.StartsWith(initialPrefix) || r.ReceiptCode.StartsWith(additionalPrefix)) &&
            (r.Status == "approved" || r.Status == "draft"))
            .Include(r => r.StockReceiptDetails).AsSplitQuery().ToListAsync();
        var allocations = new List<IssuedPortions>();
        foreach (var receipt in receipts)
        {
            var additional = receipt.ReceiptCode.StartsWith(additionalPrefix, StringComparison.Ordinal);
            var entries = new List<(PortionKey Key, int Ingredient, decimal Portions, bool Snapshot)>();
            foreach (var detail in receipt.StockReceiptDetails)
            {
                List<StockReceiptBomSnapshotEntry>? snapshot = null;
                if (!string.IsNullOrWhiteSpace(detail.BomSnapshotJson))
                {
                    try { snapshot = JsonSerializer.Deserialize<List<StockReceiptBomSnapshotEntry>>(detail.BomSnapshotJson); }
                    catch (JsonException) { }
                }
                if (snapshot?.Count > 0)
                {
                    entries.AddRange(snapshot.Where(e => e.Portions > 0).Select(e =>
                        (new PortionKey(e.FoodId, e.VariantId), detail.InventoryItemId, e.Portions, true)));
                }
                else if (!string.IsNullOrWhiteSpace(detail.Note))
                {
                    // Read-only compatibility with receipts predating immutable BOM snapshots.
                    foreach (var segment in detail.Note.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                    {
                        var pattern = additional ? @"^(?<label>.+?)\s*\[(?<n>[\d.,]+)\s+suất bổ sung\]$" : @"^(?<label>.+?)\s*\[(?<n>[\d.,]+)\s+suất\]$";
                        var match = Regex.Match(segment, pattern, RegexOptions.IgnoreCase);
                        if (!match.Success || !decimal.TryParse(match.Groups["n"].Value.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount)) continue;
                        var candidates = controls.Where(p => string.Equals(p.Variant == null ? p.Food.FoodName : $"{p.Food.FoodName} ({p.Variant.VariantName})",
                            match.Groups["label"].Value.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
                        if (candidates.Count == 1 && amount > 0) entries.Add((new(candidates[0].FoodId, candidates[0].VariantId), detail.InventoryItemId, amount, false));
                    }
                }
            }
            // Sum FEFO splits per ingredient, then count each dish once, not once per BOM ingredient.
            allocations.AddRange(entries.GroupBy(e => new { e.Key, e.Ingredient })
                .Select(g => new { g.Key.Key, Portions = g.Any(e => e.Snapshot) ? g.Where(e => e.Snapshot).Sum(e => e.Portions) : g.Max(e => e.Portions) }).GroupBy(e => e.Key)
                .Select(g => new IssuedPortions(receipt.ReceiptId, g.Key, Math.Round(g.Max(e => e.Portions), 4), additional, receipt.Status)));
        }

        var receiptIds = receipts.Select(r => r.ReceiptId).ToList();
        var closedReceipts = await db.KitchenReconciliationLines.AsNoTracking()
            .Where(line => receiptIds.Contains(line.SourceDetail.ReceiptId)).Select(line => line.SourceDetail.ReceiptId).Distinct().ToListAsync();
        var keys = controls.Select(p => new PortionKey(p.FoodId, p.VariantId)).Concat(allocations.Select(a => a.Key)).Distinct();
        var result = new DayPortionAvailability { Date = date, Allocations = allocations };
        foreach (var key in keys)
        {
            var control = controls.FirstOrDefault(p => p.FoodId == key.FoodId && p.VariantId == key.VariantId);
            var issued = allocations.Where(a => a.Key == key).ToList();
            result.Quotas[key] = new PortionQuota
            {
                Key = key, ControlId = control?.PortionControlId,
                IsManaged = control?.PlannedPortions > 0 || control?.Status is "planned" or "closed" or "reconciled" || issued.Count > 0,
                IsClosed = control?.Status is "closed" or "reconciled" || issued.Any(a => closedReceipts.Contains(a.ReceiptId)),
                InitialApprovedPortions = issued.Where(a => !a.Additional && a.Status == "approved").Sum(a => a.Portions),
                AdditionalApprovedPortions = issued.Where(a => a.Additional && a.Status == "approved").Sum(a => a.Portions),
                PendingAdditionalPortions = issued.Where(a => a.Additional && a.Status == "draft").Sum(a => a.Portions),
                ServedPortions = control?.SoldPortions ?? 0
            };
        }
        var fromUtc = date.AddHours(-7);
        var toUtc = fromUtc.AddDays(1);
        var reserved = await db.OrderItems.AsNoTracking().Where(i =>
            (i.ItemStatus == "confirmed" || i.ItemStatus == "cooking" || i.ItemStatus == "ready") &&
            (i.Order.OrderStatus == "open" || i.Order.OrderStatus == "preparing" || i.Order.OrderStatus == "serving" || i.Order.OrderStatus == "paid") &&
            (i.PortionDate == date || (i.PortionDate == null && (i.ConfirmedAt ?? i.CreatedAt) >= fromUtc && (i.ConfirmedAt ?? i.CreatedAt) < toUtc)))
            .GroupBy(i => new { i.FoodId, i.VariantId }).Select(g => new { g.Key.FoodId, g.Key.VariantId, Quantity = g.Sum(i => i.Quantity) }).ToListAsync();
        foreach (var group in reserved)
        {
            var quota = result.Resolve(group.FoodId, group.VariantId);
            if (quota != null) quota.ReservedPortions += group.Quantity;
        }
        if (readTransaction != null) await readTransaction.CommitAsync();
        return result;
    }
}
