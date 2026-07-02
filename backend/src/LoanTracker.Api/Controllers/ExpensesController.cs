using FluentValidation;
using LoanTracker.Application.DTOs.Expenses;
using LoanTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoanTracker.Api.Controllers;

[ApiController]
[Route("api/expenses")]
[Produces("application/json")]
public class ExpensesController(
    IExpenseService expenses,
    IValidator<CreateExpenseRequest> createValidator,
    IValidator<UpdateExpenseRequest> updateValidator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? category, [FromQuery] string? search,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 30, CancellationToken ct = default)
        => Ok(await expenses.GetAllAsync(category, search, from, to, page, pageSize, ct));

    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct = default)
        => Ok(await expenses.GetSummaryAsync(from, to, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var r = await expenses.GetByIdAsync(id, ct);
        return r.IsSuccess ? Ok(r.Value) : StatusCode(r.StatusCode, r.Error);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExpenseRequest req, CancellationToken ct = default)
    {
        var v = await createValidator.ValidateAsync(req, ct);
        if (!v.IsValid) return BadRequest(v.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));
        var r = await expenses.CreateAsync(req, ct);
        return r.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = r.Value!.Id }, r.Value) : StatusCode(r.StatusCode, r.Error);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateExpenseRequest req, CancellationToken ct = default)
    {
        var v = await updateValidator.ValidateAsync(req, ct);
        if (!v.IsValid) return BadRequest(v.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));
        var r = await expenses.UpdateAsync(id, req, ct);
        return r.IsSuccess ? Ok(r.Value) : StatusCode(r.StatusCode, r.Error);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var r = await expenses.DeleteAsync(id, ct);
        return r.IsSuccess ? NoContent() : StatusCode(r.StatusCode, r.Error);
    }

    /// <summary>Create an expense from a bank transaction (with chosen category/vendor).</summary>
    [HttpPost("from-transaction")]
    public async Task<IActionResult> FromTransaction([FromBody] ConvertTransactionRequest req, CancellationToken ct = default)
    {
        var r = await expenses.ConvertFromTransactionAsync(req, ct);
        return r.IsSuccess ? Ok(r.Value) : StatusCode(r.StatusCode, r.Error);
    }

    /// <summary>Upload a bill / product photo / invoice for an expense.</summary>
    [HttpPost("{id:guid}/attachments")]
    [RequestSizeLimit(15_000_000)]
    public async Task<IActionResult> Upload(Guid id, IFormFile file, [FromQuery] string kind = "Bill", CancellationToken ct = default)
    {
        if (file is null || file.Length == 0) return BadRequest("Please choose a file.");
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var r = await expenses.AddAttachmentAsync(id, file.FileName, file.ContentType ?? "application/octet-stream", kind, ms.ToArray(), ct);
        return r.IsSuccess ? Ok(r.Value) : StatusCode(r.StatusCode, r.Error);
    }

    [HttpGet("attachments/{attachmentId:guid}")]
    public async Task<IActionResult> Download(Guid attachmentId, CancellationToken ct = default)
    {
        var a = await expenses.GetAttachmentAsync(attachmentId, ct);
        if (a is null) return NotFound();
        return File(a.Value.data, a.Value.contentType, a.Value.fileName);
    }

    [HttpDelete("attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DeleteAttachment(Guid attachmentId, CancellationToken ct = default)
    {
        var r = await expenses.DeleteAttachmentAsync(attachmentId, ct);
        return r.IsSuccess ? NoContent() : StatusCode(r.StatusCode, r.Error);
    }
}
