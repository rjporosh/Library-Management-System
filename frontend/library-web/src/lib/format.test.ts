import { describe, expect, it } from 'vitest'
import { daysUntil, formatNumber, relativeExpiry, statusTone, toBanglaDigits } from './format'

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

describe('number formatting and Bengali digits', () => {
  it('converts ASCII digits to Bengali digits', () => {
    expect(toBanglaDigits(0)).toBe('০')
    expect(toBanglaDigits(12345)).toBe('১২৩৪৫')
    expect(toBanglaDigits('35')).toBe('৩৫')
    expect(toBanglaDigits('1 / 35')).toBe('১ / ৩৫')
  })

  it('formatNumber formats numbers properly in English mode by default', () => {
    expect(formatNumber(42)).toBe('42')
    expect(formatNumber(0)).toBe('0')
    expect(formatNumber('')).toBe('')
  })
})
