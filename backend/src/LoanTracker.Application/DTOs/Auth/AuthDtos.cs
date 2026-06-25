using FluentValidation;

namespace LoanTracker.Application.DTOs.Auth;

public record LoginRequest(string Username, string Password);

public record LoginResponse(
    string Token,
    string Username,
    string? DisplayName,
    DateTime ExpiresAt
);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record UpdateAccountRequest(
    string CurrentPassword,
    string? NewUsername,
    string? NewDisplayName,
    string? NewPassword
);

public record CurrentUserDto(string Username, string? DisplayName);

public class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class ChangePasswordValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6)
            .WithMessage("New password must be at least 6 characters.");
    }
}

public class UpdateAccountValidator : AbstractValidator<UpdateAccountRequest>
{
    public UpdateAccountValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty()
            .WithMessage("Enter your current password to confirm changes.");
        RuleFor(x => x.NewUsername).MinimumLength(3).MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.NewUsername));
        RuleFor(x => x.NewDisplayName).MaximumLength(200)
            .When(x => x.NewDisplayName is not null);
        RuleFor(x => x.NewPassword).MinimumLength(6)
            .When(x => !string.IsNullOrWhiteSpace(x.NewPassword))
            .WithMessage("New password must be at least 6 characters.");
        RuleFor(x => x).Must(x =>
            !string.IsNullOrWhiteSpace(x.NewUsername) ||
            !string.IsNullOrWhiteSpace(x.NewPassword) ||
            x.NewDisplayName is not null)
            .WithMessage("Provide a new username, display name, or password to update.");
    }
}
