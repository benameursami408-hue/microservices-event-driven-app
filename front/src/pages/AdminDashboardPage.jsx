import { Activity, MessageSquareWarning, Shield, UserCog, Users, Wrench } from 'lucide-react'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'

import Button from '../components/Button.jsx'
import DataTable from '../components/DataTable.jsx'
import EmptyState from '../components/EmptyState.jsx'
import ErrorState from '../components/ErrorState.jsx'
import LoadingState from '../components/LoadingState.jsx'
import MetricCard from '../components/MetricCard.jsx'
import StatusBadge from '../components/StatusBadge.jsx'
import { useAuth } from '../hooks/useAuth.js'
import LayoutAdmin from '../layouts/LayoutAdmin.jsx'
import { fetchReclamationStats, fetchUserStats } from '../services/adminStats.service.js'
import { formatDateTime } from '../utils/format.js'

function findRoleCount(userStats, roleKey) {
  return userStats?.byRole?.find((item) => String(item.role).toUpperCase() === roleKey)?.count ?? 0
}

function humanizeCategory(value) {
  if (value == null) return 'Non classe'

  return String(value)
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .replace(/_/g, ' ')
    .trim()
}

function QuickAction({ to, title, description, actionLabel }) {
  return (
    <Link to={to} className="surface-solid block p-5 transition hover:-translate-y-0.5">
      <div className="text-xs font-semibold uppercase tracking-[0.16em] text-cyan-700">{actionLabel}</div>
      <div className="mt-3 text-lg font-bold text-slate-950">{title}</div>
      <p className="mt-2 text-sm leading-6 text-slate-600">{description}</p>
    </Link>
  )
}

export default function AdminDashboardPage() {
  const { user } = useAuth()
  const role = String(user?.role ?? '').toUpperCase()
  const isAdmin = role === 'ADMIN'

  const [stats, setStats] = useState(null)
  const [userStats, setUserStats] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const loadDashboard = useCallback(async () => {
    setLoading(true)
    setError(null)

    try {
      const [reclamations, users] = await Promise.all([fetchReclamationStats(), fetchUserStats()])
      setStats(reclamations)
      setUserStats(users)
    } catch (err) {
      const message = err?.response?.data?.detail || err?.message
      setError(message || 'Impossible de charger les statistiques Admin.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    if (!isAdmin) return
    void loadDashboard()
  }, [isAdmin, loadDashboard])

  const openTickets = useMemo(() => {
    const kpis = stats?.kpis
    if (!kpis) return 0
    return Number(kpis.open ?? 0) + Number(kpis.assigned ?? 0) + Number(kpis.planned ?? 0) + Number(kpis.inProgress ?? 0)
  }, [stats])

  const closedTickets = useMemo(() => {
    const kpis = stats?.kpis
    if (!kpis) return 0
    return Number(kpis.resolved ?? 0) + Number(kpis.closed ?? 0)
  }, [stats])

  const categories = useMemo(() => (Array.isArray(stats?.byCategory) ? stats.byCategory.slice(0, 4) : []), [stats])

  if (!isAdmin) {
    return (
      <ErrorState
        title="Acces reserve"
        message="Seul un compte Admin peut ouvrir ce dashboard."
      />
    )
  }

  if (loading) {
    return <LoadingState title="Chargement du dashboard Admin..." description="Recuperation des utilisateurs et des tickets SAV." />
  }

  if (error) {
    return <ErrorState message={error} onAction={loadDashboard} />
  }

  if (!stats || !userStats) {
    return (
      <EmptyState
        title="Aucune statistique disponible"
        description="Les donnees Admin ne sont pas encore remontees par les API."
        actionLabel="Actualiser"
        onAction={loadDashboard}
      />
    )
  }

  return (
    <LayoutAdmin
      title="Dashboard admin simple"
      description="Les chiffres essentiels sont regroupes ici pour comprendre l application et rejoindre rapidement les CRUD importants."
      meta={
        <>
          <span>{stats?.kpis?.total ?? 0} tickets</span>
          <span className="text-slate-300">|</span>
          <span>{userStats?.total ?? 0} utilisateurs</span>
        </>
      }
      actions={
        <div className="flex flex-wrap gap-3">
          <Link to="/app/admin/users">
            <Button>Gerer les utilisateurs</Button>
          </Link>
          <Link to="/app/reclamations">
            <Button variant="secondary">Ouvrir les tickets SAV</Button>
          </Link>
          <Link to="/app/guide-test">
            <Button variant="secondary">Guide de test</Button>
          </Link>
        </div>
      }
    >
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-6">
        <MetricCard icon={Users} label="Utilisateurs" value={userStats.total} helper={`${userStats.active} actifs`} tone="cyan" />
        <MetricCard icon={UserCog} label="Clients" value={findRoleCount(userStats, 'CLIENT')} helper="Comptes client" tone="emerald" />
        <MetricCard icon={Shield} label="Admins" value={findRoleCount(userStats, 'ADMIN')} helper="Acces d administration" tone="slate" />
        <MetricCard icon={MessageSquareWarning} label="Tickets ouverts" value={openTickets} helper="Nouveau, affecte, planifie, en cours" tone="amber" />
        <MetricCard icon={Activity} label="Tickets traites" value={closedTickets} helper="Traites et fermes" tone="emerald" />
        <MetricCard icon={Wrench} label="Techniciens" value={findRoleCount(userStats, 'ST')} helper="Comptes techniques" tone="slate" />
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-[1.2fr_0.8fr]">
        <section className="surface-solid p-6">
          <div className="flex items-center justify-between gap-3">
            <div>
              <h2 className="text-lg font-bold text-slate-950">Acces rapides</h2>
              <p className="mt-1 text-sm text-slate-600">Les pages utiles pour tester Admin sans chercher dans les menus.</p>
            </div>
            <Button variant="secondary" onClick={loadDashboard}>
              Actualiser
            </Button>
          </div>

          <div className="mt-5 grid grid-cols-1 gap-4 md:grid-cols-2">
            <QuickAction
              to="/app/admin/users"
              title="Tous les utilisateurs"
              description="Ajouter ou modifier un compte Admin, SAV, technique ou client."
              actionLabel="CRUD"
            />
            <QuickAction
              to="/app/admin/sav"
              title="Equipe SAV"
              description="Verifier le CRUD dedie aux agents SAV."
              actionLabel="SAV"
            />
            <QuickAction
              to="/app/admin/st"
              title="Equipe technique"
              description="Gerer les comptes techniciens depuis une page ciblee."
              actionLabel="Technique"
            />
            <QuickAction
              to="/app/reclamations"
              title="Tickets SAV"
              description="Basculer tout de suite sur la liste principale des reclamations."
              actionLabel="Tickets"
            />
            <QuickAction
              to="/app/guide-test"
              title="Guide de test"
              description="Suivre le scenario complet pour la demonstration et la validation manuelle."
              actionLabel="Demo"
            />
          </div>
        </section>

        <section className="surface-solid p-6">
          <h2 className="text-lg font-bold text-slate-950">Repartition utile</h2>
          <div className="mt-4 space-y-3">
            <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
              <div className="text-xs font-semibold uppercase tracking-[0.16em] text-slate-500">Roles</div>
              <div className="mt-3 flex flex-wrap gap-2">
                <StatusBadge kind="role" value="ADMIN" label={`${findRoleCount(userStats, 'ADMIN')} Admin`} />
                <StatusBadge kind="role" value="SAV" label={`${findRoleCount(userStats, 'SAV')} SAV`} />
                <StatusBadge kind="role" value="ST" label={`${findRoleCount(userStats, 'ST')} Technique`} />
                <StatusBadge kind="role" value="CLIENT" label={`${findRoleCount(userStats, 'CLIENT')} Client`} />
              </div>
            </div>

            <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
              <div className="text-xs font-semibold uppercase tracking-[0.16em] text-slate-500">Categories tickets</div>
              <div className="mt-3 flex flex-wrap gap-2">
                {categories.length ? (
                  categories.map((item) => (
                    <StatusBadge
                      key={`${item.category}`}
                      label={`${humanizeCategory(item.category)}: ${item.count}`}
                      tone="info"
                    />
                  ))
                ) : (
                  <StatusBadge label="Aucune categorie remontee" tone="neutral" />
                )}
              </div>
            </div>
          </div>
        </section>
      </div>

      <section className="space-y-4">
        <div>
          <h2 className="text-lg font-bold text-slate-950">Derniers tickets</h2>
          <p className="mt-1 text-sm text-slate-600">Vue utile pour verifier rapidement l etat des dossiers les plus recents.</p>
        </div>

        <DataTable
          columns={[
            {
              key: 'reference',
              header: 'Reference',
              render: (item) => (
                <div>
                  <div className="font-semibold text-slate-950">{item.reference}</div>
                  <div className="mt-1 text-xs text-slate-500">{item.clientName}</div>
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
                <Link className="link font-semibold" to={`/app/reclamations/${item.id}`}>
                  Voir details
                </Link>
              ),
            },
          ]}
          rows={stats.latest ?? []}
          getRowKey={(item) => item.id}
          renderMobileCard={(item) => (
            <div className="space-y-3">
              <div className="flex items-start justify-between gap-3">
                <div>
                  <div className="font-semibold text-slate-950">{item.reference}</div>
                  <div className="mt-1 text-sm text-slate-600">{item.clientName}</div>
                </div>
                <StatusBadge kind="priority" value={item.priority} />
              </div>
              <div className="flex flex-wrap gap-2">
                <StatusBadge kind="status" value={item.status} />
                <StatusBadge label={formatDateTime(item.createdAt)} tone="neutral" />
              </div>
              <Link className="link font-semibold" to={`/app/reclamations/${item.id}`}>
                Voir details
              </Link>
            </div>
          )}
          emptyFallback={
            <EmptyState
              title="Aucun ticket recent"
              description="Les derniers tickets apparaitront ici apres creation."
            />
          }
        />
      </section>
    </LayoutAdmin>
  )
}
