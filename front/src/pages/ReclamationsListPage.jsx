import { Filter, Plus, RefreshCw, Search, ShieldAlert } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'

import Button from '../components/Button.jsx'
import DataTable from '../components/DataTable.jsx'
import EmptyState from '../components/EmptyState.jsx'
import ErrorState from '../components/ErrorState.jsx'
import LoadingState from '../components/LoadingState.jsx'
import MetricCard from '../components/MetricCard.jsx'
import SelectField from '../components/SelectField.jsx'
import StatusBadge from '../components/StatusBadge.jsx'
import TextField from '../components/TextField.jsx'
import { useAuth } from '../hooks/useAuth.js'
import LayoutSAV from '../layouts/LayoutSAV.jsx'
import { queryReclamations } from '../services/reclamations.service.js'
import { formatDateTime } from '../utils/format.js'
import { Priority, ReclamationStatus } from '../utils/enums.js'

const PAGE_SIZE = 10

const STATUS_OPTIONS = [
  { value: 'ALL', label: 'Tous les statuts' },
  { value: String(ReclamationStatus.Open), label: 'Nouveau' },
  { value: String(ReclamationStatus.InProgress), label: 'En cours' },
  { value: String(ReclamationStatus.Resolved), label: 'Traite' },
  { value: String(ReclamationStatus.Closed), label: 'Ferme' },
]

const PRIORITY_OPTIONS = [
  { value: 'ALL', label: 'Toutes les priorites' },
  { value: String(Priority.LOW), label: 'Faible' },
  { value: String(Priority.MEDUIM), label: 'Moyenne' },
  { value: String(Priority.HIGH), label: 'Haute' },
  { value: String(Priority.URGENT), label: 'Urgente' },
]

export default function ReclamationsListPage() {
  const navigate = useNavigate()
  const { user } = useAuth()
  const role = String(user?.role ?? '').toUpperCase()
  const canCreate = role === 'CLIENT' || role === 'ADMIN' || role === 'SAV'

  const [items, setItems] = useState([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [query, setQuery] = useState('')
  const [searchDraft, setSearchDraft] = useState('')
  const [statusFilter, setStatusFilter] = useState('ALL')
  const [priorityFilter, setPriorityFilter] = useState('ALL')

  async function load({
    currentPage = page,
    currentQuery = query,
    currentStatus = statusFilter,
    currentPriority = priorityFilter,
  } = {}) {
    setLoading(true)
    setError(null)

    try {
      const data = await queryReclamations({
        page: currentPage,
        pageSize: PAGE_SIZE,
        search: currentQuery?.trim() || undefined,
        status: currentStatus !== 'ALL' ? Number(currentStatus) : undefined,
        priority: currentPriority !== 'ALL' ? Number(currentPriority) : undefined,
      })

      setItems(Array.isArray(data?.items) ? data.items : [])
      setTotalCount(Number(data?.totalCount ?? 0))
    } catch (err) {
      const message = err?.response?.data?.detail || err?.message
      setError(message || 'Impossible de charger les tickets SAV.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void load({ currentPage: page, currentQuery: query, currentStatus: statusFilter, currentPriority: priorityFilter })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page])

  const totalPages = useMemo(() => Math.max(1, Math.ceil(totalCount / PAGE_SIZE)), [totalCount])
  const urgentCount = useMemo(() => items.filter((item) => Number(item.priority) === Priority.URGENT).length, [items])
  const inProgressCount = useMemo(() => items.filter((item) => Number(item.status) === ReclamationStatus.InProgress).length, [items])

  function applyFilters() {
    setPage(1)
    setQuery(searchDraft)
    void load({
      currentPage: 1,
      currentQuery: searchDraft,
      currentStatus: statusFilter,
      currentPriority: priorityFilter,
    })
  }

  function resetFilters() {
    setSearchDraft('')
    setQuery('')
    setStatusFilter('ALL')
    setPriorityFilter('ALL')
    setPage(1)
    void load({
      currentPage: 1,
      currentQuery: '',
      currentStatus: 'ALL',
      currentPriority: 'ALL',
    })
  }

  return (
    <LayoutSAV
      title="Gestion des tickets SAV"
      description="La liste prioritaire de l application: on filtre, on recherche, on ouvre le detail et on comprend l etat des dossiers d un coup d oeil."
      meta={
        <>
          <span>{totalCount} ticket(s)</span>
          <span className="text-slate-300">|</span>
          <span>Role courant: {role || 'USER'}</span>
        </>
      }
      actions={
        <div className="flex flex-wrap gap-3">
          <Button variant="secondary" onClick={() => load()} disabled={loading}>
            <RefreshCw className="h-4 w-4" aria-hidden="true" />
            Actualiser
          </Button>
          {canCreate ? (
            <Link to="/app/reclamations/new">
              <Button>
                <Plus className="h-4 w-4" aria-hidden="true" />
                Ajouter
              </Button>
            </Link>
          ) : null}
        </div>
      }
    >
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">
        <MetricCard icon={ShieldAlert} label="Tickets visibles" value={items.length} helper={`Page ${page} / ${totalPages}`} tone="cyan" />
        <MetricCard icon={Filter} label="Resultats" value={totalCount} helper="Apres filtres et recherche" tone="slate" />
        <MetricCard icon={Search} label="Urgents" value={urgentCount} helper="Sur la page courante" tone={urgentCount ? 'amber' : 'emerald'} />
        <MetricCard icon={ShieldAlert} label="En cours" value={inProgressCount} helper="Sur la page courante" tone="amber" />
      </div>

      <section className="surface-solid p-6">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <h2 className="text-lg font-bold text-slate-950">Filtres SAV</h2>
            <p className="mt-1 text-sm text-slate-600">
              Recherchez par reference, client, description, categorie ou statut, puis filtrez par statut et priorite.
            </p>
          </div>

          <div className="grid w-full gap-3 md:grid-cols-2 xl:max-w-5xl xl:grid-cols-4">
            <TextField
              label="Recherche"
              placeholder="Reference, client, produit, email si disponible..."
              value={searchDraft}
              onChange={(event) => setSearchDraft(event.target.value)}
            />
            <SelectField label="Statut" value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)}>
              {STATUS_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </SelectField>
            <SelectField label="Priorite" value={priorityFilter} onChange={(event) => setPriorityFilter(event.target.value)}>
              {PRIORITY_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </SelectField>
            <div className="flex flex-col gap-2 sm:flex-row sm:items-end xl:justify-end">
              <Button variant="secondary" onClick={resetFilters} disabled={loading}>
                Retour
              </Button>
              <Button onClick={applyFilters} disabled={loading}>
                <Filter className="h-4 w-4" aria-hidden="true" />
                Filtrer
              </Button>
            </div>
          </div>
        </div>
      </section>

      {loading ? (
        <LoadingState title="Chargement des tickets SAV..." />
      ) : error ? (
        <ErrorState message={error} onAction={() => load()} />
      ) : items.length === 0 ? (
        <EmptyState
          icon={ShieldAlert}
          title="Aucun ticket trouve"
          description={canCreate ? 'Essayez un autre filtre ou ajoutez une nouvelle reclamation.' : 'Essayez un autre filtre.'}
          actionLabel={canCreate ? 'Ajouter une reclamation' : 'Reinitialiser les filtres'}
          onAction={canCreate ? () => navigate('/app/reclamations/new') : resetFilters}
        />
      ) : (
        <>
          <DataTable
            columns={[
              {
                key: 'reference',
                header: 'Ticket',
                render: (item) => (
                  <div>
                    <div className="font-semibold text-slate-950">{item.reference}</div>
                    <div className="mt-1 text-xs text-slate-500">{item.description || 'Aucune description'}</div>
                  </div>
                ),
              },
              {
                key: 'client',
                header: 'Client / produit',
                render: (item) => (
                  <div>
                    <div className="font-semibold text-slate-900">{item.clientName || '-'}</div>
                    <div className="mt-1 text-xs text-slate-500">{item.productName || item.productReference || 'Produit non renseigne'}</div>
                  </div>
                ),
              },
              {
                key: 'priority',
                header: 'Priorite',
                render: (item) => <StatusBadge kind="priority" value={item.priority} />,
              },
              {
                key: 'status',
                header: 'Statut',
                render: (item) => <StatusBadge kind="status" value={item.status} />,
              },
              {
                key: 'createdAt',
                header: 'Creation',
                render: (item) => formatDateTime(item.createdAt),
              },
              {
                key: 'actions',
                header: 'Actions',
                headerClassName: 'text-right',
                cellClassName: 'text-right',
                render: (item) => (
                  <Link to={`/app/reclamations/${item.id}`}>
                    <Button variant="secondary" size="sm">
                      Voir details
                    </Button>
                  </Link>
                ),
              },
            ]}
            rows={items}
            getRowKey={(item) => item.id}
            renderMobileCard={(item) => (
              <div className="space-y-3">
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <div className="font-semibold text-slate-950">{item.reference}</div>
                    <div className="mt-1 text-sm text-slate-600">{item.clientName || '-'}</div>
                  </div>
                  <StatusBadge kind="priority" value={item.priority} />
                </div>

                <div className="text-sm text-slate-600">{item.description || 'Aucune description'}</div>

                <div className="flex flex-wrap gap-2">
                  <StatusBadge kind="status" value={item.status} />
                  {item.productName ? <StatusBadge label={item.productName} tone="neutral" /> : null}
                </div>

                <div className="flex items-center justify-between gap-3">
                  <div className="text-xs text-slate-500">{formatDateTime(item.createdAt)}</div>
                  <Link to={`/app/reclamations/${item.id}`}>
                    <Button variant="secondary" size="sm">
                      Voir details
                    </Button>
                  </Link>
                </div>
              </div>
            )}
          />

          <section className="surface-solid p-4">
            <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div className="text-sm text-slate-600">
                Page <span className="font-semibold text-slate-900">{page}</span> sur{' '}
                <span className="font-semibold text-slate-900">{totalPages}</span> ({totalCount} ticket(s))
              </div>
              <div className="flex gap-2">
                <Button variant="secondary" disabled={page <= 1 || loading} onClick={() => setPage((value) => value - 1)}>
                  Precedent
                </Button>
                <Button variant="secondary" disabled={page >= totalPages || loading} onClick={() => setPage((value) => value + 1)}>
                  Suivant
                </Button>
              </div>
            </div>
          </section>
        </>
      )}
    </LayoutSAV>
  )
}
