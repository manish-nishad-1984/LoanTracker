using LoanTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanTracker.Infrastructure.Persistence.Configurations;

public class LoanDocumentConfiguration : IEntityTypeConfiguration<LoanDocument>
{
    public void Configure(EntityTypeBuilder<LoanDocument> builder)
    {
        builder.ToTable("loan_documents");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.LoanId).HasColumnName("loan_id").IsRequired();
        builder.Property(e => e.DocumentName).HasColumnName("document_name").HasMaxLength(300).IsRequired();
        builder.Property(e => e.DocumentType).HasColumnName("document_type").HasMaxLength(100);
        builder.Property(e => e.FilePath).HasColumnName("file_path");
        builder.Property(e => e.FileSizeBytes).HasColumnName("file_size_bytes");
        builder.Property(e => e.Notes).HasColumnName("notes");
        builder.Property(e => e.UploadedAt).HasColumnName("uploaded_at").HasDefaultValueSql("NOW()");
        builder.Property(e => e.UploadedBy).HasColumnName("uploaded_by").HasMaxLength(200);

        builder.HasIndex(e => e.LoanId);
    }
}
