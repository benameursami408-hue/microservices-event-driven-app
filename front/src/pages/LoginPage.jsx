import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowRight, ShieldCheck } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { z } from 'zod'

import Badge from '../components/Badge.jsx'
import Button from '../components/Button.jsx'
import TextField from '../components/TextField.jsx'
import { useAuth } from '../hooks/useAuth.js'

const schema = z.object({
  email: z.string().email('Enter a valid email.'),
  password: z.string().min(1, 'Password is required.'),
})

export default function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    setError,
  } = useForm({
    resolver: zodResolver(schema),
    defaultValues: {
      email: '',
      password: '',
    },
  })

  async function onSubmit(values) {
    try {
      await login(values)
      const next = location.state?.from || '/app'
      navigate(next, { replace: true })
    } catch (err) {
      const message = err?.response?.data?.detail || err?.response?.data?.title || err?.message
      setError('root', { message: message || 'Login failed.' })
    }
  }

  return (
    <div className="surface-solid fade-in-up overflow-hidden p-6 sm:p-8">
      <div className="flex items-start justify-between gap-4">
        <div>
          <div className="eyebrow">Authentication</div>
          <h2 className="mt-4 font-display text-3xl font-bold text-slate-950">Sign in to the SAV portal</h2>
          <p className="mt-3 text-sm leading-6 text-slate-600">
            Access your operational dashboard to process reclamations, scheduling and notifications.
          </p>
        </div>
        <div className="grid h-14 w-14 place-items-center rounded-[22px] bg-slate-950 text-white shadow-[0_18px_40px_-24px_rgba(15,23,42,0.8)]">
          <ShieldCheck className="h-6 w-6" aria-hidden="true" />
        </div>
      </div>

      <div className="mt-6 flex flex-wrap gap-2">
        <Badge className="bg-cyan-50 text-cyan-800 ring-cyan-200">JWT secured</Badge>
        <Badge className="bg-amber-50 text-amber-800 ring-amber-200">Role-aware workspace</Badge>
      </div>

      <form className="mt-8 space-y-5" onSubmit={handleSubmit(onSubmit)}>
        <TextField
          label="Email"
          type="email"
          placeholder="you@example.com"
          autoComplete="email"
          hint="Use the account created in AuthService."
          error={errors.email?.message}
          {...register('email')}
        />

        <TextField
          label="Password"
          type="password"
          placeholder="Your password"
          autoComplete="current-password"
          error={errors.password?.message}
          {...register('password')}
        />

        {errors.root?.message ? <div className="notice-error">{errors.root.message}</div> : null}

        <Button type="submit" className="w-full" size="lg" disabled={isSubmitting}>
          {isSubmitting ? 'Signing in...' : 'Sign in'}
          <ArrowRight className="h-4 w-4" aria-hidden="true" />
        </Button>
      </form>

      <div className="mt-8 rounded-[24px] border border-slate-200/80 bg-slate-50/80 p-4 text-sm text-slate-600">
        <div className="font-semibold text-slate-900">New here?</div>
        <div className="mt-1 leading-6">
          Create a client account to open a reclamation and follow the full SAV workflow.
        </div>
        <Link className="link mt-3 inline-flex font-semibold" to="/register">
          Create one
        </Link>
      </div>
    </div>
  )
}
