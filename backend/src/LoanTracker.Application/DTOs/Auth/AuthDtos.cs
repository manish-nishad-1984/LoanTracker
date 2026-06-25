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
