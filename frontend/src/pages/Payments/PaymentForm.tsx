import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useCreatePayment } from '@/hooks/usePayments'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { formatCurrency } from '@/lib/utils'
import type { PaymentMode } from '@/types'

const schema = z.object({
  paymentDate: z.string().min(1, 'Date required'),
  principalAmount: z.number({ coerce: true }).min(0),
  interestAmount: z.number({ coerce: true }).min(0),
  penaltyAmount: z.number({ coerce: true }).min(0),
  paymentMode: z.enum(['Cash', 'BankTransfer', 'Upi', 'Cheque', 'DemandDraft', 'Online', 'Other']),
  referenceNumber: z.string().max(100).optional(),
  remarks: z.string().optional(),
}).refine(d => d.principalAmount + d.interestAmount + d.penaltyAmount > 0, {
  message: 'At least one amount must be greater than zero',
  path: ['principalAmount'],
})

type FormData = z.infer<typeof schema>

interface Props {
  loanId: string
  outstandingPrincipal: number
  onSuccess: () => void
  onCancel: () => void
}

export default function PaymentForm({ loanId, outstandingPrincipal, onSuccess, onCancel }: Props) {
  const createPayment = useCreatePayment()

  const form = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      paymentDate: new Date().toISOString().split('T')[0],
      principalAmount: 0,
      interestAmount: 0,
      penaltyAmount: 0,
      paymentMode: 'Cash',
    },
  })

  const principal = form.watch('principalAmount') || 0
  const interest = form.watch('interestAmount') || 0
  const penalty = form.watch('penaltyAmount') || 0
  const total = Number(principal) + Number(interest) + Number(penalty)

  const onSubmit = (data: FormData) => {
    createPayment.mutate(
      {
        loanId,
        ...data,
        totalAmount: total,
        referenceNumber: data.referenceNumber || undefined,
        remarks: data.remarks || undefined,
      },
      { onSuccess }
    )
  }

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
      {/* Outstanding info */}
      <div className="rounded-lg bg-muted/50 px-4 py-3 text-sm">
        <p className="text-muted-foreground">Outstanding Principal</p>
        <p className="text-lg font-bold text-red-600">{formatCurrency(outstandingPrincipal)}</p>
      </div>

      <div className="grid gap-2">
        <Label>Payment Date *</Label>
        <Input {...form.register('paymentDate')} type="date" />
        {form.formState.errors.paymentDate && (
          <p className="text-xs text-destructive">{form.formState.errors.paymentDate.message}</p>
        )}
      </div>

      {/* Amount splits */}
      <div className="rounded-lg border p-4 space-y-3">
        <p className="text-sm font-medium">Payment Breakdown</p>
        <p className="text-xs text-muted-foreground">
          Enter the amounts for each component. Leave 0 if not applicable.
        </p>

        <div className="grid grid-cols-3 gap-3">
          <div className="grid gap-1.5">
            <Label className="text-xs text-emerald-700">Principal</Label>
            <Input
              {...form.register('principalAmount')}
              type="number"
              step="0.01"
              min="0"
              max={outstandingPrincipal}
              placeholder="0.00"
            />
          </div>
          <div className="grid gap-1.5">
            <Label className="text-xs text-amber-700">Interest</Label>
            <Input
              {...form.register('interestAmount')}
              type="number"
              step="0.01"
              min="0"
              placeholder="0.00"
            />
          </div>
          <div className="grid gap-1.5">
            <Label className="text-xs text-red-700">Penalty</Label>
            <Input
              {...form.register('penaltyAmount')}
              type="number"
              step="0.01"
              min="0"
              placeholder="0.00"
            />
          </div>
        </div>

        {/* Total */}
        <div className="flex items-center justify-between border-t pt-3">
          <span className="text-sm text-muted-foreground">Total Payment</span>
          <span className="text-lg font-bold">{formatCurrency(total)}</span>
        </div>

        {form.formState.errors.principalAmount && (
          <p className="text-xs text-destructive">{form.formState.errors.principalAmount.message}</p>
        )}
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="grid gap-2">
          <Label>Payment Mode *</Label>
          <Select
            value={form.watch('paymentMode')}
            onValueChange={(v) => form.setValue('paymentMode', v as PaymentMode)}
          >
            <SelectTrigger><SelectValue /></SelectTrigger>
            <SelectContent>
              <SelectItem value="Cash">Cash</SelectItem>
              <SelectItem value="BankTransfer">Bank Transfer</SelectItem>
              <SelectItem value="Upi">UPI</SelectItem>
              <SelectItem value="Cheque">Cheque</SelectItem>
              <SelectItem value="DemandDraft">Demand Draft</SelectItem>
              <SelectItem value="Online">Online</SelectItem>
              <SelectItem value="Other">Other</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="grid gap-2">
          <Label>Reference / Txn No.</Label>
          <Input {...form.register('referenceNumber')} placeholder="Optional" />
        </div>
      </div>

      <div className="grid gap-2">
        <Label>Remarks</Label>
        <Input {...form.register('remarks')} placeholder="Any notes about this payment..." />
      </div>

      <div className="flex justify-end gap-2 pt-2">
        <Button type="button" variant="outline" onClick={onCancel} disabled={createPayment.isPending}>
          Cancel
        </Button>
        <Button type="submit" disabled={createPayment.isPending || total <= 0}>
          {createPayment.isPending ? 'Recording...' : `Record Payment of ${formatCurrency(total)}`}
        </Button>
      </div>

      {createPayment.isError && (
        <p className="text-xs text-destructive text-center">
          {createPayment.error?.message ?? 'Failed to record payment'}
        </p>
      )}
    </form>
  )
}
