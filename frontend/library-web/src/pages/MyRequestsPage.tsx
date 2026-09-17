import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Plus } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { borrowRequestsApi } from '@/api'
import { DataTable, type Column } from '@/components/DataTable'
import { Badge, Button, FormField, Modal, PageHeader, TextInput } from '@/components/ui'
import { normaliseError, toastError, toastSuccess } from '@/lib/api'
import { formatDateTime } from '@/lib/format'
import { t } from '@/lib/i18n'
import type { BorrowRequestRecord, BorrowRequestStatus } from '@/lib/types'

const STATUS_TONE: Record<BorrowRequestStatus, 'slate' | 'green' | 'red' | 'blue'> = {
  Pending: 'slate',
  Approved: 'blue',
  Fulfilled: 'green',
  Rejected: 'red',
}

export default function MyRequestsPage() {
  const qc = useQueryClient()
  const [open, setOpen] = useState(false)
  const [title, setTitle] = useState('')
  const [author, setAuthor] = useState('')
  const [note, setNote] = useState('')

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['borrowRequests', 'mine'],
    queryFn: () => borrowRequestsApi.mine(),
  })

  const suggest = useMutation({
    mutationFn: () =>
      borrowRequestsApi.create({
        type: 'Purchase',
        suggestedTitle: title.trim(),
        suggestedAuthor: author.trim() || undefined,
        note: note.trim() || undefined,
      }),
    onSuccess: () => {
      toastSuccess(t('requests.suggested'))
      setOpen(false)
      setTitle('')
      setAuthor('')
      setNote('')
      void qc.invalidateQueries({ queryKey: ['borrowRequests', 'mine'] })
    },
    onError: (e) => toastError(normaliseError(e).message),
  })

  const onSubmit = (e: FormEvent) => {
    e.preventDefault()
    suggest.mutate()
  }

  const columns: Column<BorrowRequestRecord>[] = [
    {
      key: 'item',
      header: t('requests.col.item'),
      render: (r) => (
        <div>
          <p className="font-medium text-slate-900">
            {r.type === 'Borrow' ? r.bookTitle : r.suggestedTitle}
          </p>
          {r.type === 'Purchase' && r.suggestedAuthor && (
            <p className="text-xs text-slate-400">{r.suggestedAuthor}</p>
          )}
        </div>
      ),
    },
    {
      key: 'type',
      header: t('requests.col.type'),
      render: (r) => <span className="text-sm">{t(r.type === 'Borrow' ? 'requests.type.borrow' : 'requests.type.purchase')}</span>,
    },
    {
      key: 'status',
      header: t('requests.col.status'),
      render: (r) => <Badge tone={STATUS_TONE[r.status]}>{t(`requests.status.${r.status}`)}</Badge>,
    },
    { key: 'requestedAt', header: t('requests.col.requestedAt'), render: (r) => formatDateTime(r.requestedAt) },
  ]

  return (
    <>
      <PageHeader
        title={t('requests.myTitle')}
        subtitle={t('requests.mySubtitle')}
        actions={
          <Button onClick={() => setOpen(true)}>
            <Plus size={16} /> {t('requests.suggestPurchase')}
          </Button>
        }
      />

      <DataTable
        columns={columns}
        rows={data ?? []}
        rowKey={(r) => r.id}
        loading={isLoading}
        error={isError ? t('requests.loadError') : undefined}
        onRetry={() => void refetch()}
        emptyTitle={t('requests.emptyTitle')}
      />

      <Modal open={open} onClose={() => setOpen(false)} title={t('requests.suggestPurchase')}>
        <form className="space-y-3" onSubmit={onSubmit}>
          <FormField label={t('requests.field.title')}>
            <TextInput value={title} onChange={(e) => setTitle(e.target.value)} required autoFocus />
          </FormField>
          <FormField label={t('requests.field.author')} hint={t('common.optional')}>
            <TextInput value={author} onChange={(e) => setAuthor(e.target.value)} />
          </FormField>
          <FormField label={t('requests.field.note')} hint={t('common.optional')}>
            <TextInput value={note} onChange={(e) => setNote(e.target.value)} />
          </FormField>
          <div className="flex justify-end gap-2 pt-1">
            <Button type="button" variant="secondary" onClick={() => setOpen(false)}>
              {t('common.cancel')}
            </Button>
            <Button type="submit" disabled={suggest.isPending || !title.trim()}>
              {suggest.isPending ? t('common.saving') : t('requests.submit')}
            </Button>
          </div>
        </form>
      </Modal>
    </>
  )
}
