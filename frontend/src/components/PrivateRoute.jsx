import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth.js'
import Spinner from './Spinner.jsx'

export default function PrivateRoute() {
  const { authenticated, isBootstrapped } = useAuth()
  const location = useLocation()

  if (!isBootstrapped) {
    return (
      <div className="min-h-screen grid place-items-center">
        <Spinner label="Loading session..." />
      </div>
    )
  }

  if (!authenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }

  return <Outlet />
}
