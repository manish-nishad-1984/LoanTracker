import { useState } from 'react'
import { useDashboardLenders, useYearlySummary, useMonthlyTrend } from '@/hooks/useDashboard'
import { usePayments } from '@/hooks/usePayments'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip,
  ResponsiveContainer, Legend, LineChart, Line,
} from 'recharts'
import { formatCurrency, formatDate, paymentTypeLabel } from '@/lib/utils'

export default function Reports() {
  const [selectedYear, setSelectedYear] = useState(new Date().getFullYear().toString())
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')

  const { data: lenderSummaries } = useDashboardLenders()
  const { data: yearlySummary } = useYearlySummary(6)
  const { data: monthly } = useMonthlyTrend(24)
  const { data: payments } = usePayments({
    from: fromDate || undefined,
    to: toDate || undefined,
    pageSize: 200,
  })

  const yearlyChartData = (yearlySummary ?? []).slice().reverse().map((y) => ({
    year: y.year.toString(),
    Principal: y.principalPaid,
    Interest: y.interestPaid,
    Total: y.totalPaid,
  }))

  const monthlyChartData = (monthly ?? [])
    .filter((m) => m.year.toString() === selectedYear)
    .slice()
    .reverse()
    .map((m) => ({
      name: m.monthName.split(' ')[0],
      Principal: m.principalPaid,
      Interest: m.interestPaid,
    }))

  const currentYear = new Date().getFullYear()
  const yearOptions = Array.from({ length: 6 }, (_, i) => (currentYear - i).toString())

  return (
    <div className="space-y-6">
      {/* Yearly Summary Chart */}
      <Card>
        <CardHeader>
          <CardTitle>Yearly Payment Summary</CardTitle>
          <CardDescription>Principal vs interest paid per year</CardDescription>
        </CardHeader>
        <CardContent>
          <ResponsiveContainer width="100%" height={280}>
            <BarChart data={yearlyChartData}>
              <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
              <XAxis dataKey="year" />
              <YAxis tickFormatter={(v) => `₹${(v / 100000).toFixed(1)}L`} />
              <Tooltip formatter={(value: number) => formatCurrency(value)} />
              <Legend />
              <Bar dataKey="Principal" fill="hsl(var(--chart-1))" radius={[4, 4, 0, 0]} />
              <Bar dataKey="Interest" fill="hsl(var(--chart-2))" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </CardContent>
      </Card>

      {/* Monthly Breakdown */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>Monthly Breakdown</CardTitle>
              <CardDescription>Payment trend for selected year</CardDescription>
            </div>
            <Select value={selectedYear} onValueChange={setSelectedYear}>
              <SelectTrigger className="w-28">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {yearOptions.map((y) => (
                  <SelectItem key={y} value={y}>{y}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </CardHeader>
        <CardContent>
          <ResponsiveContainer width="100%" height={240}>
            <LineChart data={monthlyChartData}>
              <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
              <XAxis dataKey="name" />
              <YAxis tickFormatter={(v) => `₹${(v / 1000).toFixed(0)}k`} />
              <Tooltip formatter={(value: number) => formatCurrency(value)} />
              <Legend />
              <Line type="monotone" dataKey="Principal" stroke="hsl(var(--chart-1))" strokeWidth={2} dot={false} />
              <Line type="monotone" dataKey="Interest" stroke="hsl(var(--chart-2))" strokeWidth={2} dot={false} />
            </LineChart>
          </ResponsiveContainer>
        </CardContent>
      </Card>

      {/* Party-wise Summary Table */}
      <Card>
        <CardHeader>
          <CardTitle>Party-wise Summary</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/30">
                  <th className="text-left px-4 py-3 font-medium text-muted-foreground">Party</th>
                  <th className="text-right px-4 py-3 font-medium text-muted-foreground">Borrowed</th>
                  <th className="text-right px-4 py-3 font-medium text-muted-foreground">Principal Paid</th>
                  <th className="text-right px-4 py-3 font-medium text-muted-foreground">Interest Paid</th>
                  <th className="text-right px-4 py-3 font-medium text-muted-foreground">Outstanding</th>
                  <th className="text-center px-4 py-3 font-medium text-muted-foreground">Loans</th>
                </tr>
              </thead>
              <tbody>
                {(lenderSummaries ?? []).map((l) => (
                  <tr key={l.lenderId} className="border-b hover:bg-muted/20">
                    <td className="px-4 py-3">
                      <p className="font-medium">{l.lenderName}</p>
                      <p className="text-xs text-muted-foreground">{l.lenderType}</p>
                    </td>
                    <td className="px-4 py-3 text-right">{formatCurrency(l.totalBorrowed)}</td>
                    <td className="px-4 py-3 text-right text-emerald-600">{formatCurrency(l.totalPrincipalPaid)}</td>
                    <td className="px-4 py-3 text-right text-amber-600">{formatCurrency(l.totalInterestPaid)}</td>
                    <td className="px-4 py-3 text-right font-semibold text-red-600">
                      {formatCurrency(l.totalOutstanding)}
                    </td>
                    <td className="px-4 py-3 text-center">
                      <Badge variant="outline">{l.activeLoans} active</Badge>
                    </td>
                  </tr>
                ))}
              </tbody>
              {lenderSummaries && lenderSummaries.length > 0 && (
                <tfoot>
                  <tr className="bg-muted/50 font-semibold border-t-2">
                    <td className="px-4 py-3">Total</td>
                    <td className="px-4 py-3 text-right">
                      {formatCurrency(lenderSummaries.reduce((s, l) => s + l.totalBorrowed, 0))}
                    </td>
                    <td className="px-4 py-3 text-right text-emerald-600">
                      {formatCurrency(lenderSummaries.reduce((s, l) => s + l.totalPrincipalPaid, 0))}
                    </td>
                    <td className="px-4 py-3 text-right text-amber-600">
                      {formatCurrency(lenderSummaries.reduce((s, l) => s + l.totalInterestPaid, 0))}
                    </td>
                    <td className="px-4 py-3 text-right text-red-600">
                      {formatCurrency(lenderSummaries.reduce((s, l) => s + l.totalOutstanding, 0))}
                    </td>
                    <td />
                  </tr>
                </tfoot>
              )}
            </table>
          </div>
        </CardContent>
      </Card>

      {/* Payment Ledger with date filter */}
      <Card>
        <CardHeader>
          <div className="flex items-start justify-between gap-4 flex-wrap">
            <div>
              <CardTitle>Payment Ledger</CardTitle>
              <CardDescription>All payments with date filter</CardDescription>
            </div>
            <div className="flex items-center gap-2">
              <Input
                type="date"
                value={fromDate}
                onChange={(e) => setFromDate(e.target.value)}
                className="w-36"
              />
              <span className="text-sm text-muted-foreground">to</span>
              <Input
                type="date"
                value={toDate}
                onChange={(e) => setToDate(e.target.value)}
                className="w-36"
              />
              <Button
                variant="outline"
                size="sm"
                onClick={() => { setFromDate(''); setToDate('') }}
              >
                Clear
              </Button>
            </div>
          </div>
        </CardHeader>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/30">
                  <th className="text-left px-4 py-2.5 font-medium text-muted-foreground">Date</th>
                  <th className="text-left px-4 py-2.5 font-medium text-muted-foreground">Party</th>
                  <th className="text-left px-4 py-2.5 font-medium text-muted-foreground">Loan</th>
                  <th className="text-right px-4 py-2.5 font-medium text-muted-foreground">Total</th>
                  <th className="text-right px-4 py-2.5 font-medium text-muted-foreground">Principal</th>
                  <th className="text-right px-4 py-2.5 font-medium text-muted-foreground">Interest</th>
                  <th className="text-left px-4 py-2.5 font-medium text-muted-foreground">Type</th>
                  <th className="text-left px-4 py-2.5 font-medium text-muted-foreground">Mode</th>
                </tr>
              </thead>
              <tbody>
                {(payments?.items ?? []).map((p) => (
                  <tr key={p.id} className="border-b hover:bg-muted/20">
                    <td className="px-4 py-2.5">{formatDate(p.paymentDate)}</td>
                    <td className="px-4 py-2.5">{p.lenderName}</td>
                    <td className="px-4 py-2.5 text-muted-foreground text-xs">{p.loanDescription}</td>
                    <td className="px-4 py-2.5 text-right font-semibold">{formatCurrency(p.totalAmount)}</td>
                    <td className="px-4 py-2.5 text-right text-emerald-700">
                      {p.principalAmount > 0 ? formatCurrency(p.principalAmount) : '—'}
                    </td>
                    <td className="px-4 py-2.5 text-right text-amber-700">
                      {p.interestAmount > 0 ? formatCurrency(p.interestAmount) : '—'}
                    </td>
                    <td className="px-4 py-2.5">
                      <Badge variant="outline" className="text-xs">
                        {paymentTypeLabel(p.principalAmount, p.interestAmount)}
                      </Badge>
                    </td>
                    <td className="px-4 py-2.5 text-muted-foreground">{p.paymentModeName}</td>
                  </tr>
                ))}
                {(payments?.items ?? []).length === 0 && (
                  <tr>
                    <td colSpan={8} className="px-4 py-8 text-center text-muted-foreground">
                      No payments in selected range
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
