using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;

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
// ==============================================================

[Route("api/admin/table")]
[ApiController]
public class TableController : ControllerBase
{
    private readonly RestaurantDbContext _dbContext;

    public TableController(RestaurantDbContext dbContext)
    {
        _dbContext = dbContext;
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

        table.TableCode = request.TableCode;
        table.TableName = request.TableName;
        table.AreaName = request.AreaName;
        table.Capacity = request.Capacity;
        table.TableStatus = request.TableStatus;
        table.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync();

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

        return Ok(new { message = "Đã mở lại hoạt động cho bàn ăn thành công!" });
    }
}

