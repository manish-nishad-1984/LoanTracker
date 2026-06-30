import { useState } from 'react'
import { useQuery, keepPreviousData } from '@tanstack/react-query'
import { Search, Filter, ArrowDownLeft, ArrowUpRight, ChevronLeft, ChevronRight, Download } from 'lucide-react'
import { bankApi } from '@/api/bank'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { formatCurrency, formatDate } from '@/lib/utils'

const CATEGORIES = [
  'Salary', 'Interest', 'Refund', 'Cash Deposit', 'Investment', 'Transfer In', 'Other Income',
  'Food', 'Groceries', 'Shopping', 'Fuel', 'Travel', 'Utilities', 'Medical', 'Education',
  'Entertainment', 'Insurance', 'EMI', 'Investments', 'Credit Card', 'Cash Withdrawal',
  'Rent', 'Tax', 'Donation', 'Business', 'Bank Charges', 'Transfer', 'UPI', 'Others',
]

export default function BankReport() {
  const [search, setSearch] = useState('')
  const [category, setCategory] = useState('')
  const [direction, setDirection] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [page, setPage] = useState(1)
  const pageSize = 50

  const params = {
    search: search || undefined,
    category: category || undefined,
    direction: direction || undefined,
    from: from || undefined,
    to: to || undefined,
    page,
    pageSize,
  }

  const { data, isLoading } = useQuery({
    queryKey: ['bank', 'transactions', params],
    queryFn: () => bankApi.getTransactions(params),
    placeholderData: keepPreviousData,
  })

  const reset = (fn: (v: string) => void) => (v: string) => { setPage(1); fn(v) }
  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / pageSize)) : 1

  return (
    <div className="space-y-4">
      {/* Filters */}
      <div className="rounded-lg border border-slate-200 bg-slate-50/60 p-3 print:hidden">
        <div className="flex flex-wrap items-center gap-2">
          <div className="flex items-center gap-1.5 text-xs font-medium text-slate-500 mr-1">
            <Filter className="h-3.5 w-3.5" /> Filters
          </div>
          <div className="relative flex-1 min-w-[200px] max-w-xs">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Search name / merchant / narration…"
              value={search}
              onChange={(e) => { setPage(1); setSearch(e.target.value) }}
              className="pl-9 h-9"
            />
          </div>
          <Select value={category} onValueChange={reset(setCategory)}>
            <SelectTrigger className="h-9 w-44"><SelectValue placeholder="All categories" /></SelectTrigger>
            <SelectContent>
              <SelectItem value="">All categories</SelectItem>
              {CATEGORIES.map((c) => <SelectItem key={c} value={c}>{c}</SelectItem>)}
            </SelectContent>
          </Select>
          <Select value={direction} onValueChange={reset(setDirection)}>
            <SelectTrigger className="h-9 w-36"><SelectValue placeholder="In & Out" /></SelectTrigger>
            <SelectContent>
              <SelectItem value="">In &amp; Out</SelectItem>
              <SelectItem value="In">Money In</SelectItem>
              <SelectItem value="Out">Money Out</SelectItem>
            </SelectContent>
          </Select>
          <Input type="date" value={from} onChange={(e) => { setPage(1); setFrom(e.target.value) }} className="h-9 w-36" />
          <span className="text-xs text-slate-400">to</span>
          <Input type="date" value={to} onChange={(e) => { setPage(1); setTo(e.target.value) }} className="h-9 w-36" />
          {(search || category || direction || from || to) && (
            <Button variant="outline" size="sm" className="h-9"
              onClick={() => { setPage(1); setSearch(''); setCategory(''); setDirection(''); setFrom(''); setTo('') }}>
              Clear
            </Button>
          )}
          <Button size="sm" className="h-9 ml-auto" onClick={() => window.print()}>
            <Download className="h-4 w-4" /> PDF
          </Button>
        </div>
      </div>

      {/* Totals for the current filter */}
      <div className="grid grid-cols-3 gap-3">
        <div className="rounded-lg border border-slate-200 bg-white px-4 py-3">
          <div className="flex items-center gap-1.5 text-[11px] uppercase tracking-wider text-slate-500">
            <ArrowDownLeft className="h-3.5 w-3.5 text-emerald-600" /> Money In (filtered)
          </div>
          <p className="text-lg font-semibold text-emerald-700 mt-0.5">{formatCurrency(data?.totalIn ?? 0)}</p>
        </div>
        <div className="rounded-lg border border-slate-200 bg-white px-4 py-3">
          <div className="flex items-center gap-1.5 text-[11px] uppercase tracking-wider text-slate-500">
            <ArrowUpRight className="h-3.5 w-3.5 text-rose-600" /> Money Out (filtered)
          </div>
          <p className="text-lg font-semibold text-rose-700 mt-0.5">{formatCurrency(data?.totalOut ?? 0)}</p>
        </div>
        <div className="rounded-lg border border-slate-200 bg-white px-4 py-3">
          <div className="text-[11px] uppercase tracking-wider text-slate-500">Matching Transactions</div>
          <p className="text-lg font-semibold text-slate-900 mt-0.5">{data?.totalCount ?? 0}</p>
        </div>
      </div>

      {/* Grid */}
      <div className="rounded-lg border border-slate-200 bg-white overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-slate-50 text-left">
                <th className="px-4 py-2.5 font-medium text-slate-500 text-xs uppercase tracking-wider">Date</th>
                <th className="px-4 py-2.5 font-medium text-slate-500 text-xs uppercase tracking-wider">Name / Merchant</th>
                <th className="px-4 py-2.5 font-medium text-slate-500 text-xs uppercase tracking-wider">Narration</th>
                <th className="px-4 py-2.5 font-medium text-slate-500 text-xs uppercase tracking-wider">Category</th>
                <th className="px-4 py-2.5 font-medium text-slate-500 text-xs uppercase tracking-wider">Method</th>
                <th className="px-4 py-2.5 font-medium text-slate-500 text-xs uppercase tracking-wider text-right">Out</th>
                <th className="px-4 py-2.5 font-medium text-slate-500 text-xs uppercase tracking-wider text-right">In</th>
              </tr>
            </thead>
            <tbody>
              {isLoading && (
                <tr><td colSpan={7} className="px-4 py-10 text-center text-slate-400">Loading…</td></tr>
              )}
              {!isLoading && data?.items.length === 0 && (
                <tr><td colSpan={7} className="px-4 py-10 text-center text-slate-400">No transactions match the filters.</td></tr>
              )}
              {data?.items.map((t) => (
                <tr key={t.id} className="border-b border-slate-100 hover:bg-slate-50/60">
                  <td className="px-4 py-2 whitespace-nowrap text-slate-700">{formatDate(t.date)}</td>
                  <td className="px-4 py-2 font-medium text-slate-800 max-w-[180px] truncate" title={t.merchant ?? ''}>{t.merchant ?? '—'}</td>
                  <td className="px-4 py-2 text-slate-500 text-xs max-w-[260px] truncate" title={t.narration}>{t.narration}</td>
                  <td className="px-4 py-2"><Badge variant="outline" className="text-[10px] px-1.5 py-0">{t.category}</Badge></td>
                  <td className="px-4 py-2 text-slate-500 text-xs">{t.paymentMethod}</td>
                  <td className="px-4 py-2 text-right tabular-nums text-rose-700">{t.direction === 'Out' ? formatCurrency(t.amount) : '—'}</td>
                  <td className="px-4 py-2 text-right tabular-nums text-emerald-700">{t.direction === 'In' ? formatCurrency(t.amount) : '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        <div className="flex items-center justify-between border-t border-slate-100 px-4 py-2.5 text-sm print:hidden">
          <span className="text-slate-500">
            Page {data?.page ?? 1} of {totalPages} · {data?.totalCount ?? 0} records
          </span>
          <div className="flex gap-1">
            <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
              <ChevronLeft className="h-4 w-4" /> Prev
            </Button>
            <Button variant="outline" size="sm" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>
              Next <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      </div>
    </div>
  )
}
