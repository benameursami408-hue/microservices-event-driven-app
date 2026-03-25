import clsx from 'clsx'

export default function TextField({ label, hint, error, className, ...props }) {
  return (
    <label className={clsx('block', className)}>
      {label ? <div className="mb-1 text-sm font-semibold text-slate-800">{label}</div> : null}
      <input
        className={clsx(
          'block w-full rounded-xl border-slate-200 bg-white/80 shadow-sm placeholder:text-slate-400 focus:border-cyan-600 focus:ring-cyan-600',
          error && 'border-rose-300 focus:border-rose-500 focus:ring-rose-500',
        )}
        {...props}
      />
      {error ? <div className="mt-1 text-sm text-rose-700">{error}</div> : null}
      {!error && hint ? <div className="mt-1 text-xs text-slate-500">{hint}</div> : null}
    </label>
  )
}
