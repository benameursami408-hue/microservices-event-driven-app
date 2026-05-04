import { ArrowRight, CheckCircle2, ClipboardCheck, MonitorSmartphone, Route, ShieldCheck } from 'lucide-react'
import { Link } from 'react-router-dom'

import Button from '../components/Button.jsx'
import MetricCard from '../components/MetricCard.jsx'
import RoleNextSteps from '../components/RoleNextSteps.jsx'
import StatusBadge from '../components/StatusBadge.jsx'
import { useAuth } from '../hooks/useAuth.js'
import { DEMO_WORKFLOW_STEPS } from '../utils/demoGuide.js'

function StepCard({ step }) {
  return (
    <article className="surface-solid p-5">
      <div className="flex items-start justify-between gap-4">
        <div>
          <div className="text-xs font-semibold uppercase tracking-[0.16em] text-cyan-700">{step.role}</div>
          <h3 className="mt-2 text-lg font-bold text-slate-950">{step.title}</h3>
        </div>
        <StatusBadge tone="neutral" label="Test" />
      </div>

      <p className="mt-3 text-sm leading-6 text-slate-600">{step.goal}</p>

      <div className="mt-4 space-y-2">
        {step.checks.map((check) => (
          <div key={check} className="flex items-center gap-2 text-sm text-slate-700">
            <CheckCircle2 className="h-4 w-4 text-emerald-600" aria-hidden="true" />
            <span>{check}</span>
          </div>
        ))}
      </div>

      <Link to={step.route} className="mt-5 inline-flex items-center gap-2 text-sm font-semibold text-cyan-700 hover:text-cyan-900">
        Ouvrir cette page
        <ArrowRight className="h-4 w-4" aria-hidden="true" />
      </Link>
    </article>
  )
}

export default function DemoGuidePage() {
  const { user } = useAuth()

  return (
    <div className="space-y-6">
      <section className="hero-surface">
        <div className="relative flex flex-col gap-6 lg:flex-row lg:items-end lg:justify-between">
          <div className="max-w-3xl">
            <div className="eyebrow">Guide de test</div>
            <h2 className="mt-4 text-3xl font-bold text-slate-950 sm:text-4xl">Parcours simple pour presenter le projet SAV</h2>
            <p className="mt-3 max-w-2xl text-sm leading-7 text-slate-600 sm:text-base">
              Cette page explique exactement quoi tester, dans quel ordre et avec quel role. Elle sert de support rapide avant une reunion ou une soutenance.
            </p>
            <div className="mt-4 flex flex-wrap gap-2">
              <StatusBadge kind="role" value={user?.role} />
              <StatusBadge tone="success" label="Phase 3 UX" />
              <StatusBadge tone="info" label="Demo guidee" />
            </div>
          </div>
          <div className="flex flex-wrap gap-3">
            <Link to="/app/reclamations">
              <Button size="lg">
                <ClipboardCheck className="h-4 w-4" aria-hidden="true" />
                Tickets SAV
              </Button>
            </Link>
            <Link to="/app/admin/users">
              <Button variant="secondary" size="lg">
                <ShieldCheck className="h-4 w-4" aria-hidden="true" />
                Utilisateurs
              </Button>
            </Link>
          </div>
        </div>
      </section>

      <RoleNextSteps role={user?.role} />

      <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
        <MetricCard icon={Route} label="Ordre de test" value="6 etapes" helper="De Admin vers Client/SAV/ST" tone="cyan" />
        <MetricCard icon={ShieldCheck} label="Roles couverts" value="4 roles" helper="Admin, SAV, ST, Client" tone="emerald" />
        <MetricCard icon={MonitorSmartphone} label="Presentation" value="Lisible" helper="Desktop et mobile" tone="slate" />
      </div>

      <section className="space-y-4">
        <div>
          <h2 className="text-xl font-bold text-slate-950">Scenario manuel recommande</h2>
          <p className="mt-1 text-sm text-slate-600">
            Suivez ces cartes de haut en bas pour tester toute la relation entre utilisateurs, reclamations, planning, interventions et notifications.
          </p>
        </div>

        <div className="grid grid-cols-1 gap-4 lg:grid-cols-2 xl:grid-cols-3">
          {DEMO_WORKFLOW_STEPS.map((step) => (
            <StepCard key={step.id} step={step} />
          ))}
        </div>
      </section>

      <section className="surface-solid p-6">
        <h2 className="text-xl font-bold text-slate-950">Regle de demo</h2>
        <p className="mt-2 text-sm leading-6 text-slate-600">
          Ne testez pas tout au hasard. Commencez par les comptes, creez un ticket, suivez le ticket, planifiez l intervention, terminez l intervention, puis verifiez les notifications. Ce parcours montre la vraie relation entre les modules.
        </p>
      </section>
    </div>
  )
}
