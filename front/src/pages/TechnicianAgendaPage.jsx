import { CheckCircle2, Pause, Play, RefreshCw } from 'lucide-react'
import { useCallback, useContext, useEffect, useState } from 'react'
import toast from 'react-hot-toast'

import Badge from '../components/Badge.jsx'
import Button from '../components/Button.jsx'
import EmptyState from '../components/EmptyState.jsx'
import MetricCard from '../components/MetricCard.jsx'
import PageHeader from '../components/PageHeader.jsx'
import Spinner from '../components/Spinner.jsx'
import TextAreaField from '../components/TextAreaField.jsx'
import TextField from '../components/TextField.jsx'
import { AuthContext } from '../context/AuthContext.jsx'
import {
  addDiagnostic,
  completeIntervention,
  getTechnicianAgenda,
  getTechnicianCapacity,
  listMyInterventions,
  pauseIntervention,
  publishReport,
  startIntervention,
} from '../services/intervention.service.js'
import { formatDateTime } from '../utils/format.js'

export default function TechnicianAgendaPage() {
  const { user } = useContext(AuthContext)
  const [appointments, setAppointments] = useState([])
  const [interventions, setInterventions] = useState([])
  const [capacity, setCapacity] = useState(null)
  const [loading, setLoading] = useState(true)
  const [busyId, setBusyId] = useState(null)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const [appointmentData, interventionData] = await Promise.all([
        user?.id ? getTechnicianAgenda(user.id) : Promise.resolve([]),
        listMyInterventions(),
      ])
      setAppointments(Array.isArray(appointmentData) ? appointmentData : [])
      setInterventions(Array.isArray(interventionData) ? interventionData : [])
      if (user?.id) {
        try {
          setCapacity(await getTechnicianCapacity(user.id))
        } catch {
          setCapacity(null)
        }
      }
    } catch (error) {
      toast.error(error?.response?.data?.detail || error?.message || 'Failed to load technician workspace.')
    } finally {
      setLoading(false)
    }
  }, [user?.id])

  useEffect(() => {
    void load()
  }, [load])

  async function run(id, action, successMessage) {
    setBusyId(id)
    try {
      await action()
      toast.success(successMessage)
      await load()
    } catch (error) {
      toast.error(error?.response?.data?.detail || error?.message || 'Action failed.')
    } finally {
      setBusyId(null)
    }
  }

  if (loading) {
    return (
      <div className="surface-solid p-8">
        <Spinner label="Loading technician agenda..." />
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Technician workspace"
        title="Agenda et execution d'intervention"
        description="Les informations importantes sont mieux distinguees entre visites confirmees, charge quotidienne et actions terrain disponibles."
        meta={
          <>
            <span>{appointments.length} rendez-vous</span>
            <span className="text-slate-300">|</span>
            <span>{interventions.length} interventions</span>
          </>
        }
        actions={
          <Button variant="secondary" onClick={load}>
            <RefreshCw className="h-4 w-4" aria-hidden="true" />
            Refresh
          </Button>
        }
      />

      <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
        <MetricCard icon={RefreshCw} label="Appointments" value={appointments.length} helper="Rendez-vous confirms visibles" tone="cyan" />
        <MetricCard icon={Play} label="Interventions" value={interventions.length} helper="Dossiers techniques a executer" tone="amber" />
        <MetricCard
          icon={CheckCircle2}
          label="Daily load"
          value={capacity ? `${capacity.dailyLoadPercent}%` : '—'}
          helper={capacity ? `${capacity.dailyAssignedAppointments}/${capacity.dailyMaxAppointments} today` : 'Capacity unavailable'}
          tone={capacity && capacity.dailyLoadPercent >= 100 ? 'rose' : 'emerald'}
        />
      </div>

      <div className="surface-solid p-6">
        <div className="section-title">Confirmed appointments</div>
        {capacity ? (
          <div className="mt-4 rounded-[24px] border border-slate-200 bg-slate-50 p-4 text-sm">
            <div className="font-semibold text-slate-900">Today capacity</div>
            <div className="mt-1 text-slate-700">
              {capacity.dailyAssignedAppointments}/{capacity.dailyMaxAppointments} appointments - {capacity.dailyLoadPercent}%
            </div>
            <div className="mt-1 text-slate-600">
              Week: {capacity.weeklyAssignedAppointments}/{capacity.weeklyMaxAppointments} appointments - {capacity.weeklyLoadPercent}%
            </div>
          </div>
        ) : null}
        {appointments.length === 0 ? (
          <div className="mt-3 text-sm text-slate-600">No appointment assigned yet.</div>
        ) : (
          <div className="mt-4 space-y-3">
            {appointments.map((appointment) => (
              <div key={appointment.id} className="surface-muted p-4">
                <div className="font-display text-lg font-bold text-slate-950">{appointment.reference}</div>
                <div className="mt-1 text-sm text-slate-600">
                  {formatDateTime(appointment.startAt)}
                  {appointment.endAt ? ` -> ${formatDateTime(appointment.endAt)}` : ''}
                </div>
                <div className="mt-1 text-xs text-slate-500">
                  Estimated duration: {appointment.estimatedDurationMinutes || 0} minutes
                </div>
                <div className="mt-2">
                  <Badge className="bg-sky-50 text-sky-700 ring-sky-200">{appointment.status}</Badge>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      <div className="surface-solid p-6">
        <div className="section-title">My interventions</div>
        {interventions.length === 0 ? (
          <EmptyState title="No interventions" description="An intervention will appear here after an appointment is confirmed." />
        ) : (
          <div className="mt-4 space-y-4">
            {interventions.map((intervention) => (
              <InterventionCard
                key={intervention.id}
                intervention={intervention}
                busy={busyId === intervention.id}
                onStart={() => run(intervention.id, () => startIntervention(intervention.id), 'Intervention started.')}
                onPause={() => run(intervention.id, () => pauseIntervention(intervention.id), 'Intervention paused.')}
                onDiagnostic={(payload) => run(intervention.id, () => addDiagnostic(intervention.id, payload), 'Diagnostic saved.')}
                onComplete={(payload) => run(intervention.id, () => completeIntervention(intervention.id, payload), 'Intervention completed.')}
                onPublish={(payload) => run(intervention.id, () => publishReport(intervention.id, payload), 'Visit report published.')}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  )
}

function InterventionCard({ intervention, busy, onStart, onPause, onDiagnostic, onComplete, onPublish }) {
  const allowedActions = new Set((intervention.allowedActions ?? []).map((item) => String(item).toUpperCase()))
  const [diagnosticSummary, setDiagnosticSummary] = useState('')
  const [diagnosticCategory, setDiagnosticCategory] = useState('FIELD_DIAGNOSTIC')
  const [reportSummary, setReportSummary] = useState(intervention.latestReportSummary || '')
  const [nextStep, setNextStep] = useState('')

  return (
    <div className="surface-muted p-4">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <div className="font-display text-lg font-bold text-slate-950">{intervention.reference}</div>
          <div className="mt-1 text-sm text-slate-600">
            Started: {intervention.startedAt ? formatDateTime(intervention.startedAt) : 'Not started'}
          </div>
        </div>
        <Badge className="bg-amber-50 text-amber-800 ring-amber-200">{intervention.status}</Badge>
      </div>

      {intervention.outcome ? (
        <div className="mt-3 text-sm text-slate-700">Latest outcome: {intervention.outcome}</div>
      ) : null}

      <div className="mt-4 flex flex-wrap gap-2">
        {allowedActions.has('START_INTERVENTION') ? (
          <Button disabled={busy} onClick={onStart}>
            <Play className="h-4 w-4" aria-hidden="true" />
            Start
          </Button>
        ) : null}
        {allowedActions.has('PAUSE_INTERVENTION') ? (
          <Button variant="secondary" disabled={busy} onClick={onPause}>
            <Pause className="h-4 w-4" aria-hidden="true" />
            Pause
          </Button>
        ) : null}
      </div>

      {allowedActions.has('RECORD_DIAGNOSTIC') ? (
        <form
          className="mt-4 grid grid-cols-1 gap-3 md:grid-cols-[220px_1fr_auto]"
          onSubmit={(event) => {
            event.preventDefault()
            if (!diagnosticSummary.trim()) {
              toast.error('Diagnostic summary is required.')
              return
            }
            onDiagnostic({
              category: diagnosticCategory,
              summary: diagnosticSummary,
              requiresParts: false,
              requiresFollowUp: false,
            })
          }}
        >
          <TextField label="Category" value={diagnosticCategory} onChange={(e) => setDiagnosticCategory(e.target.value)} />
          <TextField label="Summary" value={diagnosticSummary} onChange={(e) => setDiagnosticSummary(e.target.value)} />
          <div className="flex items-end">
            <Button type="submit" disabled={busy}>Save diagnostic</Button>
          </div>
        </form>
      ) : null}

      {allowedActions.has('COMPLETE_INTERVENTION') ? (
        <div className="mt-4">
          <Button
            disabled={busy}
            onClick={() => onComplete({ outcome: 0, needsReplanning: false })}
          >
            <CheckCircle2 className="h-4 w-4" aria-hidden="true" />
            Complete as solved
          </Button>
        </div>
      ) : null}

      {allowedActions.has('PUBLISH_REPORT') ? (
        <form
          className="mt-4 space-y-3"
          onSubmit={(event) => {
            event.preventDefault()
            if (!reportSummary.trim()) {
              toast.error('Report summary is required.')
              return
            }
            onPublish({
              summary: reportSummary,
              outcome: intervention.outcome ?? 0,
              customerPresent: true,
              nextStep: nextStep || undefined,
            })
          }}
        >
          <TextAreaField label="Report summary" value={reportSummary} onChange={(e) => setReportSummary(e.target.value)} />
          <TextField label="Next step" value={nextStep} onChange={(e) => setNextStep(e.target.value)} />
          <Button type="submit" disabled={busy}>Publish report</Button>
        </form>
      ) : null}
    </div>
  )
}
