import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowLeft, CheckCircle2 } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate } from 'react-router-dom'
import { z } from 'zod'
import toast from 'react-hot-toast'

import Button from '../components/Button.jsx'
import SelectField from '../components/SelectField.jsx'
import TextAreaField from '../components/TextAreaField.jsx'
import TextField from '../components/TextField.jsx'
import { useAuth } from '../hooks/useAuth.js'
import { createReclamation } from '../services/reclamations.service.js'
import { Priority, priorityLabel } from '../utils/enums.js'

const schema = z.object({
  description: z.string().min(1, 'Description is required.').max(500),
  priority: z.coerce.number().int().min(0).max(3),
  savId: z.coerce.number().int().min(0),
  savName: z.string().min(1, 'SAV name is required.').max(100),
})

export default function ReclamationCreatePage() {
  const { user } = useAuth()
  const navigate = useNavigate()

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    setError,
  } = useForm({
    resolver: zodResolver(schema),
    defaultValues: {
      description: '',
      priority: Priority.MEDUIM,
      savId: 0,
      savName: 'Unassigned',
    },
  })

  async function onSubmit(values) {
    try {
      const clientId = user?.id ? Number(user.id) : 0
      const clientName = `${user?.firstName ?? ''} ${user?.lastName ?? ''}`.trim() || user?.email || 'Client'

      const created = await createReclamation({
        description: values.description,
        priority: values.priority,
        clientId,
        clientName,
        savId: values.savId,
        savName: values.savName,
      })

      toast.success('Reclamation created.')
      navigate(`/app/reclamations/${created.id}`, { replace: true })
    } catch (err) {
      const message = err?.response?.data?.detail || err?.message
      setError('root', { message: message || 'Failed to create reclamation.' })
    }
  }

  return (
    <div className="space-y-6">
      <div className="surface-solid p-6">
        <div className="flex items-start justify-between gap-4">
          <div>
            <div className="text-sm font-semibold text-cyan-800">Reclamations</div>
            <div className="mt-1 text-2xl font-bold tracking-tight text-slate-900">New reclamation</div>
            <div className="mt-1 text-sm text-slate-600">
              This request will go through the API Gateway at <span className="font-semibold">localhost:5000</span>.
            </div>
          </div>
          <Link to="/app/reclamations">
            <Button variant="secondary">
              <ArrowLeft className="h-4 w-4" aria-hidden="true" />
              Back
            </Button>
          </Link>
        </div>
      </div>

      <form className="surface-solid p-6 space-y-4" onSubmit={handleSubmit(onSubmit)}>
        <TextAreaField
          label="Description"
          placeholder="Describe the issue..."
          error={errors.description?.message}
          {...register('description')}
        />

        <SelectField label="Priority" error={errors.priority?.message} {...register('priority')}>
          <option value={Priority.LOW}>{priorityLabel(Priority.LOW)}</option>
          <option value={Priority.MEDUIM}>{priorityLabel(Priority.MEDUIM)}</option>
          <option value={Priority.HIGH}>{priorityLabel(Priority.HIGH)}</option>
          <option value={Priority.URGENT}>{priorityLabel(Priority.URGENT)}</option>
        </SelectField>

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <TextField label="SAV Id" type="number" error={errors.savId?.message} {...register('savId')} />
          <TextField label="SAV Name" error={errors.savName?.message} {...register('savName')} />
        </div>

        {errors.root?.message ? (
          <div className="rounded-xl border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-800">
            {errors.root.message}
          </div>
        ) : null}

        <div className="flex flex-col gap-2 sm:flex-row sm:justify-end">
          <Button type="submit" disabled={isSubmitting}>
            <CheckCircle2 className="h-4 w-4" aria-hidden="true" />
            {isSubmitting ? 'Creating...' : 'Create'}
          </Button>
        </div>
      </form>
    </div>
  )
}
