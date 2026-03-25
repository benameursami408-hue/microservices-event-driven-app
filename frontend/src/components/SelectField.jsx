import clsx from 'clsx'

export default function SelectField({ label, error, className, children, ...props }) {
  return (
    <label className={clsx('block', className)}>
      {label ? <div className="mb-1 text-sm font-semibold text-slate-800">{label}</div> : null}
      <select
        className={clsx(
          'block w-full rounded-xl border-slate-200 bg-white/80 shadow-sm focus:border-cyan-600 focus:ring-cyan-600',
          error && 'border-rose-300 focus:border-rose-500 focus:ring-rose-500',
        )}
        {...props}
      >
        {children}
      </select>
      {error ? <div className="mt-1 text-sm text-rose-700">{error}</div> : null}
    </label>
  )
}
