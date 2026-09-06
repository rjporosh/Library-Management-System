# Entity–Relationship Diagram

All tables carry `Id` (uuid, PK), `IsDeleted` (bool) + `DeletedAtUtc`, and
`CreatedAtUtc` / `UpdatedAtUtc` (shadow audit). Enums are stored as their string
name. FKs are `ON DELETE RESTRICT` (the app soft-deletes and cascades in a
transaction).

```mermaid
erDiagram
  books ||--o{ book_copies : "has"
  books {
    uuid Id PK
    varchar ISBN UK
    varchar Title
    varchar Author
    varchar Category
    varchar Publisher
    varchar Description
    int PublishedYear
    bool IsDeleted
  }
  book_copies ||--o{ borrow_records : "is borrowed in"
  book_copies {
    uuid Id PK
    uuid BookId FK
    varchar Barcode UK
    varchar Status "Available|Borrowed|Lost|Damaged|Maintenance"
    bool IsDeleted
  }
  members ||--o{ borrow_records : "borrows"
  members {
    uuid Id PK
    varchar MembershipNumber UK
    varchar Name
    varchar Email UK
    varchar Phone
    varchar Address
    varchar Status "Active|Suspended|Inactive"
    timestamptz MembershipExpiresAt
    timestamptz SuspendedAt
    timestamptz LastRenewedAt
    bool IsDeleted
  }
  borrow_records {
    uuid Id PK
    uuid BookCopyId FK
    uuid MemberId FK
    timestamptz BorrowedAt
    timestamptz DueAt
    timestamptz ReturnedAt
    varchar Status "Active|Returned"
    bool IsDeleted
  }
```

## Indexes

| Table | Index | Purpose |
|---|---|---|
| books | `ISBN` unique, `Title`, `Category`, `IsDeleted` | lookup, search, filter |
| book_copies | `Barcode` unique, `BookId`, `Status`, `IsDeleted` | dependents, availability |
| members | `MembershipNumber` unique, `Email` unique, `Status`, `MembershipExpiresAt`, `IsDeleted` | lookup, expiry sweep |
| borrow_records | `(MemberId, Status)`, `(Status, DueAt)`, `IsDeleted` | one-active-borrow, overdue scan |
