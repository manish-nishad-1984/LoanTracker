import { apiClient } from '@/lib/api'
import type {
  Expense, ExpenseListItem, ExpenseSummary, CreateExpenseRequest, Attachment, PagedResult,
} from '@/types'

export const EXPENSE_CATEGORIES = [
  'Food', 'Groceries', 'Shopping', 'Fuel', 'Travel', 'Utilities', 'Rent', 'Medical',
  'Education', 'Entertainment', 'Insurance', 'EMI', 'Household', 'Maintenance', 'Furniture',
  'Appliances', 'Clothing', 'Personal Care', 'Kids', 'Gifts', 'Donation', 'Family',
  'Subscriptions', 'Investments', 'Tax', 'Other',
]
export const PAYMENT_METHODS = ['Cash', 'UPI', 'Card', 'Bank Transfer', 'Cheque', 'Other']
export const ATTACHMENT_KINDS = ['Bill', 'Product', 'Invoice', 'Other']

export const expensesApi = {
  getAll: (params: { category?: string; search?: string; from?: string; to?: string; page?: number; pageSize?: number }) =>
    apiClient.get<PagedResult<ExpenseListItem>>('/expenses', { params }).then((r) => r.data),

  getById: (id: string) => apiClient.get<Expense>(`/expenses/${id}`).then((r) => r.data),

  create: (data: CreateExpenseRequest) => apiClient.post<Expense>('/expenses', data).then((r) => r.data),
  update: (id: string, data: CreateExpenseRequest) => apiClient.put<Expense>(`/expenses/${id}`, data).then((r) => r.data),
  delete: (id: string) => apiClient.delete(`/expenses/${id}`).then((r) => r.data),

  summary: (params: { from?: string; to?: string } = {}) =>
    apiClient.get<ExpenseSummary>('/expenses/summary', { params }).then((r) => r.data),

  fromTransaction: (data: { transactionId: string; category: string; vendor?: string; notes?: string; paymentMethod?: string }) =>
    apiClient.post<Expense>('/expenses/from-transaction', data).then((r) => r.data),

  uploadAttachment: (expenseId: string, file: File, kind: string) => {
    const fd = new FormData()
    fd.append('file', file)
    return apiClient
      .post<Attachment>(`/expenses/${expenseId}/attachments?kind=${encodeURIComponent(kind)}`, fd, {
        headers: { 'Content-Type': undefined },
      })
      .then((r) => r.data)
  },

  deleteAttachment: (attachmentId: string) =>
    apiClient.delete(`/expenses/attachments/${attachmentId}`).then((r) => r.data),

  // Fetch an attachment as an object URL (carries the JWT via axios).
  attachmentUrl: (attachmentId: string) =>
    apiClient
      .get(`/expenses/attachments/${attachmentId}`, { responseType: 'blob' })
      .then((r) => URL.createObjectURL(r.data as Blob)),
}
