namespace LoanTracker.Application.DTOs.Dashboard;

public record DashboardSummaryDto(
    decimal TotalBorrowed,
    decimal TotalOutstanding,
    decimal TotalPrincipalPaid,
    decimal TotalInterestPaid,
    decimal TotalInterestAccrued,
    decimal TotalInterestOutstanding,
    decimal TotalPenaltyPaid,
    decimal TotalAmountPaid,
    int TotalLoans,
    int ActiveLoans,
    int ClosedLoans,
    int DefaultedLoans,
    decimal ThisMonthPayment,
    decimal ThisYearPayment,
    IReadOnlyList<LenderSummaryDto> LenderSummaries,
    IReadOnlyList<RecentPaymentDto> RecentPayments,
    IReadOnlyList<MonthlyPaymentDto> MonthlyTrend,
    IReadOnlyList<LoanOutstandingDto> TopOutstandingLoans
);

public record LenderSummaryDto(
    Guid LenderId,
    string LenderName,
    string LenderType,
    int TotalLoans,
    int ActiveLoans,
    decimal TotalBorrowed,
    decimal TotalOutstanding,
    decimal TotalPrincipalPaid,
    decimal TotalInterestPaid,
    decimal TotalAmountPaid
);

public record MonthlyPaymentDto(
    int Year,
    int Month,
    string MonthName,
    decimal PrincipalPaid,
    decimal InterestPaid,
    decimal PenaltyPaid,
    decimal TotalPaid,
    int PaymentCount
);

public record RecentPaymentDto(
    Guid Id,
    Guid LoanId,
    string LoanDescription,
    string LenderName,
    DateOnly PaymentDate,
    decimal TotalAmount,
    decimal PrincipalAmount,
    decimal InterestAmount,
    string PaymentModeName
);

public record LoanOutstandingDto(
    Guid LoanId,
    string LoanDescription,
    string LenderName,
    decimal OriginalPrincipal,
    decimal OutstandingPrincipal,
    decimal CurrentInterestRate,
    string Status
);

public record YearlySummaryDto(
    int Year,
    decimal TotalPaid,
    decimal PrincipalPaid,
    decimal InterestPaid,
    int PaymentCount
);
