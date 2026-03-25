import { Inbox } from 'lucide-react'
import Button from './Button.jsx'

export default function EmptyState({ title, description, actionLabel, onAction }) {
  return (
    <div className="surface-solid p-8 text-center">
      <div className="mx-auto grid h-12 w-12 place-items-center rounded-2xl bg-slate-100 text-slate-700">
        <Inbox className="h-6 w-6" aria-hidden="true" />
      </div>
      <div className="mt-4 text-lg font-semibold text-slate-900">{title}</div>
      {description ? <div className="mt-1 text-sm text-slate-600">{description}</div> : null}
      {actionLabel ? (
        <div className="mt-5">
          <Button variant="secondary" onClick={onAction}>
            {actionLabel}
          </Button>
        </div>
      ) : null}
    </div>
  )
}
