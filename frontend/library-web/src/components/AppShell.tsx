'use client'

import {
  BookMarked,
  BookOpen,
  ClipboardList,
  LayoutDashboard,
  Library,
  LogOut,
  Repeat,
  Users,
} from 'lucide-react'
import Link from 'next/link'
import { usePathname, useRouter } from 'next/navigation'
import { useEffect, type ReactNode } from 'react'
import { ChatWidget } from '@/components/ChatWidget'
import { useAuth } from '@/context/authContextValue'
import { getLang, setLang, t } from '@/lib/i18n'
import type { MessageKey } from '@/lib/locales'
import { startSignalR, stopSignalR } from '@/lib/signalr'

interface NavItem {
  to: string
  labelKey: MessageKey
  icon: typeof BookOpen
  librarianOnly?: boolean
}

const NAV: NavItem[] = [
  { to: '/', labelKey: 'nav.dashboard', icon: LayoutDashboard, librarianOnly: true },
  { to: '/books', labelKey: 'nav.books', icon: BookOpen },
  { to: '/book-copies', labelKey: 'nav.copies', icon: BookMarked, librarianOnly: true },
  { to: '/members', labelKey: 'nav.members', icon: Users, librarianOnly: true },
  { to: '/borrowing', labelKey: 'nav.borrowing', icon: Repeat, librarianOnly: true },
  { to: '/requests', labelKey: 'nav.requests', icon: ClipboardList },
]

const isActive = (pathname: string, to: string) =>
  to === '/' ? pathname === '/' : pathname === to || pathname.startsWith(`${to}/`)

/** Authenticated frame: guards the session, wires SignalR, renders navigation. */
export function AppShell({ children }: Readonly<{ children: ReactNode }>) {
  const { auth, isLibrarian, logout } = useAuth()
  const pathname = usePathname()
  const router = useRouter()

  const nav = NAV.filter((item) => !item.librarianOnly || isLibrarian)
  const allowed = isLibrarian || nav.some((n) => isActive(pathname, n.to))

  useEffect(() => {
    if (!auth) {
      const target = pathname && pathname !== '/' ? `?from=${encodeURIComponent(pathname)}` : ''
      router.replace(`/login${target}`)
    } else if (!allowed) {
      router.replace('/books')
    }
  }, [auth, allowed, pathname, router])

  useEffect(() => {
    if (auth?.accessToken) void startSignalR()
    else void stopSignalR()
  }, [auth])

  useEffect(() => {
    document.title =
      getLang() === 'bn' ? 'লিব্রা - গ্রন্থাগার ব্যবস্থাপনা পদ্ধতি' : 'Libra - Library Management System'
  }, [pathname])

  if (!auth || !allowed) return null

  const lang = getLang()

  return (
    <div className="flex min-h-screen">
      <aside className="sticky top-0 hidden h-screen w-64 shrink-0 flex-col bg-brand-800 text-brand-100 lg:flex">
        <div className="flex items-center gap-3 px-6 py-6">
          <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/10 text-white ring-1 ring-white/20">
            <Library size={20} />
          </span>
          <div className="leading-tight">
            <p className="font-serif text-xl font-semibold text-white">{t('app.name')}</p>
            <p className="text-xs text-brand-200/80">{t('app.tagline')}</p>
          </div>
        </div>

        <nav aria-label="Primary" className="flex-1 space-y-0.5 px-3 py-2">
          {nav.map(({ to, labelKey, icon: Icon }) => {
            const active = isActive(pathname, to)
            return (
              <Link
                key={to}
                href={to}
                aria-current={active ? 'page' : undefined}
                className={`group relative flex items-center gap-3 rounded-lg py-2.5 pr-3 pl-4 text-sm font-medium transition-colors ${
                  active ? 'bg-white/10 text-white' : 'text-brand-100/80 hover:bg-white/5 hover:text-white'
                }`}
              >
                <span
                  aria-hidden
                  className={`absolute top-2 bottom-2 left-0 w-1 rounded-r-sm bg-amber-500 transition-opacity ${
                    active ? 'opacity-100' : 'opacity-0'
                  }`}
                />
                <Icon size={18} />
                {t(labelKey)}
              </Link>
            )
          })}
        </nav>

        <div className="space-y-3 px-4 py-5">
          <div className="flex gap-1 rounded-lg bg-black/20 p-0.5 text-xs font-medium">
            {(['en', 'bn'] as const).map((l) => (
              <button
                key={l}
                type="button"
                onClick={() => l !== lang && setLang(l)}
                aria-pressed={lang === l}
                className={`flex-1 rounded-md px-2 py-1.5 transition-colors ${
                  lang === l ? 'bg-white text-brand-800 shadow-sm' : 'text-brand-100/70 hover:text-white'
                }`}
              >
                {l === 'en' ? 'English' : 'বাংলা'}
              </button>
            ))}
          </div>
          <div className="flex items-center justify-between gap-2 rounded-lg bg-white/5 px-3 py-2.5 text-xs">
            <span className="truncate font-medium text-white/90" title={auth.username}>
              {t('auth.loggedInAs', { name: auth.username })}
            </span>
            <button
              type="button"
              onClick={logout}
              className="shrink-0 text-brand-200/70 transition-colors hover:text-white"
              title={t('nav.logout')}
              aria-label={t('nav.logout')}
            >
              <LogOut size={15} />
            </button>
          </div>
          <p className="flex items-center gap-2 text-xs text-brand-200/70">
            <span className="h-2 w-2 rounded-full bg-emerald-400" />
            {t('app.online')}
          </p>
        </div>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex items-center gap-3 bg-brand-800 px-5 py-3 text-white lg:hidden">
          <Library size={20} />
          <span className="font-serif text-lg font-semibold">{t('app.name')}</span>
          <button type="button" onClick={logout} aria-label={t('nav.logout')} className="ml-auto text-brand-100/80 hover:text-white">
            <LogOut size={18} />
          </button>
        </header>

        <nav aria-label="Primary" className="flex gap-1 overflow-x-auto border-b border-slate-200 bg-white px-3 py-2 lg:hidden">
          {nav.map(({ to, labelKey, icon: Icon }) => (
            <Link
              key={to}
              href={to}
              aria-current={isActive(pathname, to) ? 'page' : undefined}
              className={`flex shrink-0 items-center gap-1.5 rounded-lg px-3 py-1.5 text-xs font-medium ${
                isActive(pathname, to) ? 'bg-brand-50 text-brand-700' : 'text-slate-500'
              }`}
            >
              <Icon size={15} />
              {t(labelKey)}
            </Link>
          ))}
        </nav>

        <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-8 sm:px-8">{children}</main>
      </div>

      {isLibrarian && <ChatWidget />}
    </div>
  )
}
