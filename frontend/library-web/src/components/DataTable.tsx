import { clsx } from 'clsx'
import { ArrowDown, ArrowUp, ChevronsUpDown } from 'lucide-react'
import type { ReactNode } from 'react'
import type { SortSpec } from '@/lib/types'
import { t } from '@/lib/i18n'
import { EmptyState, ErrorState, Spinner } from './ui'

export interface Column<T> {
  key: string
  header: string
  sortable?: boolean
  render: (row: T) => ReactNode
  className?: string
}

interface Props<T> {
  columns: Column<T>[]
  rows: T[]
  rowKey: (row: T) => string
  loading?: boolean
  error?: string
  onRetry?: () => void
  emptyTitle?: string
  emptyHint?: string
  sort?: SortSpec[]
  onSortChange?: (field: string) => void
  onRowClick?: (row: T) => void
}

export function DataTable<T>({
  columns,
  rows,
  rowKey,
  loading,
  error,
  onRetry,
  emptyTitle,
  emptyHint,
  sort = [],
  onSortChange,
  onRowClick,
}: Props<T>) {
  const sortFor = (key: string) => sort.find((s) => s.field.toLowerCase() === key.toLowerCase())

  return (
    <div className="overflow-x-auto rounded-2xl border border-slate-200 bg-white shadow-sm">
      <table className="min-w-full divide-y divide-slate-200 text-sm">
        <thead className="bg-slate-50/80">
          <tr>
            {columns.map((col) => {
              const active = sortFor(col.key)
              return (
                <th
                  key={col.key}
                  className={clsx(
                    'px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500',
                    col.sortable && onSortChange && 'cursor-pointer select-none hover:text-slate-700',
                    col.className,
                  )}
                  onClick={
                    col.sortable && onSortChange ? () => onSortChange(col.key) : undefined
                  }
                >
                  <span className="inline-flex items-center gap-1">
                    {col.header}
                    {col.sortable &&
                      onSortChange &&
                      (active ? (
                        active.direction === 'asc' ? (
                          <ArrowUp size={13} />
                        ) : (
                          <ArrowDown size={13} />
                        )
                      ) : (
                        <ChevronsUpDown size={13} className="text-slate-300" />
                      ))}
                  </span>
                </th>
              )
            })}
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100">
          {loading ? (
            <tr>
              <td colSpan={columns.length}>
                <Spinner />
              </td>
            </tr>
          ) : error ? (
            <tr>
              <td colSpan={columns.length}>
                <ErrorState message={error} onRetry={onRetry} />
              </td>
            </tr>
          ) : rows.length === 0 ? (
            <tr>
              <td colSpan={columns.length}>
                <EmptyState title={emptyTitle ?? t('common.nothingToShow')} hint={emptyHint} />
              </td>
            </tr>
          ) : (
            rows.map((row) => (
              <tr
                key={rowKey(row)}
                className={clsx(
                  'transition hover:bg-slate-50',
                  onRowClick && 'cursor-pointer',
                )}
                onClick={onRowClick ? () => onRowClick(row) : undefined}
              >
                {columns.map((col) => (
                  <td key={col.key} className={clsx('px-4 py-3 text-slate-700', col.className)}>
                    {col.render(row)}
                  </td>
                ))}
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  )
}
