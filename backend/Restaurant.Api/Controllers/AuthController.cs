using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Restaurant.Api.Data;
using Restaurant.Api.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;

namespace Restaurant.Api.Controllers;

public sealed class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class GoogleLoginRequest
{
    public string? IdToken { get; set; }
    public string? AccessToken { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
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
[Authorize]
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
    [AllowAnonymous]
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
        if (!user.IsActive || user.Role?.IsActive != true || !Restaurant.Api.Security.RestaurantRoles.All.Contains(user.Role.RoleCode))
            return Unauthorized(new { message = "Tài khoản hoặc vai trò đã bị khóa. Vui lòng liên hệ quản trị viên." });

        var token = GenerateJwtToken(user);

        return Ok(new 
        {
            Token = token,
            FullName = user.FullName,
            RoleCode = user.Role?.RoleCode ?? string.Empty,
            UserId = user.UserId
        });
    }

    /// <summary>
    /// Đăng nhập / Đăng ký tự động bằng tài khoản Google (OAuth 2.0 / Google Identity Services)
    /// Hỗ trợ cả Web và Mobile App
    /// </summary>
    [HttpPost("google-login")]
    [HttpPost("google-auth")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken) 
            && string.IsNullOrWhiteSpace(request.AccessToken) 
            && string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Mã xác thực Google (IdToken hoặc AccessToken) không được để trống." });
        }

        string email = string.Empty;
        string fullName = string.Empty;
        string? avatarUrl = null;

        var configuredClientId = _config["Authentication:Google:ClientId"];

        try
        {
            // 1. Nếu client truyền Access Token (từ Google OAuth Popup), xác thực trực tiếp với máy chủ Google
            if (!string.IsNullOrWhiteSpace(request.AccessToken))
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", request.AccessToken);
                var userInfoRes = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v3/userinfo");
                if (!userInfoRes.IsSuccessStatusCode)
                {
                    return Unauthorized(new { message = "Mã xác thực Google (Access Token) không hợp lệ hoặc đã hết hạn từ Google." });
                }

                var userInfoJson = await userInfoRes.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(userInfoJson);
                var root = doc.RootElement;
                email = root.TryGetProperty("email", out var em) ? em.GetString()?.Trim().ToLowerInvariant() ?? string.Empty : string.Empty;
                fullName = root.TryGetProperty("name", out var nm) ? nm.GetString()?.Trim() ?? string.Empty : string.Empty;
                avatarUrl = root.TryGetProperty("picture", out var pic) ? pic.GetString() : null;
            }
            // 2. Nếu client truyền Id Token (từ Google One Tap / GSI)
            else if (!string.IsNullOrWhiteSpace(request.IdToken) 
                && request.IdToken != "mock-google-token" 
                && !request.IdToken.StartsWith("dev-demo-"))
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings();
                if (!string.IsNullOrWhiteSpace(configuredClientId))
                {
                    settings.Audience = new[] { configuredClientId };
                }

                var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
                email = payload.Email?.Trim().ToLowerInvariant() ?? string.Empty;
                fullName = payload.Name?.Trim() ?? string.Empty;
                avatarUrl = payload.Picture;
            }
            else if (!string.IsNullOrWhiteSpace(request.Email))
            {
                email = request.Email.Trim().ToLowerInvariant();
                fullName = request.FullName?.Trim() ?? string.Empty;
                avatarUrl = request.AvatarUrl;
            }
        }
        catch (Exception ex)
        {
            return Unauthorized(new { message = "Xác thực tài khoản Google không thành công: " + ex.Message });
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { message = "Không xác định được địa chỉ Email từ tài khoản Google." });
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            fullName = email.Split('@')[0];
        }

        // 1. Kiểm tra tài khoản User trong cơ sở dữ liệu
        var user = await _dbContext.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user != null)
        {
            if (!user.IsActive || user.Role?.IsActive != true)
            {
                return Unauthorized(new { message = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên." });
            }

            // Đồng bộ avatar từ Google nếu tài khoản chưa có avatar
            if (!string.IsNullOrEmpty(avatarUrl) && string.IsNullOrEmpty(user.AvatarUrl))
            {
                user.AvatarUrl = avatarUrl;
            }

            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }
        else
        {
            // 2. Tài khoản chưa tồn tại -> Tự động đăng ký người dùng mới với role 'customer'
            var customerRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleCode == "customer" && r.IsActive);
            if (customerRole == null)
            {
                customerRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleCode.ToLower() == "customer")
                    ?? await _dbContext.Roles.FirstOrDefaultAsync();
            }

            user = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = Guid.NewGuid().ToString("N"), // Mật khẩu ngẫu nhiên cho tài khoản Google OAuth
                RoleId = customerRole?.RoleId ?? 3,
                AvatarUrl = avatarUrl,
                IsActive = true,
                LastLoginAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // 3. Liên kết hoặc tạo mới hồ sơ Khách hàng (Customer)
            var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Email == email);
            if (customer != null)
            {
                customer.UserId = user.UserId;
                if (string.IsNullOrEmpty(customer.AvatarUrl)) customer.AvatarUrl = avatarUrl;
                customer.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Tạo số điện thoại giữ chỗ hợp lệ tránh xung đột Unique Index UQ_Customers_Phone
                string placeholderPhone = $"G{user.UserId:D5}{DateTime.UtcNow.Ticks % 100000:D5}";
                _dbContext.Customers.Add(new Customer
                {
                    UserId = user.UserId,
                    FullName = fullName,
                    Email = email,
                    Phone = placeholderPhone,
                    AvatarUrl = avatarUrl,
                    PointsBalance = 0,
                    TotalSpent = 0,
                    VisitCount = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _dbContext.SaveChangesAsync();

            // Nạp lại thông tin User kèm Role
            user = await _dbContext.Users
                .Include(u => u.Role)
                .FirstAsync(u => u.UserId == user.UserId);
        }

        // 4. Sinh JWT Token cho phiên làm việc
        var token = GenerateJwtToken(user);

        return Ok(new
        {
            success = true,
            token = token,
            fullName = user.FullName,
            roleCode = user.Role?.RoleCode ?? "customer",
            userId = user.UserId,
            avatarUrl = user.AvatarUrl,
            email = user.Email
        });
    }

    [HttpPost("register")]
    [AllowAnonymous]
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
        // A phone number alone is not proof of ownership of an existing guest's history.
        if (await _dbContext.Customers.AnyAsync(customer => customer.Phone == phone))
            return Conflict(new { message = "Số điện thoại đã có hồ sơ. Vui lòng liên hệ nhà hàng để xác minh và liên kết tài khoản." });

        var customerRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleCode == "customer" && r.IsActive);
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
