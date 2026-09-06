import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Plus, Trash2, Upload } from 'lucide-react'
import { useState } from 'react'
import { booksApi, copiesApi } from '@/api'
import { AdvancedSearch, type FieldDef } from '@/components/AdvancedSearch'
import { BulkImportModal } from '@/components/BulkImportModal'
import { DataTable, type Column } from '@/components/DataTable'
import { Pagination } from '@/components/Pagination'
import {
  Button,
  FormField,
  Modal,
  PageHeader,
  Select,
  StatusPill,
  TextInput,
} from '@/components/ui'
import { cascadeDelete, normaliseError, toastError, toastSuccess } from '@/lib/api'
import { t, tStatus } from '@/lib/i18n'
import type { BookCopy } from '@/lib/types'
import { useSearchList } from '@/lib/useSearchList'

const STATUSES = ['Available', 'Lost', 'Damaged', 'Maintenance']

const FIELDS: FieldDef[] = [
  { name: 'barcode', label: t('copies.col.barcode'), type: 'text' },
  {
    name: 'status',
    label: t('copies.col.status'),
    type: 'enum',
    options: ['Available', 'Borrowed', 'Lost', 'Damaged', 'Maintenance'],
  },
]

export default function BookCopiesPage() {
  const qc = useQueryClient()
  const { state, setState, query, setPage, toggleSort, errorMessage } = useSearchList(
    'copies',
    copiesApi.search,
  )

  const [importOpen, setImportOpen] = useState(false)
  const [addOpen, setAddOpen] = useState(false)
  const [bookId, setBookId] = useState('')
  const [barcode, setBarcode] = useState('')
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const [formError, setFormError] = useState<string | null>(null)

  const books = useQuery({
    queryKey: ['books', 'all-for-select'],
    queryFn: () =>
      booksApi.search({ filters: [], match: 'all', sort: [{ field: 'title', direction: 'asc' }], page: 1, pageSize: 100 }),
    enabled: addOpen,
  })

  const create = useMutation({
    mutationFn: () => copiesApi.create({ bookId, barcode: barcode.trim() }),
    onSuccess: () => {
      toastSuccess(t('copies.registered'))
      setAddOpen(false)
      setBookId('')
      setBarcode('')
      void qc.invalidateQueries({ queryKey: ['copies'] })
    },
    onError: (e) => {
      const n = normaliseError(e)
      setFieldErrors(n.fieldErrors)
      setFormError(Object.keys(n.fieldErrors).length ? null : n.message)
    },
  })

  const changeStatus = useMutation({
    mutationFn: ({ id, status }: { id: string; status: string }) =>
      copiesApi.changeStatus(id, status),
    onSuccess: () => {
      toastSuccess(t('copies.statusUpdated'))
      void qc.invalidateQueries({ queryKey: ['copies'] })
    },
    onError: (e) => toastError(normaliseError(e).message),
  })

  const askDelete = async (c: BookCopy) => {
    const deleted = await cascadeDelete(
      (force) => copiesApi.remove(c.id, force),
      { title: t('copies.deleteTitle', { barcode: c.barcode }), entity: t('common.entity.copy') },
    )
    if (deleted) {
      void qc.invalidateQueries({ queryKey: ['copies'] })
      void qc.invalidateQueries({ queryKey: ['dashboard'] })
    }
  }

  const columns: Column<BookCopy>[] = [
    {
      key: 'barcode',
      header: t('copies.col.barcode'),
      sortable: true,
      render: (c) => <span className="font-mono text-sm font-medium text-slate-800">{c.barcode}</span>,
    },
    { key: 'status', header: t('copies.col.status'), sortable: true, render: (c) => <StatusPill status={c.status} /> },
    { key: 'bookId', header: t('copies.col.book'), render: (c) => <span className="font-mono text-xs text-slate-400">{c.bookId.slice(0, 8)}</span> },
    {
      key: 'actions',
      header: '',
      className: 'text-right',
      render: (c) => (
        <div className="flex items-center justify-end gap-2">
          {c.status !== 'Borrowed' && (
            <Select
              className="w-36 py-1 text-xs"
              value={c.status}
              onClick={(e) => e.stopPropagation()}
              onChange={(e) => changeStatus.mutate({ id: c.id, status: e.target.value })}
            >
              {STATUSES.map((s) => (
                <option key={s} value={s}>
                  {tStatus(s)}
                </option>
              ))}
            </Select>
          )}
          <Button variant="ghost" size="sm" onClick={() => askDelete(c)}>
            <Trash2 size={14} className="text-rose-500" />
          </Button>
        </div>
      ),
    },
  ]

  return (
    <>
      <PageHeader
        title={t('copies.title')}
        subtitle={t('copies.subtitle')}
        actions={
          <>
            <Button variant="secondary" onClick={() => setImportOpen(true)}>
              <Upload size={16} /> {t('common.bulkImport')}
            </Button>
            <Button onClick={() => { setAddOpen(true); setFieldErrors({}); setFormError(null) }}>
              <Plus size={16} /> {t('copies.register')}
            </Button>
          </>
        }
      />

      <div className="space-y-4">
        <AdvancedSearch fields={FIELDS} state={state} onChange={setState} />
        <DataTable
          columns={columns}
          rows={query.data?.items ?? []}
          rowKey={(c) => c.id}
          loading={query.isLoading}
          error={errorMessage}
          onRetry={() => void query.refetch()}
          emptyTitle={t('copies.emptyTitle')}
          sort={state.sort}
          onSortChange={toggleSort}
        />
        <Pagination page={state.page} onPageChange={setPage} data={query.data} />
      </div>

      <BulkImportModal
        open={importOpen}
        onClose={() => setImportOpen(false)}
        title={t('copies.importTitle')}
        templateUrl={copiesApi.importTemplateUrl}
        onImport={copiesApi.import}
        onDone={() => void qc.invalidateQueries({ queryKey: ['copies'] })}
      />

      <Modal open={addOpen} onClose={() => setAddOpen(false)} title={t('copies.registerTitle')}>
        <form
          className="space-y-3"
          onSubmit={(e) => {
            e.preventDefault()
            create.mutate()
          }}
        >
          {formError && (
            <div className="rounded-lg border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700">
              {formError}
            </div>
          )}
          <FormField label={t('copies.field.book')} error={fieldErrors.bookId}>
            <Select value={bookId} onChange={(e) => setBookId(e.target.value)}>
              <option value="">{t('copies.selectBook')}</option>
              {books.data?.items.map((b) => (
                <option key={b.id} value={b.id}>
                  {b.title} — {b.author}
                </option>
              ))}
            </Select>
          </FormField>
          <FormField label={t('copies.field.barcode')} error={fieldErrors.barcode}>
            <TextInput
              value={barcode}
              invalid={!!fieldErrors.barcode}
              onChange={(e) => setBarcode(e.target.value)}
            />
          </FormField>
          <div className="flex justify-end gap-2 pt-1">
            <Button type="button" variant="secondary" onClick={() => setAddOpen(false)}>
              {t('common.cancel')}
            </Button>
            <Button type="submit" disabled={create.isPending || !bookId}>
              {create.isPending ? t('common.saving') : t('copies.doRegister')}
            </Button>
          </div>
        </form>
      </Modal>
    </>
  )
}
