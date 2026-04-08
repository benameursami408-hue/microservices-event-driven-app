/* eslint-disable react-refresh/only-export-components, react-hooks/set-state-in-effect */
import { createContext, useCallback, useEffect, useMemo, useState } from 'react'
import toast from 'react-hot-toast'

import { login as loginRequest, register as registerRequest } from '../services/auth.service.js'
import { isTokenExpired, parseUserFromToken } from '../utils/jwt.js'

const TOKEN_KEY = 'auth_token'

export const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [token, setToken] = useState(null)
  const [user, setUser] = useState(null)
  const [isBootstrapped, setIsBootstrapped] = useState(false)

  const logout = useCallback((opts = {}) => {
    localStorage.removeItem(TOKEN_KEY)
    setToken(null)
    setUser(null)

    if (!opts.silent) toast('You are signed out.')
  }, [])

  useEffect(() => {
    const stored = localStorage.getItem(TOKEN_KEY)
    if (stored && !isTokenExpired(stored)) {
      setToken(stored)
      setUser(parseUserFromToken(stored))
    } else if (stored) {
      localStorage.removeItem(TOKEN_KEY)
    }

    setIsBootstrapped(true)
  }, [])

  useEffect(() => {
    function onForceLogout() {
      logout({ silent: true })
      toast.error('Session expired. Please sign in again.')
    }

    window.addEventListener('auth:logout', onForceLogout)
    return () => window.removeEventListener('auth:logout', onForceLogout)
  }, [logout])

  const login = useCallback(async ({ email, password }) => {
    const data = await loginRequest({ email, password })
    const nextToken = data?.token

    if (!nextToken) throw new Error('Login succeeded but no token was returned.')

    localStorage.setItem(TOKEN_KEY, nextToken)
    setToken(nextToken)
    setUser(parseUserFromToken(nextToken))

    toast.success('Welcome back!')
    return data
  }, [])

  const register = useCallback(async (payload) => {
    const data = await registerRequest(payload)
    toast.success('Account created. You can now sign in.')
    return data
  }, [])

  const value = useMemo(() => {
    const authenticated = !!token && !isTokenExpired(token)

    return {
      token,
      user,
      authenticated,
      isBootstrapped,
      login,
      register,
      logout,
    }
  }, [token, user, isBootstrapped, login, register, logout])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
