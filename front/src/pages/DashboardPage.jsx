import {
  Bell,
  ClipboardList,
  LayoutDashboard,
  MessageSquareWarning,
  Plus,
  Shield,
  UserCog,
  Users,
  Wrench,
} from 'lucide-react'
import { Link } from 'react-router-dom'

import Button from '../components/Button.jsx'
import MetricCard from '../components/MetricCard.jsx'
import RoleNextSteps from '../components/RoleNextSteps.jsx'
import StatusBadge from '../components/StatusBadge.jsx'
import { useAuth } from '../hooks/useAuth.js'
import { roleLabel } from '../utils/enums.js'

function QuickCard({ to, icon, title, description, actionLabel }) {
  const IconComponent = icon

  return (
    <Link to={to} className="surface-solid block p-5 transition hover:-translate-y-0.5">
      <div className="flex items-start justify-between gap-4">
        <div className="grid h-12 w-12 place-items-center rounded-2xl bg-slate-950 text-white">
          <IconComponent className="h-5 w-5" aria-hidden="true" />
        </div>
        <span className="text-xs font-semibold uppercase tracking-[0.16em] text-cyan-700">{actionLabel}</span>
      </div>
      <div className="mt-5 text-xl font-bold text-slate-950">{title}</div>
      <p className="mt-2 text-sm leading-6 text-slate-600">{description}</p>
    </Link>
  )
}

export default function DashboardPage() {
  const { user } = useAuth()
  const role = String(user?.role ?? '').toUpperCase()
  const isAdmin = role === 'ADMIN'
  const canCreate = role === 'ADMIN' || role === 'SAV' || role === 'CLIENT'

  const primaryLinks = [
    {
      to: '/app/reclamations',
      icon: MessageSquareWarning,
      title: 'Tester les tickets SAV',
      description: 'Acceder a la liste principale, appliquer des filtres et ouvrir les details.',
      actionLabel: 'Voir',
    },
    ...(canCreate
      ? [
          {
            to: '/app/reclamations/new',
            icon: Plus,
            title: 'Creer une reclamation',
            description: 'Verifier rapidement le formulaire, la validation et les messages d erreur.',
            actionLabel: 'Ajouter',
          },
        ]
      : []),
    {
      to: '/app/guide-test',
      icon: LayoutDashboard,
      title: 'Suivre le guide de test',
      description: 'Voir le scenario complet pour comprendre quoi tester selon chaque role.',
      actionLabel: 'Guide',
    },
    {
      to: '/app/notifications',
      icon: Bell,
      title: 'Verifier les notifications',
      description: 'Voir les evenements et les retours systeme visibles par le profil connecte.',
      actionLabel: 'Consulter',
    },
  ]

  const adminLinks = isAdmin
    ? [
        {
          to: '/app/admin',
          icon: Shield,
          title: 'Ouvrir le dashboard admin',
          description: 'Lire les compteurs essentiels sans passer par des pages trop chargees.',
          actionLabel: 'Voir',
        },
        {
          to: '/app/admin/users',
          icon: UserCog,
          title: 'Gerer les utilisateurs',
          description: 'Ajouter, modifier et supprimer les comptes de test sur une page unique.',
          actionLabel: 'Tester',
        },
        {
          to: '/app/admin/sav',
          icon: Users,
          title: 'Verifier l equipe SAV',
          description: 'Filtrer les comptes SAV et tester leur CRUD dedie.',
          actionLabel: 'Ouvrir',
        },
      ]
    : []

  return (
    <div className="space-y-6">
      <section className="hero-surface">
        <div className="relative flex flex-col gap-6 lg:flex-row lg:items-end lg:justify-between">
          <div className="max-w-3xl">
            <div className="eyebrow">Parcours de demonstration</div>
            <h2 className="mt-4 text-3xl font-bold text-slate-950 sm:text-4xl">
              {user?.firstName ? `Bonjour ${user.firstName}, voici les pages utiles.` : 'Portail SAV simplifie'}
            </h2>
            <p className="mt-3 max-w-2xl text-sm leading-7 text-slate-600 sm:text-base">
              Cette page sert de point d entree: on comprend immediatement ou aller pour tester l espace Admin, l espace SAV
              et les CRUD prioritaires.
            </p>
            <div className="mt-4 flex flex-wrap gap-2">
              <StatusBadge kind="role" value={user?.role} />
              <StatusBadge label={user?.email || 'Session active'} tone="neutral" />
            </div>
          </div>

          <div className="flex flex-wrap gap-3">
            <Link to="/app/reclamations">
              <Button size="lg">
                <ClipboardList className="h-4 w-4" aria-hidden="true" />
                Ouvrir les tickets
              </Button>
            </Link>
            {canCreate ? (
              <Link to="/app/reclamations/new">
                <Button variant="secondary" size="lg">
                  <Plus className="h-4 w-4" aria-hidden="true" />
                  Ajouter un ticket
                </Button>
              </Link>
            ) : null}
          </div>
        </div>
      </section>

      <RoleNextSteps role={user?.role} />

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">
        <MetricCard icon={LayoutDashboard} label="Profil connecte" value={roleLabel(user?.role)} helper="Navigation adaptee au role" tone="cyan" />
        <MetricCard icon={MessageSquareWarning} label="Priorite produit" value="SAV" helper="Tickets, suivi et traitement" tone="amber" />
        <MetricCard icon={Shield} label="Espace Admin" value={isAdmin ? 'Disponible' : 'Selon role'} helper="Tableau de bord et utilisateurs" tone="slate" />
        <MetricCard icon={Wrench} label="Lisibilite" value="Simple" helper="Parcours clarifies pour la presentation" tone="emerald" />
      </div>

      <section className="space-y-4">
        <div className="flex items-center justify-between gap-3">
          <div>
            <h2 className="text-xl font-bold text-slate-950">Commencer les tests</h2>
            <p className="mt-1 text-sm text-slate-600">Les raccourcis ci-dessous pointent vers les modules les plus importants.</p>
          </div>
        </div>

        <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
          {primaryLinks.map((item) => (
            <QuickCard key={item.to} {...item} />
          ))}
        </div>
      </section>

      {adminLinks.length ? (
        <section className="space-y-4">
          <div>
            <h2 className="text-xl font-bold text-slate-950">Priorite Admin</h2>
            <p className="mt-1 text-sm text-slate-600">Acces directs vers le dashboard et la gestion des utilisateurs.</p>
          </div>

          <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
            {adminLinks.map((item) => (
              <QuickCard key={item.to} {...item} />
            ))}
          </div>
        </section>
      ) : null}
    </div>
  )
}
