using LoanTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LoanTracker.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Lender> Lenders { get; }
    DbSet<Loan> Loans { get; }
    DbSet<LoanPayment> LoanPayments { get; }
    DbSet<LoanInterestRateHistory> LoanInterestRateHistories { get; }
    DbSet<LoanDocument> LoanDocuments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
