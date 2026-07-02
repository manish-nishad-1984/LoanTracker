import { useQuery } from '@tanstack/react-query'
import { FileText, X } from 'lucide-react'
import { expensesApi } from '@/api/expenses'
import { Badge } from '@/components/ui/badge'
import type { Attachment } from '@/types'

export default function AttachmentThumb({ att, onDelete }: { att: Attachment; onDelete?: () => void }) {
  const isImage = att.contentType.startsWith('image/')
  const { data: url } = useQuery({
    queryKey: ['attachment', att.id],
    queryFn: () => expensesApi.attachmentUrl(att.id),
    staleTime: Infinity,
  })

  return (
    <div className="relative group">
      <button
        type="button"
        onClick={() => url && window.open(url, '_blank')}
        title={att.fileName}
        className="block h-20 w-20 overflow-hidden rounded-md border border-slate-200 bg-slate-50 hover:ring-2 hover:ring-blue-300"
      >
        {isImage && url ? (
          <img src={url} alt={att.fileName} className="h-full w-full object-cover" />
        ) : (
          <div className="flex h-full w-full flex-col items-center justify-center text-slate-400">
            <FileText className="h-6 w-6" />
            <span className="mt-1 max-w-[70px] truncate text-[9px]">{att.fileName}</span>
          </div>
        )}
      </button>
      <Badge variant="outline" className="absolute left-0.5 top-0.5 bg-white/90 px-1 py-0 text-[8px]">{att.kind}</Badge>
      {onDelete && (
        <button
          type="button"
          onClick={onDelete}
          className="absolute -right-1.5 -top-1.5 rounded-full bg-rose-600 p-0.5 text-white opacity-0 transition group-hover:opacity-100"
        >
          <X className="h-3 w-3" />
        </button>
      )}
    </div>
  )
}
