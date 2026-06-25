using LoanTracker.Domain.Enums;

namespace LoanTracker.Application.DTOs.Lenders;

public record LenderDto(
    Guid Id,
    LenderType LenderType,
    string LenderTypeName,
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
    bool IsActive,
    DateTime CreatedAt,
    // Computed
    int TotalLoans,
    int ActiveLoans,
    decimal TotalBorrowed,
    decimal TotalOutstanding
);

public record LenderListItemDto(
    Guid Id,
    LenderType LenderType,
    string LenderTypeName,
    string Name,
    string? Phone,
    string? Email,
    bool IsActive,
    int ActiveLoans,
    decimal TotalOutstanding,
    DateTime CreatedAt
);
