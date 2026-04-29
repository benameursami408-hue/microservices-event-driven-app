import clsx from 'clsx'

export default function Badge({ children, className }) {
  return (
    <span
      className={clsx(
        'inline-flex items-center rounded-full px-3 py-1.5 text-[11px] font-extrabold uppercase tracking-[0.18em] ring-1 ring-inset shadow-[inset_0_1px_0_rgba(255,255,255,0.75)]',
        className,
      )}
    >
      {children}
    </span>
  )
}
