'use client'

import { useEffect, useState, type FormEvent } from 'react'
import { useSearchParams } from 'next/navigation'
import { Link, useNavigate } from '@/lib/router'
import { useAuth } from '@/context/authContextValue'
import { AuthFrame } from '@/components/AuthFrame'
import { Button, TextInput } from '@/components/ui'
import { normaliseError } from '@/lib/api'
import { t } from '@/lib/i18n'

export default function LoginPage() {
  const { auth, login } = useAuth()
  const navigate = useNavigate()
  const searchParams = useSearchParams()
  const requested = searchParams.get('from')
  const from = requested?.startsWith('/') && !requested.startsWith('//') ? requested : '/'
  useEffect(() => {
    if (auth) navigate(from, { replace: true })
  }, [auth, from, navigate])
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
    } catch (err) {
      setError(normaliseError(err).message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <AuthFrame title={t('auth.loginTitle')} subtitle={t('auth.loginSubtitle')}>
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
    </AuthFrame>
  )
}
