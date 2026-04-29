import clsx from 'clsx'

export default function TextAreaField({ label, hint, error, className, rows = 5, ...props }) {
  const isRequired = Boolean(props.required)

  return (
    <label className={clsx('input-shell block', className)}>
      {label ? (
        <div className="input-label">
          {label}
          {isRequired ? <span className="ml-1 text-rose-600">*</span> : null}
        </div>
      ) : null}
      <textarea
        rows={rows}
        className={clsx(
          'input-control min-h-32 resize-y',
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
