#!/usr/bin/env python3
"""Generates the Postman v2.1 collection + environment for the Library Management System API.
Run:  python3 postman/build_collection.py   (from the solution root)
Kept in the repo so the collection can be regenerated when endpoints change."""
import json, pathlib

BASE = "{{baseUrl}}"
OUT = pathlib.Path(__file__).parent


def url(path, query=None):
    raw = f"{BASE}{path}"
    u = {"raw": raw, "host": [BASE], "path": [p for p in path.strip("/").split("/") if p != ""]}
    if query:
        u["query"] = query
        u["raw"] = raw + "?" + "&".join(f"{q['key']}={q.get('value','')}" for q in query)
    return u


def q(key, value, desc=None, disabled=False):
    d = {"key": key, "value": str(value)}
    if desc: d["description"] = desc
    if disabled: d["disabled"] = True
    return d


def req(name, method, path, desc, body=None, query=None, form=None, tests=None, prereq=None,
        auth_note=None):
    r = {"name": name, "request": {"method": method,
         "header": [{"key": "Accept", "value": "application/json"}],
         "url": url(path, query), "description": desc}}
    if body is not None:
        r["request"]["header"].append({"key": "Content-Type", "value": "application/json"})
        r["request"]["body"] = {"mode": "raw", "raw": json.dumps(body, indent=2),
                                 "options": {"raw": {"language": "json"}}}
    if form is not None:
        r["request"]["body"] = {"mode": "formdata", "formdata": form}
        r["request"]["header"] = [h for h in r["request"]["header"] if h["key"] != "Content-Type"]
    ev = []
    if prereq:
        ev.append({"listen": "prerequest", "script": {"type": "text/javascript", "exec": prereq}})
    if tests:
        ev.append({"listen": "test", "script": {"type": "text/javascript", "exec": tests}})
    if ev:
        r["event"] = ev
    return r


def folder(name, desc, items):
    return {"name": name, "description": desc, "item": items}


BOOK_BODY = {"isbn": "978-0000000001", "title": "The Pragmatic Sequel", "author": "A. Hunt",
             "publishedYear": 2020, "category": "Software Engineering", "publisher": "Addison-Wesley",
             "description": "Example book created from Postman."}
SEARCH_BODY = {"filters": [{"field": "title", "operator": "contains", "values": ["clean"]},
                           {"field": "publishedYear", "operator": "gte", "value": "2000"}],
               "match": "all",
               "sort": [{"field": "publishedYear", "direction": "desc"}],
               "page": 1, "pageSize": 20}
MEMBER_SEARCH = {"filters": [{"field": "status", "operator": "in", "values": ["Active", "Suspended"]}],
                 "match": "all", "sort": [{"field": "name", "direction": "asc"}], "page": 1, "pageSize": 20}
COPY_SEARCH = {"filters": [{"field": "status", "operator": "eq", "value": "Available"}],
               "match": "all", "sort": [{"field": "barcode", "direction": "asc"}], "page": 1, "pageSize": 20}
BORROW_SEARCH = {"filters": [{"field": "status", "operator": "eq", "value": "Active"}],
                 "match": "all", "sort": [{"field": "dueAt", "direction": "asc"}], "page": 1, "pageSize": 20}
MEMBER_BODY = {"membershipNumber": "MEM-900001", "name": "Jordan Rivera",
               "email": "jordan.rivera@example.com", "phone": "555-0101", "address": "12 Library Lane"}

paging = [q("pageNumber", 1, "1-based page (books)", True), q("pageSize", 20, "", True),
          q("search", "clean", "quick partial match", True),
          q("searchBy", "title,author", "comma list: title,author,isbn", True),
          q("sortBy", "publishedYear", "", True), q("sortDirection", "desc", "asc|desc", True)]

books = folder("Books", "Catalog CRUD, advanced search and Excel bulk import.", [
    req("List books", "GET", "/api/books",
        "Quick text search + paging. All query params optional. Use **Search books** for advanced filters.",
        query=paging),
    req("Get book by id", "GET", "/api/books/{{bookId}}", "Fetch a single book. 404 if it does not exist."),
    req("Search books (advanced)", "POST", "/api/books/search",
        "GitLab-style search: multiple filters (operators eq, neq, contains, notContains, startsWith, "
        "endsWith, gt, gte, lt, lte, in, notIn, between), `match` all|any, multi-sort. Enum fields match by name.",
        body=SEARCH_BODY),
    req("Create book", "POST", "/api/books",
        "Adds a book. `category` and `publisher` are required. Duplicate ISBN -> 409 BOOK_ISBN_DUPLICATE. "
        "Validation returns every error at once.", body=BOOK_BODY,
        tests=["const b = pm.response.json();",
               "if (pm.response.code === 201 && b.id) { pm.collectionVariables.set('bookId', b.id); }",
               "pm.test('created', () => pm.expect(pm.response.code).to.be.oneOf([201,409,400]));"]),
    req("Update book", "PUT", "/api/books/{{bookId}}", "Replaces all editable fields.",
        body={**BOOK_BODY, "title": "The Pragmatic Sequel (2nd ed.)"}),
    req("Delete book", "DELETE", "/api/books/{{bookId}}",
        "Soft delete. If copies exist -> 409 BOOK_HAS_DEPENDENT_COPIES unless `force=true` (cascades). "
        "If any copy is borrowed -> always 409 BOOK_HAS_BORROWED_COPIES.",
        query=[q("force", "false", "set true to cascade-delete copies")]),
    req("Download import template", "GET", "/api/books/import/template",
        "Streams the .xlsx template (headers + example rows + instructions sheet)."),
    req("Bulk import books", "POST", "/api/books/import",
        "multipart/form-data, field `file`. All-or-nothing: any invalid/duplicate row rolls back the whole "
        "file and every error is returned with row + column + code.",
        form=[{"key": "file", "type": "file", "src": [], "description": "the filled-in .xlsx template"}]),
])

copies = folder("Book Copies", "Physical copies of a book: CRUD, status changes, search, bulk import.", [
    req("List copies", "GET", "/api/book-copies", "Quick search + optional bookId/status filter + paging.",
        query=[q("search", "", "", True), q("bookId", "{{bookId}}", "", True),
               q("status", "Available", "enum name", True), q("page", 1, "", True), q("pageSize", 20, "", True),
               q("sortBy", "barcode", "", True), q("sortDirection", "asc", "", True)]),
    req("Search copies (advanced)", "POST", "/api/book-copies/search",
        "Advanced search; `status` matched by name (Available, Borrowed, Lost, Damaged, Maintenance).",
        body=COPY_SEARCH),
    req("Get copies of a book", "GET", "/api/book-copies/book/{{bookId}}", "All copies for one book id."),
    req("Get copy by id", "GET", "/api/book-copies/{{copyId}}", "Single copy by id."),
    req("Create copy", "POST", "/api/book-copies",
        "Registers a new copy. Duplicate barcode -> 409 BOOK_COPY_BARCODE_DUPLICATE.",
        body={"bookId": "{{bookId}}", "barcode": "BC-90001"},
        tests=["const c = pm.response.json();",
               "if (pm.response.code === 201 && c.id) { pm.collectionVariables.set('copyId', c.id); }"]),
    req("Update copy barcode", "PUT", "/api/book-copies/{{copyId}}", "Corrects the barcode.",
        body={"barcode": "BC-90002"}),
    req("Change copy status", "POST", "/api/book-copies/{{copyId}}/status",
        "Condition change. Cannot set Borrowed directly; cannot change a borrowed copy.",
        body={"status": "Maintenance"}),
    req("Delete copy", "DELETE", "/api/book-copies/{{copyId}}",
        "Soft delete. Blocked while borrowed. With borrow history -> 409 unless `force=true`.",
        query=[q("force", "false")]),
    req("Download import template", "GET", "/api/book-copies/import/template", "Streams the .xlsx template."),
    req("Bulk import copies", "POST", "/api/book-copies/import",
        "multipart field `file`. All-or-nothing; references books by ISBN.",
        form=[{"key": "file", "type": "file", "src": []}]),
])

members = folder("Members", "Membership CRUD, lifecycle (suspend/reactivate/renew/deactivate), search, bulk import.", [
    req("List members", "GET", "/api/members", "Quick search + status filter + paging.",
        query=[q("search", "", "", True), q("status", "Active", "enum name", True), q("page", 1, "", True),
               q("pageSize", 20, "", True), q("sortBy", "name", "", True), q("sortDirection", "asc", "", True)]),
    req("Search members (advanced)", "POST", "/api/members/search",
        "Advanced search; `status` matched by name (Active, Inactive, Suspended).", body=MEMBER_SEARCH),
    req("Get member by id", "GET", "/api/members/{{memberId}}", "Single member."),
    req("Get member detail", "GET", "/api/members/{{memberId}}/detail",
        "Member + borrowing summary (total/active/overdue/last) + history."),
    req("Create member", "POST", "/api/members",
        "Enrols a member (starts Active, one-year term). `phone` and `address` required. "
        "Duplicate number or email -> 409 (both errors returned together).", body=MEMBER_BODY,
        tests=["const m = pm.response.json();",
               "if (pm.response.code === 201 && m.id) { pm.collectionVariables.set('memberId', m.id); }"]),
    req("Update member", "PUT", "/api/members/{{memberId}}", "Updates profile fields.",
        body={**MEMBER_BODY, "address": "34 Reading Road"}),
    req("Delete member", "DELETE", "/api/members/{{memberId}}",
        "Soft delete. Blocked with an active borrow (409 MEMBER_HAS_ACTIVE_BORROW). "
        "With borrow history -> 409 unless `force=true`.", query=[q("force", "false")]),
    req("Suspend member", "POST", "/api/members/{{memberId}}/suspend", "Same action the nightly job performs for overdue borrowers."),
    req("Reactivate member", "POST", "/api/members/{{memberId}}/reactivate", "Back to Active without recording a renewal."),
    req("Renew membership", "POST", "/api/members/{{memberId}}/renew", "Clears suspension and extends the term by one year."),
    req("Deactivate member", "POST", "/api/members/{{memberId}}/deactivate", "Active -> Inactive (manual equivalent of the expiry job)."),
    req("Download import template", "GET", "/api/members/import/template", "Streams the .xlsx template."),
    req("Bulk import members", "POST", "/api/members/import", "multipart field `file`. All-or-nothing.",
        form=[{"key": "file", "type": "file", "src": []}]),
])

borrowing = folder("Borrowing", "Issue and return copies; search borrow records.", [
    req("Search borrow records", "POST", "/api/borrowing/search",
        "Advanced search; `status` matched by name (Active, Returned, Overdue).", body=BORROW_SEARCH),
    req("Issue copy to member", "POST", "/api/borrowing/issue",
        "Member must be Active and not expired; copy must be Available. Conflicts return 409.",
        body={"memberId": "{{memberId}}", "bookCopyId": "{{copyId}}", "dueAt": "2026-12-31T00:00:00Z"},
        tests=["const r = pm.response.json();",
               "if (pm.response.code === 201 && r.id) { pm.collectionVariables.set('borrowRecordId', r.id); }"]),
    req("Return copy", "POST", "/api/borrowing/{{borrowRecordId}}/return",
        "Closes the borrow record. `returnedAt` optional (defaults to now).", body={"returnedAt": None}),
])

dashboard = folder("Dashboard", "Aggregated totals for the librarian home screen.", [
    req("Get dashboard summary", "GET", "/api/dashboard",
        "Catalogue/copy/member/borrowing totals + recent borrows. Served via EF Core or Dapper per `Database:Orm`."),
])

jobs = folder("Jobs", "Manual triggers for scheduled maintenance work.", [
    req("Run membership maintenance now", "POST", "/api/jobs/member-maintenance/run",
        "Runs the same pass as the midnight job: suspend overdue borrowers + mark expired memberships Inactive. "
        "Returns a summary. Gated by the EnableMemberSuspensionCronJob feature flag."),
])

metadata = folder("Metadata", "Reference data for building the UI (enums, operators, languages, messages).", [
    req("Enums", "GET", "/api/metadata/enums", "Every status enum and its allowed values, by name."),
    req("Search operators", "GET", "/api/metadata/search-operators", "Operators the advanced-search endpoints accept."),
    req("Languages", "GET", "/api/metadata/languages", "Supported UI languages (default first)."),
    req("Localized messages", "GET", "/api/metadata/messages",
        "Localized text for every error code / system message in the requested culture.",
        query=[q("culture", "en", "en | bn", True)]),
])

release = folder("Release Notes", "Machine-readable release information.", [
    req("Current release", "GET", "/api/release-notes/current", "Version, date, features, fixes, QA checklist."),
])

logs = folder("Logs", "Structured runtime / build-error / query log files.", [
    req("List available logs", "GET", "/api/logs/available",
        "Log categories and, optionally, the files for one category.",
        query=[q("category", "runtime-errors", "runtime-errors|build-errors|query-logs|exceptions", True)]),
    req("Download a log file", "GET", "/api/logs/download",
        "Downloads the exact file for a category/date.",
        query=[q("category", "runtime-errors"), q("date", "", "yyyy-MM-dd; omit for today", True)]),
])

health = folder("Health", "Liveness / readiness.", [
    req("Health check", "GET", "/health", "200 Healthy when the app and its dependencies are reachable."),
])

smoke = folder("Smoke Flow (run in order)",
    "Runnable end-to-end happy path. Use the Collection Runner on this folder: it creates a book, a copy, "
    "a member, issues and returns the copy, then cascade-deletes the book. IDs are chained via collection variables.", [
    req("1. Create book", "POST", "/api/books",
        "Creates a uniquely-named book so the flow is re-runnable.",
        body=BOOK_BODY,
        prereq=["const n = Date.now().toString().slice(-9);",
                "pm.collectionVariables.set('smokeIsbn', '978' + n);"],
        tests=["pm.test('201 Created', () => pm.response.to.have.status(201));",
               "const b = pm.response.json();",
               "pm.collectionVariables.set('bookId', b.id);",
               "pm.expect(b.category).to.eql('Software Engineering');"]),
    req("2. Get the book", "GET", "/api/books/{{bookId}}", "Confirms it is retrievable.",
        tests=["pm.test('200 OK', () => pm.response.to.have.status(200));"]),
    req("3. Create a copy", "POST", "/api/book-copies",
        "Adds one physical copy.", body={"bookId": "{{bookId}}", "barcode": "BC-SMOKE-1"},
        prereq=["pm.collectionVariables.set('smokeBarcode', 'BC-' + Date.now().toString().slice(-8));"],
        tests=["pm.test('201 Created', () => pm.response.to.have.status(201));",
               "pm.collectionVariables.set('copyId', pm.response.json().id);"]),
    req("4. Create a member", "POST", "/api/members", "Enrols a member.",
        body=MEMBER_BODY,
        prereq=["const n = Date.now().toString().slice(-6);",
                "pm.collectionVariables.set('smokeMem', 'MEM-' + n);"],
        tests=["pm.test('201 Created', () => pm.response.to.have.status(201));",
               "pm.collectionVariables.set('memberId', pm.response.json().id);"]),
    req("5. Issue the copy", "POST", "/api/borrowing/issue", "Lends the copy to the member.",
        body={"memberId": "{{memberId}}", "bookCopyId": "{{copyId}}", "dueAt": "2026-12-31T00:00:00Z"},
        tests=["pm.test('201 Created', () => pm.response.to.have.status(201));",
               "pm.collectionVariables.set('borrowRecordId', pm.response.json().id);"]),
    req("6. Delete book while borrowed (expect 409)", "DELETE", "/api/books/{{bookId}}",
        "Demonstrates the borrowed-dependent guard.", query=[q("force", "true")],
        tests=["pm.test('409 Conflict', () => pm.response.to.have.status(409));",
               "pm.test('reports borrowed copies', () => pm.expect(pm.response.text()).to.include('BOOK_HAS_BORROWED_COPIES'));"]),
    req("7. Return the copy", "POST", "/api/borrowing/{{borrowRecordId}}/return", "Closes the borrow.",
        body={"returnedAt": None},
        tests=["pm.test('200 OK', () => pm.response.to.have.status(200));"]),
    req("8. Cascade-delete the book", "DELETE", "/api/books/{{bookId}}",
        "Now succeeds with force=true, removing the book and its copy.", query=[q("force", "true")],
        tests=["pm.test('204 No Content', () => pm.response.to.have.status(204));"]),
    req("9. Confirm book is gone", "GET", "/api/books/{{bookId}}", "Soft-deleted -> hidden from reads.",
        tests=["pm.test('404 Not Found', () => pm.response.to.have.status(404));"]),
])

collection = {
    "info": {
        "name": "Library Management System API",
        "_postman_id": "6f3b1c2a-0d4e-4a7b-9c11-libmgmt000001",
        "description": "Enterprise Library Management System (.NET 10). Import this file, select the "
                       "\"Library MS - Local\" environment (or keep the baseUrl collection variable = "
                       "http://localhost:5254), start the API with `dotnet run --project src/Library.Api`, "
                       "and every request runs as-is. The **Smoke Flow** folder is a runnable end-to-end "
                       "happy path for the Collection Runner. Failure responses follow RFC 7807 "
                       "(application/problem+json) with `success`, `errors[]` and `correlationId`.",
        "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
    },
    "item": [books, copies, members, borrowing, dashboard, jobs, metadata, release, logs, health, smoke],
    "variable": [
        {"key": "baseUrl", "value": "http://localhost:5254"},
        {"key": "bookId", "value": ""},
        {"key": "copyId", "value": ""},
        {"key": "memberId", "value": ""},
        {"key": "borrowRecordId", "value": ""},
    ],
}

environment = {
    "name": "Library MS - Local",
    "values": [
        {"key": "baseUrl", "value": "http://localhost:5254", "enabled": True},
        {"key": "bookId", "value": "", "enabled": True},
        {"key": "copyId", "value": "", "enabled": True},
        {"key": "memberId", "value": "", "enabled": True},
        {"key": "borrowRecordId", "value": "", "enabled": True},
    ],
    "_postman_variable_scope": "environment",
}

(OUT / "Library-Management-System.postman_collection.json").write_text(json.dumps(collection, indent=2) + "\n")
(OUT / "Library-Management-System.postman_environment.json").write_text(json.dumps(environment, indent=2) + "\n")

n = sum(len(f["item"]) for f in collection["item"])
print(f"wrote collection ({len(collection['item'])} folders, {n} requests) + environment")
