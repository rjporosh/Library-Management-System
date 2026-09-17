import type { Book } from './types'

/** A neutral inline placeholder - shown when no cover is set and the Open Library fallback fails to load. */
export const PLACEHOLDER_COVER =
  'data:image/svg+xml;utf8,' +
  encodeURIComponent(
    `<svg xmlns="http://www.w3.org/2000/svg" width="200" height="300" viewBox="0 0 200 300">
      <rect width="200" height="300" fill="#e2e8f0"/>
      <path d="M60 90h80v120H60z" fill="#cbd5e1"/>
      <path d="M70 110h60M70 130h60M70 150h40" stroke="#94a3b8" stroke-width="4" stroke-linecap="round"/>
    </svg>`,
  )

/**
 * Resolves the cover image to display: an explicit `coverImageUrl` when set,
 * else a derived Open Library cover-by-ISBN URL (a free public API - a real
 * cover shows up for most published ISBNs, and callers should fall back to
 * `PLACEHOLDER_COVER` via an `onError` handler for the rest).
 */
export function coverImageSrc(book: Pick<Book, 'coverImageUrl' | 'isbn'>): string {
  if (book.coverImageUrl) return book.coverImageUrl
  const isbn = book.isbn.replace(/[^0-9Xx]/g, '')
  return isbn ? `https://covers.openlibrary.org/b/isbn/${isbn}-M.jpg` : PLACEHOLDER_COVER
}
