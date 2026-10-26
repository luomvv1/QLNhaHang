using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;
using Restaurant.Api.Hubs;
using System.Security.Claims;
using System.Data;

namespace Restaurant.Api.Controllers.Admin;

// ==============================================================
// DTOs CHO HẠNG MỤC 4: KIỂM KÊ KHO & CÂN BẰNG TỒN (STOCK AUDITS)
// ==============================================================

public class CreateStockAuditRequest
{
    public DateTime? AuditDate { get; set; }
    public string? Note { get; set; }
    public List<int>? SpecificItemIds { get; set; } // Nếu null hoặc rỗng -> kiểm kê toàn bộ nguyên liệu active
}

public class UpdateAuditCountItemInput
{
    public int AuditItemId { get; set; }
    public decimal ActualQuantity { get; set; } // Số lượng thực đếm (theo BaseUnit)
    public string? Reason { get; set; } // Lý do chênh lệch (nếu có)
}

public class UpdateAuditCountsRequest
{
    public string? Note { get; set; }
    public List<UpdateAuditCountItemInput> Items { get; set; } = new();
}

// ==============================================================
// CONTROLLER: KIỂM KÊ & ĐIỀU CHỈNH TỒN KHO (HẠNG MỤC 4)
// ==============================================================

[Route("api/admin/inventory/audits")]
[Authorize(Roles = "admin,manager")]
[ApiController]
public class AdminAuditController : ControllerBase
{
    private readonly RestaurantDbContext _dbContext;
    private readonly IHubContext<RestaurantHub> _hubContext;

    public AdminAuditController(RestaurantDbContext dbContext, IHubContext<RestaurantHub> hubContext)
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
    // 1. DANH SÁCH CÁC ĐỢT KIỂM KÊ
    // ==============================================================
    [HttpGet]
    public async Task<IActionResult> GetAudits(
        [FromQuery] string? status = null,
        [FromQuery] string? fromDate = null,
        [FromQuery] string? toDate = null)
    {
        var query = _dbContext.StockAudits
            .Include(a => a.CreatedByNavigation)
            .Include(a => a.CompletedByNavigation)
            .Include(a => a.StockAuditItems)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            var s = status.Trim().ToLower();
            query = query.Where(a => a.Status.ToLower() == s);
        }

        if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParse(fromDate, out var from))
        {
            query = query.Where(a => a.AuditDate >= from.Date);
        }

        if (!string.IsNullOrWhiteSpace(toDate) && DateTime.TryParse(toDate, out var to))
        {
            query = query.Where(a => a.AuditDate < to.Date.AddDays(1));
        }

        var audits = await query
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.AuditId,
                a.AuditCode,
                a.AuditDate,
                a.Status,
                a.Note,
                TotalItems = a.StockAuditItems.Count,
                DiscrepancyItemsCount = a.StockAuditItems.Count(i => i.DifferenceQuantity != 0),
                CreatedBy = a.CreatedBy,
                CreatedByName = a.CreatedByNavigation.FullName,
                CompletedBy = a.CompletedBy,
                CompletedByName = a.CompletedByNavigation != null ? a.CompletedByNavigation.FullName : null,
                a.CompletedAt,
                a.CreatedAt
            })
            .ToListAsync();

        return Ok(new { success = true, data = audits });
    }

    // ==============================================================
    // 2. CHI TIẾT MỘT PHIẾU KIỂM KÊ
    // ==============================================================
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAuditById(int id)
    {
        var audit = await _dbContext.StockAudits
            .Include(a => a.CreatedByNavigation)
            .Include(a => a.CompletedByNavigation)
            .Include(a => a.StockAuditItems)
                .ThenInclude(i => i.InventoryItem)
            .FirstOrDefaultAsync(a => a.AuditId == id);

        if (audit == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy phiếu kiểm kê." });
        }

        var items = audit.StockAuditItems.Select(i => new
        {
            i.AuditItemId,
            i.InventoryItemId,
            ItemName = i.InventoryItem.ItemName,
            BaseUnit = i.InventoryItem.BaseUnit,
            PurchaseUnit = i.InventoryItem.PurchaseUnit,
            ConversionRate = i.InventoryItem.ConversionRate,
            CostPerUnit = i.InventoryItem.CostPerUnit,
            CostPerPurchaseUnit = i.InventoryItem.CostPerUnit,
            CostPerBaseUnit = i.InventoryItem.ConversionRate > 0 ? i.InventoryItem.CostPerUnit / i.InventoryItem.ConversionRate : i.InventoryItem.CostPerUnit,
            i.SystemQuantity,
            i.ActualQuantity,
            i.DifferenceQuantity,
            DifferenceAmount = InventoryCostHelper.CalculateAmount(
                i.DifferenceQuantity,
                i.InventoryItem.CostPerUnit,
                i.InventoryItem.ConversionRate),
            i.Reason
        }).OrderByDescending(i => Math.Abs(i.DifferenceQuantity)).ToList();

        return Ok(new
        {
            success = true,
            data = new
            {
                audit.AuditId,
                audit.AuditCode,
                audit.AuditDate,
                audit.Status,
                audit.Note,
                audit.CreatedBy,
                CreatedByName = audit.CreatedByNavigation.FullName,
                audit.CompletedBy,
                CompletedByName = audit.CompletedByNavigation?.FullName,
                audit.CompletedAt,
                audit.CreatedAt,
                TotalItems = items.Count,
                DiscrepancyCount = items.Count(i => i.DifferenceQuantity != 0),
                TotalDifferenceAmount = items.Sum(i => i.DifferenceAmount),
                Items = items
            }
        });
    }

    // ==============================================================
    // 3. TẠO MỚI PHIẾU KIỂM KÊ (SNAPSHOT TỒN SỔ SÁCH SYSTEM QUANTITY)
    // ==============================================================
    [HttpPost]
    public async Task<IActionResult> CreateAudit([FromBody] CreateStockAuditRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { success = false, message = "Không xác định được danh tính người tạo phiếu." });
        }

        var now = DateTime.UtcNow;
        var auditDate = request.AuditDate?.Date ?? now.Date;

        var itemQuery = _dbContext.InventoryItems.Where(i => i.IsActive);
        if (request.SpecificItemIds != null && request.SpecificItemIds.Count > 0)
        {
            itemQuery = itemQuery.Where(i => request.SpecificItemIds.Contains(i.InventoryItemId));
        }

        var targetItems = await itemQuery.ToListAsync();
        if (targetItems.Count == 0)
        {
            return BadRequest(new { success = false, message = "Không có nguyên liệu nào khả dụng để tạo phiếu kiểm kê." });
        }

        var auditCode = $"KK-{auditDate:yyyyMMdd}-{new Random().Next(100, 999)}";
        while (await _dbContext.StockAudits.AnyAsync(a => a.AuditCode == auditCode))
        {
            auditCode = $"KK-{auditDate:yyyyMMdd}-{new Random().Next(100, 999)}";
        }

        var audit = new StockAudit
        {
            AuditCode = auditCode,
            AuditDate = auditDate,
            Status = "draft",
            Note = request.Note,
            CreatedBy = userId.Value,
            CreatedAt = now,
            UpdatedAt = now
        };

        foreach (var item in targetItems)
        {
            audit.StockAuditItems.Add(new StockAuditItem
            {
                InventoryItemId = item.InventoryItemId,
                SystemQuantity = item.CurrentQuantity, // Chụp snapshot tồn sổ sách tại thời điểm lập phiếu
                ActualQuantity = item.CurrentQuantity, // Mặc định ban đầu bằng số sổ sách
                DifferenceQuantity = 0,
                Reason = null
            });
        }

        _dbContext.StockAudits.Add(audit);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAuditById), new { id = audit.AuditId }, new
        {
            success = true,
            message = $"Đã tạo phiếu kiểm kê '{audit.AuditCode}' cho {targetItems.Count} nguyên liệu.",
            auditId = audit.AuditId,
            auditCode = audit.AuditCode
        });
    }

    // ==============================================================
    // 4. NHẬP SỐ LIỆU ĐẾM THỰC TẾ (ACTUAL QUANTITY)
    // ==============================================================
    [HttpPut("{id}/counts")]
    public async Task<IActionResult> UpdateAuditCounts(int id, [FromBody] UpdateAuditCountsRequest request)
    {
        var audit = await _dbContext.StockAudits
            .Include(a => a.StockAuditItems)
            .FirstOrDefaultAsync(a => a.AuditId == id);

        if (audit == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy phiếu kiểm kê." });
        }

        if (audit.Status != "draft")
        {
            return BadRequest(new { success = false, message = $"Chỉ có thể nhập số liệu kiểm đếm khi phiếu ở trạng thái nháp ('draft'). Phiếu này đang là '{audit.Status}'." });
        }

        var itemMap = audit.StockAuditItems.ToDictionary(i => i.AuditItemId);

        foreach (var input in request.Items)
        {
            if (!itemMap.TryGetValue(input.AuditItemId, out var auditItem))
                return BadRequest(new { success = false, message = $"AuditItemId {input.AuditItemId} không thuộc phiếu kiểm kê này." });

            if (input.ActualQuantity < 0)
                return BadRequest(new { success = false, message = "Số lượng thực đếm không được âm." });

            auditItem.ActualQuantity = input.ActualQuantity;
            auditItem.DifferenceQuantity = auditItem.ActualQuantity - auditItem.SystemQuantity;
            auditItem.Reason = input.Reason;
        }

        if (request.Note != null)
        {
            audit.Note = request.Note;
        }
        audit.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Đã cập nhật số liệu kiểm đếm thực tế thành công.",
            auditId = audit.AuditId
        });
    }

    // ==============================================================
    // 5. HOÀN TẤT KIỂM KÊ & CÂN BẰNG KHO (COMPLETE AUDIT WITH TRANSACTION)
    // ==============================================================
    [HttpPost("{id}/complete")]
    public async Task<IActionResult> CompleteAudit(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { success = false, message = "Không xác định được danh tính người phê duyệt hoàn tất kiểm kê." });
        }

        using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var audit = await _dbContext.StockAudits
                .Include(a => a.StockAuditItems)
                    .ThenInclude(i => i.InventoryItem)
                .FirstOrDefaultAsync(a => a.AuditId == id);

            if (audit == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy phiếu kiểm kê." });
            }

            if (audit.Status == "completed")
            {
                return Conflict(new { success = false, message = "Phiếu kiểm kê này đã được hoàn tất trước đó rồi." });
            }

            if (audit.Status == "cancelled")
            {
                return BadRequest(new { success = false, message = "Phiếu kiểm kê đã bị hủy, không thể hoàn tất." });
            }

            if (audit.Status != "draft")
            {
                return BadRequest(new { success = false, message = $"Chỉ có thể hoàn tất phiếu kiểm kê ở trạng thái draft, hiện tại là '{audit.Status}'." });
            }

            var now = DateTime.UtcNow;

            // Không được áp dụng chênh lệch trên snapshot cũ nếu tồn sổ đã thay đổi
            // sau lúc lập phiếu kiểm kê. Người dùng phải tạo lại phiếu để tránh trừ/cộng đúp.
            var changedAfterSnapshot = audit.StockAuditItems
                .Where(i => i.InventoryItem.CurrentQuantity != i.SystemQuantity)
                .ToList();

            if (changedAfterSnapshot.Count > 0)
            {
                await transaction.RollbackAsync();
                return Conflict(new
                {
                    success = false,
                    message = "Tồn sổ đã thay đổi sau khi lập phiếu kiểm kê. Vui lòng hủy phiếu này và tạo lại snapshot mới.",
                    changedItemIds = changedAfterSnapshot.Select(i => i.InventoryItemId).ToList()
                });
            }

            // Lọc ra các mặt hàng có chênh lệch giữa đếm thực tế và sổ sách
            var discrepancyItems = audit.StockAuditItems
                .Where(i => i.DifferenceQuantity != 0)
                .ToList();

            if (discrepancyItems.Count > 0)
            {
                // Tạo một phiếu kho điều chỉnh kiểm kê để lưu vết chính thức trong lịch sử phiếu kho
                var receiptCode = $"DC-{now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
                var adjustmentReceipt = new StockReceipt
                {
                    ReceiptCode = receiptCode,
                    ReceiptType = "audit_adjustment",
                    Status = "approved",
                    Note = $"Phiếu điều chỉnh cân bằng kho tự động từ đợt kiểm kê {audit.AuditCode}",
                    CreatedBy = userId.Value,
                    ApprovedBy = userId.Value,
                    ApprovedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                decimal totalAdjustmentAmount = 0;

                foreach (var d in discrepancyItems)
                {
                    var item = d.InventoryItem;
                    decimal diff = d.DifferenceQuantity; // > 0: thừa, < 0: thiếu

                    if (diff > 0)
                    {
                        // Thừa kho: tạo một lô điều chỉnh riêng để không làm
                        // QuantityRemaining vượt OriginalQuantity của lô cũ.
                        item.CurrentQuantity += diff;

                        var adjustmentBatchCode = $"ADJ-{item.InventoryItemId}-{now:yyyyMMddHHmmss}-{new Random().Next(100, 999)}";
                        while (await _dbContext.InventoryBatches.AnyAsync(b => b.InventoryItemId == item.InventoryItemId && b.BatchCode == adjustmentBatchCode))
                        {
                            adjustmentBatchCode = $"ADJ-{item.InventoryItemId}-{now:yyyyMMddHHmmss}-{new Random().Next(100, 999)}";
                        }

                        var adjustmentBatch = new InventoryBatch
                        {
                            InventoryItemId = item.InventoryItemId,
                            BatchCode = adjustmentBatchCode,
                            QuantityRemaining = diff,
                            OriginalQuantity = diff,
                            UnitCost = item.CostPerUnit,
                            ManufacturingDate = now.Date,
                            ExpiryDate = now.Date.AddYears(1),
                            ReceivedDate = now,
                            SupplierId = item.SupplierId,
                            Status = "active",
                            CreatedAt = now,
                            UpdatedAt = now
                        };
                        _dbContext.InventoryBatches.Add(adjustmentBatch);
                        await _dbContext.SaveChangesAsync();

                        var movement = new StockMovement
                        {
                            InventoryItemId = item.InventoryItemId,
                            BatchId = adjustmentBatch.BatchId,
                            MovementType = "adjustment",
                            Quantity = diff,
                            UnitCost = item.CostPerUnit,
                            Reason = $"Kiểm kê thừa: {d.Reason ?? audit.AuditCode}",
                            ReferenceType = "stock_audit",
                            ReferenceId = audit.AuditId,
                            CreatedBy = userId.Value,
                            CreatedAt = now
                        };
                        _dbContext.StockMovements.Add(movement);

                        var subtotal = InventoryCostHelper.CalculateAmount(diff, item.CostPerUnit, item.ConversionRate);
                        totalAdjustmentAmount += subtotal;

                        adjustmentReceipt.StockReceiptDetails.Add(new StockReceiptDetail
                        {
                            InventoryItemId = item.InventoryItemId,
                            BatchId = adjustmentBatch.BatchId,
                            Quantity = diff,
                            UnitCost = item.CostPerUnit,
                            Subtotal = subtotal,
                            Note = $"Điều chỉnh tăng: {d.Reason}"
                        });
                    }
                    else
                    {
                        // Thiếu kho (hao hụt / mất mát): Giảm tồn kho
                        decimal shortage = Math.Abs(diff);

                        // Trừ FEFO theo từng lô. Hàng đã hết hạn vẫn phải được tính
                        // khi cân bằng thực tế, vì nó vẫn đang nằm trong tồn sổ.
                        var activeBatches = await _dbContext.InventoryBatches
                            .Where(b => b.InventoryItemId == item.InventoryItemId &&
                                        (b.Status == "active" || b.Status == "expired") &&
                                        b.QuantityRemaining > 0)
                            .OrderBy(b => b.ExpiryDate)
                            .ThenBy(b => b.ReceivedDate)
                            .ThenBy(b => b.BatchId)
                            .ToListAsync();

                        if (activeBatches.Sum(b => b.QuantityRemaining) < shortage)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest(new
                            {
                                success = false,
                                message = $"Không đủ tồn theo lô để cân bằng nguyên liệu '{item.ItemName}'."
                            });
                        }

                        decimal remainingShortage = shortage;
                        foreach (var b in activeBatches)
                        {
                            if (remainingShortage <= 0) break;
                            decimal deduct = Math.Min(remainingShortage, b.QuantityRemaining);
                            b.QuantityRemaining -= deduct;
                            if (b.QuantityRemaining <= 0)
                            {
                                b.QuantityRemaining = 0;
                                b.Status = "depleted";
                            }
                            b.UpdatedAt = now;
                            remainingShortage -= deduct;

                            var subtotal = InventoryCostHelper.CalculateAmount(deduct, b.UnitCost, item.ConversionRate);
                            totalAdjustmentAmount += subtotal;

                            _dbContext.StockMovements.Add(new StockMovement
                            {
                                InventoryItemId = item.InventoryItemId,
                                BatchId = b.BatchId,
                                MovementType = "adjustment",
                                Quantity = deduct,
                                UnitCost = b.UnitCost,
                                Reason = $"Kiểm kê hao hụt: {d.Reason ?? audit.AuditCode}",
                                ReferenceType = "stock_audit",
                                ReferenceId = audit.AuditId,
                                CreatedBy = userId.Value,
                                CreatedAt = now
                            });

                            adjustmentReceipt.StockReceiptDetails.Add(new StockReceiptDetail
                            {
                                InventoryItemId = item.InventoryItemId,
                                BatchId = b.BatchId,
                                Quantity = deduct,
                                UnitCost = b.UnitCost,
                                Subtotal = subtotal,
                                Note = $"Điều chỉnh giảm (hao hụt): {d.Reason}"
                            });
                        }

                        item.CurrentQuantity -= shortage;
                    }
                }

                adjustmentReceipt.TotalAmount = totalAdjustmentAmount;
                _dbContext.StockReceipts.Add(adjustmentReceipt);
            }

            audit.Status = "completed";
            audit.CompletedBy = userId.Value;
            audit.CompletedAt = now;
            audit.UpdatedAt = now;

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            try
            {
                await _hubContext.PublishAsync(_dbContext, "InventoryChanged", new
                {
                    action = "audit_completed",
                    auditCode = audit.AuditCode,
                    discrepancyCount = discrepancyItems.Count
                });
            }
            catch { }

            return Ok(new
            {
                success = true,
                message = $"Đã hoàn tất kiểm kê '{audit.AuditCode}'. Đã tự động cân bằng tồn kho và lô hàng cho {discrepancyItems.Count} mặt hàng chênh lệch.",
                auditId = audit.AuditId,
                discrepancyCount = discrepancyItems.Count
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { success = false, message = "Lỗi khi hoàn tất kiểm kê: " + ex.Message });
        }
    }

    // ==============================================================
    // 6. HỦY PHIẾU KIỂM KÊ (CHỈ KHI DRAFT)
    // ==============================================================
    [HttpDelete("{id}")]
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelAudit(int id)
    {
        var audit = await _dbContext.StockAudits.FindAsync(id);
        if (audit == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy phiếu kiểm kê." });
        }

        if (audit.Status != "draft")
        {
            return BadRequest(new { success = false, message = $"Chỉ có thể hủy phiếu kiểm kê ở trạng thái nháp ('draft'). Phiếu này đang là '{audit.Status}'." });
        }

        audit.Status = "cancelled";
        audit.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return Ok(new { success = true, message = $"Đã hủy phiếu kiểm kê '{audit.AuditCode}'." });
    }
}
