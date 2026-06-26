import { apiClient } from '@/lib/api'
import type {
  DashboardSummaryDto,
  LenderSummaryDto,
  MonthlyPaymentDto,
  YearlySummaryDto,
  LoanDirection,
} from '@/types'

export const dashboardApi = {
  getSummary: (direction?: LoanDirection) =>
    apiClient
      .get<DashboardSummaryDto>('/dashboard', { params: { direction } })
      .then((r) => r.data),

  getLenderSummaries: (direction?: LoanDirection) =>
    apiClient
      .get<LenderSummaryDto[]>('/dashboard/lenders', { params: { direction } })
      .then((r) => r.data),

  getMonthlyTrend: (months = 24) =>
    apiClient
      .get<MonthlyPaymentDto[]>('/dashboard/monthly', { params: { months } })
      .then((r) => r.data),

  getYearlySummary: (years = 5) =>
    apiClient
      .get<YearlySummaryDto[]>('/dashboard/yearly', { params: { years } })
      .then((r) => r.data),
}
