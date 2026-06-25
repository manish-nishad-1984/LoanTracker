import { apiClient } from '@/lib/api'
import type {
  DashboardSummaryDto,
  LenderSummaryDto,
  MonthlyPaymentDto,
  YearlySummaryDto,
} from '@/types'

export const dashboardApi = {
  getSummary: () =>
    apiClient.get<DashboardSummaryDto>('/dashboard').then((r) => r.data),

  getLenderSummaries: () =>
    apiClient
      .get<LenderSummaryDto[]>('/dashboard/lenders')
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
