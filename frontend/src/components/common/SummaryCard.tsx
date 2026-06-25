import { type LucideIcon } from 'lucide-react'
import { Card, CardContent } from '@/components/ui/card'
import { cn } from '@/lib/utils'
import { formatCurrency } from '@/lib/utils'

interface SummaryCardProps {
  title: string
  value: number | string
  isCurrency?: boolean
  icon: LucideIcon
  iconColor?: string
  description?: string
  trend?: { value: number; label: string }
  className?: string
}

export default function SummaryCard({
  title,
  value,
  isCurrency = true,
  icon: Icon,
  iconColor = 'text-primary',
  description,
  trend,
  className,
}: SummaryCardProps) {
  const displayValue =
    typeof value === 'number' && isCurrency
      ? formatCurrency(value)
      : typeof value === 'number'
      ? value.toLocaleString('en-IN')
      : value

  return (
    <Card className={cn('hover:shadow-md transition-shadow', className)}>
      <CardContent className="p-6">
        <div className="flex items-start justify-between">
          <div className="space-y-1 flex-1 min-w-0">
            <p className="text-sm font-medium text-muted-foreground truncate">{title}</p>
            <p className="text-2xl font-bold tracking-tight truncate">{displayValue}</p>
            {description && (
              <p className="text-xs text-muted-foreground">{description}</p>
            )}
            {trend && (
              <p
                className={cn(
                  'text-xs font-medium',
                  trend.value >= 0 ? 'text-emerald-600' : 'text-red-600'
                )}
              >
                {trend.value >= 0 ? '+' : ''}
                {trend.value}% {trend.label}
              </p>
            )}
          </div>
          <div className={cn('flex h-10 w-10 items-center justify-center rounded-full bg-muted shrink-0 ml-3', iconColor)}>
            <Icon className="h-5 w-5" />
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
