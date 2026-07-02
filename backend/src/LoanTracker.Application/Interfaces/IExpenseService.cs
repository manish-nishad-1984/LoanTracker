using LoanTracker.Application.Common;
using LoanTracker.Application.DTOs.Expenses;

namespace LoanTracker.Application.Interfaces;

public interface IExpenseService
{
    Task<PagedResult<ExpenseListItemDto>> GetAllAsync(
        string? category, string? search, DateOnly? from, DateOnly? to,
        int page, int pageSize, CancellationToken ct = default);

    Task<Result<ExpenseDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<ExpenseDto>> CreateAsync(CreateExpenseRequest request, CancellationToken ct = default);
    Task<Result<ExpenseDto>> UpdateAsync(Guid id, UpdateExpenseRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);

    Task<Result<ExpenseDto>> ConvertFromTransactionAsync(ConvertTransactionRequest request, CancellationToken ct = default);

    Task<Result<AttachmentDto>> AddAttachmentAsync(Guid expenseId, string fileName, string contentType, string kind, byte[] data, CancellationToken ct = default);
    Task<(byte[] data, string contentType, string fileName)?> GetAttachmentAsync(Guid attachmentId, CancellationToken ct = default);
    Task<Result> DeleteAttachmentAsync(Guid attachmentId, CancellationToken ct = default);

    Task<ExpenseSummaryDto> GetSummaryAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default);
}
