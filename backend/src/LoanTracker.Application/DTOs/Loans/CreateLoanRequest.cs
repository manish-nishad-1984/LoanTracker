using FluentValidation;
using LoanTracker.Domain.Enums;

namespace LoanTracker.Application.DTOs.Loans;

public record CreateLoanRequest(
    Guid LenderId,
    LoanDirection Direction,
    string? LoanNumber,
    string? Description,
    decimal PrincipalAmount,
    decimal CurrentInterestRate,
    InterestCalculationType InterestCalculationType,
    PaymentFrequency PaymentFrequency,
    bool IsEmiLoan,
    decimal? EmiAmount,
    DateOnly LoanStartDate,
    DateOnly? LoanEndDate,
    string? Notes
);

public record UpdateLoanRequest(
    string? LoanNumber,
    string? Description,
    decimal CurrentInterestRate,
    InterestCalculationType InterestCalculationType,
    PaymentFrequency PaymentFrequency,
    bool IsEmiLoan,
    decimal? EmiAmount,
    DateOnly? LoanEndDate,
    string? Notes
);

public record CloseLoanRequest(
    DateOnly ClosureDate,
    string? ClosureNotes
);

public record UpdateInterestRateRequest(
    decimal NewRate,
    DateOnly EffectiveDate,
    string? Reason
);

public class CreateLoanValidator : AbstractValidator<CreateLoanRequest>
{
    public CreateLoanValidator()
    {
        RuleFor(x => x.LenderId).NotEmpty();
        RuleFor(x => x.Direction).IsInEnum();
        RuleFor(x => x.PrincipalAmount).GreaterThan(0);
        RuleFor(x => x.CurrentInterestRate).GreaterThanOrEqualTo(0).LessThanOrEqualTo(100);
        RuleFor(x => x.InterestCalculationType).IsInEnum();
        RuleFor(x => x.PaymentFrequency).IsInEnum();
        RuleFor(x => x.LoanStartDate).NotEmpty();
        RuleFor(x => x.EmiAmount)
            .GreaterThan(0)
            .When(x => x.IsEmiLoan)
            .WithMessage("EMI amount is required when loan is EMI-based.");
        RuleFor(x => x.LoanEndDate)
            .GreaterThanOrEqualTo(x => x.LoanStartDate)
            .When(x => x.LoanEndDate.HasValue);
        RuleFor(x => x.LoanNumber).MaximumLength(100).When(x => x.LoanNumber is not null);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}

public class UpdateLoanValidator : AbstractValidator<UpdateLoanRequest>
{
    public UpdateLoanValidator()
    {
        RuleFor(x => x.CurrentInterestRate).GreaterThanOrEqualTo(0).LessThanOrEqualTo(100);
        RuleFor(x => x.InterestCalculationType).IsInEnum();
        RuleFor(x => x.PaymentFrequency).IsInEnum();
        RuleFor(x => x.EmiAmount)
            .GreaterThan(0)
            .When(x => x.IsEmiLoan)
            .WithMessage("EMI amount required for EMI loans.");
    }
}

public class UpdateInterestRateValidator : AbstractValidator<UpdateInterestRateRequest>
{
    public UpdateInterestRateValidator()
    {
        RuleFor(x => x.NewRate).GreaterThanOrEqualTo(0).LessThanOrEqualTo(100);
        RuleFor(x => x.EffectiveDate).NotEmpty();
    }
}
