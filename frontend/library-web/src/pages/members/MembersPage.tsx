import { useMutation, useQuery } from '@tanstack/react-query'
import {
  Loader2,
  Mail,
  Plus,
  Search,
  User,
  Users,
} from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { httpClient } from '../../api/httpClient'

export default function MembersPage() {
  const [lookupId, setLookupId] = useState('')
  const [memberId, setMemberId] = useState('')
  const [membershipNumber, setMembershipNumber] =
    useState('')
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [errorMessage, setErrorMessage] = useState('')
  const [successMessage, setSuccessMessage] =
    useState('')

  const memberQuery = useQuery({
    queryKey: ['member', lookupId],
    queryFn: () =>
      httpClient
        .get(`/Members/${lookupId}`)
        .then((response) => response.data),
    enabled: Boolean(lookupId),
    retry: false,
  })

  const createMutation = useMutation({
    mutationFn: async () => {
      const response = await httpClient.post(
        '/Members',
        {
          membershipNumber: membershipNumber.trim(),
          name: name.trim(),
          email: email.trim(),
        },
      )

      return response.data
    },

    onSuccess: (member) => {
      setErrorMessage('')
      setSuccessMessage('Member created successfully.')

      if (member?.id) {
        setMemberId(member.id)
        setLookupId(member.id)
      }

      setMembershipNumber('')
      setName('')
      setEmail('')
    },

    onError: (error: any) => {
      setSuccessMessage('')
      setErrorMessage(
        error?.response?.data?.detail ||
          error?.response?.data?.title ||
          'Unable to create member.',
      )
    },
  })

  const handleLookup = (event: FormEvent) => {
    event.preventDefault()

    if (!memberId.trim()) {
      return
    }

    setLookupId(memberId.trim())
    setErrorMessage('')
    setSuccessMessage('')
  }

  const handleCreate = (event: FormEvent) => {
    event.preventDefault()

    setErrorMessage('')
    setSuccessMessage('')

    if (
      !membershipNumber.trim() ||
      !name.trim() ||
      !email.trim()
    ) {
      setErrorMessage(
        'Membership number, name and email are required.',
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
            <Users size={14} />
            Library members
          </div>

          <h1>Members</h1>

          <p>
            Create members and look up their library
            membership details.
          </p>
        </div>
      </header>

      <div className="books-grid">
        <div
          className="book-card"
          style={{
            display: 'block',
            padding: 24,
          }}
        >
          <h2>Find Member</h2>

          <form
            onSubmit={handleLookup}
            style={{ marginTop: 18 }}
          >
            <label>
              <span>Member ID</span>

              <input
                value={memberId}
                onChange={(event) =>
                  setMemberId(event.target.value)
                }
                placeholder="UUID"
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
              style={{ marginTop: 14 }}
            >
              <Search size={17} />
              Find Member
            </button>
          </form>
        </div>

        <div
          className="book-card"
          style={{
            display: 'block',
            padding: 24,
          }}
        >
          <h2>Add Member</h2>

          <form
            onSubmit={handleCreate}
            style={{ marginTop: 18 }}
          >
            <label style={{ display: 'block' }}>
              <span>Membership Number</span>
              <input
                value={membershipNumber}
                onChange={(event) =>
                  setMembershipNumber(event.target.value)
                }
                placeholder="MEM-001"
                style={{
                  width: '100%',
                  marginTop: 8,
                  padding: 12,
                }}
              />
            </label>

            <label
              style={{
                display: 'block',
                marginTop: 14,
              }}
            >
              <span>Name</span>
              <input
                value={name}
                onChange={(event) =>
                  setName(event.target.value)
                }
                placeholder="Member name"
                style={{
                  width: '100%',
                  marginTop: 8,
                  padding: 12,
                }}
              />
            </label>

            <label
              style={{
                display: 'block',
                marginTop: 14,
              }}
            >
              <span>Email</span>
              <input
                type="email"
                value={email}
                onChange={(event) =>
                  setEmail(event.target.value)
                }
                placeholder="member@example.com"
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
              style={{ marginTop: 16 }}
            >
              {createMutation.isPending ? (
                <Loader2 size={17} />
              ) : (
                <Plus size={17} />
              )}

              Create Member
            </button>
          </form>
        </div>
      </div>

      {errorMessage && (
        <div
          className="books-state books-state--error"
          style={{ marginTop: 24 }}
        >
          <div className="books-state__icon">!</div>
          <p>{errorMessage}</p>
        </div>
      )}

      {successMessage && (
        <div
          className="books-state"
          style={{ marginTop: 24 }}
        >
          <div className="books-state__icon">✓</div>
          <p>{successMessage}</p>
        </div>
      )}

      {memberQuery.isLoading && (
        <div className="books-state">
          <Loader2 size={28} />
          <h2>Loading member...</h2>
        </div>
      )}

      {memberQuery.isError && !memberQuery.isLoading && (
        <div
          className="books-state books-state--error"
          style={{ marginTop: 24 }}
        >
          <div className="books-state__icon">!</div>
          <h2>Member not found</h2>
          <p>
            No member could be loaded for this ID.
          </p>
        </div>
      )}

      {memberQuery.data && (
        <div
          className="book-card"
          style={{
            display: 'block',
            marginTop: 24,
            padding: 28,
          }}
        >
          <div className="books-page__eyebrow">
            <User size={14} />
            Member profile
          </div>

          <h2 style={{ marginTop: 10 }}>
            {memberQuery.data.name}
          </h2>

          <div
            style={{
              display: 'grid',
              gridTemplateColumns:
                'repeat(auto-fit, minmax(220px, 1fr))',
              gap: 18,
              marginTop: 22,
            }}
          >
            <div>
              <strong>Membership Number</strong>
              <p>{memberQuery.data.membershipNumber}</p>
            </div>

            <div>
              <strong>Email</strong>
              <p>
                <Mail size={14} style={{ marginRight: 6 }} />
                {memberQuery.data.email}
              </p>
            </div>

            <div>
              <strong>Member ID</strong>
              <p>{memberQuery.data.id}</p>
            </div>

            <div>
              <strong>Status</strong>
              <p>{memberQuery.data.status}</p>
            </div>
          </div>
        </div>
      )}
    </section>
  )
}