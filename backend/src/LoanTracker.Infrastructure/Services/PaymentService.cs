using LoanTracker.Application.Common;
using LoanTracker.Application.DTOs.Payments;
using LoanTracker.Application.Interfaces;
using LoanTracker.Domain.Entities;
using LoanTracker.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LoanTracker.Infrastructure.Services;

public class PaymentService(IApplicationDbContext db, ILogger<PaymentService> logger) : IPaymentService
{
    public async Task<PagedResult<PaymentListItemDto>> GetByLoanAsync(
        Guid loanId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.LoanPayments
            .AsNoTracking()
            .Include(p => p.Loan).ThenInclude(l => l.Lender)
            .Where(p => p.LoanId == loanId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => MapToListItem(p))
            .ToListAsync(ct);

        return PagedResult<PaymentListItemDto>.Create(items, total, page, pageSize);
    }

    public async Task<PagedResult<PaymentListItemDto>> GetAllAsync(
        Guid? loanId, Guid? lenderId, DateOnly? from, DateOnly? to,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.LoanPayments
            .AsNoTracking()
            .Include(p => p.Loan).ThenInclude(l => l.Lender)
            .AsQueryable();

        if (loanId.HasValue)
            query = query.Where(p => p.LoanId == loanId.Value);

        if (lenderId.HasValue)
            query = query.Where(p => p.Loan.LenderId == lenderId.Value);

        if (from.HasValue)
            query = query.Where(p => p.PaymentDate >= from.Value);

        if (to.HasValue)
            query = query.Where(p => p.PaymentDate <= to.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => MapToListItem(p))
            .ToListAsync(ct);

        return PagedResult<PaymentListItemDto>.Create(items, total, page, pageSize);
    }

    public async Task<Result<PaymentDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var payment = await db.LoanPayments
            .AsNoTracking()
            .Include(p => p.Loan).ThenInclude(l => l.Lender)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (payment is null) return Result<PaymentDto>.NotFound("Payment", id);
        return Result<PaymentDto>.Success(MapToDto(payment));
    }

    public async Task<Result<PaymentDto>> CreateAsync(CreatePaymentRequest request, CancellationToken ct = default)
    {
        var loan = await db.Loans
            .Include(l => l.Payments.Where(p => !p.IsDeleted))
            .FirstOrDefaultAsync(l => l.Id == request.LoanId, ct);

        if (loan is null)
            return Result<PaymentDto>.NotFound("Loan", request.LoanId);

        if (loan.Status == LoanStatus.Closed)
            return Result<PaymentDto>.BadRequest("Cannot record payment on a closed loan.");

        var currentOutstanding = loan.PrincipalAmount - loan.Payments.Sum(p => p.PrincipalAmount);

        if (request.PrincipalAmount > currentOutstanding)
            return Result<PaymentDto>.BadRequest(
                $"Principal payment ({request.PrincipalAmount:N2}) exceeds outstanding principal ({currentOutstanding:N2}). " +
                $"Use the exact outstanding amount to close the loan.");

        var payment = new LoanPayment
        {
            LoanId = request.LoanId,
            PaymentDate = request.PaymentDate,
            TotalAmount = request.TotalAmount,
            PrincipalAmount = request.PrincipalAmount,
            InterestAmount = request.InterestAmount,
            PenaltyAmount = request.PenaltyAmount,
            PaymentMode = request.PaymentMode,
            ReferenceNumber = request.ReferenceNumber?.Trim(),
            Remarks = request.Remarks?.Trim()
        };

        db.LoanPayments.Add(payment);

        // Auto-close loan if outstanding becomes 0
        var newOutstanding = currentOutstanding - request.PrincipalAmount;
        if (newOutstanding <= 0 && loan.Status == LoanStatus.Active)
        {
            loan.Status = LoanStatus.Closed;
            loan.ClosureDate = request.PaymentDate;
            loan.ClosureNotes = "Auto-closed: outstanding principal fully repaid.";
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Recorded payment {PaymentId} for loan {LoanId} — total {TotalAmount}",
            payment.Id, request.LoanId, request.TotalAmount);

        var created = await GetByIdAsync(payment.Id, ct);
        return Result<PaymentDto>.Created(created.Value!);
    }

    public async Task<Result<PaymentDto>> UpdateAsync(Guid id, UpdatePaymentRequest request, CancellationToken ct = default)
    {
        var payment = await db.LoanPayments
            .Include(p => p.Loan)
                .ThenInclude(l => l.Payments.Where(p2 => !p2.IsDeleted && p2.Id != id))
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (payment is null) return Result<PaymentDto>.NotFound("Payment", id);

        if (payment.Loan.Status == LoanStatus.Closed)
            return Result<PaymentDto>.BadRequest("Cannot edit payment on a closed loan.");

        // Validate new principal doesn't exceed outstanding excluding this payment
        var otherPrincipalPaid = payment.Loan.Payments.Sum(p => p.PrincipalAmount);
        var currentOutstanding = payment.Loan.PrincipalAmount - otherPrincipalPaid;

        if (request.PrincipalAmount > currentOutstanding)
            return Result<PaymentDto>.BadRequest(
                $"Updated principal ({request.PrincipalAmount:N2}) exceeds available outstanding ({currentOutstanding:N2}).");

        payment.PaymentDate = request.PaymentDate;
        payment.TotalAmount = request.TotalAmount;
        payment.PrincipalAmount = request.PrincipalAmount;
        payment.InterestAmount = request.InterestAmount;
        payment.PenaltyAmount = request.PenaltyAmount;
        payment.PaymentMode = request.PaymentMode;
        payment.ReferenceNumber = request.ReferenceNumber?.Trim();
        payment.Remarks = request.Remarks?.Trim();

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Updated payment {PaymentId}", id);

        return await GetByIdAsync(id, ct);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var payment = await db.LoanPayments
            .Include(p => p.Loan)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (payment is null) return Result.NotFound("Payment", id);

        if (payment.Loan.Status == LoanStatus.Closed && payment.PrincipalAmount > 0)
            return Result.BadRequest(
                "Cannot delete a principal payment from a closed loan. Reopen the loan first.");

        payment.IsDeleted = true;

        // Re-evaluate loan status if it was auto-closed
        if (payment.Loan.Status == LoanStatus.Closed && payment.PrincipalAmount > 0)
        {
            payment.Loan.Status = LoanStatus.Active;
            payment.Loan.ClosureDate = null;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Soft-deleted payment {PaymentId}", id);
        return Result.Success();
    }

    private static PaymentDto MapToDto(LoanPayment p) => new(
        p.Id, p.LoanId,
        p.Loan?.Description ?? p.Loan?.LoanNumber ?? "Loan",
        p.Loan?.Lender?.Name ?? string.Empty,
        p.PaymentDate, p.TotalAmount, p.PrincipalAmount,
        p.InterestAmount, p.PenaltyAmount,
        p.PaymentMode, p.PaymentMode.ToString(),
        p.ReferenceNumber, p.Remarks, p.CreatedAt
    );

    private static PaymentListItemDto MapToListItem(LoanPayment p) => new(
        p.Id, p.LoanId,
        p.Loan?.Description ?? p.Loan?.LoanNumber ?? "Loan",
        p.Loan?.Lender?.Name ?? string.Empty,
        p.PaymentDate, p.TotalAmount, p.PrincipalAmount,
        p.InterestAmount, p.PenaltyAmount,
        p.PaymentMode, p.PaymentMode.ToString(),
        p.ReferenceNumber, p.Remarks, p.CreatedAt
    );
}
