import { createElement } from 'react'
import { Inbox } from 'lucide-react'
import Button from './Button.jsx'

export default function EmptyState({ title, description, actionLabel, onAction, icon }) {
  const IconComponent = icon ?? Inbox

  return (
    <div className="surface-solid relative overflow-hidden p-8 text-center">
      <div className="absolute inset-x-8 top-0 h-24 rounded-b-[100px] bg-linear-to-r from-cyan-100/45 via-white to-amber-100/35" aria-hidden="true" />
      <div className="relative mx-auto grid h-16 w-16 place-items-center rounded-[22px] bg-linear-to-br from-cyan-600 to-sky-700 text-white shadow-[0_20px_40px_-24px_rgba(14,116,144,0.75)]">
        {createElement(IconComponent, { className: 'h-7 w-7', 'aria-hidden': true })}
      </div>
      <div className="relative mt-5 text-xl font-bold text-slate-900">{title}</div>
      {description ? <div className="relative mt-2 mx-auto max-w-md text-sm leading-6 text-slate-600">{description}</div> : null}
      {actionLabel ? (
        <div className="relative mt-6">
          <Button variant="secondary" onClick={onAction}>
            {actionLabel}
          </Button>
        </div>
      ) : null}
    </div>
  )
}
