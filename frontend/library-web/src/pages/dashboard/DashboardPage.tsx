import {
  ArrowUpRight,
  BookOpen,
  BookMarked,
  Clock3,
  Library,
  Search,
  Sparkles,
  Users,
} from 'lucide-react'
import { useQuery } from '@tanstack/react-query'
import { booksApi } from '../../api/booksApi'
import { bookCopiesApi } from '../../api/bookCopiesApi'
import { useMemo } from 'react'

function DashboardPage() {
  const booksQuery = useQuery({
    queryKey: ['books', 'dashboard'],
    queryFn: () =>
      booksApi.getAll({
        pageNumber: 1,
        pageSize: 100,
      }),
  })

  const books = booksQuery.data?.items ?? []

  const copyQueries = books.map((book) =>
    bookCopiesApi.getByBookId(book.id),
  )

  const copiesQuery = useQuery({
    queryKey: ['book-copies', 'dashboard', books.map((book) => book.id)],
    queryFn: async () => {
      const results = await Promise.all(copyQueries)
      return results.flat()
    },
    enabled: books.length > 0,
  })

  const copies = copiesQuery.data ?? []

  const availableCopies = useMemo(
    () => copies.filter((copy) => copy.status === 'Available').length,
    [copies],
  )

  const borrowedCopies = useMemo(
    () => copies.filter((copy) => copy.status === 'Borrowed').length,
    [copies],
  )

  const loading =
    booksQuery.isLoading || copiesQuery.isLoading

  const stats = [
    {
      label: 'Total Books',
      value: books.length,
      caption: 'Titles in catalog',
      icon: BookOpen,
      className: 'dashboard-stat--blue',
    },
    {
      label: 'Available Copies',
      value: availableCopies,
      caption: 'Ready to borrow',
      icon: BookMarked,
      className: 'dashboard-stat--green',
    },
    {
      label: 'Borrowed Copies',
      value: borrowedCopies,
      caption: 'Currently issued',
      icon: Clock3,
      className: 'dashboard-stat--orange',
    },
    {
      label: 'Members',
      value: '—',
      caption: 'Member directory',
      icon: Users,
      className: 'dashboard-stat--purple',
    },
  ]

  return (
    <section className="dashboard-page">
      <div className="page-heading dashboard-heading">
        <div>
          <div className="eyebrow">
            <Sparkles size={14} />
            Library overview
          </div>

          <h1>Good evening, Librarian.</h1>

          <p>
            Everything you need to keep your library moving,
            beautifully organized.
          </p>
        </div>

        <div className="dashboard-live">
          <span />
          Live system
        </div>
      </div>

      <div className="dashboard-hero">
        <div className="dashboard-hero__content">
          <span className="dashboard-hero__eyebrow">
            LIBRA LIBRARY SYSTEM
          </span>

          <h2>
            Your library,
            <br />
            <em>beautifully managed.</em>
          </h2>

          <p>
            Search the collection, manage physical copies,
            issue books and keep every borrowing journey
            under control.
          </p>

          <a
            className="dashboard-hero__action"
            href="/books"
          >
            Explore library
            <ArrowUpRight size={17} />
          </a>
        </div>

        <div className="dashboard-hero__visual">
          <div className="hero-book hero-book--back" />
          <div className="hero-book hero-book--middle" />
          <div className="hero-book hero-book--front">
            <BookOpen size={48} />
            <span>LIBRA</span>
          </div>

          <div className="hero-orbit hero-orbit--one" />
          <div className="hero-orbit hero-orbit--two" />
        </div>
      </div>

      <div className="dashboard-stats">
        {stats.map((stat) => {
          const Icon = stat.icon

          return (
            <article
              className={`dashboard-stat ${stat.className}`}
              key={stat.label}
            >
              <div className="dashboard-stat__top">
                <span className="dashboard-stat__icon">
                  <Icon size={19} />
                </span>

                <span className="dashboard-stat__trend">
                  LIVE
                </span>
              </div>

              <div className="dashboard-stat__value">
                {loading ? '—' : stat.value}
              </div>

              <div className="dashboard-stat__label">
                {stat.label}
              </div>

              <div className="dashboard-stat__caption">
                {stat.caption}
              </div>
            </article>
          )
        })}
      </div>

      <div className="dashboard-grid">
        <article className="dashboard-panel dashboard-panel--wide">
          <div className="panel-heading">
            <div>
              <span className="panel-kicker">QUICK ACTIONS</span>
              <h3>What would you like to do?</h3>
            </div>
          </div>

          <div className="quick-actions">
            <a href="/books" className="quick-action">
              <span className="quick-action__icon">
                <Search size={20} />
              </span>

              <span>
                <strong>Search books</strong>
                <small>Find a title, author or ISBN</small>
              </span>

              <ArrowUpRight size={17} />
            </a>

            <a href="/borrowing" className="quick-action">
              <span className="quick-action__icon quick-action__icon--green">
                <Library size={20} />
              </span>

              <span>
                <strong>Issue a book</strong>
                <small>Give an available copy to a member</small>
              </span>

              <ArrowUpRight size={17} />
            </a>

            <a href="/borrowing" className="quick-action">
              <span className="quick-action__icon quick-action__icon--orange">
                <Clock3 size={20} />
              </span>

              <span>
                <strong>Return a book</strong>
                <small>Process an active borrowing</small>
              </span>

              <ArrowUpRight size={17} />
            </a>
          </div>
        </article>

        <article className="dashboard-panel">
          <div className="panel-heading">
            <div>
              <span className="panel-kicker">CATALOG</span>
              <h3>Collection snapshot</h3>
            </div>
          </div>

          <div className="collection-summary">
            <div className="collection-summary__ring">
              <div>
                <strong>
                  {copies.length === 0
                    ? '—'
                    : Math.round(
                        (availableCopies / copies.length) * 100,
                      )}
                  %
                </strong>

                <span>available</span>
              </div>
            </div>

            <div className="collection-summary__legend">
              <div>
                <span className="legend-dot legend-dot--green" />
                <span>Available</span>
                <strong>{availableCopies}</strong>
              </div>

              <div>
                <span className="legend-dot legend-dot--orange" />
                <span>Borrowed</span>
                <strong>{borrowedCopies}</strong>
              </div>
            </div>
          </div>
        </article>
      </div>
    </section>
  )
}

export default DashboardPage