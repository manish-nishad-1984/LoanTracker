import { NavLink } from 'react-router-dom'
import {
  LayoutDashboard,
  Users,
  CreditCard,
  BarChart3,
  Landmark,
  TrendingDown,
  ArrowDownCircle,
  ArrowUpCircle,
  Landmark as BankIcon,
  FileUp,
  Table2 as TableIcon,
  Receipt,
  PieChart,
} from 'lucide-react'
import { cn } from '@/lib/utils'

const navItems = [
  { to: '/dashboard', icon: LayoutDashboard, label: 'Dashboard' },
  { to: '/loan-dashboard', icon: ArrowDownCircle, label: 'Loan Dashboard' },
  { to: '/lent-dashboard', icon: ArrowUpCircle, label: 'Lent Dashboard' },
  { to: '/lenders', icon: Users, label: 'Party Master' },
  { to: '/loans', icon: CreditCard, label: 'Loans' },
  { to: '/reports', icon: BarChart3, label: 'Reports' },
  { to: '/bank/dashboard', icon: BankIcon, label: 'Bank Dashboard' },
  { to: '/bank/report', icon: TableIcon, label: 'Bank Report' },
  { to: '/bank/import', icon: FileUp, label: 'Import Statement' },
  { to: '/expenses/dashboard', icon: PieChart, label: 'Expense Dashboard' },
  { to: '/expenses', icon: Receipt, label: 'Expenses' },
]

export default function Sidebar() {
  return (
    <aside className="fixed left-0 top-0 z-30 h-full w-60 flex-col bg-sidebar text-sidebar-foreground hidden lg:flex print:!hidden">
      {/* Brand */}
      <div className="flex h-16 items-center gap-2.5 border-b border-sidebar-border px-6">
        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-sidebar-primary">
          <TrendingDown className="h-5 w-5 text-sidebar-primary-foreground" />
        </div>
        <div>
          <p className="font-semibold text-sm leading-tight">LoanTracker</p>
          <p className="text-[11px] text-sidebar-foreground/50 leading-tight">Personal Finance</p>
        </div>
      </div>

      {/* Nav */}
      <nav className="flex-1 overflow-y-auto py-4 px-3">
        <div className="space-y-1">
          {navItems.map(({ to, icon: Icon, label }) => (
            <NavLink
              key={to}
              to={to}
              className={({ isActive }) =>
                cn(
                  'flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors',
                  isActive
                    ? 'bg-sidebar-primary text-sidebar-primary-foreground'
                    : 'text-sidebar-foreground/70 hover:bg-sidebar-accent hover:text-sidebar-accent-foreground'
                )
              }
            >
              <Icon className="h-4 w-4 shrink-0" />
              {label}
            </NavLink>
          ))}
        </div>
      </nav>

      {/* Footer */}
      <div className="border-t border-sidebar-border p-4">
        <div className="flex items-center gap-2.5">
          <div className="flex h-8 w-8 items-center justify-center rounded-full bg-sidebar-accent">
            <Landmark className="h-4 w-4 text-sidebar-accent-foreground" />
          </div>
          <div>
            <p className="text-xs font-medium">Loan Ledger</p>
            <p className="text-[11px] text-sidebar-foreground/50">v1.0.0</p>
          </div>
        </div>
      </div>
    </aside>
  )
}
