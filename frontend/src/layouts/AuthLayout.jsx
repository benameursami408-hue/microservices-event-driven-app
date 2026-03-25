import { Outlet } from 'react-router-dom'

export default function AuthLayout() {
  return (
    <div className="min-h-screen">
      <div className="mx-auto grid min-h-screen max-w-6xl grid-cols-1 lg:grid-cols-2">
        <div className="hidden lg:flex flex-col justify-between p-12">
          <div className="surface p-8">
            <div className="text-sm font-semibold text-cyan-800">PFE.NET</div>
            <div className="mt-3 text-3xl font-bold tracking-tight text-slate-900">
              Microservices Portal
            </div>
            <p className="mt-3 text-slate-600">
              Auth, reclamations, and notifications behind a single API Gateway.
            </p>
          </div>

          <div className="text-sm text-slate-600">
            Built on ASP.NET Core, MassTransit, RabbitMQ, Docker, and Ocelot.
          </div>
        </div>

        <div className="flex items-center justify-center p-6 lg:p-12">
          <div className="w-full max-w-md">
            <Outlet />
          </div>
        </div>
      </div>
    </div>
  )
}
