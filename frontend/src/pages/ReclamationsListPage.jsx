import { Plus, RefreshCw } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import toast from 'react-hot-toast'

import Badge from '../components/Badge.jsx'
import Button from '../components/Button.jsx'
import EmptyState from '../components/EmptyState.jsx'
import Spinner from '../components/Spinner.jsx'
import TextField from '../components/TextField.jsx'
import { listReclamations } from '../services/reclamations.service.js'
import { formatDateTime } from '../utils/format.js'
import { priorityBadgeClasses, priorityLabel } from '../utils/enums.js'

export default function ReclamationsListPage() {
  const [items, setItems] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [query, setQuery] = useState('')

  async function load() {
    setLoading(true)
    setError(null)
    try {
      const data = await listReclamations()
      setItems(Array.isArray(data) ? data : [])
    } catch (err) {
      const message = err?.response?.data?.detail || err?.message
      setError(message || 'Failed to load reclamations.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    load()
  }, [])

  const filtered = useMemo(() => {
    if (!query.trim()) return items
    const q = query.trim().toLowerCase()
    return items.filter((x) =>
      [x.reference, x.status, x.clientName].some((v) => String(v ?? '').toLowerCase().includes(q)),
    )
  }, [items, query])

  return (
    <div className="space-y-6">
      <div className="surface-solid p-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <div className="text-sm font-semibold text-cyan-800">Reclamations</div>
            <div className="mt-1 text-2xl font-bold tracking-tight text-slate-900">All complaints</div>
            <div className="mt-1 text-sm text-slate-600">Create, track and update reclamations.</div>
          </div>

          <div className="flex flex-col gap-2 sm:flex-row">
            <Button
              variant="secondary"
              onClick={() => {
                toast.dismiss()
                load()
              }}
              disabled={loading}
            >
              <RefreshCw className="h-4 w-4" aria-hidden="true" />
              Refresh
            </Button>
            <Link to="/app/reclamations/new">
              <Button>
                <Plus className="h-4 w-4" aria-hidden="true" />
                New reclamation
              </Button>
            </Link>
          </div>
        </div>

        <div className="mt-4">
          <TextField
            label="Search"
            placeholder="Reference, status, client name..."
            value={query}
            onChange={(e) => setQuery(e.target.value)}
          />
        </div>
      </div>

      {loading ? (
        <div className="surface-solid p-8">
          <Spinner label="Loading reclamations..." />
        </div>
      ) : error ? (
        <div className="surface-solid p-6">
          <div className="rounded-xl border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-800">
            {error}
          </div>
        </div>
      ) : filtered.length === 0 ? (
        <EmptyState
          title="No reclamations"
          description="Create your first complaint to get started."
          actionLabel="Create reclamation"
          onAction={() => (window.location.href = '/app/reclamations/new')}
        />
      ) : (
        <div className="surface-solid overflow-hidden">
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-200">
              <thead className="bg-slate-50">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-600">
                    Reference
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-600">
                    Priority
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-600">
                    Status
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-600">
                    Client
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-600">
                    Created
                  </th>
                  <th className="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wider text-slate-600">
                    Action
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200 bg-white">
                {filtered.map((x) => (
                  <tr key={x.id} className="hover:bg-slate-50/60">
                    <td className="px-4 py-3 text-sm font-semibold text-slate-900">{x.reference}</td>
                    <td className="px-4 py-3 text-sm">
                      <Badge className={priorityBadgeClasses(x.priority)}>
                        {priorityLabel(x.priority)}
                      </Badge>
                    </td>
                    <td className="px-4 py-3 text-sm text-slate-700">{x.status}</td>
                    <td className="px-4 py-3 text-sm text-slate-700">{x.clientName}</td>
                    <td className="px-4 py-3 text-sm text-slate-700">{formatDateTime(x.createdAt)}</td>
                    <td className="px-4 py-3 text-right text-sm">
                      <Link className="link font-semibold" to={`/app/reclamations/${x.id}`}>
                        View
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  )
}
