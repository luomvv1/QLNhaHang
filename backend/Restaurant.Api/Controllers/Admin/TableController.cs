using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;

using System.Security.Claims;
using Restaurant.Api.Services;

namespace Restaurant.Api.Controllers.Admin;

// ==============================================================
// KHUÔN MẪU DỮ LIỆU BÀN ĂN
// ==============================================================
public class TableRequest
{
    public string TableCode { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string? AreaName { get; set; }
    public int Capacity { get; set; }
}

public class UpdateTableRequest : TableRequest
{
    public int TableId { get; set; }
    public string TableStatus { get; set; } = "available"; // available, occupied, reserved, cleaning
    public bool IsActive { get; set; }
}
public class RenameAreaRequest
{
    public string OldAreaName { get; set; } = string.Empty;
    public string NewAreaName { get; set; } = string.Empty;
}
// ==============================================================

[Route("api/admin/table")]
[Authorize(Roles = "admin,manager")]
[ApiController]
public class TableController : ControllerBase
{
    private readonly RestaurantDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;

    public TableController(RestaurantDbContext dbContext, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    [HttpGet("list")]
    // Cho phép lọc theo IsActive. Bỏ trống thì lấy tất cả.
    public async Task<IActionResult> GetList([FromQuery] bool? isActive = null)
    {
        var query = _dbContext.DiningTables.AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(t => t.IsActive == isActive.Value);
        }

        var tables = await query
            .Select(t => new
            {
                t.TableId,
                t.TableCode,
                t.TableName,
                t.AreaName,
                t.Capacity,
                t.TableStatus,
                t.IsActive
            })
            .OrderBy(t => t.AreaName).ThenBy(t => t.TableName)
            .ToListAsync();

        return Ok(new { data = tables });
    }

    [HttpPost("insert")]
    public async Task<IActionResult> InsertTable([FromBody] TableRequest request)
    {
        if (string.IsNullOrEmpty(request.TableName) || string.IsNullOrEmpty(request.TableCode))
            return BadRequest(new { message = "Mã bàn và Tên bàn không được để trống!" });

        // Kiểm tra trùng mã bàn
        var isCodeExists = await _dbContext.DiningTables.AnyAsync(t => t.TableCode == request.TableCode);
        if (isCodeExists)
            return BadRequest(new { message = "Mã bàn này đã tồn tại, vui lòng chọn mã khác!" });

        var newTable = new DiningTable
        {
            TableCode = request.TableCode,
            TableName = request.TableName,
            AreaName = request.AreaName,
            Capacity = request.Capacity,
            TableStatus = "empty", // Bàn mới tạo thì luôn ở trạng thái trống
            IsActive = true
        };

        _dbContext.DiningTables.Add(newTable);
        await _dbContext.SaveChangesAsync();

        var adminId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var aid) ? (int?)aid : null;
        await _auditLogService.LogAsync(
            adminId,
            "table_created",
            "DiningTable",
            newTable.TableId.ToString(),
            null,
            new { tableCode = newTable.TableCode, tableName = newTable.TableName, areaName = newTable.AreaName, capacity = newTable.Capacity });

        return Ok(new { message = "Thêm bàn ăn thành công!" });
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateTable([FromBody] UpdateTableRequest request)
    {
        if (string.IsNullOrEmpty(request.TableName) || string.IsNullOrEmpty(request.TableCode))
            return BadRequest(new { message = "Mã bàn và Tên bàn không được để trống!" });

        var table = await _dbContext.DiningTables.FindAsync(request.TableId);
        if (table == null)
            return BadRequest(new { message = "Không tìm thấy bàn ăn này!" });

        // Kiểm tra trùng mã bàn với các bàn KHÁC
        var isCodeExists = await _dbContext.DiningTables
            .AnyAsync(t => t.TableCode == request.TableCode && t.TableId != request.TableId);
        if (isCodeExists)
            return BadRequest(new { message = "Mã bàn này đã bị trùng với một bàn khác!" });

        var oldSnapshot = new {
            tableCode = table.TableCode,
            tableName = table.TableName,
            areaName = table.AreaName,
            capacity = table.Capacity,
            tableStatus = table.TableStatus,
            isActive = table.IsActive
        };

        table.TableCode = request.TableCode;
        table.TableName = request.TableName;
        table.AreaName = request.AreaName;
        table.Capacity = request.Capacity;
        table.TableStatus = request.TableStatus;
        table.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync();

        var updateAdminId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uaid) ? (int?)uaid : null;
        var newSnapshot = new {
            tableCode = table.TableCode,
            tableName = table.TableName,
            areaName = table.AreaName,
            capacity = table.Capacity,
            tableStatus = table.TableStatus,
            isActive = table.IsActive
        };
        await _auditLogService.LogAsync(updateAdminId, "table_updated", "DiningTable", table.TableId.ToString(), oldSnapshot, newSnapshot);

        return Ok(new { message = "Cập nhật bàn ăn thành công!" });
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteTable([FromQuery] int tableId)
    {
        var table = await _dbContext.DiningTables.FindAsync(tableId);
        if (table == null)
            return BadRequest(new { message = "Không tìm thấy bàn ăn này!" });

        // Vẫn dùng chiến thuật Xóa Mềm (Soft Delete) để bảo vệ lịch sử hóa đơn
        table.IsActive = false;
        await _dbContext.SaveChangesAsync();

        var delAdminId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var daid) ? (int?)daid : null;
        await _auditLogService.LogAsync(
            delAdminId,
            "table_status_changed",
            "DiningTable",
            tableId.ToString(),
            new { isActive = true, tableName = table.TableName },
            new { isActive = false, tableName = table.TableName, status = "ngừng sử dụng" });

        return Ok(new { message = "Đã ngừng sử dụng bàn ăn này thành công!" });
    }

    [HttpPatch("reactivate")]
    public async Task<IActionResult> ReactivateTable([FromQuery] int tableId)
    {
        var table = await _dbContext.DiningTables.FindAsync(tableId);
        if (table == null)
            return BadRequest(new { message = "Không tìm thấy bàn ăn này!" });

        table.IsActive = true;
        await _dbContext.SaveChangesAsync();

        var reAdminId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var raid) ? (int?)raid : null;
        await _auditLogService.LogAsync(
            reAdminId,
            "table_status_changed",
            "DiningTable",
            tableId.ToString(),
            new { isActive = false, tableName = table.TableName },
            new { isActive = true, tableName = table.TableName, status = "kích hoạt lại" });

        return Ok(new { message = "Đã mở lại hoạt động cho bàn ăn thành công!" });
    }

    /// <summary>
    /// Xóa vĩnh viễn bàn ăn khỏi cơ sở dữ liệu.
    /// Cho phép xóa vì phiếu hóa đơn đã được in và lưu vết vào thời điểm thanh toán.
    /// </summary>
    [HttpDelete("permanent-delete")]
    public async Task<IActionResult> PermanentDeleteTable([FromQuery] int tableId)
    {
        var table = await _dbContext.DiningTables.FindAsync(tableId);
        if (table == null)
            return NotFound(new { message = "Không tìm thấy bàn ăn này!" });

        // Kiểm tra nếu bàn đang có khách ngồi ăn trực tiếp
        if (table.TableStatus == "occupied")
            return BadRequest(new { message = $"Bàn \"{table.TableName}\" đang có khách dùng bữa. Không thể xóa lúc này!" });

        // Kiểm tra nếu có đơn hàng đang hoạt động chưa hoàn tất/thanh toán
        var hasActiveOrder = await _dbContext.OrderTables
            .AnyAsync(ot => ot.TableId == tableId && ot.Order.OrderStatus != "completed" && ot.Order.OrderStatus != "cancelled");
        if (hasActiveOrder)
            return BadRequest(new { message = $"Bàn \"{table.TableName}\" đang có hóa đơn chưa thanh toán. Vui lòng thanh toán hoặc hủy đơn trước khi xóa!" });

        // Kiểm tra lịch đặt bàn tương lai chưa hoàn tất
        var hasActiveReservation = await _dbContext.Reservations
            .AnyAsync(r => r.TableId == tableId && r.ReservationStatus != "completed" && r.ReservationStatus != "cancelled" && r.ReservationStatus != "rejected");
        if (hasActiveReservation)
            return BadRequest(new { message = $"Bàn \"{table.TableName}\" đang có lịch đặt trước. Vui lòng chuyển bàn hoặc hủy lịch đặt trước khi xóa!" });

        // Gỡ liên kết Reservations lịch sử (chuyển TableId về null để bảo tồn thông tin đặt bàn cũ)
        var reservations = await _dbContext.Reservations.Where(r => r.TableId == tableId).ToListAsync();
        foreach (var r in reservations)
        {
            r.TableId = null;
        }

        // Gỡ liên kết OrderTables lịch sử (do hóa đơn giấy đã được in và lưu vào thời điểm thanh toán)
        var orderTables = await _dbContext.OrderTables.Where(ot => ot.TableId == tableId).ToListAsync();
        _dbContext.OrderTables.RemoveRange(orderTables);

        // Xóa hoàn toàn bản ghi bàn ăn
        _dbContext.DiningTables.Remove(table);
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = $"Đã xóa vĩnh viễn bàn \"{table.TableName}\" ({table.TableCode}) thành công!" });
    }

    /// <summary>
    /// Xóa toàn bộ một phân khu vực bàn ăn (cho phép chuyển bàn sang khu khác hoặc xóa luôn các bàn).
    /// </summary>
    [HttpDelete("area/delete")]
    public async Task<IActionResult> DeleteArea(
        [FromQuery] string areaName,
        [FromQuery] string? transferToArea = null,
        [FromQuery] bool deleteTables = false)
    {
        if (string.IsNullOrWhiteSpace(areaName))
            return BadRequest(new { message = "Tên khu vực không được để trống!" });

        var trimmedArea = areaName.Trim();
        var tables = await _dbContext.DiningTables
            .Where(t => t.AreaName == trimmedArea)
            .ToListAsync();

        if (!tables.Any())
            return Ok(new { message = $"Khu vực \"{trimmedArea}\" hiện không có bàn nào hoặc đã được xóa." });

        if (!string.IsNullOrWhiteSpace(transferToArea))
        {
            var targetArea = transferToArea.Trim();
            foreach (var t in tables)
            {
                t.AreaName = targetArea;
                t.UpdatedAt = DateTime.UtcNow;
            }
            await _dbContext.SaveChangesAsync();
            return Ok(new { message = $"Đã chuyển {tables.Count} bàn sang khu vực \"{targetArea}\" và xóa khu vực \"{trimmedArea}\" thành công!" });
        }
        else if (deleteTables)
        {
            var tableIds = tables.Select(t => t.TableId).ToList();

            var hasActiveOrder = await _dbContext.OrderTables
                .AnyAsync(ot => tableIds.Contains(ot.TableId) && ot.Order.OrderStatus != "completed" && ot.Order.OrderStatus != "cancelled");
            if (hasActiveOrder)
                return BadRequest(new { message = $"Khu vực \"{trimmedArea}\" có bàn đang phục vụ khách hoặc có đơn chưa thanh toán. Không thể xóa!" });

            // Gỡ liên kết Reservations
            var reservations = await _dbContext.Reservations.Where(r => r.TableId.HasValue && tableIds.Contains(r.TableId.Value)).ToListAsync();
            foreach (var r in reservations)
            {
                r.TableId = null;
            }

            // Gỡ liên kết OrderTables
            var orderTables = await _dbContext.OrderTables.Where(ot => tableIds.Contains(ot.TableId)).ToListAsync();
            _dbContext.OrderTables.RemoveRange(orderTables);

            // Xóa toàn bộ bàn trong khu vực này
            _dbContext.DiningTables.RemoveRange(tables);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = $"Đã xóa khu vực \"{trimmedArea}\" và {tables.Count} bàn ăn thuộc khu vực này thành công!" });
        }
        else
        {
            return BadRequest(new { message = $"Khu vực \"{trimmedArea}\" đang có {tables.Count} bàn ăn. Vui lòng chọn chuyển bàn sang khu vực khác hoặc chọn xóa kèm các bàn!" });
        }
    }

    /// <summary>
    /// Đổi tên phân khu vực cho toàn bộ các bàn trong khu vực đó.
    /// </summary>
    [HttpPut("area/rename")]
    public async Task<IActionResult> RenameArea([FromBody] RenameAreaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OldAreaName) || string.IsNullOrWhiteSpace(request.NewAreaName))
            return BadRequest(new { message = "Tên khu vực cũ và tên mới không được để trống!" });

        var oldArea = request.OldAreaName.Trim();
        var newArea = request.NewAreaName.Trim();

        var tables = await _dbContext.DiningTables
            .Where(t => t.AreaName == oldArea)
            .ToListAsync();

        if (!tables.Any())
            return NotFound(new { message = $"Không tìm thấy khu vực \"{oldArea}\"!" });

        foreach (var t in tables)
        {
            t.AreaName = newArea;
            t.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();
        return Ok(new { message = $"Đã đổi tên khu vực từ \"{oldArea}\" thành \"{newArea}\" cho {tables.Count} bàn ăn thành công!" });
    }
}

