import { zodResolver } from '@hookform/resolvers/zod'
import { AlertTriangle, Save, Trash2 } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate, useParams } from 'react-router-dom'
import toast from 'react-hot-toast'
import { z } from 'zod'

import Badge from '../components/Badge.jsx'
import Button from '../components/Button.jsx'
import SelectField from '../components/SelectField.jsx'
import Spinner from '../components/Spinner.jsx'
import TextAreaField from '../components/TextAreaField.jsx'
import TextField from '../components/TextField.jsx'
import { deleteReclamation, getReclamation, updateReclamation } from '../services/reclamations.service.js'
import { formatDateTime } from '../utils/format.js'
import { Priority, priorityBadgeClasses, priorityLabel } from '../utils/enums.js'

const schema = z.object({
  description: z.string().min(1, 'Description is required.').max(500),
  priority: z.coerce.number().int().min(0).max(3),
  status: z.string().min(1, 'Status is required.').max(30),
})

export default function ReclamationDetailPage() {
  const { id } = useParams()
  const navigate = useNavigate()

  const [item, setItem] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    reset,
    setError: setFormError,
  } = useForm({
    resolver: zodResolver(schema),
    defaultValues: {
      description: '',
      priority: Priority.MEDUIM,
      status: 'Ouverte',
    },
  })

  useEffect(() => {
    async function load() {
      setLoading(true)
      setError(null)
      try {
        const data = await getReclamation(id)
        setItem(data)
        reset({
          description: data.description ?? '',
          priority: data.priority ?? Priority.MEDUIM,
          status: data.status ?? 'Ouverte',
        })
      } catch (err) {
        const message = err?.response?.data?.detail || err?.message
        setError(message || 'Failed to load reclamation.')
      } finally {
        setLoading(false)
      }
    }

    load()
  }, [id, reset])

  async function onSubmit(values) {
    try {
      const updated = await updateReclamation(id, values)
      setItem(updated)
      toast.success('Reclamation updated.')
    } catch (err) {
      const message = err?.response?.data?.detail || err?.message
      setFormError('root', { message: message || 'Update failed.' })
    }
  }

  async function onDelete() {
    const ok = window.confirm('Delete this reclamation? This cannot be undone.')
    if (!ok) return

    try {
      await deleteReclamation(id)
      toast.success('Reclamation deleted.')
      navigate('/app/reclamations', { replace: true })
    } catch (err) {
      const message = err?.response?.data?.detail || err?.message
      toast.error(message || 'Delete failed.')
    }
  }

  return (
    <div className="space-y-6">
      <div className="surface-solid p-6">
        <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <div className="text-sm font-semibold text-cyan-800">Reclamation</div>
            <div className="mt-1 text-2xl font-bold tracking-tight text-slate-900">
              {item?.reference || `#${id}`}
            </div>
            {item ? (
              <div className="mt-2 flex flex-wrap items-center gap-2 text-sm text-slate-600">
                <Badge className={priorityBadgeClasses(item.priority)}>{priorityLabel(item.priority)}</Badge>
                <span className="text-slate-300">|</span>
                <span className="font-semibold text-slate-800">Status:</span> {item.status}
                <span className="text-slate-300">|</span>
                <span className="font-semibold text-slate-800">Client:</span> {item.clientName}
              </div>
            ) : null}
          </div>

          <div className="flex gap-2">
            <Link to="/app/reclamations">
              <Button variant="secondary">Back</Button>
            </Link>
            <Button variant="danger" onClick={onDelete}>
              <Trash2 className="h-4 w-4" aria-hidden="true" />
              Delete
            </Button>
          </div>
        </div>
      </div>

      {loading ? (
        <div className="surface-solid p-8">
          <Spinner label="Loading reclamation..." />
        </div>
      ) : error ? (
        <div className="surface-solid p-6">
          <div className="flex items-start gap-3 rounded-xl border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-800">
            <AlertTriangle className="mt-0.5 h-4 w-4" aria-hidden="true" />
            <div>{error}</div>
          </div>
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
          <div className="surface-solid p-6 lg:col-span-2">
            <div className="text-lg font-bold text-slate-900">Update</div>
            <form className="mt-4 space-y-4" onSubmit={handleSubmit(onSubmit)}>
              <TextAreaField
                label="Description"
                error={errors.description?.message}
                {...register('description')}
              />

              <SelectField label="Priority" error={errors.priority?.message} {...register('priority')}>
                <option value={Priority.LOW}>{priorityLabel(Priority.LOW)}</option>
                <option value={Priority.MEDUIM}>{priorityLabel(Priority.MEDUIM)}</option>
                <option value={Priority.HIGH}>{priorityLabel(Priority.HIGH)}</option>
                <option value={Priority.URGENT}>{priorityLabel(Priority.URGENT)}</option>
              </SelectField>

              <TextField label="Status" error={errors.status?.message} {...register('status')} />

              {errors.root?.message ? (
                <div className="rounded-xl border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-800">
                  {errors.root.message}
                </div>
              ) : null}

              <div className="flex justify-end">
                <Button type="submit" disabled={isSubmitting}>
                  <Save className="h-4 w-4" aria-hidden="true" />
                  {isSubmitting ? 'Saving...' : 'Save changes'}
                </Button>
              </div>
            </form>
          </div>

          <div className="surface-solid p-6">
            <div className="text-lg font-bold text-slate-900">Details</div>
            <div className="mt-4 space-y-3 text-sm">
              <div>
                <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Created</div>
                <div className="mt-1 font-semibold text-slate-900">{formatDateTime(item.createdAt)}</div>
              </div>
              <div>
                <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Updated</div>
                <div className="mt-1 font-semibold text-slate-900">{formatDateTime(item.updatedAt)}</div>
              </div>
              <div>
                <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Client</div>
                <div className="mt-1 font-semibold text-slate-900">
                  {item.clientName} (ID: {item.clientId})
                </div>
              </div>
              <div>
                <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">SAV</div>
                <div className="mt-1 font-semibold text-slate-900">
                  {item.savName} (ID: {item.savId})
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
