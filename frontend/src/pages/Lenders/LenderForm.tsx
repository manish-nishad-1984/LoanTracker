import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useCreateLender, useUpdateLender } from '@/hooks/useLenders'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select'
import type { LenderDto, LenderType } from '@/types'

const schema = z.object({
  lenderType: z.enum(['Person', 'Bank', 'Nbfc', 'Cooperative', 'Other']),
  name: z.string().min(1, 'Name is required').max(200),
  contactName: z.string().max(200).optional(),
  phone: z.string().max(20).optional(),
  email: z.string().email('Invalid email').max(200).optional().or(z.literal('')),
  address: z.string().optional(),
  bankName: z.string().max(200).optional(),
  accountNumber: z.string().max(50).optional(),
  ifscCode: z.string().max(20).optional(),
  panNumber: z.string().max(20).optional(),
  notes: z.string().optional(),
  isActive: z.boolean().optional(),
})

type FormData = z.infer<typeof schema>

interface Props {
  lender?: LenderDto
  onSuccess: () => void
  onCancel: () => void
}

export default function LenderForm({ lender, onSuccess, onCancel }: Props) {
  const createLender = useCreateLender()
  const updateLender = useUpdateLender(lender?.id ?? '')

  const form = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      lenderType: (lender?.lenderType as LenderType) ?? 'Person',
      name: lender?.name ?? '',
      contactName: lender?.contactName ?? '',
      phone: lender?.phone ?? '',
      email: lender?.email ?? '',
      address: lender?.address ?? '',
      bankName: lender?.bankName ?? '',
      accountNumber: lender?.accountNumber ?? '',
      ifscCode: lender?.ifscCode ?? '',
      panNumber: lender?.panNumber ?? '',
      notes: lender?.notes ?? '',
      isActive: lender?.isActive ?? true,
    },
  })

  const lenderType = form.watch('lenderType')
  const isBank = lenderType === 'Bank' || lenderType === 'Nbfc' || lenderType === 'Cooperative'

  const onSubmit = (data: FormData) => {
    const payload = {
      ...data,
      email: data.email || undefined,
      isActive: data.isActive ?? true,
    }

    if (lender) {
      updateLender.mutate(payload as any, { onSuccess })
    } else {
      createLender.mutate(payload as any, { onSuccess })
    }
  }

  const isPending = createLender.isPending || updateLender.isPending

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
      {/* Party Type */}
      <div className="grid gap-2">
        <Label>Party Type *</Label>
        <Select
          value={form.watch('lenderType')}
          onValueChange={(v) => form.setValue('lenderType', v as LenderType)}
        >
          <SelectTrigger>
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="Person">Person</SelectItem>
            <SelectItem value="Bank">Bank</SelectItem>
            <SelectItem value="Nbfc">NBFC</SelectItem>
            <SelectItem value="Cooperative">Cooperative</SelectItem>
            <SelectItem value="Other">Other</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {/* Name */}
      <div className="grid gap-2">
        <Label>Name *</Label>
        <Input
          {...form.register('name')}
          placeholder={isBank ? 'e.g. HDFC Bank — Home Loan' : 'e.g. Rajesh Sharma'}
        />
        {form.formState.errors.name && (
          <p className="text-xs text-destructive">{form.formState.errors.name.message}</p>
        )}
      </div>

      {/* Contact / Phone / Email in 2 columns */}
      <div className="grid grid-cols-2 gap-4">
        <div className="grid gap-2">
          <Label>{isBank ? 'Contact Person' : 'Contact Name'}</Label>
          <Input {...form.register('contactName')} placeholder="Optional" />
        </div>
        <div className="grid gap-2">
          <Label>Phone</Label>
          <Input {...form.register('phone')} placeholder="+91-98765-43210" />
        </div>
      </div>

      <div className="grid gap-2">
        <Label>Email</Label>
        <Input {...form.register('email')} type="email" placeholder="email@example.com" />
        {form.formState.errors.email && (
          <p className="text-xs text-destructive">{form.formState.errors.email.message}</p>
        )}
      </div>

      <div className="grid gap-2">
        <Label>Address</Label>
        <Input {...form.register('address')} placeholder="City, State" />
      </div>

      {/* Bank-specific fields */}
      {isBank && (
        <div className="rounded-lg border p-4 space-y-4 bg-muted/30">
          <p className="text-sm font-medium text-muted-foreground">Bank Details</p>
          <div className="grid grid-cols-2 gap-4">
            <div className="grid gap-2">
              <Label>Bank Name</Label>
              <Input {...form.register('bankName')} placeholder="e.g. HDFC Bank Ltd" />
            </div>
            <div className="grid gap-2">
              <Label>Account Number</Label>
              <Input {...form.register('accountNumber')} placeholder="Account number" />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div className="grid gap-2">
              <Label>IFSC Code</Label>
              <Input {...form.register('ifscCode')} placeholder="e.g. HDFC0001234" />
            </div>
            <div className="grid gap-2">
              <Label>PAN Number</Label>
              <Input {...form.register('panNumber')} placeholder="e.g. ABCDE1234F" />
            </div>
          </div>
        </div>
      )}

      <div className="grid gap-2">
        <Label>Notes</Label>
        <Input {...form.register('notes')} placeholder="Any additional notes..." />
      </div>

      <div className="flex justify-end gap-2 pt-2">
        <Button type="button" variant="outline" onClick={onCancel} disabled={isPending}>
          Cancel
        </Button>
        <Button type="submit" disabled={isPending}>
          {isPending ? 'Saving...' : lender ? 'Update Party' : 'Create Party'}
        </Button>
      </div>
    </form>
  )
}
