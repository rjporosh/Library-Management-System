import { http } from '@/lib/api'
import type {
  Book,
  BookCopy,
  BorrowRecord,
  DashboardSnapshot,
  MaintenanceResult,
  Member,
  MemberDetail,
  Paged,
  SearchRequest,
} from '@/lib/types'

// --- books ---------------------------------------------------------------

export const booksApi = {
  search: (req: SearchRequest) =>
    http.post<Paged<Book>>('/books/search', req).then((r) => r.data),
  get: (id: string) => http.get<Book>(`/books/${id}`).then((r) => r.data),
  create: (body: Omit<Book, 'id'>) =>
    http.post<Book>('/books', body).then((r) => r.data),
  update: (id: string, body: Omit<Book, 'id'>) =>
    http.put<Book>(`/books/${id}`, body).then((r) => r.data),
  remove: (id: string, force = false) =>
    http.delete(`/books/${id}`, { params: { force } }).then((r) => r.data),
  importTemplateUrl: `${http.defaults.baseURL}/books/import/template`,
  import: (file: File) => uploadFile('/books/import', file),
}

// --- book copies -------------------------------------------------------

export const copiesApi = {
  search: (req: SearchRequest) =>
    http.post<Paged<BookCopy>>('/book-copies/search', req).then((r) => r.data),
  byBook: (bookId: string) =>
    http.get<BookCopy[]>(`/book-copies/book/${bookId}`).then((r) => r.data),
  get: (id: string) =>
    http.get<BookCopy>(`/book-copies/${id}`).then((r) => r.data),
  create: (body: { bookId: string; barcode: string }) =>
    http.post<BookCopy>('/book-copies', body).then((r) => r.data),
  update: (id: string, body: { barcode: string }) =>
    http.put<BookCopy>(`/book-copies/${id}`, body).then((r) => r.data),
  changeStatus: (id: string, status: string) =>
    http
      .post<BookCopy>(`/book-copies/${id}/status`, { status })
      .then((r) => r.data),
  remove: (id: string, force = false) =>
    http.delete(`/book-copies/${id}`, { params: { force } }).then((r) => r.data),
  importTemplateUrl: `${http.defaults.baseURL}/book-copies/import/template`,
  import: (file: File) => uploadFile('/book-copies/import', file),
}

// --- members ----------------------------------------------------------

export interface MemberInput {
  membershipNumber: string
  name: string
  email: string
  phone: string
  address: string
}

export const membersApi = {
  search: (req: SearchRequest) =>
    http.post<Paged<Member>>('/members/search', req).then((r) => r.data),
  get: (id: string) => http.get<Member>(`/members/${id}`).then((r) => r.data),
  detail: (id: string) =>
    http.get<MemberDetail>(`/members/${id}/detail`).then((r) => r.data),
  create: (body: MemberInput) =>
    http.post<Member>('/members', body).then((r) => r.data),
  update: (id: string, body: MemberInput) =>
    http.put<Member>(`/members/${id}`, body).then((r) => r.data),
  remove: (id: string, force = false) =>
    http.delete(`/members/${id}`, { params: { force } }).then((r) => r.data),
  lifecycle: (id: string, action: 'suspend' | 'reactivate' | 'renew' | 'deactivate') =>
    http.post<Member>(`/members/${id}/${action}`).then((r) => r.data),
  importTemplateUrl: `${http.defaults.baseURL}/members/import/template`,
  import: (file: File) => uploadFile('/members/import', file),
}

// --- borrowing / dashboard / jobs ------------------------------------

export const borrowingApi = {
  issue: (body: { memberId: string; bookCopyId: string; dueAt: string }) =>
    http.post<BorrowRecord>('/borrowing/issue', body).then((r) => r.data),
  returnBook: (borrowRecordId: string) =>
    http
      .post<BorrowRecord>(`/borrowing/${borrowRecordId}/return`, {})
      .then((r) => r.data),
  search: (req: SearchRequest) =>
    http.post<Paged<BorrowRecord>>('/borrowing/search', req).then((r) => r.data),
}

export const dashboardApi = {
  get: () => http.get<DashboardSnapshot>('/dashboard').then((r) => r.data),
}

export const jobsApi = {
  runMemberMaintenance: () =>
    http
      .post<MaintenanceResult>('/jobs/member-maintenance/run')
      .then((r) => r.data),
}

export const metadataApi = {
  enums: () =>
    http
      .get<Record<string, string[]>>('/metadata/enums')
      .then((r) => r.data),
}

// --- auth -----------------------------------------------------------

export interface LoginResponse {
  accessToken: string
  expiresAtUtc: string
  userId: string
  username: string
  role: 'Librarian' | 'Member'
  memberId: string | null
}

export const authApi = {
  login: (usernameOrEmail: string, password: string) =>
    http.post<LoginResponse>('/auth/login', { usernameOrEmail, password }).then((r) => r.data),
  register: (body: { name: string; email: string; password: string; phone?: string; address?: string }) =>
    http.post<LoginResponse>('/auth/register', body).then((r) => r.data),
}

// --- helpers --------------------------------------------------------

async function uploadFile(path: string, file: File) {
  const form = new FormData()
  form.append('file', file)
  const { data } = await http.post(path, form, {
    headers: { 'Content-Type': 'multipart/form-data' },
  })
  return data as { success: boolean; imported: number }
}
