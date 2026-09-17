import { Library } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '@/context/authContextValue'
import { Button, TextInput } from '@/components/ui'
import { normaliseError } from '@/lib/api'
import { t } from '@/lib/i18n'

export default function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [usernameOrEmail, setUsernameOrEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setBusy(true)
    try {
      await login(usernameOrEmail, password)
      const from = (location.state as { from?: string } | null)?.from ?? '/'
      navigate(from, { replace: true })
    } catch (err) {
      setError(normaliseError(err).message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-100 px-4">
      <div className="w-full max-w-sm rounded-2xl border border-slate-200 bg-white p-8 shadow-sm">
        <div className="mb-6 flex flex-col items-center gap-2 text-center">
          <span className="flex h-11 w-11 items-center justify-center rounded-xl bg-brand-600 text-white">
            <Library size={22} />
          </span>
          <h1 className="text-lg font-bold text-slate-900">{t('auth.loginTitle')}</h1>
          <p className="text-sm text-slate-500">{t('auth.loginSubtitle')}</p>
        </div>

        <form className="space-y-4" onSubmit={onSubmit}>
          <label className="block">
            <span className="mb-1 block text-sm font-medium text-slate-700">
              {t('auth.usernameOrEmail')}
            </span>
            <TextInput
              autoFocus
              value={usernameOrEmail}
              onChange={(e) => setUsernameOrEmail(e.target.value)}
              required
            />
          </label>

          <label className="block">
            <span className="mb-1 block text-sm font-medium text-slate-700">
              {t('auth.password')}
            </span>
            <TextInput
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
          </label>

          {error && <p className="text-sm font-medium text-rose-600">{error}</p>}

          <Button type="submit" className="w-full justify-center" disabled={busy}>
            {busy ? t('auth.signingIn') : t('auth.signIn')}
          </Button>
        </form>

        <p className="mt-6 text-center text-sm text-slate-500">
          {t('auth.noAccount')}{' '}
          <Link to="/register" className="font-semibold text-brand-600 hover:underline">
            {t('auth.registerLink')}
          </Link>
        </p>
      </div>
    </div>
  )
}
