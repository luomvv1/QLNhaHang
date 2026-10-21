using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;
using System.Text.Json;
using Restaurant.Api.Hubs;
using System.Security.Claims;
using System.Data;

namespace Restaurant.Api.Controllers.Admin;

// ==============================================================
// DTOs CHO HẠNG MỤC 3: PHIẾU KHO & DUYỆT PHIẾU (STOCK RECEIPTS)
// ==============================================================

public class ReceiptDetailItemInput
{
    public int InventoryItemId { get; set; }
    public decimal WastagePercent { get; set; } = 0;
    public decimal? ActualUsableQuantity { get; set; } // Lượng thực nhận theo BaseUnit sau sơ chế
    public string? AdjustmentNote { get; set; }
    public int? BatchId { get; set; } // Null nếu muốn hệ thống tự động xuất FEFO khi duyệt
    public decimal Quantity { get; set; } // Số lượng tính theo BaseUnit
    public decimal? UnitCost { get; set; } // Đơn giá theo PurchaseUnit (nếu null lấy CostPerUnit của Item)
    public string? Note { get; set; }

    // Thông tin tạo lô mới nếu là phiếu nhập từ NCC
    public string? NewBatchCode { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class CreateReceiptRequest
{
    public string ReceiptType { get; set; } = "export_kitchen"; // import_supplier, export_kitchen, return_from_kitchen, waste_disposal, audit_adjustment
    public int? SupplierId { get; set; }
    public string? Note { get; set; }
    public List<ReceiptDetailItemInput> Details { get; set; } = new();
}

public class UpdateReceiptRequest
{
    public int? SupplierId { get; set; }
    public string? Note { get; set; }
    public List<ReceiptDetailItemInput> Details { get; set; } = new();
}

public class QuickWasteItemInput
{
    public int InventoryItemId { get; set; }
    public int? BatchId { get; set; }
    public decimal Quantity { get; set; } // BaseUnit
    public string? Reason { get; set; }
}

public class QuickWasteRequest
{
    public string? Note { get; set; }
    public List<QuickWasteItemInput> Items { get; set; } = new();
}

// ==============================================================
// CONTROLLER: QUẢN LÝ PHIẾU KHO & DUYỆT PHIẾU FEFO/FIFO (HẠNG MỤC 3)
// ==============================================================

[Route("api/admin/inventory/receipts")]
[Authorize(Roles = "admin,manager,chef")]
[ApiController]
public class AdminStockReceiptController : ControllerBase
{
    private readonly RestaurantDbContext _dbContext;
    private readonly IHubContext<RestaurantHub> _hubContext;

    public AdminStockReceiptController(RestaurantDbContext dbContext, IHubContext<RestaurantHub> hubContext)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    private int? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out var userId) ? userId : null;
    }

    private static bool CanApplyPrepWastage(string? baseUnit)
    {
        return !string.IsNullOrWhiteSpace(baseUnit);
    }

    private static string? ScaleBomSnapshot(string? snapshotJson, decimal ratio)
    {
        if (string.IsNullOrWhiteSpace(snapshotJson) || ratio >= 0.999999m)
            return snapshotJson;

        try
        {
            var snapshot = JsonSerializer.Deserialize<List<StockReceiptBomSnapshotEntry>>(snapshotJson);
            if (snapshot == null) return snapshotJson;
            foreach (var entry in snapshot)
            {
                entry.Portions = decimal.Round(entry.Portions * ratio, 6, MidpointRounding.AwayFromZero);
            }
            return JsonSerializer.Serialize(snapshot);
        }
        catch (JsonException)
        {
            return snapshotJson;
        }
    }

    // ==============================================================
    // 1. DANH SÁCH PHIẾU KHO
    // ==============================================================
    [Authorize(Roles = "admin,manager")]
    [HttpGet]
    public async Task<IActionResult> GetReceipts(
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        [FromQuery] string? fromDate = null,
        [FromQuery] string? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _dbContext.StockReceipts
            .Include(r => r.Supplier)
            .Include(r => r.CreatedByNavigation)
            .Include(r => r.ApprovedByNavigation)
            .Include(r => r.StockReceiptDetails)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(type) && type != "all")
        {
            var t = type.Trim().ToLower();
            query = query.Where(r => r.ReceiptType.ToLower() == t);
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            var s = status.Trim().ToLower();
            query = query.Where(r => r.Status.ToLower() == s);
        }

        if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParse(fromDate, out var from))
        {
            query = query.Where(r => r.CreatedAt >= from.Date);
        }

        if (!string.IsNullOrWhiteSpace(toDate) && DateTime.TryParse(toDate, out var to))
        {
            query = query.Where(r => r.CreatedAt < to.Date.AddDays(1));
        }

        var totalCount = await query.CountAsync();

        if (pageSize <= 0) pageSize = 20;
        if (pageSize > 100) pageSize = 100;
        if (page <= 0) page = 1;

        var receipts = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.ReceiptId,
                r.ReceiptCode,
                r.ReceiptType,
                r.Status,
                r.SupplierId,
                SupplierName = r.Supplier != null ? r.Supplier.SupplierName : null,
                r.TotalAmount,
                r.Note,
                ItemCount = r.StockReceiptDetails.Count,
                CreatedBy = r.CreatedBy,
                CreatedByName = r.CreatedByNavigation.FullName,
                r.ApprovedBy,
                ApprovedByName = r.ApprovedByNavigation != null ? r.ApprovedByNavigation.FullName : null,
                r.ApprovedAt,
                r.CreatedAt,
                r.UpdatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            data = receipts
        });
    }

    // ==============================================================
    // 2. CHI TIẾT MỘT PHIẾU KHO
    // ==============================================================
    [Authorize(Roles = "admin,manager")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetReceiptById(int id)
    {
        var receipt = await _dbContext.StockReceipts
            .Include(r => r.Supplier)
            .Include(r => r.CreatedByNavigation)
            .Include(r => r.ApprovedByNavigation)
            .Include(r => r.StockReceiptDetails)
                .ThenInclude(d => d.InventoryItem)
            .Include(r => r.StockReceiptDetails)
                .ThenInclude(d => d.Batch)
            .FirstOrDefaultAsync(r => r.ReceiptId == id);

        if (receipt == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy phiếu kho này." });
        }

        var details = receipt.StockReceiptDetails.Select(d => new
        {
            d.DetailId,
            d.InventoryItemId,
            ItemName = d.InventoryItem.ItemName,
            BaseUnit = d.InventoryItem.BaseUnit,
            PurchaseUnit = d.InventoryItem.PurchaseUnit,
            ConversionRate = d.InventoryItem.ConversionRate,
            CurrentStock = d.InventoryItem.CurrentQuantity,
            d.BatchId,
            BatchCode = d.Batch != null ? d.Batch.BatchCode : null,
            ExpiryDate = d.Batch != null ? d.Batch.ExpiryDate : (DateTime?)null,
            d.Quantity, // Luong dung duoc theo BaseUnit
            d.GrossQuantity,
            GrossQuantityInPurchaseUnit = d.InventoryItem.ConversionRate > 0
                ? Math.Round((d.GrossQuantity ?? d.Quantity) / d.InventoryItem.ConversionRate, 2)
                : (d.GrossQuantity ?? d.Quantity),
            d.WastagePercent,
            d.WastageQuantity,
            d.ExpectedUsableQuantity,
            d.AdjustmentNote,
            QuantityInPurchaseUnit = d.InventoryItem.ConversionRate > 0 ? Math.Round(d.Quantity / d.InventoryItem.ConversionRate, 2) : d.Quantity,
            d.UnitCost,
            UnitCostPerPurchaseUnit = d.UnitCost,
            UnitCostPerBaseUnit = d.InventoryItem.ConversionRate > 0 ? d.UnitCost / d.InventoryItem.ConversionRate : d.UnitCost,
            d.Subtotal,
             d.Note,
             d.NewBatchCode,
             d.ManufacturingDate,
             RequestedExpiryDate = d.ExpiryDate
        }).ToList();

        return Ok(new
        {
            success = true,
            data = new
            {
                receipt.ReceiptId,
                receipt.ReceiptCode,
                receipt.ReceiptType,
                receipt.Status,
                receipt.SupplierId,
                SupplierName = receipt.Supplier?.SupplierName,
                receipt.TotalAmount,
                receipt.Note,
                receipt.CreatedBy,
                CreatedByName = receipt.CreatedByNavigation.FullName,
                receipt.ApprovedBy,
                ApprovedByName = receipt.ApprovedByNavigation?.FullName,
                receipt.ApprovedAt,
                receipt.CreatedAt,
                receipt.UpdatedAt,
                Details = details
            }
        });
    }

    // ==============================================================
    // 3. TẠO PHIẾU KHO NHÁP (DRAFT RECEIPT)
    // ==============================================================
    [Authorize(Roles = "admin,manager")]
    [HttpPost]
    public async Task<IActionResult> CreateReceipt([FromBody] CreateReceiptRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { success = false, message = "Không xác định được danh tính người dùng." });
        }

        var validTypes = new[] { "import_supplier", "export_kitchen", "waste_disposal", "audit_adjustment" };
        var type = request.ReceiptType.Trim().ToLower();
        if (!validTypes.Contains(type))
        {
            return BadRequest(new { success = false, message = $"Loại phiếu '{request.ReceiptType}' không hợp lệ." });
        }

        if (request.Details == null || request.Details.Count == 0)
        {
            return BadRequest(new { success = false, message = "Phiếu kho phải có ít nhất một mặt hàng." });
        }

        // Validate items
        var itemIds = request.Details.Select(d => d.InventoryItemId).Distinct().ToList();
        var itemsMap = await _dbContext.InventoryItems
            .Where(i => itemIds.Contains(i.InventoryItemId))
            .ToDictionaryAsync(i => i.InventoryItemId);

        foreach (var d in request.Details)
        {
            if (d.Quantity <= 0)
            {
                return BadRequest(new { success = false, message = "Số lượng mỗi mặt hàng phải lớn hơn 0." });
            }
            if (!itemsMap.ContainsKey(d.InventoryItemId))
            {
                return BadRequest(new { success = false, message = $"Nguyên liệu ID {d.InventoryItemId} không tồn tại." });
            }

            if (d.BatchId.HasValue)
            {
                var batch = await _dbContext.InventoryBatches
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.BatchId == d.BatchId.Value);

                if (batch == null || batch.InventoryItemId != d.InventoryItemId)
                    return BadRequest(new { success = false, message = $"Lô {d.BatchId} không thuộc nguyên liệu ID {d.InventoryItemId}." });
            }

            if (d.ManufacturingDate.HasValue && d.ExpiryDate.HasValue && d.ManufacturingDate.Value.Date > d.ExpiryDate.Value.Date)
                return BadRequest(new { success = false, message = "Ngày sản xuất không được sau hạn sử dụng." });
        }

        // Tạo mã phiếu theo loại và ngày
        string prefix = type switch
        {
            "import_supplier" => "PN",
            "export_kitchen" => "PX",
            "return_from_kitchen" => "TH",
            "waste_disposal" => "XH",
            "audit_adjustment" => "DC",
            _ => "PK"
        };

        var now = DateTime.UtcNow;
        var dateStr = now.ToString("yyyyMMdd");
        var randomSuffix = new Random().Next(1000, 9999);
        var receiptCode = $"{prefix}-{dateStr}-{randomSuffix}";

        // Đảm bảo không trùng mã
        while (await _dbContext.StockReceipts.AnyAsync(r => r.ReceiptCode == receiptCode))
        {
            randomSuffix = new Random().Next(1000, 9999);
            receiptCode = $"{prefix}-{dateStr}-{randomSuffix}";
        }

        var receipt = new StockReceipt
        {
            ReceiptCode = receiptCode,
            ReceiptType = type,
            Status = "draft",
            SupplierId = request.SupplierId,
            Note = request.Note,
            CreatedBy = userId.Value,
            CreatedAt = now,
            UpdatedAt = now
        };

        decimal totalAmount = 0;

        foreach (var d in request.Details)
        {
            var item = itemsMap[d.InventoryItemId];
            decimal unitCost = d.UnitCost ?? item.CostPerUnit;
            var wastagePercent = type == "import_supplier"
                ? (d.WastagePercent > 0 ? d.WastagePercent : item.PrepWastagePercent)
                : 0m;
            if (wastagePercent < 0 || wastagePercent >= 100)
                return BadRequest(new { success = false, message = $"Tỷ lệ hao hụt của '{item.ItemName}' phải từ 0 đến dưới 100%." });
            var grossQuantity = d.Quantity;
            var expectedUsableQuantity = Math.Round(grossQuantity * (1m - wastagePercent / 100m), 4);
            var usableQuantity = type == "import_supplier" ? d.ActualUsableQuantity ?? expectedUsableQuantity : d.Quantity;
            if (type == "import_supplier" && (usableQuantity <= 0 || usableQuantity > grossQuantity))
                return BadRequest(new { success = false, message = $"Lượng thực nhận sau sơ chế của '{item.ItemName}' phải lớn hơn 0 và không vượt lượng mua." });
            var wastageQuantity = type == "import_supplier" ? grossQuantity - usableQuantity : 0m;
            var adjustmentNote = d.AdjustmentNote?.Trim();
            if (type == "import_supplier" && usableQuantity < expectedUsableQuantity - 0.0001m && string.IsNullOrWhiteSpace(adjustmentNote))
                return BadRequest(new { success = false, message = $"'{item.ItemName}' hao hụt thực tế vượt dự kiến. Vui lòng ghi lý do điều chỉnh." });
            if (usableQuantity <= 0)
                return BadRequest(new { success = false, message = $"Lượng dùng được của '{item.ItemName}' phải lớn hơn 0." });
            decimal subtotal = InventoryCostHelper.CalculateAmount(type == "import_supplier" ? grossQuantity : d.Quantity, unitCost, item.ConversionRate);
            totalAmount += subtotal;

            var detail = new StockReceiptDetail
            {
                InventoryItemId = d.InventoryItemId,
                BatchId = d.BatchId,
                Quantity = type == "import_supplier" ? usableQuantity : d.Quantity,
                GrossQuantity = type == "import_supplier" ? grossQuantity : d.Quantity,
                WastagePercent = wastagePercent,
                WastageQuantity = type == "import_supplier" ? wastageQuantity : 0m,
                ExpectedUsableQuantity = type == "import_supplier" ? expectedUsableQuantity : null,
                AdjustmentNote = string.IsNullOrWhiteSpace(adjustmentNote) ? null : adjustmentNote,
                UnitCost = unitCost,
                Subtotal = subtotal,
                Note = d.Note,
                NewBatchCode = string.IsNullOrWhiteSpace(d.NewBatchCode) ? null : d.NewBatchCode.Trim(),
                ManufacturingDate = d.ManufacturingDate?.Date,
                ExpiryDate = d.ExpiryDate?.Date
            };
            receipt.StockReceiptDetails.Add(detail);
        }

        receipt.TotalAmount = totalAmount;

        _dbContext.StockReceipts.Add(receipt);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetReceiptById), new { id = receipt.ReceiptId }, new
        {
            success = true,
            message = $"Đã tạo phiếu kho '{receipt.ReceiptCode}' (nháp) thành công.",
            receiptId = receipt.ReceiptId,
            receiptCode = receipt.ReceiptCode
        });
    }

    // ==============================================================
    // 4. CẬP NHẬT PHIẾU KHO NHÁP
    // ==============================================================
    [Authorize(Roles = "admin,manager")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateReceipt(int id, [FromBody] UpdateReceiptRequest request)
    {
        var receipt = await _dbContext.StockReceipts
            .Include(r => r.StockReceiptDetails)
            .FirstOrDefaultAsync(r => r.ReceiptId == id);

        if (receipt == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy phiếu kho." });
        }

        if (receipt.Status != "draft")
        {
            return BadRequest(new { success = false, message = $"Không thể chỉnh sửa phiếu ở trạng thái '{receipt.Status}'. Chỉ cho phép sửa phiếu nháp (draft)." });
        }

        if (request.Details == null || request.Details.Count == 0)
        {
            return BadRequest(new { success = false, message = "Phiếu kho phải có ít nhất một mặt hàng." });
        }

        var itemIds = request.Details.Select(d => d.InventoryItemId).Distinct().ToList();
        var itemsMap = await _dbContext.InventoryItems
            .Where(i => itemIds.Contains(i.InventoryItemId))
            .ToDictionaryAsync(i => i.InventoryItemId);

        foreach (var d in request.Details)
        {
            if (d.Quantity <= 0)
            {
                return BadRequest(new { success = false, message = "Số lượng mỗi mặt hàng phải lớn hơn 0." });
            }
            if (!itemsMap.ContainsKey(d.InventoryItemId))
            {
                return BadRequest(new { success = false, message = $"Nguyên liệu ID {d.InventoryItemId} không tồn tại." });
            }

            if (d.BatchId.HasValue)
            {
                var batch = await _dbContext.InventoryBatches
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.BatchId == d.BatchId.Value);

                if (batch == null || batch.InventoryItemId != d.InventoryItemId)
                    return BadRequest(new { success = false, message = $"Lô {d.BatchId} không thuộc nguyên liệu ID {d.InventoryItemId}." });
            }

            if (d.ManufacturingDate.HasValue && d.ExpiryDate.HasValue && d.ManufacturingDate.Value.Date > d.ExpiryDate.Value.Date)
                return BadRequest(new { success = false, message = "Ngày sản xuất không được sau hạn sử dụng." });
        }

        // Xóa chi tiết cũ và gán chi tiết mới
        _dbContext.StockReceiptDetails.RemoveRange(receipt.StockReceiptDetails);

        decimal totalAmount = 0;
        foreach (var d in request.Details)
        {
            var item = itemsMap[d.InventoryItemId];
            decimal unitCost = d.UnitCost ?? item.CostPerUnit;
            var wastagePercent = receipt.ReceiptType == "import_supplier"
                ? (d.WastagePercent > 0 ? d.WastagePercent : item.PrepWastagePercent)
                : 0m;
            if (wastagePercent < 0 || wastagePercent >= 100)
                return BadRequest(new { success = false, message = $"Tỷ lệ hao hụt của '{item.ItemName}' phải từ 0 đến dưới 100%." });
            var grossQuantity = d.Quantity;
            var expectedUsableQuantity = Math.Round(grossQuantity * (1m - wastagePercent / 100m), 4);
            var usableQuantity = receipt.ReceiptType == "import_supplier" ? d.ActualUsableQuantity ?? expectedUsableQuantity : d.Quantity;
            if (receipt.ReceiptType == "import_supplier" && (usableQuantity <= 0 || usableQuantity > grossQuantity))
                return BadRequest(new { success = false, message = $"Lượng thực nhận sau sơ chế của '{item.ItemName}' phải lớn hơn 0 và không vượt lượng mua." });
            var wastageQuantity = receipt.ReceiptType == "import_supplier" ? grossQuantity - usableQuantity : 0m;
            var adjustmentNote = d.AdjustmentNote?.Trim();
            if (receipt.ReceiptType == "import_supplier" && usableQuantity < expectedUsableQuantity - 0.0001m && string.IsNullOrWhiteSpace(adjustmentNote))
                return BadRequest(new { success = false, message = $"'{item.ItemName}' hao hụt thực tế vượt dự kiến. Vui lòng ghi lý do điều chỉnh." });
            if (usableQuantity <= 0)
                return BadRequest(new { success = false, message = $"Lượng dùng được của '{item.ItemName}' phải lớn hơn 0." });
            decimal subtotal = InventoryCostHelper.CalculateAmount(receipt.ReceiptType == "import_supplier" ? grossQuantity : d.Quantity, unitCost, item.ConversionRate);
            totalAmount += subtotal;

            receipt.StockReceiptDetails.Add(new StockReceiptDetail
            {
                ReceiptId = receipt.ReceiptId,
                InventoryItemId = d.InventoryItemId,
                BatchId = d.BatchId,
                Quantity = receipt.ReceiptType == "import_supplier" ? usableQuantity : d.Quantity,
                GrossQuantity = receipt.ReceiptType == "import_supplier" ? grossQuantity : d.Quantity,
                WastagePercent = wastagePercent,
                WastageQuantity = receipt.ReceiptType == "import_supplier" ? wastageQuantity : 0m,
                ExpectedUsableQuantity = receipt.ReceiptType == "import_supplier" ? expectedUsableQuantity : null,
                AdjustmentNote = string.IsNullOrWhiteSpace(adjustmentNote) ? null : adjustmentNote,
                UnitCost = unitCost,
                Subtotal = subtotal,
                Note = d.Note,
                NewBatchCode = string.IsNullOrWhiteSpace(d.NewBatchCode) ? null : d.NewBatchCode.Trim(),
                ManufacturingDate = d.ManufacturingDate?.Date,
                ExpiryDate = d.ExpiryDate?.Date
            });
        }

        receipt.SupplierId = request.SupplierId ?? receipt.SupplierId;
        receipt.Note = request.Note;
        receipt.TotalAmount = totalAmount;
        receipt.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = $"Đã cập nhật phiếu kho '{receipt.ReceiptCode}' thành công.",
            receiptId = receipt.ReceiptId
        });
    }

    // ==============================================================
    // 5. HỦY PHIẾU KHO NHÁP
    // ==============================================================
    [Authorize(Roles = "admin,manager")]
    [HttpDelete("{id}")]
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelReceipt(int id)
    {
        var receipt = await _dbContext.StockReceipts.FindAsync(id);
        if (receipt == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy phiếu kho." });
        }

        if (receipt.Status != "draft")
        {
            return BadRequest(new { success = false, message = $"Chỉ có thể hủy phiếu đang ở trạng thái 'draft'. Phiếu này đang là '{receipt.Status}'." });
        }

        receipt.Status = "cancelled";
        receipt.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        try
        {
            await _hubContext.Clients.All.SendAsync("ReceiptStatusChanged", new { receiptId = id, status = "cancelled" });
        }
        catch { }

        return Ok(new { success = true, message = $"Đã hủy phiếu kho '{receipt.ReceiptCode}'." });
    }

    // ==============================================================
    // 6. DUYỆT PHIẾU KHO (APPROVE RECEIPT WITH TRANSACTION + FEFO/FIFO)
    // ==============================================================
    [Authorize(Roles = "admin,manager")]
    [HttpPost("{id}/approve")]
    [HttpPut("{id}/approve")]
    public async Task<IActionResult> ApproveReceipt(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { success = false, message = "Không xác định được danh tính người phê duyệt." });
        }

        // Bắt đầu giao dịch với mức cô lập Serializable để đảm bảo tính toàn vẹn tồn kho tuyệt đối
        using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            // 1. Kiểm tra và khóa phiếu kho
            var receipt = await _dbContext.StockReceipts
                .Include(r => r.StockReceiptDetails)
                    .ThenInclude(d => d.InventoryItem)
                .Include(r => r.StockReceiptDetails)
                    .ThenInclude(d => d.Batch)
                .FirstOrDefaultAsync(r => r.ReceiptId == id);

            if (receipt == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy phiếu kho cần duyệt." });
            }

            if (receipt.Status == "approved")
            {
                return Conflict(new { success = false, message = $"Phiếu kho '{receipt.ReceiptCode}' đã được duyệt trước đó rồi." });
            }

            if (receipt.Status == "cancelled")
            {
                return BadRequest(new { success = false, message = $"Phiếu kho '{receipt.ReceiptCode}' đã bị hủy, không thể duyệt." });
            }

            if (receipt.StockReceiptDetails.Count == 0)
            {
                return BadRequest(new { success = false, message = "Phiếu kho không có chi tiết mặt hàng nào để duyệt." });
            }

            var now = DateTime.UtcNow;
            var detailsToProcess = receipt.StockReceiptDetails.ToList();

            if (receipt.ReceiptType == "return_from_kitchen")
            {
                await transaction.RollbackAsync();
                return BadRequest(new { success = false, message = "Hoan kho phai thuc hien qua doi soat phieu xuat bep." });
            }

            // 2. Xử lý nghiệp vụ theo từng loại phiếu
            switch (receipt.ReceiptType)
            {
                // ------------------------------------------------------
                // CASE A: XUẤT KHO CHO BẾP HOẶC XUẤT HỦY (FEFO ALLOCATION)
                // ------------------------------------------------------
                case "export_kitchen":
                case "waste_disposal":
                {
                    string movementType = receipt.ReceiptType == "waste_disposal" ? "waste" : "export";
                    string movementReason = receipt.Note ?? (receipt.ReceiptType == "waste_disposal" ? "Xuất hủy nguyên liệu" : "Xuất kho cho bếp chế biến");

                    // Gom tổng số lượng yêu cầu theo từng nguyên liệu
                    var itemGroups = detailsToProcess.GroupBy(d => d.InventoryItemId).ToList();

                    foreach (var group in itemGroups)
                    {
                        var itemId = group.Key;
                        var totalRequired = group.Sum(d => d.Quantity);

                        var item = await _dbContext.InventoryItems
                            .FirstOrDefaultAsync(i => i.InventoryItemId == itemId);

                        if (item == null)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest(new { success = false, message = $"Không tìm thấy nguyên liệu ID {itemId}." });
                        }

                        // Kiểm tra tổng tồn kho trước
                        if (item.CurrentQuantity < totalRequired)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest(new
                            {
                                success = false,
                                message = $"Không đủ tồn kho cho '{item.ItemName}'. Yêu cầu: {totalRequired:N2} {item.BaseUnit}, Tồn hiện tại: {item.CurrentQuantity:N2} {item.BaseUnit}."
                            });
                        }

                        // Xử lý từng detail trong group
                        foreach (var detail in group)
                        {
                            if (detail.BatchId.HasValue)
                            {
                                // A1: Người dùng chỉ định lô cụ thể
                                var batch = await _dbContext.InventoryBatches
                                    .FirstOrDefaultAsync(b => b.BatchId == detail.BatchId.Value);

                                if (batch == null)
                                {
                                    await transaction.RollbackAsync();
                                    return BadRequest(new { success = false, message = $"Lô ID {detail.BatchId} của '{item.ItemName}' không tồn tại." });
                                }

                                if (batch.InventoryItemId != itemId)
                                {
                                    await transaction.RollbackAsync();
                                    return BadRequest(new { success = false, message = $"Lô '{batch.BatchCode}' không thuộc nguyên liệu '{item.ItemName}'." });
                                }

                                if (batch.Status is not ("active" or "expired") || batch.QuantityRemaining <= 0
                                    || (receipt.ReceiptType == "export_kitchen" && batch.Status != "active"))
                                {
                                    await transaction.RollbackAsync();
                                    return BadRequest(new { success = false, message = $"Lô '{batch.BatchCode}' không còn khả dụng để xuất." });
                                }

                                if (receipt.ReceiptType == "export_kitchen" && batch.ExpiryDate.Date < now.AddHours(7).Date)
                                {
                                    await transaction.RollbackAsync();
                                    return BadRequest(new { success = false, message = $"Không thể xuất bếp từ lô '{batch.BatchCode}' đã hết hạn." });
                                }

                                if (batch.QuantityRemaining < detail.Quantity)
                                {
                                    await transaction.RollbackAsync();
                                    return BadRequest(new
                                    {
                                        success = false,
                                        message = $"Lô '{batch.BatchCode}' của '{item.ItemName}' không đủ tồn. Yêu cầu: {detail.Quantity:N2}, Còn: {batch.QuantityRemaining:N2} {item.BaseUnit}."
                                    });
                                }

                                batch.QuantityRemaining -= detail.Quantity;
                                if (batch.QuantityRemaining <= 0)
                                {
                                    batch.QuantityRemaining = 0;
                                    batch.Status = "depleted";
                                }
                                batch.UpdatedAt = now;

                                // Ghi nhận movement
                                var movement = new StockMovement
                                {
                                    InventoryItemId = itemId,
                                    BatchId = batch.BatchId,
                                    MovementType = movementType,
                                    Quantity = detail.Quantity,
                                    UnitCost = batch.UnitCost,
                                    Reason = movementReason,
                                    ReferenceType = "stock_receipt",
                                    ReferenceId = receipt.ReceiptId,
                                    CreatedBy = userId.Value,
                                    CreatedAt = now
                                };
                                _dbContext.StockMovements.Add(movement);
                            }
                            else
                            {
                                // A2: Thuật toán FEFO (First-Expired, First-Out) tự động
                                var requestedQuantity = detail.Quantity;
                                var sourceBomSnapshot = detail.BomSnapshotJson;
                                var availableBatches = await _dbContext.InventoryBatches
                                    .Where(b => b.InventoryItemId == itemId &&
                                                b.Status == "active" &&
                                                b.QuantityRemaining > 0 &&
                                                    (receipt.ReceiptType == "waste_disposal" || b.ExpiryDate >= now.AddHours(7).Date))
                                    .OrderByDescending(b => receipt.ReceiptType == "export_kitchen" && b.BatchCode.StartsWith("TH-BEP-"))
                                    .ThenBy(b => b.ExpiryDate)
                                    .ThenBy(b => b.ReceivedDate)
                                    .ThenBy(b => b.BatchId)
                                    .ToListAsync();

                                decimal totalBatchAvailable = availableBatches.Sum(b => b.QuantityRemaining);
                                if (totalBatchAvailable < detail.Quantity)
                                {
                                    await transaction.RollbackAsync();
                                    return BadRequest(new
                                    {
                                        success = false,
                                        message = $"Không đủ tồn trong các lô còn hạn sử dụng cho '{item.ItemName}'. Yêu cầu: {detail.Quantity:N2}, Tổng lô khả dụng: {totalBatchAvailable:N2} {item.BaseUnit}."
                                    });
                                }

                                decimal needToAllocate = detail.Quantity;
                                bool isFirstBatch = true;

                                foreach (var batch in availableBatches)
                                {
                                    if (needToAllocate <= 0) break;

                                    decimal allocate = Math.Min(needToAllocate, batch.QuantityRemaining);
                                    batch.QuantityRemaining -= allocate;
                                    if (batch.QuantityRemaining <= 0)
                                    {
                                        batch.QuantityRemaining = 0;
                                        batch.Status = "depleted";
                                    }
                                    batch.UpdatedAt = now;

                                    // Ghi nhận movement cho lô này
                                    var movement = new StockMovement
                                    {
                                        InventoryItemId = itemId,
                                        BatchId = batch.BatchId,
                                        MovementType = movementType,
                                        Quantity = allocate,
                                        UnitCost = batch.UnitCost,
                                        Reason = movementReason,
                                        ReferenceType = "stock_receipt",
                                        ReferenceId = receipt.ReceiptId,
                                        CreatedBy = userId.Value,
                                        CreatedAt = now
                                    };
                                    _dbContext.StockMovements.Add(movement);

                                    // Cập nhật dòng detail hoặc thêm dòng detail mới nếu 1 yêu cầu lấy từ nhiều lô
                                    if (isFirstBatch)
                                    {
                                        detail.BatchId = batch.BatchId;
                                        detail.Quantity = allocate;
                                        detail.UnitCost = batch.UnitCost;
                                        detail.Subtotal = InventoryCostHelper.CalculateAmount(allocate, batch.UnitCost, item.ConversionRate);
                                        detail.BomSnapshotJson = ScaleBomSnapshot(
                                            sourceBomSnapshot,
                                            requestedQuantity > 0 ? allocate / requestedQuantity : 1m);
                                        isFirstBatch = false;
                                    }
                                    else
                                    {
                                        var additionalDetail = new StockReceiptDetail
                                        {
                                            ReceiptId = receipt.ReceiptId,
                                            InventoryItemId = itemId,
                                            BatchId = batch.BatchId,
                                            Quantity = allocate,
                                            UnitCost = batch.UnitCost,
                                            Subtotal = InventoryCostHelper.CalculateAmount(allocate, batch.UnitCost, item.ConversionRate),
                                            Note = $"Phân bổ FEFO lô {batch.BatchCode} cho {item.ItemName}",
                                            BomSnapshotJson = ScaleBomSnapshot(
                                                sourceBomSnapshot,
                                                requestedQuantity > 0 ? allocate / requestedQuantity : 1m)
                                        };
                                        _dbContext.StockReceiptDetails.Add(additionalDetail);
                                    }

                                    needToAllocate -= allocate;
                                }
                            }
                        }

                        // Giảm tồn kho tổng của mặt hàng
                        item.CurrentQuantity -= totalRequired;
                    }
                    break;
                }

                // ------------------------------------------------------
                // CASE B: NHẬP KHO TỪ NHÀ CUNG CẤP (IMPORT SUPPLIER)
                // ------------------------------------------------------
                case "import_supplier":
                {
                    string movementReason = receipt.Note ?? "Nhập hàng từ nhà cung cấp";

                    foreach (var detail in detailsToProcess)
                    {
                        var item = await _dbContext.InventoryItems
                            .FirstOrDefaultAsync(i => i.InventoryItemId == detail.InventoryItemId);

                        if (item == null)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest(new { success = false, message = $"Không tìm thấy nguyên liệu ID {detail.InventoryItemId}." });
                        }

                        var grossQuantity = detail.GrossQuantity ?? detail.Quantity;
                        var effectiveUnitCost = detail.Quantity > 0
                            ? detail.UnitCost * grossQuantity / detail.Quantity
                            : detail.UnitCost;

                        InventoryBatch batch;

                        if (detail.BatchId.HasValue)
                        {
                            // Nhập bổ sung vào lô đã có
                            var existingBatch = await _dbContext.InventoryBatches
                                .FirstOrDefaultAsync(b => b.BatchId == detail.BatchId.Value);

                            if (existingBatch == null)
                            {
                                await transaction.RollbackAsync();
                                return BadRequest(new { success = false, message = $"Không tìm thấy lô ID {detail.BatchId}." });
                            }

                            if (existingBatch.InventoryItemId != item.InventoryItemId)
                            {
                                await transaction.RollbackAsync();
                                return BadRequest(new { success = false, message = $"Lô '{existingBatch.BatchCode}' không thuộc nguyên liệu '{item.ItemName}'." });
                            }

                            existingBatch.QuantityRemaining += detail.Quantity;
                            existingBatch.OriginalQuantity += detail.Quantity;
                            if (detail.Quantity > 0)
                            {
                                var previousQuantity = existingBatch.QuantityRemaining - detail.Quantity;
                                existingBatch.UnitCost = previousQuantity > 0
                                    ? ((previousQuantity * existingBatch.UnitCost) + (detail.Quantity * effectiveUnitCost)) / existingBatch.QuantityRemaining
                                    : effectiveUnitCost;
                            }
                            if (existingBatch.Status == "depleted")
                            {
                                existingBatch.Status = "active";
                            }
                            existingBatch.UpdatedAt = now;
                            batch = existingBatch;
                        }
                        else
                        {
                            // Tự động tạo mã lô nếu chưa có
                            string batchCode = string.IsNullOrWhiteSpace(detail.NewBatchCode)
                                ? $"LOT-{item.InventoryItemId}-{now:yyyyMMdd}-{new Random().Next(100, 999)}"
                                : detail.NewBatchCode.Trim();
                            while (await _dbContext.InventoryBatches.AnyAsync(b => b.InventoryItemId == item.InventoryItemId && b.BatchCode == batchCode))
                            {
                                if (!string.IsNullOrWhiteSpace(detail.NewBatchCode))
                                {
                                    await transaction.RollbackAsync();
                                    return Conflict(new { success = false, message = $"Mã lô '{batchCode}' đã tồn tại cho nguyên liệu '{item.ItemName}'." });
                                }

                                batchCode = $"LOT-{item.InventoryItemId}-{now:yyyyMMdd}-{new Random().Next(100, 999)}";
                            }

                            DateTime expiryDate = detail.ExpiryDate?.Date ?? now.AddMonths(3); // Mặc định 3 tháng nếu không chỉ định
                            if (expiryDate.Date < now.Date)
                            {
                                await transaction.RollbackAsync();
                                return BadRequest(new { success = false, message = $"Hạn sử dụng của lô '{batchCode}' không được nằm trong quá khứ." });
                            }

                            DateTime manufacturingDate = detail.ManufacturingDate?.Date ?? now.Date;
                            if (manufacturingDate.Date > expiryDate.Date)
                            {
                                await transaction.RollbackAsync();
                                return BadRequest(new { success = false, message = $"Ngày sản xuất của lô '{batchCode}' không được sau hạn sử dụng." });
                            }

                            batch = new InventoryBatch
                            {
                                InventoryItemId = item.InventoryItemId,
                                BatchCode = batchCode,
                                QuantityRemaining = detail.Quantity,
                                OriginalQuantity = detail.Quantity,
                                UnitCost = effectiveUnitCost,
                                ManufacturingDate = manufacturingDate,
                                ExpiryDate = expiryDate,
                                ReceivedDate = now,
                                SupplierId = receipt.SupplierId,
                                Status = "active",
                                CreatedAt = now,
                                UpdatedAt = now
                            };

                            _dbContext.InventoryBatches.Add(batch);
                            await _dbContext.SaveChangesAsync(); // Lưu để lấy BatchId

                            detail.BatchId = batch.BatchId;
                        }

                        // Ghi nhận StockMovement
                        detail.BatchId = batch.BatchId;

                        var movement = new StockMovement
                        {
                            InventoryItemId = item.InventoryItemId,
                            BatchId = batch.BatchId,
                            MovementType = "import",
                            Quantity = detail.Quantity,
                            UnitCost = effectiveUnitCost,
                            Reason = movementReason,
                            ReferenceType = "stock_receipt",
                            ReferenceId = receipt.ReceiptId,
                            CreatedBy = userId.Value,
                            CreatedAt = now
                        };
                        _dbContext.StockMovements.Add(movement);

                        // Tăng tồn kho tổng
                        item.CurrentQuantity += detail.Quantity;

                        // Cập nhật giá vốn gần nhất nếu có
                        if (detail.UnitCost > 0)
                        {
                            item.CostPerUnit = detail.UnitCost;
                        }
                    }
                    break;
                }

                // ------------------------------------------------------
                // CASE C: HOÀN KHO TỪ BẾP VỀ (RETURN FROM KITCHEN)
                // ------------------------------------------------------
                case "return_from_kitchen":
                {
                    string movementReason = receipt.Note ?? "Hoàn trả nguyên liệu dư từ bếp về kho";

                    foreach (var detail in detailsToProcess)
                    {
                        var item = await _dbContext.InventoryItems
                            .FirstOrDefaultAsync(i => i.InventoryItemId == detail.InventoryItemId);

                        if (item == null)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest(new { success = false, message = $"Không tìm thấy nguyên liệu ID {detail.InventoryItemId}." });
                        }

                        InventoryBatch? batch = null;

                        if (detail.BatchId.HasValue)
                        {
                            batch = await _dbContext.InventoryBatches
                                .FirstOrDefaultAsync(b => b.BatchId == detail.BatchId.Value);

                            if (batch == null || batch.InventoryItemId != item.InventoryItemId)
                            {
                                await transaction.RollbackAsync();
                                return BadRequest(new { success = false, message = $"Lô {detail.BatchId} không thuộc nguyên liệu '{item.ItemName}'." });
                            }

                            // Hàng hoàn trả có thể làm lô depleted sống lại, nhưng phải tăng cả
                            // OriginalQuantity để không phá vỡ invariant QuantityRemaining <= OriginalQuantity.
                            batch.QuantityRemaining += detail.Quantity;
                            batch.OriginalQuantity += detail.Quantity;
                            if (batch.ExpiryDate.Date >= now.Date) batch.Status = "active";
                            batch.UpdatedAt = now;
                        }

                        // Không được tăng tồn tổng mà bỏ sót tồn theo lô. Nếu không chỉ định lô,
                        // tạo một lô hoàn kho riêng để FEFO vẫn truy được nguồn hàng.
                        if (batch == null)
                        {
                            string returnBatchCode = $"RET-{item.InventoryItemId}-{now:yyyyMMddHHmmss}-{new Random().Next(100, 999)}";
                            while (await _dbContext.InventoryBatches.AnyAsync(b => b.InventoryItemId == item.InventoryItemId && b.BatchCode == returnBatchCode))
                            {
                                returnBatchCode = $"RET-{item.InventoryItemId}-{now:yyyyMMddHHmmss}-{new Random().Next(100, 999)}";
                            }

                            batch = new InventoryBatch
                            {
                                InventoryItemId = item.InventoryItemId,
                                BatchCode = returnBatchCode,
                                QuantityRemaining = detail.Quantity,
                                OriginalQuantity = detail.Quantity,
                                UnitCost = detail.UnitCost,
                                ManufacturingDate = now.Date,
                                ExpiryDate = now.Date.AddDays(30),
                                ReceivedDate = now,
                                SupplierId = item.SupplierId,
                                Status = "active",
                                CreatedAt = now,
                                UpdatedAt = now
                            };
                            _dbContext.InventoryBatches.Add(batch);
                            await _dbContext.SaveChangesAsync();
                            detail.BatchId = batch.BatchId;
                        }

                        // Ghi nhận StockMovement
                        var movement = new StockMovement
                        {
                            InventoryItemId = item.InventoryItemId,
                            BatchId = batch?.BatchId,
                            MovementType = "import",
                            Quantity = detail.Quantity,
                            UnitCost = detail.UnitCost,
                            Reason = movementReason,
                            ReferenceType = "stock_receipt",
                            ReferenceId = receipt.ReceiptId,
                            CreatedBy = userId.Value,
                            CreatedAt = now
                        };
                        _dbContext.StockMovements.Add(movement);

                        // Tăng tồn kho tổng
                        item.CurrentQuantity += detail.Quantity;
                    }
                    break;
                }

                default:
                    await transaction.RollbackAsync();
                    return BadRequest(new { success = false, message = $"Chưa hỗ trợ duyệt tự động cho loại phiếu '{receipt.ReceiptType}'." });
            }

            // 3. Tính lại tổng tiền phiếu kho và đổi trạng thái sang 'approved'
            receipt.TotalAmount = receipt.StockReceiptDetails.Sum(d => d.Subtotal);
            receipt.Status = "approved";
            receipt.ApprovedBy = userId.Value;
            receipt.ApprovedAt = now;
            receipt.UpdatedAt = now;

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            // 4. Phát tín hiệu Realtime SignalR
            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiptStatusChanged", new
                {
                    receiptId = receipt.ReceiptId,
                    receiptCode = receipt.ReceiptCode,
                    receiptType = receipt.ReceiptType,
                    status = "approved",
                    approvedBy = userId.Value,
                    approvedAt = now
                });

                await _hubContext.Clients.All.SendAsync("InventoryChanged", new
                {
                    action = "receipt_approved",
                    receiptType = receipt.ReceiptType,
                    receiptCode = receipt.ReceiptCode
                });

                await _hubContext.Clients.All.SendAsync("InventoryUpdated", new
                {
                    receiptId = receipt.ReceiptId,
                    receiptCode = receipt.ReceiptCode,
                    receiptType = receipt.ReceiptType,
                    status = "approved"
                });

                await _hubContext.Clients.All.SendAsync("KitchenUpdated", new
                {
                    receiptId = receipt.ReceiptId,
                    receiptCode = receipt.ReceiptCode,
                    receiptType = receipt.ReceiptType,
                    status = "approved"
                });
            }
            catch { }

            return Ok(new
            {
                success = true,
                message = $"Đã phê duyệt thành công phiếu kho '{receipt.ReceiptCode}'. Tồn kho và lô hàng đã được cập nhật.",
                receiptId = receipt.ReceiptId,
                status = "approved"
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { success = false, message = "Lỗi khi duyệt phiếu kho: " + ex.Message });
        }
    }

    // ==============================================================
    // 8. TIỆN ÍCH: XUẤT HỦY NHANH NGUYÊN LIỆU HỎNG (QUICK WASTE)
    // ==============================================================
    [Authorize(Roles = "admin,manager")]
    [HttpPost("quick-waste")]
    public async Task<IActionResult> QuickWasteDisposal([FromBody] QuickWasteRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { success = false, message = "Không xác định được người dùng." });
        }

        if (request.Items == null || request.Items.Count == 0)
        {
            return BadRequest(new { success = false, message = "Vui lòng chọn ít nhất một nguyên liệu để xuất hủy." });
        }

        if (request.Note?.Length > 1000 || request.Items.Any(item =>
            item.Quantity <= 0 || decimal.Round(item.Quantity, 4) != item.Quantity
            || string.IsNullOrWhiteSpace(item.Reason) || item.Reason.Length > 500))
        {
            return BadRequest(new { success = false, message = "Mỗi dòng hủy cần số lượng hợp lệ và lý do (tối đa 500 ký tự)." });
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var now = DateTime.UtcNow;
            var receiptCode = $"XH-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

            var receipt = new StockReceipt
            {
                ReceiptCode = receiptCode,
                ReceiptType = "waste_disposal",
                Status = "approved",
                Note = request.Note ?? "Xuất hủy nguyên liệu ôi thiu / hư hỏng / hết hạn",
                CreatedBy = userId.Value,
                ApprovedBy = userId.Value,
                ApprovedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            _dbContext.StockReceipts.Add(receipt);
            await _dbContext.SaveChangesAsync();

            decimal totalAmount = 0;
            var processedCount = 0;

            foreach (var itemInput in request.Items)
            {
                if (itemInput.Quantity <= 0)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new { success = false, message = "Số lượng xuất hủy phải lớn hơn 0." });
                }

                var item = await _dbContext.InventoryItems
                    .FirstOrDefaultAsync(i => i.InventoryItemId == itemInput.InventoryItemId && i.IsActive);
                if (item == null)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new { success = false, message = $"Không tìm thấy nguyên liệu ID {itemInput.InventoryItemId}." });
                }

                var availableBatchesQuery = _dbContext.InventoryBatches
                    .Where(b => b.InventoryItemId == item.InventoryItemId &&
                                b.QuantityRemaining > 0 &&
                                (b.Status == "active" || b.Status == "expired"));

                if (itemInput.BatchId.HasValue)
                {
                    availableBatchesQuery = availableBatchesQuery
                        .Where(b => b.BatchId == itemInput.BatchId.Value);
                }

                var availableBatches = await availableBatchesQuery
                    .OrderBy(b => b.ExpiryDate)
                    .ThenBy(b => b.ReceivedDate)
                    .ThenBy(b => b.BatchId)
                    .ToListAsync();

                if (itemInput.BatchId.HasValue && availableBatches.Count == 0)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new { success = false, message = $"Lô {itemInput.BatchId} không thuộc nguyên liệu hoặc không còn tồn để hủy." });
                }

                if (item.CurrentQuantity < itemInput.Quantity || availableBatches.Sum(b => b.QuantityRemaining) < itemInput.Quantity)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Không đủ tồn để xuất hủy cho '{item.ItemName}'. Yêu cầu: {itemInput.Quantity}, Còn: {item.CurrentQuantity}."
                    });
                }

                decimal remainingToWaste = itemInput.Quantity;
                foreach (var batch in availableBatches)
                {
                    if (remainingToWaste <= 0) break;

                    var allocated = Math.Min(remainingToWaste, batch.QuantityRemaining);
                    batch.QuantityRemaining -= allocated;
                    if (batch.QuantityRemaining <= 0)
                    {
                        batch.QuantityRemaining = 0;
                        batch.Status = "depleted";
                    }
                    batch.UpdatedAt = now;

                    var subtotal = InventoryCostHelper.CalculateAmount(allocated, batch.UnitCost, item.ConversionRate);
                    totalAmount += subtotal;
                    receipt.StockReceiptDetails.Add(new StockReceiptDetail
                    {
                        InventoryItemId = item.InventoryItemId,
                        BatchId = batch.BatchId,
                        Quantity = allocated,
                        UnitCost = batch.UnitCost,
                        Subtotal = subtotal,
                        Note = itemInput.Reason!.Trim()
                    });
                    _dbContext.StockMovements.Add(new StockMovement
                    {
                        InventoryItemId = item.InventoryItemId,
                        BatchId = batch.BatchId,
                        MovementType = "waste",
                        Quantity = allocated,
                        UnitCost = batch.UnitCost,
                        Reason = $"Xuất hủy: {itemInput.Reason!.Trim()}",
                        ReferenceType = "stock_receipt",
                        ReferenceId = receipt.ReceiptId,
                        CreatedBy = userId.Value,
                        CreatedAt = now
                    });
                    remainingToWaste -= allocated;
                }

                if (remainingToWaste > 0)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new { success = false, message = $"Không đủ tồn theo lô để xuất hủy cho '{item.ItemName}'." });
                }

                item.CurrentQuantity -= itemInput.Quantity;
                processedCount++;
            }

            if (processedCount == 0)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { success = false, message = "Không có dòng xuất hủy hợp lệ." });
            }

            receipt.TotalAmount = totalAmount;

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            try
            {
                await _hubContext.Clients.All.SendAsync("InventoryChanged", new { action = "quick_waste", receiptCode });
            }
            catch { }

            return Ok(new
            {
                success = true,
                message = $"Đã xuất hủy thành công {receipt.StockReceiptDetails.Count} mặt hàng (Phiếu '{receiptCode}').",
                receiptId = receipt.ReceiptId,
                receiptCode
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { success = false, message = "Lỗi khi xuất hủy: " + ex.Message });
        }
    }
}
