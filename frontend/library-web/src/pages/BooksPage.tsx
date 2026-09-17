import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Eye, Pencil, Plus, Trash2, Upload } from 'lucide-react'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { booksApi } from '@/api'
import { AdvancedSearch, type FieldDef } from '@/components/AdvancedSearch'
import { BulkImportModal } from '@/components/BulkImportModal'
import { DataTable, type Column } from '@/components/DataTable'
import { Pagination } from '@/components/Pagination'
import {
  Button,
  FormField,
  Modal,
  PageHeader,
  TextInput,
} from '@/components/ui'
import { useAuth } from '@/context/authContextValue'
import { cascadeDelete, normaliseError, toastSuccess } from '@/lib/api'
import { coverImageSrc, PLACEHOLDER_COVER } from '@/lib/covers'
import { t } from '@/lib/i18n'
import type { Book } from '@/lib/types'
import { useSearchList } from '@/lib/useSearchList'

const FIELDS: FieldDef[] = [
  { name: 'title', label: t('books.col.title'), type: 'text' },
  { name: 'author', label: t('books.col.author'), type: 'text' },
  { name: 'isbn', label: t('books.col.isbn'), type: 'text' },
  { name: 'category', label: t('books.col.category'), type: 'text' },
  { name: 'publisher', label: t('books.col.publisher'), type: 'text' },
  { name: 'publishedYear', label: t('books.field.publishedYear'), type: 'number' },
]

interface FormValues {
  isbn: string
  title: string
  author: string
  category: string
  publisher: string
  publishedYear: string
  description: string
  coverImageUrl: string
  edition: string
  hasEbook: boolean
  ebookUrl: string
  hasAudiobook: boolean
  audiobookUrl: string
  externalBuyUrl: string
  externalPdfUrl: string
  totalCopies: string
}

const EMPTY_FORM: FormValues = {
  isbn: '',
  title: '',
  author: '',
  category: '',
  publisher: '',
  publishedYear: '',
  description: '',
  coverImageUrl: '',
  edition: '',
  hasEbook: false,
  ebookUrl: '',
  hasAudiobook: false,
  audiobookUrl: '',
  externalBuyUrl: '',
  externalPdfUrl: '',
  totalCopies: '',
}

export default function BooksPage() {
  const qc = useQueryClient()
  const navigate = useNavigate()
  const { isLibrarian } = useAuth()
  const { state, setState, query, setPage, toggleSort, errorMessage } = useSearchList(
    'books',
    booksApi.search,
  )

  const [importOpen, setImportOpen] = useState(false)
  const [editing, setEditing] = useState<Book | null | undefined>(undefined) // undefined = closed, null = new
  const [form, setForm] = useState<FormValues>(EMPTY_FORM)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const [formError, setFormError] = useState<string | null>(null)

  const openCreate = () => {
    setEditing(null)
    setForm(EMPTY_FORM)
    setFieldErrors({})
    setFormError(null)
  }

  const openEdit = (book: Book) => {
    setEditing(book)
    setForm({
      isbn: book.isbn,
      title: book.title,
      author: book.author,
      category: book.category,
      publisher: book.publisher,
      publishedYear: String(book.publishedYear),
      description: book.description ?? '',
      coverImageUrl: book.coverImageUrl ?? '',
      edition: book.edition ?? '',
      hasEbook: book.hasEbook,
      ebookUrl: book.ebookUrl ?? '',
      hasAudiobook: book.hasAudiobook,
      audiobookUrl: book.audiobookUrl ?? '',
      externalBuyUrl: book.externalBuyUrl ?? '',
      externalPdfUrl: book.externalPdfUrl ?? '',
      totalCopies: '',
    })
    setFieldErrors({})
    setFormError(null)
  }

  const save = useMutation({
    mutationFn: async () => {
      const body = {
        isbn: form.isbn.trim(),
        title: form.title.trim(),
        author: form.author.trim(),
        category: form.category.trim(),
        publisher: form.publisher.trim(),
        publishedYear: Number(form.publishedYear),
        description: form.description.trim() || null,
        coverImageUrl: form.coverImageUrl.trim() || null,
        edition: form.edition.trim() || null,
        hasEbook: form.hasEbook,
        ebookUrl: form.hasEbook ? form.ebookUrl.trim() || null : null,
        hasAudiobook: form.hasAudiobook,
        audiobookUrl: form.hasAudiobook ? form.audiobookUrl.trim() || null : null,
        externalBuyUrl: form.externalBuyUrl.trim() || null,
        externalPdfUrl: form.externalPdfUrl.trim() || null,
      }
      return editing
        ? booksApi.update(editing.id, body)
        : booksApi.create({ ...body, totalCopies: Number(form.totalCopies) || 0 })
    },
    onSuccess: () => {
      toastSuccess(editing ? t('books.updated') : t('books.added'))
      setEditing(undefined)
      void qc.invalidateQueries({ queryKey: ['books'] })
    },
    onError: (e) => {
      const n = normaliseError(e)
      setFieldErrors(n.fieldErrors)
      setFormError(Object.keys(n.fieldErrors).length ? null : n.message)
    },
  })

  const askDelete = async (book: Book) => {
    const deleted = await cascadeDelete(
      (force) => booksApi.remove(book.id, force),
      { title: t('books.deleteTitle', { title: book.title }), entity: t('common.entity.book') },
    )
    if (deleted) {
      void qc.invalidateQueries({ queryKey: ['books'] })
      void qc.invalidateQueries({ queryKey: ['copies'] })
      void qc.invalidateQueries({ queryKey: ['dashboard'] })
    }
  }

  const columns: Column<Book>[] = [
    {
      key: 'title',
      header: t('books.col.title'),
      sortable: true,
      render: (b) => (
        <div className="flex items-center gap-3">
          <img
            src={coverImageSrc(b)}
            alt=""
            className="h-12 w-9 shrink-0 rounded object-cover ring-1 ring-slate-200"
            onError={(e) => {
              e.currentTarget.onerror = null
              e.currentTarget.src = PLACEHOLDER_COVER
            }}
          />
          <div>
            <p className="font-semibold text-slate-900">{b.title}</p>
            <p className="text-xs text-slate-400">
              {b.author}
              {b.edition && ` · ${b.edition}`}
            </p>
          </div>
        </div>
      ),
    },
    { key: 'isbn', header: t('books.col.isbn'), sortable: true, render: (b) => <span className="font-mono text-xs">{b.isbn}</span> },
    {
      key: 'category',
      header: t('books.col.category'),
      sortable: true,
      render: (b) => (
        <div className="text-sm">
          {b.category}
          <span className="block text-xs text-slate-400">{b.publisher}</span>
        </div>
      ),
    },
    { key: 'publishedYear', header: t('books.col.year'), sortable: true, render: (b) => b.publishedYear },
    {
      key: 'actions',
      header: '',
      className: 'text-right',
      render: (b) => (
        <div className="flex justify-end gap-1">
          <Button variant="ghost" size="sm" onClick={() => navigate(`/books/${b.id}`)}>
            <Eye size={14} />
          </Button>
          {isLibrarian && (
            <>
              <Button variant="ghost" size="sm" onClick={() => openEdit(b)}>
                <Pencil size={14} />
              </Button>
              <Button variant="ghost" size="sm" onClick={() => askDelete(b)}>
                <Trash2 size={14} className="text-rose-500" />
              </Button>
            </>
          )}
        </div>
      ),
    },
  ]

  return (
    <>
      <PageHeader
        title={t('books.title')}
        subtitle={t('books.subtitle')}
        actions={
          isLibrarian && (
            <>
              <Button variant="secondary" onClick={() => setImportOpen(true)}>
                <Upload size={16} /> {t('common.bulkImport')}
              </Button>
              <Button onClick={openCreate}>
                <Plus size={16} /> {t('books.add')}
              </Button>
            </>
          )
        }
      />

      <div className="space-y-4">
        <AdvancedSearch fields={FIELDS} state={state} onChange={setState} />

        <DataTable
          columns={columns}
          rows={query.data?.items ?? []}
          rowKey={(b) => b.id}
          loading={query.isLoading}
          error={errorMessage}
          onRetry={() => void query.refetch()}
          emptyTitle={t('books.emptyTitle')}
          emptyHint={t('books.emptyHint')}
          sort={state.sort}
          onSortChange={toggleSort}
        />
        <Pagination page={state.page} onPageChange={setPage} data={query.data} />
      </div>

      <BulkImportModal
        open={importOpen}
        onClose={() => setImportOpen(false)}
        title={t('books.importTitle')}
        templateUrl={booksApi.importTemplateUrl}
        onImport={booksApi.import}
        onDone={() => void qc.invalidateQueries({ queryKey: ['books'] })}
      />

      <Modal
        open={editing !== undefined}
        onClose={() => setEditing(undefined)}
        title={editing ? t('books.edit') : t('books.add')}
      >
        <form
          className="space-y-3"
          onSubmit={(e) => {
            e.preventDefault()
            save.mutate()
          }}
        >
          {formError && (
            <div className="rounded-lg border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700">
              {formError}
            </div>
          )}
          <FormField label={t('books.col.isbn')} error={fieldErrors.isbn}>
            <TextInput
              value={form.isbn}
              invalid={!!fieldErrors.isbn}
              onChange={(e) => setForm({ ...form, isbn: e.target.value })}
            />
          </FormField>
          <FormField label={t('books.col.title')} error={fieldErrors.title}>
            <TextInput
              value={form.title}
              invalid={!!fieldErrors.title}
              onChange={(e) => setForm({ ...form, title: e.target.value })}
            />
          </FormField>
          <FormField label={t('books.col.author')} error={fieldErrors.author}>
            <TextInput
              value={form.author}
              invalid={!!fieldErrors.author}
              onChange={(e) => setForm({ ...form, author: e.target.value })}
            />
          </FormField>
          <div className="grid grid-cols-2 gap-3">
            <FormField label={t('books.col.category')} error={fieldErrors.category}>
              <TextInput
                value={form.category}
                invalid={!!fieldErrors.category}
                onChange={(e) => setForm({ ...form, category: e.target.value })}
              />
            </FormField>
            <FormField label={t('books.col.publisher')} error={fieldErrors.publisher}>
              <TextInput
                value={form.publisher}
                invalid={!!fieldErrors.publisher}
                onChange={(e) => setForm({ ...form, publisher: e.target.value })}
              />
            </FormField>
          </div>
          <FormField label={t('books.field.publishedYear')} error={fieldErrors.publishedYear}>
            <TextInput
              type="number"
              value={form.publishedYear}
              invalid={!!fieldErrors.publishedYear}
              onChange={(e) => setForm({ ...form, publishedYear: e.target.value })}
            />
          </FormField>
          <FormField label={t('books.field.description')} hint={t('common.optional')}>
            <TextInput
              value={form.description}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
            />
          </FormField>
          <div className="grid grid-cols-2 gap-3">
            <FormField label={t('books.field.coverImageUrl')} hint={t('common.optional')}>
              <TextInput
                value={form.coverImageUrl}
                onChange={(e) => setForm({ ...form, coverImageUrl: e.target.value })}
              />
            </FormField>
            <FormField label={t('books.field.edition')} hint={t('common.optional')}>
              <TextInput
                value={form.edition}
                onChange={(e) => setForm({ ...form, edition: e.target.value })}
              />
            </FormField>
          </div>

          {!editing && (
            <FormField label={t('books.field.totalCopies')} hint={t('common.optional')} error={fieldErrors.totalCopies}>
              <TextInput
                type="number"
                min={0}
                value={form.totalCopies}
                invalid={!!fieldErrors.totalCopies}
                onChange={(e) => setForm({ ...form, totalCopies: e.target.value })}
              />
            </FormField>
          )}

          <div className="space-y-2 rounded-lg border border-slate-200 p-3">
            <label className="flex items-center gap-2 text-sm font-medium text-slate-700">
              <input
                type="checkbox"
                checked={form.hasEbook}
                onChange={(e) => setForm({ ...form, hasEbook: e.target.checked })}
              />
              {t('books.field.hasEbook')}
            </label>
            {form.hasEbook && (
              <TextInput
                placeholder={t('books.field.ebookUrl')}
                value={form.ebookUrl}
                invalid={!!fieldErrors.ebookUrl}
                onChange={(e) => setForm({ ...form, ebookUrl: e.target.value })}
              />
            )}
            {fieldErrors.ebookUrl && <p className="text-xs font-medium text-rose-600">{fieldErrors.ebookUrl}</p>}

            <label className="flex items-center gap-2 text-sm font-medium text-slate-700">
              <input
                type="checkbox"
                checked={form.hasAudiobook}
                onChange={(e) => setForm({ ...form, hasAudiobook: e.target.checked })}
              />
              {t('books.field.hasAudiobook')}
            </label>
            {form.hasAudiobook && (
              <TextInput
                placeholder={t('books.field.audiobookUrl')}
                value={form.audiobookUrl}
                invalid={!!fieldErrors.audiobookUrl}
                onChange={(e) => setForm({ ...form, audiobookUrl: e.target.value })}
              />
            )}
            {fieldErrors.audiobookUrl && <p className="text-xs font-medium text-rose-600">{fieldErrors.audiobookUrl}</p>}
          </div>

          <div className="grid grid-cols-2 gap-3">
            <FormField label={t('books.field.externalBuyUrl')} hint={t('common.optional')}>
              <TextInput
                value={form.externalBuyUrl}
                onChange={(e) => setForm({ ...form, externalBuyUrl: e.target.value })}
              />
            </FormField>
            <FormField label={t('books.field.externalPdfUrl')} hint={t('common.optional')}>
              <TextInput
                value={form.externalPdfUrl}
                onChange={(e) => setForm({ ...form, externalPdfUrl: e.target.value })}
              />
            </FormField>
          </div>

          <div className="flex justify-end gap-2 pt-1">
            <Button type="button" variant="secondary" onClick={() => setEditing(undefined)}>
              {t('common.cancel')}
            </Button>
            <Button type="submit" disabled={save.isPending}>
              {save.isPending ? t('common.saving') : editing ? t('common.saveChanges') : t('books.add')}
            </Button>
          </div>
        </form>
      </Modal>
    </>
  )
}
