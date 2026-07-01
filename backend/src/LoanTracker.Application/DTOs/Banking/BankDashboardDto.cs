namespace LoanTracker.Application.DTOs.Banking;

public record ImportResultDto(
    Guid AccountId, string AccountNumber, string? AccountName,
    int TotalParsed, int Imported, int SkippedDuplicates);

public record BankAccountDto(Guid Id, string AccountNumber, string? AccountName, string? Bank, int TransactionCount);

public record CategoryStatDto(
    string Category, decimal Total, decimal Percentage, int Count,
    decimal Average, decimal Highest, decimal Lowest);

public record MerchantStatDto(string Merchant, decimal Total, int Count);

public record PaymentMethodStatDto(string Method, decimal Total, int Count, decimal Percentage);

public record MonthlyCashflowDto(int Year, int Month, string MonthName,
    decimal Income, decimal Expense, decimal Net, decimal? ClosingBalance);

public record DailySpendDto(DateOnly Date, decimal Spend);

public record WeekendWeekdayDto(decimal WeekendSpend, decimal WeekdaySpend, int WeekendCount, int WeekdayCount);

public record RecurringDto(string Merchant, int Count, decimal AvgAmount, string Category, bool LikelySubscription);

public record TxnLineDto(
    Guid Id, DateOnly Date, string Narration, string? Merchant,
    decimal Amount, string Direction, string Category, string PaymentMethod);

public record BankTxnListDto(
    IReadOnlyList<TxnLineDto> Items, int TotalCount, int Page, int PageSize,
    decimal TotalIn, decimal TotalOut);

public record GroupSummaryDto(string Key, int Count, decimal TotalIn, decimal TotalOut, decimal Net);

public record BankDashboardDto(
    string? AccountNumber,
    string? AccountName,
    string? Bank,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int Days,
    // Executive summary
    decimal OpeningBalance,
    decimal ClosingBalance,
    decimal TotalCredits,
    decimal TotalDebits,
    decimal NetCashFlow,
    decimal AvgDailyIncome,
    decimal AvgDailyExpense,
    decimal HighestCredit,
    decimal HighestDebit,
    int TransactionCount,
    decimal AvgTransactionAmount,
    string? LargestMerchant,
    string? MostFrequentMerchant,
    string? MostUsedPaymentMethod,
    decimal SavingsRate,
    decimal ExpenseRatio,
    int HealthScore,
    string HealthLabel,
    // Breakdowns
    IReadOnlyList<CategoryStatDto> IncomeByCategory,
    IReadOnlyList<CategoryStatDto> ExpenseByCategory,
    IReadOnlyList<MerchantStatDto> TopMerchants,
    IReadOnlyList<PaymentMethodStatDto> PaymentMethods,
    IReadOnlyList<MonthlyCashflowDto> MonthlyCashflow,
    IReadOnlyList<DailySpendDto> DailySpend,
    WeekendWeekdayDto WeekendVsWeekday,
    IReadOnlyList<RecurringDto> RecurringPayments,
    IReadOnlyList<TxnLineDto> LargestExpenses,
    IReadOnlyList<TxnLineDto> LargestIncomes,
    IReadOnlyList<string> Insights
);
