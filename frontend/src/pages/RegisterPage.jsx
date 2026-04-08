import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowRight } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate } from 'react-router-dom'
import { z } from 'zod'

import Button from '../components/Button.jsx'
import TextField from '../components/TextField.jsx'
import { useAuth } from '../hooks/useAuth.js'

const schema = z
  .object({
    firstName: z.string().min(1, 'First name is required.').max(50),
    lastName: z.string().min(1, 'Last name is required.').max(50),
    phoneNumber: z.string().min(6, 'Phone number is required.').max(20),
    address: z.string().max(250).optional(),
    email: z.string().email('Enter a valid email.'),
    password: z.string().min(8, 'Password must be at least 8 characters.'),
    confirmPassword: z.string().min(1, 'Please confirm your password.'),
  })
  .refine((v) => v.password === v.confirmPassword, {
    message: 'Passwords do not match.',
    path: ['confirmPassword'],
  })

export default function RegisterPage() {
  const { register: registerAccount } = useAuth()
  const navigate = useNavigate()

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    setError,
  } = useForm({
    resolver: zodResolver(schema),
    defaultValues: {
      firstName: '',
      lastName: '',
      phoneNumber: '',
      address: '',
      email: '',
      password: '',
      confirmPassword: '',
    },
  })

  async function onSubmit(values) {
    try {
      await registerAccount({
        firstName: values.firstName,
        lastName: values.lastName,
        phoneNumber: values.phoneNumber,
        address: values.address || '',
        email: values.email,
        password: values.password,
      })
      navigate('/login', { replace: true })
    } catch (err) {
      const message = err?.response?.data?.detail || err?.response?.data?.title || err?.message
      setError('root', { message: message || 'Registration failed.' })
    }
  }

  return (
    <div className="surface-solid p-6 sm:p-8">
      <div className="text-2xl font-bold tracking-tight text-slate-900">Create account</div>
      <div className="mt-1 text-sm text-slate-600">Register as a client and start creating reclamations.</div>

      <form className="mt-6 space-y-4" onSubmit={handleSubmit(onSubmit)}>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <TextField label="First name" error={errors.firstName?.message} {...register('firstName')} />
          <TextField label="Last name" error={errors.lastName?.message} {...register('lastName')} />
        </div>

        <TextField
          label="Phone number"
          placeholder="0600000000"
          error={errors.phoneNumber?.message}
          {...register('phoneNumber')}
        />

        <TextField label="Address" error={errors.address?.message} {...register('address')} />

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
          autoComplete="new-password"
          error={errors.password?.message}
          {...register('password')}
        />

        <TextField
          label="Confirm password"
          type="password"
          autoComplete="new-password"
          error={errors.confirmPassword?.message}
          {...register('confirmPassword')}
        />

        {errors.root?.message ? (
          <div className="rounded-xl border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-800">
            {errors.root.message}
          </div>
        ) : null}

        <Button type="submit" className="w-full" disabled={isSubmitting}>
          {isSubmitting ? 'Creating...' : 'Create account'}
          <ArrowRight className="h-4 w-4" aria-hidden="true" />
        </Button>
      </form>

      <div className="mt-6 text-sm text-slate-600">
        Already have an account?{' '}
        <Link className="link font-semibold" to="/login">
          Sign in
        </Link>
      </div>
    </div>
  )
}
