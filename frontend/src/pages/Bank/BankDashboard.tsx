import { useQuery } from '@tanstack/react-query'
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend,
  PieChart, Pie, Cell, LineChart, Line,
} from 'recharts'
import { AlertCircle, Lightbulb, Landmark } from 'lucide-react'
import { bankApi } from '@/api/bank'
import { formatCurrency } from '@/lib/utils'
import type { BankDashboard as Dash } from '@/types'

// Cold, corporate blue–grey palette
const BLUES = ['#1e3a5f', '#2f5d8a', '#4a7ab5', '#6f9bcb', '#9bbcd9', '#c3d5e6', '#dde7f0', '#8ba7bd']
const GRID = '#e2e8f0'

const kFmt = (v: number) =>
  Math.abs(v) >= 1e7 ? `₹${(v / 1e7).toFixed(1)}Cr` :
  Math.abs(v) >= 1e5 ? `₹${(v / 1e5).toFixed(1)}L` :
  Math.abs(v) >= 1e3 ? `₹${(v / 1e3).toFixed(0)}k` : `₹${v}`

function Section({ title, desc, children, className = '' }: { title: string; desc?: string; children: React.ReactNode; className?: string }) {
  return (
    <div className={`rounded-lg border border-slate-200 bg-white ${className}`}>
      <div className="border-b border-slate-100 px-5 py-3">
        <h3 className="text-xs font-semibold uppercase tracking-wider text-slate-600">{title}</h3>
        {desc && <p className="text-xs text-slate-400 mt-0.5">{desc}</p>}
      </div>
      <div className="p-5">{children}</div>
    </div>
  )
}

function Stat({ label, value, sub, tone }: { label: string; value: string; sub?: string; tone?: 'pos' | 'neg' }) {
  return (
    <div className="rounded-md border border-slate-200 bg-slate-50/60 px-3.5 py-3">
      <p className="text-[10.5px] font-medium uppercase tracking-wider text-slate-500">{label}</p>
      <p className={`mt-1 text-base font-semibold tabular-nums ${tone === 'pos' ? 'text-blue-800' : tone === 'neg' ? 'text-rose-700' : 'text-slate-900'}`}>{value}</p>
      {sub && <p className="text-[11px] text-slate-400 mt-0.5 truncate">{sub}</p>}
    </div>
  )
}

const tooltipStyle = { fontSize: 12, borderRadius: 8, border: '1px solid #e2e8f0' }

export default function BankDashboard() {
  const accountId: string | undefined = undefined
  const { data, isLoading, error } = useQuery<Dash>({
    queryKey: ['bank', 'dashboard', accountId ?? 'all'],
    queryFn: () => bankApi.getDashboard({ accountId }),
  })

  if (isLoading) return <div className="h-64 rounded-lg bg-slate-100 animate-pulse" />
  if (error || !data) {
    return (
      <div className="flex items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 p-4 text-amber-800">
        <AlertCircle className="h-4 w-4" />
        <p className="text-sm">{error instanceof Error ? error.message : 'No data. Import a statement first (Bank Statement → Import).'}</p>
      </div>
    )
  }

  const expensePie = data.expenseByCategory.slice(0, 7).map((c) => ({ name: c.category, value: c.total }))
  const methodPie = data.paymentMethods.map((m) => ({ name: m.method, value: m.total }))
  const monthly = data.monthlyCashflow.map((m) => ({ name: m.monthName.split(' ')[0], Income: m.income, Expense: m.expense }))
  const balanceTrend = data.monthlyCashflow.map((m) => ({ name: m.monthName.split(' ')[0], Balance: m.closingBalance ?? 0 }))
  const daily = data.dailySpend.map((d) => ({ name: d.date.slice(5), Spend: d.spend }))
  const catBar = data.expenseByCategory.slice(0, 8).map((c) => ({ name: c.category, Amount: c.total }))

  return (
    <div className="space-y-5">
      {/* Title */}
      <div className="flex flex-wrap items-end justify-between gap-3 border-b border-slate-200 pb-4">
        <div>
          <p className="flex items-center gap-2 text-xs font-medium uppercase tracking-widest text-slate-400">
            <Landmark className="h-3.5 w-3.5" /> {data.bank} · Account Statement Analysis
          </p>
          <h1 className="text-2xl font-semibold text-slate-900 mt-1">{data.accountName ?? 'Account'}</h1>
          <p className="text-sm text-slate-500">
            A/C {data.accountNumber} · {data.fromDate} → {data.toDate} ({data.days} days)
          </p>
        </div>
        <div className="text-right">
          <p className="text-[11px] uppercase tracking-wider text-slate-400">Financial Health</p>
          <p className="text-3xl font-semibold text-slate-900 tabular-nums">{data.healthScore}<span className="text-base text-slate-400">/100</span></p>
          <p className="text-xs font-medium text-blue-800">{data.healthLabel}</p>
        </div>
      </div>

      {/* Executive summary */}
      <Section title="Executive Summary">
        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3">
          <Stat label="Opening Balance" value={formatCurrency(data.openingBalance)} />
          <Stat label="Closing Balance" value={formatCurrency(data.closingBalance)} />
          <Stat label="Total Credits (In)" value={formatCurrency(data.totalCredits)} tone="pos" />
          <Stat label="Total Debits (Out)" value={formatCurrency(data.totalDebits)} tone="neg" />
          <Stat label="Net Cash Flow" value={formatCurrency(data.netCashFlow)} tone={data.netCashFlow >= 0 ? 'pos' : 'neg'} />
          <Stat label="Savings Rate" value={`${data.savingsRate}%`} tone={data.savingsRate >= 0 ? 'pos' : 'neg'} />
          <Stat label="Expense Ratio" value={`${data.expenseRatio}%`} />
          <Stat label="Transactions" value={String(data.transactionCount)} />
          <Stat label="Avg Daily Income" value={formatCurrency(data.avgDailyIncome)} />
          <Stat label="Avg Daily Expense" value={formatCurrency(data.avgDailyExpense)} />
          <Stat label="Highest Credit" value={formatCurrency(data.highestCredit)} />
          <Stat label="Highest Debit" value={formatCurrency(data.highestDebit)} />
          <Stat label="Avg Transaction" value={formatCurrency(data.avgTransactionAmount)} />
          <Stat label="Most Used Method" value={data.mostUsedPaymentMethod ?? '—'} />
          <Stat label="Largest Merchant" value={data.largestMerchant ?? '—'} />
          <Stat label="Most Frequent Payee" value={data.mostFrequentMerchant ?? '—'} />
        </div>
      </Section>

      {/* Cash flow charts */}
      <div className="grid gap-5 lg:grid-cols-2">
        <Section title="Monthly Income vs Expense">
          <ResponsiveContainer width="100%" height={260}>
            <BarChart data={monthly}>
              <CartesianGrid strokeDasharray="3 3" stroke={GRID} vertical={false} />
              <XAxis dataKey="name" tick={{ fontSize: 12, fill: '#64748b' }} />
              <YAxis tickFormatter={kFmt} tick={{ fontSize: 11, fill: '#94a3b8' }} />
              <Tooltip formatter={(v: number) => formatCurrency(v)} contentStyle={tooltipStyle} />
              <Legend wrapperStyle={{ fontSize: 12 }} />
              <Bar dataKey="Income" fill={BLUES[2]} radius={[3, 3, 0, 0]} />
              <Bar dataKey="Expense" fill={BLUES[0]} radius={[3, 3, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </Section>
        <Section title="Closing Balance Trend">
          <ResponsiveContainer width="100%" height={260}>
            <LineChart data={balanceTrend}>
              <CartesianGrid strokeDasharray="3 3" stroke={GRID} vertical={false} />
              <XAxis dataKey="name" tick={{ fontSize: 12, fill: '#64748b' }} />
              <YAxis tickFormatter={kFmt} tick={{ fontSize: 11, fill: '#94a3b8' }} />
              <Tooltip formatter={(v: number) => formatCurrency(v)} contentStyle={tooltipStyle} />
              <Line type="monotone" dataKey="Balance" stroke={BLUES[1]} strokeWidth={2} dot={{ r: 3 }} />
            </LineChart>
          </ResponsiveContainer>
        </Section>
      </div>

      {/* Distribution charts */}
      <div className="grid gap-5 lg:grid-cols-3">
        <Section title="Expense Distribution">
          <ResponsiveContainer width="100%" height={240}>
            <PieChart>
              <Pie data={expensePie} dataKey="value" nameKey="name" cx="50%" cy="50%" innerRadius={50} outerRadius={85} paddingAngle={2}>
                {expensePie.map((_, i) => <Cell key={i} fill={BLUES[i % BLUES.length]} />)}
              </Pie>
              <Tooltip formatter={(v: number) => formatCurrency(v)} contentStyle={tooltipStyle} />
              <Legend wrapperStyle={{ fontSize: 11 }} />
            </PieChart>
          </ResponsiveContainer>
        </Section>
        <Section title="Category-wise Expense">
          <ResponsiveContainer width="100%" height={240}>
            <BarChart data={catBar} layout="vertical" margin={{ left: 20 }}>
              <CartesianGrid strokeDasharray="3 3" stroke={GRID} horizontal={false} />
              <XAxis type="number" tickFormatter={kFmt} tick={{ fontSize: 11, fill: '#94a3b8' }} />
              <YAxis type="category" dataKey="name" width={90} tick={{ fontSize: 11, fill: '#64748b' }} />
              <Tooltip formatter={(v: number) => formatCurrency(v)} contentStyle={tooltipStyle} />
              <Bar dataKey="Amount" fill={BLUES[1]} radius={[0, 3, 3, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </Section>
        <Section title="Payment Method">
          <ResponsiveContainer width="100%" height={240}>
            <PieChart>
              <Pie data={methodPie} dataKey="value" nameKey="name" cx="50%" cy="50%" outerRadius={85} paddingAngle={2}>
                {methodPie.map((_, i) => <Cell key={i} fill={BLUES[i % BLUES.length]} />)}
              </Pie>
              <Tooltip formatter={(v: number) => formatCurrency(v)} contentStyle={tooltipStyle} />
              <Legend wrapperStyle={{ fontSize: 11 }} />
            </PieChart>
          </ResponsiveContainer>
        </Section>
      </div>

      {/* Daily spend */}
      <Section title="Daily Spending">
        <ResponsiveContainer width="100%" height={200}>
          <LineChart data={daily}>
            <CartesianGrid strokeDasharray="3 3" stroke={GRID} vertical={false} />
            <XAxis dataKey="name" tick={{ fontSize: 10, fill: '#94a3b8' }} interval="preserveStartEnd" minTickGap={24} />
            <YAxis tickFormatter={kFmt} tick={{ fontSize: 11, fill: '#94a3b8' }} />
            <Tooltip formatter={(v: number) => formatCurrency(v)} contentStyle={tooltipStyle} />
            <Line type="monotone" dataKey="Spend" stroke={BLUES[0]} strokeWidth={1.5} dot={false} />
          </LineChart>
        </ResponsiveContainer>
      </Section>

      {/* Income + Expense analysis tables */}
      <div className="grid gap-5 lg:grid-cols-2">
        <Section title="Income Analysis">
          <Table head={['Source', 'Amount', '%']}>
            {data.incomeByCategory.map((c) => (
              <tr key={c.category} className="border-b border-slate-100">
                <td className="py-2 text-slate-700">{c.category}</td>
                <td className="py-2 text-right tabular-nums text-blue-800">{formatCurrency(c.total)}</td>
                <td className="py-2 text-right text-slate-500">{c.percentage}%</td>
              </tr>
            ))}
          </Table>
        </Section>
        <Section title="Expense Analysis">
          <Table head={['Category', 'Amount', '%', 'Txns', 'Avg']}>
            {data.expenseByCategory.map((c) => (
              <tr key={c.category} className="border-b border-slate-100">
                <td className="py-2 text-slate-700">{c.category}</td>
                <td className="py-2 text-right tabular-nums">{formatCurrency(c.total)}</td>
                <td className="py-2 text-right text-slate-500">{c.percentage}%</td>
                <td className="py-2 text-right text-slate-500">{c.count}</td>
                <td className="py-2 text-right text-slate-500">{formatCurrency(c.average)}</td>
              </tr>
            ))}
          </Table>
        </Section>
      </div>

      {/* Merchants + recurring */}
      <div className="grid gap-5 lg:grid-cols-2">
        <Section title="Top 10 Merchants (by spend)">
          <Table head={['Merchant', 'Spend', 'Txns']}>
            {data.topMerchants.map((m) => (
              <tr key={m.merchant} className="border-b border-slate-100">
                <td className="py-2 text-slate-700 truncate max-w-[220px]">{m.merchant}</td>
                <td className="py-2 text-right tabular-nums">{formatCurrency(m.total)}</td>
                <td className="py-2 text-right text-slate-500">{m.count}</td>
              </tr>
            ))}
          </Table>
        </Section>
        <Section title="Recurring & Possible Subscriptions">
          {data.recurringPayments.length === 0 ? (
            <p className="text-sm text-slate-400">No recurring payees detected.</p>
          ) : (
            <Table head={['Payee', 'Times', 'Avg', 'Type']}>
              {data.recurringPayments.map((r) => (
                <tr key={r.merchant} className="border-b border-slate-100">
                  <td className="py-2 text-slate-700 truncate max-w-[200px]">{r.merchant}</td>
                  <td className="py-2 text-right text-slate-500">{r.count}</td>
                  <td className="py-2 text-right tabular-nums">{formatCurrency(r.avgAmount)}</td>
                  <td className="py-2 text-right text-xs text-slate-500">{r.likelySubscription ? 'Subscription?' : r.category}</td>
                </tr>
              ))}
            </Table>
          )}
        </Section>
      </div>

      {/* Largest + weekend */}
      <div className="grid gap-5 lg:grid-cols-3">
        <Section title="Largest Expenses" className="lg:col-span-1">
          <ul className="space-y-2 text-sm">
            {data.largestExpenses.slice(0, 6).map((t) => (
              <li key={t.id} className="flex justify-between gap-2">
                <span className="truncate text-slate-600">{t.merchant ?? t.narration.slice(0, 24)}</span>
                <span className="tabular-nums text-rose-700">{formatCurrency(t.amount)}</span>
              </li>
            ))}
          </ul>
        </Section>
        <Section title="Largest Incomes" className="lg:col-span-1">
          <ul className="space-y-2 text-sm">
            {data.largestIncomes.slice(0, 6).map((t) => (
              <li key={t.id} className="flex justify-between gap-2">
                <span className="truncate text-slate-600">{t.merchant ?? t.narration.slice(0, 24)}</span>
                <span className="tabular-nums text-blue-800">{formatCurrency(t.amount)}</span>
              </li>
            ))}
          </ul>
        </Section>
        <Section title="Weekend vs Weekday" className="lg:col-span-1">
          <div className="space-y-3">
            <div className="flex justify-between text-sm"><span className="text-slate-600">Weekday spend</span><span className="tabular-nums">{formatCurrency(data.weekendVsWeekday.weekdaySpend)}</span></div>
            <div className="flex justify-between text-sm"><span className="text-slate-600">Weekend spend</span><span className="tabular-nums">{formatCurrency(data.weekendVsWeekday.weekendSpend)}</span></div>
            <div className="flex justify-between text-xs text-slate-400 border-t pt-2"><span>{data.weekendVsWeekday.weekdayCount} weekday txns</span><span>{data.weekendVsWeekday.weekendCount} weekend txns</span></div>
          </div>
        </Section>
      </div>

      {/* AI insights */}
      <Section title="AI Financial Insights">
        <ul className="space-y-2.5">
          {data.insights.map((s, i) => (
            <li key={i} className="flex gap-2.5 text-sm text-slate-700">
              <Lightbulb className="h-4 w-4 text-blue-700 shrink-0 mt-0.5" />
              <span>{s}</span>
            </li>
          ))}
        </ul>
      </Section>
    </div>
  )
}

function Table({ head, children }: { head: string[]; children: React.ReactNode }) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-slate-200">
            {head.map((h, i) => (
              <th key={h} className={`pb-2 text-[11px] font-medium uppercase tracking-wider text-slate-400 ${i === 0 ? 'text-left' : 'text-right'}`}>{h}</th>
            ))}
          </tr>
        </thead>
        <tbody>{children}</tbody>
      </table>
    </div>
  )
}
