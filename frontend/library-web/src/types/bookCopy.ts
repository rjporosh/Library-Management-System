export const BookCopyStatus = {
  Available: 0,
  Borrowed: 1,
} as const

export type BookCopyStatus =
  (typeof BookCopyStatus)[keyof typeof BookCopyStatus]

export interface BookCopy {
  id: string
  bookId: string
  barcode: string
  status: BookCopyStatus
}