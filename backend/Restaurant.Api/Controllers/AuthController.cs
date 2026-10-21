using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Restaurant.Api.Controllers;

public sealed class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class RegisterRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

public sealed class UpdateProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
}

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly RestaurantDbContext _dbContext;
    private readonly IConfiguration _config;

    public AuthController(RestaurantDbContext dbContext, IConfiguration config)
    {
        _dbContext = dbContext;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(request.Password))
            return BadRequest(new { message = "Vui lòng nhập Email và Mật khẩu." });

        var user = await _dbContext.Users
            .Include(u => u.Role) 
            .FirstOrDefaultAsync(u => u.Email == email);
        
        if (user == null || user.PasswordHash != request.Password)
            return Unauthorized(new { message = "Email hoặc mật khẩu không chính xác." });

        var token = GenerateJwtToken(user);

        return Ok(new 
        {
            Token = token,
            FullName = user.FullName,
            RoleCode = user.Role?.RoleCode ?? string.Empty,
            UserId = user.UserId
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var fullName = request.FullName.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        var password = request.Password;
        var phone = request.Phone.Trim();
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(phone))
            return BadRequest(new { message = "Vui lòng điền đủ họ tên, email, mật khẩu và số điện thoại." });

        var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existingUser != null)
            return BadRequest(new { message = "Email này đã được sử dụng." });

        var customerRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleCode == "customer");
        if (customerRole == null)
            return BadRequest(new { message = "Lỗi hệ thống: Chưa có Role 'customer' trong CSDL." });

        var newUser = new User();
        newUser.FullName = fullName;
        newUser.Email = email;
        newUser.PasswordHash = password;
        newUser.Phone = phone;
        newUser.RoleId = customerRole.RoleId;
        newUser.IsActive = true;

        _dbContext.Users.Add(newUser);
        await _dbContext.SaveChangesAsync();

        var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Phone == phone);
        if (customer == null)
        {
            _dbContext.Customers.Add(new Customer
            {
                UserId = newUser.UserId,
                FullName = fullName,
                Email = email,
                Phone = phone,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else if (customer.UserId == null)
        {
            customer.UserId = newUser.UserId;
            customer.FullName = fullName;
            customer.Email = email;
            customer.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Đăng ký tài khoản thành công!" });
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Vui lòng đăng nhập lại." });

        var user = await _dbContext.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
            return NotFound(new { message = "Không tìm thấy thông tin tài khoản." });

        return Ok(new
        {
            userId = user.UserId,
            fullName = user.FullName,
            email = user.Email,
            phone = user.Phone,
            avatarUrl = user.AvatarUrl,
            roleId = user.RoleId,
            roleCode = user.Role?.RoleCode ?? "",
            roleName = user.Role?.RoleName ?? "",
            isActive = user.IsActive,
            createdAt = user.CreatedAt
        });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Vui lòng đăng nhập lại." });

        var user = await _dbContext.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
            return NotFound(new { message = "Không tìm thấy người dùng." });

        if (!string.IsNullOrWhiteSpace(request.FullName))
            user.FullName = request.FullName.Trim();

        user.Phone = request.Phone?.Trim();
        if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
            user.AvatarUrl = request.AvatarUrl.Trim();

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            if (string.IsNullOrEmpty(request.CurrentPassword))
                return BadRequest(new { message = "Vui lòng nhập mật khẩu hiện tại để đổi mật khẩu mới." });

            if (user.PasswordHash != request.CurrentPassword)
                return BadRequest(new { message = "Mật khẩu hiện tại không chính xác." });

            if (request.NewPassword.Length < 6)
                return BadRequest(new { message = "Mật khẩu mới phải có ít nhất 6 ký tự." });

            user.PasswordHash = request.NewPassword;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = "Cập nhật thông tin tài khoản thành công!",
            user = new
            {
                userId = user.UserId,
                fullName = user.FullName,
                email = user.Email,
                phone = user.Phone,
                avatarUrl = user.AvatarUrl,
                roleCode = user.Role?.RoleCode ?? "",
                roleName = user.Role?.RoleName ?? ""
            }
        });
    }

    private string GenerateJwtToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "SuperSecretKeyForRestaurantApi12345!"));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role?.RoleCode ?? "Customer")
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddDays(7),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
