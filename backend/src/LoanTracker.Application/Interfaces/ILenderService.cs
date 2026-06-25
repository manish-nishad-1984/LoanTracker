using LoanTracker.Application.Common;
using LoanTracker.Application.DTOs.Lenders;

namespace LoanTracker.Application.Interfaces;

public interface ILenderService
{
    Task<PagedResult<LenderListItemDto>> GetAllAsync(
        string? search, bool? isActive, int page, int pageSize,
        CancellationToken ct = default);

    Task<Result<LenderDto>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Result<LenderDto>> CreateAsync(CreateLenderRequest request, CancellationToken ct = default);

    Task<Result<LenderDto>> UpdateAsync(Guid id, UpdateLenderRequest request, CancellationToken ct = default);

    Task<Result> DeactivateAsync(Guid id, CancellationToken ct = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
