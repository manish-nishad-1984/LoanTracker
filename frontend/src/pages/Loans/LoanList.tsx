import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { CreditCard, Plus, Search, Filter } from 'lucide-react'
import { useLoans } from '@/hooks/useLoans'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card } from '@/components/ui/card'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import EmptyState from '@/components/common/EmptyState'
import StatusBadge from '@/components/common/StatusBadge'
import { formatCurrency, formatDate, formatPercent } from '@/lib/utils'
import LoanForm from './LoanForm'
import { useToast } from '@/hooks/useToast'
import type { LoanStatus, LoanDirection } from '@/types'

export default function LoanList() {
  const navigate = useNavigate()
  const { toast } = useToast()
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<LoanStatus | ''>('')
  const [direction, setDirection] = useState<LoanDirection | ''>('')
  const [showCreate, setShowCreate] = useState(false)

  const { data, isLoading } = useLoans({
    search,
    status: status || undefined,
    direction: direction || undefined,
    pageSize: 50,
  })

  const loans = data?.items ?? []

  return (
    <div className="space-y-4">
      {/* Toolbar */}
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex gap-2 flex-1">
          <div className="relative flex-1 max-w-sm">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Search loans..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="pl-9"
            />
          </div>
          <Select value={direction} onValueChange={(v) => setDirection(v as LoanDirection | '')}>
            <SelectTrigger className="w-40">
              <Filter className="h-4 w-4 mr-1.5 text-muted-foreground" />
              <SelectValue placeholder="Type" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="">All Types</SelectItem>
              <SelectItem value="Borrowed">Borrowed (I owe)</SelectItem>
              <SelectItem value="Lent">Lent (owed to me)</SelectItem>
            </SelectContent>
          </Select>
          <Select value={status} onValueChange={(v) => setStatus(v as LoanStatus | '')}>
            <SelectTrigger className="w-36">
              <Filter className="h-4 w-4 mr-1.5 text-muted-foreground" />
              <SelectValue placeholder="Status" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="">All Status</SelectItem>
              <SelectItem value="Active">Active</SelectItem>
              <SelectItem value="Closed">Closed</SelectItem>
              <SelectItem value="Defaulted">Defaulted</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <Button onClick={() => setShowCreate(true)}>
          <Plus className="h-4 w-4" />
          Add Loan
        </Button>
      </div>

      <div className="text-sm text-muted-foreground">
        {data?.totalCount ?? 0} loans found
      </div>

      {/* Table */}
      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="h-20 rounded-lg bg-muted animate-pulse" />
          ))}
        </div>
      ) : loans.length === 0 ? (
        <EmptyState
          icon={CreditCard}
          title="No loans yet"
          description="Add your first loan to start tracking your borrowings."
          actionLabel="Add Loan"
          onAction={() => setShowCreate(true)}
        />
      ) : (
        <Card>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/30">
                  <th className="text-left px-4 py-3 font-medium text-muted-foreground">Loan</th>
                  <th className="text-left px-4 py-3 font-medium text-muted-foreground">Lender</th>
                  <th className="text-right px-4 py-3 font-medium text-muted-foreground">Principal</th>
                  <th className="text-right px-4 py-3 font-medium text-muted-foreground">Outstanding</th>
                  <th className="text-right px-4 py-3 font-medium text-muted-foreground">Interest (accrued)</th>
                  <th className="text-right px-4 py-3 font-medium text-muted-foreground">Rate</th>
                  <th className="text-left px-4 py-3 font-medium text-muted-foreground">Started</th>
                  <th className="text-center px-4 py-3 font-medium text-muted-foreground">Status</th>
                  <th className="text-right px-4 py-3 font-medium text-muted-foreground">Last Payment</th>
                </tr>
              </thead>
              <tbody>
                {loans.map((loan) => (
                  <tr
                    key={loan.id}
                    className="border-b hover:bg-muted/20 cursor-pointer transition-colors"
                    onClick={() => navigate(`/loans/${loan.id}`)}
                  >
                    <td className="px-4 py-3">
                      <div>
                        <div className="flex items-center gap-1.5">
                          <p className="font-medium">{loan.description ?? 'Unnamed Loan'}</p>
                          <Badge
                            variant="outline"
                            className={`text-[10px] px-1.5 py-0 ${
                              loan.direction === 'Lent'
                                ? 'bg-blue-50 text-blue-700 border-blue-200'
                                : 'bg-orange-50 text-orange-700 border-orange-200'
                            }`}
                          >
                            {loan.direction === 'Lent' ? 'Lent' : 'Borrowed'}
                          </Badge>
                        </div>
                        {loan.loanNumber && (
                          <p className="text-xs text-muted-foreground">{loan.loanNumber}</p>
                        )}
                        {loan.isEmiLoan && (
                          <Badge variant="outline" className="text-[10px] px-1.5 py-0 mt-0.5">EMI</Badge>
                        )}
                      </div>
                    </td>
                    <td className="px-4 py-3 text-muted-foreground">{loan.lenderName}</td>
                    <td className="px-4 py-3 text-right">{formatCurrency(loan.principalAmount)}</td>
                    <td className="px-4 py-3 text-right font-semibold text-red-600">
                      {loan.outstandingPrincipal > 0
                        ? formatCurrency(loan.outstandingPrincipal)
                        : <span className="text-emerald-600">Nil</span>}
                    </td>
                    <td className="px-4 py-3 text-right text-amber-700">
                      {formatCurrency(loan.interestAccruedToDate)}
                      <span className="block text-[11px] text-muted-foreground">
                        paid {formatCurrency(loan.totalInterestPaid)}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-right">{formatPercent(loan.currentInterestRate)}</td>
                    <td className="px-4 py-3">{formatDate(loan.loanStartDate)}</td>
                    <td className="px-4 py-3 text-center">
                      <StatusBadge status={loan.statusName} />
                    </td>
                    <td className="px-4 py-3 text-right text-muted-foreground">
                      {loan.lastPaymentDate ? formatDate(loan.lastPaymentDate) : '—'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Card>
      )}

      <Dialog open={showCreate} onOpenChange={setShowCreate}>
        <DialogContent className="max-w-xl">
          <DialogHeader>
            <DialogTitle>Add New Loan</DialogTitle>
          </DialogHeader>
          <LoanForm
            onSuccess={() => {
              setShowCreate(false)
              toast({ title: 'Loan created successfully' })
            }}
            onCancel={() => setShowCreate(false)}
          />
        </DialogContent>
      </Dialog>
    </div>
  )
}
