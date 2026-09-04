# Frontend — Developer Guide

React 19 + Vite + TypeScript (strict) + Tailwind v4 + TanStack Query + axios.

## Run

```bash
npm install
npm run dev            # http://localhost:5173 (needs the API on :5254)
npm run build          # tsc -b && vite build
npm run lint
```

`.env` → `VITE_API_BASE_URL` (dev). `.env.production` → `/api` (nginx proxy).

## Layout

```
src/
  api/index.ts          typed clients (booksApi, membersApi, …) over lib/api.ts
  lib/
    api.ts              axios instance + normaliseError() + SweetAlert2 helpers
    types.ts            API contract types
    search.ts           SearchState <-> SearchRequest, operator sets
    format.ts           status -> badge tone, date formatting
    useSearchList.ts    hook: AdvancedSearch state + debounced query + sorting
  components/
    ui.tsx              Badge, StatusPill, Button, Card, Modal, FormField, TextInput, Select, Spinner, …
    DataTable.tsx       sortable table with loading/empty/error states
    AdvancedSearch.tsx  GitLab-style filter builder (field / operator / value, AND/OR, sort chips)
    Pagination.tsx
    BulkImportModal.tsx dropzone + template download + row/field error table
  pages/                one file per route
  App.tsx               shell (sidebar) + routes
```

## Add a list page

Copy `pages/BooksPage.tsx`:

1. `FIELDS: FieldDef[]` — the searchable fields (`type: 'text' | 'enum' |
   'number' | 'date'`, `options` for enums).
2. `useSearchList('key', xApi.search)` → `{ state, setState, query, setPage,
   toggleSort, errorMessage }`.
3. `<AdvancedSearch fields state onChange={setState} />` +
   `<DataTable columns rows={query.data?.items} sort onSortChange={toggleSort} />`
   + `<Pagination page setPage data={query.data} />`.
4. Mutations via `useMutation`; on error, `normaliseError(e).fieldErrors` feeds
   `<FormField error={…}>`; on success, `toastSuccess(...)` +
   `qc.invalidateQueries({ queryKey: ['key'] })`.

## Errors & status

- All API errors go through `normaliseError` → `{ message, fieldErrors,
  rowErrors, traceId }`. 5xx also raises a SweetAlert automatically.
- Status values are strings from the API; render with `<StatusPill status={…} />`
  (colour map in `lib/format.ts`).

## Tests

None yet. Add Vitest + Testing Library; `npm run build` + `npm run lint` is the
current gate (also enforced in CI).
