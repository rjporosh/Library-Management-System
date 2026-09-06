import { ChevronLeft, ChevronRight } from 'lucide-react'
import type { Paged } from '@/lib/types'
import { t } from '@/lib/i18n'
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
        {t('common.results', {
          count: data.totalItems,
          page,
          pages: Math.max(data.totalPages, 1),
        })}
      </span>
      <div className="flex gap-2">
        <Button
          variant="secondary"
          size="sm"
          disabled={!data.hasPreviousPage}
          onClick={() => onPageChange(page - 1)}
        >
          <ChevronLeft size={14} /> {t('common.prev')}
        </Button>
        <Button
          variant="secondary"
          size="sm"
          disabled={!data.hasNextPage}
          onClick={() => onPageChange(page + 1)}
        >
          {t('common.next')} <ChevronRight size={14} />
        </Button>
      </div>
    </div>
  )
}
