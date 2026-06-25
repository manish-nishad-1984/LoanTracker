using LoanTracker.Application.Interfaces;
using LoanTracker.Domain.Entities;
using LoanTracker.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace LoanTracker.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Lender> Lenders => Set<Lender>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanPayment> LoanPayments => Set<LoanPayment>();
    public DbSet<LoanInterestRateHistory> LoanInterestRateHistories => Set<LoanInterestRateHistory>();
    public DbSet<LoanDocument> LoanDocuments => Set<LoanDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new LenderConfiguration());
        modelBuilder.ApplyConfiguration(new LoanConfiguration());
        modelBuilder.ApplyConfiguration(new LoanPaymentConfiguration());
        modelBuilder.ApplyConfiguration(new LoanInterestRateHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new LoanDocumentConfiguration());

        // Global soft-delete filter for Lender and Loan
        modelBuilder.Entity<Lender>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Loan>().HasQueryFilter(e => e.DeletedAt == null);

        // Global filter for non-deleted payments
        modelBuilder.Entity<LoanPayment>().HasQueryFilter(e => !e.IsDeleted);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;

            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
