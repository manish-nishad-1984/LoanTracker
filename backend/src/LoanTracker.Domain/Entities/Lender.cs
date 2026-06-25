using LoanTracker.Domain.Enums;

namespace LoanTracker.Domain.Entities;

public class Lender : BaseAuditableEntity
{
    public LenderType LenderType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }

    // Bank-specific fields
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? IfscCode { get; set; }
    public string? PanNumber { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
