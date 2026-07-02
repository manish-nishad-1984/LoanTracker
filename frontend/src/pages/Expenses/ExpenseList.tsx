import { useState } from 'react'
import { useQuery, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import {
  Plus, Search, Filter, Receipt, Paperclip, Pencil, Trash2, ChevronLeft, ChevronRight, Landmark,
} from 'lucide-react'
import { expensesApi, EXPENSE_CATEGORIES } from '@/api/expenses'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import EmptyState from '@/components/common/EmptyState'
import ExpenseForm from './ExpenseForm'
import { formatCurrency, formatDate } from '@/lib/utils'
import { useToast } from '@/hooks/useToast'
import type { Expense } from '@/types'

export default function ExpenseList() {
  const qc = useQueryClient()
  const { toast } = useToast()
  const [search, setSearch] = useState('')
  const [category, setCategory] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [page, setPage] = useState(1)
  const [showForm, setShowForm] = useState(false)
  const [editing, setEditing] = useState<Expense | null>(null)
  const pageSize = 30

  const params = { search: search || undefined, category: category || undefined, from: from || undefined, to: to || undefined, page, pageSize }
  const { data, isLoading } = useQuery({
    queryKey: ['expenses', 'list', params],
    queryFn: () => expensesApi.getAll(params),
    placeholderData: keepPreviousData,
  })

  const openEdit = async (id: string) => {
    const full = await expensesApi.getById(id)
    setEditing(full); setShowForm(true)
  }
  const remove = async (id: string) => {
    if (!confirm('Delete this expense?')) return
    await expensesApi.delete(id)
    qc.invalidateQueries({ queryKey: ['expenses'] })
    toast({ title: 'Expense deleted' })
  }
  const closeForm = () => { setShowForm(false); setEditing(null) }
  const items = data?.items ?? []
  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / pageSize)) : 1

  return (
    <div className="space-y-4">
      {/* Toolbar */}
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex flex-wrap gap-2 flex-1">
          <div className="relative flex-1 min-w-[180px] max-w-xs">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input placeholder="Search title / vendor…" value={search} onChange={(e) => { setPage(1); setSearch(e.target.value) }} className="pl-9" />
          </div>
          <Select value={category} onValueChange={(v) => { setPage(1); setCategory(v) }}>
            <SelectTrigger className="w-40"><Filter className="h-4 w-4 mr-1.5 text-muted-foreground" /><SelectValue placeholder="All categories" /></SelectTrigger>
            <SelectContent className="max-h-64">
              <SelectItem value="">All categories</SelectItem>
              {EXPENSE_CATEGORIES.map((c) => <SelectItem key={c} value={c}>{c}</SelectItem>)}
            </SelectContent>
          </Select>
          <Input type="date" value={from} onChange={(e) => { setPage(1); setFrom(e.target.value) }} className="w-36" />
          <Input type="date" value={to} onChange={(e) => { setPage(1); setTo(e.target.value) }} className="w-36" />
        </div>
        <Button onClick={() => { setEditing(null); setShowForm(true) }}><Plus className="h-4 w-4" /> Add Expense</Button>
      </div>

      {isLoading ? (
        <div className="space-y-2">{Array.from({ length: 5 }).map((_, i) => <div key={i} className="h-14 rounded-lg bg-muted animate-pulse" />)}</div>
      ) : items.length === 0 ? (
        <EmptyState icon={Receipt} title="No expenses yet" description="Add your home & personal expenses, attach bills or product photos, and track by category."
          actionLabel="Add Expense" onAction={() => setShowForm(true)} />
      ) : (
        <div className="rounded-lg border bg-white overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-slate-50 text-left">
                  <th className="px-4 py-2.5 font-medium text-slate-500 text-xs uppercase tracking-wider">Date</th>
                  <th className="px-4 py-2.5 font-medium text-slate-500 text-xs uppercase tracking-wider">Title</th>
                  <th className="px-4 py-2.5 font-medium text-slate-500 text-xs uppercase tracking-wider">Category</th>
                  <th className="px-4 py-2.5 font-medium text-slate-500 text-xs uppercase tracking-wider">Vendor</th>
                  <th className="px-4 py-2.5 font-medium text-slate-500 text-xs uppercase tracking-wider">Method</th>
                  <th className="px-4 py-2.5 font-medium text-slate-500 text-xs uppercase tracking-wider text-right">Amount</th>
                  <th className="px-4 py-2.5" />
                </tr>
              </thead>
              <tbody>
                {items.map((e) => (
                  <tr key={e.id} className="border-b border-slate-100 hover:bg-slate-50/60">
                    <td className="px-4 py-2 whitespace-nowrap text-slate-700">{formatDate(e.date)}</td>
                    <td className="px-4 py-2 font-medium text-slate-800">
                      <div className="flex items-center gap-1.5">
                        {e.title}
                        {e.attachmentCount > 0 && <span className="flex items-center gap-0.5 text-[11px] text-slate-400"><Paperclip className="h-3 w-3" />{e.attachmentCount}</span>}
                        {e.source === 'Bank' && <Badge variant="outline" className="text-[9px] px-1 py-0 bg-blue-50 text-blue-700"><Landmark className="h-2.5 w-2.5 mr-0.5" />bank</Badge>}
                      </div>
                    </td>
                    <td className="px-4 py-2"><Badge variant="outline" className="text-[10px]">{e.category}</Badge></td>
                    <td className="px-4 py-2 text-slate-500 max-w-[160px] truncate">{e.vendor ?? '—'}</td>
                    <td className="px-4 py-2 text-slate-500 text-xs">{e.paymentMethod ?? '—'}</td>
                    <td className="px-4 py-2 text-right tabular-nums font-semibold text-slate-900">{formatCurrency(e.amount)}</td>
                    <td className="px-4 py-2 text-right whitespace-nowrap">
                      <Button variant="ghost" size="icon" className="h-7 w-7" onClick={() => openEdit(e.id)}><Pencil className="h-3.5 w-3.5" /></Button>
                      <Button variant="ghost" size="icon" className="h-7 w-7 text-muted-foreground hover:text-destructive" onClick={() => remove(e.id)}><Trash2 className="h-3.5 w-3.5" /></Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="flex items-center justify-between border-t border-slate-100 px-4 py-2.5 text-sm">
            <span className="text-slate-500">{data?.totalCount ?? 0} expenses · page {data?.page ?? 1} of {totalPages}</span>
            <div className="flex gap-1">
              <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}><ChevronLeft className="h-4 w-4" /> Prev</Button>
              <Button variant="outline" size="sm" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>Next <ChevronRight className="h-4 w-4" /></Button>
            </div>
          </div>
        </div>
      )}

      <Dialog open={showForm} onOpenChange={(o) => { if (!o) closeForm() }}>
        <DialogContent className="max-w-xl">
          <DialogHeader><DialogTitle>{editing ? 'Edit Expense' : 'Add Expense'}</DialogTitle></DialogHeader>
          <ExpenseForm expense={editing ?? undefined} onSuccess={closeForm} onCancel={closeForm} />
        </DialogContent>
      </Dialog>
    </div>
  )
}
