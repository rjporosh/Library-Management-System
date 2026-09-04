import type { FilterOperator, SearchRequest, SortSpec } from './types'

export type FieldType = 'text' | 'enum' | 'number' | 'date'

export interface FieldDef {
  name: string
  label: string
  type: FieldType
  options?: string[]
}

export interface FilterRow {
  id: number
  field: string
  operator: FilterOperator
  value: string
}

export interface SearchState {
  quick: string
  match: 'all' | 'any'
  filters: FilterRow[]
  sort: SortSpec[]
  page: number
  pageSize: number
}

export const OPERATORS: Record<FieldType, { value: FilterOperator; label: string }[]> = {
  text: [
    { value: 'contains', label: 'contains' },
    { value: 'notContains', label: 'does not contain' },
    { value: 'eq', label: 'is' },
    { value: 'neq', label: 'is not' },
    { value: 'startsWith', label: 'starts with' },
    { value: 'endsWith', label: 'ends with' },
  ],
  enum: [
    { value: 'eq', label: 'is' },
    { value: 'neq', label: 'is not' },
  ],
  number: [
    { value: 'eq', label: '=' },
    { value: 'neq', label: '≠' },
    { value: 'gt', label: '>' },
    { value: 'gte', label: '≥' },
    { value: 'lt', label: '<' },
    { value: 'lte', label: '≤' },
  ],
  date: [
    { value: 'eq', label: 'on' },
    { value: 'gt', label: 'after' },
    { value: 'lt', label: 'before' },
  ],
}

export function emptySearchState(pageSize = 10): SearchState {
  return { quick: '', match: 'all', filters: [], sort: [], page: 1, pageSize }
}

export function toSearchRequest(state: SearchState): SearchRequest {
  return {
    search: state.quick.trim() || undefined,
    match: state.match,
    filters: state.filters
      .filter((f) => f.field && f.value.trim() !== '')
      .map((f) => ({ field: f.field, operator: f.operator, value: f.value.trim() })),
    sort: state.sort,
    page: state.page,
    pageSize: state.pageSize,
  }
}
