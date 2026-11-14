'use client'

import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate } from '@/lib/router'
import { useAuth } from '@/context/authContextValue'
import { AuthFrame } from '@/components/AuthFrame'
import { Button, TextInput } from '@/components/ui'
import { normaliseError } from '@/lib/api'
import { t } from '@/lib/i18n'

export default function RegisterPage() {
  const { auth, register } = useAuth()
  const navigate = useNavigate()
  useEffect(() => {
    if (auth) navigate('/', { replace: true })
  }, [auth, navigate])
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [phone, setPhone] = useState('')
  const [address, setAddress] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setBusy(true)
    try {
      await register({ name, email, password, phone, address })
    } catch (err) {
      setError(normaliseError(err).message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <AuthFrame title={t('auth.registerTitle')} subtitle={t('auth.registerSubtitle')}>
        <form className="space-y-4" onSubmit={onSubmit}>
          <label className="block">
            <span className="mb-1 block text-sm font-medium text-slate-700">{t('auth.name')}</span>
            <TextInput autoFocus value={name} onChange={(e) => setName(e.target.value)} required />
          </label>

          <label className="block">
            <span className="mb-1 block text-sm font-medium text-slate-700">{t('auth.email')}</span>
            <TextInput type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
          </label>

          <label className="block">
            <span className="mb-1 block text-sm font-medium text-slate-700">{t('auth.phone')}</span>
            <TextInput value={phone} onChange={(e) => setPhone(e.target.value)} />
          </label>

          <label className="block">
            <span className="mb-1 block text-sm font-medium text-slate-700">{t('auth.address')}</span>
            <TextInput value={address} onChange={(e) => setAddress(e.target.value)} />
          </label>

          <label className="block">
            <span className="mb-1 block text-sm font-medium text-slate-700">{t('auth.password')}</span>
            <TextInput
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              minLength={8}
            />
          </label>

          {error && <p className="text-sm font-medium text-rose-600">{error}</p>}

          <Button type="submit" className="w-full justify-center" disabled={busy}>
            {busy ? t('auth.registering') : t('auth.register')}
          </Button>
        </form>

        <p className="mt-6 text-center text-sm text-slate-500">
          {t('auth.haveAccount')}{' '}
          <Link to="/login" className="font-semibold text-brand-600 hover:underline">
            {t('auth.signInLink')}
          </Link>
        </p>
    </AuthFrame>
  )
}
