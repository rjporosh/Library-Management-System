# Libra web (Next.js 16 / React 19)

App Router, Tailwind CSS 4, TanStack Query. All screens are client components because the session
and language live in `localStorage`; `src/app/providers.tsx` gates the tree until the client is ready.

```bash
npm install
npm run dev        # http://localhost:5173 (proxies /api, /hubs to API_INTERNAL_URL, default http://localhost:5254)
npm run lint && npm run typecheck && npm test
npm run build && npm start
```

| Path | Purpose |
|---|---|
| `src/app` | Routes: `login`, `register`, and the authenticated `(app)` group (guarded by `AppShell`) |
| `src/views` | Screen components (one per route) |
| `src/components` | Shared UI (`ui.tsx`, `AppShell`, `AuthFrame`, `SpineWall`, tables, chat) |
| `src/lib` | API client, i18n (`en`/`bn`), formatting, SignalR |

Design tokens (palette, fonts) live in `src/app/globals.css`; neutrals and semantic colors are remapped
there so utility classes (`slate-*`, `rose-*`, `amber-*`, `brand-*`) follow the Libra palette.
