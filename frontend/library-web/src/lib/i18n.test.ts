import { describe, expect, it } from 'vitest'
import { en } from './locales/en'
import { bn } from './locales/bn'
import { t, tStatus } from './i18n'

describe('i18n dictionaries', () => {
  it('bn covers every en key and adds none of its own', () => {
    const enKeys = Object.keys(en).sort()
    const bnKeys = Object.keys(bn).sort()
    expect(bnKeys).toEqual(enKeys)
  })

  it('no bn string is left as the English source', () => {
    // A handful of keys are intentionally identical (ISBN, dash, an em-dash…).
    const allowedSame = new Set(['books.col.isbn', 'common.dash', 'import.kb'])
    for (const [key, value] of Object.entries(en)) {
      if (allowedSame.has(key)) continue
      expect(bn[key as keyof typeof bn], `bn[${key}] should be translated`).not.toBe(value)
    }
  })

  it('t() fills placeholders and falls back to the key', () => {
    expect(t('common.results', { count: 3, page: 1, pages: 2 })).toContain('3')
    expect(t('nonexistent.key' as never)).toBe('nonexistent.key')
  })

  it('tStatus maps known enum values and passes through unknown ones', () => {
    expect(tStatus('Active')).toBe(en['status.Active'])
    expect(tStatus('Weird')).toBe('Weird')
  })
})
