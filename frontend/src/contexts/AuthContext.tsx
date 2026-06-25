import { createContext, useContext, useState, type ReactNode } from 'react'
import { authApi } from '@/api/auth'
import { TOKEN_KEY } from '@/lib/api'

/**
 * Real authentication backed by the API (JWT).
 * The token is stored in localStorage and attached to requests by the axios
 * interceptor in lib/api.ts. A 401 from the API clears the session.
 */

const USER_KEY = 'loantracker.user'

interface AuthUser {
  username: string
  displayName: string | null
}

interface AuthContextValue {
  user: AuthUser | null
  isAuthenticated: boolean
  login: (username: string, password: string) => Promise<{ ok: boolean; error?: string }>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

function readStoredUser(): AuthUser | null {
  try {
    const token = localStorage.getItem(TOKEN_KEY)
    const raw = localStorage.getItem(USER_KEY)
    return token && raw ? (JSON.parse(raw) as AuthUser) : null
  } catch {
    return null
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(readStoredUser)

  const login: AuthContextValue['login'] = async (username, password) => {
    try {
      const res = await authApi.login(username, password)
      const authUser: AuthUser = { username: res.username, displayName: res.displayName }
      localStorage.setItem(TOKEN_KEY, res.token)
      localStorage.setItem(USER_KEY, JSON.stringify(authUser))
      setUser(authUser)
      return { ok: true }
    } catch (e) {
      return { ok: false, error: e instanceof Error ? e.message : 'Login failed.' }
    }
  }

  const logout = () => {
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(USER_KEY)
    setUser(null)
  }

  return (
    <AuthContext.Provider value={{ user, isAuthenticated: !!user, login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within an AuthProvider')
  return ctx
}
