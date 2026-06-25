import { formatCurrency } from '@/lib/utils'
import { cn } from '@/lib/utils'

interface CurrencyDisplayProps {
  amount: number
  className?: string
  size?: 'sm' | 'md' | 'lg' | 'xl'
  positive?: boolean
  negative?: boolean
}

export default function CurrencyDisplay({
  amount,
  className,
  size = 'md',
  positive,
  negative,
}: CurrencyDisplayProps) {
  const sizeClasses = {
    sm: 'text-sm',
    md: 'text-base',
    lg: 'text-lg font-semibold',
    xl: 'text-2xl font-bold',
  }

  return (
    <span
      className={cn(
        sizeClasses[size],
        positive && 'text-emerald-600',
        negative && 'text-red-600',
        className
      )}
    >
      {formatCurrency(amount)}
    </span>
  )
}
