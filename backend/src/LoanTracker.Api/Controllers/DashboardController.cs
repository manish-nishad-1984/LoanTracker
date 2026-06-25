using LoanTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoanTracker.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Produces("application/json")]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    /// <summary>Full dashboard summary: totals, counts, recent payments, trends</summary>
    [HttpGet]
    public async Task<IActionResult> GetSummary(CancellationToken ct = default)
    {
        var result = await dashboardService.GetSummaryAsync(ct);
        return Ok(result);
    }

    /// <summary>Per-lender aggregated summary</summary>
    [HttpGet("lenders")]
    public async Task<IActionResult> GetLenderSummaries(CancellationToken ct = default)
    {
        var result = await dashboardService.GetLenderSummariesAsync(ct);
        return Ok(result);
    }

    /// <summary>Monthly payment trend (last N months)</summary>
    [HttpGet("monthly")]
    public async Task<IActionResult> GetMonthlyTrend(
        [FromQuery] int months = 24,
        CancellationToken ct = default)
    {
        var result = await dashboardService.GetMonthlyTrendAsync(months, ct);
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
