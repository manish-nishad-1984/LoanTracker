using LoanTracker.Domain.Enums;

namespace LoanTracker.Application.DTOs.Loans;

public record LoanDto(
    Guid Id,
    Guid LenderId,
    string LenderName,
    LenderType LenderType,
    LoanDirection Direction,
    string DirectionName,
    string? LoanNumber,
    string? Description,
    decimal PrincipalAmount,
    decimal CurrentInterestRate,
    InterestCalculationType InterestCalculationType,
    string InterestCalculationTypeName,
    PaymentFrequency PaymentFrequency,
    string PaymentFrequencyName,
    bool IsEmiLoan,
    decimal? EmiAmount,
    DateOnly LoanStartDate,
    DateOnly? LoanEndDate,
    LoanStatus Status,
    string StatusName,
    DateOnly? ClosureDate,
    string? ClosureNotes,
    string? Notes,
    DateTime CreatedAt,
    // Computed financials
    decimal TotalPrincipalPaid,
    decimal TotalInterestPaid,
    decimal TotalPenaltyPaid,
    decimal TotalAmountPaid,
    decimal OutstandingPrincipal,
    decimal InterestAccruedToDate,
    decimal InterestOutstanding,
    int PaymentCount,
    DateOnly? LastPaymentDate
);

public record LoanListItemDto(
    Guid Id,
    Guid LenderId,
    string LenderName,
    LenderType LenderType,
    LoanDirection Direction,
    string DirectionName,
    string? LoanNumber,
    string? Description,
    decimal PrincipalAmount,
    decimal CurrentInterestRate,
    LoanStatus Status,
    string StatusName,
    DateOnly LoanStartDate,
    DateOnly? LoanEndDate,
    bool IsEmiLoan,
    decimal OutstandingPrincipal,
    decimal TotalAmountPaid,
    decimal InterestAccruedToDate,
    decimal TotalInterestPaid,
    DateOnly? LastPaymentDate,
    DateTime CreatedAt
);

public record LoanSummaryDto(
    Guid LoanId,
    decimal OriginalPrincipal,
    decimal TotalPrincipalPaid,
    decimal TotalInterestPaid,
    decimal TotalPenaltyPaid,
    decimal TotalAmountPaid,
    decimal OutstandingPrincipal,
    int PaymentCount,
    DateOnly? LastPaymentDate,
    DateOnly? FirstPaymentDate
);
