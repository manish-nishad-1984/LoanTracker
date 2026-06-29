import { useLocation, useNavigate } from 'react-router-dom'
import { Bell, LogOut, UserCircle } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { useAuth } from '@/contexts/AuthContext'

const PAGE_TITLES: Record<string, string> = {
  '/dashboard': 'Dashboard',
  '/loan-dashboard': 'Loan Dashboard',
  '/lent-dashboard': 'Lent Dashboard',
  '/lenders': 'Party Master',
  '/loans': 'Loans',
  '/reports': 'Reports',
  '/bank': 'Bank Statement',
  '/account': 'Account Settings',
}

export default function Header() {
  const { pathname } = useLocation()
  const navigate = useNavigate()
  const { user, logout } = useAuth()
  const base = '/' + pathname.split('/')[1]
  const title = PAGE_TITLES[base] ?? 'LoanTracker'

  const handleLogout = () => {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <header className="sticky top-0 z-20 flex h-16 items-center justify-between border-b bg-background/95 backdrop-blur px-6">
      <h1 className="text-lg font-semibold">{title}</h1>
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="icon" className="relative">
          <Bell className="h-4 w-4" />
        </Button>
        {user && (
          <div className="flex items-center gap-1 border-l pl-3">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => navigate('/account')}
              className="text-muted-foreground"
              title="Account settings"
            >
              <UserCircle className="h-5 w-5" />
              <span className="text-sm font-medium hidden sm:inline">{user.displayName || user.username}</span>
            </Button>
            <Button variant="ghost" size="sm" onClick={handleLogout} className="text-muted-foreground">
              <LogOut className="h-4 w-4" />
              <span className="hidden sm:inline">Logout</span>
            </Button>
          </div>
        )}
      </div>
    </header>
  )
}
