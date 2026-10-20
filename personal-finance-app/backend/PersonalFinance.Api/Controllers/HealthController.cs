using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Api.Data;

namespace PersonalFinance.Api.Controllers;

[ApiController]
public class HealthController(AppDbContext db) : ControllerBase
{
    [HttpGet("/")]
    public IActionResult GetApiStatus() => Ok(new
    {
        application = "Personal Finance API",
        status = "online",
        utc = DateTime.UtcNow
    });

    [HttpGet("health")]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            return await db.Database.CanConnectAsync()
                ? Ok(new { api = "online", database = "connected" })
                : StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { api = "online", database = "unavailable" });
        }
        catch
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { api = "online", database = "unavailable" });
        }
    }
}
