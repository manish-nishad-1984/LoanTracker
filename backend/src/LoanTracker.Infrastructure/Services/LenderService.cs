using LoanTracker.Application.Common;
using LoanTracker.Application.DTOs.Lenders;
using LoanTracker.Application.Interfaces;
using LoanTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LoanTracker.Infrastructure.Services;

public class LenderService(IApplicationDbContext db, ILogger<LenderService> logger) : ILenderService
{
    public async Task<PagedResult<LenderListItemDto>> GetAllAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Lenders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(l => l.Name.ToLower().Contains(search.ToLower())
                                  || (l.Phone != null && l.Phone.Contains(search))
                                  || (l.Email != null && l.Email.ToLower().Contains(search.ToLower())));

        if (isActive.HasValue)
            query = query.Where(l => l.IsActive == isActive.Value);

        var total = await query.CountAsync(ct);

        // Global query filters exclude soft-deleted loans/payments; aggregate in memory.
        var pageLenders = await query
            .OrderBy(l => l.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(l => l.Loans)
                .ThenInclude(ln => ln.Payments)
            .ToListAsync(ct);

        var items = pageLenders.Select(l =>
        {
            var loans = l.Loans.ToList();
            var totalBorrowed = loans.Sum(ln => ln.PrincipalAmount);
            var principalPaid = loans.SelectMany(ln => ln.Payments).Sum(p => p.PrincipalAmount);

            return new LenderListItemDto(
                l.Id,
                l.LenderType,
                l.LenderType.ToString(),
                l.Name,
                l.Phone,
                l.Email,
                l.IsActive,
                loans.Count(ln => ln.Status == Domain.Enums.LoanStatus.Active),
                totalBorrowed - principalPaid,
                l.CreatedAt
            );
        }).ToList();

        return PagedResult<LenderListItemDto>.Create(items, total, page, pageSize);
    }

    public async Task<Result<LenderDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var lender = await db.Lenders
            .AsNoTracking()
            .Include(l => l.Loans.Where(ln => ln.DeletedAt == null))
                .ThenInclude(ln => ln.Payments.Where(p => !p.IsDeleted))
            .FirstOrDefaultAsync(l => l.Id == id, ct);

        if (lender is null)
            return Result<LenderDto>.NotFound("Lender", id);

        var activeLoanIds = lender.Loans.Where(l => l.Status == Domain.Enums.LoanStatus.Active).Select(l => l.Id).ToHashSet();
        var totalBorrowed = lender.Loans.Sum(l => l.PrincipalAmount);
        var totalPrincipalPaid = lender.Loans.SelectMany(l => l.Payments).Sum(p => p.PrincipalAmount);
        var totalOutstanding = totalBorrowed - totalPrincipalPaid;

        return Result<LenderDto>.Success(new LenderDto(
            lender.Id, lender.LenderType, lender.LenderType.ToString(),
            lender.Name, lender.ContactName, lender.Phone, lender.Email,
            lender.Address, lender.BankName, lender.AccountNumber,
            lender.IfscCode, lender.PanNumber, lender.Notes, lender.IsActive,
            lender.CreatedAt,
            lender.Loans.Count, activeLoanIds.Count,
            totalBorrowed, totalOutstanding
        ));
    }

    public async Task<Result<LenderDto>> CreateAsync(CreateLenderRequest request, CancellationToken ct = default)
    {
        var lender = new Lender
        {
            LenderType = request.LenderType,
            Name = request.Name.Trim(),
            ContactName = request.ContactName?.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim().ToLower(),
            Address = request.Address?.Trim(),
            BankName = request.BankName?.Trim(),
            AccountNumber = request.AccountNumber?.Trim(),
            IfscCode = request.IfscCode?.Trim().ToUpper(),
            PanNumber = request.PanNumber?.Trim().ToUpper(),
            Notes = request.Notes?.Trim()
        };

        db.Lenders.Add(lender);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Created lender {LenderId} — {Name}", lender.Id, lender.Name);
        return Result<LenderDto>.Created(await MapToDto(lender));
    }

    public async Task<Result<LenderDto>> UpdateAsync(Guid id, UpdateLenderRequest request, CancellationToken ct = default)
    {
        var lender = await db.Lenders.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lender is null) return Result<LenderDto>.NotFound("Lender", id);

        lender.LenderType = request.LenderType;
        lender.Name = request.Name.Trim();
        lender.ContactName = request.ContactName?.Trim();
        lender.Phone = request.Phone?.Trim();
        lender.Email = request.Email?.Trim().ToLower();
        lender.Address = request.Address?.Trim();
        lender.BankName = request.BankName?.Trim();
        lender.AccountNumber = request.AccountNumber?.Trim();
        lender.IfscCode = request.IfscCode?.Trim().ToUpper();
        lender.PanNumber = request.PanNumber?.Trim().ToUpper();
        lender.Notes = request.Notes?.Trim();
        lender.IsActive = request.IsActive;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Updated lender {LenderId}", id);
        return Result<LenderDto>.Success(await MapToDto(lender));
    }

    public async Task<Result> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var lender = await db.Lenders.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lender is null) return Result.NotFound("Lender", id);

        lender.IsActive = false;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Deactivated lender {LenderId}", id);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var lender = await db.Lenders
            .Include(l => l.Loans.Where(ln => ln.Status == Domain.Enums.LoanStatus.Active))
            .FirstOrDefaultAsync(l => l.Id == id, ct);

        if (lender is null) return Result.NotFound("Lender", id);

        if (lender.Loans.Any())
            return Result.BadRequest("Cannot delete lender with active loans. Close or delete loans first.");

        lender.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Soft-deleted lender {LenderId}", id);
        return Result.Success();
    }

    private static Task<LenderDto> MapToDto(Lender l) =>
        Task.FromResult(new LenderDto(
            l.Id, l.LenderType, l.LenderType.ToString(),
            l.Name, l.ContactName, l.Phone, l.Email,
            l.Address, l.BankName, l.AccountNumber,
            l.IfscCode, l.PanNumber, l.Notes, l.IsActive,
            l.CreatedAt, 0, 0, 0, 0
        ));
}
