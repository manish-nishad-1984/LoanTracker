using LoanTracker.Application.DTOs.Dashboard;
using LoanTracker.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LoanTracker.Infrastructure.Services;

public class DashboardService(IApplicationDbContext db) : IDashboardService
{
    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var thisMonth = new DateOnly(today.Year, today.Month, 1);
        var thisYear = new DateOnly(today.Year, 1, 1);

        // Core loan aggregates
        var loanStats = await db.Loans
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalBorrowed = g.Sum(l => l.PrincipalAmount),
                TotalLoans = g.Count(),
                ActiveLoans = g.Count(l => l.Status == Domain.Enums.LoanStatus.Active),
                ClosedLoans = g.Count(l => l.Status == Domain.Enums.LoanStatus.Closed),
                DefaultedLoans = g.Count(l => l.Status == Domain.Enums.LoanStatus.Defaulted)
            })
            .FirstOrDefaultAsync(ct);

        // Payment aggregates
        var paymentStats = await db.LoanPayments
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalPrincipalPaid = g.Sum(p => p.PrincipalAmount),
                TotalInterestPaid = g.Sum(p => p.InterestAmount),
                TotalPenaltyPaid = g.Sum(p => p.PenaltyAmount),
                TotalAmountPaid = g.Sum(p => p.TotalAmount),
                ThisMonthPayment = g.Where(p => p.PaymentDate >= thisMonth).Sum(p => p.TotalAmount),
                ThisYearPayment = g.Where(p => p.PaymentDate >= thisYear).Sum(p => p.TotalAmount)
            })
            .FirstOrDefaultAsync(ct);

        var lenderSummaries = await GetLenderSummariesAsync(ct);
        var monthlyTrend = await GetMonthlyTrendAsync(12, ct);
        var recentPayments = await GetRecentPaymentsAsync(10, ct);
        var topOutstanding = await GetTopOutstandingLoansAsync(5, ct);

        var totalBorrowed = loanStats?.TotalBorrowed ?? 0;
        var totalPrincipalPaid = paymentStats?.TotalPrincipalPaid ?? 0;
        var totalInterestPaid = paymentStats?.TotalInterestPaid ?? 0;

        // Accrued interest to date across all loans (reducing-balance daily accrual).
        var allLoans = await db.Loans
            .AsNoTracking()
            .Include(l => l.Payments)
            .Include(l => l.InterestRateHistory)
            .ToListAsync(ct);

        var totalInterestAccrued = allLoans.Sum(l =>
            LoanService.ComputeAccruedInterest(l, l.Payments.Where(p => !p.IsDeleted).ToList()));
        var totalInterestOutstanding = Math.Max(0m, totalInterestAccrued - totalInterestPaid);

        // Direction split: Borrowed = money I owe, Lent = money owed to me.
        decimal Outstanding(Domain.Entities.Loan l) =>
            l.PrincipalAmount - l.Payments.Where(p => !p.IsDeleted).Sum(p => p.PrincipalAmount);

        var borrowedLoans = allLoans.Where(l => l.Direction == Domain.Enums.LoanDirection.Borrowed).ToList();
        var lentLoans = allLoans.Where(l => l.Direction == Domain.Enums.LoanDirection.Lent).ToList();
        var borrowedOutstanding = borrowedLoans.Sum(Outstanding);
        var lentOutstanding = lentLoans.Sum(Outstanding);
        var netPosition = lentOutstanding - borrowedOutstanding;

        return new DashboardSummaryDto(
            TotalBorrowed: totalBorrowed,
            TotalOutstanding: totalBorrowed - totalPrincipalPaid,
            TotalPrincipalPaid: totalPrincipalPaid,
            TotalInterestPaid: totalInterestPaid,
            TotalInterestAccrued: totalInterestAccrued,
            TotalInterestOutstanding: totalInterestOutstanding,
            TotalPenaltyPaid: paymentStats?.TotalPenaltyPaid ?? 0,
            TotalAmountPaid: paymentStats?.TotalAmountPaid ?? 0,
            BorrowedOutstanding: borrowedOutstanding,
            LentOutstanding: lentOutstanding,
            NetPosition: netPosition,
            BorrowedLoans: borrowedLoans.Count,
            LentLoans: lentLoans.Count,
            TotalLoans: loanStats?.TotalLoans ?? 0,
            ActiveLoans: loanStats?.ActiveLoans ?? 0,
            ClosedLoans: loanStats?.ClosedLoans ?? 0,
            DefaultedLoans: loanStats?.DefaultedLoans ?? 0,
            ThisMonthPayment: paymentStats?.ThisMonthPayment ?? 0,
            ThisYearPayment: paymentStats?.ThisYearPayment ?? 0,
            LenderSummaries: lenderSummaries,
            RecentPayments: recentPayments,
            MonthlyTrend: monthlyTrend,
            TopOutstandingLoans: topOutstanding
        );
    }

    public async Task<IReadOnlyList<LenderSummaryDto>> GetLenderSummariesAsync(CancellationToken ct = default)
    {
        // Global query filters already exclude soft-deleted loans/payments.
        // Fetch then aggregate in memory — data volume is small and this avoids
        // untranslatable nested-subquery projections.
        var lenders = await db.Lenders
            .AsNoTracking()
            .Include(le => le.Loans)
                .ThenInclude(l => l.Payments)
            .ToListAsync(ct);

        return lenders
            .Select(le =>
            {
                var loans = le.Loans.ToList();
                var payments = loans.SelectMany(l => l.Payments).ToList();
                var totalBorrowed = loans.Sum(l => l.PrincipalAmount);
                var principalPaid = payments.Sum(p => p.PrincipalAmount);

                return new LenderSummaryDto(
                    le.Id,
                    le.Name,
                    le.LenderType.ToString(),
                    loans.Count,
                    loans.Count(l => l.Status == Domain.Enums.LoanStatus.Active),
                    totalBorrowed,
                    totalBorrowed - principalPaid,
                    principalPaid,
                    payments.Sum(p => p.InterestAmount),
                    payments.Sum(p => p.TotalAmount)
                );
            })
            .OrderByDescending(s => s.TotalOutstanding)
            .ToList();
    }

    public async Task<IReadOnlyList<MonthlyPaymentDto>> GetMonthlyTrendAsync(int months = 24, CancellationToken ct = default)
    {
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-months));

        var raw = await db.LoanPayments
            .AsNoTracking()
            .Where(p => p.PaymentDate >= cutoff)
            .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                PrincipalPaid = g.Sum(p => p.PrincipalAmount),
                InterestPaid = g.Sum(p => p.InterestAmount),
                PenaltyPaid = g.Sum(p => p.PenaltyAmount),
                TotalPaid = g.Sum(p => p.TotalAmount),
                PaymentCount = g.Count()
            })
            .OrderByDescending(x => x.Year).ThenByDescending(x => x.Month)
            .ToListAsync(ct);

        return raw.Select(r => new MonthlyPaymentDto(
            r.Year, r.Month,
            new DateTime(r.Year, r.Month, 1).ToString("MMM yyyy"),
            r.PrincipalPaid, r.InterestPaid, r.PenaltyPaid,
            r.TotalPaid, r.PaymentCount
        )).ToList();
    }

    public async Task<IReadOnlyList<YearlySummaryDto>> GetYearlySummaryAsync(int years = 5, CancellationToken ct = default)
    {
        var cutoffYear = DateTime.UtcNow.Year - years + 1;

        var raw = await db.LoanPayments
            .AsNoTracking()
            .Where(p => p.PaymentDate.Year >= cutoffYear)
            .GroupBy(p => p.PaymentDate.Year)
            .Select(g => new YearlySummaryDto(
                g.Key,
                g.Sum(p => p.TotalAmount),
                g.Sum(p => p.PrincipalAmount),
                g.Sum(p => p.InterestAmount),
                g.Count()
            ))
            .OrderByDescending(x => x.Year)
            .ToListAsync(ct);

        return raw;
    }

    private async Task<IReadOnlyList<RecentPaymentDto>> GetRecentPaymentsAsync(int count, CancellationToken ct)
    {
        return await db.LoanPayments
            .AsNoTracking()
            .Include(p => p.Loan).ThenInclude(l => l.Lender)
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.CreatedAt)
            .Take(count)
            .Select(p => new RecentPaymentDto(
                p.Id, p.LoanId,
                p.Loan.Description ?? p.Loan.LoanNumber ?? "Loan",
                p.Loan.Lender.Name,
                p.PaymentDate,
                p.TotalAmount, p.PrincipalAmount, p.InterestAmount,
                p.PaymentMode.ToString()
            ))
            .ToListAsync(ct);
    }

    private async Task<IReadOnlyList<LoanOutstandingDto>> GetTopOutstandingLoansAsync(int count, CancellationToken ct)
    {
        var activeLoans = await db.Loans
            .AsNoTracking()
            .Include(l => l.Lender)
            .Include(l => l.Payments)
            .Where(l => l.Status == Domain.Enums.LoanStatus.Active)
            .ToListAsync(ct);

        return activeLoans
            .Select(l => new LoanOutstandingDto(
                l.Id,
                l.Description ?? l.LoanNumber ?? "Loan",
                l.Lender.Name,
                l.PrincipalAmount,
                l.PrincipalAmount - l.Payments.Sum(p => p.PrincipalAmount),
                l.CurrentInterestRate,
                l.Status.ToString()
            ))
            .OrderByDescending(x => x.OutstandingPrincipal)
            .Take(count)
            .ToList();
    }
}
