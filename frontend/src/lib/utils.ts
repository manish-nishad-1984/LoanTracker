import { type ClassValue, clsx } from 'clsx'
import { twMerge } from 'tailwind-merge'

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

export function formatCurrency(
  amount: number,
  currency = 'INR',
  locale = 'en-IN'
): string {
  return new Intl.NumberFormat(locale, {
    style: 'currency',
    currency,
    maximumFractionDigits: 0,
  }).format(amount)
}

export function formatDate(dateStr: string | null | undefined): string {
  if (!dateStr) return '—'
  return new Date(dateStr).toLocaleDateString('en-IN', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  })
}

export function formatPercent(rate: number): string {
  return `${rate.toFixed(2)}%`
}

export function formatNumber(n: number): string {
  return new Intl.NumberFormat('en-IN').format(n)
}

export function loanStatusColor(status: string): string {
  switch (status) {
    case 'Active': return 'bg-emerald-100 text-emerald-800 border-emerald-200'
    case 'Closed': return 'bg-slate-100 text-slate-700 border-slate-200'
    case 'Defaulted': return 'bg-red-100 text-red-800 border-red-200'
    case 'Restructured': return 'bg-amber-100 text-amber-800 border-amber-200'
    case 'WrittenOff': return 'bg-gray-100 text-gray-600 border-gray-200'
    default: return 'bg-slate-100 text-slate-700 border-slate-200'
  }
}

export function lenderTypeIcon(type: string): string {
  switch (type) {
    case 'Bank': return '🏦'
    case 'Person': return '👤'
    case 'Nbfc': return '🏢'
    case 'Cooperative': return '🤝'
    default: return '💼'
  }
}

export function paymentTypeLabel(principal: number, interest: number): string {
  if (principal > 0 && interest > 0) return 'Mixed'
  if (principal > 0) return 'Principal'
  if (interest > 0) return 'Interest'
  return 'Penalty'
}
