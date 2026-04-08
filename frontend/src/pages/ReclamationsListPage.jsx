import { Plus, RefreshCw } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import toast from 'react-hot-toast'

import Badge from '../components/Badge.jsx'
import Button from '../components/Button.jsx'
import EmptyState from '../components/EmptyState.jsx'
import Spinner from '../components/Spinner.jsx'
import TextField from '../components/TextField.jsx'
import { useAuth } from '../hooks/useAuth.js'
import { queryReclamations } from '../services/reclamations.service.js'
import { formatDateTime } from '../utils/format.js'
import {
  priorityBadgeClasses,
  priorityLabel,
  reclamationStatusBadgeClasses,
  reclamationStatusLabel,
} from '../utils/enums.js'

const PAGE_SIZE = 10

export default function ReclamationsListPage() {
  const { user } = useAuth()
  const role = String(user?.role ?? '').toUpperCase()
  const canCreate = role === 'CLIENT' || role === 'ADMIN'

  const [items, setItems] = useState([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [query, setQuery] = useState('')

  async function load({ currentPage = page, currentQuery = query } = {}) {
    setLoading(true)
    setError(null)
    try {
      const data = await queryReclamations({
        page: currentPage,
        pageSize: PAGE_SIZE,
        search: currentQuery?.trim() || undefined,
      })

      setItems(Array.isArray(data?.items) ? data.items : [])
      setTotalCount(Number(data?.totalCount ?? 0))
    } catch (err) {
      const message = err?.response?.data?.detail || err?.message
      setError(message || 'Failed to load reclamations.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    load({ currentPage: page, currentQuery: query })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page])

  const totalPages = useMemo(() => Math.max(1, Math.ceil(totalCount / PAGE_SIZE)), [totalCount])

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
                load({ currentPage: page, currentQuery: query })
              }}
              disabled={loading}
            >
              <RefreshCw className="h-4 w-4" aria-hidden="true" />
              Refresh
            </Button>
            {canCreate ? (
              <Link to="/app/reclamations/new">
                <Button>
                  <Plus className="h-4 w-4" aria-hidden="true" />
                  New reclamation
                </Button>
              </Link>
            ) : null}
          </div>
        </div>

        <div className="mt-4 flex flex-col gap-3 sm:flex-row sm:items-end">
          <div className="w-full sm:max-w-lg">
            <TextField
              label="Search"
              placeholder="Reference, status, client name..."
              value={query}
              onChange={(e) => setQuery(e.target.value)}
            />
          </div>
          <Button
            variant="secondary"
            onClick={() => {
              setPage(1)
              load({ currentPage: 1, currentQuery: query })
            }}
            disabled={loading}
          >
            Apply filters
          </Button>
        </div>
      </div>

      {loading ? (
        <div className="surface-solid p-8">
          <Spinner label="Loading reclamations..." />
        </div>
      ) : error ? (
        <div className="surface-solid p-6">
          <div className="rounded-xl border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-800">{error}</div>
        </div>
      ) : items.length === 0 ? (
        <EmptyState
          title="No reclamations"
          description={canCreate ? 'Create your first complaint to get started.' : 'No reclamations found.'}
          actionLabel={canCreate ? 'Create reclamation' : undefined}
          onAction={canCreate ? () => (window.location.href = '/app/reclamations/new') : undefined}
        />
      ) : (
        <>
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
                  {items.map((x) => (
                    <tr key={x.id} className="hover:bg-slate-50/60">
                      <td className="px-4 py-3 text-sm font-semibold text-slate-900">{x.reference}</td>
                      <td className="px-4 py-3 text-sm">
                        <Badge className={priorityBadgeClasses(x.priority)}>{priorityLabel(x.priority)}</Badge>
                      </td>
                      <td className="px-4 py-3 text-sm">
                        <Badge className={reclamationStatusBadgeClasses(x.status)}>
                          {reclamationStatusLabel(x.status)}
                        </Badge>
                      </td>
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

          <div className="surface-solid p-4">
            <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div className="text-sm text-slate-600">
                Showing page {page} of {totalPages} ({totalCount} total)
              </div>
              <div className="flex gap-2">
                <Button variant="secondary" disabled={page <= 1 || loading} onClick={() => setPage((p) => p - 1)}>
                  Previous
                </Button>
                <Button
                  variant="secondary"
                  disabled={page >= totalPages || loading}
                  onClick={() => setPage((p) => p + 1)}
                >
                  Next
                </Button>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  )
}
