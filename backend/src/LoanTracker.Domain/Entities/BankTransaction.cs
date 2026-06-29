namespace LoanTracker.Domain.Entities;

public class BankTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BankAccountId { get; set; }

    public int Seq { get; set; }   // statement line order (for stable balance ordering)
    public DateOnly TxnDate { get; set; }
    public DateOnly? ValueDate { get; set; }
    public string Narration { get; set; } = string.Empty;
    public string? ReferenceNo { get; set; }

    public decimal Withdrawal { get; set; }   // money OUT
    public decimal Deposit { get; set; }       // money IN
    public decimal? ClosingBalance { get; set; }

    public string Direction { get; set; } = "Out";   // In / Out
    public string Group { get; set; } = "Expense";    // Income / Expense / Transfer
    public string PaymentMethod { get; set; } = "Other"; // UPI / NEFT / IMPS / ATM / POS / ACH / ...
    public string Category { get; set; } = "Uncategorized";
    public string? Merchant { get; set; }

    /// <summary>Stable hash used to skip duplicates on re-import.</summary>
    public string DedupKey { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public BankAccount Account { get; set; } = null!;
}
