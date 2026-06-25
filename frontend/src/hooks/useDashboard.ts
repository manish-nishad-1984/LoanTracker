import { useQuery } from '@tanstack/react-query'
import { dashboardApi } from '@/api/dashboard'

const KEYS = {
  summary: ['dashboard', 'summary'] as const,
  lenders: ['dashboard', 'lenders'] as const,
  monthly: (months: number) => ['dashboard', 'monthly', months] as const,
  yearly: (years: number) => ['dashboard', 'yearly', years] as const,
}

export function useDashboard() {
  return useQuery({
    queryKey: KEYS.summary,
    queryFn: () => dashboardApi.getSummary(),
    staleTime: 60_000,
  })
}

export function useDashboardLenders() {
  return useQuery({
    queryKey: KEYS.lenders,
    queryFn: () => dashboardApi.getLenderSummaries(),
  })
}

export function useMonthlyTrend(months = 12) {
  return useQuery({
    queryKey: KEYS.monthly(months),
    queryFn: () => dashboardApi.getMonthlyTrend(months),
  })
}

export function useYearlySummary(years = 5) {
  return useQuery({
    queryKey: KEYS.yearly(years),
    queryFn: () => dashboardApi.getYearlySummary(years),
  })
}
