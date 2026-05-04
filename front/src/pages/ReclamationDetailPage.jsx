import { zodResolver } from '@hookform/resolvers/zod'
import { AlertTriangle } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate, useParams } from 'react-router-dom'
import toast from 'react-hot-toast'
import { z } from 'zod'

import Badge from '../components/Badge.jsx'
import Button from '../components/Button.jsx'
import PageHeader from '../components/PageHeader.jsx'
import {
  ReclamationDetailForm,
  ReclamationHistoryCard,
  ReclamationKpiGrid,
  ReclamationPrioritySlaCard,
  ReclamationSidePanel,
  ReclamationWorkflowCard,
} from '../components/reclamations/ReclamationDetailSections.jsx'
import Spinner from '../components/Spinner.jsx'
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
      setError(message || 'Impossible de charger la reclamation.')
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
      toast.error(message || 'Action impossible.')
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

  function handleAssign() {
    runAction(() => assignReclamation(id, { comment: 'Assigned from UI' }), 'Affectation effectuee.')
  }

  function handleRequestPlanning() {
    runAction(
      () => requestPlanning(id, { comment: 'Planning requested from UI' }),
      'Demande de planification envoyee.',
    )
  }

  function handleRecalculatePriority() {
    runAction(
      async () => {
        const updatedPriority = await recalculatePriority(id)
        setPriorityInfo(updatedPriority)
        const updatedSla = await getReclamationSla(id)
        setSlaInfo(updatedSla)
        return getReclamation(id)
      },
      'Priorite recalculee.',
    )
  }

  function handleOverridePriority(event) {
    event.preventDefault()
    if (!overridePriorityReason.trim()) {
      toast.error("La raison de l'override est obligatoire.")
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
      'Priorite modifiee.',
    )
  }

  function handleClose(event) {
    event.preventDefault()
    runAction(() => closeReclamation(id, { comment: closeComment || undefined }), 'Reclamation cloturee.')
  }

  async function handleDelete() {
    const ok = window.confirm('Supprimer definitivement cette reclamation ?')
    if (!ok) return

    setActionBusy(true)
    try {
      await deleteReclamation(id)
      toast.success('Reclamation supprimee.')
      navigate('/app/reclamations', { replace: true })
    } catch (err) {
      const message = err?.response?.data?.detail || err?.response?.data?.title || err?.message
      toast.error(message || 'Suppression impossible.')
    } finally {
      setActionBusy(false)
    }
  }

  function handleCancel() {
    const ok = window.confirm('Annuler cette reclamation ?')
    if (!ok) return
    runAction(() => cancelReclamation(id), 'Reclamation annulee.')
  }

  function handleReject(event) {
    event.preventDefault()
    if (!rejectReason.trim()) {
      toast.error('La raison est obligatoire.')
      return
    }
    runAction(() => rejectReclamation(id, { reason: rejectReason }), 'Reclamation rejetee.')
  }

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
          <Spinner label="Chargement de la reclamation..." />
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
          <ReclamationKpiGrid
            item={item}
            priorityInfo={priorityInfo}
            slaInfo={slaInfo}
            allowedActionsCount={allowedActions.size}
          />

          <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
            <div className="space-y-6 lg:col-span-2">
              <ReclamationDetailForm
                canEditDetails={canEditDetails}
                errors={errors}
                handleSubmit={handleSubmit}
                isSubmitting={isSubmitting}
                onUpdate={onUpdate}
                productImageFile={productImageFile}
                productImageUrl={productImageUrl}
                proofIsImage={proofIsImage}
                purchaseProofFile={purchaseProofFile}
                purchaseProofUrl={purchaseProofUrl}
                register={register}
                removeProductImage={removeProductImage}
                removePurchaseProof={removePurchaseProof}
                setProductImageFile={setProductImageFile}
                setPurchaseProofFile={setPurchaseProofFile}
                setRemoveProductImage={setRemoveProductImage}
                setRemovePurchaseProof={setRemovePurchaseProof}
              />

              <ReclamationPrioritySlaCard item={item} priorityInfo={priorityInfo} slaInfo={slaInfo} />

              <ReclamationWorkflowCard
                actionBusy={actionBusy}
                canAssign={canAssign}
                canCancel={canCancel}
                canClose={canClose}
                canDelete={canDelete}
                canOverridePriority={canOverridePriority}
                canRecalculatePriority={canRecalculatePriority}
                canReject={canReject}
                canRequestPlanning={canRequestPlanning}
                closeComment={closeComment}
                onAssign={handleAssign}
                onCancel={handleCancel}
                onClose={handleClose}
                onDelete={handleDelete}
                onOverridePriority={handleOverridePriority}
                onRecalculatePriority={handleRecalculatePriority}
                onReject={handleReject}
                onRequestPlanning={handleRequestPlanning}
                overridePriorityReason={overridePriorityReason}
                overridePriorityValue={overridePriorityValue}
                rejectReason={rejectReason}
                setCloseComment={setCloseComment}
                setOverridePriorityReason={setOverridePriorityReason}
                setOverridePriorityValue={setOverridePriorityValue}
                setRejectReason={setRejectReason}
              />

              <ReclamationHistoryCard history={history} />
            </div>

            <ReclamationSidePanel
              appointment={appointment}
              item={item}
              latestIntervention={latestIntervention}
              productImageUrl={productImageUrl}
              proofIsImage={proofIsImage}
              purchaseProofUrl={purchaseProofUrl}
            />
          </div>
        </div>
      )}
    </div>
  )
}
