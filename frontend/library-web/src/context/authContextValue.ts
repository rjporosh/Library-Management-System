import { createContext, useContext } from 'react'
import type { StoredAuth } from '@/lib/api'

export interface AuthContextValue {
  auth: StoredAuth | null
  isLibrarian: boolean
  isMember: boolean
  login: (usernameOrEmail: string, password: string) => Promise<void>
  register: (body: { name: string; email: string; password: string; phone?: string; address?: string }) => Promise<void>
  logout: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within an AuthProvider')
  return ctx
}
