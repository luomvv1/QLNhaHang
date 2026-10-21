using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;

namespace Restaurant.Api.Controllers.Admin;

/// <summary>
/// Quản lý Danh mục Món ăn (Food Categories) dành cho Quản lý / Admin
/// Sử dụng trực tiếp Entity Model: FoodCategory
/// </summary>
[Route("api/admin/category")]
[ApiController]
public class CategoryController : ControllerBase
{
    private readonly RestaurantDbContext _dbContext;

    public CategoryController(RestaurantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// [GET] Lấy danh sách toàn bộ danh mục món ăn
    /// URL: /api/admin/category/list
    /// </summary>
    /// <returns>Danh sách danh mục gồm CategoryId, CategoryName, Description</returns>
    [HttpGet("list")]
    public async Task<IActionResult> GetList()
    {
        var categories = await _dbContext.FoodCategories
            .OrderBy(c => c.SortOrder)
            .Select(c => new 
            { 
                c.CategoryId, 
                c.CategoryName, 
                c.Description,
                c.SortOrder,
                c.IsActive
            })
            .ToListAsync();

        return Ok(new { success = true, data = categories }); 
    }

    /// <summary>
    /// [POST] Thêm mới một danh mục món ăn
    /// URL: /api/admin/category/insert?categoryName=...&description=...
    /// </summary>
    /// <param name="categoryName">Tên danh mục (bắt buộc)</param>
    /// <param name="description">Mô tả danh mục (tùy chọn)</param>
    [HttpPost("insert")]
    public async Task<IActionResult> InsertCategory([FromQuery] string categoryName, [FromQuery] string? description)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            return BadRequest(new { success = false, message = "Tên danh mục không được để trống!" });
        }
        
        var category = new FoodCategory
        {
            CategoryName = categoryName.Trim(),
            Description = description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.FoodCategories.Add(category);
        await _dbContext.SaveChangesAsync();

        return Ok(new { success = true, message = "Thêm danh mục thành công!", data = category });
    }

    /// <summary>
    /// [PUT] Cập nhật thông tin danh mục món ăn
    /// URL: /api/admin/category/update?categoryId=...&categoryName=...&description=...
    /// </summary>
    /// <param name="categoryId">ID danh mục cần sửa</param>
    /// <param name="categoryName">Tên danh mục mới</param>
    /// <param name="description">Mô tả mới</param>
    [HttpPut("update")]
    public async Task<IActionResult> UpdateCategory([FromQuery] int categoryId, [FromQuery] string categoryName, [FromQuery] string? description)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            return BadRequest(new { success = false, message = "Tên danh mục không được để trống!" });
        }

        var category = await _dbContext.FoodCategories.FindAsync(categoryId);
        if (category == null)
        {
            return NotFound(new { success = false, message = "Danh mục không tồn tại!" });
        }

        category.CategoryName = categoryName.Trim();
        category.Description = description?.Trim();
        category.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Ok(new { success = true, message = "Cập nhật danh mục thành công!" });
    }

    /// <summary>
    /// [DELETE] Xóa danh mục món ăn (nếu không có món ràng buộc)
    /// URL: /api/admin/category/delete?categoryId=...
    /// </summary>
    /// <param name="categoryId">ID danh mục cần xóa</param>
    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteCategory([FromQuery] int categoryId)
    {
        var category = await _dbContext.FoodCategories.FindAsync(categoryId);
        if (category == null)
        {
            return NotFound(new { success = false, message = "Danh mục không tồn tại!" });
        }

        // Kiểm tra xem danh mục có đang chứa món ăn nào không
        var hasFoods = await _dbContext.Foods.AnyAsync(f => f.CategoryId == categoryId);
        if (hasFoods)
        {
            // Thay vì xóa cứng, tắt IsActive để không làm mất dữ liệu lịch sử
            category.IsActive = false;
            category.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            return Ok(new { success = true, message = "Danh mục đang có món ăn, đã chuyển sang trạng thái Ẩn." });
        }

        _dbContext.FoodCategories.Remove(category);
        await _dbContext.SaveChangesAsync();

        return Ok(new { success = true, message = "Xóa danh mục thành công!" });
    }
}
