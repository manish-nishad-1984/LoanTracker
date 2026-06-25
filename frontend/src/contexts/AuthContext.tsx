import { createContext, useContext, useState, type ReactNode } from 'react'

/**
 * Frontend-only auth gate (placeholder).
 *
 * The backend currently has no authentication (it was kept optional by design).
 * This context guards the UI with a local credential and persists a flag in
 * localStorage. When a real auth API is added, replace `login()` with a call to
 * POST /api/auth/login and store the returned JWT instead of this boolean.
 */

// Demo credential — change here, or wire to a real backend later.
const DEMO_USERNAME = 'admin'
const DEMO_PASSWORD = 'admin123'

const STORAGE_KEY = 'loantracker.auth'

interface AuthUser {
  username: string
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
    const raw = localStorage.getItem(STORAGE_KEY)
    return raw ? (JSON.parse(raw) as AuthUser) : null
  } catch {
    return null
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(readStoredUser)

  const login: AuthContextValue['login'] = async (username, password) => {
    // Simulate async so swapping in a real fetch later is trivial.
    await new Promise((r) => setTimeout(r, 300))

    if (username.trim() === DEMO_USERNAME && password === DEMO_PASSWORD) {
      const authUser: AuthUser = { username: username.trim() }
      localStorage.setItem(STORAGE_KEY, JSON.stringify(authUser))
      setUser(authUser)
      return { ok: true }
    }
    return { ok: false, error: 'Invalid username or password.' }
  }

  const logout = () => {
    localStorage.removeItem(STORAGE_KEY)
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
