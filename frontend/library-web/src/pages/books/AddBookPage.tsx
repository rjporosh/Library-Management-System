import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  ArrowLeft,
  BookOpen,
  Loader2,
  Save,
} from 'lucide-react'
import {  useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { httpClient } from '../../api/httpClient'

interface CreateBookRequest {
  isbn: string
  title: string
  author: string
  publishedYear: number
  description: string | null
}

export default function AddBookPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const [isbn, setIsbn] = useState('')
  const [title, setTitle] = useState('')
  const [author, setAuthor] = useState('')
  const [publishedYear, setPublishedYear] = useState('')
  const [description, setDescription] = useState('')
  const [errorMessage, setErrorMessage] = useState('')

  const mutation = useMutation({
    mutationFn: async (request: CreateBookRequest) => {
      const response = await httpClient.post(
        '/Books',
        request,
      )

      return response.data
    },

    onSuccess: (book) => {
      queryClient.invalidateQueries({
        queryKey: ['books'],
      })

      if (book?.id) {
        navigate(`/books/${book.id}`)
      } else {
        navigate('/books')
      }
    },

    onError: (error: any) => {
      setErrorMessage(
        error?.response?.data?.detail ||
          error?.response?.data?.title ||
          'Unable to create the book.',
      )
    },
  })

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setErrorMessage('')

    if (!isbn.trim() || !title.trim() || !author.trim()) {
      setErrorMessage(
        'ISBN, title and author are required.',
      )
      return
    }

    const year = Number(publishedYear)

    if (!publishedYear || !Number.isInteger(year)) {
      setErrorMessage('Please enter a valid published year.')
      return
    }

    mutation.mutate({
      isbn: isbn.trim(),
      title: title.trim(),
      author: author.trim(),
      publishedYear: year,
      description: description.trim() || null,
    })
  }

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
            Library collection
          </div>

          <h1>Add Book</h1>

          <p>
            Add a new title to your library collection.
          </p>
        </div>
      </header>

      <form
        onSubmit={handleSubmit}
        className="book-card"
        style={{
          display: 'block',
          maxWidth: 820,
          padding: 28,
        }}
      >
        {errorMessage && (
          <div
            className="books-state books-state--error"
            style={{ marginBottom: 24 }}
          >
            <div className="books-state__icon">!</div>
            <p>{errorMessage}</p>
          </div>
        )}

        <div
          style={{
            display: 'grid',
            gridTemplateColumns:
              'repeat(auto-fit, minmax(240px, 1fr))',
            gap: 20,
          }}
        >
          <label>
            <span>ISBN</span>
            <input
              value={isbn}
              onChange={(event) =>
                setIsbn(event.target.value)
              }
              placeholder="978..."
              required
              style={{
                width: '100%',
                marginTop: 8,
                padding: 12,
              }}
            />
          </label>

          <label>
            <span>Published Year</span>
            <input
              type="number"
              value={publishedYear}
              onChange={(event) =>
                setPublishedYear(event.target.value)
              }
              placeholder="2026"
              required
              style={{
                width: '100%',
                marginTop: 8,
                padding: 12,
              }}
            />
          </label>

          <label>
            <span>Title</span>
            <input
              value={title}
              onChange={(event) =>
                setTitle(event.target.value)
              }
              placeholder="Book title"
              required
              style={{
                width: '100%',
                marginTop: 8,
                padding: 12,
              }}
            />
          </label>

          <label>
            <span>Author</span>
            <input
              value={author}
              onChange={(event) =>
                setAuthor(event.target.value)
              }
              placeholder="Author name"
              required
              style={{
                width: '100%',
                marginTop: 8,
                padding: 12,
              }}
            />
          </label>
        </div>

        <label
          style={{
            display: 'block',
            marginTop: 20,
          }}
        >
          <span>Description</span>

          <textarea
            value={description}
            onChange={(event) =>
              setDescription(event.target.value)
            }
            placeholder="Optional description..."
            rows={6}
            style={{
              width: '100%',
              marginTop: 8,
              padding: 12,
              resize: 'vertical',
            }}
          />
        </label>

        <div
          style={{
            display: 'flex',
            gap: 12,
            marginTop: 24,
          }}
        >
          <Link
            to="/books"
            className="book-card__details"
            style={{
              textDecoration: 'none',
              padding: '11px 16px',
            }}
          >
            Cancel
          </Link>

          <button
            className="books-add-button"
            type="submit"
            disabled={mutation.isPending}
          >
            {mutation.isPending ? (
              <Loader2 size={17} />
            ) : (
              <Save size={17} />
            )}

            {mutation.isPending
              ? 'Saving...'
              : 'Save Book'}
          </button>
        </div>
      </form>
    </section>
  )
}