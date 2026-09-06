import {
  BookMarked,
  BookOpen,
  LayoutDashboard,
  Library,
  Repeat,
  Users,
} from 'lucide-react'
import { NavLink, Route, Routes } from 'react-router-dom'
import { getLang, setLang, t } from '@/lib/i18n'
import type { MessageKey } from '@/lib/locales'
import DashboardPage from '@/pages/DashboardPage'
import BooksPage from '@/pages/BooksPage'
import BookCopiesPage from '@/pages/BookCopiesPage'
import MembersPage from '@/pages/MembersPage'
import MemberDetailPage from '@/pages/MemberDetailPage'
import BorrowingPage from '@/pages/BorrowingPage'

const NAV: { to: string; labelKey: MessageKey; icon: typeof BookOpen; end?: boolean }[] = [
  { to: '/', labelKey: 'nav.dashboard', icon: LayoutDashboard, end: true },
  { to: '/books', labelKey: 'nav.books', icon: BookOpen },
  { to: '/book-copies', labelKey: 'nav.copies', icon: BookMarked },
  { to: '/members', labelKey: 'nav.members', icon: Users },
  { to: '/borrowing', labelKey: 'nav.borrowing', icon: Repeat },
]

export default function App() {
  return (
    <div className="flex min-h-screen bg-slate-100">
      <aside className="hidden w-60 shrink-0 flex-col border-r border-slate-200 bg-white lg:flex">
        <div className="flex items-center gap-2.5 px-5 py-5">
          <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-brand-600 text-white">
            <Library size={20} />
          </span>
          <div className="leading-tight">
            <p className="text-sm font-bold text-slate-900">{t('app.name')}</p>
            <p className="text-xs text-slate-400">{t('app.tagline')}</p>
          </div>
        </div>
        <nav className="flex-1 space-y-1 px-3 py-2">
          {NAV.map(({ to, labelKey, icon: Icon, end }) => (
            <NavLink
              key={to}
              to={to}
              end={end}
              className={({ isActive }) =>
                `flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition ${
                  isActive
                    ? 'bg-brand-50 text-brand-700'
                    : 'text-slate-600 hover:bg-slate-100'
                }`
              }
            >
              <Icon size={18} />
              {t(labelKey)}
            </NavLink>
          ))}
        </nav>
        <div className="space-y-2 px-5 py-4">
          <div className="flex items-center gap-1 rounded-lg bg-slate-100 p-0.5 text-xs font-medium">
            {(['en', 'bn'] as const).map((l) => (
              <button
                key={l}
                type="button"
                onClick={() => l !== getLang() && setLang(l)}
                className={`flex-1 rounded-md px-2 py-1 ${
                  getLang() === l ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-500'
                }`}
              >
                {l === 'en' ? 'English' : 'বাংলা'}
              </button>
            ))}
          </div>
          <div className="flex items-center gap-2 text-xs text-slate-400">
            <span className="h-2 w-2 rounded-full bg-emerald-500" />
            {t('app.online')}
          </div>
        </div>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex items-center gap-3 border-b border-slate-200 bg-white px-5 py-3 lg:hidden">
          <Library size={20} className="text-brand-600" />
          <span className="font-bold text-slate-900">{t('app.name')}</span>
        </header>

        <nav className="flex gap-1 overflow-x-auto border-b border-slate-200 bg-white px-3 py-2 lg:hidden">
          {NAV.map(({ to, labelKey, icon: Icon, end }) => (
            <NavLink
              key={to}
              to={to}
              end={end}
              className={({ isActive }) =>
                `flex shrink-0 items-center gap-1.5 rounded-lg px-3 py-1.5 text-xs font-medium ${
                  isActive ? 'bg-brand-50 text-brand-700' : 'text-slate-500'
                }`
              }
            >
              <Icon size={15} />
              {t(labelKey)}
            </NavLink>
          ))}
        </nav>

        <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-6 sm:px-6">
          <Routes>
            <Route path="/" element={<DashboardPage />} />
            <Route path="/books" element={<BooksPage />} />
            <Route path="/book-copies" element={<BookCopiesPage />} />
            <Route path="/members" element={<MembersPage />} />
            <Route path="/members/:id" element={<MemberDetailPage />} />
            <Route path="/borrowing" element={<BorrowingPage />} />
          </Routes>
        </main>
      </div>
    </div>
  )
}
