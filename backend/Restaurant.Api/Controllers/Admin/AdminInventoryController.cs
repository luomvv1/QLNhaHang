using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;
using Restaurant.Api.Hubs;
using System.Security.Claims;

namespace Restaurant.Api.Controllers.Admin;

// ==============================================================
// DTOs CHO HẠNG MỤC 1: NGUYÊN LIỆU, NHÀ CUNG CẤP, LÔ HÀNG & BOM
// ==============================================================

public class CreateSupplierRequest
{
    public string SupplierName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateSupplierRequest
{
    public string SupplierName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
}

public class CreateInventoryItemRequest
{
    public int? SupplierId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string PurchaseUnit { get; set; } = "kg";
    public string BaseUnit { get; set; } = "g";
    public decimal ConversionRate { get; set; } = 1000m;
    public decimal InitialQuantity { get; set; } = 0; // Lượng ban đầu tính theo BaseUnit
    public decimal MinimumQuantity { get; set; } = 1000m; // Ngưỡng tối thiểu tính theo BaseUnit
    public decimal CostPerUnit { get; set; } = 0; // Giá vốn theo PurchaseUnit
    public decimal PrepWastagePercent { get; set; }
    public DateTime? InitialExpiryDate { get; set; } // Hạn sử dụng của lô đầu kỳ
    public decimal? InitialActualUsableQuantity { get; set; }
    public string? InitialAdjustmentNote { get; set; }
}

public class UpdateInventoryItemRequest
{
    public int? SupplierId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string PurchaseUnit { get; set; } = "kg";
    public string BaseUnit { get; set; } = "g";
    public decimal ConversionRate { get; set; } = 1000m;
    public decimal MinimumQuantity { get; set; }
    public decimal CostPerUnit { get; set; }
    public decimal? PrepWastagePercent { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CreateBatchRequest
{
    public int InventoryItemId { get; set; }
    public decimal WastagePercent { get; set; } = 0;
    public decimal? ActualUsableQuantity { get; set; }
    public string? AdjustmentNote { get; set; }
    public string? BatchCode { get; set; }
    public decimal Quantity { get; set; } // Lượng theo BaseUnit
    public decimal UnitCost { get; set; } // Đơn giá theo PurchaseUnit
    public DateTime? ManufacturingDate { get; set; }
    public DateTime ExpiryDate { get; set; } // Hạn sử dụng bắt buộc cho FEFO
    public int? SupplierId { get; set; }
    public string? Note { get; set; }
}

public class UpdateBatchRequest
{
    public DateTime? ManufacturingDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string Status { get; set; } = "active"; // active, depleted, expired
}

public class BomIngredientItemRequest
{
    public int InventoryItemId { get; set; }
    public decimal QuantityPerUnit { get; set; } // Luong nguyen lieu sach dung cho mot phan, theo BaseUnit
    public decimal WastagePercent { get; set; } = 0; // Legacy only; prep wastage is recorded on stock receipt lines.
}

public class SaveBomRequest
{
    public int? VariantId { get; set; } // Null nếu món không có biến thể
    public List<BomIngredientItemRequest> Ingredients { get; set; } = new();
}

public class CreateStockMovementRequest
{
    public int InventoryItemId { get; set; }
    public int? BatchId { get; set; }
    public string MovementType { get; set; } = "import"; // import, export, adjustment, waste
    public decimal Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Reason { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
}

// ==============================================================
// CONTROLLER: QUẢN LÝ KHO & BOM (HẠNG MỤC 1)
// ==============================================================

[Route("api/admin/inventory")]
[Authorize(Roles = "admin,manager")]
[ApiController]
public class AdminInventoryController : ControllerBase
{
    private readonly RestaurantDbContext _dbContext;
    private readonly IHubContext<RestaurantHub> _hubContext;

    public AdminInventoryController(RestaurantDbContext dbContext, IHubContext<RestaurantHub> hubContext)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    private int? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out var userId) ? userId : null;
    }

    // ==============================================================
    // 1. NHÀ CUNG CẤP (SUPPLIERS)
    // ==============================================================

    [HttpGet("suppliers")]
    public async Task<IActionResult> GetSuppliers([FromQuery] string? search = null, [FromQuery] bool? isActive = null)
    {
        var query = _dbContext.Suppliers.AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(s => s.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(sup => sup.SupplierName.ToLower().Contains(s) ||
                                       (sup.Phone != null && sup.Phone.Contains(s)) ||
                                       (sup.Email != null && sup.Email.ToLower().Contains(s)));
        }

        var suppliers = await query
            .OrderByDescending(s => s.SupplierId)
            .Select(s => new
            {
                s.SupplierId,
                s.SupplierName,
                s.Phone,
                s.Email,
                s.Address,
                s.IsActive,
                s.CreatedAt,
                ItemCount = s.InventoryItems.Count(i => i.IsActive)
            })
            .ToListAsync();

        return Ok(new { success = true, data = suppliers });
    }

    [HttpGet("suppliers/{id}")]
    public async Task<IActionResult> GetSupplierById(int id)
    {
        var supplier = await _dbContext.Suppliers
            .Include(s => s.InventoryItems.Where(i => i.IsActive))
            .FirstOrDefaultAsync(s => s.SupplierId == id);

        if (supplier == null)
            return NotFound(new { success = false, message = "Không tìm thấy nhà cung cấp này." });

        return Ok(new
        {
            success = true,
            data = new
            {
                supplier.SupplierId,
                supplier.SupplierName,
                supplier.Phone,
                supplier.Email,
                supplier.Address,
                supplier.IsActive,
                supplier.CreatedAt,
                Items = supplier.InventoryItems.Select(i => new
                {
                    i.InventoryItemId,
                    i.ItemName,
                    i.PurchaseUnit,
                    i.BaseUnit,
                    i.ConversionRate,
                    i.CurrentQuantity,
                    i.CostPerUnit
                })
            }
        });
    }

    [Authorize(Roles = "admin,manager")]
    [HttpPost("suppliers")]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SupplierName))
            return BadRequest(new { success = false, message = "Tên nhà cung cấp không được để trống." });

        var supplier = new Supplier
        {
            SupplierName = request.SupplierName.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Suppliers.Add(supplier);
        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Tạo nhà cung cấp mới thành công.",
            data = supplier
        });
    }

    [Authorize(Roles = "admin,manager")]
    [HttpPut("suppliers/{id}")]
    public async Task<IActionResult> UpdateSupplier(int id, [FromBody] UpdateSupplierRequest request)
    {
        var supplier = await _dbContext.Suppliers.FirstOrDefaultAsync(s => s.SupplierId == id);
        if (supplier == null)
            return NotFound(new { success = false, message = "Không tìm thấy nhà cung cấp." });

        if (string.IsNullOrWhiteSpace(request.SupplierName))
            return BadRequest(new { success = false, message = "Tên nhà cung cấp không được để trống." });

        supplier.SupplierName = request.SupplierName.Trim();
        supplier.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        supplier.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        supplier.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        supplier.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Cập nhật nhà cung cấp thành công.",
            data = supplier
        });
    }

    [Authorize(Roles = "admin,manager")]
    [HttpDelete("suppliers/{id}")]
    public async Task<IActionResult> DeleteSupplier(int id)
    {
        var supplier = await _dbContext.Suppliers
            .Include(s => s.InventoryItems)
            .FirstOrDefaultAsync(s => s.SupplierId == id);

        if (supplier == null)
            return NotFound(new { success = false, message = "Không tìm thấy nhà cung cấp." });

        // Giữ lại supplier nếu đã từng được tham chiếu bởi nguyên liệu, kể cả nguyên liệu đã ngừng dùng.
        // Xóa cứng trong trường hợp này sẽ làm hỏng lịch sử kho và có thể vướng FK của InventoryItems/InventoryBatches.
        if (supplier.InventoryItems.Any())
        {
            supplier.IsActive = false;
            await _dbContext.SaveChangesAsync();
            return Ok(new { success = true, message = "Đã ngừng hợp tác với nhà cung cấp (vô hiệu hóa để giữ lịch sử)." });
        }

        _dbContext.Suppliers.Remove(supplier);
        await _dbContext.SaveChangesAsync();
        return Ok(new { success = true, message = "Đã xóa nhà cung cấp thành công." });
    }

    // ==============================================================
    // 2. NGUYÊN LIỆU TỒN KHO (INVENTORY ITEMS)
    // ==============================================================

    [HttpGet("items")]
    public async Task<IActionResult> GetInventoryItems(
        [FromQuery] string? search = null,
        [FromQuery] int? supplierId = null,
        [FromQuery] bool? isLowStock = null,
        [FromQuery] bool? isActive = null)
    {
        var query = _dbContext.InventoryItems
            .Include(i => i.Supplier)
            .Include(i => i.InventoryBatches.Where(b => b.QuantityRemaining > 0))
            .AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(i => i.IsActive == isActive.Value);
        }

        if (supplierId.HasValue)
        {
            query = query.Where(i => i.SupplierId == supplierId.Value);
        }

        if (isLowStock.HasValue && isLowStock.Value)
        {
            query = query.Where(i => i.CurrentQuantity <= i.MinimumQuantity);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(i => i.ItemName.ToLower().Contains(s) ||
                                     i.Unit.ToLower().Contains(s) ||
                                     i.PurchaseUnit.ToLower().Contains(s) ||
                                     i.BaseUnit.ToLower().Contains(s));
        }

        var itemEntities = await query
            .OrderBy(i => i.CurrentQuantity <= i.MinimumQuantity ? 0 : 1)
            .ThenBy(i => i.ItemName)
            .ToListAsync();

        var items = itemEntities.Select(i =>
        {
            var remainingBatches = i.InventoryBatches.Where(b => b.QuantityRemaining > 0).ToList();
            var batchQuantity = remainingBatches.Sum(b => b.QuantityRemaining);
            var batchValue = remainingBatches.Sum(b => b.QuantityRemaining * InventoryCostHelper.ToBaseUnitCost(b.UnitCost, i.ConversionRate));
            var totalValue = remainingBatches.Count > 0
                ? Math.Round(batchValue, 2)
                : InventoryCostHelper.CalculateAmount(i.CurrentQuantity, i.CostPerUnit, i.ConversionRate);
            var averagePurchaseCost = batchQuantity > 0
                ? batchValue / (i.ConversionRate > 0 ? batchQuantity / i.ConversionRate : batchQuantity)
                : i.CostPerUnit;
            var averageBaseCost = batchQuantity > 0
                ? batchValue / batchQuantity
                : InventoryCostHelper.ToBaseUnitCost(i.CostPerUnit, i.ConversionRate);

            return new
            {
                i.InventoryItemId,
                i.SupplierId,
                SupplierName = i.Supplier != null ? i.Supplier.SupplierName : "Chưa gắn",
                i.ItemName,
                Unit = i.BaseUnit, // Hiển thị đơn vị gốc
                i.PurchaseUnit,
                i.BaseUnit,
                i.ConversionRate,
                i.PrepWastagePercent,
                i.CurrentQuantity,
                QuantityInPurchaseUnit = i.ConversionRate > 0 ? i.CurrentQuantity / i.ConversionRate : i.CurrentQuantity,
                i.MinimumQuantity,
                i.CostPerUnit,
                BatchQuantityTotal = batchQuantity,
                HasBatchQuantityMismatch = Math.Abs(i.CurrentQuantity - batchQuantity) > 0.01m,
                BatchValueTotal = Math.Round(batchValue, 2),
                CostPerPurchaseUnit = averagePurchaseCost,
                CostPerBaseUnit = averageBaseCost,
                TotalValue = totalValue,
                IsLowStock = i.CurrentQuantity <= i.MinimumQuantity,
                ActiveBatchesCount = remainingBatches.Count(b => b.Status == "active"),
                NearestExpiryDate = remainingBatches
                    .Where(b => b.Status == "active" && b.QuantityRemaining > 0)
                    .OrderBy(b => b.ExpiryDate)
                    .Select(b => (DateTime?)b.ExpiryDate)
                    .FirstOrDefault(),
                i.IsActive,
                i.CreatedAt,
                i.UpdatedAt
            };
        }).ToList();

        return Ok(new { success = true, data = items });
    }

    [HttpGet("items/{id}")]
    public async Task<IActionResult> GetInventoryItemById(int id)
    {
        var item = await _dbContext.InventoryItems
            .Include(i => i.Supplier)
            .Include(i => i.InventoryBatches.Where(b => b.Status == "active"))
                .ThenInclude(b => b.StockReceiptDetails)
                    .ThenInclude(d => d.Receipt)
            .Include(i => i.FoodIngredients)
                .ThenInclude(fi => fi.Food)
            .Include(i => i.FoodIngredients)
                .ThenInclude(fi => fi.Variant)
            .FirstOrDefaultAsync(i => i.InventoryItemId == id);

        if (item == null)
            return NotFound(new { success = false, message = "Không tìm thấy nguyên liệu." });

        var activeBatches = item.InventoryBatches
            .Where(b => b.QuantityRemaining > 0)
            .OrderBy(b => b.ExpiryDate) // FEFO
            .Select(b => new
            {
                b.BatchId,
                b.BatchCode,
                b.QuantityRemaining,
                b.OriginalQuantity,
                GrossReceivedQuantity = b.StockReceiptDetails
                    .Where(d => d.Receipt.ReceiptType == "import_supplier" && d.Receipt.Status == "approved")
                    .Sum(d => (decimal?)(d.GrossQuantity ?? d.Quantity)) ?? b.OriginalQuantity,
                ExpectedUsableQuantity = b.StockReceiptDetails
                    .Where(d => d.Receipt.ReceiptType == "import_supplier" && d.Receipt.Status == "approved")
                    .Sum(d => (decimal?)(d.ExpectedUsableQuantity ?? d.Quantity)) ?? b.OriginalQuantity,
                ActualUsableQuantity = b.StockReceiptDetails
                    .Where(d => d.Receipt.ReceiptType == "import_supplier" && d.Receipt.Status == "approved")
                    .Sum(d => (decimal?)d.Quantity) ?? b.OriginalQuantity,
                b.UnitCost,
                b.ExpiryDate,
                b.ReceivedDate,
                b.Status,
                DaysUntilExpiry = (int)(b.ExpiryDate.Date - DateTime.UtcNow.Date).TotalDays
            })
            .ToList();

        var usedInBoms = item.FoodIngredients.Select(fi => new
        {
            fi.FoodIngredientId,
            fi.FoodId,
            fi.Food.FoodName,
            fi.VariantId,
            VariantName = fi.Variant != null ? fi.Variant.VariantName : "Mặc định",
            fi.QuantityPerUnit,
            fi.WastagePercent
        }).ToList();

        return Ok(new
        {
            success = true,
            data = new
            {
                item.InventoryItemId,
                item.SupplierId,
                SupplierName = item.Supplier?.SupplierName,
                item.ItemName,
                item.PurchaseUnit,
                item.BaseUnit,
                item.ConversionRate,
                item.CurrentQuantity,
                item.MinimumQuantity,
                item.CostPerUnit,
                item.PrepWastagePercent,
                CostPerPurchaseUnit = item.CostPerUnit,
                CostPerBaseUnit = item.ConversionRate > 0 ? item.CostPerUnit / item.ConversionRate : item.CostPerUnit,
                item.IsActive,
                item.CreatedAt,
                item.UpdatedAt,
                ActiveBatches = activeBatches,
                UsedInBoms = usedInBoms
            }
        });
    }

    [Authorize(Roles = "admin,manager")]
    [HttpPost("items")]
    public async Task<IActionResult> CreateInventoryItem([FromBody] CreateInventoryItemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ItemName))
            return BadRequest(new { success = false, message = "Tên nguyên liệu không được để trống." });

        if (request.ConversionRate <= 0)
            return BadRequest(new { success = false, message = "Tỷ lệ quy đổi phải lớn hơn 0." });

        if (request.InitialQuantity < 0 || request.MinimumQuantity < 0 || request.CostPerUnit < 0)
            return BadRequest(new { success = false, message = "Số lượng và giá thành không được là số âm." });

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return Unauthorized(new { success = false, message = "Không xác định được người thao tác từ token đăng nhập." });

        var itemName = request.ItemName.Trim();
        if (await _dbContext.InventoryItems.AnyAsync(i => i.ItemName == itemName))
            return Conflict(new { success = false, message = $"Nguyên liệu '{itemName}' đã tồn tại." });

        if (request.SupplierId.HasValue && !await _dbContext.Suppliers.AnyAsync(s => s.SupplierId == request.SupplierId.Value && s.IsActive))
            return BadRequest(new { success = false, message = "Nhà cung cấp không tồn tại hoặc đã ngừng hoạt động." });

        if (request.InitialQuantity > 0 && request.InitialExpiryDate.HasValue && request.InitialExpiryDate.Value.Date < DateTime.UtcNow.Date)
            return BadRequest(new { success = false, message = "Hạn sử dụng của lô đầu kỳ không được nằm trong quá khứ." });

        var purchaseUnit = string.IsNullOrWhiteSpace(request.PurchaseUnit) ? "kg" : request.PurchaseUnit.Trim();
        var baseUnit = string.IsNullOrWhiteSpace(request.BaseUnit) ? "g" : request.BaseUnit.Trim();
        if (request.PrepWastagePercent < 0 || request.PrepWastagePercent >= 100)
            return BadRequest(new { success = false, message = "Hao hụt sơ chế mặc định phải từ 0 đến dưới 100%." });
        var initialWastagePercent = request.PrepWastagePercent;
        var initialExpectedUsable = Math.Round(request.InitialQuantity * (1m - initialWastagePercent / 100m), 4);
        var initialActualUsable = request.InitialActualUsableQuantity ?? initialExpectedUsable;
        if (request.InitialQuantity > 0 && (initialActualUsable <= 0 || initialActualUsable > request.InitialQuantity))
            return BadRequest(new { success = false, message = "Lượng thực nhận đầu kỳ không hợp lệ (phải lớn hơn 0 và không vượt lượng mua)." });
        if (request.InitialQuantity > 0 && initialActualUsable < initialExpectedUsable - 0.0001m && string.IsNullOrWhiteSpace(request.InitialAdjustmentNote))
            return BadRequest(new { success = false, message = "Hao hụt đầu kỳ vượt dự kiến. Vui lòng ghi lý do điều chỉnh." });

        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var item = new InventoryItem
            {
                SupplierId = request.SupplierId,
                ItemName = itemName,
                Unit = baseUnit,
                PurchaseUnit = purchaseUnit,
                BaseUnit = baseUnit,
                ConversionRate = request.ConversionRate,
                PrepWastagePercent = request.PrepWastagePercent,
                // Tồn chỉ tăng sau khi phiếu nhập được duyệt.
                CurrentQuantity = 0,
                MinimumQuantity = request.MinimumQuantity,
                CostPerUnit = request.CostPerUnit,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.InventoryItems.Add(item);
            await _dbContext.SaveChangesAsync();

            string? pendingReceiptCode = null;

            // Tồn đầu kỳ cũng phải qua phiếu nhập nháp; chưa tạo lô và chưa cộng tồn.
            if (request.InitialQuantity > 0)
            {
                var now = DateTime.UtcNow;
                var expiryDate = request.InitialExpiryDate ?? now.AddDays(30);
                string batchCode = $"BAT-{now:yyyyMMdd}-{item.InventoryItemId:D3}";

                pendingReceiptCode = $"PN-{now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
                while (await _dbContext.StockReceipts.AnyAsync(r => r.ReceiptCode == pendingReceiptCode))
                {
                    pendingReceiptCode = $"PN-{now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
                }

                var receipt = new StockReceipt
                {
                    ReceiptCode = pendingReceiptCode,
                    ReceiptType = "import_supplier",
                    Status = "draft",
                    SupplierId = request.SupplierId,
                    Note = "Nhập tồn đầu kỳ chờ duyệt",
                    CreatedBy = currentUserId.Value,
                    CreatedAt = now,
                    UpdatedAt = now,
                    TotalAmount = InventoryCostHelper.CalculateAmount(request.InitialQuantity, request.CostPerUnit, item.ConversionRate)
                };

                receipt.StockReceiptDetails.Add(new StockReceiptDetail
                {
                    InventoryItemId = item.InventoryItemId,
                    Quantity = initialActualUsable,
                    GrossQuantity = request.InitialQuantity,
                    WastagePercent = initialWastagePercent,
                    WastageQuantity = request.InitialQuantity - initialActualUsable,
                    ExpectedUsableQuantity = initialExpectedUsable,
                    AdjustmentNote = request.InitialAdjustmentNote?.Trim(),
                    UnitCost = request.CostPerUnit,
                    Subtotal = receipt.TotalAmount,
                    NewBatchCode = batchCode,
                    ManufacturingDate = now.Date,
                    ExpiryDate = expiryDate.Date
                });
                _dbContext.StockReceipts.Add(receipt);
                await _dbContext.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            try
            {
                await _hubContext.Clients.All.SendAsync("InventoryUpdated", new { itemId = item.InventoryItemId, eventType = "created" });
            }
            catch { }

            return Ok(new
            {
                success = true,
                message = pendingReceiptCode == null
                    ? "Thêm nguyên liệu mới thành công."
                    : $"Thêm nguyên liệu thành công và tạo phiếu nhập '{pendingReceiptCode}' đang chờ duyệt. Tồn kho chưa thay đổi.",
                pendingReceiptCode,
                data = new
                {
                    item.InventoryItemId,
                    item.ItemName,
                    item.PurchaseUnit,
                    item.BaseUnit,
                    item.ConversionRate,
                    item.PrepWastagePercent,
                    item.CurrentQuantity,
                    item.MinimumQuantity,
                    item.CostPerUnit,
                    item.SupplierId,
                    item.IsActive
                }
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { success = false, message = "Lỗi khi tạo nguyên liệu: " + ex.Message });
        }
    }

    [Authorize(Roles = "admin,manager")]
    [HttpPut("items/{id}")]
    public async Task<IActionResult> UpdateInventoryItem(int id, [FromBody] UpdateInventoryItemRequest request)
    {
        var item = await _dbContext.InventoryItems.FirstOrDefaultAsync(i => i.InventoryItemId == id);
        if (item == null)
            return NotFound(new { success = false, message = "Không tìm thấy nguyên liệu này." });

        if (string.IsNullOrWhiteSpace(request.ItemName))
            return BadRequest(new { success = false, message = "Tên nguyên liệu không được để trống." });

        if (request.ConversionRate <= 0)
            return BadRequest(new { success = false, message = "Tỷ lệ quy đổi phải lớn hơn 0." });

        if (request.MinimumQuantity < 0 || request.CostPerUnit < 0)
            return BadRequest(new { success = false, message = "Ngưỡng tối thiểu và giá thành không được âm." });

        var itemName = request.ItemName.Trim();
        if (await _dbContext.InventoryItems.AnyAsync(i => i.InventoryItemId != id && i.ItemName == itemName))
            return Conflict(new { success = false, message = $"Nguyên liệu '{itemName}' đã tồn tại." });

        if (request.SupplierId.HasValue && !await _dbContext.Suppliers.AnyAsync(s => s.SupplierId == request.SupplierId.Value && s.IsActive))
            return BadRequest(new { success = false, message = "Nhà cung cấp không tồn tại hoặc đã ngừng hoạt động." });

        var requestedBaseUnit = string.IsNullOrWhiteSpace(request.BaseUnit) ? item.BaseUnit : request.BaseUnit.Trim();
        var prepWastagePercent = request.PrepWastagePercent ?? item.PrepWastagePercent;
        if (prepWastagePercent < 0 || prepWastagePercent >= 100)
            return BadRequest(new { success = false, message = "Hao hụt sơ chế mặc định phải từ 0 đến dưới 100%." });

        item.SupplierId = request.SupplierId;
        item.ItemName = itemName;
        item.PurchaseUnit = string.IsNullOrWhiteSpace(request.PurchaseUnit) ? item.PurchaseUnit : request.PurchaseUnit.Trim();
        item.BaseUnit = string.IsNullOrWhiteSpace(request.BaseUnit) ? item.BaseUnit : request.BaseUnit.Trim();
        item.Unit = item.BaseUnit;
        item.ConversionRate = request.ConversionRate;
        item.PrepWastagePercent = prepWastagePercent;
        item.MinimumQuantity = request.MinimumQuantity;
        item.CostPerUnit = request.CostPerUnit;
        item.IsActive = request.IsActive;
        item.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        try
        {
            await _hubContext.Clients.All.SendAsync("InventoryUpdated", new { itemId = item.InventoryItemId, eventType = "updated" });
        }
        catch { }

        return Ok(new
        {
            success = true,
            message = "Cập nhật nguyên liệu thành công.",
            data = new
            {
                item.InventoryItemId,
                item.ItemName,
                item.PurchaseUnit,
                item.BaseUnit,
                item.ConversionRate,
                item.PrepWastagePercent,
                item.CurrentQuantity,
                item.MinimumQuantity,
                item.CostPerUnit,
                item.SupplierId,
                item.IsActive
            }
        });
    }

    [Authorize(Roles = "admin,manager")]
    [HttpDelete("items/{id}")]
    public async Task<IActionResult> DeleteInventoryItem(int id)
    {
        var item = await _dbContext.InventoryItems.FirstOrDefaultAsync(i => i.InventoryItemId == id);
        if (item == null)
            return NotFound(new { success = false, message = "Không tìm thấy nguyên liệu." });

        item.IsActive = false;
        item.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        try
        {
            await _hubContext.Clients.All.SendAsync("InventoryUpdated", new { itemId = id, eventType = "deactivated" });
        }
        catch { }

        return Ok(new { success = true, message = "Đã ngừng sử dụng nguyên liệu này (vô hiệu hóa)." });
    }

    // ==============================================================
    // 3. QUẢN LÝ LÔ HÀNG (INVENTORY BATCHES - FEFO/FIFO)
    // ==============================================================

    [HttpGet("batches")]
    public async Task<IActionResult> GetBatches(
        [FromQuery] int? inventoryItemId = null,
        [FromQuery] string? status = null, // active, depleted, expired
        [FromQuery] int? expiringWithinDays = null,
        [FromQuery] string? search = null)
    {
        var query = _dbContext.InventoryBatches
            .Include(b => b.InventoryItem)
            .Include(b => b.Supplier)
            .AsQueryable();

        if (inventoryItemId.HasValue)
        {
            query = query.Where(b => b.InventoryItemId == inventoryItemId.Value);
        }

        var today = DateTime.UtcNow.AddHours(7).Date;

        if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            var st = status.Trim().ToLower();
            query = st switch
            {
                "expired" => query.Where(b => b.QuantityRemaining > 0 && b.ExpiryDate.Date < today),
                "active" => query.Where(b => b.Status == "active" && b.QuantityRemaining > 0 && b.ExpiryDate.Date >= today),
                _ => query.Where(b => b.Status.ToLower() == st)
            };
        }

        if (expiringWithinDays.HasValue && expiringWithinDays.Value > 0)
        {
            var targetDate = today.AddDays(expiringWithinDays.Value);
            query = query.Where(b => b.Status == "active" && b.ExpiryDate.Date >= today && b.ExpiryDate.Date <= targetDate && b.QuantityRemaining > 0);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(b => b.BatchCode.ToLower().Contains(s) ||
                                     b.InventoryItem.ItemName.ToLower().Contains(s));
        }

        // FEFO: Hạn dùng gần nhất xếp lên trước, sau đó FIFO: Ngày nhập trước
        var batches = await query
            .OrderBy(b => b.ExpiryDate)
            .ThenBy(b => b.ReceivedDate)
            .ThenBy(b => b.BatchId)
            .Select(b => new
            {
                b.BatchId,
                b.InventoryItemId,
                ItemName = b.InventoryItem.ItemName,
                BaseUnit = b.InventoryItem.BaseUnit,
                PurchaseUnit = b.InventoryItem.PurchaseUnit,
                ConversionRate = b.InventoryItem.ConversionRate,
                b.BatchCode,
                IsKitchenReturn = b.BatchCode.StartsWith("TH-BEP-"),
                b.QuantityRemaining,
                b.OriginalQuantity,
                GrossReceivedQuantity = b.StockReceiptDetails
                    .Where(d => d.Receipt.ReceiptType == "import_supplier" && d.Receipt.Status == "approved")
                    .Sum(d => (decimal?)(d.GrossQuantity ?? d.Quantity)) ?? b.OriginalQuantity,
                ExpectedUsableQuantity = b.StockReceiptDetails
                    .Where(d => d.Receipt.ReceiptType == "import_supplier" && d.Receipt.Status == "approved")
                    .Sum(d => (decimal?)(d.ExpectedUsableQuantity ?? d.Quantity)) ?? b.OriginalQuantity,
                ActualUsableQuantity = b.StockReceiptDetails
                    .Where(d => d.Receipt.ReceiptType == "import_supplier" && d.Receipt.Status == "approved")
                    .Sum(d => (decimal?)d.Quantity) ?? b.OriginalQuantity,
                b.UnitCost,
                UnitCostPerPurchaseUnit = b.UnitCost,
                UnitCostPerBaseUnit = b.InventoryItem.ConversionRate > 0 ? b.UnitCost / b.InventoryItem.ConversionRate : b.UnitCost,
                TotalBatchValue = b.InventoryItem.ConversionRate > 0 ? (b.QuantityRemaining / b.InventoryItem.ConversionRate) * b.UnitCost : b.QuantityRemaining * b.UnitCost,
                b.ManufacturingDate,
                b.ExpiryDate,
                b.ReceivedDate,
                b.SupplierId,
                SupplierName = b.Supplier != null ? b.Supplier.SupplierName : "Chưa có",
                Status = b.QuantityRemaining > 0 && b.ExpiryDate.Date < today ? "expired" : b.Status,
                DaysUntilExpiry = (int)(b.ExpiryDate.Date - today).TotalDays,
                IsExpired = b.ExpiryDate.Date < today,
                IsExpiringSoon = (int)(b.ExpiryDate.Date - today).TotalDays <= 7 && (int)(b.ExpiryDate.Date - today).TotalDays >= 0
            })
            .ToListAsync();

        return Ok(new { success = true, count = batches.Count, data = batches });
    }

    [HttpGet("batches/{id}")]
    public async Task<IActionResult> GetBatchById(int id)
    {
        var batch = await _dbContext.InventoryBatches
            .Include(b => b.InventoryItem)
            .Include(b => b.Supplier)
            .Include(b => b.StockMovements)
                .ThenInclude(m => m.CreatedByNavigation)
            .Include(b => b.StockReceiptDetails)
                .ThenInclude(d => d.Receipt)
            .FirstOrDefaultAsync(b => b.BatchId == id);

        if (batch == null)
            return NotFound(new { success = false, message = "Không tìm thấy lô hàng này." });

        var today = DateTime.UtcNow.AddHours(7).Date;

        return Ok(new
        {
            success = true,
            data = new
            {
                batch.BatchId,
                batch.InventoryItemId,
                ItemName = batch.InventoryItem.ItemName,
                BaseUnit = batch.InventoryItem.BaseUnit,
                PurchaseUnit = batch.InventoryItem.PurchaseUnit,
                ConversionRate = batch.InventoryItem.ConversionRate,
                batch.BatchCode,
                IsKitchenReturn = batch.BatchCode.StartsWith("TH-BEP-"),
                batch.QuantityRemaining,
                batch.OriginalQuantity,
                Intakes = batch.StockReceiptDetails
                    .Where(d => d.Receipt.ReceiptType == "import_supplier" && d.Receipt.Status == "approved")
                    .OrderBy(d => d.Receipt.CreatedAt)
                    .Select(d => new
                    {
                        d.Receipt.ReceiptCode,
                        d.Receipt.CreatedAt,
                        GrossQuantity = d.GrossQuantity ?? d.Quantity,
                        ExpectedUsableQuantity = d.ExpectedUsableQuantity ?? d.Quantity,
                        ActualUsableQuantity = d.Quantity,
                        d.WastagePercent,
                        d.WastageQuantity,
                        d.AdjustmentNote
                    }),
                batch.UnitCost,
                UnitCostPerPurchaseUnit = batch.UnitCost,
                UnitCostPerBaseUnit = batch.InventoryItem.ConversionRate > 0 ? batch.UnitCost / batch.InventoryItem.ConversionRate : batch.UnitCost,
                batch.ManufacturingDate,
                batch.ExpiryDate,
                batch.ReceivedDate,
                batch.SupplierId,
                SupplierName = batch.Supplier?.SupplierName,
                Status = batch.QuantityRemaining > 0 && batch.ExpiryDate.Date < today ? "expired" : batch.Status,
                DaysUntilExpiry = (int)(batch.ExpiryDate.Date - today).TotalDays,
                Movements = batch.StockMovements.OrderByDescending(m => m.CreatedAt).Select(m => new
                {
                    m.StockMovementId,
                    m.MovementType,
                    m.Quantity,
                    m.UnitCost,
                    m.Reason,
                    m.ReferenceType,
                    m.ReferenceId,
                    CreatedByName = m.CreatedByNavigation.FullName,
                    m.CreatedAt
                })
            }
        });
    }

    [Authorize(Roles = "admin,manager")]
    [HttpPost("batches")]
    public async Task<IActionResult> CreateBatch([FromBody] CreateBatchRequest request)
    {
        if (request.Quantity <= 0)
            return BadRequest(new { success = false, message = "Số lượng nhập lô phải lớn hơn 0." });

        if (request.UnitCost < 0)
            return BadRequest(new { success = false, message = "Đơn giá lô không được âm." });

        var now = DateTime.UtcNow;

        if (request.ExpiryDate == default)
            return BadRequest(new { success = false, message = "Hạn sử dụng của lô là bắt buộc." });

        if (request.ExpiryDate.Date < now.Date)
            return BadRequest(new { success = false, message = "Không thể tạo lô đang ở trạng thái active nhưng đã hết hạn." });

        if (request.ManufacturingDate.HasValue && request.ManufacturingDate.Value.Date > request.ExpiryDate.Date)
            return BadRequest(new { success = false, message = "Ngày sản xuất không được sau hạn sử dụng." });

        if (request.SupplierId.HasValue && !await _dbContext.Suppliers.AnyAsync(s => s.SupplierId == request.SupplierId.Value && s.IsActive))
            return BadRequest(new { success = false, message = "Nhà cung cấp không tồn tại hoặc đã ngừng hoạt động." });

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return Unauthorized(new { success = false, message = "Không xác định được người thao tác từ token đăng nhập." });

        var item = await _dbContext.InventoryItems
            .FirstOrDefaultAsync(i => i.InventoryItemId == request.InventoryItemId && i.IsActive);
        if (item == null)
            return NotFound(new { success = false, message = "Không tìm thấy nguyên liệu hợp lệ." });

        // Endpoint cũ vẫn được giữ để không làm hỏng client cũ, nhưng chỉ tạo
        // phiếu nhập nháp. Lô và tồn kho chỉ được ghi nhận trong ApproveReceipt.
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            if (!string.IsNullOrWhiteSpace(request.BatchCode) && await _dbContext.InventoryBatches.AnyAsync(
                b => b.InventoryItemId == request.InventoryItemId && b.BatchCode == request.BatchCode.Trim()))
            {
                await transaction.RollbackAsync();
                return Conflict(new { success = false, message = $"Mã lô '{request.BatchCode.Trim()}' đã tồn tại cho nguyên liệu này." });
            }

            var receiptCode = $"PN-{now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
            while (await _dbContext.StockReceipts.AnyAsync(r => r.ReceiptCode == receiptCode))
            {
                receiptCode = $"PN-{now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
            }

            var wastagePercent = request.WastagePercent > 0 ? request.WastagePercent : item.PrepWastagePercent;
            if (wastagePercent < 0 || wastagePercent >= 100)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { success = false, message = "Tỷ lệ hao hụt phải từ 0 đến dưới 100%." });
            }

            var grossQuantity = request.Quantity;
            var expectedUsableQuantity = Math.Round(grossQuantity * (1m - wastagePercent / 100m), 4);
            var usableQuantity = request.ActualUsableQuantity ?? expectedUsableQuantity;
            var wastageQuantity = grossQuantity - usableQuantity;
            if (usableQuantity <= 0)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { success = false, message = "Lượng dùng được sau sơ chế phải lớn hơn 0." });
            }
            if (usableQuantity > grossQuantity)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { success = false, message = "Lượng thực nhận sau sơ chế không được lớn hơn lượng mua." });
            }
            if (usableQuantity < expectedUsableQuantity - 0.0001m && string.IsNullOrWhiteSpace(request.AdjustmentNote))
            {
                await transaction.RollbackAsync();
                return BadRequest(new { success = false, message = "Hao hụt thực tế vượt dự kiến. Vui lòng ghi lý do điều chỉnh." });
            }

            var unitCost = request.UnitCost > 0 ? request.UnitCost : item.CostPerUnit;
            var receipt = new StockReceipt
            {
                ReceiptCode = receiptCode,
                ReceiptType = "import_supplier",
                Status = "draft",
                SupplierId = request.SupplierId ?? item.SupplierId,
                Note = string.IsNullOrWhiteSpace(request.Note)
                    ? "Lập phiếu nhập hàng chờ duyệt"
                    : request.Note.Trim(),
                CreatedBy = currentUserId.Value,
                CreatedAt = now,
                UpdatedAt = now,
                TotalAmount = InventoryCostHelper.CalculateAmount(grossQuantity, unitCost, item.ConversionRate)
            };

            receipt.StockReceiptDetails.Add(new StockReceiptDetail
            {
                InventoryItemId = item.InventoryItemId,
                Quantity = usableQuantity,
                GrossQuantity = grossQuantity,
                WastagePercent = wastagePercent,
                WastageQuantity = wastageQuantity,
                ExpectedUsableQuantity = expectedUsableQuantity,
                AdjustmentNote = request.AdjustmentNote?.Trim(),
                UnitCost = unitCost,
                Subtotal = receipt.TotalAmount,
                NewBatchCode = string.IsNullOrWhiteSpace(request.BatchCode) ? null : request.BatchCode.Trim(),
                ManufacturingDate = request.ManufacturingDate?.Date,
                ExpiryDate = request.ExpiryDate.Date
            });

            _dbContext.StockReceipts.Add(receipt);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return Created($"/api/admin/inventory/receipts/{receipt.ReceiptId}", new
            {
                success = true,
                message = $"Đã tạo phiếu nhập '{receipt.ReceiptCode}' ở trạng thái chờ duyệt. Tồn kho chưa thay đổi.",
                receiptId = receipt.ReceiptId,
                receiptCode = receipt.ReceiptCode,
                status = receipt.Status
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { success = false, message = "Lỗi khi tạo phiếu nhập: " + ex.Message });
        }
    }

    [Authorize(Roles = "admin,manager")]
    [HttpPut("batches/{id}")]
    public async Task<IActionResult> UpdateBatch(int id, [FromBody] UpdateBatchRequest request)
    {
        var batch = await _dbContext.InventoryBatches.FirstOrDefaultAsync(b => b.BatchId == id);
        if (batch == null)
            return NotFound(new { success = false, message = "Không tìm thấy lô hàng." });

        var allowedStatuses = new[] { "active", "depleted", "expired" };
        var status = request.Status?.Trim().ToLower() ?? "active";
        if (!allowedStatuses.Contains(status))
            return BadRequest(new { success = false, message = "Trạng thái lô không hợp lệ (hỗ trợ: active, depleted, expired)." });

        if (request.ExpiryDate == default)
            return BadRequest(new { success = false, message = "Hạn sử dụng của lô là bắt buộc." });

        if (batch.BatchCode.StartsWith("TH-BEP-", StringComparison.Ordinal)
            && request.ExpiryDate.Date != batch.ExpiryDate.Date)
            return BadRequest(new { success = false, message = "Lô hoàn bếp chỉ được cấp phát đến hết ngày hôm sau; không được đổi hạn." });

        if (request.ManufacturingDate.HasValue && request.ManufacturingDate.Value.Date > request.ExpiryDate.Date)
            return BadRequest(new { success = false, message = "Ngày sản xuất không được sau hạn sử dụng." });

        if (status == "active" && (request.ExpiryDate.Date < DateTime.UtcNow.Date || batch.QuantityRemaining <= 0))
            return BadRequest(new { success = false, message = "Lô active phải còn tồn và chưa hết hạn." });

        if (status == "depleted" && batch.QuantityRemaining > 0)
            return BadRequest(new { success = false, message = "Chỉ được đánh dấu depleted khi số lượng lô đã bằng 0." });

        batch.ManufacturingDate = request.ManufacturingDate;
        batch.ExpiryDate = request.ExpiryDate;
        batch.Status = status;
        batch.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Cập nhật thông tin lô hàng thành công.",
            data = batch
        });
    }

    // ==============================================================
    // 4. ĐỊNH LƯỢNG MÓN ĂN & BIẾN THỂ (BOM - BILL OF MATERIALS)
    // ==============================================================

    [HttpGet("boms")]
    [HttpGet("recipes")]
    public async Task<IActionResult> GetBomsList([FromQuery] int? categoryId = null, [FromQuery] string? search = null)
    {
        var query = _dbContext.Foods
            .Include(f => f.Category)
            .Include(f => f.FoodVariants.Where(v => v.IsActive))
                .ThenInclude(v => v.FoodIngredients)
                    .ThenInclude(fi => fi.InventoryItem)
            .Include(f => f.FoodIngredients.Where(fi => fi.VariantId == null))
                .ThenInclude(fi => fi.InventoryItem)
            .Where(f => f.IsActive)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(f => f.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(f => f.FoodName.ToLower().Contains(s));
        }

        var foods = await query
            .OrderBy(f => f.CategoryId)
            .ThenBy(f => f.FoodName)
            .ToListAsync();

        var result = foods.Select(f =>
        {
            var variantsBom = f.FoodVariants.Select(v =>
            {
                decimal estimatedCost = v.FoodIngredients.Sum(fi => InventoryCostHelper.CalculateAmount(
                    fi.QuantityPerUnit,
                    fi.InventoryItem.CostPerUnit,
                    fi.InventoryItem.ConversionRate));
                decimal grossProfitMargin = v.Price > 0 ? Math.Round((v.Price - estimatedCost) / v.Price * 100m, 1) : 0;

                return new
                {
                    v.VariantId,
                    v.VariantName,
                    v.Price,
                    v.Unit,
                    IngredientCount = v.FoodIngredients.Count,
                    EstimatedCost = estimatedCost,
                    GrossProfitMargin = grossProfitMargin,
                    Ingredients = v.FoodIngredients.Select(fi => new
                    {
                        fi.FoodIngredientId,
                        fi.InventoryItemId,
                        fi.InventoryItem.ItemName,
                        BaseUnit = fi.InventoryItem.BaseUnit,
                        fi.QuantityPerUnit,
                        WastagePercent = fi.WastagePercent,
                        fi.InventoryItem.CostPerUnit,
                        CostPerPurchaseUnit = fi.InventoryItem.CostPerUnit,
                        CostPerBaseUnit = InventoryCostHelper.ToBaseUnitCost(fi.InventoryItem.CostPerUnit, fi.InventoryItem.ConversionRate),
                        LineCost = InventoryCostHelper.CalculateAmount(fi.QuantityPerUnit, fi.InventoryItem.CostPerUnit, fi.InventoryItem.ConversionRate)
                    })
                };
            }).ToList();

            // Nếu món có định mức chung không gắn variant
            var defaultIngredients = f.FoodIngredients.Where(fi => fi.VariantId == null).Select(fi => new
            {
                fi.FoodIngredientId,
                fi.InventoryItemId,
                fi.InventoryItem.ItemName,
                BaseUnit = fi.InventoryItem.BaseUnit,
                fi.QuantityPerUnit,
                WastagePercent = fi.WastagePercent,
                fi.InventoryItem.CostPerUnit,
                CostPerPurchaseUnit = fi.InventoryItem.CostPerUnit,
                CostPerBaseUnit = InventoryCostHelper.ToBaseUnitCost(fi.InventoryItem.CostPerUnit, fi.InventoryItem.ConversionRate),
                LineCost = InventoryCostHelper.CalculateAmount(fi.QuantityPerUnit, fi.InventoryItem.CostPerUnit, fi.InventoryItem.ConversionRate)
            }).ToList();

            decimal defaultEstimatedCost = defaultIngredients.Sum(i => i.LineCost);

            return new
            {
                f.FoodId,
                f.FoodName,
                CategoryId = f.CategoryId,
                CategoryName = f.Category.CategoryName,
                f.MinPrice,
                f.FoodStatus,
                f.ImageUrl,
                HasVariants = f.FoodVariants.Count > 0,
                Variants = variantsBom,
                DefaultIngredients = defaultIngredients,
                DefaultEstimatedCost = defaultEstimatedCost
            };
        });

        return Ok(new { success = true, data = result });
    }

    [HttpGet("boms/{foodId}")]
    [HttpGet("recipes/{foodId}")]
    public async Task<IActionResult> GetBomByFoodId(int foodId, [FromQuery] int? variantId = null)
    {
        var food = await _dbContext.Foods
            .Include(f => f.Category)
            .Include(f => f.FoodVariants.Where(v => v.IsActive))
            .FirstOrDefaultAsync(f => f.FoodId == foodId);

        if (food == null)
            return NotFound(new { success = false, message = "Không tìm thấy món ăn này." });

        var query = _dbContext.FoodIngredients
            .Include(fi => fi.InventoryItem)
            .Include(fi => fi.Variant)
            .Where(fi => fi.FoodId == foodId);

        if (variantId.HasValue)
        {
            if (!food.FoodVariants.Any(v => v.VariantId == variantId.Value))
                return BadRequest(new { success = false, message = "Biến thể không thuộc món ăn này hoặc đã bị vô hiệu hóa." });

            query = query.Where(fi => fi.VariantId == variantId.Value);
        }

        var ingredients = await query.ToListAsync();

        var variants = variantId.HasValue
            ? food.FoodVariants.Where(v => v.VariantId == variantId.Value)
            : food.FoodVariants;

        var grouped = variants.Select(v =>
        {
            var items = ingredients.Where(i => i.VariantId == v.VariantId).Select(fi => new
            {
                fi.FoodIngredientId,
                fi.InventoryItemId,
                fi.InventoryItem.ItemName,
                BaseUnit = fi.InventoryItem.BaseUnit,
                PurchaseUnit = fi.InventoryItem.PurchaseUnit,
                ConversionRate = fi.InventoryItem.ConversionRate,
                fi.QuantityPerUnit,
                WastagePercent = fi.WastagePercent,
                fi.InventoryItem.CurrentQuantity,
                fi.InventoryItem.CostPerUnit,
                CostPerPurchaseUnit = fi.InventoryItem.CostPerUnit,
                CostPerBaseUnit = InventoryCostHelper.ToBaseUnitCost(fi.InventoryItem.CostPerUnit, fi.InventoryItem.ConversionRate),
                LineCost = InventoryCostHelper.CalculateAmount(fi.QuantityPerUnit, fi.InventoryItem.CostPerUnit, fi.InventoryItem.ConversionRate)
            }).ToList();

            decimal cost = items.Sum(i => i.LineCost);

            return new
            {
                v.VariantId,
                v.VariantName,
                v.Price,
                EstimatedCost = cost,
                GrossProfitMargin = v.Price > 0 ? Math.Round((v.Price - cost) / v.Price * 100m, 1) : 0,
                Ingredients = items
            };
        }).ToList();

        return Ok(new
        {
            success = true,
            data = new
            {
                food.FoodId,
                food.FoodName,
                CategoryName = food.Category.CategoryName,
                food.MinPrice,
                Variants = grouped
            }
        });
    }

    [Authorize(Roles = "admin,manager")]
    [HttpPost("boms/{foodId}")]
    [HttpPost("recipes/{foodId}")]
    public async Task<IActionResult> SaveBom(int foodId, [FromBody] SaveBomRequest request)
    {
        var food = await _dbContext.Foods.FirstOrDefaultAsync(f => f.FoodId == foodId && f.IsActive);
        if (food == null)
            return NotFound(new { success = false, message = "Không tìm thấy món ăn này." });

        if (request == null || request.Ingredients == null)
            return BadRequest(new { success = false, message = "Danh sách nguyên liệu không hợp lệ." });

        var duplicateInventoryItemIds = request.Ingredients
            .GroupBy(item => item.InventoryItemId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateInventoryItemIds.Count > 0)
        {
            return BadRequest(new
            {
                success = false,
                message = $"BOM không được lặp nguyên liệu: {string.Join(", ", duplicateInventoryItemIds)}."
            });
        }

        // Validate VariantId: Phải thuộc đúng FoodId theo handoff
        if (request.VariantId.HasValue)
        {
            var variant = await _dbContext.FoodVariants.FirstOrDefaultAsync(v => v.VariantId == request.VariantId.Value && v.FoodId == foodId);
            if (variant == null)
            {
                return BadRequest(new { success = false, message = $"Biến thể #{request.VariantId} không thuộc về món ăn '{food.FoodName}'." });
            }
        }

        // Validate items: QuantityPerUnit > 0 và 0 <= WastagePercent <= 100
        foreach (var item in request.Ingredients)
        {
            if (item.QuantityPerUnit <= 0)
                return BadRequest(new { success = false, message = $"Định lượng nguyên liệu #{item.InventoryItemId} phải lớn hơn 0." });

            if (item.WastagePercent < 0 || item.WastagePercent > 100)
                return BadRequest(new { success = false, message = $"Tỷ lệ hao hụt ({item.WastagePercent}%) phải nằm trong khoảng từ 0% đến 100%." });

            var inventoryItem = await _dbContext.InventoryItems
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InventoryItemId == item.InventoryItemId && i.IsActive);
            if (inventoryItem == null)
                return BadRequest(new { success = false, message = $"Nguyên liệu #{item.InventoryItemId} không tồn tại hoặc đã ngừng sử dụng." });

            if (item.WastagePercent < 0 || item.WastagePercent >= 100)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Nguyên liệu '{inventoryItem.ItemName}' có tỷ lệ hao hụt không hợp lệ ({item.WastagePercent}%). Tỷ lệ phải từ 0 đến dưới 100%."
                });
            }
        }

        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            // Xóa định lượng cũ của (FoodId, VariantId) này
            var existing = await _dbContext.FoodIngredients
                .Where(fi => fi.FoodId == foodId && fi.VariantId == request.VariantId)
                .ToListAsync();

            _dbContext.FoodIngredients.RemoveRange(existing);
            await _dbContext.SaveChangesAsync();

            var now = DateTime.UtcNow;

            // Thêm mới các dòng định mức
            foreach (var item in request.Ingredients)
            {
                _dbContext.FoodIngredients.Add(new FoodIngredient
                {
                    FoodId = foodId,
                    VariantId = request.VariantId,
                    InventoryItemId = item.InventoryItemId,
                    QuantityPerUnit = item.QuantityPerUnit,
                    WastagePercent = item.WastagePercent,
                    CreatedAt = now
                });
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                success = true,
                message = $"Đã lưu định mức BOM ({request.Ingredients.Count} nguyên liệu) cho món '{food.FoodName}' thành công."
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { success = false, message = "Lỗi khi lưu định mức: " + ex.Message });
        }
    }

    [Authorize(Roles = "admin,manager")]
    [HttpDelete("boms/{foodIngredientId}")]
    public async Task<IActionResult> DeleteBomItem(int foodIngredientId)
    {
        var item = await _dbContext.FoodIngredients
            .Include(fi => fi.Food)
            .Include(fi => fi.InventoryItem)
            .FirstOrDefaultAsync(fi => fi.FoodIngredientId == foodIngredientId);

        if (item == null)
            return NotFound(new { success = false, message = "Không tìm thấy dòng định mức này." });

        _dbContext.FoodIngredients.Remove(item);
        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = $"Đã xóa nguyên liệu '{item.InventoryItem.ItemName}' khỏi công thức món '{item.Food.FoodName}'."
        });
    }

    // ==============================================================
    // 5. CẢNH BÁO TỒN KHO & BÁO CÁO NHANH CHO DASHBOARD
    // ==============================================================

    [HttpGet("alerts")]
    public async Task<IActionResult> GetLowStockAlerts()
    {
        var lowStockItems = await _dbContext.InventoryItems
            .Include(i => i.Supplier)
            .Where(i => i.IsActive && i.CurrentQuantity <= i.MinimumQuantity)
            .OrderBy(i => i.CurrentQuantity)
            .Select(i => new
            {
                i.InventoryItemId,
                i.ItemName,
                Unit = i.BaseUnit,
                i.BaseUnit,
                i.PurchaseUnit,
                i.ConversionRate,
                i.CurrentQuantity,
                i.MinimumQuantity,
                i.CostPerUnit,
                Shortage = i.MinimumQuantity - i.CurrentQuantity,
                SupplierName = i.Supplier != null ? i.Supplier.SupplierName : "Chưa có",
                SupplierPhone = i.Supplier != null ? i.Supplier.Phone : null
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            count = lowStockItems.Count,
            data = lowStockItems
        });
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetInventorySummary()
    {
        var totalItems = await _dbContext.InventoryItems.CountAsync(i => i.IsActive);
        var lowStockCount = await _dbContext.InventoryItems.CountAsync(i => i.IsActive && i.CurrentQuantity <= i.MinimumQuantity);
        var today = DateTime.UtcNow.AddHours(7).Date;
        var totalBatches = await _dbContext.InventoryBatches.CountAsync(b => b.Status == "active" && b.QuantityRemaining > 0 && b.ExpiryDate.Date >= today);
        var totalSuppliers = await _dbContext.Suppliers.CountAsync(s => s.IsActive);

        var totalValue = await _dbContext.InventoryBatches
            .Where(b => b.QuantityRemaining > 0 && b.InventoryItem.IsActive)
            .SumAsync(b => (decimal?)(b.InventoryItem.ConversionRate > 0
                ? (b.QuantityRemaining / b.InventoryItem.ConversionRate) * b.UnitCost
                : b.QuantityRemaining * b.UnitCost)) ?? 0;
        totalValue += await _dbContext.InventoryItems
            .Where(i => i.IsActive && !i.InventoryBatches.Any(b => b.QuantityRemaining > 0))
            .SumAsync(i => (decimal?)(i.ConversionRate > 0
                ? (i.CurrentQuantity / i.ConversionRate) * i.CostPerUnit
                : i.CurrentQuantity * i.CostPerUnit)) ?? 0;

        var expiredBatchesCount = await _dbContext.InventoryBatches
            .CountAsync(b => b.QuantityRemaining > 0 && b.ExpiryDate.Date < today);
        var expiringBatchesCount = await _dbContext.InventoryBatches
            .CountAsync(b => b.Status == "active" && b.QuantityRemaining > 0 && b.ExpiryDate.Date >= today && b.ExpiryDate.Date <= today.AddDays(7));

        var recentMovements = await _dbContext.StockMovements
            .Include(m => m.InventoryItem)
            .Include(m => m.CreatedByNavigation)
            .OrderByDescending(m => m.CreatedAt)
            .Take(5)
            .Select(m => new
            {
                m.StockMovementId,
                ItemName = m.InventoryItem.ItemName,
                BaseUnit = m.InventoryItem.BaseUnit,
                m.MovementType,
                m.Quantity,
                m.UnitCost,
                m.Reason,
                CreatedByName = m.CreatedByNavigation.FullName,
                m.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                totalItems,
                lowStockCount,
                totalBatches,
                totalSuppliers,
                totalValue,
                expiredBatchesCount,
                expiringBatchesCount,
                recentMovements
            }
        });
    }

    // ==============================================================
    // 6. LỊCH SỬ BIẾN ĐỘNG KHO (STOCK MOVEMENTS - AUDIT LOGS)
    // ==============================================================

    [HttpGet("movements")]
    public async Task<IActionResult> GetStockMovements(
        [FromQuery] int? itemId = null,
        [FromQuery] int? batchId = null,
        [FromQuery] string? type = null,
        [FromQuery] string? fromDate = null,
        [FromQuery] string? toDate = null)
    {
        var query = _dbContext.StockMovements
            .Include(m => m.InventoryItem)
            .Include(m => m.Batch)
            .Include(m => m.CreatedByNavigation)
            .AsQueryable();

        if (itemId.HasValue)
        {
            query = query.Where(m => m.InventoryItemId == itemId.Value);
        }

        if (batchId.HasValue)
        {
            query = query.Where(m => m.BatchId == batchId.Value);
        }

        if (!string.IsNullOrWhiteSpace(type) && type != "all")
        {
            var t = type.Trim().ToLower();
            query = query.Where(m => m.MovementType.ToLower() == t);
        }

        if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParse(fromDate, out var from))
        {
            query = query.Where(m => m.CreatedAt >= from.Date);
        }

        if (!string.IsNullOrWhiteSpace(toDate) && DateTime.TryParse(toDate, out var to))
        {
            query = query.Where(m => m.CreatedAt < to.Date.AddDays(1));
        }

        var movements = await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(100)
            .Select(m => new
            {
                m.StockMovementId,
                m.InventoryItemId,
                ItemName = m.InventoryItem.ItemName,
                BaseUnit = m.InventoryItem.BaseUnit,
                m.BatchId,
                BatchCode = m.Batch != null ? m.Batch.BatchCode : null,
                m.MovementType,
                m.Quantity,
                m.UnitCost,
                UnitCostPerPurchaseUnit = m.UnitCost ?? m.InventoryItem.CostPerUnit,
                UnitCostPerBaseUnit = m.InventoryItem.ConversionRate > 0
                    ? (m.UnitCost ?? m.InventoryItem.CostPerUnit) / m.InventoryItem.ConversionRate
                    : (m.UnitCost ?? m.InventoryItem.CostPerUnit),
                TotalAmount = m.InventoryItem.ConversionRate > 0
                    ? (m.Quantity / m.InventoryItem.ConversionRate) * (m.UnitCost ?? m.InventoryItem.CostPerUnit)
                    : m.Quantity * (m.UnitCost ?? m.InventoryItem.CostPerUnit),
                m.Reason,
                m.ReferenceType,
                m.ReferenceId,
                CreatedBy = m.CreatedBy,
                CreatedByName = m.CreatedByNavigation.FullName,
                m.CreatedAt
            })
            .ToListAsync();

        return Ok(new { success = true, data = movements });
    }

    private static bool CanUseWastage(string? baseUnit)
    {
        return !string.IsNullOrWhiteSpace(baseUnit);
    }
}
