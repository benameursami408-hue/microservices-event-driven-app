import clsx from 'clsx'

export default function TextField({ label, hint, error, className, ...props }) {
  const isRequired = Boolean(props.required)

  return (
    <label className={clsx('input-shell block', className)}>
      {label ? (
        <div className="input-label">
          {label}
          {isRequired ? <span className="ml-1 text-rose-600">*</span> : null}
        </div>
      ) : null}
      <input
        className={clsx(
          'input-control',
          error && 'border-rose-300 focus:border-rose-500 focus:ring-rose-100',
        )}
        aria-invalid={error ? 'true' : 'false'}
        {...props}
      />
      {error ? <div className="text-sm font-medium text-rose-700">{error}</div> : null}
      {!error && hint ? <div className="input-hint">{hint}</div> : null}
    </label>
  )
}
