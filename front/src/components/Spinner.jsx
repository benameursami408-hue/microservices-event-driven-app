export default function Spinner({ label = 'Loading...' }) {
  return (
    <div className="flex items-center gap-4 text-slate-700">
      <span className="relative inline-flex h-11 w-11 items-center justify-center rounded-2xl bg-cyan-50 text-cyan-700">
        <span
          className="inline-block h-5 w-5 animate-spin rounded-full border-2 border-cyan-200 border-t-cyan-700"
          aria-hidden="true"
        />
        <span className="absolute inset-0 rounded-2xl ring-1 ring-inset ring-cyan-100" aria-hidden="true" />
      </span>
      <div>
        <div className="text-sm font-semibold text-slate-900">{label}</div>
        <div className="text-xs text-slate-500">Preparing the latest data for this workspace.</div>
      </div>
    </div>
  )
}
