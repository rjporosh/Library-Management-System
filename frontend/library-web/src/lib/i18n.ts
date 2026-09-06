import { http } from './api'
import { dictionaries, type MessageKey } from './locales'

export type Lang = 'en' | 'bn'

const STORAGE_KEY = 'lms.lang'

export function getLang(): Lang {
  try {
    const v = localStorage.getItem(STORAGE_KEY)
    return v === 'bn' ? 'bn' : 'en'
  } catch {
    return 'en'
  }
}

export function setLang(lang: Lang) {
  try {
    localStorage.setItem(STORAGE_KEY, lang)
  } catch {
    /* ignore */
  }
  // full reload so every query refetches with the new culture and every
  // component re-renders with the new dictionary
  window.location.reload()
}

/**
 * Translate a UI string key for the active language. Falls back to English,
 * then to the key itself. `vars` fills `{name}` placeholders.
 */
export function t(key: MessageKey, vars?: Record<string, string | number>): string {
  const lang = getLang()
  const template = dictionaries[lang][key] ?? dictionaries.en[key] ?? key
  if (!vars) return template
  return template.replace(/\{(\w+)\}/g, (_, name: string) =>
    name in vars ? String(vars[name]) : `{${name}}`,
  )
}

/** Localized label for a status enum value coming from the API (Active, Lost, …). */
export function tStatus(status: string): string {
  const key = `status.${status}` as MessageKey
  const lang = getLang()
  return dictionaries[lang][key] ?? dictionaries.en[key] ?? status
}

// --- API error-code catalogue (server-provided, per culture) ------------

let messageCache: Record<string, string> = {}

export async function loadMessages(): Promise<void> {
  try {
    const { data } = await http.get<{ culture: string; messages: Record<string, string> }>(
      '/metadata/messages',
    )
    messageCache = data.messages ?? {}
  } catch {
    messageCache = {}
  }
}

/** Localized text for an API error code, or the server-provided fallback. */
export function localizeError(code: string | undefined, fallback: string): string {
  return (code && messageCache[code]) || fallback
}
