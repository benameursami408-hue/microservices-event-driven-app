import clsx from 'clsx'

const VARIANTS = {
  primary:
    'bg-cyan-700 text-white hover:bg-cyan-800 focus-visible:outline-cyan-600 disabled:bg-slate-200 disabled:text-slate-500',
  secondary:
    'bg-white text-slate-900 ring-1 ring-slate-200 hover:bg-slate-50 focus-visible:outline-slate-300',
  ghost: 'bg-transparent text-slate-800 hover:bg-slate-100 focus-visible:outline-slate-300',
  danger: 'bg-rose-600 text-white hover:bg-rose-700 focus-visible:outline-rose-500',
}

const SIZES = {
  sm: 'h-9 px-3 text-sm',
  md: 'h-10 px-4 text-sm',
  lg: 'h-11 px-5 text-base',
}

export default function Button({
  variant = 'primary',
  size = 'md',
  className,
  type = 'button',
  ...props
}) {
  return (
    <button
      type={type}
      className={clsx(
        'inline-flex items-center justify-center gap-2 rounded-xl font-semibold shadow-sm transition focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 disabled:cursor-not-allowed',
        VARIANTS[variant] || VARIANTS.primary,
        SIZES[size] || SIZES.md,
        className,
      )}
      {...props}
    />
  )
}
