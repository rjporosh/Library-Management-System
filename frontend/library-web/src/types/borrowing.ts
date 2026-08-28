export interface BorrowRecord {
  id: string
  memberId: string
  bookCopyId: string
  borrowedAt: string
  dueAt: string
  returnedAt?: string | null
  status: string
}

export interface IssueBookRequest {
  memberId: string
  bookCopyId: string
  dueAt: string
}