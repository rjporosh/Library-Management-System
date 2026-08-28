import { httpClient } from './httpClient'

export interface Book {
  id: string
  isbn: string
  title: string
  author: string
  description?: string | null
  publishedYear: number
}

export interface PagedResult<T> {
  items: T[]
  pageNumber: number
  pageSize: number
  totalItems: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

export interface GetBooksParams {
  pageNumber?: number
  pageSize?: number
  search?: string
  searchBy?: string
  sortBy?: string
  sortDirection?: string
}

export const booksApi = {
  async getAll(
    params?: GetBooksParams,
  ): Promise<PagedResult<Book>> {
    const response = await httpClient.get<PagedResult<Book>>(
      '/books',
      {
        params,
      },
    )

    return response.data
  },

  async getById(id: string): Promise<Book> {
    const response = await httpClient.get<Book>(
      `/books/${id}`,
    )

    return response.data
  },
}