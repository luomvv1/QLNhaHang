using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Restaurant.Api.Data;

namespace Restaurant.Api.Controllers;

[ApiController]
[Route("api/health")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly RestaurantDbContext _db;

    public HealthController(RestaurantDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var databaseConnected = await _db.Database.CanConnectAsync();

        return Ok(new
        {
            api = "running",
            database = databaseConnected ? "connected" : "unavailable",
            checkedAt = DateTime.UtcNow
        });
    }
}
