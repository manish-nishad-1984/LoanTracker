using LoanTracker.Domain.Entities;
using LoanTracker.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanTracker.Infrastructure.Persistence.Configurations;

public class LenderConfiguration : IEntityTypeConfiguration<Lender>
{
    public void Configure(EntityTypeBuilder<Lender> builder)
    {
        builder.ToTable("lenders");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.LenderType)
            .HasColumnName("lender_type")
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(e => e.ContactName).HasColumnName("contact_name").HasMaxLength(200);
        builder.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20);
        builder.Property(e => e.Email).HasColumnName("email").HasMaxLength(200);
        builder.Property(e => e.Address).HasColumnName("address");
        builder.Property(e => e.BankName).HasColumnName("bank_name").HasMaxLength(200);
        builder.Property(e => e.AccountNumber).HasColumnName("account_number").HasMaxLength(50);
        builder.Property(e => e.IfscCode).HasColumnName("ifsc_code").HasMaxLength(20);
        builder.Property(e => e.PanNumber).HasColumnName("pan_number").HasMaxLength(20);
        builder.Property(e => e.Notes).HasColumnName("notes");
        builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(e => e.LenderType).HasFilter("deleted_at IS NULL");
        builder.HasIndex(e => e.IsActive).HasFilter("deleted_at IS NULL");

        builder.HasMany(e => e.Loans)
            .WithOne(e => e.Lender)
            .HasForeignKey(e => e.LenderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
