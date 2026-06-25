using LoanTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanTracker.Infrastructure.Persistence.Configurations;

public class LoanInterestRateHistoryConfiguration : IEntityTypeConfiguration<LoanInterestRateHistory>
{
    public void Configure(EntityTypeBuilder<LoanInterestRateHistory> builder)
    {
        builder.ToTable("loan_interest_rate_history");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.LoanId).HasColumnName("loan_id").IsRequired();
        builder.Property(e => e.PreviousRate).HasColumnName("previous_rate").HasColumnType("numeric(8,4)");
        builder.Property(e => e.NewRate).HasColumnName("new_rate").HasColumnType("numeric(8,4)");
        builder.Property(e => e.EffectiveDate).HasColumnName("effective_date");
        builder.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(500);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(200);

        builder.HasIndex(e => new { e.LoanId, e.EffectiveDate });
    }
}
