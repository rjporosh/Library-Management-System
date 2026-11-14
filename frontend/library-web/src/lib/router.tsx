'use client'

import NextLink from 'next/link'
import { useParams as useNextParams, useRouter } from 'next/navigation'
import { useCallback, type ComponentProps } from 'react'

/** Thin adapter so views keep a small, router-agnostic surface (Link/useNavigate/useParams). */
export function Link({ to, ...rest }: Omit<ComponentProps<typeof NextLink>, 'href'> & { to: string }) {
  return <NextLink href={to} {...rest} />
}

export function useNavigate() {
  const router = useRouter()
  return useCallback(
    (to: string, options?: { replace?: boolean }) =>
      options?.replace ? router.replace(to) : router.push(to),
    [router],
  )
}

export function useParams(): Record<string, string | undefined> {
  const params = useNextParams<Record<string, string>>()
  return params ?? {}
}
