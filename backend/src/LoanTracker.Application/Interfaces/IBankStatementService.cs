using LoanTracker.Application.Common;
using LoanTracker.Application.DTOs.Banking;

namespace LoanTracker.Application.Interfaces;

public interface IBankStatementService
{
    /// <summary>Parse an uploaded statement and return a preview — no persistence.</summary>
    Result<BankStatementPreviewDto> ParsePreview(Stream fileStream, string fileName);

    /// <summary>Parse and save transactions (de-duplicated, auto-categorized).</summary>
    Task<Result<ImportResultDto>> ImportAsync(Stream fileStream, string fileName, CancellationToken ct = default);

    Task<IReadOnlyList<BankAccountDto>> GetAccountsAsync(CancellationToken ct = default);

    /// <summary>Full executive analytics for an account (or all accounts if null).</summary>
    Task<Result<BankDashboardDto>> GetDashboardAsync(Guid? accountId, DateOnly? from, DateOnly? to, CancellationToken ct = default);

    Task<BankTxnListDto> GetTransactionsAsync(
        Guid? accountId, string? category, string? direction, string? search,
        string? merchant, string? paymentMethod,
        DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct = default);

    Task<IReadOnlyList<GroupSummaryDto>> GetGroupedSummaryAsync(
        string groupBy, Guid? accountId, string? category, string? direction, string? search,
        DateOnly? from, DateOnly? to, CancellationToken ct = default);

    Task<Result> UpdateCategoryAsync(Guid transactionId, string category, CancellationToken ct = default);
}
