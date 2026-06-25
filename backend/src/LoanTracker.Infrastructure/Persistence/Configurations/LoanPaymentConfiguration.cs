using LoanTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanTracker.Infrastructure.Persistence.Configurations;

public class LoanPaymentConfiguration : IEntityTypeConfiguration<LoanPayment>
{
    public void Configure(EntityTypeBuilder<LoanPayment> builder)
    {
        builder.ToTable("loan_payments");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.LoanId).HasColumnName("loan_id").IsRequired();
        builder.Property(e => e.PaymentDate).HasColumnName("payment_date").IsRequired();

        builder.Property(e => e.TotalAmount)
            .HasColumnName("total_amount")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(e => e.PrincipalAmount)
            .HasColumnName("principal_amount")
            .HasColumnType("numeric(18,2)")
            .HasDefaultValue(0m);

        builder.Property(e => e.InterestAmount)
            .HasColumnName("interest_amount")
            .HasColumnType("numeric(18,2)")
            .HasDefaultValue(0m);

        builder.Property(e => e.PenaltyAmount)
            .HasColumnName("penalty_amount")
            .HasColumnType("numeric(18,2)")
            .HasDefaultValue(0m);

        builder.Property(e => e.PaymentMode)
            .HasColumnName("payment_mode")
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.ReferenceNumber).HasColumnName("reference_number").HasMaxLength(100);
        builder.Property(e => e.Remarks).HasColumnName("remarks");
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(200);

        builder.HasIndex(e => e.LoanId).HasFilter("is_deleted = FALSE");
        builder.HasIndex(e => e.PaymentDate).HasFilter("is_deleted = FALSE");
    }
}
