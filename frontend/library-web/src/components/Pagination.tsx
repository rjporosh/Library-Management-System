import { ChevronLeft, ChevronRight } from 'lucide-react'
import type { Paged } from '@/lib/types'
import { Button } from './ui'

export function Pagination<T>({
  page,
  onPageChange,
  data,
}: {
  page: number
  onPageChange: (page: number) => void
  data?: Pick<Paged<T>, 'totalItems' | 'totalPages' | 'hasNextPage' | 'hasPreviousPage'>
}) {
  if (!data || data.totalItems === 0) return null

  return (
    <div className="mt-3 flex items-center justify-between text-sm text-slate-500">
      <span>
        {data.totalItems} result{data.totalItems === 1 ? '' : 's'} · page {page} of{' '}
        {Math.max(data.totalPages, 1)}
      </span>
      <div className="flex gap-2">
        <Button
          variant="secondary"
          size="sm"
          disabled={!data.hasPreviousPage}
          onClick={() => onPageChange(page - 1)}
        >
          <ChevronLeft size={14} /> Prev
        </Button>
        <Button
          variant="secondary"
          size="sm"
          disabled={!data.hasNextPage}
          onClick={() => onPageChange(page + 1)}
        >
          Next <ChevronRight size={14} />
        </Button>
      </div>
    </div>
  )
}
