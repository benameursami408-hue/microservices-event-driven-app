import Spinner from './Spinner.jsx'

export default function LoadingState({ title = 'Chargement...', description }) {
  return (
    <div className="surface-solid p-8">
      <div className="flex flex-col items-center justify-center gap-3 text-center">
        <Spinner label={title} />
        {description ? <p className="max-w-md text-sm text-slate-600">{description}</p> : null}
      </div>
    </div>
  )
}
