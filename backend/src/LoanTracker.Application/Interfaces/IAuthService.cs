using LoanTracker.Application.Common;
using LoanTracker.Application.DTOs.Auth;

namespace LoanTracker.Application.Interfaces;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<Result> ChangePasswordAsync(string username, ChangePasswordRequest request, CancellationToken ct = default);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
