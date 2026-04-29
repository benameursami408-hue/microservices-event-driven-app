import clsx from 'clsx'

export default function PageHeader({ eyebrow, title, description, meta, actions, className }) {
  return (
    <section className={clsx('hero-surface fade-in-up', className)}>
      <div className="relative flex flex-col gap-6 xl:flex-row xl:items-end xl:justify-between">
        <div className="max-w-3xl">
          {eyebrow ? <div className="eyebrow">{eyebrow}</div> : null}
          <h1 className="mt-4 text-3xl font-bold text-slate-950 sm:text-4xl">{title}</h1>
          {description ? <p className="mt-3 max-w-2xl text-sm leading-7 text-slate-600 sm:text-base">{description}</p> : null}
          {meta ? <div className="mt-4 flex flex-wrap items-center gap-2 text-sm text-slate-600">{meta}</div> : null}
        </div>

        {actions ? <div className="relative flex flex-wrap gap-3">{actions}</div> : null}
      </div>
    </section>
  )
}
