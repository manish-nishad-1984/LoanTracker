using FluentValidation;

namespace LoanTracker.Application.DTOs.Expenses;

public record AttachmentDto(Guid Id, string FileName, string ContentType, string Kind, long SizeBytes);

public record ExpenseDto(
    Guid Id, DateOnly Date, string Title, string Category, decimal Amount,
    string? PaymentMethod, string? Vendor, string? Notes, string Source, string? SourceReference,
    DateTime CreatedAt, IReadOnlyList<AttachmentDto> Attachments);

public record ExpenseListItemDto(
    Guid Id, DateOnly Date, string Title, string Category, decimal Amount,
    string? PaymentMethod, string? Vendor, string Source, int AttachmentCount);

public record CreateExpenseRequest(
    DateOnly Date, string Title, string Category, decimal Amount,
    string? PaymentMethod, string? Vendor, string? Notes);

public record UpdateExpenseRequest(
    DateOnly Date, string Title, string Category, decimal Amount,
    string? PaymentMethod, string? Vendor, string? Notes);

public record ConvertTransactionRequest(
    Guid TransactionId, string Category, string? Vendor, string? Notes, string? PaymentMethod);

// ── Dashboard ──
public record ExpenseCategoryStatDto(string Category, decimal Total, decimal Percentage, int Count);
public record ExpenseMonthlyDto(int Year, int Month, string MonthName, decimal Total, int Count);
public record ExpenseSummaryDto(
    DateOnly? FromDate, DateOnly? ToDate,
    decimal Total, int Count, decimal AveragePerDay, decimal AveragePerExpense,
    decimal ThisMonth, decimal LastMonth, string? TopCategory,
    IReadOnlyList<ExpenseCategoryStatDto> ByCategory,
    IReadOnlyList<ExpenseMonthlyDto> Monthly,
    IReadOnlyList<ExpenseListItemDto> Recent);

public class CreateExpenseValidator : AbstractValidator<CreateExpenseRequest>
{
    public CreateExpenseValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Date).NotEmpty();
    }
}

public class UpdateExpenseValidator : AbstractValidator<UpdateExpenseRequest>
{
    public UpdateExpenseValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}
