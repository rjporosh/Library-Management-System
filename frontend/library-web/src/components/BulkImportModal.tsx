import { clsx } from 'clsx'
import { Download, FileSpreadsheet, UploadCloud } from 'lucide-react'
import { useRef, useState } from 'react'
import { normaliseError, toastSuccess } from '@/lib/api'
import type { ApiError } from '@/lib/types'
import { Button, Modal } from './ui'

interface Props {
  open: boolean
  onClose: () => void
  title: string
  templateUrl: string
  onImport: (file: File) => Promise<{ imported: number }>
  onDone: () => void
}

export function BulkImportModal({
  open,
  onClose,
  title,
  templateUrl,
  onImport,
  onDone,
}: Props) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [file, setFile] = useState<File | null>(null)
  const [dragging, setDragging] = useState(false)
  const [busy, setBusy] = useState(false)
  const [rowErrors, setRowErrors] = useState<ApiError[]>([])
  const [message, setMessage] = useState<string | null>(null)

  const reset = () => {
    setFile(null)
    setRowErrors([])
    setMessage(null)
    setBusy(false)
  }

  const close = () => {
    reset()
    onClose()
  }

  const submit = async () => {
    if (!file) return
    setBusy(true)
    setRowErrors([])
    setMessage(null)
    try {
      const result = await onImport(file)
      toastSuccess(`Imported ${result.imported} row${result.imported === 1 ? '' : 's'}`)
      onDone()
      close()
    } catch (err) {
      const n = normaliseError(err)
      setMessage(n.message)
      setRowErrors(n.errors.length ? n.errors : [])
    } finally {
      setBusy(false)
    }
  }

  return (
    <Modal open={open} onClose={close} title={title} wide>
      <div className="space-y-4">
        <div className="flex items-center justify-between rounded-lg bg-slate-50 px-3 py-2 text-sm">
          <span className="text-slate-600">
            Not sure about the format? Start from the template.
          </span>
          <a
            href={templateUrl}
            className="inline-flex items-center gap-1.5 font-semibold text-brand-600 hover:text-brand-700"
          >
            <Download size={15} /> Download template
          </a>
        </div>

        <button
          type="button"
          onClick={() => inputRef.current?.click()}
          onDragOver={(e) => {
            e.preventDefault()
            setDragging(true)
          }}
          onDragLeave={() => setDragging(false)}
          onDrop={(e) => {
            e.preventDefault()
            setDragging(false)
            const dropped = e.dataTransfer.files[0]
            if (dropped) setFile(dropped)
          }}
          className={clsx(
            'flex w-full flex-col items-center gap-2 rounded-xl border-2 border-dashed px-4 py-10 text-sm transition',
            dragging
              ? 'border-brand-500 bg-brand-50'
              : 'border-slate-300 bg-white hover:border-brand-400 hover:bg-slate-50',
          )}
        >
          {file ? (
            <>
              <FileSpreadsheet size={28} className="text-emerald-600" />
              <span className="font-semibold text-slate-700">{file.name}</span>
              <span className="text-xs text-slate-400">
                {(file.size / 1024).toFixed(1)} KB · click to choose another
              </span>
            </>
          ) : (
            <>
              <UploadCloud size={28} className="text-slate-400" />
              <span className="font-semibold text-slate-600">
                Drop an .xlsx file here, or click to browse
              </span>
              <span className="text-xs text-slate-400">All-or-nothing: one bad row rolls back the whole file</span>
            </>
          )}
          <input
            ref={inputRef}
            type="file"
            accept=".xlsx"
            className="hidden"
            onChange={(e) => setFile(e.target.files?.[0] ?? null)}
          />
        </button>

        {message && (
          <div className="rounded-lg border border-rose-200 bg-rose-50 px-3 py-2 text-sm font-medium text-rose-700">
            {message}
          </div>
        )}

        {rowErrors.length > 0 && (
          <div className="max-h-64 overflow-y-auto rounded-lg border border-slate-200">
            <table className="min-w-full divide-y divide-slate-200 text-xs">
              <thead className="bg-slate-50 text-slate-500">
                <tr>
                  <th className="px-3 py-2 text-left font-semibold">Row</th>
                  <th className="px-3 py-2 text-left font-semibold">Field</th>
                  <th className="px-3 py-2 text-left font-semibold">Problem</th>
                  <th className="px-3 py-2 text-left font-semibold">Accepted</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {rowErrors.map((e, i) => (
                  <tr key={i}>
                    <td className="px-3 py-1.5 font-mono text-slate-500">{e.line ?? '—'}</td>
                    <td className="px-3 py-1.5 font-medium text-slate-700">{e.field ?? '—'}</td>
                    <td className="px-3 py-1.5 text-rose-700">{e.errorMessage}</td>
                    <td className="px-3 py-1.5 text-slate-400">{e.supportedValues ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <div className="flex justify-end gap-2">
          <Button variant="secondary" onClick={close} disabled={busy}>
            Cancel
          </Button>
          <Button onClick={submit} disabled={!file || busy}>
            {busy ? 'Importing…' : 'Import'}
          </Button>
        </div>
      </div>
    </Modal>
  )
}
