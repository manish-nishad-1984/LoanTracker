import { apiClient } from '@/lib/api'
import type {
  PaymentDto,
  PaymentListItemDto,
  CreatePaymentRequest,
  UpdatePaymentRequest,
  PagedResult,
} from '@/types'

export const paymentsApi = {
  getAll: (params: {
    loanId?: string
    lenderId?: string
    from?: string
    to?: string
    page?: number
    pageSize?: number
  }) =>
    apiClient
      .get<PagedResult<PaymentListItemDto>>('/payments', { params })
      .then((r) => r.data),

  getByLoan: (loanId: string, page = 1, pageSize = 50) =>
    apiClient
      .get<PagedResult<PaymentListItemDto>>(`/payments/loan/${loanId}`, {
        params: { page, pageSize },
      })
      .then((r) => r.data),

  getById: (id: string) =>
    apiClient.get<PaymentDto>(`/payments/${id}`).then((r) => r.data),

  create: (data: CreatePaymentRequest) =>
    apiClient.post<PaymentDto>('/payments', data).then((r) => r.data),

  update: (id: string, data: UpdatePaymentRequest) =>
    apiClient.put<PaymentDto>(`/payments/${id}`, data).then((r) => r.data),

  delete: (id: string) =>
    apiClient.delete(`/payments/${id}`).then((r) => r.data),
}
