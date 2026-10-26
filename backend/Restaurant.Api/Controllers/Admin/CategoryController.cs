using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;

namespace Restaurant.Api.Controllers.Admin;

/// <summary>
/// Quản lý Danh mục Món ăn (Food Categories) dành cho Quản lý / Admin
/// Sử dụng trực tiếp Entity Model: FoodCategory
/// </summary>
[Route("api/admin/category")]
[Authorize(Roles = "admin,manager")]
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
                c.IsActive,
                FoodCount = c.Foods.Count(f => f.IsActive),
                TotalFoodCount = c.Foods.Count,
                SampleFoods = c.Foods
                    .Where(f => f.IsActive)
                    .OrderByDescending(f => f.FoodId)
                    .Take(4)
                    .Select(f => new 
                    {
                        f.FoodId,
                        f.FoodName,
                        f.ImageUrl,
                        f.MinPrice
                    })
                    .ToList()
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

    /// <summary>
    /// [DELETE] Xóa vĩnh viễn danh mục (hỗ trợ chuyển món sang danh mục khác hoặc xóa luôn toàn bộ món).
    /// </summary>
    [HttpDelete("permanent-delete")]
    public async Task<IActionResult> PermanentDeleteCategory(
        [FromQuery] int categoryId, 
        [FromQuery] int? transferToCategoryId = null, 
        [FromQuery] bool deleteFoods = false)
    {
        var category = await _dbContext.FoodCategories
            .Include(c => c.Foods)
                .ThenInclude(f => f.FoodVariants)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

        if (category == null)
            return NotFound(new { success = false, message = "Không tìm thấy danh mục món ăn này!" });

        var foods = category.Foods.ToList();

        if (!foods.Any())
        {
            _dbContext.FoodCategories.Remove(category);
            await _dbContext.SaveChangesAsync();
            return Ok(new { success = true, message = $"Đã xóa danh mục \"{category.CategoryName}\" thành công!" });
        }

        if (transferToCategoryId.HasValue && transferToCategoryId.Value > 0)
        {
            var targetCategory = await _dbContext.FoodCategories.FindAsync(transferToCategoryId.Value);
            if (targetCategory == null)
                return BadRequest(new { success = false, message = "Danh mục đích để chuyển món không tồn tại!" });

            foreach (var f in foods)
            {
                f.CategoryId = targetCategory.CategoryId;
                f.UpdatedAt = DateTime.UtcNow;
            }
            _dbContext.FoodCategories.Remove(category);
            await _dbContext.SaveChangesAsync();

            return Ok(new { success = true, message = $"Đã chuyển {foods.Count} món ăn sang \"{targetCategory.CategoryName}\" và xóa danh mục \"{category.CategoryName}\" thành công!" });
        }
        else if (deleteFoods)
        {
            var foodIds = foods.Select(f => f.FoodId).ToList();

            // Kiểm tra món có trong đơn đang phục vụ không
            var hasActiveOrder = await _dbContext.OrderItems
                .AnyAsync(oi => foodIds.Contains(oi.FoodId)
                    && oi.Order.OrderStatus != "completed" 
                    && oi.Order.OrderStatus != "cancelled" 
                    && oi.ItemStatus != "cancelled");
            if (hasActiveOrder)
                return BadRequest(new { success = false, message = "Một số món trong danh mục này đang nằm trong đơn gọi món đang phục vụ khách. Không thể xóa!" });

            // Xóa BOM
            var boms = await _dbContext.FoodIngredients.Where(fi => foodIds.Contains(fi.FoodId)).ToListAsync();
            _dbContext.FoodIngredients.RemoveRange(boms);

            // Xóa Portions
            var portions = await _dbContext.DailyPortionControls.Where(dp => foodIds.Contains(dp.FoodId)).ToListAsync();
            _dbContext.DailyPortionControls.RemoveRange(portions);

            // Xóa Variants
            var variants = await _dbContext.FoodVariants.Where(fv => foodIds.Contains(fv.FoodId)).ToListAsync();
            _dbContext.FoodVariants.RemoveRange(variants);

            // Xóa Foods
            _dbContext.Foods.RemoveRange(foods);

            // Xóa Category
            _dbContext.FoodCategories.Remove(category);
            await _dbContext.SaveChangesAsync();

            return Ok(new { success = true, message = $"Đã xóa danh mục \"{category.CategoryName}\" cùng toàn bộ {foods.Count} món ăn thành công!" });
        }
        else
        {
            return BadRequest(new { success = false, message = $"Danh mục đang có {foods.Count} món ăn. Vui lòng chọn chuyển món sang danh mục khác hoặc chọn xóa kèm các món!" });
        }
    }

    /// <summary>
    /// [PATCH] Mở lại hoạt động cho danh mục bị ẩn
    /// </summary>
    [HttpPatch("reactivate")]
    public async Task<IActionResult> ReactivateCategory([FromQuery] int categoryId)
    {
        var category = await _dbContext.FoodCategories.FindAsync(categoryId);
        if (category == null)
            return NotFound(new { success = false, message = "Danh mục không tồn tại!" });

        category.IsActive = true;
        category.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return Ok(new { success = true, message = $"Đã mở lại hoạt động cho danh mục \"{category.CategoryName}\" thành công!" });
    }
}
