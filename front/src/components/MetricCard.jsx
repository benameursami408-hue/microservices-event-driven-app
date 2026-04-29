import clsx from 'clsx'

const TONES = {
  cyan: {
    icon: 'bg-cyan-50 text-cyan-700 ring-cyan-100',
    helper: 'text-cyan-700',
  },
  amber: {
    icon: 'bg-amber-50 text-amber-700 ring-amber-100',
    helper: 'text-amber-700',
  },
  emerald: {
    icon: 'bg-emerald-50 text-emerald-700 ring-emerald-100',
    helper: 'text-emerald-700',
  },
  rose: {
    icon: 'bg-rose-50 text-rose-700 ring-rose-100',
    helper: 'text-rose-700',
  },
  slate: {
    icon: 'bg-slate-100 text-slate-700 ring-slate-200',
    helper: 'text-slate-600',
  },
}

export default function MetricCard({ icon: Icon, label, value, helper, tone = 'cyan', className }) {
  const palette = TONES[tone] || TONES.cyan

  return (
    <div className={clsx('metric-card fade-in-up', className)}>
      <div className="flex items-start justify-between gap-4">
        <div>
          <div className="text-[11px] font-extrabold uppercase tracking-[0.22em] text-slate-500">{label}</div>
          <div className="metric-value">{value}</div>
          {helper ? <div className={clsx('mt-2 text-sm font-semibold', palette.helper)}>{helper}</div> : null}
        </div>
        {Icon ? (
          <div className={clsx('grid h-12 w-12 place-items-center rounded-2xl ring-1 ring-inset shadow-[inset_0_1px_0_rgba(255,255,255,0.65)]', palette.icon)}>
            <Icon className="h-5 w-5" aria-hidden="true" />
          </div>
        ) : null}
      </div>
    </div>
  )
}
