import {
  Bell,
  BookOpenCheck,
  ClipboardCheck,
  LayoutDashboard,
  MessageSquareWarning,
  Settings2,
  Shield,
  UserCog,
  Users,
  Wrench,
} from 'lucide-react'
import { useMemo, useState } from 'react'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'

import Button from '../components/Button.jsx'
import Header from '../components/Header.jsx'
import Sidebar from '../components/Sidebar.jsx'
import { useAuth } from '../hooks/useAuth.js'
import { roleLabel } from '../utils/enums.js'

function buildSections({ role }) {
  const isAdmin = role === 'ADMIN'
  const canSeeSav = role === 'ADMIN' || role === 'SAV' || role === 'CLIENT'
  const canSeeTech = role === 'ADMIN' || role === 'ST'

  const sections = [
    {
      title: 'Navigation',
      items: [
        {
          to: '/app',
          label: 'Accueil',
          description: 'Comprendre rapidement quoi tester',
          icon: LayoutDashboard,
          end: true,
        },
        {
          to: '/app/guide-test',
          label: 'Guide de test',
          description: 'Parcours conseille pour la demo',
          icon: BookOpenCheck,
        },
        {
          to: '/app/reclamations',
          label: 'Tickets SAV',
          description: 'Liste, filtres et detail des reclamations',
          icon: MessageSquareWarning,
        },
        {
          to: '/app/notifications',
          label: 'Notifications',
          description: 'Messages et retours du systeme',
          icon: Bell,
        },
      ],
    },
  ]

  if (canSeeSav) {
    sections.push({
      title: 'Espace SAV',
      items: [
        {
          to: '/app/reclamations',
          label: 'Liste des tickets',
          description: 'Parcours principal de demonstration',
          icon: ClipboardCheck,
        },
        {
          to: '/app/reclamations/new',
          label: 'Nouvelle reclamation',
          description: 'Creer un ticket rapidement',
          icon: MessageSquareWarning,
        },
      ],
    })
  }

  if (isAdmin) {
    sections.push({
      title: 'Espace Admin',
      items: [
        {
          to: '/app/admin',
          label: 'Dashboard admin',
          description: 'KPIs et acces rapides',
          icon: Shield,
          end: true,
        },
        {
          to: '/app/admin/users',
          label: 'Utilisateurs',
          description: 'Tous les comptes et roles',
          icon: UserCog,
        },
        {
          to: '/app/admin/sav',
          label: 'Equipe SAV',
          description: 'Agents de service apres-vente',
          icon: Users,
        },
        {
          to: '/app/admin/st',
          label: 'Equipe technique',
          description: 'Techniciens et acces',
          icon: Wrench,
        },
      ],
    })
  }

  if (canSeeTech) {
    sections.push({
      title: 'Suivi technique',
      items: [
        {
          to: '/app/interventions',
          label: 'Interventions',
          description: 'Execution et compte-rendu',
          icon: Wrench,
        },
        {
          to: '/app/planning',
          label: 'Planning',
          description: 'Agenda des rendez-vous',
          icon: Settings2,
        },
      ],
    })
  }

  return sections
}

function resolveHeader(pathname) {
  if (pathname.startsWith('/app/guide-test')) {
    return {
      sectionLabel: 'Guide',
      title: 'Guide de test',
      description: 'Parcours clair pour tester et presenter toute la plateforme SAV.',
    }
  }

  if (pathname.startsWith('/app/admin/users')) {
    return {
      sectionLabel: 'Administration',
      title: 'Gestion des utilisateurs',
      description: 'CRUD clair, formulaires simples et actions visibles.',
    }
  }

  if (pathname.startsWith('/app/admin/sav')) {
    return {
      sectionLabel: 'Administration',
      title: 'Equipe SAV',
      description: 'Gerer les comptes SAV sans surcharge visuelle.',
    }
  }

  if (pathname.startsWith('/app/admin/st')) {
    return {
      sectionLabel: 'Administration',
      title: 'Equipe technique',
      description: 'Suivre les comptes techniciens et leurs acces.',
    }
  }

  if (pathname.startsWith('/app/admin')) {
    return {
      sectionLabel: 'Administration',
      title: 'Dashboard admin',
      description: 'Voir les chiffres utiles et atteindre les CRUD principaux.',
    }
  }

  if (pathname.startsWith('/app/reclamations/new')) {
    return {
      sectionLabel: 'Espace SAV',
      title: 'Nouvelle reclamation',
      description: 'Creer un ticket complet, lisible et facile a tester.',
    }
  }

  if (pathname.startsWith('/app/reclamations/')) {
    return {
      sectionLabel: 'Espace SAV',
      title: 'Detail du ticket',
      description: 'Consulter, modifier et changer le statut sans se perdre.',
    }
  }

  if (pathname.startsWith('/app/reclamations')) {
    return {
      sectionLabel: 'Espace SAV',
      title: 'Tickets SAV',
      description: 'Liste claire avec filtres, recherche et actions rapides.',
    }
  }

  if (pathname.startsWith('/app/interventions')) {
    return {
      sectionLabel: 'Technique',
      title: 'Interventions',
      description: 'Suivi terrain et execution des visites.',
    }
  }

  if (pathname.startsWith('/app/planning')) {
    return {
      sectionLabel: 'Technique',
      title: 'Planning',
      description: 'Agenda et coordination des rendez-vous.',
    }
  }

  if (pathname.startsWith('/app/notifications')) {
    return {
      sectionLabel: 'Suivi',
      title: 'Notifications',
      description: 'Verifier les retours systeme et les alertes.',
    }
  }

  return {
    sectionLabel: 'Accueil',
    title: 'Portail SAV',
    description: 'Une interface simple pour presenter Admin et SAV.',
  }
}

export default function AppLayout() {
  const { user, logout } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const [sidebarOpen, setSidebarOpen] = useState(false)

  const role = String(user?.role ?? '').toUpperCase()
  const fullName =
    user?.firstName || user?.lastName
      ? `${user?.firstName ?? ''} ${user?.lastName ?? ''}`.trim()
      : user?.email ?? 'Utilisateur'

  const sections = useMemo(() => buildSections({ role }), [role])
  const header = useMemo(() => resolveHeader(location.pathname), [location.pathname])

  return (
    <div className="min-h-screen px-3 py-3 sm:px-4 lg:px-6">
      <div className="mx-auto grid max-w-[1500px] gap-4 lg:grid-cols-[320px_minmax(0,1fr)]">
        <Sidebar
          brand={{
            kicker: 'PFE.NET',
            title: 'Portail SAV',
            description: 'Admin, SAV et tickets dans une navigation claire.',
          }}
          user={{
            name: fullName,
            email: user?.email,
            role: roleLabel(user?.role),
          }}
          sections={sections}
          isOpen={sidebarOpen}
          onClose={() => setSidebarOpen(false)}
        />

        <div className="min-w-0 space-y-4">
          <Header
            title={header.title}
            description={header.description}
            sectionLabel={header.sectionLabel}
            roleLabel={user?.role}
            onOpenMenu={() => setSidebarOpen(true)}
          />

          <main className="space-y-6">
            <Outlet />
          </main>

          <div className="surface-solid flex flex-col gap-3 px-5 py-4 sm:flex-row sm:items-center sm:justify-between sm:px-6">
            <div>
              <div className="text-sm font-semibold text-slate-900">Session active</div>
              <div className="mt-1 text-sm text-slate-600">
                {fullName} - {user?.email}
              </div>
            </div>

            <Button
              variant="secondary"
              onClick={() => {
                logout()
                navigate('/login')
              }}
            >
              Se deconnecter
            </Button>
          </div>
        </div>
      </div>
    </div>
  )
}
