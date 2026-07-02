import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
  PieChart, Pie, Cell, LineChart, Line,
} from 'recharts'
import { Wallet, Calendar, CalendarDays, Receipt, Filter, AlertCircle } from 'lucide-react'
import { expensesApi } from '@/api/expenses'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import SummaryCard from '@/components/common/SummaryCard'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { formatCurrency, formatDate } from '@/lib/utils'

const COLORS = ['#1e3a5f', '#2f5d8a', '#4a7ab5', '#6f9bcb', '#9bbcd9', '#c3d5e6', '#8ba7bd', '#dde7f0']
const kFmt = (v: number) =>
  Math.abs(v) >= 1e5 ? `₹${(v / 1e5).toFixed(1)}L` : Math.abs(v) >= 1e3 ? `₹${(v / 1e3).toFixed(0)}k` : `₹${v}`

export default function ExpenseDashboard() {
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const { data, isLoading } = useQuery({
    queryKey: ['expenses', 'summary', from, to],
    queryFn: () => expensesApi.summary({ from: from || undefined, to: to || undefined }),
  })

  const filters = (
    <div className="flex flex-wrap items-center gap-2 rounded-lg border border-slate-200 bg-slate-50/60 px-4 py-3">
      <div className="flex items-center gap-1.5 text-xs font-medium text-slate-500 mr-1"><Filter className="h-3.5 w-3.5" /> Period</div>
      <Input type="date" value={from} onChange={(e) => setFrom(e.target.value)} className="h-9 w-36" />
      <span className="text-xs text-slate-400">to</span>
      <Input type="date" value={to} onChange={(e) => setTo(e.target.value)} className="h-9 w-36" />
      {(from || to) && <Button variant="outline" size="sm" className="h-9" onClick={() => { setFrom(''); setTo('') }}>Clear</Button>}
    </div>
  )

  if (isLoading) return <div className="space-y-4">{filters}<div className="h-64 rounded-lg bg-muted animate-pulse" /></div>
  if (!data || data.count === 0) {
    return (
      <div className="space-y-4">
        {filters}
        <div className="flex items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 p-4 text-amber-800">
          <AlertCircle className="h-4 w-4" /><p className="text-sm">No expenses yet. Add some from the Expenses page.</p>
        </div>
      </div>
    )
  }

  const momChange = data.lastMonth > 0 ? Math.round(((data.thisMonth - data.lastMonth) / data.lastMonth) * 100) : null
  const pie = data.byCategory.slice(0, 7).map((c) => ({ name: c.category, value: c.total }))
  const bars = data.byCategory.slice(0, 8).map((c) => ({ name: c.category, Amount: c.total }))
  const monthly = data.monthly.map((m) => ({ name: m.monthName.split(' ')[0], Spent: m.total }))

  return (
    <div className="space-y-5">
      {filters}

      <div className="grid gap-4 grid-cols-2 lg:grid-cols-4">
        <SummaryCard title="Total Spent" value={data.total} icon={Wallet} iconColor="text-slate-700" description={`${data.count} expenses`} />
        <SummaryCard title="This Month" value={data.thisMonth} icon={Calendar} iconColor="text-blue-700"
          trend={momChange != null ? { value: momChange, label: 'vs last month' } : undefined} />
        <SummaryCard title="Avg / Day" value={data.averagePerDay} icon={CalendarDays} iconColor="text-slate-600" />
        <SummaryCard title="Avg / Expense" value={data.averagePerExpense} icon={Receipt} iconColor="text-slate-600"
          description={data.topCategory ? `Top: ${data.topCategory}` : undefined} />
      </div>

      <div className="grid gap-5 lg:grid-cols-3">
        <Card className="lg:col-span-1">
          <CardHeader className="pb-2"><CardTitle className="text-sm uppercase tracking-wider text-slate-600">By Category</CardTitle></CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={240}>
              <PieChart>
                <Pie data={pie} dataKey="value" nameKey="name" cx="50%" cy="50%" innerRadius={50} outerRadius={85} paddingAngle={2}>
                  {pie.map((_, i) => <Cell key={i} fill={COLORS[i % COLORS.length]} />)}
                </Pie>
                <Tooltip formatter={(v: number) => formatCurrency(v)} contentStyle={{ fontSize: 12, borderRadius: 8 }} />
              </PieChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>
        <Card className="lg:col-span-2">
          <CardHeader className="pb-2"><CardTitle className="text-sm uppercase tracking-wider text-slate-600">Top Categories</CardTitle></CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={240}>
              <BarChart data={bars} layout="vertical" margin={{ left: 20 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" horizontal={false} />
                <XAxis type="number" tickFormatter={kFmt} tick={{ fontSize: 11, fill: '#94a3b8' }} />
                <YAxis type="category" dataKey="name" width={90} tick={{ fontSize: 11, fill: '#64748b' }} />
                <Tooltip formatter={(v: number) => formatCurrency(v)} contentStyle={{ fontSize: 12, borderRadius: 8 }} />
                <Bar dataKey="Amount" fill={COLORS[1]} radius={[0, 3, 3, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader className="pb-2"><CardTitle className="text-sm uppercase tracking-wider text-slate-600">Monthly Spending</CardTitle></CardHeader>
        <CardContent>
          <ResponsiveContainer width="100%" height={220}>
            <LineChart data={monthly}>
              <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" vertical={false} />
              <XAxis dataKey="name" tick={{ fontSize: 12, fill: '#64748b' }} />
              <YAxis tickFormatter={kFmt} tick={{ fontSize: 11, fill: '#94a3b8' }} />
              <Tooltip formatter={(v: number) => formatCurrency(v)} contentStyle={{ fontSize: 12, borderRadius: 8 }} />
              <Line type="monotone" dataKey="Spent" stroke={COLORS[0]} strokeWidth={2} dot={{ r: 3 }} />
            </LineChart>
          </ResponsiveContainer>
        </CardContent>
      </Card>

      <div className="grid gap-5 lg:grid-cols-2">
        <Card>
          <CardHeader className="pb-2"><CardTitle className="text-sm uppercase tracking-wider text-slate-600">Category Breakdown</CardTitle></CardHeader>
          <CardContent className="p-0">
            <table className="w-full text-sm">
              <thead><tr className="border-b bg-slate-50 text-left">
                <th className="px-4 py-2 text-[11px] uppercase tracking-wider text-slate-400">Category</th>
                <th className="px-4 py-2 text-[11px] uppercase tracking-wider text-slate-400 text-right">Spent</th>
                <th className="px-4 py-2 text-[11px] uppercase tracking-wider text-slate-400 text-right">%</th>
                <th className="px-4 py-2 text-[11px] uppercase tracking-wider text-slate-400 text-right">Count</th>
              </tr></thead>
              <tbody>
                {data.byCategory.map((c) => (
                  <tr key={c.category} className="border-b border-slate-100">
                    <td className="px-4 py-2 text-slate-700">{c.category}</td>
                    <td className="px-4 py-2 text-right tabular-nums font-medium">{formatCurrency(c.total)}</td>
                    <td className="px-4 py-2 text-right text-slate-500">{c.percentage}%</td>
                    <td className="px-4 py-2 text-right text-slate-500">{c.count}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2"><CardTitle className="text-sm uppercase tracking-wider text-slate-600">Recent Expenses</CardTitle></CardHeader>
          <CardContent className="p-0">
            <div className="divide-y">
              {data.recent.map((e) => (
                <div key={e.id} className="flex items-center justify-between px-4 py-2.5">
                  <div className="min-w-0">
                    <p className="text-sm font-medium text-slate-800 truncate">{e.title}</p>
                    <p className="text-xs text-slate-400">{formatDate(e.date)} · <Badge variant="outline" className="text-[9px] px-1 py-0">{e.category}</Badge></p>
                  </div>
                  <p className="text-sm font-semibold tabular-nums ml-3">{formatCurrency(e.amount)}</p>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
