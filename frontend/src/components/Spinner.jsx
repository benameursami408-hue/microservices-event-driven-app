export default function Spinner({ label = 'Loading...' }) {
  return (
    <div className="flex items-center gap-3 text-slate-700">
      <span
        className="inline-block h-5 w-5 animate-spin rounded-full border-2 border-slate-300 border-t-cyan-600"
        aria-hidden="true"
      />
      <span className="text-sm font-medium">{label}</span>
    </div>
  )
}
