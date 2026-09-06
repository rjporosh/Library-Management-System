import { http } from './api'

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
  // full reload so every query refetches with the new culture
  window.location.reload()
}

/** errorCode -> localized message, for the current language. Loaded once. */
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
