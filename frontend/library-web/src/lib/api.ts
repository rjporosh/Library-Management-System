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
