using LoanTracker.Domain.Enums;

namespace LoanTracker.Domain.Entities;

public class Loan : BaseAuditableEntity
{
    public Guid LenderId { get; set; }
    public LoanDirection Direction { get; set; } = LoanDirection.Borrowed;
    public string? LoanNumber { get; set; }
    public string? Description { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal CurrentInterestRate { get; set; }
    public InterestCalculationType InterestCalculationType { get; set; } = InterestCalculationType.Simple;
    public PaymentFrequency PaymentFrequency { get; set; } = PaymentFrequency.Monthly;
    public bool IsEmiLoan { get; set; }
    public decimal? EmiAmount { get; set; }
    public DateOnly LoanStartDate { get; set; }
    public DateOnly? LoanEndDate { get; set; }
    public LoanStatus Status { get; set; } = LoanStatus.Active;
    public DateOnly? ClosureDate { get; set; }
    public string? ClosureNotes { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Lender Lender { get; set; } = null!;
    public ICollection<LoanPayment> Payments { get; set; } = new List<LoanPayment>();
    public ICollection<LoanInterestRateHistory> InterestRateHistory { get; set; } = new List<LoanInterestRateHistory>();
    public ICollection<LoanDocument> Documents { get; set; } = new List<LoanDocument>();
}
