-- ===========================================================================
-- PostgreSQL schema for the Library Management System.
--
-- Generated from the EF Core migrations - DO NOT hand-edit. Regenerate after
-- adding a migration (run from the repository root):
--
--   dotnet ef migrations script --idempotent \
--     --project src/Library.Infrastructure --startup-project src/Library.Api \
--     --output docs/database/schema.sql
--
-- Apply it to a database directly (idempotent):
--
--   psql "$LMS_DESIGN_CONNECTION" -f docs/database/schema.sql
--
-- Or let EF apply the migrations instead:
--
--   dotnet ef database update \
--     --project src/Library.Infrastructure --startup-project src/Library.Api
--
-- Demo/QA rows: docs/database/seed-data.sql. Full command reference: /MIGRATIONS.md
-- ===========================================================================

CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260906152801_InitialCreate') THEN
    CREATE TABLE books (
        "Id" uuid NOT NULL,
        "ISBN" character varying(20) NOT NULL,
        "Title" character varying(400) NOT NULL,
        "Author" character varying(400) NOT NULL,
        "Category" character varying(200) NOT NULL,
        "Publisher" character varying(200) NOT NULL,
        "Description" character varying(4000),
        "PublishedYear" integer NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_books" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260906152801_InitialCreate') THEN
    CREATE TABLE members (
        "Id" uuid NOT NULL,
        "MembershipNumber" character varying(64) NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Email" character varying(256) NOT NULL,
        "Phone" character varying(64) NOT NULL,
        "Address" character varying(500) NOT NULL,
        "Status" character varying(32) NOT NULL,
        "SuspendedAt" timestamp with time zone,
        "LastRenewedAt" timestamp with time zone,
        "MembershipExpiresAt" timestamp with time zone NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_members" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260906152801_InitialCreate') THEN
    CREATE TABLE book_copies (
        "Id" uuid NOT NULL,
        "BookId" uuid NOT NULL,
        "Barcode" character varying(64) NOT NULL,
        "Status" character varying(32) NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_book_copies" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_book_copies_books_BookId" FOREIGN KEY ("BookId") REFERENCES books ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260906152801_InitialCreate') THEN
    CREATE TABLE borrow_records (
        "Id" uuid NOT NULL,
        "BookCopyId" uuid NOT NULL,
        "MemberId" uuid NOT NULL,
        "BorrowedAt" timestamp with time zone NOT NULL,
        "DueAt" timestamp with time zone NOT NULL,
        "ReturnedAt" timestamp with time zone,
        "Status" character varying(32) NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "DeletedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_borrow_records" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_borrow_records_book_copies_BookCopyId" FOREIGN KEY ("BookCopyId") REFERENCES book_copies ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_borrow_records_members_MemberId" FOREIGN KEY ("MemberId") REFERENCES members ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260906152801_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_book_copies_Barcode" ON book_copies ("Barcode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260906152801_InitialCreate') THEN
    CREATE INDEX "IX_book_copies_BookId" ON book_copies ("BookId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260906152801_InitialCreate') THEN
    CREATE INDEX "IX_book_copies_IsDeleted" ON book_copies ("IsDeleted");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260906152801_InitialCreate') THEN
    CREATE INDEX "IX_book_copies_Status" ON book_copies ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260906152801_InitialCreate') THEN
    CREATE INDEX "IX_books_Category" ON books ("Category");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260906152801_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_books_ISBN" ON books ("ISBN");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260906152801_InitialCreate') THEN
    CREATE INDEX "IX_books_IsDeleted" ON books ("IsDeleted");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260906152801_InitialCreate') THEN
    CREATE INDEX "IX_books_Title" ON books ("Title");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260906152801_InitialCreate') THEN
    CREATE INDEX "IX_borrow_records_BookCopyId" ON borrow_records ("BookCopyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260906152801_InitialCreate') THEN
    CREATE INDEX "IX_borrow_records_IsDeleted" ON borrow_records ("IsDeleted");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260906152801_InitialCreate') THEN
    CREATE INDEX "IX_borrow_records_MemberId_Status" ON borrow_records ("MemberId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260906152801_InitialCreate') THEN
    CREATE INDEX "IX_borrow_records_Status_DueAt" ON borrow_records ("Status", "DueAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260906152801_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_members_Email" ON members ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260906152801_InitialCreate') THEN
    CREATE INDEX "IX_members_IsDeleted" ON members ("IsDeleted");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260906152801_InitialCreate') THEN
    CREATE INDEX "IX_members_MembershipExpiresAt" ON members ("MembershipExpiresAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260906152801_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_members_MembershipNumber" ON members ("MembershipNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260906152801_InitialCreate') THEN
    CREATE INDEX "IX_members_Status" ON members ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260906152801_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260906152801_InitialCreate', '10.0.11');
    END IF;
END $EF$;
COMMIT;

