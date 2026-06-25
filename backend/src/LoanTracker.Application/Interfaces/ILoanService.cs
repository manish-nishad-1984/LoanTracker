using LoanTracker.Application.Common;
using LoanTracker.Application.DTOs.Loans;
using LoanTracker.Domain.Enums;

namespace LoanTracker.Application.Interfaces;

public interface ILoanService
{
    Task<PagedResult<LoanListItemDto>> GetAllAsync(
        Guid? lenderId, LoanStatus? status, LoanDirection? direction, string? search,
        int page, int pageSize, CancellationToken ct = default);

    Task<Result<LoanDto>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Result<LoanDto>> CreateAsync(CreateLoanRequest request, CancellationToken ct = default);

    Task<Result<LoanDto>> UpdateAsync(Guid id, UpdateLoanRequest request, CancellationToken ct = default);

    Task<Result<LoanDto>> CloseAsync(Guid id, CloseLoanRequest request, CancellationToken ct = default);

    Task<Result> UpdateInterestRateAsync(Guid id, UpdateInterestRateRequest request, CancellationToken ct = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
