import { apiClient } from '@/lib/api'
import type { BankStatementPreview } from '@/types'

export const bankApi = {
  previewStatement: (file: File) => {
    const fd = new FormData()
    fd.append('file', file)
    return apiClient
      .post<BankStatementPreview>('/bank/statements/preview', fd, {
        // Let the browser set multipart/form-data with the correct boundary.
        headers: { 'Content-Type': undefined },
      })
      .then((r) => r.data)
  },
}
