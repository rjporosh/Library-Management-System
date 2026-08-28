import {
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query'
import {
  BookMarked,
  Copy,
  Loader2,
  Plus,
  Search,
} from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { httpClient } from '../../api/httpClient'
import { booksApi } from '../../api/booksApi'

const statusLabel = (status: number) => {
  switch (status) {
    case 0:
      return 'Available'
    case 1:
      return 'Borrowed'
    case 2:
      return 'Lost'
    case 3:
      return 'Damaged'
    default:
      return `Status ${status}`
  }
}

export default function BookCopiesPage() {
  const queryClient = useQueryClient()

  const [bookId, setBookId] = useState('')
  const [barcode, setBarcode] = useState('')
  const [selectedBookId, setSelectedBookId] = useState('')
  const [errorMessage, setErrorMessage] = useState('')

  const booksQuery = useQuery({
    queryKey: ['books', 'copy-selector'],
    queryFn: () =>
      booksApi.getAll({
        pageNumber: 1,
        pageSize: 100,
        sortBy: 'title',
        sortDirection: 'asc',
      }),
  })

  const copiesQuery = useQuery({
    queryKey: ['book-copies', selectedBookId],
    queryFn: () =>
      httpClient
        .get(`/book-copies/book/${selectedBookId}`)
        .then((response) => response.data),
    enabled: Boolean(selectedBookId),
  })

  const createMutation = useMutation({
    mutationFn: async () => {
      const response = await httpClient.post(
        '/book-copies',
        {
          bookId,
          barcode: barcode.trim(),
        },
      )

      return response.data
    },

    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: ['book-copies', bookId],
      })

      if (selectedBookId === bookId) {
        queryClient.invalidateQueries({
          queryKey: ['book-copies', selectedBookId],
        })
      }

      setBarcode('')
      setErrorMessage('')
    },

    onError: (error: any) => {
      setErrorMessage(
        error?.response?.data?.detail ||
          error?.response?.data?.title ||
          'Unable to create book copy.',
      )
    },
  })

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    setErrorMessage('')

    if (!bookId || !barcode.trim()) {
      setErrorMessage(
        'Select a book and enter a barcode.',
      )
      return
    }

    createMutation.mutate()
  }

  return (
    <section className="books-page">
      <header className="books-page__header">
        <div>
          <div className="books-page__eyebrow">
            <BookMarked size={14} />
            Inventory
          </div>

          <h1>Book Copies</h1>

          <p>
            Manage the physical copies available for each
            book.
          </p>
        </div>
      </header>

      <div
        className="book-card"
        style={{
          display: 'block',
          marginBottom: 28,
          padding: 24,
        }}
      >
        <h2>Add Book Copy</h2>

        <form
          onSubmit={handleSubmit}
          style={{
            display: 'grid',
            gridTemplateColumns:
              'minmax(240px, 1fr) minmax(200px, 1fr) auto',
            gap: 14,
            alignItems: 'end',
            marginTop: 18,
          }}
        >
          <label>
            <span>Book</span>

            <select
              value={bookId}
              onChange={(event) => {
                setBookId(event.target.value)
                setSelectedBookId(event.target.value)
              }}
              style={{
                width: '100%',
                marginTop: 8,
                padding: 12,
              }}
            >
              <option value="">Select a book</option>

              {booksQuery.data?.items.map((book) => (
                <option key={book.id} value={book.id}>
                  {book.title} — {book.isbn}
                </option>
              ))}
            </select>
          </label>

          <label>
            <span>Barcode</span>

            <input
              value={barcode}
              onChange={(event) =>
                setBarcode(event.target.value)
              }
              placeholder="COPY-001"
              style={{
                width: '100%',
                marginTop: 8,
                padding: 12,
              }}
            />
          </label>

          <button
            className="books-add-button"
            type="submit"
            disabled={createMutation.isPending}
          >
            {createMutation.isPending ? (
              <Loader2 size={17} />
            ) : (
              <Plus size={17} />
            )}

            Add Copy
          </button>
        </form>

        {errorMessage && (
          <p
            style={{
              color: '#c0392b',
              marginTop: 14,
            }}
          >
            {errorMessage}
          </p>
        )}
      </div>

      <div className="books-results-summary">
        <strong>Find copies</strong>

        {selectedBookId && (
          <span>
            {copiesQuery.isFetching
              ? 'Loading...'
              : `${copiesQuery.data?.length ?? 0} copies`}
          </span>
        )}
      </div>

      {!selectedBookId && (
        <div className="books-state">
          <div className="books-state__icon">
            <Search size={22} />
          </div>
          <h2>Select a book</h2>
          <p>
            Choose a book above to see its physical copies.
          </p>
        </div>
      )}

      {selectedBookId &&
        copiesQuery.isLoading && (
          <div className="books-state">
            <Loader2 size={28} />
            <h2>Loading copies...</h2>
          </div>
        )}

      {selectedBookId &&
        !copiesQuery.isLoading &&
        !copiesQuery.isError &&
        (copiesQuery.data?.length ?? 0) === 0 && (
          <div className="books-state">
            <div className="books-state__icon">
              <Copy size={22} />
            </div>
            <h2>No copies found</h2>
            <p>Add the first physical copy using the form above.</p>
          </div>
        )}

      {selectedBookId &&
        !copiesQuery.isLoading &&
        !copiesQuery.isError &&
        (copiesQuery.data?.length ?? 0) > 0 && (
          <div className="books-grid">
            {copiesQuery.data.map((copy: any) => (
              <article
                className="book-card"
                key={copy.id}
              >
                <div className="book-card__cover">
                  <div className="book-card__cover-glow" />
                  <Copy size={38} />
                  <span>COPY</span>
                </div>

                <div className="book-card__content">
                  <div className="book-card__meta">
                    <span>BARCODE</span>

                    <span className="book-card__year">
                      {statusLabel(copy.status)}
                    </span>
                  </div>

                  <h2>{copy.barcode}</h2>

                  <p className="book-card__author">
                    Physical library copy
                  </p>

                  <div className="book-card__isbn">
                    <span>ID</span>
                    <strong>{copy.id}</strong>
                  </div>
                </div>
              </article>
            ))}
          </div>
        )}
    </section>
  )
}
