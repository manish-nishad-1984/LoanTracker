import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Users, Plus, Search, Phone, Mail, ExternalLink } from 'lucide-react'
import { useLenders, useDeactivateLender } from '@/hooks/useLenders'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardHeader } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import EmptyState from '@/components/common/EmptyState'
import { formatCurrency, lenderTypeIcon } from '@/lib/utils'
import LenderForm from './LenderForm'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { useToast } from '@/hooks/useToast'

export default function LenderList() {
  const navigate = useNavigate()
  const { toast } = useToast()
  const [search, setSearch] = useState('')
  const [showCreate, setShowCreate] = useState(false)

  const { data, isLoading } = useLenders({ search })
  const deactivate = useDeactivateLender()

  const lenders = data?.items ?? []

  return (
    <div className="space-y-4">
      {/* Toolbar */}
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search parties..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-9"
          />
        </div>
        <Button onClick={() => setShowCreate(true)}>
          <Plus className="h-4 w-4" />
          Add Party
        </Button>
      </div>

      {/* Stats bar */}
      <div className="flex gap-6 text-sm text-muted-foreground">
        <span>{data?.totalCount ?? 0} total parties</span>
        <span>{lenders.filter(l => l.isActive).length} active</span>
      </div>

      {/* Grid */}
      {isLoading ? (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {Array.from({ length: 6 }).map((_, i) => (
            <div key={i} className="h-48 rounded-lg bg-muted animate-pulse" />
          ))}
        </div>
      ) : lenders.length === 0 ? (
        <EmptyState
          icon={Users}
          title="No parties yet"
          description="Add your first party — a person or bank you borrow from or lend to."
          actionLabel="Add Party"
          onAction={() => setShowCreate(true)}
        />
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {lenders.map((lender) => (
            <Card
              key={lender.id}
              className="cursor-pointer hover:shadow-md transition-all hover:border-primary/30"
              onClick={() => navigate(`/lenders/${lender.id}`)}
            >
              <CardHeader className="pb-3">
                <div className="flex items-start justify-between">
                  <div className="flex items-center gap-2 min-w-0">
                    <span className="text-2xl shrink-0">{lenderTypeIcon(lender.lenderType)}</span>
                    <div className="min-w-0">
                      <p className="font-semibold truncate">{lender.name}</p>
                      <p className="text-xs text-muted-foreground">{lender.lenderTypeName}</p>
                    </div>
                  </div>
                  <Badge
                    variant={lender.isActive ? 'default' : 'secondary'}
                    className="shrink-0 ml-2"
                  >
                    {lender.isActive ? 'Active' : 'Inactive'}
                  </Badge>
                </div>
              </CardHeader>
              <CardContent className="space-y-3">
                {lender.phone && (
                  <div className="flex items-center gap-2 text-sm text-muted-foreground">
                    <Phone className="h-3.5 w-3.5 shrink-0" />
                    <span>{lender.phone}</span>
                  </div>
                )}
                {lender.email && (
                  <div className="flex items-center gap-2 text-sm text-muted-foreground truncate">
                    <Mail className="h-3.5 w-3.5 shrink-0" />
                    <span className="truncate">{lender.email}</span>
                  </div>
                )}

                <div className="border-t pt-3 grid grid-cols-2 gap-3">
                  <div>
                    <p className="text-xs text-muted-foreground mb-0.5">Active Loans</p>
                    <p className="text-lg font-bold">{lender.activeLoans}</p>
                  </div>
                  <div>
                    <p className="text-xs text-muted-foreground mb-0.5">Outstanding</p>
                    <p className="text-lg font-bold text-red-600">
                      {formatCurrency(lender.totalOutstanding)}
                    </p>
                  </div>
                </div>

                <div className="flex items-center justify-between pt-1">
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-7 px-2 text-xs"
                    onClick={(e) => {
                      e.stopPropagation()
                      navigate(`/lenders/${lender.id}`)
                    }}
                  >
                    <ExternalLink className="h-3 w-3 mr-1" />
                    View Details
                  </Button>
                  {lender.isActive && (
                    <Button
                      variant="ghost"
                      size="sm"
                      className="h-7 px-2 text-xs text-muted-foreground"
                      onClick={(e) => {
                        e.stopPropagation()
                        deactivate.mutate(lender.id, {
                          onSuccess: () =>
                            toast({ title: 'Lender deactivated', variant: 'default' }),
                        })
                      }}
                    >
                      Deactivate
                    </Button>
                  )}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {/* Create Dialog */}
      <Dialog open={showCreate} onOpenChange={setShowCreate}>
        <DialogContent className="max-w-xl">
          <DialogHeader>
            <DialogTitle>Add New Party</DialogTitle>
          </DialogHeader>
          <LenderForm
            onSuccess={() => {
              setShowCreate(false)
              toast({ title: 'Lender created successfully' })
            }}
            onCancel={() => setShowCreate(false)}
          />
        </DialogContent>
      </Dialog>
    </div>
  )
}
