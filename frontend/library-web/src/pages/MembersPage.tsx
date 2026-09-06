import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  BadgeCheck,
  Ban,
  MoreHorizontal,
  Pencil,
  Plus,
  RefreshCw,
  Trash2,
  Upload,
  UserX,
} from 'lucide-react'
import { type ReactNode, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { membersApi } from '@/api'
import { AdvancedSearch, type FieldDef } from '@/components/AdvancedSearch'
import { BulkImportModal } from '@/components/BulkImportModal'
import { DataTable, type Column } from '@/components/DataTable'
import { Pagination } from '@/components/Pagination'
import {
  Button,
  FormField,
  Modal,
  PageHeader,
  StatusPill,
  TextInput,
} from '@/components/ui'
import { cascadeDelete, confirmAction, normaliseError, toastError, toastSuccess } from '@/lib/api'
import { formatDate, relativeExpiry } from '@/lib/format'
import { t, tStatus } from '@/lib/i18n'
import type { Member } from '@/lib/types'
import { useSearchList } from '@/lib/useSearchList'

const FIELDS: FieldDef[] = [
  { name: 'name', label: t('members.field.name'), type: 'text' },
  { name: 'membershipNumber', label: t('members.field.number'), type: 'text' },
  { name: 'email', label: t('members.field.email'), type: 'text' },
  { name: 'phone', label: t('members.field.phone'), type: 'text' },
  {
    name: 'status',
    label: t('members.col.status'),
    type: 'enum',
    options: ['Active', 'Suspended', 'Inactive'],
  },
  { name: 'membershipExpiresAt', label: t('members.col.expires'), type: 'date' },
]

type Action = 'suspend' | 'reactivate' | 'renew' | 'deactivate'

const EMPTY = { membershipNumber: '', name: '', email: '', phone: '', address: '' }

export default function MembersPage() {
  const qc = useQueryClient()
  const navigate = useNavigate()
  const { state, setState, query, setPage, toggleSort, errorMessage } = useSearchList(
    'members',
    membersApi.search,
  )

  const [importOpen, setImportOpen] = useState(false)
  const [editing, setEditing] = useState<Member | null | undefined>(undefined)
  const [form, setForm] = useState(EMPTY)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [menuFor, setMenuFor] = useState<string | null>(null)

  const openCreate = () => {
    setEditing(null)
    setForm(EMPTY)
    setFieldErrors({})
    setFormError(null)
  }
  const openEdit = (m: Member) => {
    setEditing(m)
    setForm({
      membershipNumber: m.membershipNumber,
      name: m.name,
      email: m.email,
      phone: m.phone,
      address: m.address,
    })
    setFieldErrors({})
    setFormError(null)
  }

  const save = useMutation({
    mutationFn: () =>
      editing
        ? membersApi.update(editing.id, form)
        : membersApi.create(form),
    onSuccess: () => {
      toastSuccess(editing ? t('members.updated') : t('members.added'))
      setEditing(undefined)
      void qc.invalidateQueries({ queryKey: ['members'] })
    },
    onError: (e) => {
      const n = normaliseError(e)
      setFieldErrors(n.fieldErrors)
      setFormError(Object.keys(n.fieldErrors).length ? null : n.message)
    },
  })

  const lifecycle = useMutation({
    mutationFn: ({ id, action }: { id: string; action: Action }) =>
      membersApi.lifecycle(id, action),
    onSuccess: (m) => {
      toastSuccess(t('members.nowStatus', { name: m.name, status: tStatus(m.status) }))
      void qc.invalidateQueries({ queryKey: ['members'] })
      void qc.invalidateQueries({ queryKey: ['dashboard'] })
    },
    onError: (e) => toastError(normaliseError(e).message),
  })

  const runAction = async (m: Member, action: Action) => {
    setMenuFor(null)
    const labels: Record<Action, string> = {
      suspend: t('members.confirm.suspend'),
      reactivate: t('members.confirm.reactivate'),
      renew: t('members.confirm.renew'),
      deactivate: t('members.confirm.deactivate'),
    }
    const ok = await confirmAction({
      title: labels[action],
      text: `${m.name} · ${m.membershipNumber}`,
      danger: action === 'suspend' || action === 'deactivate',
      confirmText: t(
        action === 'deactivate' ? 'members.action.markInactive' : `members.action.${action}`,
      ),
    })
    if (ok) lifecycle.mutate({ id: m.id, action })
  }

  const askDelete = async (m: Member) => {
    setMenuFor(null)
    const deleted = await cascadeDelete(
      (force) => membersApi.remove(m.id, force),
      { title: t('members.deleteTitle', { name: m.name }), entity: t('common.entity.member') },
    )
    if (deleted) {
      void qc.invalidateQueries({ queryKey: ['members'] })
      void qc.invalidateQueries({ queryKey: ['dashboard'] })
    }
  }

  const columns: Column<Member>[] = [
    {
      key: 'name',
      header: t('members.col.member'),
      sortable: true,
      render: (m) => (
        <button
          className="text-left"
          onClick={(e) => {
            e.stopPropagation()
            navigate(`/members/${m.id}`)
          }}
        >
          <p className="font-semibold text-slate-900 hover:text-brand-700">{m.name}</p>
          <p className="text-xs text-slate-400">
            {m.membershipNumber} · {m.email}
          </p>
        </button>
      ),
    },
    { key: 'status', header: t('members.col.status'), sortable: true, render: (m) => <StatusPill status={m.status} /> },
    {
      key: 'membershipExpiresAt',
      header: t('members.col.expires'),
      sortable: true,
      render: (m) => (
        <div className="text-sm">
          {formatDate(m.membershipExpiresAt)}
          <span className="ml-1 text-xs text-slate-400">({relativeExpiry(m.membershipExpiresAt)})</span>
        </div>
      ),
    },
    {
      key: 'actions',
      header: '',
      className: 'text-right',
      render: (m) => (
        <div className="relative flex justify-end">
          <Button
            variant="ghost"
            size="sm"
            onClick={(e) => {
              e.stopPropagation()
              setMenuFor(menuFor === m.id ? null : m.id)
            }}
          >
            <MoreHorizontal size={16} />
          </Button>
          {menuFor === m.id && (
            <div
              className="absolute right-0 top-9 z-10 w-44 overflow-hidden rounded-lg border border-slate-200 bg-white py-1 text-sm shadow-lg"
              onClick={(e) => e.stopPropagation()}
            >
              <MenuItem icon={<Pencil />} label={t('members.action.edit')} onClick={() => { setMenuFor(null); openEdit(m) }} />
              {m.status !== 'Suspended' && (
                <MenuItem icon={<Ban />} label={t('members.action.suspend')} onClick={() => runAction(m, 'suspend')} />
              )}
              {m.status !== 'Active' && (
                <MenuItem icon={<BadgeCheck />} label={t('members.action.reactivate')} onClick={() => runAction(m, 'reactivate')} />
              )}
              <MenuItem icon={<RefreshCw />} label={t('members.action.renew')} onClick={() => runAction(m, 'renew')} />
              {m.status === 'Active' && (
                <MenuItem icon={<UserX />} label={t('members.action.markInactive')} onClick={() => runAction(m, 'deactivate')} />
              )}
              <MenuItem icon={<Trash2 />} label={t('members.action.delete')} danger onClick={() => askDelete(m)} />
            </div>
          )}
        </div>
      ),
    },
  ]

  return (
    <>
      <PageHeader
        title={t('members.title')}
        subtitle={t('members.subtitle')}
        actions={
          <>
            <Button variant="secondary" onClick={() => setImportOpen(true)}>
              <Upload size={16} /> {t('common.bulkImport')}
            </Button>
            <Button onClick={openCreate}>
              <Plus size={16} /> {t('members.add')}
            </Button>
          </>
        }
      />

      <div className="space-y-4" onClick={() => setMenuFor(null)}>
        <AdvancedSearch fields={FIELDS} state={state} onChange={setState} />
        <DataTable
          columns={columns}
          rows={query.data?.items ?? []}
          rowKey={(m) => m.id}
          loading={query.isLoading}
          error={errorMessage}
          onRetry={() => void query.refetch()}
          emptyTitle={t('members.emptyTitle')}
          sort={state.sort}
          onSortChange={toggleSort}
        />
        <Pagination page={state.page} onPageChange={setPage} data={query.data} />
      </div>

      <BulkImportModal
        open={importOpen}
        onClose={() => setImportOpen(false)}
        title={t('members.importTitle')}
        templateUrl={membersApi.importTemplateUrl}
        onImport={membersApi.import}
        onDone={() => void qc.invalidateQueries({ queryKey: ['members'] })}
      />

      <Modal
        open={editing !== undefined}
        onClose={() => setEditing(undefined)}
        title={editing ? t('members.edit') : t('members.add')}
      >
        <form
          className="space-y-3"
          onSubmit={(e) => {
            e.preventDefault()
            save.mutate()
          }}
        >
          {formError && (
            <div className="rounded-lg border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700">
              {formError}
            </div>
          )}
          <FormField label={t('members.field.number')} error={fieldErrors.membershipNumber}>
            <TextInput
              value={form.membershipNumber}
              invalid={!!fieldErrors.membershipNumber}
              onChange={(e) => setForm({ ...form, membershipNumber: e.target.value })}
            />
          </FormField>
          <FormField label={t('members.field.name')} error={fieldErrors.name}>
            <TextInput
              value={form.name}
              invalid={!!fieldErrors.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
            />
          </FormField>
          <FormField label={t('members.field.email')} error={fieldErrors.email}>
            <TextInput
              value={form.email}
              invalid={!!fieldErrors.email}
              onChange={(e) => setForm({ ...form, email: e.target.value })}
            />
          </FormField>
          <div className="grid grid-cols-2 gap-3">
            <FormField label={t('members.field.phone')} error={fieldErrors.phone}>
              <TextInput
                value={form.phone}
                invalid={!!fieldErrors.phone}
                onChange={(e) => setForm({ ...form, phone: e.target.value })}
              />
            </FormField>
            <FormField label={t('members.field.address')} error={fieldErrors.address}>
              <TextInput
                value={form.address}
                invalid={!!fieldErrors.address}
                onChange={(e) => setForm({ ...form, address: e.target.value })}
              />
            </FormField>
          </div>
          <div className="flex justify-end gap-2 pt-1">
            <Button type="button" variant="secondary" onClick={() => setEditing(undefined)}>
              {t('common.cancel')}
            </Button>
            <Button type="submit" disabled={save.isPending}>
              {save.isPending ? t('common.saving') : editing ? t('common.saveChanges') : t('members.add')}
            </Button>
          </div>
        </form>
      </Modal>
    </>
  )
}

function MenuItem({
  icon,
  label,
  onClick,
  danger,
}: {
  icon: ReactNode
  label: string
  onClick: () => void
  danger?: boolean
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`flex w-full items-center gap-2 px-3 py-1.5 text-left hover:bg-slate-50 ${
        danger ? 'text-rose-600' : 'text-slate-700'
      }`}
    >
      <span className="[&>svg]:h-3.5 [&>svg]:w-3.5">{icon}</span>
      {label}
    </button>
  )
}
