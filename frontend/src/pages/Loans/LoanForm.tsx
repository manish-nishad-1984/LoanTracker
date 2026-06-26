import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useCreateLoan, useUpdateLoan } from '@/hooks/useLoans'
import { useLenders } from '@/hooks/useLenders'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import type { LoanDto, InterestCalculationType, PaymentFrequency, LoanDirection } from '@/types'

const schema = z.object({
  lenderId: z.string().min(1, 'Counterparty is required'),
  direction: z.enum(['Borrowed', 'Lent']),
  loanNumber: z.string().max(100).optional(),
  description: z.string().max(500).optional(),
  principalAmount: z.number({ coerce: true }).positive('Must be greater than 0'),
  currentInterestRate: z.number({ coerce: true }).min(0).max(100),
  interestCalculationType: z.enum(['Simple', 'Compound', 'Flat', 'ReducingBalance', 'None']),
  paymentFrequency: z.enum(['Daily', 'Weekly', 'Fortnightly', 'Monthly', 'Quarterly', 'HalfYearly', 'Yearly', 'LumpSum', 'Irregular']),
  isEmiLoan: z.boolean(),
  emiAmount: z.number({ coerce: true }).positive().optional(),
  loanStartDate: z.string().min(1, 'Start date required'),
  loanEndDate: z.string().optional(),
  notes: z.string().optional(),
}).refine(d => !d.isEmiLoan || (d.emiAmount && d.emiAmount > 0), {
  message: 'EMI amount required for EMI loans',
  path: ['emiAmount'],
})

type FormData = z.infer<typeof schema>

interface Props {
  loan?: LoanDto
  defaultLenderId?: string
  onSuccess: () => void
  onCancel: () => void
}

export default function LoanForm({ loan, defaultLenderId, onSuccess, onCancel }: Props) {
  const createLoan = useCreateLoan()
  const updateLoan = useUpdateLoan(loan?.id ?? '')
  const { data: lendersData } = useLenders({ pageSize: 100 })
  const lenders = lendersData?.items ?? []

  const form = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      lenderId: loan?.lenderId ?? defaultLenderId ?? '',
      direction: (loan?.direction as LoanDirection) ?? 'Borrowed',
      loanNumber: loan?.loanNumber ?? '',
      description: loan?.description ?? '',
      principalAmount: loan?.principalAmount ?? undefined,
      currentInterestRate: loan?.currentInterestRate ?? 0,
      interestCalculationType: (loan?.interestCalculationType as InterestCalculationType) ?? 'Simple',
      paymentFrequency: (loan?.paymentFrequency as PaymentFrequency) ?? 'Monthly',
      isEmiLoan: loan?.isEmiLoan ?? false,
      emiAmount: loan?.emiAmount ?? undefined,
      loanStartDate: loan?.loanStartDate ?? new Date().toISOString().split('T')[0],
      loanEndDate: loan?.loanEndDate ?? '',
      notes: loan?.notes ?? '',
    },
  })

  const isEmi = form.watch('isEmiLoan')
  const isPending = createLoan.isPending || updateLoan.isPending

  const onSubmit = (data: FormData) => {
    const payload = {
      ...data,
      loanNumber: data.loanNumber || undefined,
      description: data.description || undefined,
      loanEndDate: data.loanEndDate || undefined,
      notes: data.notes || undefined,
      emiAmount: data.isEmiLoan ? data.emiAmount : undefined,
    }

    if (loan) {
      const { lenderId, principalAmount, loanStartDate, ...updatePayload } = payload
      updateLoan.mutate(updatePayload as any, { onSuccess })
    } else {
      createLoan.mutate(payload as any, { onSuccess })
    }
  }

  const direction = form.watch('direction')

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-3">
      {/* Direction — Borrowed vs Lent */}
      <div className="grid grid-cols-2 gap-2">
        <button
          type="button"
          disabled={!!loan}
          onClick={() => form.setValue('direction', 'Borrowed')}
          className={`rounded-lg border px-3 py-2 text-left text-sm transition-colors ${
            direction === 'Borrowed' ? 'border-primary bg-primary/5 ring-1 ring-primary' : 'hover:bg-muted/50'
          } ${loan ? 'opacity-60 cursor-not-allowed' : ''}`}
        >
          <span className="font-medium">📥 Borrowed</span>
          <span className="block text-xs text-muted-foreground">I owe them</span>
        </button>
        <button
          type="button"
          disabled={!!loan}
          onClick={() => form.setValue('direction', 'Lent')}
          className={`rounded-lg border px-3 py-2 text-left text-sm transition-colors ${
            direction === 'Lent' ? 'border-primary bg-primary/5 ring-1 ring-primary' : 'hover:bg-muted/50'
          } ${loan ? 'opacity-60 cursor-not-allowed' : ''}`}
        >
          <span className="font-medium">📤 Lent</span>
          <span className="block text-xs text-muted-foreground">They owe me</span>
        </button>
      </div>

      {/* EMI toggle */}
      <label className="flex items-center gap-2 rounded-md border px-3 py-1.5 text-sm cursor-pointer bg-muted/30">
        <input
          type="checkbox"
          checked={isEmi}
          onChange={(e) => form.setValue('isEmiLoan', e.target.checked)}
          className="h-4 w-4"
        />
        EMI-based loan (fixed installments)
      </label>

      {/* Fields — compact 2-column grid */}
      <div className="grid grid-cols-2 gap-x-3 gap-y-2.5">
        {!loan && (
          <div className="col-span-2 grid gap-1">
            <Label>Party *</Label>
            <Select
              value={form.watch('lenderId')}
              onValueChange={(v) => form.setValue('lenderId', v)}
              disabled={!!defaultLenderId}
            >
              <SelectTrigger>
                <SelectValue placeholder={direction === 'Lent' ? 'Who did you lend to?' : 'Who did you borrow from?'} />
              </SelectTrigger>
              <SelectContent>
                {lenders.map((l) => (
                  <SelectItem key={l.id} value={l.id}>{l.name}</SelectItem>
                ))}
              </SelectContent>
            </Select>
            {form.formState.errors.lenderId && (
              <p className="text-xs text-destructive">{form.formState.errors.lenderId.message}</p>
            )}
          </div>
        )}

        <div className="grid gap-1">
          <Label>Loan Number / Ref</Label>
          <Input {...form.register('loanNumber')} placeholder="Optional" />
        </div>
        <div className="grid gap-1">
          <Label>Principal Amount *</Label>
          <Input {...form.register('principalAmount')} type="number" step="0.01" placeholder="0.00" disabled={!!loan} />
          {form.formState.errors.principalAmount && (
            <p className="text-xs text-destructive">{form.formState.errors.principalAmount.message}</p>
          )}
        </div>

        <div className="col-span-2 grid gap-1">
          <Label>Description</Label>
          <Input {...form.register('description')} placeholder="Purpose of this loan..." />
        </div>

        <div className="grid gap-1">
          <Label>Interest Rate (% p.a.) *</Label>
          <Input {...form.register('currentInterestRate')} type="number" step="0.0001" placeholder="0.00" />
        </div>
        <div className="grid gap-1">
          <Label>Interest Type *</Label>
          <Select
            value={form.watch('interestCalculationType')}
            onValueChange={(v) => form.setValue('interestCalculationType', v as InterestCalculationType)}
          >
            <SelectTrigger><SelectValue /></SelectTrigger>
            <SelectContent>
              <SelectItem value="Simple">Simple Interest</SelectItem>
              <SelectItem value="ReducingBalance">Reducing Balance (EMI)</SelectItem>
              <SelectItem value="Flat">Flat Rate</SelectItem>
              <SelectItem value="Compound">Compound</SelectItem>
              <SelectItem value="None">None (0% Interest)</SelectItem>
            </SelectContent>
          </Select>
        </div>

        <div className="grid gap-1">
          <Label>Payment Frequency *</Label>
          <Select
            value={form.watch('paymentFrequency')}
            onValueChange={(v) => form.setValue('paymentFrequency', v as PaymentFrequency)}
          >
            <SelectTrigger><SelectValue /></SelectTrigger>
            <SelectContent>
              <SelectItem value="Monthly">Monthly</SelectItem>
              <SelectItem value="Quarterly">Quarterly</SelectItem>
              <SelectItem value="HalfYearly">Half Yearly</SelectItem>
              <SelectItem value="Yearly">Yearly</SelectItem>
              <SelectItem value="LumpSum">Lump Sum</SelectItem>
              <SelectItem value="Irregular">Irregular</SelectItem>
              <SelectItem value="Weekly">Weekly</SelectItem>
              <SelectItem value="Fortnightly">Fortnightly</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="grid gap-1">
          <Label>Loan Start Date *</Label>
          <Input {...form.register('loanStartDate')} type="date" disabled={!!loan} />
        </div>

        {isEmi && (
          <div className="grid gap-1">
            <Label>EMI Amount *</Label>
            <Input {...form.register('emiAmount')} type="number" step="0.01" placeholder="Monthly EMI" />
            {form.formState.errors.emiAmount && (
              <p className="text-xs text-destructive">{form.formState.errors.emiAmount.message}</p>
            )}
          </div>
        )}
        <div className={isEmi ? 'grid gap-1' : 'col-span-2 grid gap-1'}>
          <Label>Expected End Date</Label>
          <Input {...form.register('loanEndDate')} type="date" />
        </div>

        <div className="col-span-2 grid gap-1">
          <Label>Notes</Label>
          <Input {...form.register('notes')} placeholder="Any remarks..." />
        </div>
      </div>

      <div className="flex justify-end gap-2 pt-1">
        <Button type="button" variant="outline" onClick={onCancel} disabled={isPending}>Cancel</Button>
        <Button type="submit" disabled={isPending}>
          {isPending ? 'Saving...' : loan ? 'Update Loan' : 'Create Loan'}
        </Button>
      </div>
    </form>
  )
}
