-- ===========================================================================
-- Representative demo seed data (PostgreSQL) - fully runnable.
--
-- The application seeds an equivalent dataset automatically on startup in
-- Development (Database:SeedOnStartup = true) via
-- Library.Infrastructure/Persistence/Seed/SeedData.cs + DatabaseSeeder, with
-- fresh GUIDs each run. This script is the equivalent for manual / QA setup.
--
-- Load it (from the repository root), against an already-migrated database:
--
--   psql "$LMS_DESIGN_CONNECTION" -f docs/database/seed-data.sql
--   # or, with the docker-compose Postgres:
--   docker compose exec -T db psql -U library -d library < docs/database/seed-data.sql
--
-- To (re)create the schema first, see docs/database/schema.sql or run
-- `dotnet ef database update` (commands in /MIGRATIONS.md).
--
-- Covers: available / borrowed / lost copies; active / expiring-soon /
-- inactive members; one active borrow and one overdue borrow (the overdue one
-- makes the nightly membership-maintenance job suspend Erin Park).
-- Idempotent: existing rows (matched by natural key) are updated, not
-- duplicated.
-- ===========================================================================

BEGIN;

-- Books ---------------------------------------------------------------------
INSERT INTO books
    ("Id","ISBN","Title","Author","Category","Publisher","Description","PublishedYear","IsDeleted","CreatedAtUtc","UpdatedAtUtc")
VALUES
 (gen_random_uuid(),'9780132350884','Clean Code','Robert C. Martin','Software Engineering','Prentice Hall','A handbook of agile software craftsmanship.',2008,false,now(),now()),
 (gen_random_uuid(),'9780135957059','The Pragmatic Programmer','David Thomas & Andrew Hunt','Software Engineering','Addison-Wesley','Your journey to mastery.',2019,false,now(),now()),
 (gen_random_uuid(),'9780321125217','Domain-Driven Design','Eric Evans','Software Architecture','Addison-Wesley','Tackling complexity in the heart of software.',2003,false,now(),now()),
 (gen_random_uuid(),'9780134757599','Refactoring','Martin Fowler','Software Engineering','Addison-Wesley','Improving the design of existing code.',2018,false,now(),now())
ON CONFLICT ("ISBN") DO UPDATE SET
    "Title"=EXCLUDED."Title","Author"=EXCLUDED."Author","Category"=EXCLUDED."Category",
    "Publisher"=EXCLUDED."Publisher","Description"=EXCLUDED."Description",
    "PublishedYear"=EXCLUDED."PublishedYear","IsDeleted"=false,"UpdatedAtUtc"=now();

-- Book copies -------------------------------------------------------------
-- BC-0001/0002 -> Clean Code, Available   BC-0003 -> Pragmatic, Borrowed (overdue)
-- BC-0004      -> Pragmatic, Available    BC-0005 -> DDD, Lost
-- BC-0006      -> Refactoring, Borrowed   BC-0007 -> Refactoring, Available
INSERT INTO book_copies
    ("Id","BookId","Barcode","Status","IsDeleted","CreatedAtUtc","UpdatedAtUtc")
SELECT gen_random_uuid(), b."Id", v.barcode, v.status, false, now(), now()
FROM (VALUES
    ('BC-0001','9780132350884','Available'),
    ('BC-0002','9780132350884','Available'),
    ('BC-0003','9780135957059','Borrowed'),
    ('BC-0004','9780135957059','Available'),
    ('BC-0005','9780321125217','Lost'),
    ('BC-0006','9780134757599','Borrowed'),
    ('BC-0007','9780134757599','Available')
) AS v(barcode, isbn, status)
JOIN books b ON b."ISBN" = v.isbn
ON CONFLICT ("Barcode") DO UPDATE SET
    "Status"=EXCLUDED."Status","IsDeleted"=false,"UpdatedAtUtc"=now();

-- Members ---------------------------------------------------------------
INSERT INTO members
    ("Id","MembershipNumber","Name","Email","Phone","Address","Status","MembershipExpiresAt","SuspendedAt","LastRenewedAt","IsDeleted","CreatedAtUtc","UpdatedAtUtc")
VALUES
 (gen_random_uuid(),'MEM-100001','Alice Johnson','alice@example.com','+1-202-555-0101','12 Oak Street, Springfield','Active',   now() + interval '200 days', NULL, NULL, false, now(), now()),
 (gen_random_uuid(),'MEM-100002','Bob Smith',    'bob@example.com',  '+1-202-555-0102','48 Elm Avenue, Springfield','Active',   now() + interval '15 days',  NULL, NULL, false, now(), now()),
 (gen_random_uuid(),'MEM-100003','Charlie Brown','charlie@example.com','+1-202-555-0103','7 Pine Road, Shelbyville','Active',   now() + interval '90 days',  NULL, NULL, false, now(), now()),
 (gen_random_uuid(),'MEM-100004','Dana Lee',     'dana@example.com', '+1-202-555-0104','90 Maple Lane, Ogdenville','Inactive', now() - interval '10 days',  NULL, NULL, false, now(), now()),
 (gen_random_uuid(),'MEM-100005','Erin Park',    'erin@example.com', '+1-202-555-0105','3 Birch Court, Springfield','Active',   now() + interval '120 days', NULL, NULL, false, now(), now())
ON CONFLICT ("MembershipNumber") DO UPDATE SET
    "Name"=EXCLUDED."Name","Email"=EXCLUDED."Email","Phone"=EXCLUDED."Phone",
    "Address"=EXCLUDED."Address","Status"=EXCLUDED."Status",
    "MembershipExpiresAt"=EXCLUDED."MembershipExpiresAt","IsDeleted"=false,"UpdatedAtUtc"=now();

-- Borrow records ----------------------------------------------------
--  Alice -> BC-0006, borrowed 3 days ago, due in 11 days (Active)
--  Erin  -> BC-0003, borrowed 30 days ago, due 9 days ago (Active, overdue)
INSERT INTO borrow_records
    ("Id","BookCopyId","MemberId","BorrowedAt","DueAt","ReturnedAt","Status","IsDeleted","CreatedAtUtc","UpdatedAtUtc")
SELECT gen_random_uuid(), c."Id", m."Id", v.borrowed, v.due, NULL, 'Active', false, now(), now()
FROM (VALUES
    ('BC-0006','MEM-100001', now() - interval '3 days',  now() + interval '11 days'),
    ('BC-0003','MEM-100005', now() - interval '30 days', now() - interval '9 days')
) AS v(barcode, member, borrowed, due)
JOIN book_copies c ON c."Barcode" = v.barcode
JOIN members m ON m."MembershipNumber" = v.member
WHERE NOT EXISTS (
    SELECT 1 FROM borrow_records br
    WHERE br."BookCopyId" = c."Id" AND br."Status" = 'Active'
);

COMMIT;
