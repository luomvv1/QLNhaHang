using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;

namespace Restaurant.Api.Controllers.Admin;

// ==============================================================
// KHUÔN MẪU DỮ LIỆU TÀI KHOẢN & NHÂN VIÊN
// ==============================================================
public class CreateUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int RoleId { get; set; }
}

public class UpdateUserRequest
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int RoleId { get; set; }
    public string? NewPassword { get; set; }
    public bool IsActive { get; set; } = true;
}
// ==============================================================

[Route("api/admin/user")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly RestaurantDbContext _dbContext;

    public UserController(RestaurantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _dbContext.Roles
            .Where(r => r.IsActive)
            .Select(r => new
            {
                r.RoleId,
                r.RoleCode,
                r.RoleName,
                r.Description
            })
            .ToListAsync();

        return Ok(new { data = roles });
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetUsers([FromQuery] int? roleId = null, [FromQuery] bool? isActive = null)
    {
        var query = _dbContext.Users
            .Include(u => u.Role)
            .AsQueryable();

        if (roleId.HasValue)
        {
            query = query.Where(u => u.RoleId == roleId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new
            {
                u.UserId,
                u.FullName,
                u.Email,
                u.Phone,
                u.RoleId,
                RoleCode = u.Role != null ? u.Role.RoleCode : "",
                RoleName = u.Role != null ? u.Role.RoleName : "",
                u.IsActive,
                u.CreatedAt,
                u.LastLoginAt
            })
            .ToListAsync();

        return Ok(new { data = users });
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var fullName = request.FullName?.Trim();
        var email = request.Email?.Trim().ToLowerInvariant();
        var password = request.Password?.Trim();

        if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            return BadRequest(new { message = "Vui lòng nhập đầy đủ Họ Tên, Email và Mật khẩu!" });

        var role = await _dbContext.Roles.FindAsync(request.RoleId);
        if (role == null)
            return BadRequest(new { message = "Vai trò không hợp lệ!" });

        var isEmailExists = await _dbContext.Users.AnyAsync(u => u.Email == email);
        if (isEmailExists)
            return BadRequest(new { message = "Email này đã được sử dụng bởi một tài khoản khác!" });

        var newUser = new User
        {
            FullName = fullName,
            Email = email,
            PasswordHash = password,
            Phone = request.Phone?.Trim(),
            RoleId = request.RoleId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(newUser);
        await _dbContext.SaveChangesAsync();

        return Ok(new 
        { 
            message = $"Tạo tài khoản {role.RoleName} '{fullName}' thành công!",
            userId = newUser.UserId 
        });
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request)
    {
        var fullName = request.FullName?.Trim();
        var email = request.Email?.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(email))
            return BadRequest(new { message = "Họ Tên và Email không được để trống!" });

        var user = await _dbContext.Users.FindAsync(request.UserId);
        if (user == null)
            return BadRequest(new { message = "Không tìm thấy người dùng này!" });

        var role = await _dbContext.Roles.FindAsync(request.RoleId);
        if (role == null)
            return BadRequest(new { message = "Vai trò không hợp lệ!" });

        var isEmailTaken = await _dbContext.Users.AnyAsync(u => u.Email == email && u.UserId != request.UserId);
        if (isEmailTaken)
            return BadRequest(new { message = "Email này đã được tài khoản khác sử dụng!" });

        user.FullName = fullName;
        user.Email = email;
        user.Phone = request.Phone?.Trim();
        user.RoleId = request.RoleId;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            user.PasswordHash = request.NewPassword.Trim();
        }

        await _dbContext.SaveChangesAsync();

        return Ok(new { message = $"Cập nhật thông tin tài khoản '{fullName}' thành công!" });
    }

    [HttpPatch("toggle-status")]
    public async Task<IActionResult> ToggleStatus([FromQuery] int userId)
    {
        var user = await _dbContext.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
            return BadRequest(new { message = "Không tìm thấy tài khoản này!" });

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        var statusText = user.IsActive ? "Mở khóa hoạt động" : "Tạm khóa";
        return Ok(new { message = $"Đã {statusText} tài khoản '{user.FullName}' thành công!" });
    }
}