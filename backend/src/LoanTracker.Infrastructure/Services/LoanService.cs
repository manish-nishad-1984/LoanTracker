using LoanTracker.Application.Common;
using LoanTracker.Application.DTOs.Loans;
using LoanTracker.Application.Interfaces;
using LoanTracker.Application.Services;
using LoanTracker.Domain.Entities;
using LoanTracker.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LoanTracker.Infrastructure.Services;

public class LoanService(IApplicationDbContext db, ILogger<LoanService> logger) : ILoanService
{
    public async Task<PagedResult<LoanListItemDto>> GetAllAsync(
        Guid? lenderId, LoanStatus? status, LoanDirection? direction, string? search,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Loans.AsNoTracking()
            .Include(l => l.Lender)
            .Include(l => l.Payments)
            .Include(l => l.InterestRateHistory)
            .AsQueryable();

        if (lenderId.HasValue)
            query = query.Where(l => l.LenderId == lenderId.Value);

        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);

        if (direction.HasValue)
            query = query.Where(l => l.Direction == direction.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(l =>
                (l.Description != null && l.Description.ToLower().Contains(search.ToLower()))
                || (l.LoanNumber != null && l.LoanNumber.ToLower().Contains(search.ToLower()))
                || l.Lender.Name.ToLower().Contains(search.ToLower()));

        var total = await query.CountAsync(ct);

        // Materialize then map in memory — accrued-interest calc cannot run in SQL.
        var pageLoans = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = pageLoans.Select(l =>
        {
            var payments = l.Payments.Where(p => !p.IsDeleted).ToList();
            var accrued = ComputeAccruedInterest(l, payments);
            return new LoanListItemDto(
                l.Id,
                l.LenderId,
                l.Lender.Name,
                l.Lender.LenderType,
                l.Direction,
                l.Direction.ToString(),
                l.LoanNumber,
                l.Description,
                l.PrincipalAmount,
                l.CurrentInterestRate,
                l.Status,
                l.Status.ToString(),
                l.LoanStartDate,
                l.LoanEndDate,
                l.IsEmiLoan,
                l.PrincipalAmount - payments.Sum(p => p.PrincipalAmount),
                payments.Sum(p => p.TotalAmount),
                accrued,
                payments.Sum(p => p.InterestAmount),
                payments.OrderByDescending(p => p.PaymentDate).Select(p => (DateOnly?)p.PaymentDate).FirstOrDefault(),
                l.CreatedAt
            );
        }).ToList();

        return PagedResult<LoanListItemDto>.Create(items, total, page, pageSize);
    }

    public async Task<Result<LoanDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var loan = await db.Loans
            .AsNoTracking()
            .Include(l => l.Lender)
            .Include(l => l.Payments.Where(p => !p.IsDeleted))
            .Include(l => l.InterestRateHistory)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

        if (loan is null) return Result<LoanDto>.NotFound("Loan", id);

        return Result<LoanDto>.Success(MapToDto(loan));
    }

    public async Task<Result<LoanDto>> CreateAsync(CreateLoanRequest request, CancellationToken ct = default)
    {
        var lenderExists = await db.Lenders.AnyAsync(l => l.Id == request.LenderId, ct);
        if (!lenderExists)
            return Result<LoanDto>.NotFound("Lender", request.LenderId);

        if (request.LoanNumber is not null)
        {
            var duplicateNumber = await db.Loans.AnyAsync(l => l.LoanNumber == request.LoanNumber, ct);
            if (duplicateNumber)
                return Result<LoanDto>.Conflict($"Loan number '{request.LoanNumber}' already exists.");
        }

        var loan = new Loan
        {
            LenderId = request.LenderId,
            Direction = request.Direction,
            LoanNumber = request.LoanNumber?.Trim(),
            Description = request.Description?.Trim(),
            PrincipalAmount = request.PrincipalAmount,
            CurrentInterestRate = request.CurrentInterestRate,
            InterestCalculationType = request.InterestCalculationType,
            PaymentFrequency = request.PaymentFrequency,
            IsEmiLoan = request.IsEmiLoan,
            EmiAmount = request.EmiAmount,
            LoanStartDate = request.LoanStartDate,
            LoanEndDate = request.LoanEndDate,
            Notes = request.Notes?.Trim()
        };

        db.Loans.Add(loan);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Created loan {LoanId} for lender {LenderId}", loan.Id, loan.LenderId);

        var created = await GetByIdAsync(loan.Id, ct);
        return Result<LoanDto>.Created(created.Value!);
    }

    public async Task<Result<LoanDto>> UpdateAsync(Guid id, UpdateLoanRequest request, CancellationToken ct = default)
    {
        var loan = await db.Loans.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (loan is null) return Result<LoanDto>.NotFound("Loan", id);

        if (loan.Status == LoanStatus.Closed)
            return Result<LoanDto>.BadRequest("Cannot update a closed loan.");

        loan.LoanNumber = request.LoanNumber?.Trim();
        loan.Description = request.Description?.Trim();
        loan.CurrentInterestRate = request.CurrentInterestRate;
        loan.InterestCalculationType = request.InterestCalculationType;
        loan.PaymentFrequency = request.PaymentFrequency;
        loan.IsEmiLoan = request.IsEmiLoan;
        loan.EmiAmount = request.EmiAmount;
        loan.LoanEndDate = request.LoanEndDate;
        loan.Notes = request.Notes?.Trim();

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Updated loan {LoanId}", id);

        return await GetByIdAsync(id, ct);
    }

    public async Task<Result<LoanDto>> CloseAsync(Guid id, CloseLoanRequest request, CancellationToken ct = default)
    {
        var loan = await db.Loans.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (loan is null) return Result<LoanDto>.NotFound("Loan", id);

        if (loan.Status == LoanStatus.Closed)
            return Result<LoanDto>.BadRequest("Loan is already closed.");

        loan.Status = LoanStatus.Closed;
        loan.ClosureDate = request.ClosureDate;
        loan.ClosureNotes = request.ClosureNotes?.Trim();

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Closed loan {LoanId}", id);

        return await GetByIdAsync(id, ct);
    }

    public async Task<Result> UpdateInterestRateAsync(Guid id, UpdateInterestRateRequest request, CancellationToken ct = default)
    {
        var loan = await db.Loans.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (loan is null) return Result.NotFound("Loan", id);

        if (loan.Status == LoanStatus.Closed)
            return Result.BadRequest("Cannot change interest rate of a closed loan.");

        var history = new LoanInterestRateHistory
        {
            LoanId = id,
            PreviousRate = loan.CurrentInterestRate,
            NewRate = request.NewRate,
            EffectiveDate = request.EffectiveDate,
            Reason = request.Reason?.Trim()
        };

        loan.CurrentInterestRate = request.NewRate;
        db.LoanInterestRateHistories.Add(history);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Updated interest rate for loan {LoanId}: {OldRate}% -> {NewRate}%",
            id, history.PreviousRate, request.NewRate);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var loan = await db.Loans
            .Include(l => l.Payments.Where(p => !p.IsDeleted))
            .FirstOrDefaultAsync(l => l.Id == id, ct);

        if (loan is null) return Result.NotFound("Loan", id);

        if (loan.Payments.Any())
            return Result.BadRequest("Cannot delete loan with payment history. Consider closing the loan instead.");

        loan.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Soft-deleted loan {LoanId}", id);
        return Result.Success();
    }

    private static LoanDto MapToDto(Loan loan)
    {
        var payments = loan.Payments.Where(p => !p.IsDeleted).ToList();
        var totalPrincipalPaid = payments.Sum(p => p.PrincipalAmount);
        var totalInterestPaid = payments.Sum(p => p.InterestAmount);
        var totalPenaltyPaid = payments.Sum(p => p.PenaltyAmount);
        var totalAmountPaid = payments.Sum(p => p.TotalAmount);
        var outstanding = loan.PrincipalAmount - totalPrincipalPaid;
        var lastPayment = payments.OrderByDescending(p => p.PaymentDate).FirstOrDefault();

        var accrued = ComputeAccruedInterest(loan, payments);
        var interestOutstanding = Math.Max(0m, accrued - totalInterestPaid);

        return new LoanDto(
            loan.Id, loan.LenderId,
            loan.Lender?.Name ?? string.Empty,
            loan.Lender?.LenderType ?? LenderType.Other,
            loan.Direction, loan.Direction.ToString(),
            loan.LoanNumber, loan.Description,
            loan.PrincipalAmount, loan.CurrentInterestRate,
            loan.InterestCalculationType, loan.InterestCalculationType.ToString(),
            loan.PaymentFrequency, loan.PaymentFrequency.ToString(),
            loan.IsEmiLoan, loan.EmiAmount,
            loan.LoanStartDate, loan.LoanEndDate,
            loan.Status, loan.Status.ToString(),
            loan.ClosureDate, loan.ClosureNotes, loan.Notes,
            loan.CreatedAt,
            totalPrincipalPaid, totalInterestPaid, totalPenaltyPaid, totalAmountPaid,
            outstanding, accrued, interestOutstanding, payments.Count,
            lastPayment?.PaymentDate
        );
    }

    /// <summary>
    /// Interest accrued from loan start to today on the reducing principal balance.
    /// Requires loan.Payments and loan.InterestRateHistory to be loaded.
    /// </summary>
    internal static decimal ComputeAccruedInterest(Loan loan, IReadOnlyCollection<Domain.Entities.LoanPayment> payments)
    {
        var principalEvents = payments
            .Where(p => p.PrincipalAmount > 0)
            .Select(p => new LoanInterestCalculator.PrincipalEvent(p.PaymentDate, p.PrincipalAmount))
            .ToList();

        var rateChanges = (loan.InterestRateHistory ?? Enumerable.Empty<Domain.Entities.LoanInterestRateHistory>())
            .Select(r => new LoanInterestCalculator.RateChange(r.EffectiveDate, r.PreviousRate, r.NewRate))
            .ToList();

        return LoanInterestCalculator.AccruedInterestToDate(
            loan.PrincipalAmount,
            loan.CurrentInterestRate,
            loan.InterestCalculationType,
            loan.LoanStartDate,
            loan.ClosureDate,
            principalEvents,
            rateChanges,
            DateOnly.FromDateTime(DateTime.UtcNow));
    }
}
