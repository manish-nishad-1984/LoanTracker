using LoanTracker.Domain.Enums;

namespace LoanTracker.Domain.Entities;

public class LoanPayment : BaseAuditableEntity
{
    public Guid LoanId { get; set; }
    public DateOnly PaymentDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public PaymentMode PaymentMode { get; set; } = PaymentMode.Cash;
    public string? ReferenceNumber { get; set; }
    public string? Remarks { get; set; }
    public bool IsDeleted { get; set; }
    public string? CreatedBy { get; set; }

    // Navigation
    public Loan Loan { get; set; } = null!;
}
