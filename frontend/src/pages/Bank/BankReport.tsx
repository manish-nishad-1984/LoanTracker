import { useState } from 'react'
import { useQuery, keepPreviousData } from '@tanstack/react-query'
import {
  Search, Filter, ArrowDownLeft, ArrowUpRight, ChevronLeft, ChevronRight,
  ChevronDown, Download, Layers, ReceiptText,
} from 'lucide-react'
import { bankApi } from '@/api/bank'
import { expensesApi, EXPENSE_CATEGORIES } from '@/api/expenses'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { useToast } from '@/hooks/useToast'
import { formatCurrency, formatDate } from '@/lib/utils'
import type { GroupSummary, TxnLine } from '@/types'

const CATEGORIES = [
  'Salary', 'Interest', 'Refund', 'Cash Deposit', 'Investment', 'Transfer In', 'Other Income',
  'Food', 'Groceries', 'Shopping', 'Fuel', 'Travel', 'Utilities', 'Medical', 'Education',
  'Entertainment', 'Insurance', 'EMI', 'Investments', 'Credit Card', 'Cash Withdrawal',
  'Rent', 'Tax', 'Donation', 'Business', 'Bank Charges', 'Transfer', 'UPI', 'Others',
]
const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

function monthRange(key: string) {
  const [m, y] = key.split(' ')
  const mi = MONTHS.indexOf(m)
  const mm = String(mi + 1).padStart(2, '0')
  const last = new Date(Number(y), mi + 1, 0).getDate()
  return { from: `${y}-${mm}-01`, to: `${y}-${mm}-${String(last).padStart(2, '0')}` }
}

export default function BankReport() {
  const [search, setSearch] = useState('')
  const [category, setCategory] = useState('')
  const [direction, setDirection] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [groupBy, setGroupBy] = useState('')
  const [page, setPage] = useState(1)
  const [convertTxn, setConvertTxn] = useState<TxnLine | null>(null)
  const pageSize = 50

  const base = {
    search: search || undefined,
    category: category || undefined,
    direction: direction || undefined,
    from: from || undefined,
    to: to || undefined,
  }
  const resetPage = () => setPage(1)

  return (
    <div className="space-y-4">
      {/* Filters */}
      <div className="rounded-lg border border-slate-200 bg-slate-50/60 p-3 print:hidden">
        <div className="flex flex-wrap items-center gap-2">
          <div className="flex items-center gap-1.5 text-xs font-medium text-slate-500 mr-1">
            <Filter className="h-3.5 w-3.5" /> Filters
          </div>
          <div className="relative flex-1 min-w-[180px] max-w-xs">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input placeholder="Search name / merchant / narration…" value={search}
              onChange={(e) => { resetPage(); setSearch(e.target.value) }} className="pl-9 h-9" />
          </div>
          <Select value={category} onValueChange={(v) => { resetPage(); setCategory(v) }}>
            <SelectTrigger className="h-9 w-40"><SelectValue placeholder="All categories" /></SelectTrigger>
            <SelectContent>
              <SelectItem value="">All categories</SelectItem>
              {CATEGORIES.map((c) => <SelectItem key={c} value={c}>{c}</SelectItem>)}
            </SelectContent>
          </Select>
          <Select value={direction} onValueChange={(v) => { resetPage(); setDirection(v) }}>
            <SelectTrigger className="h-9 w-32"><SelectValue placeholder="In & Out" /></SelectTrigger>
            <SelectContent>
              <SelectItem value="">In &amp; Out</SelectItem>
              <SelectItem value="In">Money In</SelectItem>
              <SelectItem value="Out">Money Out</SelectItem>
            </SelectContent>
          </Select>
          <Input type="date" value={from} onChange={(e) => { resetPage(); setFrom(e.target.value) }} className="h-9 w-36" />
          <span className="text-xs text-slate-400">to</span>
          <Input type="date" value={to} onChange={(e) => { resetPage(); setTo(e.target.value) }} className="h-9 w-36" />
          <Select value={groupBy} onValueChange={setGroupBy}>
            <SelectTrigger className="h-9 w-40"><Layers className="h-3.5 w-3.5 mr-1 text-slate-400" /><SelectValue placeholder="No grouping" /></SelectTrigger>
            <SelectContent>
              <SelectItem value="">No grouping (detail)</SelectItem>
              <SelectItem value="category">Group by Category</SelectItem>
              <SelectItem value="merchant">Group by Merchant / Name</SelectItem>
              <SelectItem value="paymentMethod">Group by Payment Method</SelectItem>
              <SelectItem value="month">Group by Month</SelectItem>
            </SelectContent>
          </Select>
          {(search || category || direction || from || to || groupBy) && (
            <Button variant="outline" size="sm" className="h-9"
              onClick={() => { resetPage(); setSearch(''); setCategory(''); setDirection(''); setFrom(''); setTo(''); setGroupBy('') }}>
              Clear
            </Button>
          )}
          <Button size="sm" className="h-9 ml-auto" onClick={() => window.print()}>
            <Download className="h-4 w-4" /> PDF
          </Button>
        </div>
      </div>

      {groupBy
        ? <GroupedView groupBy={groupBy} base={base} onAddExpense={setConvertTxn} />
        : <DetailView base={base} page={page} pageSize={pageSize} setPage={setPage} onAddExpense={setConvertTxn} />}

      <Dialog open={!!convertTxn} onOpenChange={(o) => { if (!o) setConvertTxn(null) }}>
        <DialogContent className="max-w-md">
          <DialogHeader><DialogTitle>Save as Expense</DialogTitle></DialogHeader>
          {convertTxn && <ConvertToExpense txn={convertTxn} onDone={() => setConvertTxn(null)} />}
        </DialogContent>
      </Dialog>
    </div>
  )
}

// ─────────── Convert a bank debit into an expense ───────────
function ConvertToExpense({ txn, onDone }: { txn: TxnLine; onDone: () => void }) {
  const { toast } = useToast()
  const guess = EXPENSE_CATEGORIES.includes(txn.category) ? txn.category : 'Other'
  const [category, setCategory] = useState(guess)
  const [vendor, setVendor] = useState(txn.merchant ?? '')
  const [notes, setNotes] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const save = async () => {
    setSaving(true); setError(null)
    try {
      await expensesApi.fromTransaction({ transactionId: txn.id, category, vendor: vendor || undefined, notes: notes || undefined })
      toast({ title: 'Added to expenses' })
      onDone()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed')
    } finally { setSaving(false) }
  }

  return (
    <div className="space-y-3">
      <div className="rounded-md bg-slate-50 border px-3 py-2 text-sm">
        <div className="flex justify-between"><span className="text-slate-500">Date</span><span>{formatDate(txn.date)}</span></div>
        <div className="flex justify-between"><span className="text-slate-500">Amount</span><span className="font-semibold text-rose-700">{formatCurrency(txn.amount)}</span></div>
        <div className="flex justify-between"><span className="text-slate-500">From bank</span><span className="max-w-[200px] truncate">{txn.narration}</span></div>
      </div>
      <div className="grid gap-1">
        <Label>Category</Label>
        <Select value={category} onValueChange={setCategory}>
          <SelectTrigger><SelectValue /></SelectTrigger>
          <SelectContent className="max-h-64">{EXPENSE_CATEGORIES.map((c) => <SelectItem key={c} value={c}>{c}</SelectItem>)}</SelectContent>
        </Select>
      </div>
      <div className="grid gap-1">
        <Label>Vendor / Party</Label>
        <Input value={vendor} onChange={(e) => setVendor(e.target.value)} placeholder="Who you paid" />
      </div>
      <div className="grid gap-1">
        <Label>Notes</Label>
        <Input value={notes} onChange={(e) => setNotes(e.target.value)} placeholder="Optional" />
      </div>
      {error && <p className="text-xs text-destructive">{error}</p>}
      <div className="flex justify-end gap-2 pt-1">
        <Button variant="outline" onClick={onDone} disabled={saving}>Cancel</Button>
        <Button onClick={save} disabled={saving}>{saving ? 'Saving…' : 'Confirm & Add'}</Button>
      </div>
    </div>
  )
}

// ─────────── Ungrouped detail grid ───────────
function DetailView({ base, page, pageSize, setPage, onAddExpense }: {
  base: Record<string, string | undefined>; page: number; pageSize: number
  setPage: (f: (p: number) => number) => void; onAddExpense: (t: TxnLine) => void
}) {
  const params = { ...base, page, pageSize }
  const { data, isLoading } = useQuery({
    queryKey: ['bank', 'txns', params],
    queryFn: () => bankApi.getTransactions(params),
    placeholderData: keepPreviousData,
  })
  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / pageSize)) : 1

  return (
    <>
      <TotalsTiles inAmt={data?.totalIn ?? 0} outAmt={data?.totalOut ?? 0} count={data?.totalCount ?? 0} />
      <div className="rounded-lg border border-slate-200 bg-white overflow-hidden">
        <div className="overflow-x-auto">
          <TxnTable rows={data?.items ?? []} loading={isLoading} onAddExpense={onAddExpense} />
        </div>
        <div className="flex items-center justify-between border-t border-slate-100 px-4 py-2.5 text-sm print:hidden">
          <span className="text-slate-500">Page {data?.page ?? 1} of {totalPages} · {data?.totalCount ?? 0} records</span>
          <div className="flex gap-1">
            <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}><ChevronLeft className="h-4 w-4" /> Prev</Button>
            <Button variant="outline" size="sm" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>Next <ChevronRight className="h-4 w-4" /></Button>
          </div>
        </div>
      </div>
    </>
  )
}

// ─────────── Grouped summary + drill-down ───────────
function GroupedView({ groupBy, base, onAddExpense }: { groupBy: string; base: Record<string, string | undefined>; onAddExpense: (t: TxnLine) => void }) {
  const { data: groups, isLoading } = useQuery({
    queryKey: ['bank', 'summary', groupBy, base],
    queryFn: () => bankApi.getSummary({ groupBy, ...base }),
  })

  const totIn = (groups ?? []).reduce((s, g) => s + g.totalIn, 0)
  const totOut = (groups ?? []).reduce((s, g) => s + g.totalOut, 0)
  const totCount = (groups ?? []).reduce((s, g) => s + g.count, 0)
  const label = groupBy === 'merchant' ? 'Merchant / Name' : groupBy === 'paymentMethod' ? 'Payment Method' : groupBy === 'month' ? 'Month' : 'Category'

  return (
    <>
      <TotalsTiles inAmt={totIn} outAmt={totOut} count={totCount} />
      <div className="rounded-lg border border-slate-200 bg-white overflow-hidden">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-500 text-xs uppercase tracking-wider">{label}</th>
              <th className="px-4 py-2.5 font-medium text-slate-500 text-xs uppercase tracking-wider text-right">Txns</th>
              <th className="px-4 py-2.5 font-medium text-slate-500 text-xs uppercase tracking-wider text-right">In</th>
              <th className="px-4 py-2.5 font-medium text-slate-500 text-xs uppercase tracking-wider text-right">Out</th>
              <th className="px-4 py-2.5 font-medium text-slate-500 text-xs uppercase tracking-wider text-right">Net</th>
              <th className="px-4 py-2.5 w-24" />
            </tr>
          </thead>
          <tbody>
            {isLoading && <tr><td colSpan={6} className="px-4 py-10 text-center text-slate-400">Loading…</td></tr>}
            {!isLoading && groups?.length === 0 && <tr><td colSpan={6} className="px-4 py-10 text-center text-slate-400">No data.</td></tr>}
            {groups?.map((g) => <GroupRow key={g.key} group={g} groupBy={groupBy} base={base} onAddExpense={onAddExpense} />)}
          </tbody>
          {groups && groups.length > 0 && (
            <tfoot>
              <tr className="border-t-2 bg-slate-50 font-semibold">
                <td className="px-4 py-2.5">Total</td>
                <td className="px-4 py-2.5 text-right">{totCount}</td>
                <td className="px-4 py-2.5 text-right text-emerald-700">{formatCurrency(totIn)}</td>
                <td className="px-4 py-2.5 text-right text-rose-700">{formatCurrency(totOut)}</td>
                <td className="px-4 py-2.5 text-right">{formatCurrency(totIn - totOut)}</td>
                <td />
              </tr>
            </tfoot>
          )}
        </table>
      </div>
    </>
  )
}

function GroupRow({ group, groupBy, base, onAddExpense }: { group: GroupSummary; groupBy: string; base: Record<string, string | undefined>; onAddExpense: (t: TxnLine) => void }) {
  const [open, setOpen] = useState(false)

  // Build the detail filter for this group on top of the global filters.
  const detailParams: Record<string, string | number | undefined> = { ...base, pageSize: 500 }
  if (groupBy === 'category') detailParams.category = group.key
  else if (groupBy === 'merchant') detailParams.merchant = group.key === '(Unknown)' ? undefined : group.key
  else if (groupBy === 'paymentMethod') detailParams.paymentMethod = group.key
  else if (groupBy === 'month') { const r = monthRange(group.key); detailParams.from = r.from; detailParams.to = r.to }

  const { data, isLoading } = useQuery({
    queryKey: ['bank', 'txns', 'group', groupBy, group.key, base],
    queryFn: () => bankApi.getTransactions(detailParams),
    enabled: open,
  })

  return (
    <>
      <tr className="border-b border-slate-100 hover:bg-slate-50/60">
        <td className="px-4 py-2.5 font-medium text-slate-800">{group.key}</td>
        <td className="px-4 py-2.5 text-right text-slate-600">{group.count}</td>
        <td className="px-4 py-2.5 text-right tabular-nums text-emerald-700">{group.totalIn > 0 ? formatCurrency(group.totalIn) : '—'}</td>
        <td className="px-4 py-2.5 text-right tabular-nums text-rose-700">{group.totalOut > 0 ? formatCurrency(group.totalOut) : '—'}</td>
        <td className={`px-4 py-2.5 text-right tabular-nums ${group.net >= 0 ? 'text-slate-700' : 'text-rose-700'}`}>{formatCurrency(group.net)}</td>
        <td className="px-4 py-2.5 text-right print:hidden">
          <Button variant="outline" size="sm" className="h-7" onClick={() => setOpen((o) => !o)}>
            <ChevronDown className={`h-3.5 w-3.5 transition-transform ${open ? 'rotate-180' : ''}`} /> Detail
          </Button>
        </td>
      </tr>
      {open && (
        <tr className="bg-slate-50/40">
          <td colSpan={6} className="px-4 py-3">
            <div className="rounded-md border border-slate-200 bg-white overflow-x-auto">
              <TxnTable rows={data?.items ?? []} loading={isLoading} compact onAddExpense={onAddExpense} />
            </div>
          </td>
        </tr>
      )}
    </>
  )
}

// ─────────── shared bits ───────────
function TotalsTiles({ inAmt, outAmt, count }: { inAmt: number; outAmt: number; count: number }) {
  return (
    <div className="grid grid-cols-3 gap-3">
      <div className="rounded-lg border border-slate-200 bg-white px-4 py-3">
        <div className="flex items-center gap-1.5 text-[11px] uppercase tracking-wider text-slate-500"><ArrowDownLeft className="h-3.5 w-3.5 text-emerald-600" /> Money In (filtered)</div>
        <p className="text-lg font-semibold text-emerald-700 mt-0.5">{formatCurrency(inAmt)}</p>
      </div>
      <div className="rounded-lg border border-slate-200 bg-white px-4 py-3">
        <div className="flex items-center gap-1.5 text-[11px] uppercase tracking-wider text-slate-500"><ArrowUpRight className="h-3.5 w-3.5 text-rose-600" /> Money Out (filtered)</div>
        <p className="text-lg font-semibold text-rose-700 mt-0.5">{formatCurrency(outAmt)}</p>
      </div>
      <div className="rounded-lg border border-slate-200 bg-white px-4 py-3">
        <div className="text-[11px] uppercase tracking-wider text-slate-500">Matching Transactions</div>
        <p className="text-lg font-semibold text-slate-900 mt-0.5">{count}</p>
      </div>
    </div>
  )
}

function TxnTable({ rows, loading, compact, onAddExpense }: {
  rows: TxnLine[]; loading: boolean; compact?: boolean; onAddExpense?: (t: TxnLine) => void
}) {
  const cols = (compact ? 6 : 7) + (onAddExpense ? 1 : 0)
  return (
    <table className="w-full text-sm">
      <thead>
        <tr className="border-b bg-slate-50 text-left">
          <th className="px-4 py-2 font-medium text-slate-500 text-xs uppercase tracking-wider">Date</th>
          <th className="px-4 py-2 font-medium text-slate-500 text-xs uppercase tracking-wider">Name / Merchant</th>
          {!compact && <th className="px-4 py-2 font-medium text-slate-500 text-xs uppercase tracking-wider">Narration</th>}
          <th className="px-4 py-2 font-medium text-slate-500 text-xs uppercase tracking-wider">Category</th>
          <th className="px-4 py-2 font-medium text-slate-500 text-xs uppercase tracking-wider">Method</th>
          <th className="px-4 py-2 font-medium text-slate-500 text-xs uppercase tracking-wider text-right">Out</th>
          <th className="px-4 py-2 font-medium text-slate-500 text-xs uppercase tracking-wider text-right">In</th>
          {onAddExpense && <th className="px-4 py-2 print:hidden" />}
        </tr>
      </thead>
      <tbody>
        {loading && <tr><td colSpan={cols} className="px-4 py-8 text-center text-slate-400">Loading…</td></tr>}
        {!loading && rows.length === 0 && <tr><td colSpan={cols} className="px-4 py-8 text-center text-slate-400">No transactions.</td></tr>}
        {rows.map((t) => (
          <tr key={t.id} className="border-b border-slate-100 hover:bg-slate-50/60">
            <td className="px-4 py-2 whitespace-nowrap text-slate-700">{formatDate(t.date)}</td>
            <td className="px-4 py-2 font-medium text-slate-800 max-w-[180px] truncate" title={t.merchant ?? ''}>{t.merchant ?? '—'}</td>
            {!compact && <td className="px-4 py-2 text-slate-500 text-xs max-w-[240px] truncate" title={t.narration}>{t.narration}</td>}
            <td className="px-4 py-2"><Badge variant="outline" className="text-[10px] px-1.5 py-0">{t.category}</Badge></td>
            <td className="px-4 py-2 text-slate-500 text-xs">{t.paymentMethod}</td>
            <td className="px-4 py-2 text-right tabular-nums text-rose-700">{t.direction === 'Out' ? formatCurrency(t.amount) : '—'}</td>
            <td className="px-4 py-2 text-right tabular-nums text-emerald-700">{t.direction === 'In' ? formatCurrency(t.amount) : '—'}</td>
            {onAddExpense && (
              <td className="px-3 py-2 text-right print:hidden">
                {t.direction === 'Out' && (
                  <Button variant="ghost" size="sm" className="h-7 text-blue-700" title="Save as expense" onClick={() => onAddExpense(t)}>
                    <ReceiptText className="h-3.5 w-3.5" /> Expense
                  </Button>
                )}
              </td>
            )}
          </tr>
        ))}
      </tbody>
    </table>
  )
}
