import { Bell, MessageSquareWarning, Plus } from 'lucide-react'
import { Link } from 'react-router-dom'

import Button from '../components/Button.jsx'
import { useAuth } from '../hooks/useAuth.js'
import { roleLabel } from '../utils/enums.js'

export default function DashboardPage() {
  const { user } = useAuth()

  return (
    <div className="space-y-6">
      <div className="surface-solid p-6 sm:p-8">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <div className="text-sm font-semibold text-cyan-800">Dashboard</div>
            <div className="mt-2 text-3xl font-bold tracking-tight text-slate-900">
              {user?.firstName ? `Welcome, ${user.firstName}` : 'Welcome'}
            </div>
            <div className="mt-2 text-sm text-slate-600">
              <span className="font-semibold text-slate-800">Email:</span> {user?.email || '—'}
              {user?.role ? (
                <>
                  {' '}
                  <span className="mx-2 text-slate-300">|</span>
                  <span className="font-semibold text-slate-800">Role:</span> {roleLabel(user.role)}
                </>
              ) : null}
            </div>
          </div>

          <div className="flex flex-col gap-2 sm:flex-row">
            <Link to="/app/reclamations/new">
              <Button>
                <Plus className="h-4 w-4" aria-hidden="true" />
                New reclamation
              </Button>
            </Link>
            <Link to="/app/notifications">
              <Button variant="secondary">
                <Bell className="h-4 w-4" aria-hidden="true" />
                Notifications
              </Button>
            </Link>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <div className="surface-solid p-6">
          <div className="flex items-center gap-3">
            <div className="grid h-10 w-10 place-items-center rounded-2xl bg-slate-100 text-slate-700">
              <MessageSquareWarning className="h-5 w-5" aria-hidden="true" />
            </div>
            <div>
              <div className="text-lg font-bold text-slate-900">Reclamations</div>
              <div className="text-sm text-slate-600">Create and track customer complaints.</div>
            </div>
          </div>
          <div className="mt-4">
            <Link className="link font-semibold" to="/app/reclamations">
              Go to reclamations
            </Link>
          </div>
        </div>

        <div className="surface-solid p-6">
          <div className="flex items-center gap-3">
            <div className="grid h-10 w-10 place-items-center rounded-2xl bg-slate-100 text-slate-700">
              <Bell className="h-5 w-5" aria-hidden="true" />
            </div>
            <div>
              <div className="text-lg font-bold text-slate-900">Notifications</div>
              <div className="text-sm text-slate-600">Event-driven updates from the system.</div>
            </div>
          </div>
          <div className="mt-4">
            <Link className="link font-semibold" to="/app/notifications">
              View notifications
            </Link>
          </div>
        </div>
      </div>
    </div>
  )
}
