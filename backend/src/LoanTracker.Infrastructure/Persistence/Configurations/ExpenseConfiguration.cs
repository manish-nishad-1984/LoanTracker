using LoanTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanTracker.Infrastructure.Persistence.Configurations;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("expenses");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.Date).HasColumnName("expense_date");
        builder.Property(e => e.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        builder.Property(e => e.Category).HasColumnName("category").HasMaxLength(60);
        builder.Property(e => e.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)");
        builder.Property(e => e.PaymentMethod).HasColumnName("payment_method").HasMaxLength(30);
        builder.Property(e => e.Vendor).HasColumnName("vendor").HasMaxLength(200);
        builder.Property(e => e.Notes).HasColumnName("notes");
        builder.Property(e => e.Source).HasColumnName("source").HasMaxLength(20);
        builder.Property(e => e.SourceReference).HasColumnName("source_reference").HasMaxLength(100);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(e => e.Date);
        builder.HasIndex(e => e.Category);
        builder.HasMany(e => e.Attachments).WithOne(a => a.Expense)
            .HasForeignKey(a => a.ExpenseId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ExpenseAttachmentConfiguration : IEntityTypeConfiguration<ExpenseAttachment>
{
    public void Configure(EntityTypeBuilder<ExpenseAttachment> builder)
    {
        builder.ToTable("expense_attachments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(a => a.ExpenseId).HasColumnName("expense_id").IsRequired();
        builder.Property(a => a.FileName).HasColumnName("file_name").HasMaxLength(300);
        builder.Property(a => a.ContentType).HasColumnName("content_type").HasMaxLength(100);
        builder.Property(a => a.Kind).HasColumnName("kind").HasMaxLength(20);
        builder.Property(a => a.SizeBytes).HasColumnName("size_bytes");
        builder.Property(a => a.Data).HasColumnName("data").HasColumnType("bytea");
        builder.Property(a => a.UploadedAt).HasColumnName("uploaded_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(a => a.ExpenseId);
    }
}
