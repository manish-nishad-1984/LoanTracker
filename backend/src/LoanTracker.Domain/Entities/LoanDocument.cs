namespace LoanTracker.Domain.Entities;

public class LoanDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LoanId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string? DocumentType { get; set; }
    public string? FilePath { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? Notes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string? UploadedBy { get; set; }

    // Navigation
    public Loan Loan { get; set; } = null!;
}
