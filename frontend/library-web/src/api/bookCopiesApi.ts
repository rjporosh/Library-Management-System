import { httpClient } from './httpClient'
import type { BookCopy } from '../types/bookCopy'

export const bookCopiesApi = {
  async getByBookId(bookId: string): Promise<BookCopy[]> {
    const response = await httpClient.get<BookCopy[]>(
      `/book-copies/book/${bookId}`,
    )

    return response.data
  },

  async getById(id: string): Promise<BookCopy> {
    const response = await httpClient.get<BookCopy>(
      `/book-copies/${id}`,
    )

    return response.data
  },
}