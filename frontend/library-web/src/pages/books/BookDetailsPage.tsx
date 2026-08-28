import { useQuery } from '@tanstack/react-query'
import {
  ArrowLeft,
  BookOpen,
  CalendarDays,
  Copy,
  Hash,
  Loader2,
  Mail,
  User,
} from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { booksApi } from '../../api/booksApi'
import { bookCopiesApi } from '../../api/bookCopiesApi'

const copyStatusLabel = (status: number) => {
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

const copyStatusClass = (status: number) => {
  switch (status) {
    case 0:
      return 'is-available'
    case 1:
      return 'is-borrowed'
    default:
      return 'is-unavailable'
  }
}

export default function BookDetailsPage() {
  const { id } = useParams<{ id: string }>()

  const bookQuery = useQuery({
    queryKey: ['book', id],
    queryFn: () => booksApi.getById(id as string),
    enabled: Boolean(id),
  })

  const copiesQuery = useQuery({
    queryKey: ['book-copies', id],
    queryFn: () => bookCopiesApi.getByBookId(id as string),
    enabled: Boolean(id),
  })

  if (bookQuery.isLoading) {
    return (
      <section className="books-page">
        <div className="books-state">
          <Loader2 size={28} className="books-search__loader" />
          <h2>Loading book...</h2>
          <p>Please wait while we load the book details.</p>
        </div>
      </section>
    )
  }

  if (bookQuery.isError || !bookQuery.data) {
    return (
      <section className="books-page">
        <div className="books-state books-state--error">
          <div className="books-state__icon">!</div>
          <h2>Book not found</h2>
          <p>We couldn't load this book from the library service.</p>

          <Link className="book-card__details" to="/books">
            <ArrowLeft size={16} />
            Back to books
          </Link>
        </div>
      </section>
    )
  }

  const book = bookQuery.data
  const copies = copiesQuery.data ?? []
  const availableCopies = copies.filter(
    (copy) => copy.status === 0,
  ).length

  return (
    <section className="books-page">
      <header className="books-page__header">
        <div>
          <Link
            to="/books"
            className="book-card__details"
            style={{
              display: 'inline-flex',
              marginBottom: 18,
              textDecoration: 'none',
            }}
          >
            <ArrowLeft size={15} />
            Back to books
          </Link>

          <div className="books-page__eyebrow">
            <BookOpen size={14} />
            Book details
          </div>

          <h1>{book.title}</h1>

          <p>{book.author}</p>
        </div>

        <Link
          to="/borrowing"
          className="books-add-button"
          style={{ textDecoration: 'none' }}
        >
          Issue a copy
        </Link>
      </header>

      <div className="book-card" style={{ marginBottom: 24 }}>
        <div className="book-card__cover">
          <div className="book-card__cover-glow" />
          <BookOpen size={52} />
          <span>LIBRA</span>
        </div>

        <div className="book-card__content">
          <div className="book-card__meta">
            <span>BOOK</span>
            <span className="book-card__year">
              {book.publishedYear}
            </span>
          </div>

          <h2>{book.title}</h2>

          <p className="book-card__author">
            by {book.author}
          </p>

          <div className="book-card__isbn">
            <span>ISBN</span>
            <strong>{book.isbn}</strong>
          </div>

          {book.description && (
            <p style={{ marginTop: 18, lineHeight: 1.7 }}>
              {book.description}
            </p>
          )}
        </div>
      </div>

      <div className="books-results-summary">
        <strong>Collection information</strong>
        <span>
          {copies.length} total · {availableCopies} available
        </span>
      </div>

      <div className="books-grid">
        <div className="book-card">
          <div className="book-card__content">
            <CalendarDays size={20} />
            <h2>Published</h2>
            <p>{book.publishedYear}</p>
          </div>
        </div>

        <div className="book-card">
          <div className="book-card__content">
            <Hash size={20} />
            <h2>ISBN</h2>
            <p>{book.isbn}</p>
          </div>
        </div>

        <div className="book-card">
          <div className="book-card__content">
            <Copy size={20} />
            <h2>Copies</h2>
            <p>
              {availableCopies} of {copies.length} available
            </p>
          </div>
        </div>
      </div>

      <div style={{ marginTop: 28 }}>
        <div className="books-results-summary">
          <strong>Book copies</strong>

          {copiesQuery.isFetching && (
            <span className="books-results-summary__loading">
              Updating...
            </span>
          )}
        </div>

        {copiesQuery.isError && (
          <div className="books-state books-state--error">
            <h2>Unable to load copies</h2>
            <p>
              The book itself loaded, but its copies could not
              be retrieved.
            </p>
          </div>
        )}

        {!copiesQuery.isLoading &&
          !copiesQuery.isError &&
          copies.length === 0 && (
            <div className="books-state">
              <div className="books-state__icon">
                <Copy size={22} />
              </div>
              <h2>No copies yet</h2>
              <p>
                This book does not have any physical copies in
                the system.
              </p>
            </div>
          )}

        {copies.length > 0 && (
          <div className="books-grid">
            {copies.map((copy) => (
              <article className="book-card" key={copy.id}>
                <div className="book-card__content">
                  <div className="book-card__meta">
                    <span>COPY</span>

                    <span
                      className={`book-card__year ${copyStatusClass(
                        copy.status,
                      )}`}
                    >
                      {copyStatusLabel(copy.status)}
                    </span>
                  </div>

                  <h2>{copy.barcode}</h2>

                  <p className="book-card__author">
                    Physical library copy
                  </p>

                  <div className="book-card__isbn">
                    <span>Copy ID</span>
                    <strong>{copy.id}</strong>
                  </div>
                </div>
              </article>
            ))}
          </div>
        )}
      </div>
    </section>
  )
}