import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation } from '@tanstack/react-query'
import { User, Lock, IdCard, CheckCircle2, AlertCircle } from 'lucide-react'
import { authApi } from '@/api/auth'
import { useAuth } from '@/contexts/AuthContext'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useToast } from '@/hooks/useToast'

const schema = z
  .object({
    newUsername: z.string().min(3, 'At least 3 characters').max(100).optional().or(z.literal('')),
    newDisplayName: z.string().max(200).optional(),
    newPassword: z.string().min(6, 'At least 6 characters').optional().or(z.literal('')),
    confirmPassword: z.string().optional().or(z.literal('')),
    currentPassword: z.string().min(1, 'Enter your current password to confirm'),
  })
  .refine((d) => !d.newPassword || d.newPassword === d.confirmPassword, {
    message: 'Passwords do not match',
    path: ['confirmPassword'],
  })

type FormData = z.infer<typeof schema>

export default function Account() {
  const { user, applySession } = useAuth()
  const { toast } = useToast()
  const [error, setError] = useState<string | null>(null)

  const form = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      newUsername: user?.username ?? '',
      newDisplayName: user?.displayName ?? '',
      newPassword: '',
      confirmPassword: '',
      currentPassword: '',
    },
  })

  const mutation = useMutation({
    mutationFn: (data: FormData) =>
      authApi.updateAccount({
        currentPassword: data.currentPassword,
        newUsername:
          data.newUsername && data.newUsername !== user?.username ? data.newUsername : undefined,
        newDisplayName:
          data.newDisplayName !== (user?.displayName ?? '') ? data.newDisplayName : undefined,
        newPassword: data.newPassword || undefined,
      }),
    onSuccess: (res) => {
      applySession(res.token, { username: res.username, displayName: res.displayName })
      toast({ title: 'Account updated successfully' })
      form.reset({
        newUsername: res.username,
        newDisplayName: res.displayName ?? '',
        newPassword: '',
        confirmPassword: '',
        currentPassword: '',
      })
      setError(null)
    },
    onError: (e) => setError(e instanceof Error ? e.message : 'Update failed'),
  })

  return (
    <div className="max-w-xl space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>Account Settings</CardTitle>
          <CardDescription>
            Change your username, display name, or password. Your current password is required to
            confirm any change.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={form.handleSubmit((d) => mutation.mutate(d))} className="space-y-4">
            <div className="grid gap-2">
              <Label>Username</Label>
              <div className="relative">
                <User className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <Input {...form.register('newUsername')} className="pl-9" />
              </div>
              {form.formState.errors.newUsername && (
                <p className="text-xs text-destructive">{form.formState.errors.newUsername.message}</p>
              )}
            </div>

            <div className="grid gap-2">
              <Label>Display Name</Label>
              <div className="relative">
                <IdCard className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <Input {...form.register('newDisplayName')} className="pl-9" placeholder="Optional" />
              </div>
            </div>

            <div className="border-t pt-4 space-y-4">
              <p className="text-sm font-medium text-muted-foreground">Change Password (optional)</p>
              <div className="grid gap-2">
                <Label>New Password</Label>
                <div className="relative">
                  <Lock className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                  <Input {...form.register('newPassword')} type="password" className="pl-9" placeholder="Leave blank to keep current" />
                </div>
                {form.formState.errors.newPassword && (
                  <p className="text-xs text-destructive">{form.formState.errors.newPassword.message}</p>
                )}
              </div>
              <div className="grid gap-2">
                <Label>Confirm New Password</Label>
                <div className="relative">
                  <Lock className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                  <Input {...form.register('confirmPassword')} type="password" className="pl-9" />
                </div>
                {form.formState.errors.confirmPassword && (
                  <p className="text-xs text-destructive">{form.formState.errors.confirmPassword.message}</p>
                )}
              </div>
            </div>

            <div className="border-t pt-4 grid gap-2">
              <Label>Current Password *</Label>
              <div className="relative">
                <Lock className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <Input {...form.register('currentPassword')} type="password" className="pl-9" placeholder="Required to save changes" />
              </div>
              {form.formState.errors.currentPassword && (
                <p className="text-xs text-destructive">{form.formState.errors.currentPassword.message}</p>
              )}
            </div>

            {error && (
              <div className="flex items-center gap-2 rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800">
                <AlertCircle className="h-4 w-4 shrink-0" />
                {error}
              </div>
            )}

            <Button type="submit" disabled={mutation.isPending}>
              <CheckCircle2 className="h-4 w-4" />
              {mutation.isPending ? 'Saving…' : 'Save Changes'}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
