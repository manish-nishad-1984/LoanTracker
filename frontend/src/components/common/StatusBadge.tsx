import { Badge } from '@/components/ui/badge'
import { loanStatusColor } from '@/lib/utils'
import { cn } from '@/lib/utils'

interface StatusBadgeProps {
  status: string
  className?: string
}

export default function StatusBadge({ status, className }: StatusBadgeProps) {
  return (
    <Badge
      variant="outline"
      className={cn(loanStatusColor(status), 'font-medium', className)}
    >
      {status}
    </Badge>
  )
}
