-- Representative demo seed data (PostgreSQL).
--
-- The application seeds this automatically on startup in Development
-- (Database:SeedOnStartup = true) via Library.Infrastructure ... SeedData /
-- DatabaseSeeder, with fresh GUIDs each run. This script is the equivalent
-- for manual/QA setup - replace the :ids with your own uuids or use
-- gen_random_uuid().
--
-- Covers: available / borrowed / lost copies; active / expiring-soon /
-- inactive members; one active borrow and one overdue borrow.

BEGIN;

-- Books ---------------------------------------------------------------
INSERT INTO books ("Id","ISBN","Title","Author","Category","Publisher","Description","PublishedYear","IsDeleted","CreatedAtUtc","UpdatedAtUtc") VALUES
 (gen_random_uuid(),'9780132350884','Clean Code','Robert C. Martin','Software Engineering','Prentice Hall','A handbook of agile software craftsmanship.',2008,false,now(),now()),
 (gen_random_uuid(),'9780135957059','The Pragmatic Programmer','David Thomas & Andrew Hunt','Software Engineering','Addison-Wesley','Your journey to mastery.',2019,false,now(),now()),
 (gen_random_uuid(),'9780321125217','Domain-Driven Design','Eric Evans','Software Architecture','Addison-Wesley','Tackling complexity in the heart of software.',2003,false,now(),now()),
 (gen_random_uuid(),'9780134757599','Refactoring','Martin Fowler','Software Engineering','Addison-Wesley','Improving the design of existing code.',2018,false,now(),now());

-- Book copies (BC-0001..BC-0007) ------------------------------------
--  BC-0001 / BC-0002  -> Clean Code, Available
--  BC-0003            -> Pragmatic, Borrowed (overdue)
--  BC-0004            -> Pragmatic, Available
--  BC-0005            -> DDD, Lost
--  BC-0006            -> Refactoring, Borrowed (active)
--  BC-0007            -> Refactoring, Available
-- (join to books on "ISBN" when inserting for real)

-- Members ----------------------------------------------------------
INSERT INTO members ("Id","MembershipNumber","Name","Email","Phone","Address","Status","MembershipExpiresAt","SuspendedAt","LastRenewedAt","IsDeleted","CreatedAtUtc","UpdatedAtUtc") VALUES
 (gen_random_uuid(),'MEM-100001','Alice Johnson','alice@example.com','+1-202-555-0101','12 Oak Street, Springfield','Active',   now() + interval '200 days', NULL, NULL, false, now(), now()),
 (gen_random_uuid(),'MEM-100002','Bob Smith',    'bob@example.com',  '+1-202-555-0102','48 Elm Avenue, Springfield','Active',   now() + interval '15 days',  NULL, NULL, false, now(), now()),
 (gen_random_uuid(),'MEM-100003','Charlie Brown','charlie@example.com','+1-202-555-0103','7 Pine Road, Shelbyville','Active',   now() + interval '90 days',  NULL, NULL, false, now(), now()),
 (gen_random_uuid(),'MEM-100004','Dana Lee',     'dana@example.com', '+1-202-555-0104','90 Maple Lane, Ogdenville','Inactive', now() - interval '10 days',  NULL, NULL, false, now(), now()),
 (gen_random_uuid(),'MEM-100005','Erin Park',    'erin@example.com', '+1-202-555-0105','3 Birch Court, Springfield','Active',   now() + interval '120 days', NULL, NULL, false, now(), now());

-- Borrow records -------------------------------------------------
--  Alice  -> BC-0006, borrowed 3 days ago, due in 11 days (Active)
--  Erin   -> BC-0003, borrowed 30 days ago, due 9 days ago    (Active, overdue -> nightly job suspends Erin)

COMMIT;
