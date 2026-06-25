using LoanTracker.Application.DTOs.Dashboard;

namespace LoanTracker.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default);
    Task<IReadOnlyList<LenderSummaryDto>> GetLenderSummariesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MonthlyPaymentDto>> GetMonthlyTrendAsync(int months = 24, CancellationToken ct = default);
    Task<IReadOnlyList<YearlySummaryDto>> GetYearlySummaryAsync(int years = 5, CancellationToken ct = default);
}
