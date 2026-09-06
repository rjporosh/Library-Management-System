import { clsx } from 'clsx'
import { X } from 'lucide-react'
import type {
  ButtonHTMLAttributes,
  InputHTMLAttributes,
  ReactNode,
  SelectHTMLAttributes,
} from 'react'
import { useEffect } from 'react'
import { statusTone, type BadgeTone } from '@/lib/format'
import { t, tStatus } from '@/lib/i18n'

// --- Badge --------------------------------------------------------------

const TONE_CLASS: Record<BadgeTone, string> = {
  green: 'bg-emerald-50 text-emerald-700 ring-emerald-600/20',
  amber: 'bg-amber-50 text-amber-700 ring-amber-600/20',
  red: 'bg-rose-50 text-rose-700 ring-rose-600/20',
  slate: 'bg-slate-100 text-slate-600 ring-slate-500/20',
  blue: 'bg-sky-50 text-sky-700 ring-sky-600/20',
  violet: 'bg-brand-50 text-brand-700 ring-brand-600/20',
}

export function Badge({
  children,
  tone = 'slate',
}: {
  children: ReactNode
  tone?: BadgeTone
}) {
  return (
    <span
      className={clsx(
        'inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-semibold ring-1 ring-inset',
        TONE_CLASS[tone],
      )}
    >
      <span
        className={clsx('h-1.5 w-1.5 rounded-full', {
          'bg-emerald-500': tone === 'green',
          'bg-amber-500': tone === 'amber',
          'bg-rose-500': tone === 'red',
          'bg-slate-400': tone === 'slate',
          'bg-sky-500': tone === 'blue',
          'bg-brand-500': tone === 'violet',
        })}
      />
      {children}
    </span>
  )
}

/** A status value rendered as a coloured pill using the shared tone map. */
export function StatusPill({ status }: { status: string }) {
  return <Badge tone={statusTone(status)}>{tStatus(status)}</Badge>
}

// --- Button -----------------------------------------------------------

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: 'primary' | 'secondary' | 'ghost' | 'danger'
  size?: 'sm' | 'md'
}

export function Button({
  variant = 'primary',
  size = 'md',
  className,
  ...props
}: ButtonProps) {
  return (
    <button
      {...props}
      className={clsx(
        'inline-flex items-center justify-center gap-2 rounded-lg font-semibold transition disabled:cursor-not-allowed disabled:opacity-50',
        size === 'sm' ? 'px-3 py-1.5 text-xs' : 'px-4 py-2 text-sm',
        {
          'bg-brand-600 text-white hover:bg-brand-700 shadow-sm': variant === 'primary',
          'bg-white text-slate-700 ring-1 ring-slate-300 hover:bg-slate-50':
            variant === 'secondary',
          'text-slate-600 hover:bg-slate-100': variant === 'ghost',
          'bg-rose-600 text-white hover:bg-rose-700 shadow-sm': variant === 'danger',
        },
        className,
      )}
    />
  )
}

// --- Card ------------------------------------------------------------

export function Card({
  children,
  className,
}: {
  children: ReactNode
  className?: string
}) {
  return (
    <div
      className={clsx(
        'rounded-2xl border border-slate-200 bg-white shadow-sm',
        className,
      )}
    >
      {children}
    </div>
  )
}

// --- PageHeader ----------------------------------------------------

export function PageHeader({
  title,
  subtitle,
  actions,
}: {
  title: string
  subtitle?: string
  actions?: ReactNode
}) {
  return (
    <div className="mb-6 flex flex-wrap items-end justify-between gap-3">
      <div>
        <h1 className="text-2xl font-bold tracking-tight text-slate-900">{title}</h1>
        {subtitle && <p className="mt-1 text-sm text-slate-500">{subtitle}</p>}
      </div>
      {actions && <div className="flex flex-wrap gap-2">{actions}</div>}
    </div>
  )
}

// --- Spinner / states -------------------------------------------

export function Spinner({ label }: { label?: string }) {
  return (
    <div className="flex items-center justify-center gap-3 py-12 text-sm text-slate-500">
      <span className="h-5 w-5 animate-spin rounded-full border-2 border-slate-300 border-t-brand-600" />
      {label ?? t('common.loading')}
    </div>
  )
}

export function EmptyState({
  title,
  hint,
  action,
}: {
  title: string
  hint?: string
  action?: ReactNode
}) {
  return (
    <div className="flex flex-col items-center gap-2 py-16 text-center">
      <p className="text-sm font-semibold text-slate-700">{title}</p>
      {hint && <p className="text-sm text-slate-500">{hint}</p>}
      {action && <div className="mt-2">{action}</div>}
    </div>
  )
}

export function ErrorState({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <div className="flex flex-col items-center gap-3 py-16 text-center">
      <p className="max-w-md text-sm font-medium text-rose-700">{message}</p>
      {onRetry && (
        <Button variant="secondary" size="sm" onClick={onRetry}>
          {t('common.tryAgain')}
        </Button>
      )}
    </div>
  )
}

// --- Form fields --------------------------------------------

export function FormField({
  label,
  error,
  hint,
  children,
}: {
  label: string
  error?: string
  hint?: string
  children: ReactNode
}) {
  return (
    <label className="block">
      <span className="mb-1 block text-sm font-medium text-slate-700">{label}</span>
      {children}
      {error ? (
        <span className="mt-1 block text-xs font-medium text-rose-600">{error}</span>
      ) : (
        hint && <span className="mt-1 block text-xs text-slate-400">{hint}</span>
      )}
    </label>
  )
}

export function TextInput({
  invalid,
  className,
  ...props
}: InputHTMLAttributes<HTMLInputElement> & { invalid?: boolean }) {
  return (
    <input
      {...props}
      className={clsx(
        'w-full rounded-lg border bg-white px-3 py-2 text-sm text-slate-900 shadow-sm outline-none transition focus:ring-2',
        invalid
          ? 'border-rose-400 focus:border-rose-500 focus:ring-rose-200'
          : 'border-slate-300 focus:border-brand-500 focus:ring-brand-200',
        className,
      )}
    />
  )
}

export function Select({
  className,
  children,
  ...props
}: SelectHTMLAttributes<HTMLSelectElement>) {
  return (
    <select
      {...props}
      className={clsx(
        'w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 shadow-sm outline-none transition focus:border-brand-500 focus:ring-2 focus:ring-brand-200',
        className,
      )}
    >
      {children}
    </select>
  )
}

// --- Modal --------------------------------------------------

export function Modal({
  open,
  onClose,
  title,
  children,
  wide,
}: {
  open: boolean
  onClose: () => void
  title: string
  children: ReactNode
  wide?: boolean
}) {
  useEffect(() => {
    if (!open) return
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [open, onClose])

  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 p-4 backdrop-blur-sm">
      <div
        className={clsx(
          'mt-12 w-full rounded-2xl bg-white shadow-xl',
          wide ? 'max-w-3xl' : 'max-w-lg',
        )}
      >
        <div className="flex items-center justify-between border-b border-slate-200 px-5 py-4">
          <h2 className="text-base font-semibold text-slate-900">{title}</h2>
          <button
            type="button"
            onClick={onClose}
            className="rounded-md p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-600"
          >
            <X size={18} />
          </button>
        </div>
        <div className="px-5 py-4">{children}</div>
      </div>
    </div>
  )
}
