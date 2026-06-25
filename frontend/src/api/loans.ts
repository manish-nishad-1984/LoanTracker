import { apiClient } from '@/lib/api'
import type {
  LoanDto,
  LoanListItemDto,
  CreateLoanRequest,
  UpdateLoanRequest,
  CloseLoanRequest,
  UpdateInterestRateRequest,
  PagedResult,
  LoanStatus,
  LoanDirection,
} from '@/types'

export const loansApi = {
  getAll: (params: {
    lenderId?: string
    status?: LoanStatus
    direction?: LoanDirection
    search?: string
    page?: number
    pageSize?: number
  }) =>
    apiClient
      .get<PagedResult<LoanListItemDto>>('/loans', { params })
      .then((r) => r.data),

  getById: (id: string) =>
    apiClient.get<LoanDto>(`/loans/${id}`).then((r) => r.data),

  create: (data: CreateLoanRequest) =>
    apiClient.post<LoanDto>('/loans', data).then((r) => r.data),

  update: (id: string, data: UpdateLoanRequest) =>
    apiClient.put<LoanDto>(`/loans/${id}`, data).then((r) => r.data),

  close: (id: string, data: CloseLoanRequest) =>
    apiClient.post<LoanDto>(`/loans/${id}/close`, data).then((r) => r.data),

  updateInterestRate: (id: string, data: UpdateInterestRateRequest) =>
    apiClient.post(`/loans/${id}/interest-rate`, data).then((r) => r.data),

  delete: (id: string) =>
    apiClient.delete(`/loans/${id}`).then((r) => r.data),
}
