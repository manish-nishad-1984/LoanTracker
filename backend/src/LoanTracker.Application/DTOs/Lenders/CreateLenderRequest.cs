using FluentValidation;
using LoanTracker.Domain.Enums;

namespace LoanTracker.Application.DTOs.Lenders;

public record CreateLenderRequest(
    LenderType LenderType,
    string Name,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    string? BankName,
    string? AccountNumber,
    string? IfscCode,
    string? PanNumber,
    string? Notes
);

public record UpdateLenderRequest(
    LenderType LenderType,
    string Name,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    string? BankName,
    string? AccountNumber,
    string? IfscCode,
    string? PanNumber,
    string? Notes,
    bool IsActive
);

public class CreateLenderValidator : AbstractValidator<CreateLenderRequest>
{
    public CreateLenderValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LenderType).IsInEnum();
        RuleFor(x => x.Phone).MaximumLength(20).When(x => x.Phone is not null);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(200).When(x => x.Email is not null);
        RuleFor(x => x.IfscCode).MaximumLength(20).When(x => x.IfscCode is not null);
        RuleFor(x => x.AccountNumber).MaximumLength(50).When(x => x.AccountNumber is not null);
    }
}

public class UpdateLenderValidator : AbstractValidator<UpdateLenderRequest>
{
    public UpdateLenderValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LenderType).IsInEnum();
        RuleFor(x => x.Phone).MaximumLength(20).When(x => x.Phone is not null);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(200).When(x => x.Email is not null);
    }
}
