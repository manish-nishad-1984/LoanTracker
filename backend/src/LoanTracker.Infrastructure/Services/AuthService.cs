using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LoanTracker.Application.Common;
using LoanTracker.Application.DTOs.Auth;
using LoanTracker.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace LoanTracker.Infrastructure.Services;

public class AuthService(
    IApplicationDbContext db,
    IPasswordHasher passwordHasher,
    IConfiguration configuration,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username.Trim() && u.DeletedAt == null, ct);

        if (user is null || !user.IsActive || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            logger.LogWarning("Failed login attempt for username {Username}", request.Username);
            return Result<LoginResponse>.Failure("Invalid username or password.", 401);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var (token, expiresAt) = GenerateToken(user.Username, user.DisplayName);
        logger.LogInformation("User {Username} logged in", user.Username);

        return Result<LoginResponse>.Success(new LoginResponse(token, user.Username, user.DisplayName, expiresAt));
    }

    public async Task<Result> ChangePasswordAsync(string username, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username && u.DeletedAt == null, ct);
        if (user is null) return Result.NotFound("User", username);

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return Result.BadRequest("Current password is incorrect.");

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("User {Username} changed password", username);
        return Result.Success();
    }

    public async Task<Result<LoginResponse>> UpdateAccountAsync(
        string currentUsername, UpdateAccountRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == currentUsername && u.DeletedAt == null, ct);
        if (user is null) return Result<LoginResponse>.NotFound("User", currentUsername);

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return Result<LoginResponse>.BadRequest("Current password is incorrect.");

        if (!string.IsNullOrWhiteSpace(request.NewUsername))
        {
            var newName = request.NewUsername.Trim();
            if (!newName.Equals(user.Username, StringComparison.Ordinal))
            {
                var taken = await db.Users.AnyAsync(u => u.Username == newName && u.Id != user.Id, ct);
                if (taken) return Result<LoginResponse>.Conflict($"Username '{newName}' is already taken.");
                user.Username = newName;
            }
        }

        if (request.NewDisplayName is not null)
            user.DisplayName = string.IsNullOrWhiteSpace(request.NewDisplayName) ? null : request.NewDisplayName.Trim();

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
            user.PasswordHash = passwordHasher.Hash(request.NewPassword);

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Account updated for user {Username}", user.Username);

        // Re-issue a token so the new username/identity takes effect immediately.
        var (token, expiresAt) = GenerateToken(user.Username, user.DisplayName);
        return Result<LoginResponse>.Success(new LoginResponse(token, user.Username, user.DisplayName, expiresAt));
    }

    private (string token, DateTime expiresAt) GenerateToken(string username, string? displayName)
    {
        var jwt = configuration.GetSection("Jwt");
        var key = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var issuer = jwt["Issuer"] ?? "LoanTracker";
        var audience = jwt["Audience"] ?? "LoanTracker";
        var expiryMinutes = int.TryParse(jwt["ExpiryMinutes"], out var m) ? m : 480;

        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, username),
            new(ClaimTypes.Name, username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        if (!string.IsNullOrWhiteSpace(displayName))
            claims.Add(new Claim("displayName", displayName));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
