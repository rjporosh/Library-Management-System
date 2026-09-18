import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { membersApi } from '@/api'
import { DataTable, type Column } from '@/components/DataTable'
import {
  Badge,
  Button,
  Card,
  PageHeader,
  Spinner,
  StatusPill,
  ErrorState,
} from '@/components/ui'
import { confirmAction, normaliseError, toastError, toastSuccess } from '@/lib/api'
import { formatDate, formatDateTime, relativeExpiry } from '@/lib/format'
import { t, tStatus } from '@/lib/i18n'
import type { MemberBorrowSummary } from '@/lib/types'

type Action = 'suspend' | 'reactivate' | 'renew' | 'deactivate'

export default function MemberDetailPage() {
  const { id = '' } = useParams()
  const qc = useQueryClient()

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['members', id, 'detail'],
    queryFn: () => membersApi.detail(id),
  })

  const lifecycle = useMutation({
    mutationFn: (action: Action) => membersApi.lifecycle(id, action),
    onSuccess: (m) => {
      toastSuccess(t('members.nowStatus', { name: m.name, status: tStatus(m.status) }))
      void qc.invalidateQueries({ queryKey: ['members'] })
      void qc.invalidateQueries({ queryKey: ['dashboard'] })
    },
    onError: (e) => toastError(normaliseError(e).message),
  })

  const run = async (action: Action) => {
    const ok = await confirmAction({
      title: t('memberDetail.confirmAction', {
        action: t(
          action === 'deactivate' ? 'members.action.markInactive' : `members.action.${action}`,
        ),
      }),
      danger: action === 'suspend' || action === 'deactivate',
      confirmText: t('common.confirm'),
    })
    if (ok) lifecycle.mutate(action)
  }

  const columns: Column<MemberBorrowSummary>[] = [
    {
      key: 'copy',
      header: t('borrow.col.book'),
      render: (h) => (
        <div>
          <p className="font-medium text-slate-900">{h.bookTitle || t('common.dash')}</p>
          <p className="font-mono text-xs text-slate-400">{h.barcode}</p>
        </div>
      ),
    },
    { key: 'borrowedAt', header: t('borrow.col.borrowed'), render: (h) => formatDate(h.borrowedAt) },
    { key: 'dueAt', header: t('borrow.col.due'), render: (h) => formatDate(h.dueAt) },
    { key: 'returnedAt', header: t('borrow.col.returned'), render: (h) => formatDate(h.returnedAt) },
    {
      key: 'status',
      header: t('borrow.col.status'),
      render: (h) =>
        h.isOverdue ? <Badge tone="red">{t('status.Overdue')}</Badge> : <StatusPill status={h.status} />,
    },
  ]

  if (isLoading) return <Spinner />
  if (isError || !data)
    return <ErrorState message={t('memberDetail.loadError')} onRetry={() => void refetch()} />

  const m = data.member

  return (
    <>
      <Link to="/members" className="mb-3 inline-flex items-center gap-1 text-sm text-slate-500 hover:text-slate-700">
        <ArrowLeft size={14} /> {t('memberDetail.back')}
      </Link>

      <PageHeader
        title={m.name}
        subtitle={`${m.membershipNumber} · ${m.email}`}
        actions={
          <>
            {m.status !== 'Suspended' && (
              <Button variant="secondary" size="sm" onClick={() => run('suspend')}>
                {t('members.action.suspend')}
              </Button>
            )}
            {m.status !== 'Active' && (
              <Button variant="secondary" size="sm" onClick={() => run('reactivate')}>
                {t('members.action.reactivate')}
              </Button>
            )}
            <Button variant="secondary" size="sm" onClick={() => run('renew')}>
              {t('members.action.renew')}
            </Button>
            {m.status === 'Active' && (
              <Button variant="secondary" size="sm" onClick={() => run('deactivate')}>
                {t('members.action.markInactive')}
              </Button>
            )}
          </>
        }
      />

      <Card className="mb-4 p-5">
        <p className="text-xs font-semibold uppercase text-slate-400">{t('memberDetail.contact')}</p>
        <div className="mt-1 flex flex-wrap gap-x-8 gap-y-1 text-sm text-slate-700">
          <span>{m.phone || '—'}</span>
          <span>{m.address || '—'}</span>
        </div>
      </Card>

      <div className="grid gap-4 lg:grid-cols-4">
        <Card className="p-5">
          <p className="text-xs font-semibold uppercase text-slate-400">{t('memberDetail.status')}</p>
          <div className="mt-2">
            <StatusPill status={m.status} />
          </div>
        </Card>
        <Card className="p-5">
          <p className="text-xs font-semibold uppercase text-slate-400">{t('memberDetail.expires')}</p>
          <p className="mt-2 text-sm font-semibold text-slate-800">
            {formatDate(m.membershipExpiresAt)}
          </p>
          <p className="text-xs text-slate-400">{relativeExpiry(m.membershipExpiresAt)}</p>
        </Card>
        <Card className="p-5">
          <p className="text-xs font-semibold uppercase text-slate-400">{t('memberDetail.currentlyBorrowed')}</p>
          <p className="mt-2 text-2xl font-bold text-slate-900">{data.currentlyBorrowed}</p>
          <p className="text-xs text-slate-400">{t('memberDetail.overdueCount', { count: data.overdue })}</p>
        </Card>
        <Card className="p-5">
          <p className="text-xs font-semibold uppercase text-slate-400">{t('memberDetail.totalBorrows')}</p>
          <p className="mt-2 text-2xl font-bold text-slate-900">{data.totalBorrowed}</p>
          <p className="text-xs text-slate-400">{t('memberDetail.lastBorrowed', { date: formatDateTime(data.lastBorrowedAt) })}</p>
        </Card>
      </div>

      <div className="mt-4">
        <h2 className="mb-2 text-sm font-semibold text-slate-700">{t('memberDetail.history')}</h2>
        <DataTable
          columns={columns}
          rows={data.history}
          rowKey={(h) => h.borrowRecordId}
          emptyTitle={t('memberDetail.noHistory')}
        />
      </div>
    </>
  )
}
