import { X } from 'lucide-react'
import clsx from 'clsx'
import { NavLink } from 'react-router-dom'

import Button from './Button.jsx'

function SidebarLink({ item, onNavigate }) {
  const Icon = item.icon

  return (
    <NavLink
      to={item.to}
      end={item.end}
      onClick={onNavigate}
      className={({ isActive }) =>
        clsx(
          'group flex items-start gap-3 rounded-2xl px-3 py-3 transition',
          isActive ? 'bg-slate-950 text-white' : 'text-slate-700 hover:bg-slate-100 hover:text-slate-950',
        )
      }
    >
      {({ isActive }) => (
        <>
          <div
            className={clsx(
              'mt-0.5 grid h-10 w-10 shrink-0 place-items-center rounded-2xl transition',
              isActive ? 'bg-white/10 text-white' : 'bg-slate-100 text-slate-700 group-hover:bg-white',
            )}
          >
            <Icon className="h-4 w-4" aria-hidden="true" />
          </div>
          <div className="min-w-0">
            <div className="text-sm font-bold">{item.label}</div>
            {item.description ? <div className={clsx('mt-1 text-xs', isActive ? 'text-slate-200' : 'text-slate-500')}>{item.description}</div> : null}
          </div>
        </>
      )}
    </NavLink>
  )
}

function SidebarPanel({ brand, user, sections, onNavigate, onClose, mobileOnlyClose }) {
  return (
    <div className="flex h-full flex-col">
      <div className="flex items-start justify-between gap-3 border-b border-slate-200 px-4 py-4">
        <div>
          <div className="text-[11px] font-extrabold uppercase tracking-[0.24em] text-cyan-700">{brand.kicker}</div>
          <div className="mt-2 text-xl font-bold text-slate-950">{brand.title}</div>
          <div className="mt-1 text-sm text-slate-600">{brand.description}</div>
        </div>
        {mobileOnlyClose ? (
          <Button variant="ghost" className="h-10 w-10 rounded-2xl p-0 lg:hidden" onClick={onClose}>
            <X className="h-4 w-4" aria-hidden="true" />
          </Button>
        ) : null}
      </div>

      <div className="border-b border-slate-200 px-4 py-4">
        <div className="rounded-2xl bg-slate-50 px-4 py-3">
          <div className="text-sm font-bold text-slate-950">{user?.name || 'Utilisateur connecte'}</div>
          <div className="mt-1 text-sm text-slate-600">{user?.email || 'Session active'}</div>
          <div className="mt-2 text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">{user?.role || 'Compte'}</div>
        </div>
      </div>

      <div className="flex-1 overflow-y-auto px-3 py-4">
        <div className="space-y-5">
          {sections.map((section) => (
            <div key={section.title} className="space-y-2">
              <div className="px-2 text-[11px] font-extrabold uppercase tracking-[0.22em] text-slate-500">{section.title}</div>
              <div className="space-y-1">
                {section.items.map((item) => (
                  <SidebarLink key={item.to} item={item} onNavigate={onNavigate} />
                ))}
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}

export default function Sidebar({ brand, user, sections, isOpen, onClose }) {
  return (
    <>
      <aside className="surface-solid hidden h-[calc(100vh-48px)] overflow-hidden lg:flex">
        <SidebarPanel brand={brand} user={user} sections={sections} onNavigate={undefined} />
      </aside>

      {isOpen ? (
        <div className="fixed inset-0 z-50 flex lg:hidden">
          <button type="button" className="absolute inset-0 bg-slate-950/45 backdrop-blur-sm" aria-label="Fermer le menu" onClick={onClose} />
          <aside className="surface-solid relative z-10 m-3 h-[calc(100vh-24px)] w-full max-w-sm overflow-hidden">
            <SidebarPanel brand={brand} user={user} sections={sections} onNavigate={onClose} onClose={onClose} mobileOnlyClose />
          </aside>
        </div>
      ) : null}
    </>
  )
}
