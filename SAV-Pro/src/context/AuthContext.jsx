import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { login as loginApi, logout as logoutApi, me as meApi } from '../api/authApi';
import { setUnauthorizedHandler } from '../api/apiClient';
import { getFriendlyApiError } from '../utils/errorMessages';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null);
  const [initializing, setInitializing] = useState(true);
  const [authError, setAuthError] = useState('');
  const navigate = useNavigate();

  const clearSession = useCallback(() => {
    setUser(null);
    navigate('/login', { replace: true });
  }, [navigate]);

  useEffect(() => {
    setUnauthorizedHandler(clearSession);
    return () => setUnauthorizedHandler(null);
  }, [clearSession]);

  const refreshSession = useCallback(async () => {
    setInitializing(true);
    setAuthError('');
    try {
      const currentUser = await meApi();
      setUser(currentUser?.id ? currentUser : null);
    } catch (error) {
      setUser(null);
      if (error.status && error.status !== 401) {
        setAuthError(getFriendlyApiError(error));
      }
    } finally {
      setInitializing(false);
    }
  }, []);

  useEffect(() => {
    refreshSession();
  }, [refreshSession]);

  const login = useCallback(async (email, password) => {
    setAuthError('');
    const signedInUser = await loginApi(email, password);
    setUser(signedInUser);
    return signedInUser;
  }, []);

  const logout = useCallback(async () => {
    try {
      await logoutApi();
    } finally {
      setUser(null);
      navigate('/login', { replace: true });
    }
  }, [navigate]);

  const value = useMemo(() => ({
    user,
    initializing,
    authError,
    login,
    logout,
    refreshSession,
    clearSession,
    isAuthenticated: Boolean(user)
  }), [user, initializing, authError, login, logout, refreshSession, clearSession]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used inside AuthProvider');
  }
  return context;
}
