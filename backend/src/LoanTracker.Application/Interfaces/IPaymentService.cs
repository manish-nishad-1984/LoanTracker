using LoanTracker.Application.Common;
using LoanTracker.Application.DTOs.Payments;

namespace LoanTracker.Application.Interfaces;

public interface IPaymentService
{
    Task<PagedResult<PaymentListItemDto>> GetByLoanAsync(
        Guid loanId, int page, int pageSize, CancellationToken ct = default);

    Task<PagedResult<PaymentListItemDto>> GetAllAsync(
        Guid? loanId, Guid? lenderId, DateOnly? from, DateOnly? to,
        int page, int pageSize, CancellationToken ct = default);

    Task<Result<PaymentDto>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Result<PaymentDto>> CreateAsync(CreatePaymentRequest request, CancellationToken ct = default);

    Task<Result<PaymentDto>> UpdateAsync(Guid id, UpdatePaymentRequest request, CancellationToken ct = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
