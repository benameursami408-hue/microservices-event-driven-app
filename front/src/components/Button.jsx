import clsx from 'clsx'

const VARIANTS = {
  primary:
    'border border-cyan-700/70 bg-linear-to-r from-cyan-700 via-sky-700 to-blue-700 text-white shadow-[0_20px_40px_-24px_rgba(14,116,144,0.85)] hover:-translate-y-0.5 hover:brightness-105 focus-visible:outline-cyan-500 disabled:border-slate-200 disabled:bg-slate-200 disabled:text-slate-500 disabled:shadow-none',
  secondary:
    'border border-slate-200/85 bg-white/90 text-slate-900 shadow-[0_14px_32px_-24px_rgba(15,23,42,0.38)] hover:-translate-y-0.5 hover:bg-slate-50 focus-visible:outline-slate-300',
  ghost: 'border border-transparent bg-transparent text-slate-700 hover:bg-slate-100/80 hover:text-slate-900 focus-visible:outline-slate-300',
  danger:
    'border border-rose-600/80 bg-linear-to-r from-rose-600 to-pink-600 text-white shadow-[0_18px_38px_-24px_rgba(225,29,72,0.7)] hover:-translate-y-0.5 hover:brightness-105 focus-visible:outline-rose-500 disabled:border-rose-100 disabled:bg-rose-100 disabled:text-rose-400 disabled:shadow-none',
}

const SIZES = {
  sm: 'h-10 px-4 text-sm',
  md: 'h-11 px-5 text-sm',
  lg: 'h-12 px-6 text-base',
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
        'inline-flex items-center justify-center gap-2 rounded-2xl font-semibold transition duration-200 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 disabled:cursor-not-allowed disabled:translate-y-0',
        VARIANTS[variant] || VARIANTS.primary,
        SIZES[size] || SIZES.md,
        className,
      )}
      {...props}
    />
  )
}
