import axios from 'axios'

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api'

export const TOKEN_KEY = 'loantracker.token'

export const apiClient = axios.create({
  baseURL: BASE_URL,
  headers: { 'Content-Type': 'application/json' },
})

// Attach the JWT to every request if present.
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem(TOKEN_KEY)
  if (token) {
    config.headers = config.headers ?? {}
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    // On 401, clear the session and bounce to login (unless this *is* the login call).
    const status = error.response?.status
    const url: string = error.config?.url ?? ''
    if (status === 401 && !url.includes('/auth/login')) {
      localStorage.removeItem(TOKEN_KEY)
      if (window.location.pathname !== '/login') {
        window.location.href = '/login'
      }
    }

    const message =
      error.response?.data?.detail ??
      error.response?.data?.title ??
      (typeof error.response?.data === 'string' ? error.response.data : null) ??
      error.message ??
      'An unexpected error occurred.'
    return Promise.reject(new Error(message))
  }
)
