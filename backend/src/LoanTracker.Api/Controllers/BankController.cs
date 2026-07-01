using LoanTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoanTracker.Api.Controllers;

[ApiController]
[Route("api/bank")]
[Produces("application/json")]
public class BankController(IBankStatementService bank) : ControllerBase
{
    /// <summary>Upload a statement and get a parsed preview — nothing is saved.</summary>
    [HttpPost("statements/preview")]
    [RequestSizeLimit(20_000_000)]
    public IActionResult Preview(IFormFile file)
    {
        if (file is null || file.Length == 0) return BadRequest("Please upload a statement file.");
        using var stream = file.OpenReadStream();
        var result = bank.ParsePreview(stream, file.FileName);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.StatusCode, result.Error);
    }

    /// <summary>Upload a statement and SAVE its transactions (de-duplicated, auto-categorized).</summary>
    [HttpPost("statements/import")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("Please upload a statement file.");
        using var stream = file.OpenReadStream();
        var result = await bank.ImportAsync(stream, file.FileName, ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.StatusCode, result.Error);
    }

    [HttpGet("accounts")]
    public async Task<IActionResult> Accounts(CancellationToken ct) => Ok(await bank.GetAccountsAsync(ct));

    /// <summary>Full executive analytics. Optional accountId/from/to.</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(
        [FromQuery] Guid? accountId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        var result = await bank.GetDashboardAsync(accountId, from, to, ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.StatusCode, result.Error);
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> Transactions(
        [FromQuery] Guid? accountId, [FromQuery] string? category, [FromQuery] string? direction,
        [FromQuery] string? search, [FromQuery] string? merchant, [FromQuery] string? paymentMethod,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        => Ok(await bank.GetTransactionsAsync(accountId, category, direction, search, merchant, paymentMethod, from, to, page, pageSize, ct));

    /// <summary>Grouped summary: groupBy = category | merchant | paymentMethod | month | direction.</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> Summary(
        [FromQuery] string groupBy = "category", [FromQuery] Guid? accountId = null,
        [FromQuery] string? category = null, [FromQuery] string? direction = null, [FromQuery] string? search = null,
        [FromQuery] DateOnly? from = null, [FromQuery] DateOnly? to = null, CancellationToken ct = default)
        => Ok(await bank.GetGroupedSummaryAsync(groupBy, accountId, category, direction, search, from, to, ct));

    public record UpdateCategoryBody(string Category);

    [HttpPut("transactions/{id:guid}/category")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryBody body, CancellationToken ct)
    {
        var result = await bank.UpdateCategoryAsync(id, body.Category, ct);
        return result.IsSuccess ? NoContent() : StatusCode(result.StatusCode, result.Error);
    }
}
