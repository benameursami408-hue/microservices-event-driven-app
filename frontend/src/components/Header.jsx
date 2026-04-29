import { Menu } from 'lucide-react'

import Button from './Button.jsx'
import StatusBadge from './StatusBadge.jsx'

export default function Header({ title, description, sectionLabel, roleLabel, onOpenMenu }) {
  return (
    <header className="surface-solid flex flex-col gap-4 px-5 py-4 sm:px-6 lg:flex-row lg:items-center lg:justify-between">
      <div className="flex items-start gap-3">
        <Button variant="ghost" className="h-11 w-11 rounded-2xl p-0 lg:hidden" onClick={onOpenMenu}>
          <Menu className="h-5 w-5" aria-hidden="true" />
        </Button>
        <div>
          <div className="text-[11px] font-extrabold uppercase tracking-[0.22em] text-cyan-700">{sectionLabel}</div>
          <h1 className="mt-2 text-2xl font-bold text-slate-950">{title}</h1>
          {description ? <p className="mt-1 text-sm text-slate-600">{description}</p> : null}
        </div>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <StatusBadge kind="role" value={roleLabel} />
        <StatusBadge label="Responsive ready" tone="success" />
      </div>
    </header>
  )
}
