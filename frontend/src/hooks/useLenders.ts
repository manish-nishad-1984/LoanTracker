import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { lendersApi } from '@/api/lenders'
import type { CreateLenderRequest, UpdateLenderRequest } from '@/types'

const KEYS = {
  all: ['lenders'] as const,
  list: (params: object) => ['lenders', 'list', params] as const,
  detail: (id: string) => ['lenders', 'detail', id] as const,
}

export function useLenders(params: {
  search?: string
  isActive?: boolean
  page?: number
  pageSize?: number
} = {}) {
  return useQuery({
    queryKey: KEYS.list(params),
    queryFn: () => lendersApi.getAll(params),
  })
}

export function useLender(id: string) {
  return useQuery({
    queryKey: KEYS.detail(id),
    queryFn: () => lendersApi.getById(id),
    enabled: !!id,
  })
}

export function useCreateLender() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateLenderRequest) => lendersApi.create(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEYS.all }),
  })
}

export function useUpdateLender(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: UpdateLenderRequest) => lendersApi.update(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: KEYS.all })
      qc.invalidateQueries({ queryKey: KEYS.detail(id) })
    },
  })
}

export function useDeactivateLender() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => lendersApi.deactivate(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEYS.all }),
  })
}

export function useDeleteLender() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => lendersApi.delete(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEYS.all }),
  })
}
