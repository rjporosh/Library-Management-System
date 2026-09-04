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
import type { BorrowRecord } from '@/lib/types'

export default function BorrowingPage() {
  const qc = useQueryClient()
  const [memberQ, setMemberQ] = useState('')
  const [copyQ, setCopyQ] = useState('')
  const [memberId, setMemberId] = useState('')
  const [copyId, setCopyId] = useState('')
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
      toastSuccess('Book issued')
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
      toastSuccess('Book returned')
      void qc.invalidateQueries()
    },
    onError: (e) => toastError(normaliseError(e).message),
  })

  const columns: Column<BorrowRecord>[] = [
    { key: 'copy', header: 'Copy', render: (r) => <span className="font-mono text-xs">{r.bookCopyId.slice(0, 8)}</span> },
    { key: 'member', header: 'Member', render: (r) => <span className="font-mono text-xs">{r.memberId.slice(0, 8)}</span> },
    { key: 'borrowedAt', header: 'Borrowed', render: (r) => formatDate(r.borrowedAt) },
    {
      key: 'dueAt',
      header: 'Due',
      render: (r) => {
        const overdue = new Date(r.dueAt) < new Date()
        return (
          <span className={overdue ? 'font-semibold text-rose-600' : ''}>
            {formatDate(r.dueAt)}
            {overdue && ' · overdue'}
          </span>
        )
      },
    },
    { key: 'status', header: 'Status', render: (r) => <StatusPill status={r.status} /> },
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
          <Undo2 size={14} /> Return
        </Button>
      ),
    },
  ]

  return (
    <>
      <PageHeader title="Borrowing" subtitle="Issue and return books" />

      <div className="grid gap-4 lg:grid-cols-2">
        <Card className="p-5">
          <h2 className="mb-4 flex items-center gap-2 text-sm font-semibold text-slate-700">
            <BookUp size={16} /> Issue a book
          </h2>
          <div className="space-y-3">
            <FormField label="Find member" hint="Search by name, membership number or email">
              <TextInput
                placeholder="Start typing…"
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
                  <p className="px-3 py-2 text-slate-400">No active member matches.</p>
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

            <FormField label="Find available copy" hint="Search by barcode">
              <TextInput
                placeholder="Barcode…"
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
                  <p className="px-3 py-2 text-slate-400">No available copy matches.</p>
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

            <FormField label="Due date">
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
              {issue.isPending ? 'Issuing…' : 'Confirm issue'}
            </Button>
          </div>
        </Card>

        <Card className="p-0">
          <h2 className="flex items-center gap-2 border-b border-slate-100 px-5 py-4 text-sm font-semibold text-slate-700">
            <Undo2 size={16} /> Active borrows
          </h2>
          <div className="p-2">
            <DataTable
              columns={columns}
              rows={activeBorrows.data?.items ?? []}
              rowKey={(r) => r.id}
              loading={activeBorrows.isLoading}
              error={
                activeBorrows.isError
                  ? normaliseError(activeBorrows.error).message
                  : undefined
              }
              emptyTitle="No active borrows"
            />
          </div>
        </Card>
      </div>
    </>
  )
}
