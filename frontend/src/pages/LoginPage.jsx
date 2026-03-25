import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowRight } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { z } from 'zod'

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
    <div className="surface-solid p-6 sm:p-8">
      <div className="text-2xl font-bold tracking-tight text-slate-900">Sign in</div>
      <div className="mt-1 text-sm text-slate-600">
        Use your account to manage reclamations and notifications.
      </div>

      <form className="mt-6 space-y-4" onSubmit={handleSubmit(onSubmit)}>
        <TextField
          label="Email"
          type="email"
          placeholder="you@example.com"
          autoComplete="email"
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

        {errors.root?.message ? (
          <div className="rounded-xl border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-800">
            {errors.root.message}
          </div>
        ) : null}

        <Button type="submit" className="w-full" disabled={isSubmitting}>
          {isSubmitting ? 'Signing in...' : 'Sign in'}
          <ArrowRight className="h-4 w-4" aria-hidden="true" />
        </Button>
      </form>

      <div className="mt-6 text-sm text-slate-600">
        No account?{' '}
        <Link className="link font-semibold" to="/register">
          Create one
        </Link>
      </div>
    </div>
  )
}
