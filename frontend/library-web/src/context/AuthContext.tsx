import { useCallback, useEffect, useState, type ReactNode } from 'react'
import { authApi } from '@/api'
import { AUTH_LOGOUT_EVENT, getStoredAuth, setStoredAuth, type StoredAuth } from '@/lib/api'
import { AuthContext } from './authContextValue'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [auth, setAuth] = useState<StoredAuth | null>(() => getStoredAuth())

  useEffect(() => {
    const onLogout = () => setAuth(null)
    window.addEventListener(AUTH_LOGOUT_EVENT, onLogout)
    return () => window.removeEventListener(AUTH_LOGOUT_EVENT, onLogout)
  }, [])

  const login = useCallback(async (usernameOrEmail: string, password: string) => {
    const res = await authApi.login(usernameOrEmail, password)
    const next: StoredAuth = {
      accessToken: res.accessToken,
      role: res.role,
      username: res.username,
      memberId: res.memberId,
    }
    setStoredAuth(next)
    setAuth(next)
  }, [])

  const register = useCallback(
    async (body: { name: string; email: string; password: string; phone?: string; address?: string }) => {
      const res = await authApi.register(body)
      const next: StoredAuth = {
        accessToken: res.accessToken,
        role: res.role,
        username: res.username,
        memberId: res.memberId,
      }
      setStoredAuth(next)
      setAuth(next)
    },
    [],
  )

  const logout = useCallback(() => {
    setStoredAuth(null)
    setAuth(null)
  }, [])

  return (
    <AuthContext.Provider
      value={{
        auth,
        isLibrarian: auth?.role === 'Librarian',
        isMember: auth?.role === 'Member',
        login,
        register,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}
