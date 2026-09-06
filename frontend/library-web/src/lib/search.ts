import { t } from './i18n'
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
    { value: 'contains', label: t('op.contains') },
    { value: 'notContains', label: t('op.notContains') },
    { value: 'eq', label: t('op.is') },
    { value: 'neq', label: t('op.isNot') },
    { value: 'startsWith', label: t('op.startsWith') },
    { value: 'endsWith', label: t('op.endsWith') },
  ],
  enum: [
    { value: 'eq', label: t('op.is') },
    { value: 'neq', label: t('op.isNot') },
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
    { value: 'eq', label: t('op.on') },
    { value: 'gt', label: t('op.after') },
    { value: 'lt', label: t('op.before') },
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
