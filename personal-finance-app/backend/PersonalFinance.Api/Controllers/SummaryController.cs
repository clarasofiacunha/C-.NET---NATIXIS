using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalFinance.Api.DTOs.Summary;
using PersonalFinance.Api.Services;

namespace PersonalFinance.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/summary")]
public sealed class SummaryController(IMonthlySummaryService summaries) : ControllerBase
{
    [HttpGet("monthly")]
    [EndpointSummary("Get monthly financial summary")]
    [EndpointDescription(
        "Returns total balance, monthly expenses and budget usage for the authenticated user. " +
        "Supply both year and month; omit both to use the current UTC month. " +
        "TotalBalance uses all recorded transactions, whereas other metrics use the selected month.")]
    [ProducesResponseType<MonthlySummaryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MonthlySummaryResponse>> GetMonthly(
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken cancellationToken)
    {
        if (year.HasValue != month.HasValue ||
            year is < 1 or > 9999 || month is < 1 or > 12)
            return BadRequest(new { error = "Indica um ano válido (1-9999) e um mês válido (1-12), ou omite ambos." });

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var now = DateTime.UtcNow;
        return Ok(await summaries.GetAsync(
            userId, year ?? now.Year, month ?? now.Month, cancellationToken));
    }
}
