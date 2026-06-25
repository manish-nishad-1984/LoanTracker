using FluentValidation;
using LoanTracker.Application.DTOs.Loans;
using LoanTracker.Application.Interfaces;
using LoanTracker.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LoanTracker.Api.Controllers;

[ApiController]
[Route("api/loans")]
[Produces("application/json")]
public class LoansController(
    ILoanService loanService,
    IValidator<CreateLoanRequest> createValidator,
    IValidator<UpdateLoanRequest> updateValidator,
    IValidator<UpdateInterestRateRequest> rateValidator) : ControllerBase
{
    /// <summary>Get paginated loan list with optional filters</summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? lenderId,
        [FromQuery] LoanStatus? status,
        [FromQuery] LoanDirection? direction,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await loanService.GetAllAsync(lenderId, status, direction, search, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>Get loan by ID with full financial summary</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LoanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await loanService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    /// <summary>Create a new loan</summary>
    [HttpPost]
    [ProducesResponseType(typeof(LoanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateLoanRequest request, CancellationToken ct = default)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var result = await loanService.CreateAsync(request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result.Error);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>Update loan details (rate, frequency, notes)</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(LoanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLoanRequest request, CancellationToken ct = default)
    {
        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var result = await loanService.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.StatusCode, result.Error);
    }

    /// <summary>Close a loan manually</summary>
    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(typeof(LoanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Close(Guid id, [FromBody] CloseLoanRequest request, CancellationToken ct = default)
    {
        var result = await loanService.CloseAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.StatusCode, result.Error);
    }

    /// <summary>Update interest rate with audit log</summary>
    [HttpPost("{id:guid}/interest-rate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateInterestRate(Guid id, [FromBody] UpdateInterestRateRequest request, CancellationToken ct = default)
    {
        var validation = await rateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var result = await loanService.UpdateInterestRateAsync(id, request, ct);
        return result.IsSuccess ? NoContent() : StatusCode(result.StatusCode, result.Error);
    }

    /// <summary>Soft delete a loan (only if no payments)</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var result = await loanService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : StatusCode(result.StatusCode, result.Error);
    }
}
