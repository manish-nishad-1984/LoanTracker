import { useState } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { useLender } from '@/hooks/useLenders'
import { useLoans } from '@/hooks/useLoans'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import {
  ArrowLeft, Edit, Phone, Mail, MapPin, CreditCard,
  TrendingDown, ArrowUpCircle, Plus
} from 'lucide-react'
import StatusBadge from '@/components/common/StatusBadge'
import { formatCurrency, formatDate, lenderTypeIcon } from '@/lib/utils'
import LenderForm from './LenderForm'
import LoanForm from '../Loans/LoanForm'
import { useToast } from '@/hooks/useToast'

export default function LenderDetail() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { toast } = useToast()
  const [showEdit, setShowEdit] = useState(false)
  const [showAddLoan, setShowAddLoan] = useState(false)

  const { data: lender, isLoading } = useLender(id!)
  const { data: loansData } = useLoans({ lenderId: id!, pageSize: 50 })
  const loans = loansData?.items ?? []

  if (isLoading) {
    return <div className="h-64 rounded-lg bg-muted animate-pulse" />
  }

  if (!lender) {
    return (
      <div className="text-center py-16">
        <p className="text-muted-foreground">Lender not found.</p>
        <Button variant="outline" className="mt-4" onClick={() => navigate('/lenders')}>
          Back to Lenders
        </Button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Back + Actions */}
      <div className="flex items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <Button variant="ghost" size="sm" onClick={() => navigate('/lenders')}>
            <ArrowLeft className="h-4 w-4" />
          </Button>
          <div>
            <h2 className="text-xl font-bold flex items-center gap-2">
              <span>{lenderTypeIcon(lender.lenderType)}</span>
              {lender.name}
            </h2>
            <p className="text-sm text-muted-foreground">{lender.lenderTypeName}</p>
          </div>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" size="sm" onClick={() => setShowEdit(true)}>
            <Edit className="h-4 w-4" />
            Edit
          </Button>
          <Button size="sm" onClick={() => setShowAddLoan(true)}>
            <Plus className="h-4 w-4" />
            Add Loan
          </Button>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid gap-4 grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-2 mb-1">
              <CreditCard className="h-4 w-4 text-muted-foreground" />
              <p className="text-xs text-muted-foreground">Total Loans</p>
            </div>
            <p className="text-2xl font-bold">{lender.totalLoans}</p>
            <p className="text-xs text-muted-foreground">{lender.activeLoans} active</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-2 mb-1">
              <TrendingDown className="h-4 w-4 text-red-500" />
              <p className="text-xs text-muted-foreground">Outstanding</p>
            </div>
            <p className="text-2xl font-bold text-red-600">{formatCurrency(lender.totalOutstanding)}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-2 mb-1">
              <ArrowUpCircle className="h-4 w-4 text-emerald-500" />
              <p className="text-xs text-muted-foreground">Total Borrowed</p>
            </div>
            <p className="text-2xl font-bold">{formatCurrency(lender.totalBorrowed)}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-2 mb-1">
              <Badge variant={lender.isActive ? 'default' : 'secondary'}>
                {lender.isActive ? 'Active' : 'Inactive'}
              </Badge>
            </div>
            <p className="text-xs text-muted-foreground mt-2">Since {formatDate(lender.createdAt)}</p>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Contact Info */}
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="text-base">Contact Information</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            {lender.phone && (
              <div className="flex items-center gap-2 text-sm">
                <Phone className="h-4 w-4 text-muted-foreground shrink-0" />
                <span>{lender.phone}</span>
              </div>
            )}
            {lender.email && (
              <div className="flex items-center gap-2 text-sm">
                <Mail className="h-4 w-4 text-muted-foreground shrink-0" />
                <span className="break-all">{lender.email}</span>
              </div>
            )}
            {lender.address && (
              <div className="flex items-start gap-2 text-sm">
                <MapPin className="h-4 w-4 text-muted-foreground shrink-0 mt-0.5" />
                <span>{lender.address}</span>
              </div>
            )}
            {lender.bankName && (
              <div className="border-t pt-3 space-y-1.5">
                <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Bank Details</p>
                {lender.bankName && <p className="text-sm">{lender.bankName}</p>}
                {lender.accountNumber && (
                  <p className="text-sm text-muted-foreground">A/C: {lender.accountNumber}</p>
                )}
                {lender.ifscCode && (
                  <p className="text-sm text-muted-foreground">IFSC: {lender.ifscCode}</p>
                )}
              </div>
            )}
            {lender.notes && (
              <div className="border-t pt-3">
                <p className="text-xs text-muted-foreground">{lender.notes}</p>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Loans */}
        <div className="lg:col-span-2">
          <Card>
            <CardHeader className="pb-3">
              <div className="flex items-center justify-between">
                <CardTitle className="text-base">Loans from {lender.name}</CardTitle>
                <Button size="sm" variant="outline" onClick={() => setShowAddLoan(true)}>
                  <Plus className="h-3.5 w-3.5" />
                  Add
                </Button>
              </div>
            </CardHeader>
            <CardContent className="p-0">
              {loans.length === 0 ? (
                <div className="py-8 text-center text-sm text-muted-foreground">
                  No loans from this lender yet.
                </div>
              ) : (
                <div className="divide-y">
                  {loans.map((loan) => (
                    <Link
                      key={loan.id}
                      to={`/loans/${loan.id}`}
                      className="flex items-center justify-between px-4 py-3 hover:bg-muted/20 transition-colors"
                    >
                      <div className="min-w-0 flex-1">
                        <div className="flex items-center gap-2">
                          <p className="text-sm font-medium truncate">
                            {loan.description ?? loan.loanNumber ?? 'Loan'}
                          </p>
                          <StatusBadge status={loan.statusName} />
                        </div>
                        <p className="text-xs text-muted-foreground">
                          {loan.currentInterestRate}% · Started {formatDate(loan.loanStartDate)}
                          {loan.isEmiLoan && ' · EMI'}
                        </p>
                      </div>
                      <div className="text-right ml-4 shrink-0">
                        <p className="text-sm font-bold text-red-600">
                          {formatCurrency(loan.outstandingPrincipal)}
                        </p>
                        <p className="text-xs text-muted-foreground">outstanding</p>
                      </div>
                    </Link>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Edit Dialog */}
      <Dialog open={showEdit} onOpenChange={setShowEdit}>
        <DialogContent className="max-w-xl">
          <DialogHeader>
            <DialogTitle>Edit Lender</DialogTitle>
          </DialogHeader>
          <LenderForm
            lender={lender}
            onSuccess={() => {
              setShowEdit(false)
              toast({ title: 'Lender updated' })
            }}
            onCancel={() => setShowEdit(false)}
          />
        </DialogContent>
      </Dialog>

      {/* Add Loan Dialog */}
      <Dialog open={showAddLoan} onOpenChange={setShowAddLoan}>
        <DialogContent className="max-w-xl">
          <DialogHeader>
            <DialogTitle>Add Loan from {lender.name}</DialogTitle>
          </DialogHeader>
          <LoanForm
            defaultLenderId={lender.id}
            onSuccess={() => {
              setShowAddLoan(false)
              toast({ title: 'Loan created' })
            }}
            onCancel={() => setShowAddLoan(false)}
          />
        </DialogContent>
      </Dialog>
    </div>
  )
}
