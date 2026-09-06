import { describe, expect, it } from 'vitest'
import { daysUntil, relativeExpiry, statusTone } from './format'

describe('statusTone', () => {
  it('maps known statuses to tones', () => {
    expect(statusTone('Active')).toBe('green')
    expect(statusTone('Suspended')).toBe('amber')
    expect(statusTone('Inactive')).toBe('slate')
    expect(statusTone('Lost')).toBe('red')
    expect(statusTone('Borrowed')).toBe('blue')
  })

  it('falls back to slate for unknown', () => {
    expect(statusTone('Whatever')).toBe('slate')
  })
})

describe('relativeExpiry', () => {
  it('describes past and future dates', () => {
    const past = new Date(Date.now() - 3 * 86_400_000).toISOString()
    const future = new Date(Date.now() + 10 * 86_400_000).toISOString()

    expect(relativeExpiry(past)).toMatch(/expired \d+d ago/)
    expect(relativeExpiry(future)).toMatch(/in \d+d/)
    expect(relativeExpiry(null)).toBe('—')
  })

  it('daysUntil is null for missing input', () => {
    expect(daysUntil(undefined)).toBeNull()
  })
})
