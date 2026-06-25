using LoanTracker.Domain.Enums;

namespace LoanTracker.Application.DTOs.Payments;

public record PaymentDto(
    Guid Id,
    Guid LoanId,
    string LoanDescription,
    string LenderName,
    DateOnly PaymentDate,
    decimal TotalAmount,
    decimal PrincipalAmount,
    decimal InterestAmount,
    decimal PenaltyAmount,
    PaymentMode PaymentMode,
    string PaymentModeName,
    string? ReferenceNumber,
    string? Remarks,
    DateTime CreatedAt
);

public record PaymentListItemDto(
    Guid Id,
    Guid LoanId,
    string LoanDescription,
    string LenderName,
    DateOnly PaymentDate,
    decimal TotalAmount,
    decimal PrincipalAmount,
    decimal InterestAmount,
    decimal PenaltyAmount,
    PaymentMode PaymentMode,
    string PaymentModeName,
    string? ReferenceNumber,
    string? Remarks,
    DateTime CreatedAt
);
