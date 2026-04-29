import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowRight, UserPlus } from 'lucide-react'
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
    <div className="surface-solid fade-in-up overflow-hidden p-6 sm:p-8">
      <div className="flex items-start justify-between gap-4">
        <div>
          <div className="eyebrow">Client onboarding</div>
          <h2 className="mt-4 font-display text-3xl font-bold text-slate-950">Create your SAV account</h2>
          <p className="mt-3 text-sm leading-6 text-slate-600">
            Register as a client to submit complaints, upload purchase proof and track progress.
          </p>
        </div>
        <div className="grid h-14 w-14 place-items-center rounded-[22px] bg-linear-to-br from-cyan-700 to-sky-700 text-white shadow-[0_18px_40px_-24px_rgba(14,116,144,0.8)]">
          <UserPlus className="h-6 w-6" aria-hidden="true" />
        </div>
      </div>

      <form className="mt-8 space-y-5" onSubmit={handleSubmit(onSubmit)}>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <TextField label="First name" error={errors.firstName?.message} {...register('firstName')} />
          <TextField label="Last name" error={errors.lastName?.message} {...register('lastName')} />
        </div>

        <TextField
          label="Phone number"
          placeholder="0600000000"
          hint="Used for appointment follow-up if needed."
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

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
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
        </div>

        {errors.root?.message ? <div className="notice-error">{errors.root.message}</div> : null}

        <Button type="submit" className="w-full" size="lg" disabled={isSubmitting}>
          {isSubmitting ? 'Creating...' : 'Create account'}
          <ArrowRight className="h-4 w-4" aria-hidden="true" />
        </Button>
      </form>

      <div className="mt-8 text-sm text-slate-600">
        Already have an account?{' '}
        <Link className="link font-semibold" to="/login">
          Sign in
        </Link>
      </div>
    </div>
  )
}
