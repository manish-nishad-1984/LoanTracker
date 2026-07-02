namespace LoanTracker.Domain.Entities;

public class Expense : BaseAuditableEntity
{
    public DateOnly Date { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Other";
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }   // Cash / UPI / Card / Bank / ...
    public string? Vendor { get; set; }           // shop / party / merchant
    public string? Notes { get; set; }
    public string Source { get; set; } = "Manual"; // Manual | Bank
    public string? SourceReference { get; set; }    // originating bank transaction id, if converted

    public ICollection<ExpenseAttachment> Attachments { get; set; } = new List<ExpenseAttachment>();
}

public class ExpenseAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExpenseId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public string Kind { get; set; } = "Bill";    // Bill | Product | Invoice | Other
    public long SizeBytes { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Expense Expense { get; set; } = null!;
}
