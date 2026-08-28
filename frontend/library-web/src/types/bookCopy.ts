export type BookCopyStatus = 'Available' | 'Borrowed'

export interface BookCopy {
  id: string
  bookId: string
  barcode: string
  status: BookCopyStatus
}