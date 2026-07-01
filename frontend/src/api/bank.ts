import { apiClient } from '@/lib/api'
import type {
  BankStatementPreview, ImportResult, BankAccount, BankDashboard, BankTxnList, GroupSummary,
} from '@/types'

export const bankApi = {
  previewStatement: (file: File) => {
    const fd = new FormData()
    fd.append('file', file)
    return apiClient
      .post<BankStatementPreview>('/bank/statements/preview', fd, { headers: { 'Content-Type': undefined } })
      .then((r) => r.data)
  },

  importStatement: (file: File) => {
    const fd = new FormData()
    fd.append('file', file)
    return apiClient
      .post<ImportResult>('/bank/statements/import', fd, { headers: { 'Content-Type': undefined } })
      .then((r) => r.data)
  },

  getAccounts: () => apiClient.get<BankAccount[]>('/bank/accounts').then((r) => r.data),

  getDashboard: (params: { accountId?: string; from?: string; to?: string } = {}) =>
    apiClient.get<BankDashboard>('/bank/dashboard', { params }).then((r) => r.data),

  getTransactions: (params: {
    accountId?: string; category?: string; direction?: string; search?: string
    merchant?: string; paymentMethod?: string
    from?: string; to?: string; page?: number; pageSize?: number
  }) => apiClient.get<BankTxnList>('/bank/transactions', { params }).then((r) => r.data),

  getSummary: (params: {
    groupBy: string; accountId?: string; category?: string; direction?: string
    search?: string; from?: string; to?: string
  }) => apiClient.get<GroupSummary[]>('/bank/summary', { params }).then((r) => r.data),

  updateCategory: (id: string, category: string) =>
    apiClient.put(`/bank/transactions/${id}/category`, { category }).then((r) => r.data),
}
