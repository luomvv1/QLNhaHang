using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;
using System.Security.Claims;

namespace Restaurant.Api.Controllers.Reviews;

// ==============================================================
// DTOs CHO ĐÁNH GIÁ MÓN ĂN & TRẢI NGHIỆM (M14-R1 / UC-E05)
// ==============================================================

public class CreateReviewRequest
{
    public int? InvoiceId { get; set; }
    public byte Rating { get; set; } // 1 - 5
    public string? Comment { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
}

public class AdminReplyReviewRequest
{
    public string Reply { get; set; } = string.Empty;
}

[Route("api/reviews")]
[ApiController]
public class ReviewController : ControllerBase
{
    private readonly RestaurantDbContext _dbContext;

    public ReviewController(RestaurantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private int? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out var userId) ? userId : null;
    }

    /// <summary>
    /// Lấy danh sách đánh giá công khai kèm số sao trung bình (Customer Portal)
    /// </summary>
    [HttpGet("list")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicReviews([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var baseQuery = _dbContext.Reviews.Where(r => r.IsVisible);

        var totalReviews = await baseQuery.CountAsync();
        var avgRating = totalReviews > 0 ? await baseQuery.AverageAsync(r => (double)r.Rating) : 5.0;

        // Thống kê số lượng theo từng mức sao
        var ratingCounts = await baseQuery
            .GroupBy(r => r.Rating)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Rating, x => x.Count);

        var reviews = await baseQuery
            .Include(r => r.Customer)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.ReviewId,
                r.Rating,
                r.Comment,
                r.Reply,
                r.CreatedAt,
                CustomerName = r.Customer != null ? r.Customer.FullName : "Thực khách Hương Sen",
                CustomerAvatar = r.Customer != null ? r.Customer.AvatarUrl : null
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                totalReviews,
                averageRating = Math.Round(avgRating, 1),
                ratingBreakdown = new
                {
                    star5 = ratingCounts.GetValueOrDefault((byte)5, 0),
                    star4 = ratingCounts.GetValueOrDefault((byte)4, 0),
                    star3 = ratingCounts.GetValueOrDefault((byte)3, 0),
                    star2 = ratingCounts.GetValueOrDefault((byte)2, 0),
                    star1 = ratingCounts.GetValueOrDefault((byte)1, 0)
                },
                reviews
            }
        });
    }

    /// <summary>
    /// Khách hàng gửi đánh giá mới
    /// </summary>
    [HttpPost("create")]
    [Authorize(Roles = "customer")]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequest request)
    {
        if (request == null)
            return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });

        if (request.Rating < 1 || request.Rating > 5)
            return BadRequest(new { success = false, message = "Điểm đánh giá phải từ 1 đến 5 sao." });

        var currentUserId = GetCurrentUserId();
        var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.UserId == currentUserId);
        if (customer == null) return Forbid();
        if (request.InvoiceId.HasValue && !await _dbContext.Invoices.AnyAsync(invoice =>
            invoice.InvoiceId == request.InvoiceId && invoice.InvoiceStatus == "paid"
            && (invoice.CustomerId == customer.CustomerId || invoice.Order.CustomerId == customer.CustomerId)))
            return Forbid();

        var review = new Review
        {
            CustomerId = customer.CustomerId,
            InvoiceId = request.InvoiceId,
            Rating = request.Rating,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            Reply = null,
            IsVisible = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Reviews.Add(review);
        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Cảm ơn quý khách đã gửi đánh giá và đóng góp ý kiến cho nhà hàng!",
            data = new { review.ReviewId }
        });
    }

    // ==============================================================
    // ADMIN REVIEWS MANAGEMENT
    // ==============================================================

    [HttpGet("admin/list")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> GetAdminReviews(
        [FromQuery] byte? rating = null,
        [FromQuery] bool? isVisible = null,
        [FromQuery] string? search = null)
    {
        var query = _dbContext.Reviews
            .Include(r => r.Customer)
            .Include(r => r.Invoice)
            .AsQueryable();

        if (rating.HasValue && rating.Value >= 1 && rating.Value <= 5)
        {
            query = query.Where(r => r.Rating == rating.Value);
        }

        if (isVisible.HasValue)
        {
            query = query.Where(r => r.IsVisible == isVisible.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(r => (r.Comment != null && r.Comment.ToLower().Contains(s)) ||
                                     r.Customer.FullName.ToLower().Contains(s) ||
                                     r.Customer.Phone.Contains(s));
        }

        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.ReviewId,
                r.CustomerId,
                CustomerName = r.Customer.FullName,
                CustomerPhone = r.Customer.Phone,
                r.InvoiceId,
                InvoiceCode = r.Invoice != null ? r.Invoice.InvoiceCode : null,
                r.Rating,
                r.Comment,
                r.Reply,
                r.IsVisible,
                r.CreatedAt,
                r.UpdatedAt
            })
            .ToListAsync();

        return Ok(new { success = true, data = reviews });
    }

    [HttpPost("admin/{id}/toggle-visibility")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> ToggleVisibility(int id)
    {
        var review = await _dbContext.Reviews.FirstOrDefaultAsync(r => r.ReviewId == id);
        if (review == null)
            return NotFound(new { success = false, message = "Không tìm thấy đánh giá này." });

        review.IsVisible = !review.IsVisible;
        review.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = review.IsVisible ? "Đã hiển thị đánh giá công khai." : "Đã ẩn đánh giá khỏi cổng khách hàng.",
            isVisible = review.IsVisible
        });
    }

    [HttpPost("admin/{id}/reply")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> ReplyReview(int id, [FromBody] AdminReplyReviewRequest request)
    {
        var review = await _dbContext.Reviews.FirstOrDefaultAsync(r => r.ReviewId == id);
        if (review == null)
            return NotFound(new { success = false, message = "Không tìm thấy đánh giá này." });

        review.Reply = string.IsNullOrWhiteSpace(request?.Reply) ? null : request.Reply.Trim();
        review.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Đã lưu phản hồi cho khách hàng thành công.",
            reply = review.Reply
        });
    }
}
