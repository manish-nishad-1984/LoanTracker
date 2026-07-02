import { useRef, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useQueryClient } from '@tanstack/react-query'
import { Paperclip, Upload } from 'lucide-react'
import { expensesApi, EXPENSE_CATEGORIES, PAYMENT_METHODS, ATTACHMENT_KINDS } from '@/api/expenses'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import AttachmentThumb from '@/components/common/AttachmentThumb'
import { useToast } from '@/hooks/useToast'
import type { Expense } from '@/types'

const schema = z.object({
  date: z.string().min(1, 'Date required'),
  title: z.string().min(1, 'Title required').max(300),
  category: z.string().min(1, 'Category required'),
  amount: z.number({ coerce: true }).positive('Must be greater than 0'),
  paymentMethod: z.string().optional(),
  vendor: z.string().optional(),
  notes: z.string().optional(),
})
type FormData = z.infer<typeof schema>

export default function ExpenseForm({ expense, onSuccess, onCancel }: { expense?: Expense; onSuccess: () => void; onCancel: () => void }) {
  const qc = useQueryClient()
  const { toast } = useToast()
  const fileRef = useRef<HTMLInputElement>(null)
  const [pending, setPending] = useState<File[]>([])
  const [kind, setKind] = useState('Bill')
  const [saving, setSaving] = useState(false)

  const form = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      date: expense?.date ?? new Date().toISOString().split('T')[0],
      title: expense?.title ?? '',
      category: expense?.category ?? 'Other',
      amount: expense?.amount ?? undefined,
      paymentMethod: expense?.paymentMethod ?? 'Cash',
      vendor: expense?.vendor ?? '',
      notes: expense?.notes ?? '',
    },
  })

  const onSubmit = async (data: FormData) => {
    setSaving(true)
    try {
      const payload = { ...data, paymentMethod: data.paymentMethod || undefined, vendor: data.vendor || undefined, notes: data.notes || undefined }
      const saved = expense ? await expensesApi.update(expense.id, payload) : await expensesApi.create(payload)
      // upload any newly selected files
      for (const f of pending) await expensesApi.uploadAttachment(saved.id, f, kind)
      qc.invalidateQueries({ queryKey: ['expenses'] })
      toast({ title: expense ? 'Expense updated' : 'Expense added' })
      onSuccess()
    } catch (e) {
      toast({ title: 'Save failed', description: e instanceof Error ? e.message : '', variant: 'destructive' })
    } finally {
      setSaving(false)
    }
  }

  const deleteAtt = async (id: string) => {
    await expensesApi.deleteAttachment(id)
    qc.invalidateQueries({ queryKey: ['expenses'] })
    toast({ title: 'Attachment removed' })
  }

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-3">
      <div className="grid grid-cols-2 gap-3">
        <div className="grid gap-1">
          <Label>Date *</Label>
          <Input type="date" {...form.register('date')} />
        </div>
        <div className="grid gap-1">
          <Label>Amount *</Label>
          <Input type="number" step="0.01" placeholder="0.00" {...form.register('amount')} />
          {form.formState.errors.amount && <p className="text-xs text-destructive">{form.formState.errors.amount.message}</p>}
        </div>
      </div>

      <div className="grid gap-1">
        <Label>Title / What was it? *</Label>
        <Input placeholder="e.g. Monthly groceries at DMart" {...form.register('title')} />
        {form.formState.errors.title && <p className="text-xs text-destructive">{form.formState.errors.title.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div className="grid gap-1">
          <Label>Category *</Label>
          <Select value={form.watch('category')} onValueChange={(v) => form.setValue('category', v)}>
            <SelectTrigger><SelectValue /></SelectTrigger>
            <SelectContent className="max-h-64">
              {EXPENSE_CATEGORIES.map((c) => <SelectItem key={c} value={c}>{c}</SelectItem>)}
            </SelectContent>
          </Select>
        </div>
        <div className="grid gap-1">
          <Label>Payment Method</Label>
          <Select value={form.watch('paymentMethod')} onValueChange={(v) => form.setValue('paymentMethod', v)}>
            <SelectTrigger><SelectValue /></SelectTrigger>
            <SelectContent>{PAYMENT_METHODS.map((m) => <SelectItem key={m} value={m}>{m}</SelectItem>)}</SelectContent>
          </Select>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div className="grid gap-1">
          <Label>Vendor / Shop</Label>
          <Input placeholder="Optional" {...form.register('vendor')} />
        </div>
        <div className="grid gap-1">
          <Label>Notes</Label>
          <Input placeholder="Optional" {...form.register('notes')} />
        </div>
      </div>

      {/* Attachments */}
      <div className="rounded-lg border p-3 space-y-2">
        <div className="flex items-center justify-between">
          <Label className="flex items-center gap-1.5"><Paperclip className="h-3.5 w-3.5" /> Bill / Invoice / Product Photos</Label>
          <div className="flex items-center gap-2">
            <Select value={kind} onValueChange={setKind}>
              <SelectTrigger className="h-8 w-28"><SelectValue /></SelectTrigger>
              <SelectContent>{ATTACHMENT_KINDS.map((k) => <SelectItem key={k} value={k}>{k}</SelectItem>)}</SelectContent>
            </Select>
            <Button type="button" variant="outline" size="sm" onClick={() => fileRef.current?.click()}>
              <Upload className="h-3.5 w-3.5" /> Add file
            </Button>
            <input ref={fileRef} type="file" accept="image/*,application/pdf" multiple className="hidden"
              onChange={(e) => { setPending((p) => [...p, ...Array.from(e.target.files ?? [])]); e.target.value = '' }} />
          </div>
        </div>
        <div className="flex flex-wrap gap-2">
          {expense?.attachments.map((a) => <AttachmentThumb key={a.id} att={a} onDelete={() => deleteAtt(a.id)} />)}
          {pending.map((f, i) => (
            <div key={i} className="relative">
              <div className="flex h-20 w-20 flex-col items-center justify-center rounded-md border border-dashed bg-blue-50 text-blue-600">
                <Upload className="h-5 w-5" />
                <span className="mt-1 max-w-[70px] truncate text-[9px]">{f.name}</span>
              </div>
              <Badge variant="outline" className="absolute left-0.5 top-0.5 bg-white/90 px-1 py-0 text-[8px]">{kind}</Badge>
            </div>
          ))}
          {(expense?.attachments.length ?? 0) === 0 && pending.length === 0 && (
            <p className="text-xs text-slate-400 py-3">No files yet — attach a photo of the bill or product.</p>
          )}
        </div>
      </div>

      <div className="flex justify-end gap-2 pt-1">
        <Button type="button" variant="outline" onClick={onCancel} disabled={saving}>Cancel</Button>
        <Button type="submit" disabled={saving}>{saving ? 'Saving…' : expense ? 'Update Expense' : 'Add Expense'}</Button>
      </div>
    </form>
  )
}
