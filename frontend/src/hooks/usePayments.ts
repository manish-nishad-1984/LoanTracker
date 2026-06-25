import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { paymentsApi } from '@/api/payments'
import type { CreatePaymentRequest, UpdatePaymentRequest } from '@/types'

const KEYS = {
  all: ['payments'] as const,
  list: (params: object) => ['payments', 'list', params] as const,
  byLoan: (loanId: string) => ['payments', 'loan', loanId] as const,
  detail: (id: string) => ['payments', 'detail', id] as const,
}

export function usePayments(params: {
  loanId?: string
  lenderId?: string
  from?: string
  to?: string
  page?: number
  pageSize?: number
} = {}) {
  return useQuery({
    queryKey: KEYS.list(params),
    queryFn: () => paymentsApi.getAll(params),
  })
}

export function usePaymentsByLoan(loanId: string) {
  return useQuery({
    queryKey: KEYS.byLoan(loanId),
    queryFn: () => paymentsApi.getByLoan(loanId),
    enabled: !!loanId,
  })
}

export function useCreatePayment() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: CreatePaymentRequest) => paymentsApi.create(data),
    onSuccess: (_, variables) => {
      qc.invalidateQueries({ queryKey: KEYS.all })
      qc.invalidateQueries({ queryKey: KEYS.byLoan(variables.loanId) })
      qc.invalidateQueries({ queryKey: ['loans', 'detail', variables.loanId] })
      qc.invalidateQueries({ queryKey: ['dashboard'] })
    },
  })
}

export function useUpdatePayment(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: UpdatePaymentRequest) => paymentsApi.update(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: KEYS.all })
      qc.invalidateQueries({ queryKey: ['loans'] })
      qc.invalidateQueries({ queryKey: ['dashboard'] })
    },
  })
}

export function useDeletePayment() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => paymentsApi.delete(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: KEYS.all })
      qc.invalidateQueries({ queryKey: ['loans'] })
      qc.invalidateQueries({ queryKey: ['dashboard'] })
    },
  })
}
