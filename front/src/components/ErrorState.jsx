import { AlertTriangle, RefreshCw } from 'lucide-react'

import Button from './Button.jsx'

export default function ErrorState({ title = 'Une erreur est survenue', message, actionLabel = 'Reessayer', onAction }) {
  return (
    <div className="surface-solid p-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div className="flex gap-3">
          <div className="grid h-11 w-11 shrink-0 place-items-center rounded-2xl bg-rose-50 text-rose-700">
            <AlertTriangle className="h-5 w-5" aria-hidden="true" />
          </div>
          <div>
            <h2 className="text-lg font-bold text-slate-950">{title}</h2>
            <p className="mt-1 text-sm text-slate-600">{message || 'Veuillez reessayer.'}</p>
          </div>
        </div>

        {onAction ? (
          <Button variant="secondary" onClick={onAction}>
            <RefreshCw className="h-4 w-4" aria-hidden="true" />
            {actionLabel}
          </Button>
        ) : null}
      </div>
    </div>
  )
}
