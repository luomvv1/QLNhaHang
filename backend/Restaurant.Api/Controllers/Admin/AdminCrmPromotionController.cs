using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;
using System.Security.Claims;

namespace Restaurant.Api.Controllers.Admin;

public class CustomerCreateDto
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int InitialPoints { get; set; } = 0;
}

public class CustomerUpdateDto
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
}

public class AdjustPointsDto
{
    public int Points { get; set; }
    public string Reason { get; set; } = "Điều chỉnh điểm từ quản trị viên";
}

public class PromotionCreateDto
{
    public string PromotionCode { get; set; } = string.Empty;
    public string PromotionName { get; set; } = string.Empty;
    public string PromotionType { get; set; } = "percent"; // "percent" | "fixed"
    public decimal PromotionValue { get; set; }
    public decimal MinimumOrderValue { get; set; } = 0;
    public int? UsageLimit { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PromotionUpdateDto
{
    public string PromotionName { get; set; } = string.Empty;
    public string PromotionType { get; set; } = "percent";
    public decimal PromotionValue { get; set; }
    public decimal MinimumOrderValue { get; set; } = 0;
    public int? UsageLimit { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public bool IsActive { get; set; } = true;
}

[ApiController]
[Route("api/admin/crm")]
[Authorize(Roles = "staff,manager,admin")]
public class AdminCrmPromotionController : ControllerBase
{
    private readonly RestaurantDbContext _dbContext;

    public AdminCrmPromotionController(RestaurantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private int GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out var userId) ? userId : 1;
    }

    private static string CalculateRank(decimal totalSpent)
    {
        if (totalSpent >= 15_000_000m) return "Diamond";
        if (totalSpent >= 5_000_000m) return "Gold";
        if (totalSpent >= 1_000_000m) return "Silver";
        return "Bronze";
    }

    private static string CalculateRankName(string rank)
    {
        return rank switch
        {
            "Diamond" => "Kim Cương",
            "Gold" => "Vàng",
            "Silver" => "Bạc",
            _ => "Đồng"
        };
    }

    // =========================================================================
    // 1. TỔNG QUAN KPI CRM & VOUCHER
    // =========================================================================
    [HttpGet("stats")]
    public async Task<IActionResult> GetCrmStats()
    {
        var totalCustomers = await _dbContext.Customers.CountAsync();
        var activeCustomers = await _dbContext.Customers.CountAsync(c => c.IsActive);
        var totalPointsInCirculation = await _dbContext.Customers.SumAsync(c => (long)c.PointsBalance);
        var totalCustomerRevenue = await _dbContext.Customers.SumAsync(c => c.TotalSpent);

        // Đếm hội viên VIP (Chi tiêu >= 5 triệu)
        var vipCustomersCount = await _dbContext.Customers.CountAsync(c => c.TotalSpent >= 5_000_000m);

        // Voucher stats
        var now = DateTime.UtcNow;
        var activeVouchers = await _dbContext.Promotions.CountAsync(p => p.IsActive && p.StartAt <= now && p.EndAt >= now);
        var totalVouchersUsed = await _dbContext.Promotions.SumAsync(p => (long)p.UsedCount);
        var totalDiscountFund = await _dbContext.Invoices.SumAsync(i => i.DiscountAmount);

        return Ok(new
        {
            TotalCustomers = totalCustomers,
            ActiveCustomers = activeCustomers,
            VipCustomersCount = vipCustomersCount,
            TotalPointsInCirculation = totalPointsInCirculation,
            TotalCustomerRevenue = totalCustomerRevenue,
            ActiveVouchers = activeVouchers,
            TotalVouchersUsed = totalVouchersUsed,
            TotalDiscountFund = totalDiscountFund
        });
    }

    // =========================================================================
    // 2. QUẢN LÝ KHÁCH HÀNG (CRM & LOYALTY)
    // =========================================================================
    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] string? search = null,
        [FromQuery] string? rank = null,
        [FromQuery] string? status = null,
        [FromQuery] string? sortBy = "spent_desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15)
    {
        var query = _dbContext.Customers.AsQueryable();

        // 1. Tìm kiếm
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c =>
                c.FullName.ToLower().Contains(s) ||
                c.Phone.Contains(s) ||
                (c.Email != null && c.Email.ToLower().Contains(s)));
        }

        // 2. Lọc trạng thái
        if (!string.IsNullOrWhiteSpace(status) && status.ToLower() != "all")
        {
            bool isActive = status.ToLower() == "active";
            query = query.Where(c => c.IsActive == isActive);
        }

        // 3. Lọc theo hạng thành viên
        if (!string.IsNullOrWhiteSpace(rank) && rank.ToLower() != "all")
        {
            switch (rank.ToLower())
            {
                case "diamond":
                    query = query.Where(c => c.TotalSpent >= 15_000_000m);
                    break;
                case "gold":
                    query = query.Where(c => c.TotalSpent >= 5_000_000m && c.TotalSpent < 15_000_000m);
                    break;
                case "silver":
                    query = query.Where(c => c.TotalSpent >= 1_000_000m && c.TotalSpent < 5_000_000m);
                    break;
                case "bronze":
                    query = query.Where(c => c.TotalSpent < 1_000_000m);
                    break;
            }
        }

        // 4. Sắp xếp
        query = sortBy switch
        {
            "spent_asc" => query.OrderBy(c => c.TotalSpent),
            "points_desc" => query.OrderByDescending(c => c.PointsBalance),
            "points_asc" => query.OrderBy(c => c.PointsBalance),
            "visits_desc" => query.OrderByDescending(c => c.VisitCount),
            "recent" => query.OrderByDescending(c => c.CreatedAt),
            _ => query.OrderByDescending(c => c.TotalSpent)
        };

        var totalItems = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.CustomerId,
                c.UserId,
                c.FullName,
                c.Phone,
                c.Email,
                c.AvatarUrl,
                c.PointsBalance,
                c.TotalSpent,
                c.VisitCount,
                c.IsActive,
                c.CreatedAt,
                c.UpdatedAt
            })
            .ToListAsync();

        var result = items.Select(c =>
        {
            var rankKey = CalculateRank(c.TotalSpent);
            return new
            {
                c.CustomerId,
                c.UserId,
                c.FullName,
                c.Phone,
                c.Email,
                c.AvatarUrl,
                c.PointsBalance,
                c.TotalSpent,
                c.VisitCount,
                c.IsActive,
                c.CreatedAt,
                c.UpdatedAt,
                Rank = rankKey,
                RankName = CalculateRankName(rankKey)
            };
        }).ToList();

        return Ok(new
        {
            Items = result,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        });
    }

    [HttpGet("customers/{id}")]
    public async Task<IActionResult> GetCustomerDetail(int id)
    {
        var customer = await _dbContext.Customers
            .Include(c => c.Invoices)
            .Include(c => c.PointTransactions)
            .Include(c => c.PromotionUsages)
                .ThenInclude(pu => pu.Promotion)
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        if (customer == null)
            return NotFound(new { message = $"Không tìm thấy khách hàng #{id}" });

        var rankKey = CalculateRank(customer.TotalSpent);

        // Top 10 hóa đơn gần nhất
        var recentInvoices = customer.Invoices
            .OrderByDescending(i => i.IssuedAt)
            .Take(10)
            .Select(i => new
            {
                i.InvoiceId,
                i.InvoiceCode,
                i.TotalAmount,
                i.DiscountAmount,
                i.PointsEarned,
                i.PointsUsed,
                i.IssuedAt
            })
            .ToList();

        // Top 15 biến động điểm gần nhất
        var recentPointLogs = customer.PointTransactions
            .OrderByDescending(pt => pt.CreatedAt)
            .Take(15)
            .Select(pt => new
            {
                TransactionId = pt.PointTransactionId,
                pt.TransactionType,
                pt.Points,
                pt.BalanceAfter,
                pt.Note,
                pt.CreatedAt
            })
            .ToList();

        // Voucher đã dùng
        var usedVouchers = customer.PromotionUsages
            .OrderByDescending(pu => pu.UsedAt)
            .Take(10)
            .Select(pu => new
            {
                UsageId = pu.PromotionUsageId,
                pu.Promotion?.PromotionCode,
                pu.Promotion?.PromotionName,
                pu.DiscountAmount,
                pu.UsedAt
            })
            .ToList();

        return Ok(new
        {
            customer.CustomerId,
            customer.UserId,
            customer.FullName,
            customer.Phone,
            customer.Email,
            customer.AvatarUrl,
            customer.PointsBalance,
            customer.TotalSpent,
            customer.VisitCount,
            customer.IsActive,
            customer.CreatedAt,
            customer.UpdatedAt,
            Rank = rankKey,
            RankName = CalculateRankName(rankKey),
            Invoices = recentInvoices,
            PointLogs = recentPointLogs,
            UsedVouchers = usedVouchers
        });
    }

    [HttpPost("customers")]
    public async Task<IActionResult> CreateCustomer([FromBody] CustomerCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
            return BadRequest(new { message = "Vui lòng nhập họ và tên khách hàng." });

        if (string.IsNullOrWhiteSpace(dto.Phone))
            return BadRequest(new { message = "Vui lòng nhập số điện thoại." });

        var cleanPhone = dto.Phone.Trim();
        var existing = await _dbContext.Customers.AnyAsync(c => c.Phone == cleanPhone);
        if (existing)
            return BadRequest(new { message = $"Số điện thoại '{cleanPhone}' đã tồn tại trong hệ thống." });

        var now = DateTime.UtcNow;
        var customer = new Customer
        {
            FullName = dto.FullName.Trim(),
            Phone = cleanPhone,
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
            PointsBalance = Math.Max(0, dto.InitialPoints),
            TotalSpent = 0,
            VisitCount = 0,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();

        if (dto.InitialPoints > 0)
        {
            _dbContext.PointTransactions.Add(new PointTransaction
            {
                CustomerId = customer.CustomerId,
                TransactionType = "earn",
                Points = dto.InitialPoints,
                BalanceAfter = dto.InitialPoints,
                Note = "Điểm tặng khi tạo tài khoản thành viên mới",
                CreatedBy = GetCurrentUserId(),
                CreatedAt = now
            });
            await _dbContext.SaveChangesAsync();
        }

        return Ok(new
        {
            message = "Tạo khách hàng mới thành công!",
            customer = new
            {
                customer.CustomerId,
                customer.FullName,
                customer.Phone,
                customer.Email,
                customer.PointsBalance
            }
        });
    }

    [HttpPut("customers/{id}")]
    public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CustomerUpdateDto dto)
    {
        var customer = await _dbContext.Customers.FindAsync(id);
        if (customer == null)
            return NotFound(new { message = $"Không tìm thấy khách hàng #{id}" });

        if (string.IsNullOrWhiteSpace(dto.FullName))
            return BadRequest(new { message = "Họ và tên không được để trống." });

        if (string.IsNullOrWhiteSpace(dto.Phone))
            return BadRequest(new { message = "Số điện thoại không được để trống." });

        var cleanPhone = dto.Phone.Trim();
        var phoneTaken = await _dbContext.Customers.AnyAsync(c => c.Phone == cleanPhone && c.CustomerId != id);
        if (phoneTaken)
            return BadRequest(new { message = $"Số điện thoại '{cleanPhone}' đã được sử dụng bởi khách hàng khác." });

        customer.FullName = dto.FullName.Trim();
        customer.Phone = cleanPhone;
        customer.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        customer.IsActive = dto.IsActive;
        customer.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = "Cập nhật thông tin khách hàng thành công!",
            customer = new
            {
                customer.CustomerId,
                customer.FullName,
                customer.Phone,
                customer.Email,
                customer.IsActive
            }
        });
    }

    [HttpPost("customers/{id}/adjust-points")]
    public async Task<IActionResult> AdjustPoints(int id, [FromBody] AdjustPointsDto dto)
    {
        var customer = await _dbContext.Customers.FindAsync(id);
        if (customer == null)
            return NotFound(new { message = $"Không tìm thấy khách hàng #{id}" });

        if (dto.Points == 0)
            return BadRequest(new { message = "Số điểm điều chỉnh phải khác 0." });

        var newBalance = customer.PointsBalance + dto.Points;
        if (newBalance < 0)
            return BadRequest(new { message = $"Không thể trừ {Math.Abs(dto.Points)} điểm vì số dư hiện tại chỉ còn {customer.PointsBalance} điểm." });

        customer.PointsBalance = newBalance;
        customer.UpdatedAt = DateTime.UtcNow;

        var currentStaffId = GetCurrentUserId();
        var pointLog = new PointTransaction
        {
            CustomerId = customer.CustomerId,
            TransactionType = dto.Points > 0 ? "earn" : "redeem",
            Points = dto.Points,
            BalanceAfter = newBalance,
            Note = string.IsNullOrWhiteSpace(dto.Reason)
                ? (dto.Points > 0 ? $"Quản trị viên cộng {dto.Points} điểm" : $"Quản trị viên trừ {Math.Abs(dto.Points)} điểm")
                : dto.Reason.Trim(),
            CreatedBy = currentStaffId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.PointTransactions.Add(pointLog);
        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = $"Đã {(dto.Points > 0 ? "cộng" : "trừ")} {Math.Abs(dto.Points)} điểm thành công!",
            NewPointsBalance = newBalance,
            PointLog = new
            {
                TransactionId = pointLog.PointTransactionId,
                pointLog.Points,
                pointLog.BalanceAfter,
                pointLog.Note,
                pointLog.CreatedAt
            }
        });
    }

    [HttpGet("customers/{id}/points-history")]
    public async Task<IActionResult> GetCustomerPointsHistory(
        int id,
        [FromQuery] string? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var customer = await _dbContext.Customers.FindAsync(id);
        if (customer == null)
            return NotFound(new { message = $"Không tìm thấy khách hàng #{id}" });

        var query = _dbContext.PointTransactions
            .Include(pt => pt.Invoice)
            .Include(pt => pt.CreatedByNavigation)
            .Where(pt => pt.CustomerId == id);

        if (!string.IsNullOrWhiteSpace(type) && type.ToLower() != "all")
        {
            var t = type.ToLower().Trim();
            if (t == "earn")
            {
                query = query.Where(pt => pt.Points > 0);
            }
            else if (t == "redeem")
            {
                query = query.Where(pt => pt.Points < 0);
            }
            else
            {
                query = query.Where(pt => pt.TransactionType.ToLower() == t);
            }
        }

        var totalItems = await query.CountAsync();
        var logs = await query
            .OrderByDescending(pt => pt.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(pt => new
            {
                TransactionId = pt.PointTransactionId,
                pt.TransactionType,
                pt.Points,
                pt.BalanceAfter,
                pt.Note,
                pt.InvoiceId,
                InvoiceCode = pt.Invoice != null ? pt.Invoice.InvoiceCode : null,
                CreatedByName = pt.CreatedByNavigation != null ? pt.CreatedByNavigation.FullName : null,
                pt.CreatedAt
            })
            .ToListAsync();

        var allLogs = await _dbContext.PointTransactions
            .Where(pt => pt.CustomerId == id)
            .Select(pt => pt.Points)
            .ToListAsync();

        var totalEarned = allLogs.Where(p => p > 0).Sum();
        var totalRedeemed = Math.Abs(allLogs.Where(p => p < 0).Sum());

        var rank = CalculateRank(customer.TotalSpent);

        return Ok(new
        {
            Customer = new
            {
                customer.CustomerId,
                customer.FullName,
                customer.Phone,
                customer.Email,
                customer.PointsBalance,
                customer.TotalSpent,
                Rank = rank,
                RankName = CalculateRankName(rank)
            },
            Stats = new
            {
                CurrentBalance = customer.PointsBalance,
                TotalEarned = totalEarned,
                TotalRedeemed = totalRedeemed,
                TotalTransactions = allLogs.Count
            },
            Items = logs,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        });
    }

    [HttpDelete("customers/{id}")]
    public async Task<IActionResult> ToggleCustomerStatus(int id)
    {
        var customer = await _dbContext.Customers.FindAsync(id);
        if (customer == null)
            return NotFound(new { message = $"Không tìm thấy khách hàng #{id}" });

        customer.IsActive = !customer.IsActive;
        customer.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = customer.IsActive ? "Đã kích hoạt lại tài khoản khách hàng!" : "Đã tạm khóa tài khoản khách hàng!",
            customer.CustomerId,
            customer.IsActive
        });
    }

    // =========================================================================
    // 3. QUẢN LÝ KHUYẾN MÃI & VOUCHER (PROMOTIONS)
    // =========================================================================
    [HttpGet("promotions")]
    public async Task<IActionResult> GetPromotions(
        [FromQuery] string? search = null,
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _dbContext.Promotions.AsQueryable();
        var now = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p =>
                p.PromotionCode.ToLower().Contains(s) ||
                p.PromotionName.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(type) && type.ToLower() != "all")
        {
            query = query.Where(p => p.PromotionType.ToLower() == type.ToLower().Trim());
        }

        if (!string.IsNullOrWhiteSpace(status) && status.ToLower() != "all")
        {
            switch (status.ToLower())
            {
                case "active":
                    query = query.Where(p => p.IsActive && p.StartAt <= now && p.EndAt >= now);
                    break;
                case "expired":
                    query = query.Where(p => p.EndAt < now);
                    break;
                case "upcoming":
                    query = query.Where(p => p.StartAt > now);
                    break;
                case "paused":
                    query = query.Where(p => !p.IsActive);
                    break;
            }
        }

        var totalItems = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.PromotionId,
                p.PromotionCode,
                p.PromotionName,
                p.PromotionType,
                p.PromotionValue,
                p.MinimumOrderValue,
                p.UsageLimit,
                p.UsedCount,
                p.StartAt,
                p.EndAt,
                p.IsActive,
                p.CreatedAt
            })
            .ToListAsync();

        var result = items.Select(p =>
        {
            string state;
            if (!p.IsActive) state = "paused";
            else if (p.EndAt < now) state = "expired";
            else if (p.StartAt > now) state = "upcoming";
            else state = "active";

            return new
            {
                p.PromotionId,
                p.PromotionCode,
                p.PromotionName,
                p.PromotionType,
                p.PromotionValue,
                p.MinimumOrderValue,
                p.UsageLimit,
                p.UsedCount,
                p.StartAt,
                p.EndAt,
                p.IsActive,
                p.CreatedAt,
                State = state,
                RemainingUses = p.UsageLimit.HasValue ? Math.Max(0, p.UsageLimit.Value - p.UsedCount) : (int?)null
            };
        }).ToList();

        return Ok(new
        {
            Items = result,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        });
    }

    [HttpGet("promotions/{id}")]
    public async Task<IActionResult> GetPromotionDetail(int id)
    {
        var promo = await _dbContext.Promotions
            .Include(p => p.PromotionUsages)
                .ThenInclude(pu => pu.Customer)
            .Include(p => p.PromotionUsages)
                .ThenInclude(pu => pu.Invoice)
            .FirstOrDefaultAsync(p => p.PromotionId == id);

        if (promo == null)
            return NotFound(new { message = $"Không tìm thấy chương trình khuyến mãi #{id}" });

        var now = DateTime.UtcNow;
        string state;
        if (!promo.IsActive) state = "paused";
        else if (promo.EndAt < now) state = "expired";
        else if (promo.StartAt > now) state = "upcoming";
        else state = "active";

        var usages = promo.PromotionUsages
            .OrderByDescending(u => u.UsedAt)
            .Take(20)
            .Select(u => new
            {
                UsageId = u.PromotionUsageId,
                u.InvoiceId,
                CustomerName = u.Customer?.FullName ?? "Khách vãng lai",
                CustomerPhone = u.Customer?.Phone,
                u.DiscountAmount,
                u.UsedAt
            })
            .ToList();

        var totalDiscountFundUsed = promo.PromotionUsages.Sum(u => u.DiscountAmount);

        return Ok(new
        {
            promo.PromotionId,
            promo.PromotionCode,
            promo.PromotionName,
            promo.PromotionType,
            promo.PromotionValue,
            promo.MinimumOrderValue,
            promo.UsageLimit,
            promo.UsedCount,
            promo.StartAt,
            promo.EndAt,
            promo.IsActive,
            promo.CreatedAt,
            State = state,
            TotalDiscountFundUsed = totalDiscountFundUsed,
            Usages = usages
        });
    }

    [HttpPost("promotions")]
    public async Task<IActionResult> CreatePromotion([FromBody] PromotionCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PromotionCode))
            return BadRequest(new { message = "Mã voucher không được để trống." });

        if (string.IsNullOrWhiteSpace(dto.PromotionName))
            return BadRequest(new { message = "Tên chương trình khuyến mãi không được để trống." });

        if (dto.PromotionValue <= 0)
            return BadRequest(new { message = "Giá trị ưu đãi phải lớn hơn 0." });

        if (dto.PromotionType == "percent" && dto.PromotionValue > 100)
            return BadRequest(new { message = "Phần trăm giảm giá không thể vượt quá 100%." });

        if (dto.EndAt <= dto.StartAt)
            return BadRequest(new { message = "Thời gian kết thúc phải sau thời gian bắt đầu." });

        var code = dto.PromotionCode.Trim().ToUpper();
        var existing = await _dbContext.Promotions.AnyAsync(p => p.PromotionCode.ToUpper() == code);
        if (existing)
            return BadRequest(new { message = $"Mã voucher '{code}' đã tồn tại trong hệ thống." });

        var currentStaffId = GetCurrentUserId();
        var now = DateTime.UtcNow;

        var promo = new Promotion
        {
            PromotionCode = code,
            PromotionName = dto.PromotionName.Trim(),
            PromotionType = dto.PromotionType.Trim().ToLower(),
            PromotionValue = dto.PromotionValue,
            MinimumOrderValue = Math.Max(0, dto.MinimumOrderValue),
            UsageLimit = dto.UsageLimit > 0 ? dto.UsageLimit : null,
            UsedCount = 0,
            StartAt = dto.StartAt,
            EndAt = dto.EndAt,
            IsActive = dto.IsActive,
            CreatedBy = currentStaffId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Promotions.Add(promo);
        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = $"Tạo voucher '{promo.PromotionCode}' thành công!",
            promotion = promo
        });
    }

    [HttpPut("promotions/{id}")]
    public async Task<IActionResult> UpdatePromotion(int id, [FromBody] PromotionUpdateDto dto)
    {
        var promo = await _dbContext.Promotions.FindAsync(id);
        if (promo == null)
            return NotFound(new { message = $"Không tìm thấy chương trình #{id}" });

        if (string.IsNullOrWhiteSpace(dto.PromotionName))
            return BadRequest(new { message = "Tên chương trình không được để trống." });

        if (dto.PromotionValue <= 0)
            return BadRequest(new { message = "Giá trị ưu đãi phải lớn hơn 0." });

        if (dto.PromotionType == "percent" && dto.PromotionValue > 100)
            return BadRequest(new { message = "Phần trăm giảm giá không thể vượt quá 100%." });

        if (dto.EndAt <= dto.StartAt)
            return BadRequest(new { message = "Thời gian kết thúc phải sau thời gian bắt đầu." });

        promo.PromotionName = dto.PromotionName.Trim();
        promo.PromotionType = dto.PromotionType.Trim().ToLower();
        promo.PromotionValue = dto.PromotionValue;
        promo.MinimumOrderValue = Math.Max(0, dto.MinimumOrderValue);
        promo.UsageLimit = dto.UsageLimit > 0 ? dto.UsageLimit : null;
        promo.StartAt = dto.StartAt;
        promo.EndAt = dto.EndAt;
        promo.IsActive = dto.IsActive;
        promo.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = "Cập nhật chương trình khuyến mãi thành công!",
            promotion = promo
        });
    }

    [HttpPatch("promotions/{id}/toggle-status")]
    public async Task<IActionResult> TogglePromotionStatus(int id)
    {
        var promo = await _dbContext.Promotions.FindAsync(id);
        if (promo == null)
            return NotFound(new { message = $"Không tìm thấy chương trình #{id}" });

        promo.IsActive = !promo.IsActive;
        promo.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = promo.IsActive ? $"Đã kích hoạt voucher {promo.PromotionCode}!" : $"Đã tạm dừng voucher {promo.PromotionCode}!",
            promo.PromotionId,
            promo.IsActive
        });
    }

    [HttpDelete("promotions/{id}")]
    public async Task<IActionResult> DeletePromotion(int id)
    {
        var promo = await _dbContext.Promotions
            .Include(p => p.PromotionUsages)
            .FirstOrDefaultAsync(p => p.PromotionId == id);

        if (promo == null)
            return NotFound(new { message = $"Không tìm thấy chương trình #{id}" });

        if (promo.PromotionUsages.Count > 0)
        {
            // Đã có khách dùng -> chuyển sang tắt kích hoạt để bảo toàn dữ liệu lịch sử
            promo.IsActive = false;
            promo.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            return Ok(new { message = "Voucher này đã có lượt sử dụng trong lịch sử nên đã được chuyển sang trạng thái Tạm dừng (Không thể xóa vĩnh viễn)." });
        }

        _dbContext.Promotions.Remove(promo);
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = $"Đã xóa voucher '{promo.PromotionCode}' thành công!" });
    }
}
