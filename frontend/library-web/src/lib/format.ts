import { getLang, t } from './i18n'

export type BadgeTone = 'green' | 'amber' | 'red' | 'slate' | 'blue' | 'violet'

const BANGLA_DIGITS = ['০', '১', '২', '৩', '৪', '৫', '৬', '৭', '৮', '৯']

export function toBanglaDigits(val: number | string): string {
  return String(val).replace(/\d/g, (d) => BANGLA_DIGITS[Number(d)])
}

export function formatNumber(val: number | string): string {
  if (val === '' || val === null || val === undefined) return ''
  const str = String(val)
  if (getLang() === 'bn') {
    return toBanglaDigits(str)
  }
  return str
}

/** Intl locale for the active UI language. */
function locale(): string | undefined {
  return getLang() === 'bn' ? 'bn-BD' : undefined
}

const STATUS_TONE: Record<string, BadgeTone> = {
  // member
  Active: 'green',
  Suspended: 'amber',
  Inactive: 'slate',
  // copy
  Available: 'green',
  Borrowed: 'blue',
  Lost: 'red',
  Damaged: 'red',
  Maintenance: 'amber',
  // borrow
  Returned: 'slate',
}

export function statusTone(status: string): BadgeTone {
  return STATUS_TONE[status] ?? 'slate'
}

export function formatDate(value?: string | null): string {
  if (!value) return '—'
  const d = new Date(value)
  return d.toLocaleDateString(locale(), {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

export function formatDateTime(value?: string | null): string {
  if (!value) return '—'
  const d = new Date(value)
  return d.toLocaleString(locale(), {
    dateStyle: 'medium',
    timeStyle: 'short',
  })
}

export function daysUntil(value?: string | null): number | null {
  if (!value) return null
  const ms = new Date(value).getTime() - Date.now()
  return Math.ceil(ms / 86_400_000)
}

export function relativeExpiry(value?: string | null): string {
  const days = daysUntil(value)
  if (days == null) return '—'
  if (days < 0) return t('expiry.expiredAgo', { days: Math.abs(days) })
  if (days === 0) return t('expiry.today')
  return t('expiry.inDays', { days })
}
