export type BadgeTone = 'green' | 'amber' | 'red' | 'slate' | 'blue' | 'violet'

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
  return d.toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

export function formatDateTime(value?: string | null): string {
  if (!value) return '—'
  const d = new Date(value)
  return d.toLocaleString(undefined, {
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
  if (days < 0) return `expired ${Math.abs(days)}d ago`
  if (days === 0) return 'expires today'
  return `in ${days}d`
}
