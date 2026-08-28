import { httpClient } from './httpClient'
import type {
  BorrowRecord,
  IssueBookRequest,
} from '../types/borrowing'

export const borrowingApi = {
  async issue(
    request: IssueBookRequest,
  ): Promise<BorrowRecord> {
    const response =
      await httpClient.post<BorrowRecord>(
        '/borrowing/issue',
        request,
      )

    return response.data
  },

  async returnBook(
    borrowRecordId: string,
  ): Promise<BorrowRecord> {
    const response =
      await httpClient.post<BorrowRecord>(
        `/borrowing/${borrowRecordId}/return`,
        {},
      )

    return response.data
  },
}