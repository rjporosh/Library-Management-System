import type { ReactNode } from 'react'
import { Library } from 'lucide-react'
import { SpineWall } from '@/components/SpineWall'
import { t } from '@/lib/i18n'

/** Shared two-panel frame for sign-in and sign-up: spine wall on the left, form on the right. */
export function AuthFrame({ title, subtitle, children }: Readonly<{ title: string; subtitle: string; children: ReactNode }>) {
  return (
    <div className="grid min-h-screen lg:grid-cols-[1.05fr_1fr]">
      <aside className="relative hidden flex-col justify-between overflow-hidden bg-brand-800 p-12 text-white lg:flex">
        <div className="flex items-center gap-3">
          <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/10 ring-1 ring-white/20">
            <Library size={20} />
          </span>
          <span className="font-serif text-2xl font-semibold">{t('app.name')}</span>
        </div>
        <div>
          <h2 className="max-w-sm font-serif text-4xl leading-tight font-semibold text-balance">
            {t('auth.appTitle')}
          </h2>
          <p className="mt-3 max-w-sm text-brand-100/80">{t('app.tagline')}</p>
        </div>
        <SpineWall className="h-56 border-b-4 border-brand-900 pb-0" />
      </aside>
      <main className="flex items-center justify-center px-5 py-12">
        <div className="w-full max-w-sm">
          <div className="mb-8 flex items-center gap-2.5 lg:hidden">
            <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-brand-600 text-white">
              <Library size={18} />
            </span>
            <span className="font-serif text-xl font-semibold">{t('app.name')}</span>
          </div>
          <h1 className="font-serif text-3xl font-semibold text-slate-900">{title}</h1>
          <p className="mt-2 mb-7 text-sm text-slate-500">{subtitle}</p>
          {children}
        </div>
      </main>
    </div>
  )
}
