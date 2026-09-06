import { describe, expect, it } from 'vitest'
import { emptySearchState, toSearchRequest } from './search'

describe('toSearchRequest', () => {
  it('drops empty filters and trims values', () => {
    const state = {
      ...emptySearchState(10),
      quick: '  clean  ',
      filters: [
        { id: 1, field: 'author', operator: 'contains' as const, value: ' martin ' },
        { id: 2, field: 'title', operator: 'eq' as const, value: '' }, // dropped
      ],
      sort: [{ field: 'publishedYear', direction: 'desc' as const }],
      match: 'any' as const,
    }

    const req = toSearchRequest(state)

    expect(req.search).toBe('clean')
    expect(req.match).toBe('any')
    expect(req.filters).toEqual([{ field: 'author', operator: 'contains', value: 'martin' }])
    expect(req.sort).toEqual([{ field: 'publishedYear', direction: 'desc' }])
    expect(req.page).toBe(1)
    expect(req.pageSize).toBe(10)
  })

  it('omits an all-whitespace quick search', () => {
    const req = toSearchRequest({ ...emptySearchState(), quick: '   ' })
    expect(req.search).toBeUndefined()
  })
})
