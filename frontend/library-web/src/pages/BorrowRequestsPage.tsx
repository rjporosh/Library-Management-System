import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Check, X } from 'lucide-react'
import { borrowRequestsApi } from '@/api'
import { AdvancedSearch, type FieldDef } from '@/components/AdvancedSearch'
import { DataTable, type Column } from '@/components/DataTable'
import { Pagination } from '@/components/Pagination'
import { Badge, Button, PageHeader } from '@/components/ui'
import { confirmAction, normaliseError, toastError, toastSuccess } from '@/lib/api'
import { formatDateTime } from '@/lib/format'
import { t } from '@/lib/i18n'
import type { BorrowRequestRecord, BorrowRequestStatus } from '@/lib/types'
import { useSearchList } from '@/lib/useSearchList'

const STATUS_TONE: Record<BorrowRequestStatus, 'slate' | 'green' | 'red' | 'blue'> = {
  Pending: 'slate',
  Approved: 'blue',
  Fulfilled: 'green',
  Rejected: 'red',
}

const FIELDS: FieldDef[] = [
  { name: 'status', label: t('requests.col.status'), type: 'enum', options: ['Pending', 'Approved', 'Rejected', 'Fulfilled'] },
  { name: 'type', label: t('requests.col.type'), type: 'enum', options: ['Borrow', 'Purchase'] },
]

export default function BorrowRequestsPage() {
  const qc = useQueryClient()
  const { state, setState, query, setPage, errorMessage } = useSearchList(
    'borrowRequests',
    borrowRequestsApi.search,
  )

  const invalidate = () => void qc.invalidateQueries({ queryKey: ['borrowRequests'] })

  const approve = useMutation({
    mutationFn: (id: string) => borrowRequestsApi.approve(id),
    onSuccess: () => {
      toastSuccess(t('requests.approved'))
      invalidate()
      void qc.invalidateQueries({ queryKey: ['borrowing'] })
      void qc.invalidateQueries({ queryKey: ['copies'] })
    },
    onError: (e) => toastError(normaliseError(e).message),
  })

  const reject = useMutation({
    mutationFn: (id: string) => borrowRequestsApi.reject(id),
    onSuccess: () => {
      toastSuccess(t('requests.rejected'))
      invalidate()
    },
    onError: (e) => toastError(normaliseError(e).message),
  })

  const askApprove = async (r: BorrowRequestRecord) => {
    if (await confirmAction({ title: t('requests.confirmApprove') })) approve.mutate(r.id)
  }

  const askReject = async (r: BorrowRequestRecord) => {
    if (await confirmAction({ title: t('requests.confirmReject'), danger: true })) reject.mutate(r.id)
  }

  const columns: Column<BorrowRequestRecord>[] = [
    {
      key: 'member',
      header: t('requests.col.member'),
      render: (r) => (
        <div>
          <p className="font-medium text-slate-900">{r.memberName}</p>
          <p className="text-xs text-slate-400">{r.membershipNumber}</p>
        </div>
      ),
    },
    {
      key: 'item',
      header: t('requests.col.item'),
      render: (r) => (
        <div>
          <p className="font-medium text-slate-900">{r.type === 'Borrow' ? r.bookTitle : r.suggestedTitle}</p>
          {r.type === 'Purchase' && r.suggestedAuthor && <p className="text-xs text-slate-400">{r.suggestedAuthor}</p>}
          {r.note && <p className="text-xs italic text-slate-400">{r.note}</p>}
        </div>
      ),
    },
    {
      key: 'type',
      header: t('requests.col.type'),
      render: (r) => <span className="text-sm">{t(r.type === 'Borrow' ? 'requests.type.borrow' : 'requests.type.purchase')}</span>,
    },
    { key: 'requestedAt', header: t('requests.col.requestedAt'), render: (r) => formatDateTime(r.requestedAt) },
    {
      key: 'status',
      header: t('requests.col.status'),
      render: (r) => <Badge tone={STATUS_TONE[r.status]}>{t(`requests.status.${r.status}`)}</Badge>,
    },
    {
      key: 'actions',
      header: '',
      className: 'text-right',
      render: (r) =>
        r.status === 'Pending' ? (
          <div className="flex justify-end gap-1">
            <Button variant="secondary" size="sm" onClick={() => askApprove(r)} disabled={approve.isPending}>
              <Check size={14} className="text-emerald-600" /> {t('requests.approve')}
            </Button>
            <Button variant="ghost" size="sm" onClick={() => askReject(r)} disabled={reject.isPending}>
              <X size={14} className="text-rose-500" /> {t('requests.reject')}
            </Button>
          </div>
        ) : null,
    },
  ]

  return (
    <>
      <PageHeader title={t('requests.queueTitle')} subtitle={t('requests.queueSubtitle')} />

      <div className="space-y-4">
        <AdvancedSearch fields={FIELDS} state={state} onChange={setState} />

        <DataTable
          columns={columns}
          rows={query.data?.items ?? []}
          rowKey={(r) => r.id}
          loading={query.isLoading}
          error={errorMessage}
          onRetry={() => void query.refetch()}
          emptyTitle={t('requests.emptyTitle')}
        />
        <Pagination page={state.page} onPageChange={setPage} data={query.data} />
      </div>
    </>
  )
}
