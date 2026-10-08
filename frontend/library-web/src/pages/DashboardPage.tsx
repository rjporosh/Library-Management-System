import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  AlertTriangle,
  Bell,
  BookOpen,
  CalendarClock,
  Check,
  Layers,
  PlayCircle,
  ShoppingCart,
  Users,
  X,
} from 'lucide-react'
import { useEffect, useState, type ReactNode } from 'react'
import { booksApi, borrowRequestsApi, dashboardApi, jobsApi } from '@/api'
import { Badge, Button, Card, ErrorState, Modal, PageHeader, Spinner, StatusPill } from '@/components/ui'
import { confirmAction, normaliseError, toastError, toastSuccess } from '@/lib/api'
import { formatDateTime, formatNumber } from '@/lib/format'
import { t, tStatus } from '@/lib/i18n'
import { subscribeToNotifications, type LibraryNotification } from '@/lib/signalr'
import type { BorrowRequestStatus } from '@/lib/types'

interface NotificationItem {
  id: string
  requestId: string
  type: 'Borrow' | 'Purchase'
  title: string
  message: string
  timestamp: string
  bookTitle?: string
  suggestedAuthor?: string
  memberName?: string
  membershipNumber?: string
  note?: string
  status: BorrowRequestStatus
  isNew?: boolean
}

function playNotificationChime() {
  try {
    const AudioCtx = window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext
    if (!AudioCtx) return
    const ctx = new AudioCtx()
    const osc = ctx.createOscillator()
    const gain = ctx.createGain()
    osc.type = 'sine'
    osc.frequency.setValueAtTime(587.33, ctx.currentTime) // D5
    osc.frequency.exponentialRampToValueAtTime(880, ctx.currentTime + 0.15) // A5
    gain.gain.setValueAtTime(0.2, ctx.currentTime)
    gain.gain.exponentialRampToValueAtTime(0.01, ctx.currentTime + 0.35)
    osc.connect(gain)
    gain.connect(ctx.destination)
    osc.start()
    osc.stop(ctx.currentTime + 0.35)
  } catch {
    /* best effort */
  }
}

export default function DashboardPage() {
  const qc = useQueryClient()
  const [notifications, setNotifications] = useState<NotificationItem[]>([])
  const [selectedRequest, setSelectedRequest] = useState<NotificationItem | null>(null)

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['dashboard'],
    queryFn: dashboardApi.get,
  })

  const { data: lowStockBooks } = useQuery({
    queryKey: ['lowStock'],
    queryFn: () => booksApi.getLowStock(1),
  })

  // Load pending borrow/purchase requests into the notifications list on mount
  const { data: initialRequests } = useQuery({
    queryKey: ['pendingBorrowRequests'],
    queryFn: () =>
      borrowRequestsApi.search({
        match: 'all',
        sort: [],
        page: 1,
        pageSize: 15,
        filters: [{ field: 'status', operator: 'eq', value: 'Pending' }],
      }),
  })

  useEffect(() => {
    if (initialRequests?.items) {
      const items: NotificationItem[] = initialRequests.items.map((r) => ({
        id: r.id,
        requestId: r.id,
        type: r.type,
        title: r.type === 'Borrow' ? `Borrow Request: ${r.bookTitle}` : `Purchase Request: ${r.suggestedTitle}`,
        message: `${r.memberName} (${r.membershipNumber})`,
        timestamp: r.requestedAt,
        bookTitle: (r.type === 'Borrow' ? r.bookTitle : r.suggestedTitle) ?? undefined,
        suggestedAuthor: r.suggestedAuthor ?? undefined,
        memberName: r.memberName,
        membershipNumber: r.membershipNumber,
        note: r.note ?? undefined,
        status: r.status,
      }))
      setNotifications(items)
    }
  }, [initialRequests])

  // Subscribe to real-time SignalR notifications
  useEffect(() => {
    const unsubscribe = subscribeToNotifications((incoming: LibraryNotification) => {
      playNotificationChime()
      toastSuccess(`🔔 ${incoming.title}`)

      const isBorrow = incoming.type === 'Borrow'
      const newItem: NotificationItem = {
        id: incoming.id,
        requestId: incoming.requestId ?? incoming.id,
        type: isBorrow ? 'Borrow' : 'Purchase',
        title: incoming.title,
        message: incoming.message,
        timestamp: incoming.timestamp,
        bookTitle: incoming.bookTitle ?? undefined,
        suggestedAuthor: incoming.suggestedAuthor ?? undefined,
        memberName: incoming.memberName ?? undefined,
        membershipNumber: incoming.membershipNumber ?? undefined,
        note: incoming.note ?? undefined,
        status: (incoming.status as BorrowRequestStatus) || 'Pending',
        isNew: true,
      }

      setNotifications((prev) => {
        const filtered = prev.filter((p) => p.requestId !== newItem.requestId)
        return [newItem, ...filtered]
      })

      void qc.invalidateQueries({ queryKey: ['dashboard'] })
      void qc.invalidateQueries({ queryKey: ['borrowRequests'] })
      void qc.invalidateQueries({ queryKey: ['pendingBorrowRequests'] })
    })

    return () => {
      unsubscribe()
    }
  }, [qc])

  const approve = useMutation({
    mutationFn: (id: string) => borrowRequestsApi.approve(id),
    onSuccess: (updated) => {
      toastSuccess(t('requests.approved'))
      setNotifications((prev) =>
        prev.map((n) => (n.requestId === updated.id ? { ...n, status: updated.status } : n)),
      )
      if (selectedRequest && selectedRequest.requestId === updated.id) {
        setSelectedRequest((prev) => (prev ? { ...prev, status: updated.status } : null))
      }
      void qc.invalidateQueries({ queryKey: ['dashboard'] })
      void qc.invalidateQueries({ queryKey: ['borrowRequests'] })
      void qc.invalidateQueries({ queryKey: ['borrowing'] })
      void qc.invalidateQueries({ queryKey: ['copies'] })
      void qc.invalidateQueries({ queryKey: ['lowStock'] })
    },
    onError: (e) => toastError(normaliseError(e).message),
  })

  const reject = useMutation({
    mutationFn: (id: string) => borrowRequestsApi.reject(id),
    onSuccess: (updated) => {
      toastSuccess(t('requests.rejected'))
      setNotifications((prev) =>
        prev.map((n) => (n.requestId === updated.id ? { ...n, status: updated.status } : n)),
      )
      if (selectedRequest && selectedRequest.requestId === updated.id) {
        setSelectedRequest((prev) => (prev ? { ...prev, status: updated.status } : null))
      }
      void qc.invalidateQueries({ queryKey: ['dashboard'] })
      void qc.invalidateQueries({ queryKey: ['borrowRequests'] })
    },
    onError: (e) => toastError(normaliseError(e).message),
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

  const handleApprove = (id: string) => {
    approve.mutate(id)
  }

  const handleReject = (id: string) => {
    reject.mutate(id)
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
          {/* Top Key Metric Cards */}
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <Stat icon={<BookOpen size={18} />} label={t('dash.books')} value={formatNumber(data.totalBooks)} />
            <Stat
              icon={<Layers size={18} />}
              label={t('dash.copiesAvailable')}
              value={`${formatNumber(data.availableCopies)} / ${formatNumber(data.totalCopies)}`}
              hint={t('dash.copiesHint', {
                borrowed: data.borrowedCopies,
                outOfService: data.outOfServiceCopies,
              })}
            />
            <Stat
              icon={<Users size={18} />}
              label={t('dash.members')}
              value={formatNumber(data.totalMembers)}
              hint={t('dash.membersHint', {
                active: data.activeMembers,
                suspended: data.suspendedMembers,
                inactive: data.inactiveMembers,
              })}
            />
            <Stat
              icon={<AlertTriangle size={18} />}
              label={t('dash.overdue')}
              value={formatNumber(data.overdueBorrows)}
              hint={t('dash.overdueHint', { active: data.activeBorrows })}
              tone={data.overdueBorrows > 0 ? 'red' : undefined}
            />
          </div>

          {/* Low Stock Alarm Card (Threshold Alert) */}
          {lowStockBooks && lowStockBooks.length > 0 && (
            <div className="rounded-2xl border-2 border-rose-300 bg-rose-50/70 p-5 shadow-sm">
              <div className="flex items-start gap-3">
                <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-rose-600 text-white shadow-sm">
                  <AlertTriangle size={20} />
                </span>
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2">
                    <h3 className="text-base font-bold text-rose-900">{t('dash.lowStockAlarm')}</h3>
                    <Badge tone="red">{formatNumber(lowStockBooks.length)}</Badge>
                  </div>
                  <p className="mt-0.5 text-xs text-rose-700">{t('dash.lowStockSubtitle')}</p>

                  <div className="mt-3 grid gap-2.5 sm:grid-cols-2">
                    {lowStockBooks.map((b) => (
                      <div
                        key={b.bookId}
                        className="flex items-center justify-between rounded-xl border border-rose-200 bg-white p-3 shadow-xs"
                      >
                        <div className="min-w-0 pr-3">
                          <p className="truncate font-semibold text-slate-900">{b.title}</p>
                          <p className="text-xs text-slate-500">{b.author}</p>
                          <p className="mt-1 text-xs font-semibold text-rose-600">
                            {t('dash.copiesLeft', {
                              available: b.availableCopies,
                              total: b.totalCopies,
                            })}
                          </p>
                        </div>
                        <Button
                          variant="danger"
                          size="sm"
                          onClick={() => {
                            toastSuccess(t('dash.buyMore') + `: ${b.title}`)
                          }}
                        >
                          <ShoppingCart size={13} /> {t('dash.buyMore')}
                        </Button>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* Live SignalR Notifications & Request Action Panel */}
          <Card className="border border-brand-200 shadow-sm">
            <div className="flex items-center justify-between border-b border-slate-100 bg-brand-50/50 px-5 py-3.5">
              <div className="flex items-center gap-2.5">
                <span className="relative flex h-8 w-8 items-center justify-center rounded-lg bg-brand-600 text-white shadow-xs">
                  <Bell size={16} />
                  {notifications.some((n) => n.isNew) && (
                    <span className="absolute -top-1 -right-1 flex h-3 w-3">
                      <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-rose-400 opacity-75" />
                      <span className="relative inline-flex h-3 w-3 rounded-full bg-rose-500" />
                    </span>
                  )}
                </span>
                <div>
                  <h3 className="text-sm font-bold text-slate-900">{t('dash.notifications')}</h3>
                  <p className="text-xs text-slate-500">{t('dash.notificationsSubtitle')}</p>
                </div>
              </div>
              <Badge tone={notifications.length > 0 ? 'blue' : 'slate'}>
                {formatNumber(notifications.length)}
              </Badge>
            </div>

            <div className="divide-y divide-slate-100">
              {notifications.length === 0 ? (
                <p className="px-5 py-6 text-sm text-slate-400">{t('dash.noNotifications')}</p>
              ) : (
                notifications.slice(0, 8).map((n) => (
                  <div
                    key={n.id}
                    onClick={() => setSelectedRequest(n)}
                    className="flex cursor-pointer items-center justify-between px-5 py-3.5 transition hover:bg-slate-50"
                  >
                    <div className="flex min-w-0 flex-1 items-center gap-3 pr-3">
                      <span
                        className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-lg ${
                          n.type === 'Borrow' ? 'bg-sky-100 text-sky-700' : 'bg-emerald-100 text-emerald-700'
                        }`}
                      >
                        {n.type === 'Borrow' ? <BookOpen size={16} /> : <ShoppingCart size={16} />}
                      </span>
                      <div className="min-w-0">
                        <div className="flex items-center gap-2">
                          <p className="truncate font-semibold text-slate-800">{n.bookTitle}</p>
                          {n.isNew && <Badge tone="red">{t('dash.newBadge')}</Badge>}
                        </div>
                        <p className="truncate text-xs text-slate-500">
                          {n.memberName || t('common.unknown')} · {formatDateTime(n.timestamp)}
                        </p>
                      </div>
                    </div>
                    <div className="flex shrink-0 items-center gap-2">
                      <Badge tone={n.type === 'Borrow' ? 'blue' : 'violet'}>
                        {t(n.type === 'Borrow' ? 'requests.type.borrow' : 'requests.type.purchase')}
                      </Badge>
                      <StatusPill status={n.status} />
                    </div>
                  </div>
                ))
              )}
            </div>
          </Card>

          {/* Activity and Membership health grids */}
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
                      <div className="min-w-0 flex-1 pr-3">
                        <p className="truncate font-semibold text-slate-800">
                          {a.memberName || t('common.unknown')}
                        </p>
                        <p className="truncate text-xs text-slate-500">
                          {a.bookTitle || t('common.unknown')} ·{' '}
                          {t('dash.borrowedOn', { date: formatDateTime(a.borrowedAt) })}
                        </p>
                      </div>
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

      {/* Request Details Modal */}
      {selectedRequest && (
        <Modal
          open={!!selectedRequest}
          onClose={() => setSelectedRequest(null)}
          title={t('dash.requestModalTitle')}
        >
          <div className="space-y-4 text-sm">
            <div className="flex items-center justify-between border-b border-slate-100 pb-3">
              <div className="flex items-center gap-2">
                <Badge tone={selectedRequest.type === 'Borrow' ? 'blue' : 'violet'}>
                  {t(selectedRequest.type === 'Borrow' ? 'requests.type.borrow' : 'requests.type.purchase')}
                </Badge>
                <StatusPill status={selectedRequest.status} />
              </div>
              <span className="text-xs text-slate-400">{formatDateTime(selectedRequest.timestamp)}</span>
            </div>

            <div className="rounded-xl bg-slate-50 p-3.5 space-y-2">
              <div>
                <span className="text-xs font-semibold uppercase tracking-wide text-slate-400">
                  {t('requests.col.member')}
                </span>
                <p className="font-bold text-slate-900">{selectedRequest.memberName || t('common.unknown')}</p>
                <p className="text-xs text-slate-500">{selectedRequest.membershipNumber}</p>
              </div>

              <div className="pt-2 border-t border-slate-200/60">
                <span className="text-xs font-semibold uppercase tracking-wide text-slate-400">
                  {t('requests.col.item')}
                </span>
                <p className="font-bold text-slate-900">{selectedRequest.bookTitle}</p>
                {selectedRequest.suggestedAuthor && (
                  <p className="text-xs text-slate-600">
                    {t('dash.suggestedAuthor', { author: selectedRequest.suggestedAuthor })}
                  </p>
                )}
              </div>

              {selectedRequest.note && (
                <div className="pt-2 border-t border-slate-200/60">
                  <span className="text-xs font-semibold uppercase tracking-wide text-slate-400">
                    {t('dash.memberNote')}
                  </span>
                  <p className="italic text-slate-700">{selectedRequest.note}</p>
                </div>
              )}
            </div>

            <div className="flex items-center justify-end gap-2 pt-2">
              {selectedRequest.status === 'Pending' ? (
                <>
                  <Button
                    variant="secondary"
                    onClick={() => handleApprove(selectedRequest.requestId)}
                    disabled={approve.isPending || reject.isPending}
                  >
                    <Check size={16} className="text-emerald-600" />
                    {approve.isPending ? t('common.saving') : t('requests.approve')}
                  </Button>
                  <Button
                    variant="danger"
                    onClick={() => handleReject(selectedRequest.requestId)}
                    disabled={approve.isPending || reject.isPending}
                  >
                    <X size={16} />
                    {reject.isPending ? t('common.saving') : t('requests.reject')}
                  </Button>
                </>
              ) : (
                <Button variant="secondary" onClick={() => setSelectedRequest(null)}>
                  {t('common.cancel')}
                </Button>
              )}
            </div>
          </div>
        </Modal>
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
        {formatNumber(value)}
      </span>
    </div>
  )
}