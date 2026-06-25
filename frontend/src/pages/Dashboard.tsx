import { useDashboard, useMonthlyTrend } from '@/hooks/useDashboard'
import {
  Wallet,
  TrendingDown,
  ArrowUpCircle,
  Receipt,
  CreditCard,
  CheckCircle,
  Calendar,
  CalendarDays,
  AlertCircle,
  Percent,
  Clock,
  ArrowDownCircle,
  Scale,
} from 'lucide-react'
import SummaryCard from '@/components/common/SummaryCard'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Legend,
  PieChart,
  Pie,
  Cell,
} from 'recharts'
import { formatCurrency, formatDate, paymentTypeLabel } from '@/lib/utils'
import StatusBadge from '@/components/common/StatusBadge'

const COLORS = ['hsl(var(--chart-1))', 'hsl(var(--chart-2))', 'hsl(var(--chart-3))']

function LoadingGrid() {
  return (
    <div className="grid gap-4 grid-cols-2 lg:grid-cols-4">
      {Array.from({ length: 8 }).map((_, i) => (
        <div key={i} className="h-32 rounded-lg bg-muted animate-pulse" />
      ))}
    </div>
  )
}

export default function Dashboard() {
  const { data, isLoading, error } = useDashboard()
  const { data: monthly } = useMonthlyTrend(12)

  if (isLoading) return <LoadingGrid />

  if (error || !data) {
    return (
      <div className="flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 p-4 text-red-800">
        <AlertCircle className="h-4 w-4" />
        <p className="text-sm">Failed to load dashboard data. Ensure the API is running.</p>
      </div>
    )
  }

  const pieData = [
    { name: 'Principal Paid', value: data.totalPrincipalPaid },
    { name: 'Interest Paid', value: data.totalInterestPaid },
    { name: 'Outstanding', value: data.totalOutstanding },
  ]

  const chartData = (monthly ?? data.monthlyTrend)
    .slice(0, 12)
    .reverse()
    .map((m) => ({
      name: m.monthName.split(' ')[0],
      Principal: m.principalPaid,
      Interest: m.interestPaid,
    }))

  return (
    <div className="space-y-6">
      {/* Summary Cards Row 1 */}
      <div className="grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-4">
        <SummaryCard
          title="Total Borrowed"
          value={data.totalBorrowed}
          icon={Wallet}
          iconColor="text-blue-600"
          description="All-time principal"
        />
        <SummaryCard
          title="Outstanding Principal"
          value={data.totalOutstanding}
          icon={TrendingDown}
          iconColor="text-red-600"
          description="Still owed"
        />
        <SummaryCard
          title="Total Principal Paid"
          value={data.totalPrincipalPaid}
          icon={ArrowUpCircle}
          iconColor="text-emerald-600"
        />
        <SummaryCard
          title="Total Amount Paid"
          value={data.totalAmountPaid}
          icon={ArrowUpCircle}
          iconColor="text-emerald-600"
          description="Principal + interest + penalty"
        />
      </div>

      {/* Interest Row — accrued to date vs paid vs pending */}
      <div className="grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3">
        <SummaryCard
          title="Interest Accrued (To Date)"
          value={data.totalInterestAccrued}
          icon={Percent}
          iconColor="text-amber-600"
          description="Total interest built up till today"
        />
        <SummaryCard
          title="Interest Paid (To Date)"
          value={data.totalInterestPaid}
          icon={Receipt}
          iconColor="text-emerald-600"
          description="Interest actually paid so far"
        />
        <SummaryCard
          title="Interest Outstanding"
          value={data.totalInterestOutstanding}
          icon={Clock}
          iconColor="text-red-600"
          description="Accrued minus paid (pending)"
        />
      </div>

      {/* Direction Row — I owe vs owed to me */}
      <div className="grid gap-4 grid-cols-1 sm:grid-cols-3">
        <SummaryCard
          title="I Owe (Borrowed)"
          value={data.borrowedOutstanding}
          icon={ArrowDownCircle}
          iconColor="text-orange-600"
          description={`${data.borrowedLoans} borrowed loan${data.borrowedLoans === 1 ? '' : 's'} outstanding`}
        />
        <SummaryCard
          title="Owed to Me (Lent)"
          value={data.lentOutstanding}
          icon={ArrowUpCircle}
          iconColor="text-blue-600"
          description={`${data.lentLoans} lent loan${data.lentLoans === 1 ? '' : 's'} outstanding`}
        />
        <SummaryCard
          title="Net Position"
          value={data.netPosition}
          icon={Scale}
          iconColor={data.netPosition >= 0 ? 'text-emerald-600' : 'text-red-600'}
          description={data.netPosition >= 0 ? 'Net owed to you' : 'Net you owe'}
        />
      </div>

      {/* Summary Cards Row 2 */}
      <div className="grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-4">
        <SummaryCard
          title="Active Loans"
          value={data.activeLoans}
          isCurrency={false}
          icon={CreditCard}
          iconColor="text-violet-600"
        />
        <SummaryCard
          title="Closed Loans"
          value={data.closedLoans}
          isCurrency={false}
          icon={CheckCircle}
          iconColor="text-slate-600"
        />
        <SummaryCard
          title="This Month Paid"
          value={data.thisMonthPayment}
          icon={Calendar}
          iconColor="text-cyan-600"
        />
        <SummaryCard
          title="This Year Paid"
          value={data.thisYearPayment}
          icon={CalendarDays}
          iconColor="text-indigo-600"
        />
      </div>

      {/* Charts Row */}
      <div className="grid gap-6 lg:grid-cols-3">
        {/* Monthly Payments Chart */}
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle>Monthly Payment Trend</CardTitle>
            <CardDescription>Principal vs interest payments over last 12 months</CardDescription>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={260}>
              <BarChart data={chartData} margin={{ top: 0, right: 0, left: 0, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                <XAxis dataKey="name" tick={{ fontSize: 12 }} />
                <YAxis tickFormatter={(v) => `₹${(v / 1000).toFixed(0)}k`} tick={{ fontSize: 12 }} />
                <Tooltip
                  formatter={(value: number) => formatCurrency(value)}
                  contentStyle={{ fontSize: 12, borderRadius: 8 }}
                />
                <Legend />
                <Bar dataKey="Principal" fill={COLORS[0]} radius={[4, 4, 0, 0]} />
                <Bar dataKey="Interest" fill={COLORS[1]} radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        {/* Pie Chart */}
        <Card>
          <CardHeader>
            <CardTitle>Loan Breakdown</CardTitle>
            <CardDescription>Where does the money go?</CardDescription>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={260}>
              <PieChart>
                <Pie
                  data={pieData}
                  cx="50%"
                  cy="45%"
                  innerRadius={55}
                  outerRadius={90}
                  paddingAngle={3}
                  dataKey="value"
                  label={({ percent }) => `${(percent * 100).toFixed(0)}%`}
                  labelLine={false}
                >
                  {pieData.map((_, index) => (
                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                  ))}
                </Pie>
                <Tooltip formatter={(value: number) => formatCurrency(value)} />
                <Legend />
              </PieChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>
      </div>

      {/* Bottom Row */}
      <div className="grid gap-6 lg:grid-cols-2">
        {/* Lender Summary Table */}
        <Card>
          <CardHeader>
            <CardTitle>Lender-wise Outstanding</CardTitle>
          </CardHeader>
          <CardContent className="p-0">
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b bg-muted/30">
                    <th className="text-left px-4 py-2.5 font-medium text-muted-foreground">Lender</th>
                    <th className="text-right px-4 py-2.5 font-medium text-muted-foreground">Outstanding</th>
                    <th className="text-right px-4 py-2.5 font-medium text-muted-foreground">Loans</th>
                  </tr>
                </thead>
                <tbody>
                  {data.lenderSummaries.filter(l => l.totalOutstanding > 0).map((l) => (
                    <tr key={l.lenderId} className="border-b hover:bg-muted/20 transition-colors">
                      <td className="px-4 py-2.5">
                        <p className="font-medium">{l.lenderName}</p>
                        <p className="text-xs text-muted-foreground">{l.lenderType}</p>
                      </td>
                      <td className="px-4 py-2.5 text-right font-semibold text-red-600">
                        {formatCurrency(l.totalOutstanding)}
                      </td>
                      <td className="px-4 py-2.5 text-right">
                        <Badge variant="secondary">{l.activeLoans}</Badge>
                      </td>
                    </tr>
                  ))}
                  {data.lenderSummaries.filter(l => l.totalOutstanding > 0).length === 0 && (
                    <tr>
                      <td colSpan={3} className="px-4 py-8 text-center text-muted-foreground text-sm">
                        No outstanding loans
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </CardContent>
        </Card>

        {/* Recent Payments */}
        <Card>
          <CardHeader>
            <CardTitle>Recent Payments</CardTitle>
          </CardHeader>
          <CardContent className="p-0">
            <div className="divide-y">
              {data.recentPayments.map((p) => (
                <div key={p.id} className="flex items-start justify-between px-4 py-3 hover:bg-muted/20">
                  <div className="min-w-0 flex-1">
                    <p className="text-sm font-medium truncate">{p.lenderName}</p>
                    <p className="text-xs text-muted-foreground truncate">{p.loanDescription}</p>
                    <div className="flex items-center gap-2 mt-0.5">
                      <span className="text-xs text-muted-foreground">{formatDate(p.paymentDate)}</span>
                      <Badge variant="outline" className="text-[10px] px-1.5 py-0">
                        {paymentTypeLabel(p.principalAmount, p.interestAmount)}
                      </Badge>
                    </div>
                  </div>
                  <p className="text-sm font-semibold ml-4 shrink-0">
                    {formatCurrency(p.totalAmount)}
                  </p>
                </div>
              ))}
              {data.recentPayments.length === 0 && (
                <p className="px-4 py-8 text-center text-sm text-muted-foreground">
                  No payments recorded yet
                </p>
              )}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Top Outstanding Loans */}
      {data.topOutstandingLoans.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Top Outstanding Loans</CardTitle>
            <CardDescription>Active loans with highest outstanding principal</CardDescription>
          </CardHeader>
          <CardContent className="p-0">
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b bg-muted/30">
                    <th className="text-left px-4 py-2.5 font-medium text-muted-foreground">Loan</th>
                    <th className="text-left px-4 py-2.5 font-medium text-muted-foreground">Lender</th>
                    <th className="text-right px-4 py-2.5 font-medium text-muted-foreground">Original</th>
                    <th className="text-right px-4 py-2.5 font-medium text-muted-foreground">Outstanding</th>
                    <th className="text-right px-4 py-2.5 font-medium text-muted-foreground">Rate</th>
                    <th className="text-center px-4 py-2.5 font-medium text-muted-foreground">Status</th>
                  </tr>
                </thead>
                <tbody>
                  {data.topOutstandingLoans.map((l) => (
                    <tr key={l.loanId} className="border-b hover:bg-muted/20">
                      <td className="px-4 py-3 font-medium">{l.loanDescription}</td>
                      <td className="px-4 py-3 text-muted-foreground">{l.lenderName}</td>
                      <td className="px-4 py-3 text-right">{formatCurrency(l.originalPrincipal)}</td>
                      <td className="px-4 py-3 text-right font-semibold text-red-600">
                        {formatCurrency(l.outstandingPrincipal)}
                      </td>
                      <td className="px-4 py-3 text-right">{l.currentInterestRate}%</td>
                      <td className="px-4 py-3 text-center">
                        <StatusBadge status={l.status} />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
