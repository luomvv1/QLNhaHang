using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Restaurant.Api.Data;

namespace Restaurant.Api.Security;

public static class RestaurantRoles
{
    public const string Admin = "admin";
    public const string Management = "admin,manager";
    public const string Employees = "admin,manager,staff,chef";
    public const string Kitchen = "admin,manager,chef";
    public const string Service = "admin,manager,staff";
    public const string Customer = "customer";
    public static readonly string[] All = ["admin", "manager", "staff", "chef", "customer"];
}

public static class RestaurantSecurity
{
    public static IServiceCollection AddRestaurantSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtKey = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
            throw new InvalidOperationException("Configure Jwt:Key with a secret of at least 32 bytes before starting the API.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"] ?? "RestaurantApi",
                    ValidAudience = configuration["Jwt:Audience"] ?? "RestaurantClients",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.Request.Path.StartsWithSegments("/hubs/restaurant")
                            && context.Request.Query.TryGetValue("access_token", out var token))
                            context.Token = token;
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        var principal = context.Principal;
                        if (!int.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
                        {
                            context.Fail("Invalid account.");
                            return;
                        }
                        var db = context.HttpContext.RequestServices.GetRequiredService<RestaurantDbContext>();
                        var user = await db.Users.AsNoTracking().Where(user => user.UserId == id)
                            .Select(user => new { user.IsActive, RoleActive = user.Role.IsActive, user.Role.RoleCode })
                            .SingleOrDefaultAsync(context.HttpContext.RequestAborted);
                        // A signed token is not sufficient after the account is locked or its role changes.
                        if (user == null || !user.IsActive || !user.RoleActive
                            || !RestaurantRoles.All.Contains(user.RoleCode)
                            || !principal!.FindAll(ClaimTypes.Role).Select(claim => claim.Value).SequenceEqual([user.RoleCode]))
                            context.Fail("Account is unavailable or permissions changed. Sign in again.");
                    }
                };
            })
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, BankWebhookAuthenticationHandler>(
                BankWebhookAuthenticationHandler.SchemeName, _ => { });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser().RequireRole("admin", "manager").Build();
        });
        return services;
    }
}
