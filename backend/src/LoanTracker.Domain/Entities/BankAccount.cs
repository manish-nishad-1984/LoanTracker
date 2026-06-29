namespace LoanTracker.Domain.Entities;

public class BankAccount : BaseAuditableEntity
{
    public string AccountNumber { get; set; } = string.Empty;
    public string? AccountName { get; set; }
    public string? Bank { get; set; }

    public ICollection<BankTransaction> Transactions { get; set; } = new List<BankTransaction>();
}
