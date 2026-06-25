using FluentValidation;
using LoanTracker.Application.DTOs.Payments;
using LoanTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoanTracker.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Produces("application/json")]
public class PaymentsController(
    IPaymentService paymentService,
    IValidator<CreatePaymentRequest> createValidator,
    IValidator<UpdatePaymentRequest> updateValidator) : ControllerBase
{
    /// <summary>Get all payments with optional loan/lender/date filters</summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? loanId,
        [FromQuery] Guid? lenderId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await paymentService.GetAllAsync(loanId, lenderId, from, to, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>Get payments by loan ID (most recent first)</summary>
    [HttpGet("loan/{loanId:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByLoan(
        Guid loanId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await paymentService.GetByLoanAsync(loanId, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>Get payment by ID</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await paymentService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    /// <summary>Record a new payment (principal-only, interest-only, or mixed)</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request, CancellationToken ct = default)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var result = await paymentService.CreateAsync(request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result.Error);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>Edit an existing payment</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePaymentRequest request, CancellationToken ct = default)
    {
        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var result = await paymentService.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.StatusCode, result.Error);
    }

    /// <summary>Soft-delete a payment (recalculates balances)</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var result = await paymentService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : StatusCode(result.StatusCode, result.Error);
    }
}
