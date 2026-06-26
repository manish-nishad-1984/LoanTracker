using LoanTracker.Application.DTOs.Dashboard;
using LoanTracker.Domain.Enums;

namespace LoanTracker.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(LoanDirection? direction = null, CancellationToken ct = default);
    Task<IReadOnlyList<LenderSummaryDto>> GetLenderSummariesAsync(LoanDirection? direction = null, CancellationToken ct = default);
    Task<IReadOnlyList<MonthlyPaymentDto>> GetMonthlyTrendAsync(int months = 24, LoanDirection? direction = null, CancellationToken ct = default);
    Task<IReadOnlyList<YearlySummaryDto>> GetYearlySummaryAsync(int years = 5, CancellationToken ct = default);
}
