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

## Localization (i18n)

Two layers:

1. **UI strings** — `src/lib/locales/en.ts` is the source of truth. `t('books.title')`
   returns the string for the active language; `t('common.results', { count })`
   fills `{placeholders}`. `tStatus('Active')` localizes an enum value coming
   from the API. Never hard-code a user-visible string — add a key.
2. **API messages** — error codes / system messages come from
   `GET /api/metadata/messages?culture=<lang>` (loaded once in `main.tsx`);
   `normaliseError` maps each error code through it.

The active language lives in `localStorage['lms.lang']`; the sidebar switcher
calls `setLang()` which stores it and reloads (so every query refetches with the
new `?culture=` and every module re-reads the dictionary). Dates use
`bn-BD` / default locale via `lib/format.ts`.

### Add a language

1. `src/lib/locales/xx.ts` — `export const xx: Record<MessageKey, string> = { … }`
   (copy `bn.ts`, translate every value).
2. Register it in `src/lib/locales/index.ts` and add `'xx'` to `Lang` in
   `src/lib/i18n.ts`.
3. Add a button in `src/App.tsx`'s language switcher.
4. Backend: add `Resources/SharedResources.xx.resx` (see
   `docs/programmers-guide/13-localization.md`) and `xx` to
   `RequestLocalizationOptions` in `Program.cs`.
5. `src/lib/i18n.test.ts` enforces that every locale has exactly the `en` keys.

## Errors & status

- All API errors go through `normaliseError` → `{ message, fieldErrors,
  rowErrors, traceId }`. 5xx also raises a SweetAlert automatically.
- Status values are strings from the API; render with `<StatusPill status={…} />`
  (colour map in `lib/format.ts`).

## Tests

Vitest + Testing Library — `npm test`. Covers `lib/search`, `lib/format`,
`lib/i18n` (dictionary parity) and the `ui` components. `npm run lint` +
`npm run build` + `npm test` is the gate, all enforced in CI.
