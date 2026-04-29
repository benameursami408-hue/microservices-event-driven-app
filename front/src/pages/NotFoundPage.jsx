import { Link } from 'react-router-dom'
import Button from '../components/Button.jsx'

export default function NotFoundPage() {
  return (
    <div className="min-h-screen grid place-items-center p-6">
      <div className="surface-solid max-w-lg p-8 text-center">
        <div className="text-sm font-semibold text-cyan-800">404</div>
        <div className="mt-2 text-3xl font-bold tracking-tight text-slate-900">Page not found</div>
        <div className="mt-2 text-sm text-slate-600">
          The page you requested does not exist.
        </div>
        <div className="mt-6 flex justify-center">
          <Link to="/app">
            <Button>Go to dashboard</Button>
          </Link>
        </div>
      </div>
    </div>
  )
}
