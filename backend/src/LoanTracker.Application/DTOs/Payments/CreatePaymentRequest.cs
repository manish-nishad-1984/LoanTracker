using FluentValidation;
using LoanTracker.Domain.Enums;

namespace LoanTracker.Application.DTOs.Payments;

public record CreatePaymentRequest(
    Guid LoanId,
    DateOnly PaymentDate,
    decimal TotalAmount,
    decimal PrincipalAmount,
    decimal InterestAmount,
    decimal PenaltyAmount,
    PaymentMode PaymentMode,
    string? ReferenceNumber,
    string? Remarks
);

public record UpdatePaymentRequest(
    DateOnly PaymentDate,
    decimal TotalAmount,
    decimal PrincipalAmount,
    decimal InterestAmount,
    decimal PenaltyAmount,
    PaymentMode PaymentMode,
    string? ReferenceNumber,
    string? Remarks
);

public class CreatePaymentValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentValidator()
    {
        RuleFor(x => x.LoanId).NotEmpty();
        RuleFor(x => x.PaymentDate).NotEmpty();
        RuleFor(x => x.TotalAmount).GreaterThan(0);
        RuleFor(x => x.PrincipalAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.InterestAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PenaltyAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PaymentMode).IsInEnum();
        RuleFor(x => x).Must(x =>
            x.PrincipalAmount + x.InterestAmount + x.PenaltyAmount == x.TotalAmount)
            .WithMessage("PrincipalAmount + InterestAmount + PenaltyAmount must equal TotalAmount.");
        RuleFor(x => x).Must(x =>
            x.PrincipalAmount > 0 || x.InterestAmount > 0 || x.PenaltyAmount > 0)
            .WithMessage("At least one of PrincipalAmount, InterestAmount, or PenaltyAmount must be greater than zero.");
        RuleFor(x => x.ReferenceNumber).MaximumLength(100).When(x => x.ReferenceNumber is not null);
    }
}

public class UpdatePaymentValidator : AbstractValidator<UpdatePaymentRequest>
{
    public UpdatePaymentValidator()
    {
        RuleFor(x => x.PaymentDate).NotEmpty();
        RuleFor(x => x.TotalAmount).GreaterThan(0);
        RuleFor(x => x.PrincipalAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.InterestAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PenaltyAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PaymentMode).IsInEnum();
        RuleFor(x => x).Must(x =>
            x.PrincipalAmount + x.InterestAmount + x.PenaltyAmount == x.TotalAmount)
            .WithMessage("PrincipalAmount + InterestAmount + PenaltyAmount must equal TotalAmount.");
    }
}
