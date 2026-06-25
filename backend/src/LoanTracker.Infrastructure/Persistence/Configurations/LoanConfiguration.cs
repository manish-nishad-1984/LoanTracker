using LoanTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanTracker.Infrastructure.Persistence.Configurations;

public class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("loans");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.LenderId).HasColumnName("lender_id").IsRequired();

        builder.Property(e => e.Direction)
            .HasColumnName("direction")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(Domain.Enums.LoanDirection.Borrowed);

        builder.Property(e => e.LoanNumber).HasColumnName("loan_number").HasMaxLength(100);
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);

        builder.Property(e => e.PrincipalAmount)
            .HasColumnName("principal_amount")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(e => e.CurrentInterestRate)
            .HasColumnName("current_interest_rate")
            .HasColumnType("numeric(8,4)")
            .HasDefaultValue(0m);

        builder.Property(e => e.InterestCalculationType)
            .HasColumnName("interest_calculation_type")
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(e => e.PaymentFrequency)
            .HasColumnName("payment_frequency")
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.IsEmiLoan).HasColumnName("is_emi_loan").HasDefaultValue(false);

        builder.Property(e => e.EmiAmount)
            .HasColumnName("emi_amount")
            .HasColumnType("numeric(18,2)");

        builder.Property(e => e.LoanStartDate).HasColumnName("loan_start_date");
        builder.Property(e => e.LoanEndDate).HasColumnName("loan_end_date");

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.ClosureDate).HasColumnName("closure_date");
        builder.Property(e => e.ClosureNotes).HasColumnName("closure_notes");
        builder.Property(e => e.Notes).HasColumnName("notes");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(e => e.LenderId);
        builder.HasIndex(e => e.Status).HasFilter("deleted_at IS NULL");
        builder.HasIndex(e => e.LoanNumber)
            .IsUnique()
            .HasFilter("loan_number IS NOT NULL AND deleted_at IS NULL");

        builder.HasMany(e => e.Payments)
            .WithOne(e => e.Loan)
            .HasForeignKey(e => e.LoanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.InterestRateHistory)
            .WithOne(e => e.Loan)
            .HasForeignKey(e => e.LoanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Documents)
            .WithOne(e => e.Loan)
            .HasForeignKey(e => e.LoanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
