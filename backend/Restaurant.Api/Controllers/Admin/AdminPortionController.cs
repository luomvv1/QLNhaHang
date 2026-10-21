using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;
using Restaurant.Api.Hubs;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Restaurant.Api.Controllers.Admin;

// ==============================================================
// DTOs CHO HẠNG MỤC 2: KẾ HOẠCH & ĐIỀU PHỐI SUẤT ĂN (DAILY PORTIONS)
// ==============================================================

public class PlanPortionItemInput
{
    public int FoodId { get; set; }
    public int? VariantId { get; set; }
    public decimal PlannedPortions { get; set; }
    public decimal CarryOverPortions { get; set; } = 0;
    public string? Note { get; set; }
}

public class BatchPlanPortionsRequest
{
    public DateTime? ControlDate { get; set; }
    public List<PlanPortionItemInput> Items { get; set; } = new();
}

public class UpdateCookedPortionsRequest
{
    public decimal CookedPortions { get; set; }
}

public class UpdatePortionItemRequest
{
    public decimal? PlannedPortions { get; set; }
    public decimal? CarryOverPortions { get; set; }
    public decimal? CookedPortions { get; set; }
    public string? Status { get; set; }
    public string? Note { get; set; }
}

public class GenerateRequisitionRequest
{
    public DateTime? ControlDate { get; set; }
    public string? Note { get; set; }
}

public class AdditionalRequisitionItemInput
{
    public int FoodId { get; set; }
    public int? VariantId { get; set; }
    public decimal AdditionalPortions { get; set; }
}

public class AdditionalRequisitionRequest
{
    public DateTime? ControlDate { get; set; }
    public string? Note { get; set; }
    public List<AdditionalRequisitionItemInput> Items { get; set; } = new();
}

public class RequisitionBomBreakdown
{
    public string DishName { get; set; } = string.Empty;
    public decimal Portions { get; set; }
    public decimal QuantityPerUnit { get; set; }
    public decimal WastagePercent { get; set; }
    public decimal QuantityRequired { get; set; }
}

public class RequisitionIngredientAggregate
{
    public InventoryItem Item { get; set; } = null!;
    public decimal TotalQuantity { get; set; }
    public List<string> ForDishes { get; } = new();
    public List<RequisitionBomBreakdown> BomBreakdown { get; } = new();
    public List<StockReceiptBomSnapshotEntry> BomSnapshots { get; } = new();
}

public class AdditionalPortionLine
{
    public string Label { get; set; } = string.Empty;
    public decimal Portions { get; set; }
}

internal sealed class BatchAvailability
{
    public decimal AvailableQuantity { get; set; }
    public decimal ExpiredQuantity { get; set; }
}

// ==============================================================
// CONTROLLER: ĐIỀU PHỐI SUẤT ĂN & AUTO-BOM XUẤT BẾP (HẠNG MỤC 2)
// ==============================================================

[Route("api/admin/inventory/daily-portions")]
[Authorize(Roles = "admin,manager,chef,staff")]
[ApiController]
public class AdminPortionController : ControllerBase
{
    private readonly RestaurantDbContext _dbContext;
    private readonly IHubContext<RestaurantHub> _hubContext;

    public AdminPortionController(RestaurantDbContext dbContext, IHubContext<RestaurantHub> hubContext)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    private int GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out var userId) ? userId : 1;
    }

    private static bool CanUseWastage(string? baseUnit)
    {
        return !string.IsNullOrWhiteSpace(baseUnit);
    }

    private async Task<Dictionary<int, BatchAvailability>> GetBatchAvailability(
        IEnumerable<int> inventoryItemIds,
        DateTime asOfDate)
    {
        var ids = inventoryItemIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, BatchAvailability>();

        var batches = await _dbContext.InventoryBatches
            .AsNoTracking()
            .Where(batch => ids.Contains(batch.InventoryItemId) && batch.QuantityRemaining > 0)
            .Select(batch => new
            {
                batch.InventoryItemId,
                batch.QuantityRemaining,
                batch.ExpiryDate,
                batch.Status
            })
            .ToListAsync();

        var date = asOfDate.Date;
        return batches
            .GroupBy(batch => batch.InventoryItemId)
            .ToDictionary(
                group => group.Key,
                group => new BatchAvailability
                {
                    AvailableQuantity = group
                        .Where(batch => batch.Status == "active" && batch.ExpiryDate.Date >= date)
                        .Sum(batch => batch.QuantityRemaining),
                    ExpiredQuantity = group
                        .Where(batch => batch.Status == "expired" || batch.ExpiryDate.Date < date)
                        .Sum(batch => batch.QuantityRemaining)
                });
    }

    private static List<AdditionalPortionLine> ParseAdditionalPortionLines(string? note)
    {
        if (string.IsNullOrWhiteSpace(note)) return new List<AdditionalPortionLine>();

        var lines = new List<AdditionalPortionLine>();
        foreach (var rawSegment in note.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var match = Regex.Match(rawSegment, @"^(?<label>.+?)\s*\[(?<portions>[\d.,]+)\s+suất bổ sung\]$", RegexOptions.IgnoreCase);
            if (!match.Success) continue;

            var rawPortions = match.Groups["portions"].Value.Replace(',', '.');
            if (!decimal.TryParse(rawPortions, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var portions))
                continue;

            lines.Add(new AdditionalPortionLine
            {
                Label = match.Groups["label"].Value.Trim(),
                Portions = portions
            });
        }

        return lines;
    }

    // ==============================================================
    // 1. LẤY BẢNG ĐIỀU PHỐI SUẤT ĂN TRONG NGÀY
    // ==============================================================

    [HttpGet]
    public async Task<IActionResult> GetDailyPortions(
        [FromQuery] string? date = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] string? search = null)
    {
        var targetDate = DateTime.TryParse(date, out var parsedDate)
            ? parsedDate.Date
            : DateTime.UtcNow.Date;

        var query = _dbContext.DailyPortionControls
            .Include(p => p.Food).ThenInclude(f => f.Category)
            .Include(p => p.Variant)
            .Where(p => p.ControlDate == targetDate)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.Food.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p => p.Food.FoodName.ToLower().Contains(s) ||
                                     (p.Variant != null && p.Variant.VariantName.ToLower().Contains(s)));
        }

        var items = await query
            .OrderBy(p => p.Food.Category.SortOrder)
            .ThenBy(p => p.Food.FoodName)
            .ThenBy(p => p.Variant != null ? p.Variant.SortOrder : 0)
            .ToListAsync();

        // Kiểm tra xem đã có phiếu xuất bếp (export_kitchen) cho ngày này chưa
        string datePrefix = $"PXK-{targetDate:yyyyMMdd}";
        var initialReceipt = await _dbContext.StockReceipts
            .Where(r => r.ReceiptType == "export_kitchen" &&
                        r.ReceiptCode.StartsWith(datePrefix) &&
                        !r.ReceiptCode.StartsWith($"PXK-BS-{targetDate:yyyyMMdd}") &&
                        r.Status != "cancelled")
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.ReceiptId,
                r.ReceiptCode,
                r.Status,
                r.CreatedAt,
                r.ApprovedAt
            })
            .FirstOrDefaultAsync();

        var additionalReceipts = await _dbContext.StockReceipts
            .Where(r => r.ReceiptType == "export_kitchen" &&
                        r.ReceiptCode.StartsWith($"PXK-BS-{targetDate:yyyyMMdd}") &&
                        r.Status != "cancelled")
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.ReceiptId,
                r.ReceiptCode,
                r.Status,
                r.CreatedAt,
                r.ApprovedAt
            })
            .ToListAsync();

        var additionalReceiptIds = additionalReceipts.Select(r => r.ReceiptId).ToList();
        var additionalReceiptDetails = await _dbContext.StockReceiptDetails
            .Where(d => additionalReceiptIds.Contains(d.ReceiptId))
            .Include(d => d.InventoryItem)
            .Select(d => new
            {
                d.ReceiptId,
                d.InventoryItemId,
                ItemName = d.InventoryItem.ItemName,
                BaseUnit = d.InventoryItem.BaseUnit,
                d.Quantity,
                d.Note
            })
            .ToListAsync();

        var receiptStatusById = additionalReceipts.ToDictionary(r => r.ReceiptId, r => r.Status);
        var additionalAllocations = additionalReceiptDetails
            .SelectMany(detail => ParseAdditionalPortionLines(detail.Note)
                .Select(line => new
                {
                    detail.ReceiptId,
                    detail.InventoryItemId,
                    detail.ItemName,
                    detail.BaseUnit,
                    detail.Quantity,
                    detail.Note,
                    line.Label,
                    line.Portions,
                    Status = receiptStatusById[detail.ReceiptId]
                }))
            .ToList();

        var result = items.Select(p =>
        {
            var dishLabel = p.Variant != null
                ? $"{p.Food.FoodName} ({p.Variant.VariantName})"
                : p.Food.FoodName;

            var dishAllocations = additionalAllocations
                .Where(x => x.Label.StartsWith(dishLabel, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var initialPlannedPortions = Math.Max(p.PlannedPortions, 0);
            var additionalApprovedPortions = dishAllocations
                .Where(x => x.Status == "approved")
                .Sum(x => x.Portions);
            var pendingAdditionalPortions = dishAllocations
                .Where(x => x.Status == "draft")
                .Sum(x => x.Portions);
            var plannedPortions = initialPlannedPortions + additionalApprovedPortions;

            return new
            {
                p.PortionControlId,
                ControlDate = p.ControlDate.ToString("yyyy-MM-dd"),
                p.FoodId,
                FoodName = p.Food.FoodName,
                CategoryId = p.Food.CategoryId,
                CategoryName = p.Food.Category.CategoryName,
                p.VariantId,
                VariantName = p.Variant != null ? p.Variant.VariantName : "Mặc định",
                Price = p.Variant != null ? p.Variant.Price : p.Food.MinPrice,
                Unit = p.Variant != null ? p.Variant.Unit : "Phần",
                InitialPlannedPortions = initialPlannedPortions,
                AdditionalApprovedPortions = additionalApprovedPortions,
                PendingAdditionalPortions = pendingAdditionalPortions,
                PlannedPortions = plannedPortions,
                RemainingPortions = plannedPortions - p.SoldPortions,
                p.CarryOverPortions,
                NeedToCookPortions = plannedPortions,
                p.CookedPortions,
                p.SoldPortions,
                p.SystemRemain, // Computed column: CookedPortions - SoldPortions
                p.ActualRemain,
                p.VariancePortions, // Computed column: ActualRemain - SystemRemain
                p.DisposedPortions,
                p.NextCarryOverPortions,
                p.Status, // planned, in_service, reconciled, closed
                p.Note,
                p.CreatedAt,
                p.UpdatedAt
            };
        }).ToList();

        // Thống kê tổng hợp dùng đúng kế hoạch đã duyệt, bao gồm cả phiếu bổ sung đã duyệt.
        var summary = new
        {
            Date = targetDate.ToString("yyyy-MM-dd"),
            TotalDishes = result.Count,
            TotalPlanned = result.Sum(i => i.PlannedPortions),
            TotalCarryOver = result.Sum(i => i.CarryOverPortions),
            TotalNeedToCook = result.Sum(i => Math.Max(i.PlannedPortions, 0)),
            TotalCooked = result.Sum(i => i.CookedPortions),
            TotalSold = result.Sum(i => i.SoldPortions),
            TotalSystemRemain = result.Sum(i => i.SystemRemain),
            TotalDisposed = result.Sum(i => i.DisposedPortions),
            TotalNextCarryOver = result.Sum(i => i.NextCarryOverPortions)
        };

        var additionalReceiptViews = additionalReceipts
            .Select(receipt => new
            {
                receipt.ReceiptId,
                receipt.ReceiptCode,
                receipt.Status,
                receipt.CreatedAt,
                receipt.ApprovedAt,
                Items = additionalReceiptDetails
                    .Where(detail => detail.ReceiptId == receipt.ReceiptId)
                    .GroupBy(detail => new
                    {
                        detail.InventoryItemId,
                        detail.ItemName,
                        detail.BaseUnit
                    })
                    .Select(group => new
                    {
                        group.Key.InventoryItemId,
                        group.Key.ItemName,
                        group.Key.BaseUnit,
                        Quantity = group.Sum(detail => detail.Quantity),
                        Note = string.Join("; ", group
                            .Select(detail => detail.Note)
                            .Where(note => !string.IsNullOrWhiteSpace(note) && !note.StartsWith("Phân bổ FEFO", StringComparison.OrdinalIgnoreCase))),
                        PortionLines = group
                            .SelectMany(detail => ParseAdditionalPortionLines(detail.Note))
                            .GroupBy(line => new { line.Label, line.Portions })
                            .Select(line => new AdditionalPortionLine { Label = line.Key.Label, Portions = line.Key.Portions })
                            .ToList()
                    })
                    .ToList()
            })
            .ToList();

        var initialReceiptStatus = initialReceipt?.Status;
        var hasRequisition = initialReceiptStatus != null;
        var canGenerateAdditionalRequisition = initialReceiptStatus == "approved";

        return Ok(new
        {
            success = true,
            summary,
            hasRequisition,
            initialReceipt,
            initialReceiptStatus,
            canGenerateAdditionalRequisition,
            additionalReceipts = additionalReceiptViews,
            data = result
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPortionById(int id)
    {
        var portion = await _dbContext.DailyPortionControls
            .Include(p => p.Food).ThenInclude(f => f.Category)
            .Include(p => p.Variant)
            .FirstOrDefaultAsync(p => p.PortionControlId == id);

        if (portion == null)
            return NotFound(new { success = false, message = "Không tìm thấy bản ghi suất ăn này." });

        // Lấy danh sách nguyên liệu định mức BOM tương ứng
        var bomQuery = _dbContext.FoodIngredients
            .Include(fi => fi.InventoryItem)
            .Where(fi => fi.FoodId == portion.FoodId);

        if (portion.VariantId.HasValue)
        {
            bomQuery = bomQuery.Where(fi => fi.VariantId == portion.VariantId.Value);
        }
        else
        {
            bomQuery = bomQuery.Where(fi => fi.VariantId == null);
        }

        var boms = await bomQuery.ToListAsync();
        var availability = await GetBatchAvailability(
            boms.Select(bom => bom.InventoryItemId),
            portion.ControlDate);

        decimal needToCook = Math.Max(portion.PlannedPortions, 0);

        var ingredientsRequired = boms.Select(fi => new
        {
            fi.InventoryItemId,
            fi.InventoryItem.ItemName,
            BaseUnit = fi.InventoryItem.BaseUnit,
            PurchaseUnit = fi.InventoryItem.PurchaseUnit,
            ConversionRate = fi.InventoryItem.ConversionRate,
            fi.QuantityPerUnit,
            WastagePercent = CanUseWastage(fi.InventoryItem.BaseUnit) ? fi.WastagePercent : 0m,
            fi.InventoryItem.CurrentQuantity,
            AvailableQuantity = availability.GetValueOrDefault(fi.InventoryItemId)?.AvailableQuantity ?? 0m,
            ExpiredQuantity = availability.GetValueOrDefault(fi.InventoryItemId)?.ExpiredQuantity ?? 0m,
            MinimumQuantity = fi.InventoryItem.MinimumQuantity,
            fi.InventoryItem.CostPerUnit,
            TotalRequired = needToCook * fi.QuantityPerUnit,
            EstimatedLineCost = InventoryCostHelper.CalculateAmount(
                needToCook * fi.QuantityPerUnit,
                fi.InventoryItem.CostPerUnit,
                fi.InventoryItem.ConversionRate)
        }).ToList();

        return Ok(new
        {
            success = true,
            data = new
            {
                portion.PortionControlId,
                ControlDate = portion.ControlDate.ToString("yyyy-MM-dd"),
                portion.FoodId,
                FoodName = portion.Food.FoodName,
                portion.VariantId,
                VariantName = portion.Variant != null ? portion.Variant.VariantName : "Mặc định",
                portion.PlannedPortions,
                portion.CarryOverPortions,
                NeedToCookPortions = needToCook,
                portion.CookedPortions,
                portion.SoldPortions,
                portion.SystemRemain,
                portion.ActualRemain,
                portion.VariancePortions,
                portion.DisposedPortions,
                portion.NextCarryOverPortions,
                portion.Status,
                portion.Note,
                BOM = ingredientsRequired
            }
        });
    }

    // ==============================================================
    // 2. LẬP KẾ HOẠCH SUẤT ĂN HÀNG LOẠT ĐẦU NGÀY (BATCH PLAN)
    // ==============================================================

    [Authorize(Roles = "admin,manager,chef")]
    [HttpPost("batch-plan")]
    public async Task<IActionResult> BatchPlanPortions([FromBody] BatchPlanPortionsRequest request)
    {
        if (request == null || request.Items == null || request.Items.Count == 0)
            return BadRequest(new { success = false, message = "Danh sách kế hoạch suất ăn không được để trống." });

        var targetDate = (request.ControlDate ?? DateTime.UtcNow).Date;
        var now = DateTime.UtcNow;

        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var savedCount = 0;

            foreach (var input in request.Items)
            {
                if (input.PlannedPortions < 0 || input.CarryOverPortions < 0)
                    return BadRequest(new { success = false, message = "Số suất dự kiến hoặc tồn chuyển sang không được âm." });

                // Kiểm tra món ăn
                var food = await _dbContext.Foods.FirstOrDefaultAsync(f => f.FoodId == input.FoodId && f.IsActive);
                if (food == null)
                    continue;

                // Kiểm tra variant
                if (input.VariantId.HasValue)
                {
                    var variant = await _dbContext.FoodVariants
                        .FirstOrDefaultAsync(v => v.VariantId == input.VariantId.Value && v.FoodId == input.FoodId);
                    if (variant == null)
                        return BadRequest(new { success = false, message = $"Biến thể #{input.VariantId} không thuộc món ăn '{food.FoodName}'." });
                }

                // Tìm bản ghi đã tồn tại cho ngày + món + variant
                var existing = await _dbContext.DailyPortionControls
                    .FirstOrDefaultAsync(p => p.ControlDate == targetDate &&
                                              p.FoodId == input.FoodId &&
                                              p.VariantId == input.VariantId);

                if (existing != null)
                {
                    existing.PlannedPortions = input.PlannedPortions;
                    existing.CarryOverPortions = input.CarryOverPortions;
                    if (!string.IsNullOrWhiteSpace(input.Note)) existing.Note = input.Note.Trim();
                    existing.UpdatedAt = now;
                }
                else
                {
                    var newPortion = new DailyPortionControl
                    {
                        ControlDate = targetDate,
                        FoodId = input.FoodId,
                        VariantId = input.VariantId,
                        PlannedPortions = input.PlannedPortions,
                        CarryOverPortions = input.CarryOverPortions,
                        CookedPortions = 0,
                        SoldPortions = 0,
                        DisposedPortions = 0,
                        NextCarryOverPortions = 0,
                        Status = "planned",
                        Note = input.Note?.Trim(),
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    _dbContext.DailyPortionControls.Add(newPortion);
                }

                savedCount++;
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            try
            {
                await _hubContext.Clients.All.SendAsync("KitchenUpdated", new { eventType = "daily_portions_planned", date = targetDate });
            }
            catch { }

            return Ok(new
            {
                success = true,
                message = $"Đã lưu kế hoạch suất ăn cho {savedCount} món ngày {targetDate:yyyy-MM-dd} thành công.",
                savedCount
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { success = false, message = "Lỗi khi lập kế hoạch suất ăn: " + ex.Message });
        }
    }

    // ==============================================================
    // 3. TỰ ĐỘNG NHÂN BOM & LẬP PHIẾU XUẤT BẾP (AUTO-REQUISITION)
    // ==============================================================

    [Authorize(Roles = "admin,manager,chef")]
    [HttpPost("generate-requisition")]
    public async Task<IActionResult> GenerateRequisition([FromBody] GenerateRequisitionRequest request)
    {
        var targetDate = (request?.ControlDate ?? DateTime.UtcNow).Date;
        var now = DateTime.UtcNow;
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(
            now,
            TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows()
                ? "SE Asia Standard Time"
                : "Asia/Ho_Chi_Minh"));
        var currentUserId = GetCurrentUserId();

        // Serialize the daily uniqueness check with the insert below. Without this,
        // two operators clicking at the same time could both create a requisition.
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);
        try
        {

        var datePrefix = $"PXK-{targetDate:yyyyMMdd}";
        var existingRequisition = await _dbContext.StockReceipts
            .Where(r => r.ReceiptType == "export_kitchen" &&
                        r.ReceiptCode.StartsWith(datePrefix) &&
                        r.Status != "cancelled")
            .Select(r => new { r.ReceiptCode, r.Status })
            .FirstOrDefaultAsync();

        if (existingRequisition != null)
        {
            return Conflict(new
            {
                success = false,
                message = $"Ngày {targetDate:yyyy-MM-dd} đã có phiếu xuất bếp '{existingRequisition.ReceiptCode}' ở trạng thái '{existingRequisition.Status}'. Hãy kiểm tra hoặc hủy phiếu cũ trước khi lập lại.",
                receiptCode = existingRequisition.ReceiptCode,
                status = existingRequisition.Status
            });
        }

        // 1. Lấy tất cả các suất ăn đã lên kế hoạch trong ngày
        var portions = await _dbContext.DailyPortionControls
            .Include(p => p.Food)
            .Include(p => p.Variant)
            .Where(p => p.ControlDate == targetDate)
            .ToListAsync();

        if (portions.Count == 0)
        {
            return BadRequest(new
            {
                success = false,
                message = $"Chưa có kế hoạch suất ăn cho ngày {targetDate:yyyy-MM-dd}. Vui lòng nhập kế hoạch suất trước."
            });
        }

        // 2. Nhà hàng nấu theo order: số lượng xuất được tính trực tiếp từ số suất kế hoạch.
        // Không trừ carry-over món chín ở bước này vì bếp không chuẩn bị món chín trước.
        var neededPortions = portions
            .Where(p => p.PlannedPortions > 0)
            .ToList();

        if (neededPortions.Count == 0)
        {
            return BadRequest(new
            {
                success = false,
                message = "Các món trong kế hoạch chưa có số suất lớn hơn 0. Không có nguyên liệu để lập phiếu xuất."
            });
        }

        // 3. Lấy định mức BOM của tất cả các món liên quan
        var foodIds = neededPortions.Select(p => p.FoodId).Distinct().ToList();
        var allBoms = await _dbContext.FoodIngredients
            .Include(fi => fi.InventoryItem)
            .Where(fi => foodIds.Contains(fi.FoodId))
            .ToListAsync();

        // Bảng gom nguyên liệu cần xuất tổng cộng theo InventoryItemId
        var aggregatedIngredients = new Dictionary<int, RequisitionIngredientAggregate>();

        foreach (var portion in neededPortions)
        {
            decimal newCookedPlan = Math.Max(portion.PlannedPortions, 0);

            // Tìm BOM theo variant trước
            var bomItems = allBoms.Where(fi => fi.FoodId == portion.FoodId && fi.VariantId == portion.VariantId).ToList();

            // Nếu không có BOM riêng cho variant, fallback về BOM mặc định (VariantId == null)
            if (bomItems.Count == 0)
            {
                bomItems = allBoms.Where(fi => fi.FoodId == portion.FoodId && fi.VariantId == null).ToList();
            }

            string dishDisplay = portion.Variant != null
                ? $"{portion.Food.FoodName} ({portion.Variant.VariantName}) [{newCookedPlan} suất]"
                : $"{portion.Food.FoodName} [{newCookedPlan} suất]";

            foreach (var bi in bomItems)
            {
                // BOM quantities represent usable ingredient; prep loss is accounted for at receiving.
                decimal requiredQuantity = newCookedPlan * bi.QuantityPerUnit;

                if (!aggregatedIngredients.TryGetValue(bi.InventoryItemId, out var current))
                {
                    current = new RequisitionIngredientAggregate
                    {
                        Item = bi.InventoryItem
                    };
                    aggregatedIngredients[bi.InventoryItemId] = current;
                }

                current.TotalQuantity += requiredQuantity;
                if (!current.ForDishes.Contains(dishDisplay))
                {
                    current.ForDishes.Add(dishDisplay);
                }

                current.BomBreakdown.Add(new RequisitionBomBreakdown
                {
                    DishName = dishDisplay,
                    Portions = newCookedPlan,
                    QuantityPerUnit = bi.QuantityPerUnit,
                    WastagePercent = CanUseWastage(bi.InventoryItem.BaseUnit) ? bi.WastagePercent : 0m,
                    QuantityRequired = requiredQuantity
                });
                current.BomSnapshots.Add(new StockReceiptBomSnapshotEntry
                {
                    FoodId = portion.FoodId,
                    VariantId = portion.VariantId,
                    FoodName = portion.Food.FoodName,
                    VariantName = portion.Variant?.VariantName ?? "Mặc định",
                    Portions = newCookedPlan,
                    QuantityPerUnit = bi.QuantityPerUnit,
                    WastagePercent = CanUseWastage(bi.InventoryItem.BaseUnit) ? bi.WastagePercent : 0m
                });
            }
        }

        if (aggregatedIngredients.Count == 0)
        {
            return BadRequest(new
            {
                success = false,
                message = "Các món cần nấu hiện chưa được cài đặt công thức định mức nguyên liệu (BOM). Vui lòng cấu hình BOM trước."
            });
        }

        // Không tạo phiếu nháp chắc chắn không thể duyệt. Người vận hành cần nhập bổ sung,
        // giảm số suất hoặc tạm ngưng món trước khi lập lại phiếu.
        var availability = await GetBatchAvailability(
            aggregatedIngredients.Values.Select(x => x.Item.InventoryItemId),
            targetDate);

        var shortages = aggregatedIngredients.Values
            .Select(x => new
            {
                Aggregate = x,
                Stock = availability.GetValueOrDefault(x.Item.InventoryItemId) ?? new BatchAvailability()
            })
            .Where(x => x.Stock.AvailableQuantity < x.Aggregate.TotalQuantity)
            .Select(x => new
            {
                x.Aggregate.Item.InventoryItemId,
                x.Aggregate.Item.ItemName,
                RequiredQuantity = x.Aggregate.TotalQuantity,
                CurrentQuantity = x.Aggregate.Item.CurrentQuantity,
                AvailableQuantity = x.Stock.AvailableQuantity,
                ExpiredQuantity = x.Stock.ExpiredQuantity,
                ShortageQuantity = x.Aggregate.TotalQuantity - x.Stock.AvailableQuantity,
                ReplenishmentQuantity = x.Aggregate.TotalQuantity + x.Aggregate.Item.MinimumQuantity - x.Stock.AvailableQuantity,
                BaseUnit = x.Aggregate.Item.BaseUnit
            })
            .ToList();

        if (shortages.Count > 0)
        {
            return Conflict(new
            {
                success = false,
                code = "INSUFFICIENT_STOCK",
                message = "Không đủ nguyên liệu để lập phiếu xuất bếp. Hãy nhập bổ sung, giảm số suất hoặc tạm ngưng món thiếu nguyên liệu.",
                shortages
            });
        }

        // 4. Tạo Phiếu yêu cầu xuất kho (StockReceipts: export_kitchen - draft)
            string receiptCode = $"PXK-{targetDate:yyyyMMdd}-{localNow:HHmmssfff}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";

            var receipt = new StockReceipt
            {
                ReceiptCode = receiptCode,
                ReceiptType = "export_kitchen",
                Status = "draft", // Luôn sinh ở trạng thái draft chờ thủ kho duyệt
                TotalAmount = 0,
                Note = !string.IsNullOrWhiteSpace(request?.Note)
                    ? request.Note.Trim()
                    : $"Phiếu yêu cầu xuất kho bếp tự động từ kế hoạch {targetDate:yyyy-MM-dd} ({neededPortions.Count} món)",
                CreatedBy = currentUserId,
                CreatedAt = now,
                UpdatedAt = now
            };

            _dbContext.StockReceipts.Add(receipt);
            await _dbContext.SaveChangesAsync(); // Sinh ReceiptId

            decimal grandTotal = 0;
            var detailsList = new List<object>();

            foreach (var kvp in aggregatedIngredients)
            {
                var item = kvp.Value.Item;
                decimal qty = kvp.Value.TotalQuantity;
                decimal unitCost = item.CostPerUnit;
                decimal subtotal = InventoryCostHelper.CalculateAmount(qty, unitCost, item.ConversionRate);
                grandTotal += subtotal;

                var wastageRates = kvp.Value.BomBreakdown
                    .Where(x => x.WastagePercent > 0)
                    .GroupBy(x => x.WastagePercent)
                    .OrderBy(x => x.Key)
                    .Select(x => x.Key)
                    .ToList();

                var wastageSummary = wastageRates.Count == 0
                    ? "Không áp dụng"
                    : string.Join(" / ", wastageRates.Select(rate => $"{rate:0.##}%"));

                var detail = new StockReceiptDetail
                {
                    ReceiptId = receipt.ReceiptId,
                    InventoryItemId = item.InventoryItemId,
                    BatchId = null, // Sẽ được thuật toán FEFO phân bổ khi thủ kho duyệt phiếu ở Hạng mục 3
                    Quantity = qty,
                    UnitCost = unitCost,
                    Subtotal = subtotal,
                    Note = string.Join("; ", kvp.Value.ForDishes.Take(3)),
                    BomSnapshotJson = JsonSerializer.Serialize(kvp.Value.BomSnapshots)
                };
                _dbContext.StockReceiptDetails.Add(detail);

                detailsList.Add(new
                {
                    item.InventoryItemId,
                    item.ItemName,
                    BaseUnit = item.BaseUnit,
                    PurchaseUnit = item.PurchaseUnit,
                    ConversionRate = item.ConversionRate,
                    QuantityRequired = qty,
                    CurrentStock = item.CurrentQuantity,
                    AvailableStock = availability.GetValueOrDefault(item.InventoryItemId)?.AvailableQuantity ?? 0m,
                    ExpiredStock = availability.GetValueOrDefault(item.InventoryItemId)?.ExpiredQuantity ?? 0m,
                    MinimumStock = item.MinimumQuantity,
                    StockAfterPlan = (availability.GetValueOrDefault(item.InventoryItemId)?.AvailableQuantity ?? 0m) - qty,
                    IsStockSufficient = (availability.GetValueOrDefault(item.InventoryItemId)?.AvailableQuantity ?? 0m) >= qty,
                    HasSafetyWarning = (availability.GetValueOrDefault(item.InventoryItemId)?.AvailableQuantity ?? 0m) >= qty
                        && (availability.GetValueOrDefault(item.InventoryItemId)?.AvailableQuantity ?? 0m) - qty < item.MinimumQuantity,
                    UnitCost = unitCost,
                    Subtotal = subtotal,
                    UsedForDishes = kvp.Value.ForDishes,
                    HasWastage = wastageRates.Count > 0,
                    WastageSummary = wastageSummary,
                    WastageDetails = kvp.Value.BomBreakdown
                });
            }

            receipt.TotalAmount = grandTotal;
            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();

            try
            {
                await _hubContext.Clients.All.SendAsync("InventoryUpdated", new { receiptId = receipt.ReceiptId, eventType = "requisition_created" });
            }
            catch { }

            return Ok(new
            {
                success = true,
                message = $"Đã tự động lập phiếu yêu cầu xuất kho bếp '{receipt.ReceiptCode}' với {aggregatedIngredients.Count} loại nguyên liệu (trạng thái Chờ duyệt).",
                receipt = new
                {
                    receipt.ReceiptId,
                    receipt.ReceiptCode,
                    receipt.ReceiptType,
                    receipt.Status,
                    receipt.TotalAmount,
                    receipt.CreatedAt,
                    TotalIngredients = aggregatedIngredients.Count,
                    Items = detailsList
                }
            });
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is Microsoft.Data.SqlClient.SqlException sqlException
            && sqlException.Number == 1205)
        {
            await transaction.RollbackAsync();
            return Conflict(new
            {
                success = false,
                code = "REQUISITION_CONCURRENT",
                message = "Một yêu cầu lập phiếu khác vừa được xử lý. Hãy tải lại điều phối để xem phiếu hiện tại."
            });
        }
        catch (Exception ex) when (ex.GetBaseException() is Microsoft.Data.SqlClient.SqlException sqlException
            && sqlException.Number == 1205)
        {
            await transaction.RollbackAsync();
            return Conflict(new
            {
                success = false,
                code = "REQUISITION_CONCURRENT",
                message = "Một yêu cầu lập phiếu khác vừa được xử lý. Hãy tải lại điều phối để xem phiếu hiện tại."
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { success = false, message = "Lỗi khi lập phiếu yêu cầu xuất kho: " + ex.Message });
        }
    }

    // ============================================================== 
    // 3B. ĐIỀU PHỐI BỔ SUNG KHI MÓN BÁN HẾT TRONG NGÀY
    // ============================================================== 

    [Authorize(Roles = "admin,manager,chef,staff")]
    [HttpPost("generate-additional-requisition")]
    public async Task<IActionResult> GenerateAdditionalRequisition([FromBody] AdditionalRequisitionRequest request)
    {
        var targetDate = (request?.ControlDate ?? DateTime.UtcNow).Date;
        var selectedItems = request?.Items?
            .Where(x => x.AdditionalPortions > 0)
            .ToList() ?? new List<AdditionalRequisitionItemInput>();

        if (selectedItems.Count == 0)
        {
            return BadRequest(new { success = false, message = "Hãy nhập số suất bổ sung cho ít nhất một món." });
        }

        var hasApprovedInitialReceipt = await _dbContext.StockReceipts
            .AnyAsync(r => r.ReceiptType == "export_kitchen" &&
                           r.ReceiptCode.StartsWith($"PXK-{targetDate:yyyyMMdd}") &&
                           r.Status == "approved");

        if (!hasApprovedInitialReceipt)
        {
            return BadRequest(new
            {
                success = false,
                message = "Chỉ lập phiếu bổ sung sau khi phiếu xuất bếp ban đầu đã được thủ kho duyệt."
            });
        }

        var pendingAdditionalReceipt = await _dbContext.StockReceipts
            .Where(r => r.ReceiptType == "export_kitchen" &&
                        r.ReceiptCode.StartsWith($"PXK-BS-{targetDate:yyyyMMdd}") &&
                        r.Status == "draft")
            .Select(r => r.ReceiptCode)
            .FirstOrDefaultAsync();

        if (pendingAdditionalReceipt != null)
        {
            return Conflict(new
            {
                success = false,
                code = "ADDITIONAL_RECEIPT_PENDING",
                message = $"Phiếu bổ sung '{pendingAdditionalReceipt}' đang chờ thủ kho duyệt. Hãy duyệt hoặc hủy phiếu đó trước khi lập phiếu bổ sung tiếp theo."
            });
        }

        var foodIds = selectedItems.Select(x => x.FoodId).Distinct().ToList();
        var foods = await _dbContext.Foods
            .Where(f => foodIds.Contains(f.FoodId) && f.IsActive)
            .ToDictionaryAsync(f => f.FoodId);

        var allBoms = await _dbContext.FoodIngredients
            .Include(fi => fi.Food)
            .Include(fi => fi.Variant)
            .Include(fi => fi.InventoryItem)
            .Where(fi => foodIds.Contains(fi.FoodId))
            .ToListAsync();

        var aggregatedIngredients = new Dictionary<int, RequisitionIngredientAggregate>();

        foreach (var input in selectedItems)
        {
            if (!foods.TryGetValue(input.FoodId, out var food))
            {
                return BadRequest(new { success = false, message = $"Món #{input.FoodId} không tồn tại hoặc đã ngừng bán." });
            }

            if (input.VariantId.HasValue)
            {
                var variantExists = await _dbContext.FoodVariants
                    .AnyAsync(v => v.VariantId == input.VariantId.Value && v.FoodId == input.FoodId);
                if (!variantExists)
                {
                    return BadRequest(new { success = false, message = $"Biến thể #{input.VariantId} không thuộc món '{food.FoodName}'." });
                }
            }

            var bomItems = allBoms
                .Where(fi => fi.FoodId == input.FoodId && fi.VariantId == input.VariantId)
                .ToList();

            if (bomItems.Count == 0 && input.VariantId.HasValue)
            {
                bomItems = allBoms
                    .Where(fi => fi.FoodId == input.FoodId && fi.VariantId == null)
                    .ToList();
            }

            if (bomItems.Count == 0)
            {
                return BadRequest(new { success = false, message = $"Món '{food.FoodName}' chưa có BOM để xuất bổ sung." });
            }

            var variantName = bomItems.FirstOrDefault()?.Variant?.VariantName ?? "Mặc định";
            var dishDisplay = $"{food.FoodName} ({variantName}) [{input.AdditionalPortions} suất bổ sung]";

            foreach (var bom in bomItems)
            {
                if (!aggregatedIngredients.TryGetValue(bom.InventoryItemId, out var current))
                {
                    current = new RequisitionIngredientAggregate { Item = bom.InventoryItem };
                    aggregatedIngredients[bom.InventoryItemId] = current;
                }

                var requiredQuantity = input.AdditionalPortions * bom.QuantityPerUnit;
                current.TotalQuantity += requiredQuantity;
                if (!current.ForDishes.Contains(dishDisplay)) current.ForDishes.Add(dishDisplay);
                current.BomBreakdown.Add(new RequisitionBomBreakdown
                {
                    DishName = dishDisplay,
                    Portions = input.AdditionalPortions,
                    QuantityPerUnit = bom.QuantityPerUnit,
                    WastagePercent = CanUseWastage(bom.InventoryItem.BaseUnit) ? bom.WastagePercent : 0m,
                    QuantityRequired = requiredQuantity
                });
                current.BomSnapshots.Add(new StockReceiptBomSnapshotEntry
                {
                    FoodId = input.FoodId,
                    VariantId = input.VariantId,
                    FoodName = food.FoodName,
                    VariantName = variantName,
                    Portions = input.AdditionalPortions,
                    QuantityPerUnit = bom.QuantityPerUnit,
                    WastagePercent = CanUseWastage(bom.InventoryItem.BaseUnit) ? bom.WastagePercent : 0m
                });
            }
        }

        var availability = await GetBatchAvailability(
            aggregatedIngredients.Values.Select(x => x.Item.InventoryItemId),
            targetDate);

        var shortages = aggregatedIngredients.Values
            .Select(x => new
            {
                Aggregate = x,
                Stock = availability.GetValueOrDefault(x.Item.InventoryItemId) ?? new BatchAvailability()
            })
            .Where(x => x.Stock.AvailableQuantity < x.Aggregate.TotalQuantity)
            .Select(x => new
            {
                x.Aggregate.Item.InventoryItemId,
                x.Aggregate.Item.ItemName,
                RequiredQuantity = x.Aggregate.TotalQuantity,
                CurrentQuantity = x.Aggregate.Item.CurrentQuantity,
                AvailableQuantity = x.Stock.AvailableQuantity,
                ExpiredQuantity = x.Stock.ExpiredQuantity,
                ShortageQuantity = x.Aggregate.TotalQuantity - x.Stock.AvailableQuantity,
                ReplenishmentQuantity = x.Aggregate.TotalQuantity + x.Aggregate.Item.MinimumQuantity - x.Stock.AvailableQuantity,
                BaseUnit = x.Aggregate.Item.BaseUnit
            })
            .ToList();

        if (shortages.Count > 0)
        {
            return Conflict(new
            {
                success = false,
                code = "INSUFFICIENT_STOCK",
                message = "Không đủ nguyên liệu cho phiếu bổ sung. Hãy nhập thêm kho hoặc giảm số suất bổ sung.",
                shortages
            });
        }

        var now = DateTime.UtcNow;
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(
            now,
            TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows()
                ? "SE Asia Standard Time"
                : "Asia/Ho_Chi_Minh"));

        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var receipt = new StockReceipt
            {
                ReceiptCode = $"PXK-BS-{targetDate:yyyyMMdd}-{localNow:HHmmssfff}",
                ReceiptType = "export_kitchen",
                Status = "draft",
                TotalAmount = 0,
                Note = !string.IsNullOrWhiteSpace(request?.Note)
                    ? request.Note.Trim()
                    : $"Phiếu xuất bếp bổ sung do món bán hết ngày {targetDate:yyyy-MM-dd}",
                CreatedBy = GetCurrentUserId(),
                CreatedAt = now,
                UpdatedAt = now
            };

            _dbContext.StockReceipts.Add(receipt);
            await _dbContext.SaveChangesAsync();

            decimal grandTotal = 0;
            var detailsList = new List<object>();
            foreach (var aggregate in aggregatedIngredients.Values)
            {
                var item = aggregate.Item;
                var subtotal = InventoryCostHelper.CalculateAmount(
                    aggregate.TotalQuantity,
                    item.CostPerUnit,
                    item.ConversionRate);

                grandTotal += subtotal;
                _dbContext.StockReceiptDetails.Add(new StockReceiptDetail
                {
                    ReceiptId = receipt.ReceiptId,
                    InventoryItemId = item.InventoryItemId,
                    Quantity = aggregate.TotalQuantity,
                    UnitCost = item.CostPerUnit,
                    Subtotal = subtotal,
                    Note = string.Join("; ", aggregate.ForDishes),
                    BomSnapshotJson = JsonSerializer.Serialize(aggregate.BomSnapshots)
                });

                detailsList.Add(new
                {
                    item.InventoryItemId,
                    item.ItemName,
                    BaseUnit = item.BaseUnit,
                    QuantityRequired = aggregate.TotalQuantity,
                    CurrentStock = item.CurrentQuantity,
                    AvailableStock = availability.GetValueOrDefault(item.InventoryItemId)?.AvailableQuantity ?? 0m,
                    ExpiredStock = availability.GetValueOrDefault(item.InventoryItemId)?.ExpiredQuantity ?? 0m,
                    MinimumStock = item.MinimumQuantity,
                    StockAfterPlan = (availability.GetValueOrDefault(item.InventoryItemId)?.AvailableQuantity ?? 0m) - aggregate.TotalQuantity,
                    IsStockSufficient = (availability.GetValueOrDefault(item.InventoryItemId)?.AvailableQuantity ?? 0m) >= aggregate.TotalQuantity,
                    HasSafetyWarning = (availability.GetValueOrDefault(item.InventoryItemId)?.AvailableQuantity ?? 0m) >= aggregate.TotalQuantity
                        && (availability.GetValueOrDefault(item.InventoryItemId)?.AvailableQuantity ?? 0m) - aggregate.TotalQuantity < item.MinimumQuantity,
                    UnitCost = item.CostPerUnit,
                    Subtotal = subtotal,
                    UsedForDishes = aggregate.ForDishes
                });
            }

            receipt.TotalAmount = grandTotal;
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            try
            {
                await _hubContext.Clients.All.SendAsync("InventoryUpdated", new { receiptId = receipt.ReceiptId, eventType = "additional_requisition_created" });
            }
            catch { }

            return Ok(new
            {
                success = true,
                message = $"Đã lập phiếu bổ sung '{receipt.ReceiptCode}' ở trạng thái chờ duyệt.",
                receipt = new
                {
                    receipt.ReceiptId,
                    receipt.ReceiptCode,
                    receipt.ReceiptType,
                    receipt.Status,
                    receipt.TotalAmount,
                    receipt.CreatedAt,
                    TotalIngredients = aggregatedIngredients.Count,
                    Items = detailsList
                }
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { success = false, message = "Lỗi khi lập phiếu xuất bếp bổ sung: " + ex.Message });
        }
    }

    // ==============================================================
    // 4. BẾP CẬP NHẬT SỐ SUẤT THỰC TẾ ĐÃ NẤU (COOKED PORTIONS)
    // ==============================================================

    [Authorize(Roles = "admin,manager,chef")]
    [HttpPut("{id}/cooked")]
    public async Task<IActionResult> UpdateCookedPortions(int id, [FromBody] UpdateCookedPortionsRequest request)
    {
        if (request.CookedPortions < 0)
            return BadRequest(new { success = false, message = "Số suất nấu thực tế không được là số âm." });

        var portion = await _dbContext.DailyPortionControls
            .Include(p => p.Food)
            .Include(p => p.Variant)
            .FirstOrDefaultAsync(p => p.PortionControlId == id);

        if (portion == null)
            return NotFound(new { success = false, message = "Không tìm thấy bản ghi suất ăn này." });

        if (request.CookedPortions < portion.SoldPortions)
        {
            return BadRequest(new
            {
                success = false,
                message = $"Số suất đã nấu ({request.CookedPortions}) không được thấp hơn số suất đã bán ({portion.SoldPortions})."
            });
        }

        portion.CookedPortions = request.CookedPortions;
        if (portion.Status == "planned" && request.CookedPortions > 0)
        {
            portion.Status = "in_service";
        }
        portion.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        try
        {
            await _hubContext.Clients.All.SendAsync("KitchenUpdated", new
            {
                eventType = "portion_cooked_updated",
                portionId = portion.PortionControlId,
                cooked = portion.CookedPortions,
                remain = portion.SystemRemain
            });
        }
        catch { }

        return Ok(new
        {
            success = true,
            message = $"Đã cập nhật số suất nấu '{portion.Food.FoodName}' thành {portion.CookedPortions} phần.",
            data = new
            {
                portion.PortionControlId,
                portion.CookedPortions,
                portion.SoldPortions,
                portion.SystemRemain, // Tự động tính: CookedPortions - SoldPortions
                portion.Status
            }
        });
    }

    // ==============================================================
    // 5. CẬP NHẬT THÔNG TIN MỘT DÒNG SUẤT ĂN
    // ==============================================================

    [Authorize(Roles = "admin,manager,chef")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePortionItem(int id, [FromBody] UpdatePortionItemRequest request)
    {
        var portion = await _dbContext.DailyPortionControls.FirstOrDefaultAsync(p => p.PortionControlId == id);
        if (portion == null)
            return NotFound(new { success = false, message = "Không tìm thấy bản ghi suất ăn." });

        if (request.PlannedPortions.HasValue)
        {
            if (request.PlannedPortions.Value < 0) return BadRequest(new { success = false, message = "Số suất dự kiến không được âm." });
            portion.PlannedPortions = request.PlannedPortions.Value;
        }

        if (request.CarryOverPortions.HasValue)
        {
            if (request.CarryOverPortions.Value < 0) return BadRequest(new { success = false, message = "Tồn chuyển sang không được âm." });
            portion.CarryOverPortions = request.CarryOverPortions.Value;
        }

        if (request.CookedPortions.HasValue)
        {
            if (request.CookedPortions.Value < 0) return BadRequest(new { success = false, message = "Số suất đã nấu không được âm." });
            if (request.CookedPortions.Value < portion.SoldPortions)
                return BadRequest(new { success = false, message = $"Số suất đã nấu ({request.CookedPortions.Value}) không được thấp hơn số suất đã bán ({portion.SoldPortions})." });
            portion.CookedPortions = request.CookedPortions.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var allowed = new[] { "planned", "in_service", "reconciled", "closed" };
            var st = request.Status.Trim().ToLower();
            if (allowed.Contains(st)) portion.Status = st;
        }

        if (request.Note != null)
        {
            portion.Note = request.Note.Trim();
        }

        portion.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Cập nhật suất ăn thành công.",
            data = new
            {
                portion.PortionControlId,
                portion.ControlDate,
                portion.FoodId,
                portion.VariantId,
                portion.PlannedPortions,
                portion.CarryOverPortions,
                portion.CookedPortions,
                portion.SoldPortions,
                portion.SystemRemain,
                portion.Status,
                portion.Note
            }
        });
    }

    // ==============================================================
    // 6. XÓA BẢN GHI SUẤT ĂN (KHI CHƯA PHỤC VỤ)
    // ==============================================================

    [Authorize(Roles = "admin,manager")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePortionItem(int id)
    {
        var portion = await _dbContext.DailyPortionControls.FirstOrDefaultAsync(p => p.PortionControlId == id);
        if (portion == null)
            return NotFound(new { success = false, message = "Không tìm thấy bản ghi suất ăn." });

        if (portion.SoldPortions > 0 || portion.CookedPortions > 0)
        {
            return BadRequest(new
            {
                success = false,
                message = "Món ăn đã bắt đầu nấu hoặc đã có lượt bán phục vụ, không thể xóa khỏi kế hoạch."
            });
        }

        _dbContext.DailyPortionControls.Remove(portion);
        await _dbContext.SaveChangesAsync();

        return Ok(new { success = true, message = "Đã xóa món khỏi bảng điều phối suất ăn." });
    }

    // ==============================================================
    // 7. TỰ ĐỘNG KÉO TỒN BÁN THÀNH PHẨM HÔM QUA SANG HÔM NAY
    // ==============================================================

    [Authorize(Roles = "admin,manager,chef")]
    [HttpPost("pull-yesterday-carryover")]
    public async Task<IActionResult> PullYesterdayCarryOver([FromQuery] string? date = null)
    {
        var today = DateTime.TryParse(date, out var parsedDate) ? parsedDate.Date : DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);

        // Lấy danh sách các món ngày hôm qua có NextCarryOverPortions > 0
        var yesterdayCarryOvers = await _dbContext.DailyPortionControls
            .Where(p => p.ControlDate == yesterday && p.NextCarryOverPortions > 0)
            .ToListAsync();

        if (yesterdayCarryOvers.Count == 0)
        {
            return Ok(new
            {
                success = true,
                message = $"Ngày hôm qua ({yesterday:yyyy-MM-dd}) không có món nào còn dư lưu tủ mát sang hôm nay.",
                pulledCount = 0
            });
        }

        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            int updatedCount = 0;
            var now = DateTime.UtcNow;

            foreach (var y in yesterdayCarryOvers)
            {
                var todayRecord = await _dbContext.DailyPortionControls
                    .FirstOrDefaultAsync(p => p.ControlDate == today &&
                                              p.FoodId == y.FoodId &&
                                              p.VariantId == y.VariantId);

                if (todayRecord != null)
                {
                    todayRecord.CarryOverPortions = y.NextCarryOverPortions;
                    todayRecord.UpdatedAt = now;
                }
                else
                {
                    todayRecord = new DailyPortionControl
                    {
                        ControlDate = today,
                        FoodId = y.FoodId,
                        VariantId = y.VariantId,
                        PlannedPortions = y.NextCarryOverPortions,
                        CarryOverPortions = y.NextCarryOverPortions,
                        CookedPortions = 0,
                        SoldPortions = 0,
                        DisposedPortions = 0,
                        NextCarryOverPortions = 0,
                        Status = "planned",
                        Note = $"Tồn lưu tủ từ ngày {yesterday:dd/MM} chuyển sang",
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    _dbContext.DailyPortionControls.Add(todayRecord);
                }
                updatedCount++;
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                success = true,
                message = $"Đã tự động chuyển tồn bán thành phẩm ({updatedCount} món) từ ngày {yesterday:yyyy-MM-dd} sang ngày {today:yyyy-MM-dd}.",
                pulledCount = updatedCount
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { success = false, message = "Lỗi khi kéo tồn hôm qua: " + ex.Message });
        }
    }
}
