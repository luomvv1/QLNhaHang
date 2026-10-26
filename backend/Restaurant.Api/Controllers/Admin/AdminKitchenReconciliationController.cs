using System.Data;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;
using System.Text.Json;
using Restaurant.Api.Services;
using Microsoft.AspNetCore.SignalR;
using Restaurant.Api.Hubs;

namespace Restaurant.Api.Controllers.Admin;

public sealed class KitchenReconciliationInput
{
    public int SourceDetailId { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public decimal WastedQuantity { get; set; }
    public string? WasteReason { get; set; }
}

public sealed class CompleteKitchenReconciliationRequest
{
    public int SourceReceiptId { get; set; }
    public string? Note { get; set; }
    public List<KitchenReconciliationInput> Lines { get; set; } = new();
}

[ApiController]
[Route("api/admin/inventory/kitchen-reconciliation")]
[Authorize(Roles = "admin,manager")]
public sealed class AdminKitchenReconciliationController : ControllerBase
{
    private readonly RestaurantDbContext _db;
    private readonly IHubContext<RestaurantHub>? _hub;

    public AdminKitchenReconciliationController(RestaurantDbContext db, IHubContext<RestaurantHub>? hub = null) { _db = db; _hub = hub; }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateTime? date)
    {
        var localDate = (date ?? DateTime.UtcNow.AddHours(7)).Date;
        var fromUtc = localDate.AddHours(-7);
        var toUtc = fromUtc.AddDays(1);
        var usage = await CalculateServedUsage(localDate);

        var receipts = await _db.StockReceipts.AsNoTracking()
            .Where(r => r.ReceiptType == "export_kitchen" && r.Status == "approved"
                && ((r.ApprovedAt >= fromUtc && r.ApprovedAt < toUtc)
                    || r.ReceiptCode.StartsWith($"PXK-{localDate:yyyyMMdd}-")
                    || r.ReceiptCode.StartsWith($"PXK-BS-{localDate:yyyyMMdd}-")))
            .Include(r => r.StockReceiptDetails).ThenInclude(d => d.InventoryItem)
            .Include(r => r.StockReceiptDetails).ThenInclude(d => d.Batch)
            .AsSplitQuery()
            .OrderByDescending(r => r.ApprovedAt)
            .ToListAsync();

        var detailIds = receipts.SelectMany(r => r.StockReceiptDetails).Select(d => d.DetailId).ToList();
        var reconciled = await _db.KitchenReconciliationLines.AsNoTracking()
            .Where(line => detailIds.Contains(line.SourceDetailId))
            .Include(line => line.ReturnReceipt)
            .Include(line => line.WasteReceipt)
            .ToDictionaryAsync(line => line.SourceDetailId);

        var rows = receipts.Select(receipt => new
        {
            receipt.ReceiptId,
            receipt.ReceiptCode,
            receipt.ApprovedAt,
            Lines = receipt.StockReceiptDetails.OrderBy(d => d.InventoryItem.ItemName).ThenBy(d => d.BatchId)
                .Select(detail =>
                {
                    reconciled.TryGetValue(detail.DetailId, out var line);
                    return new
                    {
                        SourceDetailId = detail.DetailId,
                        detail.InventoryItemId,
                        detail.InventoryItem.ItemName,
                        detail.InventoryItem.BaseUnit,
                        detail.BatchId,
                        BatchCode = detail.Batch?.BatchCode,
                        BatchExpiryDate = detail.Batch?.ExpiryDate,
                        IssuedQuantity = detail.Quantity,
                        UsedQuantity = line?.UsedQuantity ?? usage.ByDetail.GetValueOrDefault(detail.DetailId),
                        ReturnedQuantity = line?.ReturnedQuantity ?? 0m,
                        WastedQuantity = line?.WastedQuantity ?? 0m,
                        PendingQuantity = line == null ? detail.Quantity - usage.ByDetail.GetValueOrDefault(detail.DetailId) : 0m,
                        UsageBreakdown = line == null && usage.ByDetail.GetValueOrDefault(detail.DetailId) > 0
                            ? usage.BreakdownByItem.GetValueOrDefault(detail.InventoryItemId) : null,
                        line?.WasteReason,
                        line?.ReconciledAt,
                        ReturnReceiptId = line?.ReturnReceiptId,
                        WasteReceiptId = line?.WasteReceiptId,
                        ReturnReceiptCode = line?.ReturnReceipt?.ReceiptCode,
                        WasteReceiptCode = line?.WasteReceipt?.ReceiptCode,
                        WasteValue = line == null ? 0m : InventoryCostHelper.CalculateAmount(
                            line.WastedQuantity, detail.UnitCost, detail.InventoryItem.ConversionRate)
                    };
                }).ToList()
        }).ToList();

        var closedToday = await _db.KitchenReconciliationLines.AsNoTracking()
            .Where(line => line.ReconciledAt >= fromUtc && line.ReconciledAt < toUtc)
            .Include(line => line.SourceDetail).ThenInclude(detail => detail.Receipt)
            .Include(line => line.SourceDetail).ThenInclude(detail => detail.InventoryItem)
            .Include(line => line.SourceDetail).ThenInclude(detail => detail.Batch)
            .Include(line => line.ReturnReceipt)
            .Include(line => line.WasteReceipt)
            .OrderByDescending(line => line.ReconciledAt)
            .ToListAsync();
        var dailyReconciled = closedToday.Select(line => new
        {
            line.SourceDetailId,
            SourceReceiptCode = line.SourceDetail.Receipt.ReceiptCode,
            line.ReconciledAt,
            line.SourceDetail.InventoryItem.ItemName,
            line.SourceDetail.InventoryItem.BaseUnit,
            BatchCode = line.SourceDetail.Batch?.BatchCode,
            line.IssuedQuantity,
            line.UsedQuantity,
            line.ReturnedQuantity,
            line.WastedQuantity,
            line.WasteReason,
            ReturnReceiptCode = line.ReturnReceipt?.ReceiptCode,
            WasteReceiptCode = line.WasteReceipt?.ReceiptCode,
            WasteValue = InventoryCostHelper.CalculateAmount(line.WastedQuantity,
                line.SourceDetail.UnitCost, line.SourceDetail.InventoryItem.ConversionRate)
        }).ToList();

        var kitchenWasteIds = await _db.KitchenReconciliationLines.AsNoTracking()
            .Where(line => line.WasteReceiptId.HasValue)
            .Select(line => line.WasteReceiptId!.Value)
            .Distinct().ToListAsync();
        var warehouseWasteReceipts = await _db.StockReceipts.AsNoTracking()
            .Where(r => r.ReceiptType == "waste_disposal" && r.Status == "approved"
                && r.ApprovedAt >= fromUtc && r.ApprovedAt < toUtc
                && !kitchenWasteIds.Contains(r.ReceiptId))
            .Include(r => r.StockReceiptDetails).ThenInclude(d => d.InventoryItem)
            .Include(r => r.StockReceiptDetails).ThenInclude(d => d.Batch)
            .AsSplitQuery()
            .OrderByDescending(r => r.ApprovedAt).ToListAsync();
        var warehouseWaste = warehouseWasteReceipts.Select(r => new
        {
            r.ReceiptId, r.ReceiptCode, r.ApprovedAt, r.TotalAmount, r.Note,
            Details = r.StockReceiptDetails.Select(d => new
            {
                d.InventoryItemId, d.InventoryItem.ItemName, d.InventoryItem.BaseUnit,
                d.BatchId, BatchCode = d.Batch?.BatchCode,
                d.Quantity, Reason = d.Note, d.Subtotal
            }).ToList()
        }).ToList();

        var all = rows.SelectMany(r => r.Lines).ToList();
        var availability = await PortionAvailabilityService.LoadAsync(_db, localDate);
        return Ok(new
        {
            success = true,
            date = localDate.ToString("yyyy-MM-dd"),
            receipts = rows,
            usageErrors = usage.Errors,
            receiptReservations = rows.Select(row => new { row.ReceiptId,
                ReservedPortions = availability.Allocations.Where(a => a.ReceiptId == row.ReceiptId).Select(a => a.Key).Distinct()
                    .Sum(key => availability.Quotas.GetValueOrDefault(key)?.ReservedPortions ?? 0) }).ToList(),
            dailyReconciled,
            warehouseWaste,
            summary = new
            {
                receiptCount = rows.Count,
                lineCount = all.Count,
                closedCount = all.Count(line => line.ReconciledAt.HasValue),
                pendingCount = all.Count(line => !line.ReconciledAt.HasValue),
                kitchenWasteValue = dailyReconciled.Sum(line => line.WasteValue),
                returnLineCount = dailyReconciled.Count(line => line.ReturnedQuantity > 0),
                wasteLineCount = dailyReconciled.Count(line => line.WastedQuantity > 0),
                warehouseWasteValue = warehouseWaste.Sum(r => r.TotalAmount)
            }
        });
    }

    [HttpPost("complete")]
    public async Task<IActionResult> Complete([FromBody] CompleteKitchenReconciliationRequest request)
    {
        var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(rawUserId, out var userId)) return Unauthorized();
        if (request.Lines == null || request.Lines.Count == 0
            || request.Lines.Select(line => line.SourceDetailId).Distinct().Count() != request.Lines.Count)
            return BadRequest(new { message = "Chon moi dong xuat bep dung mot lan." });
        if (request.Note?.Length > 300)
            return BadRequest(new { message = "Ghi chu ban giao khong duoc qua 300 ky tu." });

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            if (!await PortionAvailabilityService.TryLockAsync(_db))
                return Conflict(new { message = "Đang cập nhật suất ăn. Vui lòng thử lại." });
            var source = await _db.StockReceipts
                .Include(r => r.StockReceiptDetails).ThenInclude(d => d.InventoryItem)
                .Include(r => r.StockReceiptDetails).ThenInclude(d => d.Batch)
                .FirstOrDefaultAsync(r => r.ReceiptId == request.SourceReceiptId
                    && r.ReceiptType == "export_kitchen" && r.Status == "approved");
            if (source == null) return NotFound(new { message = "Khong tim thay phieu xuat bep da duyet." });

            var details = source.StockReceiptDetails.OrderBy(d => d.DetailId).ToList();
            if (details.Count == 0 || details.Count != request.Lines.Count
                || details.Any(d => request.Lines.All(line => line.SourceDetailId != d.DetailId)))
                return BadRequest(new { message = "Can doi soat day du cac dong cua phieu xuat bep." });
            if (details.Any(d => !d.BatchId.HasValue || d.Batch == null))
                return BadRequest(new { message = "Phieu xuat bep chua co lo FEFO; khong the doi soat." });

            var detailIds = details.Select(d => d.DetailId).ToList();
            if (await _db.KitchenReconciliationLines.AnyAsync(line => detailIds.Contains(line.SourceDetailId)))
                return Conflict(new { message = "Phieu nay da duoc doi soat. Tai lai bao cao de xem so lieu moi." });

            var usageDate = GetUsageDate(source);
            var availability = await PortionAvailabilityService.LoadAsync(_db, usageDate);
            var reserved = availability.Allocations.Where(a => a.ReceiptId == source.ReceiptId).Select(a => a.Key).Distinct()
                .Sum(key => availability.Quotas.GetValueOrDefault(key)?.ReservedPortions ?? 0);
            if (reserved > 0)
                return Conflict(new { code = "PORTIONS_RESERVED", message = $"Còn {reserved:0.####} suất đang giữ chỗ. Hoàn tất phục vụ hoặc hủy món chưa nấu trước khi đối soát.", reservedPortions = reserved });
            var usage = await CalculateServedUsage(usageDate);
            if (usage.Errors.Count > 0)
                return Conflict(new { message = string.Join(" ", usage.Errors) });

            var inputs = request.Lines.ToDictionary(line => line.SourceDetailId);
            foreach (var detail in details)
            {
                var input = inputs[detail.DetailId];
                if (!usage.ByDetail.TryGetValue(detail.DetailId, out var usedQuantity))
                    return Conflict(new { message = "Phieu xuat khong nam trong ngay phuc vu; vui long tai lai bao cao." });
                if (input.ReturnedQuantity < 0 || input.WastedQuantity < 0
                    || usedQuantity + input.ReturnedQuantity + input.WastedQuantity != detail.Quantity
                    || decimal.Round(input.ReturnedQuantity, 4) != input.ReturnedQuantity
                    || decimal.Round(input.WastedQuantity, 4) != input.WastedQuantity)
                    return Conflict(new { message = $"Số suất đã phục vụ hoặc lượng hoàn/hủy đã thay đổi cho {detail.InventoryItem.ItemName}. Tải lại và cân bằng phiếu." });
                if (input.WastedQuantity > 0 && string.IsNullOrWhiteSpace(input.WasteReason))
                    return BadRequest(new { message = $"Can ghi ly do huy: {detail.InventoryItem.ItemName}." });
                if (input.WasteReason?.Length > 500)
                    return BadRequest(new { message = "Ly do huy khong duoc qua 500 ky tu." });
                if (input.ReturnedQuantity > 0 && (detail.Batch!.Status == "expired"
                    || detail.Batch.ExpiryDate.Date < DateTime.UtcNow.AddHours(7).Date.AddDays(1)))
                    return BadRequest(new { message = $"Lo {detail.Batch.BatchCode} khong con han den ngay mai; hay xu ly phan du tai bep, khong hoan kho." });
            }

            var now = DateTime.UtcNow;
            var returnDeadline = now.AddHours(7).Date.AddDays(1);
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            var returns = request.Lines.Any(line => line.ReturnedQuantity > 0) ? new StockReceipt
            {
                ReceiptCode = $"TH-{now:yyyyMMdd}-{suffix}", ReceiptType = "return_from_kitchen", Status = "approved",
                Note = $"Hoan tu {source.ReceiptCode}. {request.Note}".Trim(), CreatedBy = userId,
                ApprovedBy = userId, ApprovedAt = now, CreatedAt = now, UpdatedAt = now
            } : null;
            var waste = request.Lines.Any(line => line.WastedQuantity > 0) ? new StockReceipt
            {
                ReceiptCode = $"XH-BEP-{now:yyyyMMdd}-{suffix}", ReceiptType = "waste_disposal", Status = "approved",
                Note = $"Huy tai bep tu {source.ReceiptCode}. {request.Note}".Trim(), CreatedBy = userId,
                ApprovedBy = userId, ApprovedAt = now, CreatedAt = now, UpdatedAt = now
            } : null;
            if (returns != null) _db.StockReceipts.Add(returns);
            if (waste != null) _db.StockReceipts.Add(waste);
            if (returns != null || waste != null) await _db.SaveChangesAsync();

            foreach (var detail in details)
            {
                var input = inputs[detail.DetailId];
                var batch = detail.Batch!;
                if (input.ReturnedQuantity > 0)
                {
                    var returnBatch = new InventoryBatch
                    {
                        InventoryItemId = detail.InventoryItemId,
                        BatchCode = $"TH-BEP-{now:yyyyMMdd}-{detail.DetailId}-{Guid.NewGuid().ToString("N")[..8]}",
                        QuantityRemaining = input.ReturnedQuantity,
                        OriginalQuantity = input.ReturnedQuantity,
                        UnitCost = detail.UnitCost,
                        ManufacturingDate = batch.ManufacturingDate,
                        ExpiryDate = batch.ExpiryDate.Date < returnDeadline ? batch.ExpiryDate.Date : returnDeadline,
                        ReceivedDate = now,
                        SupplierId = batch.SupplierId,
                        Status = "active",
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    _db.InventoryBatches.Add(returnBatch);
                    await _db.SaveChangesAsync();
                    detail.InventoryItem.CurrentQuantity += input.ReturnedQuantity;
                    var amount = InventoryCostHelper.CalculateAmount(input.ReturnedQuantity, detail.UnitCost, detail.InventoryItem.ConversionRate);
                    returns!.TotalAmount += amount;
                    returns.StockReceiptDetails.Add(new StockReceiptDetail
                    {
                        InventoryItemId = detail.InventoryItemId, BatchId = returnBatch.BatchId, Quantity = input.ReturnedQuantity,
                        UnitCost = detail.UnitCost, Subtotal = amount, Note = $"Hoan tu phieu {source.ReceiptCode}, dong {detail.DetailId}"
                    });
                    _db.StockMovements.Add(new StockMovement
                    {
                        InventoryItemId = detail.InventoryItemId, BatchId = returnBatch.BatchId, MovementType = "import",
                        Quantity = input.ReturnedQuantity, UnitCost = detail.UnitCost,
                        Reason = $"Hoan bep tu phieu {source.ReceiptCode}, dong {detail.DetailId}",
                        ReferenceType = "stock_receipt", ReferenceId = returns.ReceiptId,
                        CreatedBy = userId, CreatedAt = now
                    });
                }
                if (input.WastedQuantity > 0)
                {
                    var amount = InventoryCostHelper.CalculateAmount(input.WastedQuantity, detail.UnitCost, detail.InventoryItem.ConversionRate);
                    waste!.TotalAmount += amount;
                    waste.StockReceiptDetails.Add(new StockReceiptDetail
                    {
                        InventoryItemId = detail.InventoryItemId, BatchId = detail.BatchId, Quantity = input.WastedQuantity,
                        UnitCost = detail.UnitCost, Subtotal = amount,
                        Note = $"Huy tai bep tu phieu {source.ReceiptCode}, dong {detail.DetailId}: {input.WasteReason!.Trim()}"
                    });
                }
                _db.KitchenReconciliationLines.Add(new KitchenReconciliationLine
                {
                    SourceDetailId = detail.DetailId, IssuedQuantity = detail.Quantity,
                    UsedQuantity = usage.ByDetail[detail.DetailId],
                    ReturnedQuantity = input.ReturnedQuantity, WastedQuantity = input.WastedQuantity,
                    WasteReason = input.WastedQuantity > 0 ? input.WasteReason!.Trim() : null,
                    ReturnReceiptId = input.ReturnedQuantity > 0 ? returns?.ReceiptId : null,
                    WasteReceiptId = input.WastedQuantity > 0 ? waste?.ReceiptId : null,
                    ReconciledBy = userId, ReconciledAt = now
                });
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            if (_hub != null)
            {
                try {
                    await _hub.PublishAsync(_db, "PortionAvailabilityChanged", new { eventType = "reconciled", source.ReceiptId });
                    await _hub.PublishAsync(_db, "KitchenUpdated", new { eventType = "reconciled", source.ReceiptId });
                } catch { }
            }
            return Ok(new { success = true, returnReceiptId = returns?.ReceiptId, wasteReceiptId = waste?.ReceiptId });
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return Conflict(new { message = "Du lieu doi soat da thay doi. Tai lai va thu tiep." });
        }
    }

    private sealed record UsageBreakdown(string FoodName, string VariantName, decimal SoldPortions,
        decimal QuantityPerUnit, decimal RequiredQuantity);

    private sealed class ServedUsage
    {
        public Dictionary<int, decimal> ByDetail { get; } = new();
        public Dictionary<int, List<UsageBreakdown>> BreakdownByItem { get; } = new();
        public List<string> Errors { get; } = new();
    }

    private static DateTime GetUsageDate(StockReceipt receipt)
    {
        var offset = receipt.ReceiptCode.StartsWith("PXK-BS-", StringComparison.Ordinal) ? 7
            : receipt.ReceiptCode.StartsWith("PXK-", StringComparison.Ordinal) ? 4 : -1;
        if (offset >= 0 && receipt.ReceiptCode.Length >= offset + 8
            && DateTime.TryParseExact(receipt.ReceiptCode.Substring(offset, 8), "yyyyMMdd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date.Date;
        return (receipt.ApprovedAt ?? receipt.CreatedAt).AddHours(7).Date;
    }

    private async Task<ServedUsage> CalculateServedUsage(DateTime localDate)
    {
        var result = new ServedUsage();
        var fromUtc = localDate.AddHours(-7);
        var toUtc = fromUtc.AddDays(1);
        var initialPrefix = $"PXK-{localDate:yyyyMMdd}-";
        var additionalPrefix = $"PXK-BS-{localDate:yyyyMMdd}-";
        var receipts = await _db.StockReceipts.AsNoTracking()
            .Where(r => r.ReceiptType == "export_kitchen" && r.Status == "approved"
                && (r.ReceiptCode.StartsWith(initialPrefix) || r.ReceiptCode.StartsWith(additionalPrefix)
                    || (r.ApprovedAt >= fromUtc && r.ApprovedAt < toUtc)))
            .Include(r => r.StockReceiptDetails)
            .OrderBy(r => r.ApprovedAt).ThenBy(r => r.ReceiptId)
            .ToListAsync();
        var portions = await _db.DailyPortionControls.AsNoTracking()
            .Where(p => p.ControlDate == localDate && p.SoldPortions > 0)
            .Include(p => p.Food).Include(p => p.Variant)
            .ToListAsync();
        var foodIds = portions.Select(p => p.FoodId).Distinct().ToList();
        var boms = await _db.FoodIngredients.AsNoTracking()
            .Where(bom => foodIds.Contains(bom.FoodId))
            .ToListAsync();

        var requiredByItem = new Dictionary<int, decimal>();
        foreach (var portion in portions)
        {
            var recipe = boms.Where(bom => bom.FoodId == portion.FoodId
                && bom.VariantId == portion.VariantId).ToList();
            if (recipe.Count == 0 && portion.VariantId.HasValue)
                recipe = boms.Where(bom => bom.FoodId == portion.FoodId && bom.VariantId == null).ToList();
            if (recipe.Count == 0)
            {
                result.Errors.Add($"{portion.Food.FoodName} đã phục vụ nhưng chưa có BOM cho biến thể; không thể tính nguyên liệu đã dùng.");
                continue;
            }

            foreach (var bom in recipe)
            {
                var required = decimal.Round(portion.SoldPortions * bom.QuantityPerUnit, 4,
                    MidpointRounding.AwayFromZero);
                requiredByItem[bom.InventoryItemId] = requiredByItem.GetValueOrDefault(bom.InventoryItemId) + required;
                if (!result.BreakdownByItem.TryGetValue(bom.InventoryItemId, out var breakdown))
                {
                    breakdown = new List<UsageBreakdown>();
                    result.BreakdownByItem[bom.InventoryItemId] = breakdown;
                }
                breakdown.Add(new UsageBreakdown(portion.Food.FoodName,
                    portion.Variant?.VariantName ?? "Mặc định", portion.SoldPortions,
                    bom.QuantityPerUnit, required));
            }
        }

        var details = receipts.SelectMany(receipt => receipt.StockReceiptDetails.OrderBy(detail => detail.DetailId))
            .ToList();
        var detailIds = details.Select(detail => detail.DetailId).ToList();
        var closed = await _db.KitchenReconciliationLines.AsNoTracking()
            .Where(line => detailIds.Contains(line.SourceDetailId))
            .ToDictionaryAsync(line => line.SourceDetailId);

        // New receipts carry the exact BOM context used when they were created.
        // Allocate sold portions independently per ingredient so a snapshot on the
        // meat line cannot consume the portions available to the vegetable line.
        var remainingSoldByIngredientDish = new Dictionary<string, decimal>();
        var snapshotRequiredByItem = new Dictionary<int, decimal>();
        foreach (var detail in details)
        {
            if (string.IsNullOrWhiteSpace(detail.BomSnapshotJson)) continue;

            List<StockReceiptBomSnapshotEntry>? snapshot;
            try
            {
                snapshot = JsonSerializer.Deserialize<List<StockReceiptBomSnapshotEntry>>(detail.BomSnapshotJson);
            }
            catch (JsonException)
            {
                result.Errors.Add($"Dòng phiếu #{detail.DetailId} có snapshot BOM không hợp lệ; không thể đối soát tự động.");
                continue;
            }

            if (snapshot == null) continue;
            foreach (var entry in snapshot)
            {
                var dishKey = $"{detail.InventoryItemId}:{entry.FoodId}:{entry.VariantId?.ToString() ?? "default"}";
                if (!remainingSoldByIngredientDish.TryGetValue(dishKey, out var remainingSold))
                {
                    var portion = portions.FirstOrDefault(p => p.FoodId == entry.FoodId && p.VariantId == entry.VariantId)
                        ?? (entry.VariantId.HasValue
                            ? portions.FirstOrDefault(p => p.FoodId == entry.FoodId && p.VariantId == null)
                            : null);
                    remainingSold = portion?.SoldPortions ?? 0m;
                }

                var allocatedPortions = Math.Min(Math.Max(remainingSold, 0m), Math.Max(entry.Portions, 0m));
                remainingSoldByIngredientDish[dishKey] = remainingSold - allocatedPortions;
                var required = decimal.Round(allocatedPortions * entry.QuantityPerUnit, 4,
                    MidpointRounding.AwayFromZero);
                snapshotRequiredByItem[detail.InventoryItemId] =
                    snapshotRequiredByItem.GetValueOrDefault(detail.InventoryItemId) + required;

                if (allocatedPortions > 0)
                {
                    if (!result.BreakdownByItem.TryGetValue(detail.InventoryItemId, out var breakdown))
                    {
                        breakdown = new List<UsageBreakdown>();
                        result.BreakdownByItem[detail.InventoryItemId] = breakdown;
                    }
                    breakdown.Add(new UsageBreakdown(entry.FoodName, entry.VariantName,
                        allocatedPortions, entry.QuantityPerUnit, required));
                }
            }
        }

        foreach (var (itemId, amount) in snapshotRequiredByItem)
        {
            requiredByItem[itemId] = amount;
        }

        var remainingByItem = new Dictionary<int, decimal>(requiredByItem);
        foreach (var detail in details.Where(detail => closed.ContainsKey(detail.DetailId)))
        {
            var used = closed[detail.DetailId].UsedQuantity;
            result.ByDetail[detail.DetailId] = used;
            remainingByItem[detail.InventoryItemId] = remainingByItem.GetValueOrDefault(detail.InventoryItemId) - used;
        }
        foreach (var detail in details.Where(detail => !closed.ContainsKey(detail.DetailId)))
        {
            var remaining = remainingByItem.GetValueOrDefault(detail.InventoryItemId);
            var used = Math.Min(Math.Max(remaining, 0m), detail.Quantity);
            result.ByDetail[detail.DetailId] = used;
            remainingByItem[detail.InventoryItemId] = remaining - used;
        }
        foreach (var (itemId, remaining) in remainingByItem)
        {
            if (remaining > 0)
                result.Errors.Add($"Nguyên liệu #{itemId} thiếu {remaining:0.####} theo số suất đã phục vụ; cần duyệt phiếu xuất bổ sung.");
            else if (remaining < 0)
                result.Errors.Add($"Nguyên liệu #{itemId} đã chốt dùng nhiều hơn số suất hiện đã phục vụ; kiểm tra món hoàn tác hoặc BOM đã sửa.");
        }
        return result;
    }
}
