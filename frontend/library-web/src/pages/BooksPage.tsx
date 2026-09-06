import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Pencil, Plus, Trash2, Upload } from 'lucide-react'
import { useState } from 'react'
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
import { cascadeDelete, normaliseError, toastSuccess } from '@/lib/api'
import type { Book } from '@/lib/types'
import { useSearchList } from '@/lib/useSearchList'

const FIELDS: FieldDef[] = [
  { name: 'title', label: 'Title', type: 'text' },
  { name: 'author', label: 'Author', type: 'text' },
  { name: 'isbn', label: 'ISBN', type: 'text' },
  { name: 'category', label: 'Category', type: 'text' },
  { name: 'publisher', label: 'Publisher', type: 'text' },
  { name: 'publishedYear', label: 'Published year', type: 'number' },
]

interface FormValues {
  isbn: string
  title: string
  author: string
  category: string
  publisher: string
  publishedYear: string
  description: string
}

const EMPTY_FORM: FormValues = {
  isbn: '',
  title: '',
  author: '',
  category: '',
  publisher: '',
  publishedYear: '',
  description: '',
}

export default function BooksPage() {
  const qc = useQueryClient()
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
      }
      return editing
        ? booksApi.update(editing.id, body)
        : booksApi.create(body)
    },
    onSuccess: () => {
      toastSuccess(editing ? 'Book updated' : 'Book added')
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
      { title: `Delete “${book.title}”?`, entity: 'book' },
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
      header: 'Title',
      sortable: true,
      render: (b) => (
        <div>
          <p className="font-semibold text-slate-900">{b.title}</p>
          <p className="text-xs text-slate-400">{b.author}</p>
        </div>
      ),
    },
    { key: 'isbn', header: 'ISBN', sortable: true, render: (b) => <span className="font-mono text-xs">{b.isbn}</span> },
    {
      key: 'category',
      header: 'Category',
      sortable: true,
      render: (b) => (
        <div className="text-sm">
          {b.category}
          <span className="block text-xs text-slate-400">{b.publisher}</span>
        </div>
      ),
    },
    { key: 'publishedYear', header: 'Year', sortable: true, render: (b) => b.publishedYear },
    {
      key: 'actions',
      header: '',
      className: 'text-right',
      render: (b) => (
        <div className="flex justify-end gap-1">
          <Button variant="ghost" size="sm" onClick={() => openEdit(b)}>
            <Pencil size={14} />
          </Button>
          <Button variant="ghost" size="sm" onClick={() => askDelete(b)}>
            <Trash2 size={14} className="text-rose-500" />
          </Button>
        </div>
      ),
    },
  ]

  return (
    <>
      <PageHeader
        title="Books"
        subtitle="Catalogue titles"
        actions={
          <>
            <Button variant="secondary" onClick={() => setImportOpen(true)}>
              <Upload size={16} /> Bulk import
            </Button>
            <Button onClick={openCreate}>
              <Plus size={16} /> Add book
            </Button>
          </>
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
          emptyTitle="No books found"
          emptyHint="Adjust your filters or add a new book."
          sort={state.sort}
          onSortChange={toggleSort}
        />
        <Pagination page={state.page} onPageChange={setPage} data={query.data} />
      </div>

      <BulkImportModal
        open={importOpen}
        onClose={() => setImportOpen(false)}
        title="Bulk import books"
        templateUrl={booksApi.importTemplateUrl}
        onImport={booksApi.import}
        onDone={() => void qc.invalidateQueries({ queryKey: ['books'] })}
      />

      <Modal
        open={editing !== undefined}
        onClose={() => setEditing(undefined)}
        title={editing ? 'Edit book' : 'Add book'}
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
          <FormField label="ISBN" error={fieldErrors.isbn}>
            <TextInput
              value={form.isbn}
              invalid={!!fieldErrors.isbn}
              onChange={(e) => setForm({ ...form, isbn: e.target.value })}
            />
          </FormField>
          <FormField label="Title" error={fieldErrors.title}>
            <TextInput
              value={form.title}
              invalid={!!fieldErrors.title}
              onChange={(e) => setForm({ ...form, title: e.target.value })}
            />
          </FormField>
          <FormField label="Author" error={fieldErrors.author}>
            <TextInput
              value={form.author}
              invalid={!!fieldErrors.author}
              onChange={(e) => setForm({ ...form, author: e.target.value })}
            />
          </FormField>
          <div className="grid grid-cols-2 gap-3">
            <FormField label="Category" error={fieldErrors.category}>
              <TextInput
                value={form.category}
                invalid={!!fieldErrors.category}
                onChange={(e) => setForm({ ...form, category: e.target.value })}
              />
            </FormField>
            <FormField label="Publisher" error={fieldErrors.publisher}>
              <TextInput
                value={form.publisher}
                invalid={!!fieldErrors.publisher}
                onChange={(e) => setForm({ ...form, publisher: e.target.value })}
              />
            </FormField>
          </div>
          <FormField label="Published year" error={fieldErrors.publishedYear}>
            <TextInput
              type="number"
              value={form.publishedYear}
              invalid={!!fieldErrors.publishedYear}
              onChange={(e) => setForm({ ...form, publishedYear: e.target.value })}
            />
          </FormField>
          <FormField label="Description" hint="Optional">
            <TextInput
              value={form.description}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
            />
          </FormField>
          <div className="flex justify-end gap-2 pt-1">
            <Button type="button" variant="secondary" onClick={() => setEditing(undefined)}>
              Cancel
            </Button>
            <Button type="submit" disabled={save.isPending}>
              {save.isPending ? 'Saving…' : editing ? 'Save changes' : 'Add book'}
            </Button>
          </div>
        </form>
      </Modal>
    </>
  )
}
