import { apiClient } from '@/lib/api'
import type {
  LenderDto,
  LenderListItemDto,
  CreateLenderRequest,
  UpdateLenderRequest,
  PagedResult,
} from '@/types'

export const lendersApi = {
  getAll: (params: {
    search?: string
    isActive?: boolean
    page?: number
    pageSize?: number
  }) =>
    apiClient
      .get<PagedResult<LenderListItemDto>>('/lenders', { params })
      .then((r) => r.data),

  getById: (id: string) =>
    apiClient.get<LenderDto>(`/lenders/${id}`).then((r) => r.data),

  create: (data: CreateLenderRequest) =>
    apiClient.post<LenderDto>('/lenders', data).then((r) => r.data),

  update: (id: string, data: UpdateLenderRequest) =>
    apiClient.put<LenderDto>(`/lenders/${id}`, data).then((r) => r.data),

  deactivate: (id: string) =>
    apiClient.post(`/lenders/${id}/deactivate`).then((r) => r.data),

  delete: (id: string) =>
    apiClient.delete(`/lenders/${id}`).then((r) => r.data),
}
