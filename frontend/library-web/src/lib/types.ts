// ---------------------------------------------------------------------------
// API contract types. Mirror the backend DTOs (Library.Application ... .Models).
// ---------------------------------------------------------------------------

export type MemberStatus = 'Active' | 'Suspended' | 'Inactive'
export type BookCopyStatus =
  | 'Available'
  | 'Borrowed'
  | 'Lost'
  | 'Damaged'
  | 'Maintenance'
export type BorrowStatus = 'Active' | 'Returned'

export interface Book {
  id: string
  isbn: string
  title: string
  author: string
  category: string
  publisher: string
  description?: string | null
  publishedYear: number
  coverImageUrl?: string | null
  edition?: string | null
  hasEbook: boolean
  ebookUrl?: string | null
  hasAudiobook: boolean
  audiobookUrl?: string | null
  externalBuyUrl?: string | null
  externalPdfUrl?: string | null
}

export type BookAvailabilityStatus = 'PhysicalAvailable' | 'Ebook' | 'Audiobook' | 'Unavailable'

export interface BookAvailability {
  status: BookAvailabilityStatus
  totalCopies: number
  availableCopies: number
  accessUrl?: string | null
  externalBuyUrl?: string | null
  externalPdfUrl?: string | null
}

export interface BookDetail {
  book: Book
  availability: BookAvailability
}

export interface BookCopy {
  id: string
  bookId: string
  barcode: string
  status: BookCopyStatus
}

export interface Member {
  id: string
  membershipNumber: string
  name: string
  email: string
  phone: string
  address: string
  status: MemberStatus
  membershipExpiresAt: string
  suspendedAt?: string | null
  lastRenewedAt?: string | null
  currentlyBorrowed: number
}

export interface MemberBorrowSummary {
  borrowRecordId: string
  bookCopyId: string
  borrowedAt: string
  dueAt: string
  returnedAt?: string | null
  status: BorrowStatus
  isOverdue: boolean
}

export interface MemberDetail {
  member: Member
  totalBorrowed: number
  currentlyBorrowed: number
  overdue: number
  lastBorrowedAt?: string | null
  history: MemberBorrowSummary[]
}

export interface BorrowRecord {
  id: string
  memberId: string
  bookCopyId: string
  borrowedAt: string
  dueAt: string
  returnedAt?: string | null
  status: BorrowStatus
  memberName: string
  membershipNumber: string
  bookTitle: string
  barcode: string
}

export interface Paged<T> {
  items: T[]
  pageNumber: number
  pageSize: number
  totalItems: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

export interface DashboardSnapshot {
  totalBooks: number
  totalCopies: number
  availableCopies: number
  borrowedCopies: number
  outOfServiceCopies: number
  totalMembers: number
  activeMembers: number
  suspendedMembers: number
  inactiveMembers: number
  membersExpiringSoon: number
  activeBorrows: number
  overdueBorrows: number
  recentActivity: {
    borrowRecordId: string
    memberId: string
    bookCopyId: string
    borrowedAt: string
    dueAt: string
    status: BorrowStatus
  }[]
}

export interface MaintenanceResult {
  overdueSuspended: number
  expiredDeactivated: number
  ranAtUtc: string
}

// --- error contract --------------------------------------------------------

export interface ApiError {
  errorCode: string
  errorMessage: string
  field?: string | null
  line?: number | null
  required?: boolean | null
  supportedValues?: string | null
}

export interface ApiErrorResponse {
  success: false
  errors: ApiError[]
  correlationId?: string | null
}

export interface BulkImportError extends ApiErrorResponse {
  imported: number
  truncated: boolean
}

/** Normalised error the UI layer works with. */
export interface NormalisedError {
  message: string
  errors: ApiError[]
  fieldErrors: Record<string, string>
  rowErrors: ApiError[]
  traceId?: string | null
  status?: number
}

// --- advanced search -----------------------------------------------------

export type FilterOperator =
  | 'eq'
  | 'neq'
  | 'contains'
  | 'notContains'
  | 'startsWith'
  | 'endsWith'
  | 'gt'
  | 'gte'
  | 'lt'
  | 'lte'
  | 'in'
  | 'notIn'
  | 'between'

export interface SearchFilter {
  field: string
  operator: FilterOperator
  value?: string
  values?: string[]
}

export interface SortSpec {
  field: string
  direction: 'asc' | 'desc'
}

export interface SearchRequest {
  filters: SearchFilter[]
  match: 'all' | 'any'
  sort: SortSpec[]
  page: number
  pageSize: number
  search?: string
}
