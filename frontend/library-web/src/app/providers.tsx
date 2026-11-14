'use client'

import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useEffect, useState, type ReactNode } from 'react'
import { AuthProvider } from '@/context/AuthContext'
import { getLang, loadMessages } from '@/lib/i18n'

/**
 * Session and language live in localStorage, so the tree renders on the client only.
 * Gating on `ready` keeps server and client markup identical (no hydration mismatch) and
 * guarantees the localized error catalogue is loaded before the first screen.
 */
export function Providers({ children }: Readonly<{ children: ReactNode }>) {
  const [client] = useState(
    () => new QueryClient({ defaultOptions: { queries: { staleTime: 30_000, retry: 1 } } }),
  )
  const [ready, setReady] = useState(false)

  useEffect(() => {
    document.documentElement.lang = getLang()
    void loadMessages().finally(() => setReady(true))
  }, [])

  if (!ready) return null

  return (
    <QueryClientProvider client={client}>
      <AuthProvider>{children}</AuthProvider>
    </QueryClientProvider>
  )
}
