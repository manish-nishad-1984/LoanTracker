namespace LoanTracker.Domain.Entities;

public class LoanInterestRateHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LoanId { get; set; }
    public decimal PreviousRate { get; set; }
    public decimal NewRate { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }

    // Navigation
    public Loan Loan { get; set; } = null!;
}
