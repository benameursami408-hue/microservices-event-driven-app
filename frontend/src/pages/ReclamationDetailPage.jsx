import { zodResolver } from '@hookform/resolvers/zod'
import { AlertTriangle, Ban, CalendarClock, CheckCircle2, Play, Save, UserCheck, XCircle } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useParams } from 'react-router-dom'
import toast from 'react-hot-toast'
import { z } from 'zod'

import Badge from '../components/Badge.jsx'
import Button from '../components/Button.jsx'
import SelectField from '../components/SelectField.jsx'
import Spinner from '../components/Spinner.jsx'
import TextAreaField from '../components/TextAreaField.jsx'
import TextField from '../components/TextField.jsx'
import { listUsers } from '../services/adminUsers.service.js'
import {
  assignReclamation,
  cancelReclamation,
  closeReclamation,
  getReclamation,
  getReclamationHistory,
  planReclamation,
  rejectReclamation,
  resolveReclamation,
  startReclamation,
  updateReclamation,
} from '../services/reclamations.service.js'
import { formatDateTime } from '../utils/format.js'
import {
  Priority,
  priorityBadgeClasses,
  priorityLabel,
  reclamationStatusBadgeClasses,
  reclamationStatusLabel,
} from '../utils/enums.js'

const updateSchema = z.object({
  description: z.string().min(1, 'Description is required.').max(500),
  priority: z.coerce.number().int().min(0).max(3),
})

export default function ReclamationDetailPage() {
  const { id } = useParams()

  const [item, setItem] = useState(null)
  const [history, setHistory] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const [actionBusy, setActionBusy] = useState(false)
  const [rejectReason, setRejectReason] = useState('')
  const [closeComment, setCloseComment] = useState('')
  const [resolutionNote, setResolutionNote] = useState('')

  const [planningNote, setPlanningNote] = useState('')
  const [plannedStart, setPlannedStart] = useState('')
  const [plannedEnd, setPlannedEnd] = useState('')
  const [technicianId, setTechnicianId] = useState('')
  const [technicianName, setTechnicianName] = useState('')
  const [technicians, setTechnicians] = useState([])
  const [techLoading, setTechLoading] = useState(false)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    reset,
    setError: setFormError,
  } = useForm({
    resolver: zodResolver(updateSchema),
    defaultValues: {
      description: '',
      priority: Priority.MEDUIM,
    },
  })

  async function reload() {
    setLoading(true)
    setError(null)
    try {
      const data = await getReclamation(id)
      setItem(data)
      reset({
        description: data.description ?? '',
        priority: data.priority ?? Priority.MEDUIM,
      })

      try {
        const hist = await getReclamationHistory(id)
        setHistory(Array.isArray(hist) ? hist : [])
      } catch {
        setHistory([])
      }
    } catch (err) {
      const message = err?.response?.data?.detail || err?.message
      setError(message || 'Failed to load reclamation.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    reload()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id])

  useEffect(() => {
    async function loadTechnicians() {
      if (!item) return
      const actions = (item.allowedActions ?? []).map((x) => String(x).toUpperCase())
      if (!actions.includes('PLAN')) return

      setTechLoading(true)
      try {
        const data = await listUsers({ role: 'ST' })
        setTechnicians(Array.isArray(data) ? data : [])
      } catch {
        setTechnicians([])
      } finally {
        setTechLoading(false)
      }
    }

    loadTechnicians()
  }, [item])

  async function onUpdate(values) {
    try {
      const updated = await updateReclamation(id, values)
      setItem(updated)
      toast.success('Saved.')
    } catch (err) {
      const message = err?.response?.data?.detail || err?.message
      setFormError('root', { message: message || 'Update failed.' })
    }
  }

  async function runAction(fn, successMessage) {
    if (actionBusy) return
    setActionBusy(true)
    try {
      const updated = await fn()
      if (updated) setItem(updated)
      await reload()
      if (successMessage) toast.success(successMessage)
    } catch (err) {
      const message = err?.response?.data?.detail || err?.response?.data?.title || err?.message
      toast.error(message || 'Action failed.')
    } finally {
      setActionBusy(false)
    }
  }

  const allowedActions = useMemo(
    () => new Set((item?.allowedActions ?? []).map((x) => String(x).toUpperCase())),
    [item],
  )

  const canEditDetails = allowedActions.has('EDIT')
  const canAssign = allowedActions.has('ASSIGN')
  const canPlan = allowedActions.has('PLAN')
  const canStart = allowedActions.has('START')
  const canResolve = allowedActions.has('RESOLVE')
  const canClose = allowedActions.has('CLOSE')
  const canCancel = allowedActions.has('CANCEL')
  const canReject = allowedActions.has('REJECT')

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
                <Badge className={reclamationStatusBadgeClasses(item.status)}>
                  {reclamationStatusLabel(item.status)}
                </Badge>
                <span className="text-slate-300">|</span>
                <span className="font-semibold text-slate-800">Client:</span> {item.clientName}
              </div>
            ) : null}
          </div>

          <div className="flex gap-2">
            <Link to="/app/reclamations">
              <Button variant="secondary">Back</Button>
            </Link>
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
          <div className="space-y-6 lg:col-span-2">
            <div className="surface-solid p-6">
              <div className="text-lg font-bold text-slate-900">Details</div>
              <form className="mt-4 space-y-4" onSubmit={handleSubmit(onUpdate)}>
                <TextAreaField
                  label="Description"
                  error={errors.description?.message}
                  disabled={!canEditDetails}
                  {...register('description')}
                />

                <SelectField
                  label="Priority"
                  error={errors.priority?.message}
                  disabled={!canEditDetails}
                  {...register('priority')}
                >
                  <option value={Priority.LOW}>{priorityLabel(Priority.LOW)}</option>
                  <option value={Priority.MEDUIM}>{priorityLabel(Priority.MEDUIM)}</option>
                  <option value={Priority.HIGH}>{priorityLabel(Priority.HIGH)}</option>
                  <option value={Priority.URGENT}>{priorityLabel(Priority.URGENT)}</option>
                </SelectField>

                {errors.root?.message ? (
                  <div className="rounded-xl border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-800">
                    {errors.root.message}
                  </div>
                ) : null}

                <div className="flex justify-end">
                  <Button type="submit" disabled={isSubmitting || !canEditDetails}>
                    <Save className="h-4 w-4" aria-hidden="true" />
                    {isSubmitting ? 'Saving...' : 'Save changes'}
                  </Button>
                </div>
              </form>
            </div>

            <div className="surface-solid p-6">
              <div className="text-lg font-bold text-slate-900">Workflow</div>
              <div className="mt-4 space-y-5">
                {canAssign ? (
                  <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                    <div className="text-sm font-semibold text-slate-900">Assign</div>
                    <div className="mt-1 text-sm text-slate-600">Take ownership as SAV (or ADMIN).</div>
                    <div className="mt-3">
                      <Button
                        onClick={() =>
                          runAction(() => assignReclamation(id, { comment: 'Assigned from UI' }), 'Assigned.')
                        }
                        disabled={actionBusy}
                      >
                        <UserCheck className="h-4 w-4" aria-hidden="true" />
                        Assign to me
                      </Button>
                    </div>
                  </div>
                ) : null}

                {canPlan ? (
                  <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                    <div className="text-sm font-semibold text-slate-900">Plan intervention</div>
                    <div className="mt-1 text-sm text-slate-600">Schedule date/time and assign a technician.</div>

                    <form
                      className="mt-3 grid grid-cols-1 gap-3 sm:grid-cols-2"
                      onSubmit={(e) => {
                        e.preventDefault()

                        const tId = Number(technicianId)
                        if (!tId) {
                          toast.error('Technician id is required.')
                          return
                        }

                        if (!plannedStart) {
                          toast.error('Planned start time is required.')
                          return
                        }

                        const payload = {
                          technicianId: tId,
                          technicianName: technicianName || undefined,
                          plannedStartAt: new Date(plannedStart).toISOString(),
                          plannedEndAt: plannedEnd ? new Date(plannedEnd).toISOString() : null,
                          planningNote: planningNote || undefined,
                        }

                        runAction(() => planReclamation(id, payload), 'Planned.')
                      }}
                    >
                      <div className="sm:col-span-2">
                        <label className="text-sm font-semibold text-slate-900">Technician</label>
                        <div className="mt-1 grid grid-cols-1 gap-2 sm:grid-cols-2">
                          <SelectField
                            label="Technician (from AuthService)"
                            value={technicianId}
                            onChange={(e) => {
                              const nextId = e.target.value
                              setTechnicianId(nextId)

                              const found = technicians.find((x) => String(x.id) === String(nextId))
                              if (found) {
                                const full = `${found.firstName ?? ''} ${found.lastName ?? ''}`.trim()
                                setTechnicianName(full || found.email || '')
                              }
                            }}
                            disabled={techLoading || technicians.length === 0}
                          >
                            <option value="">{techLoading ? 'Loading...' : 'Select technician'}</option>
                            {technicians.map((t) => (
                              <option key={t.id} value={t.id}>
                                {`${t.firstName ?? ''} ${t.lastName ?? ''}`.trim() || t.email || `#${t.id}`}
                              </option>
                            ))}
                          </SelectField>

                          <TextField
                            label="Technician id"
                            type="number"
                            value={technicianId}
                            onChange={(e) => setTechnicianId(e.target.value)}
                          />
                        </div>
                      </div>

                      <TextField
                        label="Planned start"
                        type="datetime-local"
                        value={plannedStart}
                        onChange={(e) => setPlannedStart(e.target.value)}
                      />

                      <TextField
                        label="Planned end (optional)"
                        type="datetime-local"
                        value={plannedEnd}
                        onChange={(e) => setPlannedEnd(e.target.value)}
                      />

                      <div className="sm:col-span-2">
                        <TextField
                          label="Planning note (optional)"
                          value={planningNote}
                          onChange={(e) => setPlanningNote(e.target.value)}
                        />
                      </div>

                      <div className="sm:col-span-2 flex justify-end">
                        <Button type="submit" disabled={actionBusy}>
                          <CalendarClock className="h-4 w-4" aria-hidden="true" />
                          Plan
                        </Button>
                      </div>
                    </form>
                  </div>
                ) : null}

                {canStart ? (
                  <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                    <div className="text-sm font-semibold text-slate-900">Start</div>
                    <div className="mt-1 text-sm text-slate-600">Technician starts the intervention.</div>
                    <div className="mt-3">
                      <Button onClick={() => runAction(() => startReclamation(id), 'Started.')} disabled={actionBusy}>
                        <Play className="h-4 w-4" aria-hidden="true" />
                        Start
                      </Button>
                    </div>
                  </div>
                ) : null}

                {canResolve ? (
                  <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                    <div className="text-sm font-semibold text-slate-900">Resolve</div>
                    <div className="mt-1 text-sm text-slate-600">Add a resolution note and mark as resolved.</div>
                    <form
                      className="mt-3 space-y-3"
                      onSubmit={(e) => {
                        e.preventDefault()
                        if (!resolutionNote.trim()) {
                          toast.error('Resolution note is required.')
                          return
                        }
                        runAction(() => resolveReclamation(id, { resolutionNote }), 'Resolved.')
                      }}
                    >
                      <TextAreaField
                        label="Resolution note"
                        value={resolutionNote}
                        onChange={(e) => setResolutionNote(e.target.value)}
                      />
                      <div className="flex justify-end">
                        <Button type="submit" disabled={actionBusy}>
                          <CheckCircle2 className="h-4 w-4" aria-hidden="true" />
                          Resolve
                        </Button>
                      </div>
                    </form>
                  </div>
                ) : null}

                {canClose ? (
                  <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                    <div className="text-sm font-semibold text-slate-900">Close</div>
                    <div className="mt-1 text-sm text-slate-600">SAV closes the resolved reclamation.</div>
                    <form
                      className="mt-3 space-y-3"
                      onSubmit={(e) => {
                        e.preventDefault()
                        runAction(() => closeReclamation(id, { comment: closeComment || undefined }), 'Closed.')
                      }}
                    >
                      <TextField
                        label="Comment (optional)"
                        value={closeComment}
                        onChange={(e) => setCloseComment(e.target.value)}
                      />
                      <div className="flex justify-end">
                        <Button type="submit" disabled={actionBusy}>
                          <CheckCircle2 className="h-4 w-4" aria-hidden="true" />
                          Close
                        </Button>
                      </div>
                    </form>
                  </div>
                ) : null}

                {canCancel ? (
                  <div className="rounded-2xl border border-rose-200 bg-rose-50 p-4">
                    <div className="text-sm font-semibold text-rose-900">Cancel</div>
                    <div className="mt-1 text-sm text-rose-800">Client can cancel only while OPEN.</div>
                    <div className="mt-3">
                      <Button
                        variant="danger"
                        onClick={() => {
                          const ok = window.confirm('Cancel this reclamation?')
                          if (!ok) return
                          runAction(() => cancelReclamation(id), 'Cancelled.')
                        }}
                        disabled={actionBusy}
                      >
                        <XCircle className="h-4 w-4" aria-hidden="true" />
                        Cancel reclamation
                      </Button>
                    </div>
                  </div>
                ) : null}

                {canReject ? (
                  <div className="rounded-2xl border border-rose-200 bg-rose-50 p-4">
                    <div className="text-sm font-semibold text-rose-900">Reject</div>
                    <div className="mt-1 text-sm text-rose-800">Reject with a clear reason.</div>
                    <form
                      className="mt-3 space-y-3"
                      onSubmit={(e) => {
                        e.preventDefault()
                        if (!rejectReason.trim()) {
                          toast.error('Reason is required.')
                          return
                        }
                        runAction(() => rejectReclamation(id, { reason: rejectReason }), 'Rejected.')
                      }}
                    >
                      <TextField
                        label="Reason"
                        value={rejectReason}
                        onChange={(e) => setRejectReason(e.target.value)}
                      />
                      <div className="flex justify-end">
                        <Button variant="danger" type="submit" disabled={actionBusy}>
                          <Ban className="h-4 w-4" aria-hidden="true" />
                          Reject
                        </Button>
                      </div>
                    </form>
                  </div>
                ) : null}

                {!canAssign &&
                !canPlan &&
                !canStart &&
                !canResolve &&
                !canClose &&
                !canCancel &&
                !canReject ? (
                  <div className="text-sm text-slate-600">No actions available for your role at this status.</div>
                ) : null}
              </div>
            </div>

            <div className="surface-solid p-6">
              <div className="text-lg font-bold text-slate-900">History</div>
              {history.length === 0 ? (
                <div className="mt-3 text-sm text-slate-600">No history yet.</div>
              ) : (
                <div className="mt-4 space-y-3">
                  {history.map((h) => (
                    <div key={h.id} className="rounded-2xl border border-slate-200 bg-white p-4">
                      <div className="flex flex-wrap items-center gap-2 text-sm">
                        <Badge className={reclamationStatusBadgeClasses(h.fromStatus)}>
                          {reclamationStatusLabel(h.fromStatus)}
                        </Badge>
                        <span className="text-slate-400">→</span>
                        <Badge className={reclamationStatusBadgeClasses(h.toStatus)}>
                          {reclamationStatusLabel(h.toStatus)}
                        </Badge>
                        <span className="text-slate-300">|</span>
                        <span className="font-semibold text-slate-800">{h.actorRole}</span>
                        <span className="text-slate-500">#{h.actorUserId}</span>
                      </div>
                      <div className="mt-2 text-xs text-slate-600">{formatDateTime(h.occurredAt)}</div>
                      {h.comment ? <div className="mt-2 text-sm text-slate-700">{h.comment}</div> : null}
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>

          <div className="space-y-6">
            <div className="surface-solid p-6">
              <div className="text-lg font-bold text-slate-900">Overview</div>
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
                    {item.savName || 'Unassigned'} {item.savId ? `(ID: ${item.savId})` : ''}
                  </div>
                </div>
                <div>
                  <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Technician</div>
                  <div className="mt-1 font-semibold text-slate-900">
                    {item.technicianName || 'Unassigned'} {item.technicianId ? `(ID: ${item.technicianId})` : ''}
                  </div>
                </div>
                <div>
                  <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Planned</div>
                  <div className="mt-1 font-semibold text-slate-900">
                    {item.plannedStartAt ? formatDateTime(item.plannedStartAt) : '—'}
                    {item.plannedEndAt ? ` → ${formatDateTime(item.plannedEndAt)}` : ''}
                  </div>
                </div>

                {item.resolutionNote ? (
                  <div>
                    <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Resolution</div>
                    <div className="mt-1 whitespace-pre-wrap text-slate-900">{item.resolutionNote}</div>
                  </div>
                ) : null}

                {item.rejectionReason ? (
                  <div>
                    <div className="text-xs font-semibold uppercase tracking-wider text-rose-700">Rejection reason</div>
                    <div className="mt-1 whitespace-pre-wrap text-rose-900">{item.rejectionReason}</div>
                  </div>
                ) : null}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
