import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  AlertTriangle,
  BookOpen,
  CalendarClock,
  Layers,
  PlayCircle,
  Users,
} from 'lucide-react'
import type { ReactNode } from 'react'
import { dashboardApi, jobsApi } from '@/api'
import { confirmAction, normaliseError, toastError, toastSuccess } from '@/lib/api'
import { formatDateTime } from '@/lib/format'
import { t, tStatus } from '@/lib/i18n'
import { Badge, Button, Card, PageHeader, Spinner, ErrorState } from '@/components/ui'

export default function DashboardPage() {
  const qc = useQueryClient()
  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['dashboard'],
    queryFn: dashboardApi.get,
  })

  const runJob = useMutation({
    mutationFn: jobsApi.runMemberMaintenance,
    onSuccess: (r) => {
      toastSuccess(
        t('dash.jobDone', {
          suspended: r.overdueSuspended,
          inactive: r.expiredDeactivated,
        }),
      )
      void qc.invalidateQueries({ queryKey: ['dashboard'] })
      void qc.invalidateQueries({ queryKey: ['members'] })
    },
    onError: (e) => toastError(normaliseError(e).message),
  })

  const triggerJob = async () => {
    const ok = await confirmAction({
      title: t('dash.confirmTitle'),
      text: t('dash.confirmText'),
      confirmText: t('dash.confirmRun'),
    })
    if (ok) runJob.mutate()
  }

  return (
    <>
      <PageHeader
        title={t('dash.title')}
        subtitle={t('dash.subtitle')}
        actions={
          <Button onClick={triggerJob} disabled={runJob.isPending}>
            <PlayCircle size={16} />
            {runJob.isPending ? t('dash.runningJob') : t('dash.runJob')}
          </Button>
        }
      />

      {isLoading ? (
        <Spinner />
      ) : isError || !data ? (
        <ErrorState message={t('dash.loadError')} onRetry={() => void refetch()} />
      ) : (
        <div className="space-y-6">
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <Stat icon={<BookOpen size={18} />} label={t('dash.books')} value={data.totalBooks} />
            <Stat
              icon={<Layers size={18} />}
              label={t('dash.copiesAvailable')}
              value={`${data.availableCopies} / ${data.totalCopies}`}
              hint={t('dash.copiesHint', {
                borrowed: data.borrowedCopies,
                outOfService: data.outOfServiceCopies,
              })}
            />
            <Stat
              icon={<Users size={18} />}
              label={t('dash.members')}
              value={data.totalMembers}
              hint={t('dash.membersHint', {
                active: data.activeMembers,
                suspended: data.suspendedMembers,
                inactive: data.inactiveMembers,
              })}
            />
            <Stat
              icon={<AlertTriangle size={18} />}
              label={t('dash.overdue')}
              value={data.overdueBorrows}
              hint={t('dash.overdueHint', { active: data.activeBorrows })}
              tone={data.overdueBorrows > 0 ? 'red' : undefined}
            />
          </div>

          <div className="grid gap-4 lg:grid-cols-3">
            <Card className="lg:col-span-2">
              <div className="border-b border-slate-100 px-5 py-3 text-sm font-semibold text-slate-700">
                {t('dash.recentActivity')}
              </div>
              <div className="divide-y divide-slate-100">
                {data.recentActivity.length === 0 ? (
                  <p className="px-5 py-6 text-sm text-slate-400">{t('dash.noActivity')}</p>
                ) : (
                  data.recentActivity.map((a) => (
                    <div
                      key={a.borrowRecordId}
                      className="flex items-center justify-between px-5 py-3 text-sm"
                    >
                      <span className="font-mono text-xs text-slate-500">
                        {a.bookCopyId.slice(0, 8)}
                      </span>
                      <span className="text-slate-600">
                        {t('dash.borrowedOn', { date: formatDateTime(a.borrowedAt) })}
                      </span>
                      <Badge tone={a.status === 'Active' ? 'blue' : 'slate'}>
                        {tStatus(a.status)}
                      </Badge>
                    </div>
                  ))
                )}
              </div>
            </Card>

            <Card>
              <div className="border-b border-slate-100 px-5 py-3 text-sm font-semibold text-slate-700">
                {t('dash.membershipHealth')}
              </div>
              <div className="space-y-3 px-5 py-4 text-sm">
                <Row label={t('dash.expiring30')} value={data.membersExpiringSoon} warn />
                <Row label={t('dash.suspended')} value={data.suspendedMembers} />
                <Row label={t('dash.inactive')} value={data.inactiveMembers} />
                <p className="flex items-center gap-1.5 pt-1 text-xs text-slate-400">
                  <CalendarClock size={13} />
                  {t('dash.nightlyNote')}
                </p>
              </div>
            </Card>
          </div>
        </div>
      )}
    </>
  )
}

function Stat({
  icon,
  label,
  value,
  hint,
  tone,
}: {
  icon: ReactNode
  label: string
  value: ReactNode
  hint?: string
  tone?: 'red'
}) {
  return (
    <Card className="p-5">
      <div className="flex items-center gap-2 text-slate-400">
        {icon}
        <span className="text-xs font-semibold uppercase tracking-wide">{label}</span>
      </div>
      <p
        className={`mt-2 text-2xl font-bold ${tone === 'red' ? 'text-rose-600' : 'text-slate-900'}`}
      >
        {value}
      </p>
      {hint && <p className="mt-1 text-xs text-slate-400">{hint}</p>}
    </Card>
  )
}

function Row({ label, value, warn }: { label: string; value: number; warn?: boolean }) {
  return (
    <div className="flex items-center justify-between">
      <span className="text-slate-600">{label}</span>
      <span
        className={`font-semibold ${warn && value > 0 ? 'text-amber-600' : 'text-slate-900'}`}
      >
        {value}
      </span>
    </div>
  )
}
