import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { useEffect, useMemo, useState } from 'react'
import { emptySearchState, toSearchRequest, type SearchState } from '@/lib/search'
import { normaliseError } from '@/lib/api'
import type { Paged, SearchRequest, SortSpec } from '@/lib/types'

/**
 * Wires an AdvancedSearch state to a paged /search endpoint with a debounced
 * quick-search term and column-header sorting.
 */
export function useSearchList<T>(
  key: string,
  fetcher: (req: SearchRequest) => Promise<Paged<T>>,
  pageSize = 10,
) {
  const [state, setState] = useState<SearchState>(() => emptySearchState(pageSize))
  const [debouncedQuick, setDebouncedQuick] = useState('')

  // debounce quick search
  useEffect(() => {
    const id = setTimeout(() => setDebouncedQuick(state.quick), 300)
    return () => clearTimeout(id)
  }, [state.quick])

  const request = useMemo(
    () => toSearchRequest({ ...state, quick: debouncedQuick }),
    [state, debouncedQuick],
  )

  const query = useQuery({
    queryKey: [key, request],
    queryFn: () => fetcher(request),
    placeholderData: keepPreviousData,
  })

  const setPage = (page: number) => setState((s) => ({ ...s, page }))

  const toggleSort = (field: string) =>
    setState((s) => {
      const existing = s.sort.find((x) => x.field.toLowerCase() === field.toLowerCase())
      let sort: SortSpec[]
      if (!existing) sort = [{ field, direction: 'asc' }]
      else if (existing.direction === 'asc') sort = [{ field, direction: 'desc' }]
      else sort = []
      return { ...s, sort, page: 1 }
    })

  return {
    state,
    setState,
    query,
    setPage,
    toggleSort,
    errorMessage: query.isError ? normaliseError(query.error).message : undefined,
  }
}
