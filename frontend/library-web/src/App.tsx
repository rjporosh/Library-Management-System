import {
  BookOpen,
  BookMarked,
  ChevronDown,
  CircleUserRound,
  LayoutDashboard,
  Library,
  Menu,
  Users,
  X,
} from 'lucide-react'
import { useState } from 'react'
import './App.css'
import { NavLink, Route, Routes } from 'react-router-dom'
import DashboardPage from './pages/dashboard/DashboardPage'
import BooksPage from './pages/books/BooksPage'
import BookCopiesPage from './pages/book-copies/BookCopiesPage'
import MembersPage from './pages/members/MembersPage'
import BorrowingPage from './pages/borrowing/BorrowingPage'


const navigation = [
  {
    label: 'Overview',
    items: [
      {
        label: 'Dashboard',
        path: '/',
        icon: LayoutDashboard,
      },
    ],
  },
  {
    label: 'Library',
    items: [
      {
        label: 'Books',
        path: '/books',
        icon: BookOpen,
      },
      {
        label: 'Book Copies',
        path: '/book-copies',
        icon: BookMarked,
      },
      {
        label: 'Members',
        path: '/members',
        icon: Users,
      },
      {
        label: 'Borrowing',
        path: '/borrowing',
        icon: Library,
      },
    ],
  },
]

function App() {
  const [sidebarOpen, setSidebarOpen] = useState(false)

  return (
    <div className="app-shell">
      {sidebarOpen && (
        <button
          className="sidebar-overlay"
          type="button"
          aria-label="Close navigation"
          onClick={() => setSidebarOpen(false)}
        />
      )}

      <aside className={`sidebar ${sidebarOpen ? 'sidebar--open' : ''}`}>
        <div className="sidebar__header">
          <NavLink
            to="/"
            className="brand"
            onClick={() => setSidebarOpen(false)}
          >
            <span className="brand__icon">
              <BookOpen size={21} strokeWidth={2.2} />
            </span>

            <span className="brand__text">
              <strong>Libra</strong>
              <span>Library System</span>
            </span>
          </NavLink>

          <button
            className="icon-button sidebar__close"
            type="button"
            aria-label="Close navigation"
            onClick={() => setSidebarOpen(false)}
          >
            <X size={20} />
          </button>
        </div>

        <nav className="sidebar__nav">
          {navigation.map((section) => (
            <div className="nav-section" key={section.label}>
              <span className="nav-section__label">{section.label}</span>

              <div className="nav-section__items">
                {section.items.map((item) => {
                  const Icon = item.icon

                  return (
                    <NavLink
                      key={item.path}
                      to={item.path}
                      end={item.path === '/'}
                      className={({ isActive }) =>
                        `nav-item ${isActive ? 'nav-item--active' : ''}`
                      }
                      onClick={() => setSidebarOpen(false)}
                    >
                      <Icon size={19} strokeWidth={1.9} />
                      <span>{item.label}</span>
                    </NavLink>
                  )
                })}
              </div>
            </div>
          ))}
        </nav>

        <div className="sidebar__footer">
          <div className="sidebar__status">
            <span className="status-dot" />
            <div>
              <strong>System Online</strong>
              <span>All services operational</span>
            </div>
          </div>
        </div>
      </aside>

      <div className="main-shell">
        <header className="topbar">
          <div className="topbar__left">
            <button
              className="icon-button menu-button"
              type="button"
              aria-label="Open navigation"
              onClick={() => setSidebarOpen(true)}
            >
              <Menu size={21} />
            </button>

            <div className="breadcrumb">
              <span>Library</span>
              <ChevronDown size={14} />
              <strong>Management</strong>
            </div>
          </div>

          <div className="topbar__right">
            <div className="user-menu">
              <div className="user-avatar">
                <CircleUserRound size={19} />
              </div>

              <div className="user-info">
                <strong>Library Admin</strong>
                <span>Administrator</span>
              </div>

              <ChevronDown size={16} className="user-menu__chevron" />
            </div>
          </div>
        </header>

      <main className="page-content">
        <Routes>
          <Route path="/" element=      {<DashboardPage />} />
          <Route path="/books" element=     {<BooksPage />} />
          <Route path="/book-copies" element=     {<BookCopiesPage />} />
          <Route path="/members" element=     {<MembersPage />} />
          <Route path="/borrowing" element=     {<BorrowingPage />} />
        </Routes>
      </main>
      </div>
    </div>
  )
}

export default App