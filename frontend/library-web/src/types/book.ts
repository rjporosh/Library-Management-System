export interface Book {
  id: string
  isbn: string
  title: string
  author: string
  description?: string | null
  publishedYear: number
}

export interface PagedBookResponse {
  items: Book[]
  pageNumber: number
  pageSize: number
  totalItems: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}