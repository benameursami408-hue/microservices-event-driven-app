import { zodResolver } from '@hookform/resolvers/zod'
import { AlertTriangle, Ban, CalendarClock, CheckCircle2, Save, Trash2, Upload, UserCheck, XCircle } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate, useParams } from 'react-router-dom'
import toast from 'react-hot-toast'
import { z } from 'zod'

import Badge from '../components/Badge.jsx'
import Button from '../components/Button.jsx'
import MetricCard from '../components/MetricCard.jsx'
import PageHeader from '../components/PageHeader.jsx'
import SelectField from '../components/SelectField.jsx'
import Spinner from '../components/Spinner.jsx'
import TextAreaField from '../components/TextAreaField.jsx'
import TextField from '../components/TextField.jsx'
import {
  assignReclamation,
  cancelReclamation,
  closeReclamation,
  deleteReclamation,
  getReclamation,
  getReclamationHistory,
  getReclamationPriority,
  getReclamationSla,
  overridePriority,
  recalculatePriority,
  requestPlanning,
  rejectReclamation,
  uploadReclamationFile,
  updateReclamation,
} from '../services/reclamations.service.js'
import { getAppointmentByReclamation, listInterventionsByReclamation } from '../services/intervention.service.js'
import { formatDateTime } from '../utils/format.js'
import {
  Priority,
  priorityBadgeClasses,
  priorityLabel,
  prioritySourceLabel,
  reclamationStatusBadgeClasses,
  reclamationStatusLabel,
  slaStatusBadgeClasses,
  slaStatusLabel,
} from '../utils/enums.js'

const optionalText = (max) => z.string().max(max).optional().or(z.literal(''))

const updateSchema = z.object({
  description: z.string().min(1, 'Description is required.').max(500),
  priority: z.coerce.number().int().min(0).max(3),
  isBlocking: z.boolean().optional(),
  followUpCount: z.coerce.number().int().min(0).max(100),
  productName: optionalText(150),
  barcode: optionalText(64),
  brand: optionalText(100),
  model: optionalText(100),
  serialNumber: optionalText(100),
  productReference: optionalText(100),
  sellerName: optionalText(150),
  purchaseDate: z.string().optional().or(z.literal('')),
})

function validateImage(file) {
  if (!file) return { ok: true }
  if (!file.type.startsWith('image/')) return { ok: false, message: 'Image must be PNG/JPG/WEBP.' }
  if (file.size > 5 * 1024 * 1024) return { ok: false, message: 'Image must be <= 5 MB.' }
  return { ok: true }
}

function validateProof(file) {
  if (!file) return { ok: true }
  const isImage = file.type.startsWith('image/')
  const isPdf = file.type === 'application/pdf'
  if (!isImage && !isPdf) return { ok: false, message: 'Proof must be PDF or image.' }
  if (file.size > 10 * 1024 * 1024) return { ok: false, message: 'Proof must be <= 10 MB.' }
  return { ok: true }
}

function normalizeOptional(value) {
  const normalized = value?.trim()
  return normalized ? normalized : null
}

function getAppointmentStatusLabel(appointment) {
  if (!appointment) return 'Not scheduled yet'
  return appointment.statusLabel || appointment.status || 'Scheduled'
}

function getInterventionSummary(intervention) {
  if (!intervention) return 'No intervention yet'
  return intervention.outcome || intervention.status || 'In progress'
}

export default function ReclamationDetailPage() {
  const { id } = useParams()
  const navigate = useNavigate()

  const [item, setItem] = useState(null)
  const [history, setHistory] = useState([])
  const [appointment, setAppointment] = useState(null)
  const [interventions, setInterventions] = useState([])
  const [priorityInfo, setPriorityInfo] = useState(null)
  const [slaInfo, setSlaInfo] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const [actionBusy, setActionBusy] = useState(false)
  const [rejectReason, setRejectReason] = useState('')
  const [closeComment, setCloseComment] = useState('')
  const [overridePriorityValue, setOverridePriorityValue] = useState(String(Priority.HIGH))
  const [overridePriorityReason, setOverridePriorityReason] = useState('')
  const [productImageFile, setProductImageFile] = useState(null)
  const [purchaseProofFile, setPurchaseProofFile] = useState(null)
  const [removeProductImage, setRemoveProductImage] = useState(false)
  const [removePurchaseProof, setRemovePurchaseProof] = useState(false)

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
      isBlocking: false,
      followUpCount: 0,
      productName: '',
      barcode: '',
      brand: '',
      model: '',
      serialNumber: '',
      productReference: '',
      sellerName: '',
      purchaseDate: '',
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
        priority: data.severity ?? data.priority ?? Priority.MEDUIM,
        isBlocking: Boolean(data.isBlocking),
        followUpCount: Number(data.followUpCount ?? 0),
        productName: data.productName ?? '',
        barcode: data.barcode ?? '',
        brand: data.brand ?? '',
        model: data.model ?? '',
        serialNumber: data.serialNumber ?? '',
        productReference: data.productReference ?? '',
        sellerName: data.sellerName ?? '',
        purchaseDate: data.purchaseDate ? new Date(data.purchaseDate).toISOString().slice(0, 10) : '',
      })
      setProductImageFile(null)
      setPurchaseProofFile(null)
      setRemoveProductImage(false)
      setRemovePurchaseProof(false)

      try {
        const hist = await getReclamationHistory(id)
        setHistory(Array.isArray(hist) ? hist : [])
      } catch {
        setHistory([])
      }

      try {
        const appointmentData = await getAppointmentByReclamation(id)
        setAppointment(appointmentData)
      } catch {
        setAppointment(null)
      }

      try {
        const interventionData = await listInterventionsByReclamation(id)
        setInterventions(Array.isArray(interventionData) ? interventionData : [])
      } catch {
        setInterventions([])
      }

      try {
        const [priorityData, slaData] = await Promise.all([
          getReclamationPriority(id),
          getReclamationSla(id),
        ])
        setPriorityInfo(priorityData)
        setSlaInfo(slaData)
        setOverridePriorityValue(String(priorityData?.priority ?? Priority.HIGH))
      } catch {
        setPriorityInfo(null)
        setSlaInfo(null)
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

  async function onUpdate(values) {
    try {
      const imageCheck = validateImage(productImageFile)
      if (!imageCheck.ok) {
        setFormError('root', { message: imageCheck.message })
        return
      }

      const proofCheck = validateProof(purchaseProofFile)
      if (!proofCheck.ok) {
        setFormError('root', { message: proofCheck.message })
        return
      }

      let nextProductImageUrl = removeProductImage ? null : item?.productImageUrl ?? null
      let nextPurchaseProofUrl = removePurchaseProof ? null : item?.purchaseProofUrl ?? null

      if (productImageFile) {
        const upload = await uploadReclamationFile(productImageFile, 'image')
        nextProductImageUrl = upload?.url ?? null
      }

      if (purchaseProofFile) {
        const upload = await uploadReclamationFile(purchaseProofFile, 'proof')
        nextPurchaseProofUrl = upload?.url ?? null
      }

      const updated = await updateReclamation(id, {
        description: values.description.trim(),
        priority: Number(values.priority),
        isBlocking: Boolean(values.isBlocking),
        followUpCount: Number(values.followUpCount ?? 0),
        productName: normalizeOptional(values.productName),
        barcode: normalizeOptional(values.barcode),
        brand: normalizeOptional(values.brand),
        model: normalizeOptional(values.model),
        serialNumber: normalizeOptional(values.serialNumber),
        productReference: normalizeOptional(values.productReference),
        sellerName: normalizeOptional(values.sellerName),
        purchaseDate: values.purchaseDate || null,
        productImageUrl: nextProductImageUrl,
        purchaseProofUrl: nextPurchaseProofUrl,
      })
      setItem(updated)
      await reload()
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
  const canRequestPlanning = allowedActions.has('REQUEST_PLANNING')
  const canRecalculatePriority = allowedActions.has('RECALCULATE_PRIORITY')
  const canOverridePriority = allowedActions.has('OVERRIDE_PRIORITY')
  const canClose = allowedActions.has('CLOSE')
  const canCancel = allowedActions.has('CANCEL')
  const canReject = allowedActions.has('REJECT')
  const canDelete = allowedActions.has('DELETE')
  const latestIntervention = interventions.length > 0 ? interventions[0] : null
  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5005'

  const resolveFileUrl = (value) => {
    if (!value) return ''
    if (value.startsWith('http://') || value.startsWith('https://')) return value
    return `${apiBaseUrl}${value}`
  }

  const productImageUrl = resolveFileUrl(item?.productImageUrl)
  const purchaseProofUrl = resolveFileUrl(item?.purchaseProofUrl)
  const proofIsImage = purchaseProofUrl ? /\.(png|jpe?g|webp|gif)$/i.test(purchaseProofUrl) : false

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Reclamation detail"
        title={item?.reference || `#${id}`}
        description="Vue detaillee du dossier avec priorite, SLA, historique et actions metier autorisees."
        meta={
          item ? (
            <>
              <Badge className={priorityBadgeClasses(item.priority)}>{priorityLabel(item.priority)}</Badge>
              {priorityInfo ? (
                <Badge className="bg-white text-slate-700 ring-slate-200">
                  Score {priorityInfo.priorityScore} - {prioritySourceLabel(priorityInfo.prioritySource)}
                </Badge>
              ) : null}
              <Badge className={reclamationStatusBadgeClasses(item.status)}>{reclamationStatusLabel(item.status)}</Badge>
              {slaInfo ? (
                <Badge className={slaStatusBadgeClasses(slaInfo.slaStatus)}>{slaStatusLabel(slaInfo.slaStatus)}</Badge>
              ) : null}
              <span className="text-slate-300">|</span>
              <span className="font-semibold text-slate-800">Client:</span> {item.clientName}
            </>
          ) : null
        }
        actions={
          <Link to="/app/reclamations">
            <Button variant="secondary">Back</Button>
          </Link>
        }
      />

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
        <div className="space-y-6">
          <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">
            <MetricCard
              icon={CalendarClock}
              label="Priority"
              value={priorityLabel(priorityInfo?.priority ?? item.priority)}
              helper={priorityInfo ? `Score ${priorityInfo.priorityScore}` : 'Computed from workflow'}
              tone={Number(priorityInfo?.priority ?? item.priority) >= 2 ? 'amber' : 'cyan'}
            />
            <MetricCard
              icon={AlertTriangle}
              label="SLA"
              value={slaInfo ? slaStatusLabel(slaInfo.slaStatus) : '—'}
              helper={slaInfo?.activeTarget ? `${slaInfo.activeTarget} target active` : 'No active target'}
              tone={Number(slaInfo?.slaStatus) === 2 ? 'rose' : Number(slaInfo?.slaStatus) === 1 ? 'amber' : 'emerald'}
            />
            <MetricCard
              icon={UserCheck}
              label="Assignment"
              value={item.savName || 'Unassigned'}
              helper={item.technicianName ? `Tech: ${item.technicianName}` : 'No technician yet'}
              tone="slate"
            />
            <MetricCard
              icon={CheckCircle2}
              label="Allowed actions"
              value={allowedActions.size}
              helper="Visible and role-aware from backend"
              tone="cyan"
            />
          </div>

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
                  label="Declared severity"
                  error={errors.priority?.message}
                  disabled={!canEditDetails}
                  {...register('priority')}
                >
                  <option value={Priority.LOW}>{priorityLabel(Priority.LOW)}</option>
                  <option value={Priority.MEDUIM}>{priorityLabel(Priority.MEDUIM)}</option>
                  <option value={Priority.HIGH}>{priorityLabel(Priority.HIGH)}</option>
                  <option value={Priority.URGENT}>{priorityLabel(Priority.URGENT)}</option>
                </SelectField>

                <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                  <TextField
                    label="Follow-up count"
                    type="number"
                    min="0"
                    max="100"
                    disabled={!canEditDetails}
                    error={errors.followUpCount?.message}
                    {...register('followUpCount')}
                  />

                  <label className="input-shell block">
                    <div className="input-label">Blocking issue</div>
                    <div className="flex min-h-[52px] items-center rounded-[26px] border border-slate-200 bg-white px-4 py-3">
                      <input
                        type="checkbox"
                        className="h-4 w-4 rounded border-slate-300 text-cyan-700 focus:ring-cyan-200"
                        disabled={!canEditDetails}
                        {...register('isBlocking')}
                      />
                      <span className="ml-3 text-sm text-slate-700">Escalate this ticket as blocking.</span>
                    </div>
                  </label>
                </div>

                <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                  <div className="text-sm font-semibold text-slate-900">Product information</div>
                  <div className="mt-4 grid grid-cols-1 gap-4 md:grid-cols-2">
                    <TextField label="Product name" disabled={!canEditDetails} {...register('productName')} />
                    <TextField label="Barcode" disabled={!canEditDetails} {...register('barcode')} />
                    <TextField label="Brand" disabled={!canEditDetails} {...register('brand')} />
                    <TextField label="Model" disabled={!canEditDetails} {...register('model')} />
                    <TextField label="Serial number" disabled={!canEditDetails} {...register('serialNumber')} />
                    <TextField label="Product reference" disabled={!canEditDetails} {...register('productReference')} />
                    <TextField label="Seller name" disabled={!canEditDetails} {...register('sellerName')} />
                    <TextField label="Purchase date" type="date" disabled={!canEditDetails} {...register('purchaseDate')} />
                  </div>
                </div>

                <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                  <div className="text-sm font-semibold text-slate-900">Attachments</div>
                  <div className="mt-4 grid grid-cols-1 gap-4 md:grid-cols-2">
                    <div className="space-y-3">
                      <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Product image</div>
                      {productImageUrl && !removeProductImage ? (
                        <div className="overflow-hidden rounded-xl border border-slate-200 bg-white">
                          <img src={productImageUrl} alt="Product" className="h-40 w-full object-cover" />
                        </div>
                      ) : (
                        <div className="rounded-xl border border-dashed border-slate-200 bg-white px-4 py-6 text-sm text-slate-500">
                          No product image attached.
                        </div>
                      )}
                      <input
                        type="file"
                        accept="image/*"
                        disabled={!canEditDetails}
                        onChange={(event) => {
                          const file = event.target.files?.[0] || null
                          setProductImageFile(file)
                          if (file) {
                            setRemoveProductImage(false)
                          }
                        }}
                      />
                      {productImageFile ? <div className="text-xs text-slate-500">{productImageFile.name}</div> : null}
                      {productImageUrl && !removeProductImage ? (
                        <Button
                          type="button"
                          variant="secondary"
                          disabled={!canEditDetails}
                          onClick={() => {
                            setProductImageFile(null)
                            setRemoveProductImage(true)
                          }}
                        >
                          Remove current image
                        </Button>
                      ) : null}
                      {removeProductImage ? <div className="text-xs font-semibold text-rose-700">Current image will be removed on save.</div> : null}
                    </div>

                    <div className="space-y-3">
                      <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Purchase proof</div>
                      {purchaseProofUrl && !removePurchaseProof ? (
                        proofIsImage ? (
                          <div className="overflow-hidden rounded-xl border border-slate-200 bg-white">
                            <img src={purchaseProofUrl} alt="Purchase proof" className="h-40 w-full object-cover" />
                          </div>
                        ) : (
                          <div className="rounded-xl border border-slate-200 bg-white px-4 py-6 text-sm">
                            <a className="link font-semibold" href={purchaseProofUrl} target="_blank" rel="noreferrer">
                              Open current proof
                            </a>
                          </div>
                        )
                      ) : (
                        <div className="rounded-xl border border-dashed border-slate-200 bg-white px-4 py-6 text-sm text-slate-500">
                          No purchase proof attached.
                        </div>
                      )}
                      <input
                        type="file"
                        accept="image/*,application/pdf"
                        disabled={!canEditDetails}
                        onChange={(event) => {
                          const file = event.target.files?.[0] || null
                          setPurchaseProofFile(file)
                          if (file) {
                            setRemovePurchaseProof(false)
                          }
                        }}
                      />
                      {purchaseProofFile ? <div className="text-xs text-slate-500">{purchaseProofFile.name}</div> : null}
                      {purchaseProofUrl && !removePurchaseProof ? (
                        <Button
                          type="button"
                          variant="secondary"
                          disabled={!canEditDetails}
                          onClick={() => {
                            setPurchaseProofFile(null)
                            setRemovePurchaseProof(true)
                          }}
                        >
                          Remove current proof
                        </Button>
                      ) : null}
                      {removePurchaseProof ? <div className="text-xs font-semibold text-rose-700">Current proof will be removed on save.</div> : null}
                    </div>
                  </div>
                  <div className="mt-4 inline-flex items-center gap-2 text-xs font-semibold text-slate-600">
                    <Upload className="h-4 w-4" aria-hidden="true" />
                    Upload a new file to replace the existing attachment.
                  </div>
                </div>

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
              <div className="text-lg font-bold text-slate-900">Priority and SLA</div>
              <div className="mt-4 space-y-4 text-sm">
                <div className="flex flex-wrap items-center gap-2">
                  <Badge className={priorityBadgeClasses(priorityInfo?.priority ?? item.priority)}>
                    {priorityLabel(priorityInfo?.priority ?? item.priority)}
                  </Badge>
                  {priorityInfo ? (
                    <Badge className="bg-white text-slate-700 ring-slate-200">
                      Score {priorityInfo.priorityScore} - {prioritySourceLabel(priorityInfo.prioritySource)}
                    </Badge>
                  ) : null}
                  {slaInfo ? (
                    <Badge className={slaStatusBadgeClasses(slaInfo.slaStatus)}>{slaStatusLabel(slaInfo.slaStatus)}</Badge>
                  ) : null}
                </div>

                {priorityInfo?.priorityReasons?.length ? (
                  <div className="flex flex-wrap gap-2">
                    {priorityInfo.priorityReasons.map((reason) => (
                      <Badge key={reason} className="bg-amber-50 text-amber-800 ring-amber-200">{reason}</Badge>
                    ))}
                  </div>
                ) : (
                  <div className="text-slate-600">No explicit priority reasons available yet.</div>
                )}

                {slaInfo ? (
                  <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
                    <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                      <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">First response</div>
                      <div className="mt-1 font-semibold text-slate-900">{slaInfo.firstResponseDeadline ? formatDateTime(slaInfo.firstResponseDeadline) : '—'}</div>
                    </div>
                    <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                      <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Planning</div>
                      <div className="mt-1 font-semibold text-slate-900">{slaInfo.planningDeadline ? formatDateTime(slaInfo.planningDeadline) : '—'}</div>
                    </div>
                    <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                      <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Resolution</div>
                      <div className="mt-1 font-semibold text-slate-900">{slaInfo.resolutionDeadline ? formatDateTime(slaInfo.resolutionDeadline) : '—'}</div>
                    </div>
                    <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                      <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Active target</div>
                      <div className="mt-1 font-semibold text-slate-900">
                        {slaInfo.activeTarget || '—'}
                        {slaInfo.activeDeadline ? ` - ${formatDateTime(slaInfo.activeDeadline)}` : ''}
                      </div>
                      {slaInfo.slaBreachedAt ? (
                        <div className="mt-2 text-xs font-semibold text-rose-700">Breached at {formatDateTime(slaInfo.slaBreachedAt)}</div>
                      ) : null}
                    </div>
                  </div>
                ) : (
                  <div className="text-slate-600">No SLA snapshot available yet.</div>
                )}
              </div>
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

                {canRequestPlanning ? (
                  <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                    <div className="text-sm font-semibold text-slate-900">Request planning</div>
                    <div className="mt-1 text-sm text-slate-600">
                      Send this assigned reclamation into the scheduling workflow.
                    </div>
                    <div className="mt-3">
                      <Button
                        onClick={() =>
                          runAction(
                            () => requestPlanning(id, { comment: 'Planning requested from UI' }),
                            'Planning requested.',
                          )
                        }
                        disabled={actionBusy}
                      >
                        <CalendarClock className="h-4 w-4" aria-hidden="true" />
                        Request planning
                      </Button>
                    </div>
                  </div>
                ) : null}

                {canRecalculatePriority ? (
                  <div className="rounded-2xl border border-amber-200 bg-amber-50 p-4">
                    <div className="text-sm font-semibold text-amber-900">Recalculate priority</div>
                    <div className="mt-1 text-sm text-amber-800">
                      Refresh deterministic score, reasons and SLA risk from the backend rules.
                    </div>
                    <div className="mt-3">
                      <Button
                        variant="secondary"
                        disabled={actionBusy}
                        onClick={() =>
                          runAction(
                            async () => {
                              const updatedPriority = await recalculatePriority(id)
                              setPriorityInfo(updatedPriority)
                              const updatedSla = await getReclamationSla(id)
                              setSlaInfo(updatedSla)
                              return getReclamation(id)
                            },
                            'Priority recalculated.',
                          )
                        }
                      >
                        <AlertTriangle className="h-4 w-4" aria-hidden="true" />
                        Recalculate priority
                      </Button>
                    </div>
                  </div>
                ) : null}

                {canOverridePriority ? (
                  <div className="rounded-2xl border border-amber-200 bg-amber-50 p-4">
                    <div className="text-sm font-semibold text-amber-900">Manual priority override</div>
                    <div className="mt-1 text-sm text-amber-800">Use only when business context requires an explicit manual escalation.</div>
                    <form
                      className="mt-3 space-y-3"
                      onSubmit={(e) => {
                        e.preventDefault()
                        if (!overridePriorityReason.trim()) {
                          toast.error('Override reason is required.')
                          return
                        }
                        runAction(
                          async () => {
                            const updatedPriority = await overridePriority(id, {
                              priority: Number(overridePriorityValue),
                              reason: overridePriorityReason,
                            })
                            setPriorityInfo(updatedPriority)
                            const updatedSla = await getReclamationSla(id)
                            setSlaInfo(updatedSla)
                            return getReclamation(id)
                          },
                          'Priority overridden.',
                        )
                      }}
                    >
                      <SelectField label="Priority" value={overridePriorityValue} onChange={(e) => setOverridePriorityValue(e.target.value)}>
                        <option value={Priority.LOW}>LOW</option>
                        <option value={Priority.MEDUIM}>MEDIUM</option>
                        <option value={Priority.HIGH}>HIGH</option>
                        <option value={Priority.URGENT}>CRITICAL</option>
                      </SelectField>
                      <TextField
                        label="Override reason"
                        value={overridePriorityReason}
                        onChange={(e) => setOverridePriorityReason(e.target.value)}
                      />
                      <div className="flex justify-end">
                        <Button type="submit" disabled={actionBusy}>
                          <Save className="h-4 w-4" aria-hidden="true" />
                          Apply override
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

                {canDelete ? (
                  <div className="rounded-2xl border border-rose-200 bg-rose-50 p-4">
                    <div className="text-sm font-semibold text-rose-900">Delete</div>
                    <div className="mt-1 text-sm text-rose-800">
                      Permanently remove this reclamation while it is still open or already cancelled.
                    </div>
                    <div className="mt-3">
                      <Button
                        variant="danger"
                        disabled={actionBusy}
                        onClick={async () => {
                          const ok = window.confirm('Delete this reclamation permanently?')
                          if (!ok) return

                          setActionBusy(true)
                          try {
                            await deleteReclamation(id)
                            toast.success('Reclamation deleted.')
                            navigate('/app/reclamations', { replace: true })
                          } catch (err) {
                            const message = err?.response?.data?.detail || err?.response?.data?.title || err?.message
                            toast.error(message || 'Delete failed.')
                          } finally {
                            setActionBusy(false)
                          }
                        }}
                      >
                        <Trash2 className="h-4 w-4" aria-hidden="true" />
                        Delete permanently
                      </Button>
                    </div>
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

                {!canAssign && !canRequestPlanning && !canClose && !canDelete && !canCancel && !canReject ? (
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
                        <span className="text-slate-400">&rarr;</span>
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
              <div className="text-lg font-bold text-slate-900">Informations produit</div>
              <div className="mt-4 grid grid-cols-1 gap-3 text-sm">
                <div>
                  <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Produit</div>
                  <div className="mt-1 font-semibold text-slate-900">{item.productName || '—'}</div>
                </div>
                <div>
                  <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Marque / modele</div>
                  <div className="mt-1 font-semibold text-slate-900">
                    {[item.brand, item.model].filter(Boolean).join(' / ') || '—'}
                  </div>
                </div>
                <div>
                  <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Reference / serie</div>
                  <div className="mt-1 font-semibold text-slate-900">
                    {[item.productReference, item.serialNumber].filter(Boolean).join(' / ') || '—'}
                  </div>
                </div>
                <div>
                  <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Code-barres</div>
                  <div className="mt-1 font-semibold text-slate-900">{item.barcode || '—'}</div>
                </div>
                <div>
                  <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Date d'achat</div>
                  <div className="mt-1 font-semibold text-slate-900">
                    {item.purchaseDate ? new Date(item.purchaseDate).toLocaleDateString('fr-FR') : '—'}
                  </div>
                </div>
                <div>
                  <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Vendeur / magasin</div>
                  <div className="mt-1 font-semibold text-slate-900">{item.sellerName || '—'}</div>
                </div>

                {productImageUrl ? (
                  <div>
                    <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Image</div>
                    <div className="mt-2 overflow-hidden rounded-xl border border-slate-200">
                      <img src={productImageUrl} alt="Produit" className="h-48 w-full object-cover" />
                    </div>
                  </div>
                ) : null}

                {purchaseProofUrl ? (
                  <div>
                    <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Preuve d'achat</div>
                    {proofIsImage ? (
                      <div className="mt-2 overflow-hidden rounded-xl border border-slate-200">
                        <img src={purchaseProofUrl} alt="Preuve d'achat" className="h-40 w-full object-cover" />
                      </div>
                    ) : (
                      <div className="mt-2">
                        <a className="link font-semibold" href={purchaseProofUrl} target="_blank" rel="noreferrer">
                          Voir la facture
                        </a>
                      </div>
                    )}
                  </div>
                ) : null}
              </div>
            </div>

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
                  <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Next appointment</div>
                  <div className="mt-1 font-semibold text-slate-900">
                    {item.nextAppointmentAt ? formatDateTime(item.nextAppointmentAt) : '—'}
                    {item.nextAppointmentEndAt ? ` → ${formatDateTime(item.nextAppointmentEndAt)}` : ''}
                  </div>
                </div>
                <div>
                  <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Scheduling status</div>
                  <div className="mt-1 font-semibold text-slate-900">{getAppointmentStatusLabel(appointment)}</div>
                </div>
                <div>
                  <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Latest intervention</div>
                  <div className="mt-1 font-semibold text-slate-900">{getInterventionSummary(latestIntervention)}</div>
                </div>
                <div>
                  <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Latest intervention outcome</div>
                  <div className="mt-1 font-semibold text-slate-900">{item.lastInterventionOutcome || '—'}</div>
                </div>
                <div>
                  <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Needs replanning</div>
                  <div className="mt-1 font-semibold text-slate-900">{item.requiresReplanning ? 'Yes' : 'No'}</div>
                </div>

                {appointment ? (
                  <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                    <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Active appointment</div>
                    <div className="mt-2 text-sm text-slate-900">
                      <div className="font-semibold">{formatDateTime(appointment.startAt)}</div>
                      <div className="text-slate-600">
                        {appointment.endAt ? `Ends ${formatDateTime(appointment.endAt)}` : 'End time not specified'}
                      </div>
                      <div className="mt-2 text-slate-600">
                        Technician: {appointment.technicianName || 'Unassigned'}
                      </div>
                    </div>
                  </div>
                ) : null}

                {latestIntervention ? (
                  <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                    <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Latest intervention details</div>
                    <div className="mt-2 text-sm text-slate-900">
                      <div className="font-semibold">{getInterventionSummary(latestIntervention)}</div>
                      <div className="text-slate-600">
                        Started: {latestIntervention.startedAt ? formatDateTime(latestIntervention.startedAt) : '—'}
                      </div>
                      <div className="text-slate-600">
                        Ended: {latestIntervention.endedAt ? formatDateTime(latestIntervention.endedAt) : '—'}
                      </div>
                    </div>
                  </div>
                ) : null}

                {item.lastInterventionReportSummary ? (
                  <div>
                    <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Latest visit report</div>
                    <div className="mt-1 whitespace-pre-wrap text-slate-900">{item.lastInterventionReportSummary}</div>
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
        </div>
      )}
    </div>
  )
}
