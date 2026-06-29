using LoanTracker.Application.Common;
using LoanTracker.Application.DTOs.Banking;

namespace LoanTracker.Application.Interfaces;

public interface IBankStatementService
{
    /// <summary>Parse an uploaded bank statement (HDFC .xls/.xlsx) and return a preview — no persistence.</summary>
    Result<BankStatementPreviewDto> ParsePreview(Stream fileStream, string fileName);
}
