using FluentValidation;
using LoanTracker.Application.DTOs.Lenders;
using LoanTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoanTracker.Api.Controllers;

[ApiController]
[Route("api/lenders")]
[Produces("application/json")]
public class LendersController(
    ILenderService lenderService,
    IValidator<CreateLenderRequest> createValidator,
    IValidator<UpdateLenderRequest> updateValidator) : ControllerBase
{
    /// <summary>Get paginated list of lenders</summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await lenderService.GetAllAsync(search, isActive, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>Get lender by ID with computed financials</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LenderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await lenderService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    /// <summary>Create a new lender</summary>
    [HttpPost]
    [ProducesResponseType(typeof(LenderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateLenderRequest request, CancellationToken ct = default)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var result = await lenderService.CreateAsync(request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result.Error);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>Update lender details</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(LenderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLenderRequest request, CancellationToken ct = default)
    {
        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var result = await lenderService.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.StatusCode, result.Error);
    }

    /// <summary>Deactivate a lender (soft disable, loans remain)</summary>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct = default)
    {
        var result = await lenderService.DeactivateAsync(id, ct);
        return result.IsSuccess ? NoContent() : StatusCode(result.StatusCode, result.Error);
    }

    /// <summary>Soft delete a lender (only if no active loans)</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var result = await lenderService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : StatusCode(result.StatusCode, result.Error);
    }
}
