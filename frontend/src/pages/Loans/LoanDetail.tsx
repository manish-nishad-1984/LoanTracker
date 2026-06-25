import { useState } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { useLoan, useCloseLoan } from '@/hooks/useLoans'
import { usePaymentsByLoan, useDeletePayment } from '@/hooks/usePayments'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import {
  ArrowLeft, Edit, Plus, TrendingDown, ArrowUpCircle,
  Receipt, Trash2, CheckCircle2, CreditCard, Percent, Clock
} from 'lucide-react'
import StatusBadge from '@/components/common/StatusBadge'
import { formatCurrency, formatDate, formatPercent, paymentTypeLabel } from '@/lib/utils'
import LoanForm from './LoanForm'
import PaymentForm from '../Payments/PaymentForm'
import { useToast } from '@/hooks/useToast'

export default function LoanDetail() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { toast } = useToast()

  const [showEdit, setShowEdit] = useState(false)
  const [showPayment, setShowPayment] = useState(false)

  const { data: loan, isLoading } = useLoan(id!)
  const { data: paymentsData } = usePaymentsByLoan(id!)
  const payments = paymentsData?.items ?? []

  const closeLoan = useCloseLoan(id!)
  const deletePayment = useDeletePayment()

  if (isLoading) {
    return <div className="space-y-4">
      {Array.from({ length: 3 }).map((_, i) => (
        <div key={i} className="h-40 rounded-lg bg-muted animate-pulse" />
      ))}
    </div>
  }

  if (!loan) {
    return (
      <div className="text-center py-16">
        <p className="text-muted-foreground">Loan not found.</p>
        <Button variant="outline" className="mt-4" onClick={() => navigate('/loans')}>Back</Button>
      </div>
    )
  }

  const progressPct = loan.principalAmount > 0
    ? Math.min(100, (loan.totalPrincipalPaid / loan.principalAmount) * 100)
    : 0

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-center gap-3">
          <Button variant="ghost" size="sm" onClick={() => navigate('/loans')}>
            <ArrowLeft className="h-4 w-4" />
          </Button>
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-xl font-bold">{loan.description ?? loan.loanNumber ?? 'Loan'}</h2>
              <StatusBadge status={loan.statusName} />
              <Badge
                variant="outline"
                className={
                  loan.direction === 'Lent'
                    ? 'bg-blue-50 text-blue-700 border-blue-200'
                    : 'bg-orange-50 text-orange-700 border-orange-200'
                }
              >
                {loan.direction === 'Lent' ? '📤 Lent (owed to me)' : '📥 Borrowed (I owe)'}
              </Badge>
            </div>
            <div className="flex items-center gap-2 text-sm text-muted-foreground">
              <Link to={`/lenders/${loan.lenderId}`} className="hover:underline">{loan.lenderName}</Link>
              <span>·</span>
              <span>{formatPercent(loan.currentInterestRate)} {loan.interestCalculationTypeName}</span>
              {loan.isEmiLoan && <Badge variant="outline" className="text-xs">EMI</Badge>}
            </div>
          </div>
        </div>
        <div className="flex gap-2 shrink-0">
          {loan.status === 'Active' && (
            <>
              <Button variant="outline" size="sm" onClick={() => setShowEdit(true)}>
                <Edit className="h-4 w-4" />
                Edit
              </Button>
              <Button size="sm" onClick={() => setShowPayment(true)}>
                <Plus className="h-4 w-4" />
                Add Payment
              </Button>
            </>
          )}
          {loan.status === 'Active' && loan.outstandingPrincipal === 0 && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => closeLoan.mutate(
                { closureDate: new Date().toISOString().split('T')[0] },
                { onSuccess: () => toast({ title: 'Loan closed' }) }
              )}
            >
              <CheckCircle2 className="h-4 w-4" />
              Close Loan
            </Button>
          )}
        </div>
      </div>

      {/* Financial Summary */}
      <div className="grid gap-4 grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-2 mb-1">
              <CreditCard className="h-4 w-4 text-muted-foreground" />
              <p className="text-xs text-muted-foreground">Original Principal</p>
            </div>
            <p className="text-xl font-bold">{formatCurrency(loan.principalAmount)}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-2 mb-1">
              <TrendingDown className="h-4 w-4 text-red-500" />
              <p className="text-xs text-muted-foreground">Outstanding</p>
            </div>
            <p className="text-xl font-bold text-red-600">{formatCurrency(loan.outstandingPrincipal)}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-2 mb-1">
              <ArrowUpCircle className="h-4 w-4 text-emerald-500" />
              <p className="text-xs text-muted-foreground">Principal Paid</p>
            </div>
            <p className="text-xl font-bold text-emerald-600">{formatCurrency(loan.totalPrincipalPaid)}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-2 mb-1">
              <Receipt className="h-4 w-4 text-amber-500" />
              <p className="text-xs text-muted-foreground">Interest Paid</p>
            </div>
            <p className="text-xl font-bold text-amber-600">{formatCurrency(loan.totalInterestPaid)}</p>
          </CardContent>
        </Card>
      </div>

      {/* Interest accrued to date */}
      <div className="grid gap-4 grid-cols-1 sm:grid-cols-3">
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-2 mb-1">
              <Percent className="h-4 w-4 text-amber-500" />
              <p className="text-xs text-muted-foreground">Interest Accrued (To Date)</p>
            </div>
            <p className="text-xl font-bold">{formatCurrency(loan.interestAccruedToDate)}</p>
            <p className="text-xs text-muted-foreground mt-0.5">Built up since {formatDate(loan.loanStartDate)}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-2 mb-1">
              <Receipt className="h-4 w-4 text-emerald-500" />
              <p className="text-xs text-muted-foreground">Interest Paid (To Date)</p>
            </div>
            <p className="text-xl font-bold text-emerald-600">{formatCurrency(loan.totalInterestPaid)}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-2 mb-1">
              <Clock className="h-4 w-4 text-red-500" />
              <p className="text-xs text-muted-foreground">Interest Outstanding (Pending)</p>
            </div>
            <p className="text-xl font-bold text-red-600">{formatCurrency(loan.interestOutstanding)}</p>
          </CardContent>
        </Card>
      </div>

      {/* Repayment Progress */}
      <Card>
        <CardContent className="p-4">
          <div className="flex items-center justify-between mb-2">
            <p className="text-sm font-medium">Repayment Progress</p>
            <p className="text-sm font-bold">{progressPct.toFixed(1)}% repaid</p>
          </div>
          <div className="h-3 w-full rounded-full bg-muted overflow-hidden">
            <div
              className="h-full rounded-full bg-emerald-500 transition-all"
              style={{ width: `${progressPct}%` }}
            />
          </div>
          <div className="flex justify-between mt-1.5 text-xs text-muted-foreground">
            <span>Paid: {formatCurrency(loan.totalPrincipalPaid)}</span>
            <span>Remaining: {formatCurrency(loan.outstandingPrincipal)}</span>
          </div>
        </CardContent>
      </Card>

      {/* Loan Details + Payment History */}
      <div className="grid gap-6 lg:grid-cols-3">
        {/* Loan Details */}
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="text-base">Loan Details</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3 text-sm">
            {[
              ['Loan Number', loan.loanNumber ?? '—'],
              ['Interest Rate', formatPercent(loan.currentInterestRate)],
              ['Interest Type', loan.interestCalculationTypeName],
              ['Payment Frequency', loan.paymentFrequencyName],
              ['Start Date', formatDate(loan.loanStartDate)],
              ['End Date', loan.loanEndDate ? formatDate(loan.loanEndDate) : '—'],
              ['EMI Amount', loan.isEmiLoan && loan.emiAmount ? formatCurrency(loan.emiAmount) : '—'],
              ['Total Paid (incl. interest)', formatCurrency(loan.totalAmountPaid)],
              ['Closure Date', loan.closureDate ? formatDate(loan.closureDate) : '—'],
            ].map(([label, value]) => (
              <div key={label} className="flex justify-between">
                <span className="text-muted-foreground">{label}</span>
                <span className="font-medium text-right ml-4">{value}</span>
              </div>
            ))}
            {loan.notes && (
              <div className="border-t pt-3">
                <p className="text-xs text-muted-foreground">{loan.notes}</p>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Payment History */}
        <div className="lg:col-span-2">
          <Card>
            <CardHeader className="pb-3">
              <div className="flex items-center justify-between">
                <CardTitle className="text-base">Payment History ({payments.length})</CardTitle>
                {loan.status === 'Active' && (
                  <Button size="sm" onClick={() => setShowPayment(true)}>
                    <Plus className="h-3.5 w-3.5" />
                    Record Payment
                  </Button>
                )}
              </div>
            </CardHeader>
            <CardContent className="p-0">
              {payments.length === 0 ? (
                <p className="px-4 py-8 text-center text-sm text-muted-foreground">
                  No payments recorded yet.
                </p>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b bg-muted/30">
                        <th className="text-left px-4 py-2.5 font-medium text-muted-foreground">Date</th>
                        <th className="text-right px-4 py-2.5 font-medium text-muted-foreground">Total</th>
                        <th className="text-right px-4 py-2.5 font-medium text-muted-foreground">Principal</th>
                        <th className="text-right px-4 py-2.5 font-medium text-muted-foreground">Interest</th>
                        <th className="text-left px-4 py-2.5 font-medium text-muted-foreground">Type</th>
                        <th className="text-left px-4 py-2.5 font-medium text-muted-foreground">Mode</th>
                        <th className="px-4 py-2.5" />
                      </tr>
                    </thead>
                    <tbody>
                      {payments.map((p) => (
                        <tr key={p.id} className="border-b hover:bg-muted/20">
                          <td className="px-4 py-2.5">{formatDate(p.paymentDate)}</td>
                          <td className="px-4 py-2.5 text-right font-semibold">
                            {formatCurrency(p.totalAmount)}
                          </td>
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
                          <td className="px-4 py-2.5 text-muted-foreground">
                            {p.paymentModeName}
                          </td>
                          <td className="px-4 py-2.5">
                            {loan.status === 'Active' && (
                              <Button
                                variant="ghost"
                                size="icon"
                                className="h-7 w-7 text-muted-foreground hover:text-destructive"
                                onClick={() => deletePayment.mutate(p.id, {
                                  onSuccess: () => toast({ title: 'Payment deleted' }),
                                })}
                              >
                                <Trash2 className="h-3.5 w-3.5" />
                              </Button>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Edit Dialog */}
      <Dialog open={showEdit} onOpenChange={setShowEdit}>
        <DialogContent className="max-w-xl">
          <DialogHeader><DialogTitle>Edit Loan</DialogTitle></DialogHeader>
          <LoanForm
            loan={loan}
            onSuccess={() => { setShowEdit(false); toast({ title: 'Loan updated' }) }}
            onCancel={() => setShowEdit(false)}
          />
        </DialogContent>
      </Dialog>

      {/* Payment Dialog */}
      <Dialog open={showPayment} onOpenChange={setShowPayment}>
        <DialogContent className="max-w-lg">
          <DialogHeader><DialogTitle>Record Payment</DialogTitle></DialogHeader>
          <PaymentForm
            loanId={loan.id}
            outstandingPrincipal={loan.outstandingPrincipal}
            onSuccess={() => { setShowPayment(false); toast({ title: 'Payment recorded' }) }}
            onCancel={() => setShowPayment(false)}
          />
        </DialogContent>
      </Dialog>
    </div>
  )
}
