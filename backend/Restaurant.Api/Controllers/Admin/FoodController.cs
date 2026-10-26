using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;

using System.Security.Claims;
using Restaurant.Api.Services;

namespace Restaurant.Api.Controllers.Admin;

// ==============================================================
// KHUÔN MẪU DỮ LIỆU MÓN ĂN & BIẾN THỂ
// ==============================================================
public class VariantRequest
{
    public int? VariantId { get; set; }
    public string VariantName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Unit { get; set; } = "Phần";
}

public class InsertFoodRequest
{
    public int CategoryId { get; set; }
    public string FoodName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal MinPrice { get; set; }
    public List<VariantRequest> Variants { get; set; } = new List<VariantRequest>();
}

public class UpdateFoodRequest
{
    public int FoodId { get; set; }
    public int CategoryId { get; set; }
    public string FoodName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal MinPrice { get; set; }
    public string FoodStatus { get; set; } = "available";
    public bool IsActive { get; set; }
    public List<VariantRequest> Variants { get; set; } = new List<VariantRequest>();
}

public class UploadFoodImageRequest
{
    public IFormFile File { get; set; } = null!;
}
// ==============================================================

[Route("api/admin/food")]
[Authorize(Roles = "admin,manager")]
[ApiController]
public class FoodController : ControllerBase
{
    private readonly RestaurantDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;

    public FoodController(RestaurantDbContext dbContext, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetList([FromQuery] bool? isActive = null)
    {
        var query = _dbContext.Foods.AsQueryable();
        
        if (isActive.HasValue)
        {
            query = query.Where(f => f.IsActive == isActive.Value);
        }

        var foods = await query
            .Select(f => new 
            { 
                f.FoodId, 
                f.FoodName, 
                f.Description,
                f.ImageUrl,
                f.MinPrice,
                f.FoodStatus,
                f.IsActive,
                f.CategoryId,
                CategoryName = f.Category.CategoryName,
                
                // Món đang bán thì chỉ lấy biến thể active, món ngừng bán thì lấy hết để hiển thị lại
                Variants = f.FoodVariants
                    .Where(v => v.IsActive || !f.IsActive)
                    .Select(v => new 
                    {
                        v.VariantId,
                        v.VariantName,
                        v.Price,
                        v.Unit,
                        v.SortOrder,
                        v.IsActive 
                    }).ToList()
            })
            .ToListAsync();

        return Ok(new { data = foods }); 
    }

    [HttpPost("upload-image")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadImage([FromForm] UploadFoodImageRequest request)
    {
        var file = request?.File;
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Vui lòng chọn file hình ảnh hợp lệ!" });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return BadRequest(new { message = "Định dạng file không hỗ trợ. Vui lòng chọn ảnh JPG, PNG hoặc WEBP!" });

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "foods");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var fileName = $"food-{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var fullUrl = $"{Request.Scheme}://{Request.Host}/uploads/foods/{fileName}";

        return Ok(new 
        { 
            message = "Tải ảnh món ăn lên thành công!", 
            url = fullUrl 
        });
    }

    [HttpPost("insert")]
    public async Task<IActionResult> InsertFood([FromBody] InsertFoodRequest request)
    {
        if (string.IsNullOrEmpty(request.FoodName))
            return BadRequest(new { message = "Tên món ăn không được để trống!" });

        var category = await _dbContext.FoodCategories.FindAsync(request.CategoryId);
        if (category == null)
            return BadRequest(new { message = "Danh mục không tồn tại. Vui lòng chọn danh mục khác!" });

        var food = new Food
        {
            CategoryId = request.CategoryId,
            FoodName = request.FoodName,
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            MinPrice = request.MinPrice,
            FoodStatus = "available", 
            IsActive = true
        };

        _dbContext.Foods.Add(food);
        await _dbContext.SaveChangesAsync(); 

        if (request.Variants != null && request.Variants.Count > 0)
        {
            int sortIndex = 1;
            foreach (var v in request.Variants)
            {
                var variant = new FoodVariant
                {
                    FoodId = food.FoodId, 
                    VariantName = string.IsNullOrWhiteSpace(v.VariantName) ? "Phần Tiêu Chuẩn" : v.VariantName,
                    Price = v.Price,
                    Unit = string.IsNullOrEmpty(v.Unit) ? "Phần" : v.Unit,
                    SortOrder = sortIndex++,
                    IsActive = true
                };

                _dbContext.FoodVariants.Add(variant);
            }
            await _dbContext.SaveChangesAsync();
        }
        else
        {
            // Mặc định luôn tạo 1 biến thể tiêu chuẩn nếu không truyền
            var defaultVariant = new FoodVariant
            {
                FoodId = food.FoodId,
                VariantName = "Phần Tiêu Chuẩn",
                Price = food.MinPrice,
                Unit = "Phần",
                SortOrder = 1,
                IsActive = true
            };
        _dbContext.FoodVariants.Add(defaultVariant);
        await _dbContext.SaveChangesAsync();
    }

    var adminId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var aid) ? (int?)aid : null;
    await _auditLogService.LogAsync(
        adminId,
        "food_created",
        "Food",
        food.FoodId.ToString(),
        null,
        new { foodName = food.FoodName, minPrice = food.MinPrice, categoryId = food.CategoryId });

    return Ok(new { message = "Thêm món ăn và biến thể thành công!", foodId = food.FoodId });
}

[HttpPut("update")]
public async Task<IActionResult> UpdateFood([FromBody] UpdateFoodRequest request)
{
    if (string.IsNullOrEmpty(request.FoodName))
        return BadRequest(new { message = "Tên món ăn không được để trống!" });

    var food = await _dbContext.Foods
        .Include(f => f.FoodVariants) 
        .FirstOrDefaultAsync(f => f.FoodId == request.FoodId);

    if (food == null)
        return BadRequest(new { message = "Không tìm thấy món ăn này!" });

    var category = await _dbContext.FoodCategories.FindAsync(request.CategoryId);
    if (category == null)
        return BadRequest(new { message = "Danh mục không tồn tại!" });

    var oldSnapshot = new {
        foodName = food.FoodName,
        minPrice = food.MinPrice,
        categoryId = food.CategoryId,
        foodStatus = food.FoodStatus,
        isActive = food.IsActive,
        variants = food.FoodVariants.Select(v => new { v.VariantName, v.Price }).ToList()
    };

    // 1. Cập nhật thông tin món mẹ
    food.CategoryId = request.CategoryId;
    food.FoodName = request.FoodName;
    food.Description = request.Description;
    food.ImageUrl = request.ImageUrl;
    food.MinPrice = request.MinPrice;
    food.FoodStatus = request.FoodStatus;
    food.IsActive = request.IsActive;

    // 2. Xử lý đồng bộ biến thể: Cập nhật, Thêm mới và Xóa các biến thể bị bỏ
    if (request.Variants != null)
    {
        var submittedIds = request.Variants
            .Where(v => v.VariantId.GetValueOrDefault() > 0)
            .Select(v => v.VariantId.Value)
            .ToHashSet();

        // Các biến thể cũ không còn nằm trong danh sách gửi lên -> Xóa
        var removedVariants = food.FoodVariants
            .Where(v => !submittedIds.Contains(v.VariantId))
            .ToList();

        foreach (var rem in removedVariants)
        {
            var hasHistory = await _dbContext.OrderItems.AnyAsync(oi => oi.VariantId == rem.VariantId)
                          || await _dbContext.InvoiceItems.AnyAsync(ii => ii.VariantId == rem.VariantId);

            if (hasHistory)
            {
                rem.IsActive = false; // Đã từng có đơn hàng thì ẩn đi
            }
            else
            {
                _dbContext.FoodVariants.Remove(rem); // Chưa từng bán thì xóa cứng khỏi DB
            }
        }

        int sortIndex = 1;
        foreach (var v in request.Variants)
        {
            if (v.VariantId.GetValueOrDefault() > 0)
            {
                var existingVariant = food.FoodVariants.FirstOrDefault(x => x.VariantId == v.VariantId.GetValueOrDefault());
                if (existingVariant != null)
                {
                    existingVariant.VariantName = v.VariantName;
                    existingVariant.Price = v.Price;
                    existingVariant.Unit = string.IsNullOrEmpty(v.Unit) ? "Phần" : v.Unit;
                    existingVariant.SortOrder = sortIndex++;
                    existingVariant.IsActive = true;
                }
            }
            else
            {
                var newVariant = new FoodVariant
                {
                    FoodId = food.FoodId,
                    VariantName = string.IsNullOrWhiteSpace(v.VariantName) ? "Phần Tiêu Chuẩn" : v.VariantName,
                    Price = v.Price,
                    Unit = string.IsNullOrEmpty(v.Unit) ? "Phần" : v.Unit,
                    SortOrder = sortIndex++,
                    IsActive = true
                };
                _dbContext.FoodVariants.Add(newVariant);
            }
        }
    }

    // Đảm bảo món không bao giờ bị rỗng biến thể
    if (!food.FoodVariants.Any(v => v.IsActive))
    {
        var fallback = new FoodVariant
        {
            FoodId = food.FoodId,
            VariantName = "Phần Tiêu Chuẩn",
            Price = food.MinPrice,
            Unit = "Phần",
            SortOrder = 1,
            IsActive = true
        };
        _dbContext.FoodVariants.Add(fallback);
    }

    await _dbContext.SaveChangesAsync();

    var updateAdminId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uaid) ? (int?)uaid : null;
    var newSnapshot = new {
        foodName = food.FoodName,
        minPrice = food.MinPrice,
        categoryId = food.CategoryId,
        foodStatus = food.FoodStatus,
        isActive = food.IsActive,
        variants = food.FoodVariants.Where(v => v.IsActive).Select(v => new { v.VariantName, v.Price }).ToList()
    };
    await _auditLogService.LogAsync(updateAdminId, "food_updated", "Food", food.FoodId.ToString(), oldSnapshot, newSnapshot);

    return Ok(new { message = "Cập nhật món ăn thành công!" });
}

[HttpDelete("delete")]
public async Task<IActionResult> DeleteFood([FromQuery] int foodId)
{
    var food = await _dbContext.Foods
        .Include(f => f.FoodVariants)
        .FirstOrDefaultAsync(f => f.FoodId == foodId);

    if (food == null)
        return BadRequest(new { message = "Không tìm thấy món ăn này!" });
    
    var oldSnapshot = new { foodName = food.FoodName, minPrice = food.MinPrice, categoryId = food.CategoryId, isActive = true };
    food.IsActive = false;
    await _dbContext.SaveChangesAsync();

    var delAdminId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var daid) ? (int?)daid : null;
    await _auditLogService.LogAsync(delAdminId, "food_deleted", "Food", foodId.ToString(), oldSnapshot, new { isActive = false, foodName = food.FoodName });

    return Ok(new { message = "Đã ngừng bán món ăn thành công!" });
}

/// <summary>
/// Xóa vĩnh viễn món ăn khỏi cơ sở dữ liệu.
/// Cho phép xóa vì phiếu hóa đơn và đơn hàng đã lưu vết tên món và giá tiền tại thời điểm thanh toán.
/// </summary>
[HttpDelete("permanent-delete")]
public async Task<IActionResult> PermanentDeleteFood([FromQuery] int foodId)
{
    var food = await _dbContext.Foods
        .Include(f => f.FoodVariants)
        .FirstOrDefaultAsync(f => f.FoodId == foodId);

    if (food == null)
        return NotFound(new { message = "Không tìm thấy món ăn này!" });

    // Kiểm tra xem món ăn có đang nằm trong order nào chưa hoàn tất không
    var hasActiveOrder = await _dbContext.OrderItems
        .AnyAsync(oi => oi.FoodId == foodId 
            && oi.Order.OrderStatus != "completed" 
            && oi.Order.OrderStatus != "cancelled" 
            && oi.ItemStatus != "cancelled");
    if (hasActiveOrder)
        return BadRequest(new { message = $"Món \"{food.FoodName}\" đang nằm trong đơn gọi món đang phục vụ khách. Vui lòng hoàn tất hoặc hủy món trong đơn trước khi xóa!" });

    var foodName = food.FoodName;
    var oldSnapshot = new { foodName = food.FoodName, minPrice = food.MinPrice, categoryId = food.CategoryId };

    // 1. Xóa định lượng công thức món (FoodIngredients) nếu có
    var boms = await _dbContext.FoodIngredients.Where(fi => fi.FoodId == foodId).ToListAsync();
    if (boms.Any())
    {
        _dbContext.FoodIngredients.RemoveRange(boms);
    }

    // 2. Xóa định mức khẩu phần ngày (DailyPortionControls) nếu có
    var portions = await _dbContext.DailyPortionControls.Where(dp => dp.FoodId == foodId).ToListAsync();
    if (portions.Any())
    {
        _dbContext.DailyPortionControls.RemoveRange(portions);
    }

    // 3. Xóa các biến thể (FoodVariants)
    if (food.FoodVariants.Any())
    {
        _dbContext.FoodVariants.RemoveRange(food.FoodVariants);
    }

    // 4. Xóa món ăn khỏi CSDL
    _dbContext.Foods.Remove(food);
    await _dbContext.SaveChangesAsync();

    var pDelAdminId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var pdaid) ? (int?)pdaid : null;
    await _auditLogService.LogAsync(pDelAdminId, "food_deleted", "Food", foodId.ToString(), oldSnapshot, new { permanentlyDeleted = true, foodName });

    return Ok(new { message = $"Đã xóa vĩnh viễn món \"{foodName}\" thành công!" });
}

    [HttpPatch("reactivate")]
    public async Task<IActionResult> ReactivateFood([FromQuery] int foodId)
    {
        var food = await _dbContext.Foods
            .Include(f => f.FoodVariants)
            .FirstOrDefaultAsync(f => f.FoodId == foodId);

        if (food == null)
            return BadRequest(new { message = "Không tìm thấy món ăn này!" });

        food.IsActive = true;

        // Kích hoạt lại toàn bộ các biến thể của món này
        foreach (var variant in food.FoodVariants)
        {
            variant.IsActive = true;
        }

        // Nếu món không có biến thể nào đang hoạt động, tạo 1 biến thể mặc định
        if (!food.FoodVariants.Any(v => v.IsActive))
        {
            var defaultVariant = new FoodVariant
            {
                FoodId = food.FoodId,
                VariantName = "Phần Tiêu Chuẩn",
                Price = food.MinPrice > 0 ? food.MinPrice : 0,
                Unit = "Phần",
                SortOrder = 1,
                IsActive = true
            };
            _dbContext.FoodVariants.Add(defaultVariant);
        }

        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Đã mở bán lại món ăn và các biến thể thành công!" });
    }

    [HttpDelete("variant/delete")]
    public async Task<IActionResult> DeleteVariant([FromQuery] int variantId)
    {
        var variant = await _dbContext.FoodVariants.FindAsync(variantId);
        if (variant == null)
            return BadRequest(new { message = "Không tìm thấy biến thể này!" });

        var hasHistory = await _dbContext.OrderItems.AnyAsync(oi => oi.VariantId == variantId)
                      || await _dbContext.InvoiceItems.AnyAsync(ii => ii.VariantId == variantId);

        if (hasHistory)
        {
            variant.IsActive = false;
        }
        else
        {
            _dbContext.FoodVariants.Remove(variant);
        }

        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Xóa biến thể thành công!" });
    }
}
