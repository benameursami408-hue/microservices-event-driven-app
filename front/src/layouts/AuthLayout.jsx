import { createElement } from 'react'
import { Bell, CalendarDays, ShieldCheck, Wrench } from 'lucide-react'
import { Outlet } from 'react-router-dom'

export default function AuthLayout() {
  return (
    <div className="min-h-screen px-4 py-5 sm:px-6 lg:px-8">
      <div className="mx-auto grid min-h-[calc(100vh-2.5rem)] max-w-7xl grid-cols-1 gap-6 lg:grid-cols-[1.1fr_0.9fr]">
        <section className="hero-surface hidden lg:flex lg:flex-col lg:justify-between">
          <div className="relative">
            <div className="eyebrow">SAV Platform</div>
            <h1 className="mt-5 max-w-xl font-display text-5xl font-bold leading-[1.05] text-slate-950">
              Un portail SAV moderne, clair et operationnel.
            </h1>
            <p className="mt-5 max-w-2xl text-base leading-8 text-slate-600">
              Centralisez les reclamations, la planification terrain, les notifications et le pilotage d'equipe
              dans une experience premium prete pour une soutenance PFE.
            </p>
          </div>

          <div className="relative grid grid-cols-2 gap-4">
            <FeatureCard icon={ShieldCheck} title="ADMIN" description="KPI, supervision et gestion des equipes SAV / ST." />
            <FeatureCard icon={CalendarDays} title="SAV" description="Qualification, planification et suivi SLA." />
            <FeatureCard icon={Wrench} title="TECHNICIEN" description="Agenda, diagnostics et rapports d'intervention." />
            <FeatureCard icon={Bell} title="CLIENT" description="Suivi de dossier et retours d'etat plus lisibles." />
          </div>
        </section>

        <div className="flex items-center justify-center">
          <div className="w-full max-w-lg">
            <Outlet />
          </div>
        </div>
      </div>
    </div>
  )
}

function FeatureCard({ icon, title, description }) {
  return (
    <div className="surface-muted p-4">
      <div className="grid h-11 w-11 place-items-center rounded-2xl bg-slate-950 text-white">
        {createElement(icon, { className: 'h-5 w-5', 'aria-hidden': true })}
      </div>
      <div className="mt-4 text-sm font-extrabold uppercase tracking-[0.2em] text-slate-500">{title}</div>
      <div className="mt-2 text-sm leading-6 text-slate-600">{description}</div>
    </div>
  )
}
