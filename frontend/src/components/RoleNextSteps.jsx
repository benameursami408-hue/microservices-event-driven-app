import { ArrowRight, CheckCircle2, Route } from 'lucide-react'
import { Link } from 'react-router-dom'

import Button from './Button.jsx'
import StatusBadge from './StatusBadge.jsx'
import { getRoleNextSteps } from '../utils/demoGuide.js'
import { roleLabel } from '../utils/enums.js'

export default function RoleNextSteps({ role }) {
  const guide = getRoleNextSteps(role)

  return (
    <section className="surface-solid p-6">
      <div className="flex flex-col gap-5 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex items-start gap-4">
          <div className="grid h-12 w-12 shrink-0 place-items-center rounded-2xl bg-slate-950 text-white">
            <Route className="h-5 w-5" aria-hidden="true" />
          </div>
          <div>
            <div className="flex flex-wrap items-center gap-2">
              <StatusBadge kind="role" value={role} label={roleLabel(role)} />
              <StatusBadge tone="info" label="Aide demo" />
            </div>
            <h2 className="mt-3 text-xl font-bold text-slate-950">{guide.title}</h2>
            <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-600">{guide.summary}</p>
          </div>
        </div>

        <div className="grid gap-2 sm:min-w-[280px]">
          {guide.actions.map((action, index) => (
            <Link key={action.route + action.label} to={action.route}>
              <Button variant={index === 0 ? 'primary' : 'secondary'} className="w-full justify-between">
                <span className="inline-flex items-center gap-2">
                  {index === 0 ? <CheckCircle2 className="h-4 w-4" aria-hidden="true" /> : null}
                  {action.label}
                </span>
                <ArrowRight className="h-4 w-4" aria-hidden="true" />
              </Button>
            </Link>
          ))}
        </div>
      </div>
    </section>
  )
}
