import { Plus, Search, SlidersHorizontal, X } from 'lucide-react'
import { useMemo, useState } from 'react'
import {
  OPERATORS,
  type FieldDef,
  type FilterRow,
  type SearchState,
} from '@/lib/search'
import type { FilterOperator, SortSpec } from '@/lib/types'
import { Button, Select, TextInput } from './ui'

export type { FieldDef, SearchState } from '@/lib/search'

let rowSeq = 1

export function AdvancedSearch({
  fields,
  state,
  onChange,
}: {
  fields: FieldDef[]
  state: SearchState
  onChange: (next: SearchState) => void
}) {
  const [open, setOpen] = useState(false)
  const fieldByName = useMemo(
    () => Object.fromEntries(fields.map((f) => [f.name, f])) as Record<string, FieldDef>,
    [fields],
  )

  const patch = (partial: Partial<SearchState>) =>
    onChange({ ...state, page: 1, ...partial })

  const addFilter = () => {
    const first = fields[0]
    patch({
      filters: [
        ...state.filters,
        {
          id: rowSeq++,
          field: first.name,
          operator: OPERATORS[first.type][0].value,
          value: '',
        },
      ],
    })
    setOpen(true)
  }

  const updateFilter = (id: number, partial: Partial<FilterRow>) =>
    patch({
      filters: state.filters.map((f) => (f.id === id ? { ...f, ...partial } : f)),
    })

  const removeFilter = (id: number) =>
    patch({ filters: state.filters.filter((f) => f.id !== id) })

  const toggleSort = (field: string) => {
    const existing = state.sort.find((s) => s.field === field)
    let next: SortSpec[]
    if (!existing) next = [...state.sort, { field, direction: 'asc' }]
    else if (existing.direction === 'asc')
      next = state.sort.map((s) => (s.field === field ? { ...s, direction: 'desc' } : s))
    else next = state.sort.filter((s) => s.field !== field)
    patch({ sort: next })
  }

  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-3 shadow-sm">
      <div className="flex flex-wrap items-center gap-2">
        <div className="relative min-w-[220px] flex-1">
          <Search
            size={16}
            className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-400"
          />
          <TextInput
            className="pl-9"
            placeholder="Quick search…"
            value={state.quick}
            onChange={(e) => patch({ quick: e.target.value })}
          />
        </div>
        <Button
          variant={open || state.filters.length ? 'primary' : 'secondary'}
          size="sm"
          onClick={() => setOpen((o) => !o)}
        >
          <SlidersHorizontal size={14} />
          Filters{state.filters.length ? ` (${state.filters.length})` : ''}
        </Button>
      </div>

      {open && (
        <div className="mt-3 space-y-2 border-t border-slate-100 pt-3">
          <div className="flex items-center gap-2 text-xs text-slate-500">
            <span>Match</span>
            <Select
              className="w-auto py-1 text-xs"
              value={state.match}
              onChange={(e) => patch({ match: e.target.value as 'all' | 'any' })}
            >
              <option value="all">ALL filters (AND)</option>
              <option value="any">ANY filter (OR)</option>
            </Select>
          </div>

          {state.filters.map((row) => {
            const def = fieldByName[row.field]
            const ops = OPERATORS[def?.type ?? 'text']
            return (
              <div key={row.id} className="flex flex-wrap items-center gap-2">
                <Select
                  className="w-40 py-1.5 text-sm"
                  value={row.field}
                  onChange={(e) => {
                    const nd = fieldByName[e.target.value]
                    updateFilter(row.id, {
                      field: e.target.value,
                      operator: OPERATORS[nd.type][0].value,
                      value: '',
                    })
                  }}
                >
                  {fields.map((f) => (
                    <option key={f.name} value={f.name}>
                      {f.label}
                    </option>
                  ))}
                </Select>
                <Select
                  className="w-40 py-1.5 text-sm"
                  value={row.operator}
                  onChange={(e) =>
                    updateFilter(row.id, { operator: e.target.value as FilterOperator })
                  }
                >
                  {ops.map((o) => (
                    <option key={o.value} value={o.value}>
                      {o.label}
                    </option>
                  ))}
                </Select>
                {def?.type === 'enum' ? (
                  <Select
                    className="w-44 py-1.5 text-sm"
                    value={row.value}
                    onChange={(e) => updateFilter(row.id, { value: e.target.value })}
                  >
                    <option value="">— pick —</option>
                    {def.options?.map((o) => (
                      <option key={o} value={o}>
                        {o}
                      </option>
                    ))}
                  </Select>
                ) : (
                  <TextInput
                    className="w-44 py-1.5 text-sm"
                    type={
                      def?.type === 'number'
                        ? 'number'
                        : def?.type === 'date'
                          ? 'date'
                          : 'text'
                    }
                    value={row.value}
                    onChange={(e) => updateFilter(row.id, { value: e.target.value })}
                  />
                )}
                <button
                  type="button"
                  onClick={() => removeFilter(row.id)}
                  className="rounded-md p-1 text-slate-400 hover:bg-rose-50 hover:text-rose-600"
                >
                  <X size={16} />
                </button>
              </div>
            )
          })}

          <div className="flex flex-wrap items-center gap-2 pt-1">
            <Button variant="ghost" size="sm" onClick={addFilter}>
              <Plus size={14} /> Add filter
            </Button>
            {state.filters.length > 0 && (
              <Button variant="ghost" size="sm" onClick={() => patch({ filters: [] })}>
                Clear all
              </Button>
            )}
          </div>

          <div className="flex flex-wrap items-center gap-2 border-t border-slate-100 pt-2 text-xs text-slate-500">
            <span>Sort by</span>
            {fields.map((f) => {
              const s = state.sort.find((x) => x.field === f.name)
              return (
                <button
                  key={f.name}
                  type="button"
                  onClick={() => toggleSort(f.name)}
                  className={
                    s
                      ? 'rounded-full bg-brand-600 px-2.5 py-1 text-xs font-medium text-white'
                      : 'rounded-full bg-slate-100 px-2.5 py-1 text-xs font-medium text-slate-600 hover:bg-slate-200'
                  }
                >
                  {f.label}
                  {s ? (s.direction === 'asc' ? ' ↑' : ' ↓') : ''}
                </button>
              )
            })}
          </div>
        </div>
      )}
    </div>
  )
}
