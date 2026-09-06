import axios, { AxiosError } from 'axios'
import Swal from 'sweetalert2'
import type { ApiErrorResponse, NormalisedError } from './types'

const SUPPORT_MESSAGE =
  'Something went wrong. Please contact service provider MD. IKRAMUL ISLAM SIDDIQUE POROSH, phone: +8801672896992 for details.'

export const API_BASE_URL: string =
  (import.meta.env.VITE_API_BASE_URL as string | undefined)?.replace(/\/$/, '') ??
  'http://localhost:5254/api'

export const http = axios.create({
  baseURL: API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
})

/** Turns any axios failure into a predictable, field-aware shape. */
export function normaliseError(error: unknown): NormalisedError {
  if (axios.isAxiosError(error)) {
    const err = error as AxiosError<ApiErrorResponse | { detail?: string; title?: string }>
    const status = err.response?.status
    const data = err.response?.data

    if (data && typeof data === 'object' && 'errors' in data && Array.isArray(data.errors)) {
      const body = data as ApiErrorResponse
      const fieldErrors: Record<string, string> = {}
      for (const e of body.errors) {
        if (e.field && e.line == null) fieldErrors[e.field] = e.errorMessage
      }
      return {
        message: body.errors[0]?.errorMessage ?? 'Request failed.',
        errors: body.errors,
        fieldErrors,
        rowErrors: body.errors.filter((e) => e.line != null),
        traceId: body.correlationId,
        status,
      }
    }

    const problem = data as { detail?: string; title?: string } | undefined
    const message =
      problem?.detail ??
      problem?.title ??
      (status && status >= 500 ? SUPPORT_MESSAGE : err.message)

    return { message, errors: [], fieldErrors: {}, rowErrors: [], status }
  }

  return { message: SUPPORT_MESSAGE, errors: [], fieldErrors: {}, rowErrors: [] }
}

/** Show a blocking error alert for unexpected / server failures. */
http.interceptors.response.use(
  (r) => r,
  (error: unknown) => {
    const n = normaliseError(error)
    if (n.status && n.status >= 500) {
      void Swal.fire({ icon: 'error', title: 'Unexpected error', text: n.message })
    }
    return Promise.reject(error)
  },
)

// --- SweetAlert helpers ---------------------------------------------------

export const toast = Swal.mixin({
  toast: true,
  position: 'top-end',
  showConfirmButton: false,
  timer: 2600,
  timerProgressBar: true,
})

export function toastSuccess(title: string) {
  void toast.fire({ icon: 'success', title })
}

export function toastError(title: string) {
  void toast.fire({ icon: 'error', title })
}

export async function confirmAction(options: {
  title: string
  text?: string
  confirmText?: string
  danger?: boolean
}): Promise<boolean> {
  const result = await Swal.fire({
    icon: options.danger ? 'warning' : 'question',
    title: options.title,
    text: options.text,
    showCancelButton: true,
    confirmButtonText: options.confirmText ?? 'Confirm',
    confirmButtonColor: options.danger ? '#dc2626' : '#4f46e5',
    reverseButtons: true,
  })
  return result.isConfirmed
}

/**
 * Runs a delete that supports smart cascade:
 *  - first attempt without force;
 *  - if the API asks to confirm dependent data (code ends `_HAS_DEPENDENT_COPIES`
 *    or `_HAS_BORROW_HISTORY`), shows the API's message and retries with force;
 *  - if the API blocks the delete (code ends `_HAS_BORROWED_COPIES` /
 *    `_HAS_ACTIVE_BORROW`), shows a blocking alert and stops.
 * Returns true when something was actually deleted.
 */
export async function cascadeDelete(
  run: (force: boolean) => Promise<unknown>,
  opts: { title: string; entity: string },
): Promise<boolean> {
  const confirmed = await confirmAction({
    title: opts.title,
    text: `The ${opts.entity} will be soft-deleted (recoverable). Continue?`,
    danger: true,
    confirmText: 'Delete',
  })
  if (!confirmed) return false

  try {
    await run(false)
    toastSuccess(`${opts.entity[0].toUpperCase() + opts.entity.slice(1)} deleted`)
    return true
  } catch (err) {
    const n = normaliseError(err)
    const code = n.errors[0]?.errorCode ?? ''

    if (/_HAS_BORROWED_COPIES$|_HAS_ACTIVE_BORROW$/.test(code)) {
      await Swal.fire({ icon: 'error', title: 'Cannot delete', text: n.message })
      return false
    }

    if (/_HAS_DEPENDENT_COPIES$|_HAS_BORROW_HISTORY$/.test(code)) {
      const go = await Swal.fire({
        icon: 'warning',
        title: 'Dependent data exists',
        text: n.message,
        showCancelButton: true,
        confirmButtonText: 'Delete everything',
        confirmButtonColor: '#dc2626',
        reverseButtons: true,
      })
      if (!go.isConfirmed) return false
      try {
        await run(true)
        toastSuccess(`${opts.entity[0].toUpperCase() + opts.entity.slice(1)} and related data deleted`)
        return true
      } catch (err2) {
        toastError(normaliseError(err2).message)
        return false
      }
    }

    toastError(n.message)
    return false
  }
}
