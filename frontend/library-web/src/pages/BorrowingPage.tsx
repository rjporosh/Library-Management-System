import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowLeftRight, BookUp, Undo2 } from 'lucide-react'
import { useState } from 'react'
import { borrowingApi, copiesApi, membersApi } from '@/api'
import { DataTable, type Column } from '@/components/DataTable'
import {
  Button,
  Card,
  FormField,
  PageHeader,
  StatusPill,
  TextInput,
} from '@/components/ui'
import { normaliseError, toastError, toastSuccess } from '@/lib/api'
import { formatDate } from '@/lib/format'
import { t } from '@/lib/i18n'
import type { BorrowRecord } from '@/lib/types'

export default function BorrowingPage() {
  const qc = useQueryClient()
  const [memberQ, setMemberQ] = useState('')
  const [copyQ, setCopyQ] = useState('')
  const [memberId, setMemberId] = useState('')
  const [copyId, setCopyId] = useState('')
  const [activeQ, setActiveQ] = useState('')
  const [dueAt, setDueAt] = useState(() => {
    const d = new Date()
    d.setDate(d.getDate() + 14)
    return d.toISOString().slice(0, 10)
  })

  const members = useQuery({
    queryKey: ['members', 'issue-lookup', memberQ],
    queryFn: () =>
      membersApi.search({
        filters: [{ field: 'status', operator: 'eq', value: 'Active' }],
        match: 'all',
        sort: [],
        page: 1,
        pageSize: 20,
        search: memberQ || undefined,
      }),
    enabled: memberQ.length >= 2,
  })

  const copies = useQuery({
    queryKey: ['copies', 'issue-lookup', copyQ],
    queryFn: () =>
      copiesApi.search({
        filters: [{ field: 'status', operator: 'eq', value: 'Available' }],
        match: 'all',
        sort: [],
        page: 1,
        pageSize: 20,
        search: copyQ || undefined,
      }),
    enabled: copyQ.length >= 1,
  })

  const activeBorrows = useQuery({
    queryKey: ['borrowing', 'active'],
    queryFn: () =>
      borrowingApi.search({
        filters: [{ field: 'status', operator: 'eq', value: 'Active' }],
        match: 'all',
        sort: [{ field: 'dueAt', direction: 'asc' }],
        page: 1,
        pageSize: 50,
      }),
  })

  const issue = useMutation({
    mutationFn: () =>
      borrowingApi.issue({
        memberId,
        bookCopyId: copyId,
        dueAt: new Date(dueAt).toISOString(),
      }),
    onSuccess: () => {
      toastSuccess(t('borrow.issued'))
      setMemberId('')
      setCopyId('')
      setMemberQ('')
      setCopyQ('')
      void qc.invalidateQueries()
    },
    onError: (e) => toastError(normaliseError(e).message),
  })

  const returnBook = useMutation({
    mutationFn: (id: string) => borrowingApi.returnBook(id),
    onSuccess: () => {
      toastSuccess(t('borrow.returned'))
      void qc.invalidateQueries()
    },
    onError: (e) => toastError(normaliseError(e).message),
  })

  const activeItems = activeBorrows.data?.items ?? []
  const activeFilter = activeQ.trim().toLowerCase()
  const filteredActive = activeFilter
    ? activeItems.filter((r) =>
        [r.memberName, r.membershipNumber, r.bookTitle, r.barcode]
          .join(' ')
          .toLowerCase()
          .includes(activeFilter),
      )
    : activeItems

  const columns: Column<BorrowRecord>[] = [
    {
      key: 'book',
      header: t('borrow.col.book'),
      render: (r) => (
        <div>
          <p className="font-medium text-slate-900">{r.bookTitle || t('common.dash')}</p>
          <p className="font-mono text-xs text-slate-400">{r.barcode}</p>
        </div>
      ),
    },
    {
      key: 'member',
      header: t('borrow.col.member'),
      render: (r) => (
        <div>
          <p className="font-medium text-slate-900">{r.memberName || t('common.dash')}</p>
          <p className="text-xs text-slate-400">{r.membershipNumber}</p>
        </div>
      ),
    },
    { key: 'borrowedAt', header: t('borrow.col.borrowed'), render: (r) => formatDate(r.borrowedAt) },
    {
      key: 'dueAt',
      header: t('borrow.col.due'),
      render: (r) => {
        const overdue = new Date(r.dueAt) < new Date()
        return (
          <span className={overdue ? 'font-semibold text-rose-600' : ''}>
            {formatDate(r.dueAt)}
            {overdue && t('common.overdueSuffix')}
          </span>
        )
      },
    },
    { key: 'status', header: t('borrow.col.status'), render: (r) => <StatusPill status={r.status} /> },
    {
      key: 'actions',
      header: '',
      className: 'text-right',
      render: (r) => (
        <Button
          variant="secondary"
          size="sm"
          disabled={returnBook.isPending}
          onClick={() => returnBook.mutate(r.id)}
        >
          <Undo2 size={14} /> {t('borrow.return')}
        </Button>
      ),
    },
  ]

  return (
    <>
      <PageHeader title={t('borrow.title')} subtitle={t('borrow.subtitle')} />

      <div className="grid gap-4 lg:grid-cols-2">
        <Card className="p-5">
          <h2 className="mb-4 flex items-center gap-2 text-sm font-semibold text-slate-700">
            <BookUp size={16} /> {t('borrow.issueHeading')}
          </h2>
          <div className="space-y-3">
            <FormField label={t('borrow.findMember')} hint={t('borrow.findMemberHint')}>
              <TextInput
                placeholder={t('borrow.startTyping')}
                value={memberQ}
                onChange={(e) => {
                  setMemberQ(e.target.value)
                  setMemberId('')
                }}
              />
            </FormField>
            {members.data && memberQ.length >= 2 && !memberId && (
              <div className="max-h-40 overflow-y-auto rounded-lg border border-slate-200 text-sm">
                {members.data.items.length === 0 ? (
                  <p className="px-3 py-2 text-slate-400">{t('borrow.noMemberMatch')}</p>
                ) : (
                  members.data.items.map((m) => (
                    <button
                      key={m.id}
                      type="button"
                      className="flex w-full items-center justify-between px-3 py-1.5 text-left hover:bg-brand-50"
                      onClick={() => {
                        setMemberId(m.id)
                        setMemberQ(`${m.name} (${m.membershipNumber})`)
                      }}
                    >
                      <span>{m.name}</span>
                      <span className="text-xs text-slate-400">{m.membershipNumber}</span>
                    </button>
                  ))
                )}
              </div>
            )}

            <FormField label={t('borrow.findCopy')} hint={t('borrow.findCopyHint')}>
              <TextInput
                placeholder={t('borrow.barcodePlaceholder')}
                value={copyQ}
                onChange={(e) => {
                  setCopyQ(e.target.value)
                  setCopyId('')
                }}
              />
            </FormField>
            {copies.data && copyQ.length >= 1 && !copyId && (
              <div className="max-h-40 overflow-y-auto rounded-lg border border-slate-200 text-sm">
                {copies.data.items.length === 0 ? (
                  <p className="px-3 py-2 text-slate-400">{t('borrow.noCopyMatch')}</p>
                ) : (
                  copies.data.items.map((c) => (
                    <button
                      key={c.id}
                      type="button"
                      className="flex w-full items-center justify-between px-3 py-1.5 text-left hover:bg-brand-50"
                      onClick={() => {
                        setCopyId(c.id)
                        setCopyQ(c.barcode)
                      }}
                    >
                      <span className="font-mono">{c.barcode}</span>
                      <StatusPill status={c.status} />
                    </button>
                  ))
                )}
              </div>
            )}

            <FormField label={t('borrow.dueDate')}>
              <TextInput
                type="date"
                value={dueAt}
                onChange={(e) => setDueAt(e.target.value)}
              />
            </FormField>

            <Button
              className="w-full"
              disabled={!memberId || !copyId || issue.isPending}
              onClick={() => issue.mutate()}
            >
              <ArrowLeftRight size={16} />
              {issue.isPending ? t('borrow.issuing') : t('borrow.confirmIssue')}
            </Button>
          </div>
        </Card>

        <Card className="p-0">
          <h2 className="flex items-center gap-2 border-b border-slate-100 px-5 py-4 text-sm font-semibold text-slate-700">
            <Undo2 size={16} /> {t('borrow.activeHeading')}
          </h2>
          <div className="border-b border-slate-100 p-3">
            <TextInput
              placeholder={t('borrow.searchActivePlaceholder')}
              value={activeQ}
              onChange={(e) => setActiveQ(e.target.value)}
            />
          </div>
          <div className="p-2">
            <DataTable
              columns={columns}
              rows={filteredActive}
              rowKey={(r) => r.id}
              loading={activeBorrows.isLoading}
              error={
                activeBorrows.isError
                  ? normaliseError(activeBorrows.error).message
                  : undefined
              }
              emptyTitle={t('borrow.emptyTitle')}
            />
          </div>
        </Card>
      </div>
    </>
  )
}
