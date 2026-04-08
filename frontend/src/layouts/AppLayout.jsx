import { Bell, LayoutDashboard, LogOut, MessageSquareWarning } from 'lucide-react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import clsx from 'clsx'

import Button from '../components/Button.jsx'
import { useAuth } from '../hooks/useAuth.js'
import { roleLabel } from '../utils/enums.js'

function Item({ to, icon, children }) {
  const Icon = icon

  return (
    <NavLink
      to={to}
      className={({ isActive }) =>
        clsx(
          'flex items-center gap-3 rounded-xl px-3 py-2 text-sm font-semibold transition',
          isActive
            ? 'bg-cyan-700 text-white shadow-sm'
            : 'text-slate-700 hover:bg-slate-100 hover:text-slate-900',
        )
      }
    >
      <Icon className="h-4 w-4" aria-hidden="true" />
      {children}
    </NavLink>
  )
}

export default function AppLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  return (
    <div className="min-h-screen">
      <div className="mx-auto max-w-7xl px-4 py-6 sm:px-6 lg:px-8">
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-[280px_1fr]">
          <aside className="surface-solid p-4">
            <div className="flex items-center justify-between">
              <div>
                <div className="text-sm font-semibold text-cyan-800">PFE.NET</div>
                <div className="text-lg font-bold text-slate-900">Portal</div>
              </div>
            </div>

            <div className="mt-4 rounded-xl bg-slate-50 p-3">
              <div className="text-sm font-semibold text-slate-900">
                {user?.firstName || user?.lastName
                  ? `${user?.firstName ?? ''} ${user?.lastName ?? ''}`.trim()
                  : user?.email ?? 'User'}
              </div>
              <div className="mt-1 text-xs font-medium text-slate-600">
                {user?.role ? roleLabel(user.role) : 'Authenticated'}
              </div>
            </div>

            <nav className="mt-4 space-y-1">
              <Item to="/app" icon={LayoutDashboard}>
                Dashboard
              </Item>
              <Item to="/app/reclamations" icon={MessageSquareWarning}>
                Reclamations
              </Item>
              <Item to="/app/notifications" icon={Bell}>
                Notifications
              </Item>
            </nav>

            <div className="mt-6">
              <Button
                variant="secondary"
                className="w-full justify-center"
                onClick={() => {
                  logout()
                  navigate('/login')
                }}
              >
                <LogOut className="h-4 w-4" aria-hidden="true" />
                Sign out
              </Button>
            </div>
          </aside>

          <main className="space-y-6">
            <Outlet />
          </main>
        </div>
      </div>
    </div>
  )
}
