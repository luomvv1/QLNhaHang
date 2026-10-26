using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Restaurant.Api.Security;

public sealed class BankWebhookAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "BankWebhook";
    private readonly IConfiguration _configuration;

    public BankWebhookAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, IConfiguration configuration) : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var expected = _configuration["BankWebhook:ApiKey"] ?? ReadStoredSecret();
        var supplied = Request.Headers["X-Api-Key"].ToString();
        var authorization = Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Apikey ", StringComparison.OrdinalIgnoreCase))
            supplied = authorization[7..].Trim();
        else if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            supplied = authorization[7..].Trim();

        if (string.IsNullOrWhiteSpace(supplied) && Request.Query.TryGetValue("apiKey", out var queryKey))
            supplied = queryKey.ToString().Trim();

        if (string.IsNullOrWhiteSpace(expected) || expected.Length < 8 || string.IsNullOrWhiteSpace(supplied)
            || !CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(expected)),
                SHA256.HashData(Encoding.UTF8.GetBytes(supplied))))
            return Task.FromResult(AuthenticateResult.Fail("A configured bank webhook key is required."));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "bank-gateway")], SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }

    private static string? ReadStoredSecret()
    {
        foreach (var path in new[] { Path.Combine(AppContext.BaseDirectory, "bank_config.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "bank_config.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "backend", "Restaurant.Api", "bank_config.json") })
        {
            if (!File.Exists(path)) continue;
            try
            {
                using var json = JsonDocument.Parse(File.ReadAllText(path));
                foreach (var property in json.RootElement.EnumerateObject())
                    if (property.Name.Equals("WebhookSecret", StringComparison.OrdinalIgnoreCase))
                        return property.Value.GetString();
            }
            catch (Exception error) when (error is IOException or JsonException or UnauthorizedAccessException) { }
        }
        return null;
    }
}
