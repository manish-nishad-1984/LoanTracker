import { apiClient } from '@/lib/api'

export interface LoginResponse {
  token: string
  username: string
  displayName: string | null
  expiresAt: string
}

export interface CurrentUser {
  username: string
  displayName: string | null
}

export const authApi = {
  login: (username: string, password: string) =>
    apiClient
      .post<LoginResponse>('/auth/login', { username, password })
      .then((r) => r.data),

  me: () => apiClient.get<CurrentUser>('/auth/me').then((r) => r.data),

  changePassword: (currentPassword: string, newPassword: string) =>
    apiClient
      .post('/auth/change-password', { currentPassword, newPassword })
      .then((r) => r.data),
}
