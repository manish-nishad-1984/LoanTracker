namespace LoanTracker.Domain.Enums;

public enum LoanDirection
{
    /// <summary>Money I borrowed — I owe the counterparty.</summary>
    Borrowed = 1,

    /// <summary>Money I lent out — the counterparty owes me.</summary>
    Lent     = 2
}
