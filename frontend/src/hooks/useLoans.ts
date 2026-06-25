import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { loansApi } from '@/api/loans'
import type {
  CreateLoanRequest,
  UpdateLoanRequest,
  CloseLoanRequest,
  UpdateInterestRateRequest,
  LoanStatus,
  LoanDirection,
} from '@/types'

const KEYS = {
  all: ['loans'] as const,
  list: (params: object) => ['loans', 'list', params] as const,
  detail: (id: string) => ['loans', 'detail', id] as const,
}

export function useLoans(params: {
  lenderId?: string
  status?: LoanStatus
  direction?: LoanDirection
  search?: string
  page?: number
  pageSize?: number
} = {}) {
  return useQuery({
    queryKey: KEYS.list(params),
    queryFn: () => loansApi.getAll(params),
  })
}

export function useLoan(id: string) {
  return useQuery({
    queryKey: KEYS.detail(id),
    queryFn: () => loansApi.getById(id),
    enabled: !!id,
  })
}

export function useCreateLoan() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateLoanRequest) => loansApi.create(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEYS.all }),
  })
}

export function useUpdateLoan(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: UpdateLoanRequest) => loansApi.update(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: KEYS.all })
      qc.invalidateQueries({ queryKey: KEYS.detail(id) })
    },
  })
}

export function useCloseLoan(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: CloseLoanRequest) => loansApi.close(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: KEYS.all })
      qc.invalidateQueries({ queryKey: KEYS.detail(id) })
      qc.invalidateQueries({ queryKey: ['dashboard'] })
    },
  })
}

export function useUpdateInterestRate(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: UpdateInterestRateRequest) =>
      loansApi.updateInterestRate(id, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEYS.detail(id) }),
  })
}

export function useDeleteLoan() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => loansApi.delete(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEYS.all }),
  })
}
