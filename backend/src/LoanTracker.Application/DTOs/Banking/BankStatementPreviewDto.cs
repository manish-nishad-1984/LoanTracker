namespace LoanTracker.Application.DTOs.Banking;

public record BankStatementPreviewDto(
    string? AccountNumber,
    string? AccountName,
    string? Bank,
    string? Period,
    int TransactionCount,
    decimal TotalDeposit,   // money IN
    decimal TotalWithdrawal, // money OUT
    decimal NetChange,
    IReadOnlyList<BankTransactionPreviewDto> Transactions
);

public record BankTransactionPreviewDto(
    int RowNumber,
    DateOnly? Date,
    string Narration,
    string? ReferenceNo,
    DateOnly? ValueDate,
    decimal Withdrawal,
    decimal Deposit,
    decimal? ClosingBalance,
    string Direction,      // "In" or "Out"
    string GuessedType,    // UPI / NEFT / ATM / POS / EMI / Salary / ...
    string GuessedCategory
);
