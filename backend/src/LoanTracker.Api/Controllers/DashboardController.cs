using LoanTracker.Application.Interfaces;
using LoanTracker.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LoanTracker.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Produces("application/json")]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    /// <summary>Full dashboard summary. Optional ?direction=Borrowed|Lent filters everything.</summary>
    [HttpGet]
    public async Task<IActionResult> GetSummary([FromQuery] LoanDirection? direction, CancellationToken ct = default)
    {
        var result = await dashboardService.GetSummaryAsync(direction, ct);
        return Ok(result);
    }

    /// <summary>Per-party aggregated summary (optionally filtered by direction)</summary>
    [HttpGet("lenders")]
    public async Task<IActionResult> GetLenderSummaries([FromQuery] LoanDirection? direction, CancellationToken ct = default)
    {
        var result = await dashboardService.GetLenderSummariesAsync(direction, ct);
        return Ok(result);
    }

    /// <summary>Monthly payment trend (last N months, optionally filtered by direction)</summary>
    [HttpGet("monthly")]
    public async Task<IActionResult> GetMonthlyTrend(
        [FromQuery] int months = 24,
        [FromQuery] LoanDirection? direction = null,
        CancellationToken ct = default)
    {
        var result = await dashboardService.GetMonthlyTrendAsync(months, direction, ct);
        return Ok(result);
    }

    /// <summary>Yearly payment summary</summary>
    [HttpGet("yearly")]
    public async Task<IActionResult> GetYearlySummary(
        [FromQuery] int years = 5,
        CancellationToken ct = default)
    {
        var result = await dashboardService.GetYearlySummaryAsync(years, ct);
        return Ok(result);
    }
}
