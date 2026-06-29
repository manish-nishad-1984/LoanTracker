using LoanTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoanTracker.Api.Controllers;

[ApiController]
[Route("api/bank")]
[Produces("application/json")]
public class BankController(IBankStatementService bankStatementService) : ControllerBase
{
    /// <summary>Upload a bank statement (.xls/.xlsx) and get a parsed preview — nothing is saved yet.</summary>
    [HttpPost("statements/preview")]
    [RequestSizeLimit(20_000_000)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Preview(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest("Please upload a statement file.");

        using var stream = file.OpenReadStream();
        var result = bankStatementService.ParsePreview(stream, file.FileName);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.StatusCode, result.Error);
    }
}
