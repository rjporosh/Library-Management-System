import { useMutation, useQuery } from '@tanstack/react-query'
import {
  BookMarked,
  CalendarDays,
  CheckCircle2,
  Loader2,
  RotateCcw,
} from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { httpClient } from '../../api/httpClient'

export default function BorrowingPage() {
  const [memberId, setMemberId] = useState('')
  const [bookCopyId, setBookCopyId] = useState('')
  const [dueAt, setDueAt] = useState('')
  const [borrowRecordId, setBorrowRecordId] =
    useState('')
  const [message, setMessage] = useState('')
  const [errorMessage, setErrorMessage] = useState('')

  const memberQuery = useQuery({
    queryKey: ['borrow-member', memberId],
    queryFn: () =>
      httpClient
        .get(`/Members/${memberId}`)
        .then((response) => response.data),
    enabled: false,
    retry: false,
  })

  const copyQuery = useQuery({
    queryKey: ['borrow-copy', bookCopyId],
    queryFn: () =>
      httpClient
        .get(`/book-copies/${bookCopyId}`)
        .then((response) => response.data),
    enabled: false,
    retry: false,
  })

  const issueMutation = useMutation({
    mutationFn: async () => {
      const response = await httpClient.post(
        '/borrowing/issue',
        {
          memberId,
          bookCopyId,
          dueAt: new Date(dueAt).toISOString(),
        },
      )

      return response.data
    },

    onSuccess: (record) => {
      setMessage(
        'Book issued successfully.',
      )
      setErrorMessage('')

      if (record?.id) {
        setBorrowRecordId(record.id)
      }
    },

    onError: (error: any) => {
      setMessage('')
      setErrorMessage(
        error?.response?.data?.detail ||
          error?.response?.data?.title ||
          'Unable to issue the book.',
      )
    },
  })

  const returnMutation = useMutation({
    mutationFn: async () => {
      const response = await httpClient.post(
        `/borrowing/${borrowRecordId}/return`,
        {
          returnedAt: new Date().toISOString(),
        },
      )

      return response.data
    },

    onSuccess: () => {
      setMessage(
        'Book returned successfully.',
      )
      setErrorMessage('')
    },

    onError: (error: any) => {
      setMessage('')
      setErrorMessage(
        error?.response?.data?.detail ||
          error?.response?.data?.title ||
          'Unable to return the book.',
      )
    },
  })

  const handleIssue = (event: FormEvent) => {
    event.preventDefault()

    setMessage('')
    setErrorMessage('')

    if (
      !memberId.trim() ||
      !bookCopyId.trim() ||
      !dueAt
    ) {
      setErrorMessage(
        'Member ID, Book Copy ID and due date are required.',
      )
      return
    }

    issueMutation.mutate()
  }

  const lookupMember = () => {
    if (memberId.trim()) {
      memberQuery.refetch()
    }
  }

  const lookupCopy = () => {
    if (bookCopyId.trim()) {
      copyQuery.refetch()
    }
  }

  return (
    <section className="books-page">
      <header className="books-page__header">
        <div>
          <div className="books-page__eyebrow">
            <BookMarked size={14} />
            Circulation
          </div>

          <h1>Borrowing</h1>

          <p>
            Issue books to members and process returns.
          </p>
        </div>
      </header>

      <div
        className="book-card"
        style={{
          display: 'block',
          padding: 28,
        }}
      >
        <h2>Issue Book</h2>

        <form
          onSubmit={handleIssue}
          style={{ marginTop: 20 }}
        >
          <div
            style={{
              display: 'grid',
              gridTemplateColumns:
                'repeat(auto-fit, minmax(250px, 1fr))',
              gap: 18,
            }}
          >
            <label>
              <span>Member ID</span>

              <input
                value={memberId}
                onChange={(event) =>
                  setMemberId(event.target.value)
                }
                placeholder="Member UUID"
                style={{
                  width: '100%',
                  marginTop: 8,
                  padding: 12,
                }}
              />

              <button
                type="button"
                className="book-card__details"
                onClick={lookupMember}
                style={{ marginTop: 8 }}
              >
                Check member
              </button>
            </label>

            <label>
              <span>Book Copy ID</span>

              <input
                value={bookCopyId}
                onChange={(event) =>
                  setBookCopyId(event.target.value)
                }
                placeholder="Book copy UUID"
                style={{
                  width: '100%',
                  marginTop: 8,
                  padding: 12,
                }}
              />

              <button
                type="button"
                className="book-card__details"
                onClick={lookupCopy}
                style={{ marginTop: 8 }}
              >
                Check copy
              </button>
            </label>

            <label>
              <span>Due Date</span>

              <input
                type="datetime-local"
                value={dueAt}
                onChange={(event) =>
                  setDueAt(event.target.value)
                }
                style={{
                  width: '100%',
                  marginTop: 8,
                  padding: 12,
                }}
              />
            </label>
          </div>

          {memberQuery.data && (
            <div style={{ marginTop: 18 }}>
              <CheckCircle2 size={16} />
              Member: <strong>{memberQuery.data.name}</strong>
            </div>
          )}

          {copyQuery.data && (
            <div style={{ marginTop: 10 }}>
              <CheckCircle2 size={16} />
              Copy: <strong>{copyQuery.data.barcode}</strong>
              {' · '}
              Status: <strong>{copyQuery.data.status}</strong>
            </div>
          )}

          <button
            className="books-add-button"
            type="submit"
            disabled={issueMutation.isPending}
            style={{ marginTop: 22 }}
          >
            {issueMutation.isPending ? (
              <Loader2 size={17} />
            ) : (
              <BookMarked size={17} />
            )}

            {issueMutation.isPending
              ? 'Issuing...'
              : 'Issue Book'}
          </button>
        </form>
      </div>

      <div
        className="book-card"
        style={{
          display: 'block',
          marginTop: 24,
          padding: 28,
        }}
      >
        <h2>Return Book</h2>

        <p style={{ marginTop: 8 }}>
          Enter the borrow record ID returned when a book was
          issued.
        </p>

        <div
          style={{
            display: 'flex',
            gap: 12,
            marginTop: 18,
            flexWrap: 'wrap',
          }}
        >
          <input
            value={borrowRecordId}
            onChange={(event) =>
              setBorrowRecordId(event.target.value)
            }
            placeholder="Borrow record UUID"
            style={{
              flex: '1 1 300px',
              padding: 12,
            }}
          />

          <button
            className="books-add-button"
            type="button"
            disabled={
              !borrowRecordId ||
              returnMutation.isPending
            }
            onClick={() => {
              setMessage('')
              setErrorMessage('')
              returnMutation.mutate()
            }}
          >
            {returnMutation.isPending ? (
              <Loader2 size={17} />
            ) : (
              <RotateCcw size={17} />
            )}

            {returnMutation.isPending
              ? 'Returning...'
              : 'Return Book'}
          </button>
        </div>
      </div>

      {(message || errorMessage) && (
        <div
          className={`books-state ${
            errorMessage
              ? 'books-state--error'
              : ''
          }`}
          style={{ marginTop: 24 }}
        >
          <div className="books-state__icon">
            {errorMessage ? '!' : <CheckCircle2 size={22} />}
          </div>

          <p>{errorMessage || message}</p>
        </div>
      )}

      <div
        className="books-grid"
        style={{ marginTop: 24 }}
      >
        <div className="book-card">
          <div className="book-card__content">
            <CalendarDays size={22} />
            <h2>Issue workflow</h2>
            <p>
              Member → Book Copy → Due Date → Issue
            </p>
          </div>
        </div>

        <div className="book-card">
          <div className="book-card__content">
            <RotateCcw size={22} />
            <h2>Return workflow</h2>
            <p>
              Borrow Record → Return
            </p>
          </div>
        </div>
      </div>
    </section>
  )
}