import { CalendarDays, CheckCircle2, RefreshCw, UserCheck } from 'lucide-react'
import { useEffect, useState } from 'react'
import toast from 'react-hot-toast'

import Badge from '../components/Badge.jsx'
import Button from '../components/Button.jsx'
import EmptyState from '../components/EmptyState.jsx'
import MetricCard from '../components/MetricCard.jsx'
import PageHeader from '../components/PageHeader.jsx'
import SelectField from '../components/SelectField.jsx'
import Spinner from '../components/Spinner.jsx'
import TextField from '../components/TextField.jsx'
import { listUsers } from '../services/adminUsers.service.js'
import {
  assignTechnician,
  confirmAppointment,
  createAppointment,
  getTechnicianCapacity,
  listAppointments,
  listPlanningRequests,
} from '../services/intervention.service.js'
import { formatDateTime } from '../utils/format.js'

export default function PlanningBoardPage() {
  const [requests, setRequests] = useState([])
  const [appointments, setAppointments] = useState([])
  const [technicians, setTechnicians] = useState([])
  const [capacities, setCapacities] = useState([])
  const [loading, setLoading] = useState(true)
  const [busyId, setBusyId] = useState(null)

  const [planningRequestId, setPlanningRequestId] = useState('')
  const [startAt, setStartAt] = useState('')
  const [endAt, setEndAt] = useState('')
  const [estimatedDurationMinutes, setEstimatedDurationMinutes] = useState('90')
  const [planningNote, setPlanningNote] = useState('')

  async function load() {
    setLoading(true)
    try {
      const [requestData, appointmentData, technicianData] = await Promise.all([
        listPlanningRequests(),
        listAppointments(),
        listUsers({ role: 'ST' }),
      ])

      setRequests(Array.isArray(requestData) ? requestData : [])
      setAppointments(Array.isArray(appointmentData) ? appointmentData : [])
      setTechnicians(Array.isArray(technicianData) ? technicianData : [])
      const capacityData = await Promise.all(
        (Array.isArray(technicianData) ? technicianData : []).map(async (technician) => {
          try {
            return await getTechnicianCapacity(technician.id)
          } catch {
            return null
          }
        }),
      )
      setCapacities(capacityData.filter(Boolean))

      if (!planningRequestId && Array.isArray(requestData) && requestData.length > 0) {
        setPlanningRequestId(String(requestData[0].id))
      }
    } catch (error) {
      toast.error(error?.response?.data?.detail || error?.message || 'Failed to load planning.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  async function handleCreateAppointment(event) {
    event.preventDefault()
    if (!planningRequestId || !startAt) {
      toast.error('Planning request and start date are required.')
      return
    }

    try {
      await createAppointment({
        planningRequestId,
        startAt: new Date(startAt).toISOString(),
        endAt: endAt ? new Date(endAt).toISOString() : null,
        estimatedDurationMinutes: Number(estimatedDurationMinutes || 90),
        planningNote: planningNote || undefined,
      })
      toast.success('Appointment proposed.')
      setPlanningNote('')
      await load()
    } catch (error) {
      toast.error(error?.response?.data?.detail || error?.message || 'Failed to create appointment.')
    }
  }

  async function handleAssign(appointmentId, technicianId) {
    const technician = technicians.find((item) => String(item.id) === String(technicianId))
    if (!technician) {
      toast.error('Please select a technician.')
      return
    }

    setBusyId(appointmentId)
    try {
      await assignTechnician(appointmentId, {
        technicianId: technician.id,
        technicianName: `${technician.firstName ?? ''} ${technician.lastName ?? ''}`.trim() || technician.email,
      })
      toast.success('Technician assigned.')
      await load()
    } catch (error) {
      toast.error(error?.response?.data?.detail || error?.message || 'Failed to assign technician.')
    } finally {
      setBusyId(null)
    }
  }

  async function handleConfirm(appointmentId) {
    setBusyId(appointmentId)
    try {
      await confirmAppointment(appointmentId)
      toast.success('Appointment confirmed.')
      await load()
    } catch (error) {
      toast.error(error?.response?.data?.detail || error?.message || 'Failed to confirm appointment.')
    } finally {
      setBusyId(null)
    }
  }

  if (loading) {
    return (
      <div className="surface-solid p-8">
        <Spinner label="Loading planning board..." />
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Planning board"
        title="Agenda SAV et capacite terrain"
        description="La planification gagne en lisibilite avec une meilleure separation entre demandes, capacite technicien et cartes de rendez-vous."
        meta={
          <>
            <span>{requests.length} demandes en attente</span>
            <span className="text-slate-300">|</span>
            <span>{appointments.length} rendez-vous</span>
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
        <MetricCard icon={CalendarDays} label="Planning requests" value={requests.length} helper="Demandes a traiter" tone="amber" />
        <MetricCard icon={UserCheck} label="Appointments" value={appointments.length} helper="Cartes planifiees ou confirmees" tone="cyan" />
        <MetricCard icon={RefreshCw} label="Technicians" value={technicians.length} helper="Capacite chargee dans la vue" tone="emerald" />
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-[360px_1fr]">
        <div className="surface-solid p-6">
          <div className="section-title">New appointment</div>
          <div className="section-copy mt-2">Creation rapide avec duree estimee et note de planification.</div>
          <form className="mt-4 space-y-4" onSubmit={handleCreateAppointment}>
            <SelectField label="Planning request" value={planningRequestId} onChange={(e) => setPlanningRequestId(e.target.value)}>
              <option value="">Select a request</option>
              {requests.map((request) => (
              <option key={request.id} value={request.id}>
                  {request.reference} - {request.customerName}
                </option>
              ))}
            </SelectField>

            <TextField label="Start at" type="datetime-local" value={startAt} onChange={(e) => setStartAt(e.target.value)} />
            <TextField label="End at" type="datetime-local" value={endAt} onChange={(e) => setEndAt(e.target.value)} />
            <TextField
              label="Estimated duration (min)"
              type="number"
              min="15"
              step="15"
              value={estimatedDurationMinutes}
              onChange={(e) => setEstimatedDurationMinutes(e.target.value)}
            />
            <TextField label="Planning note" value={planningNote} onChange={(e) => setPlanningNote(e.target.value)} />

            <Button type="submit">
              <CalendarDays className="h-4 w-4" aria-hidden="true" />
              Propose appointment
            </Button>
          </form>
        </div>

        <div className="space-y-6">
          <div className="surface-solid p-6">
            <div className="section-title">Pending planning requests</div>
            {requests.length === 0 ? (
              <div className="mt-3 text-sm text-slate-600">No planning requests yet.</div>
            ) : (
              <div className="mt-4 space-y-3">
                {requests.map((request) => (
                  <div key={request.id} className="surface-muted p-4">
                    <div className="flex flex-wrap items-center gap-2">
                      <div className="font-semibold text-slate-900">{request.reference}</div>
                      <Badge className="bg-slate-50 text-slate-700 ring-slate-200">{request.status}</Badge>
                    </div>
                    <div className="mt-2 text-sm text-slate-700">{request.customerName}</div>
                    <div className="mt-1 text-xs text-slate-500">{formatDateTime(request.requestedAt)}</div>
                  </div>
                ))}
              </div>
            )}
          </div>

          <div className="surface-solid p-6">
            <div className="section-title">Technician capacity</div>
            {capacities.length === 0 ? (
              <div className="mt-3 text-sm text-slate-600">Capacity snapshots unavailable.</div>
            ) : (
              <div className="mt-4 grid grid-cols-1 gap-3 md:grid-cols-2">
                {capacities.map((capacity) => {
                  const overload = capacity.dailyLoadPercent >= 100 || capacity.weeklyLoadPercent >= 100
                  return (
                    <div
                      key={capacity.technicianId}
                      className={`rounded-[24px] border p-4 shadow-[0_16px_36px_-28px_rgba(15,23,42,0.3)] ${overload ? 'border-rose-200 bg-rose-50' : 'border-slate-200 bg-white'}`}
                    >
                      <div className="text-sm font-semibold text-slate-900">Technician #{capacity.technicianId}</div>
                      <div className="mt-2 text-xs text-slate-600">
                        Day: {capacity.dailyAssignedAppointments}/{capacity.dailyMaxAppointments} appointments - {capacity.dailyLoadPercent}%
                      </div>
                      <div className="mt-1 text-xs text-slate-600">
                        Week: {capacity.weeklyAssignedAppointments}/{capacity.weeklyMaxAppointments} appointments - {capacity.weeklyLoadPercent}%
                      </div>
                    </div>
                  )
                })}
              </div>
            )}
          </div>

          <div className="surface-solid p-6">
            <div className="section-title">Appointments</div>
            {appointments.length === 0 ? (
              <EmptyState title="No appointments" description="Create the first appointment from a planning request." />
            ) : (
              <div className="mt-4 space-y-4">
                {appointments.map((appointment) => (
                  <AppointmentCard
                    key={appointment.id}
                    appointment={appointment}
                    capacities={capacities}
                    technicians={technicians}
                    busy={busyId === appointment.id}
                    onAssign={handleAssign}
                    onConfirm={handleConfirm}
                  />
                ))}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}

function AppointmentCard({ appointment, technicians, capacities, busy, onAssign, onConfirm }) {
  const [technicianId, setTechnicianId] = useState(appointment.technicianId ? String(appointment.technicianId) : '')

  const allowedActions = new Set((appointment.allowedActions ?? []).map((item) => String(item).toUpperCase()))
  const selectedCapacity = capacities.find((item) => String(item.technicianId) === String(appointment.technicianId || technicianId))

  return (
    <div className="surface-muted p-4">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <div className="font-display text-lg font-bold text-slate-950">{appointment.reference}</div>
          <div className="text-sm text-slate-600">
            {formatDateTime(appointment.startAt)}
            {appointment.endAt ? ` -> ${formatDateTime(appointment.endAt)}` : ''}
          </div>
        </div>
        <Badge className="bg-sky-50 text-sky-700 ring-sky-200">{appointment.status}</Badge>
      </div>

      <div className="mt-3 text-sm text-slate-700">
        Technician: {appointment.technicianName || 'Unassigned'}
      </div>
      <div className="mt-1 text-xs text-slate-500">
        Estimated duration: {appointment.estimatedDurationMinutes || 0} minutes
      </div>
      {selectedCapacity ? (
        <div className="mt-2 text-xs text-slate-600">
          Capacity today: {selectedCapacity.dailyAssignedAppointments}/{selectedCapacity.dailyMaxAppointments} - {selectedCapacity.dailyLoadPercent}%
        </div>
      ) : null}

      {allowedActions.has('ASSIGN_TECHNICIAN') ? (
        <div className="mt-4 flex flex-col gap-3 sm:flex-row sm:items-end">
          <div className="flex-1">
            <SelectField label="Technician" value={technicianId} onChange={(e) => setTechnicianId(e.target.value)}>
              <option value="">Select technician</option>
              {technicians.map((technician) => (
                <option key={technician.id} value={technician.id}>
                  {`${technician.firstName ?? ''} ${technician.lastName ?? ''}`.trim() || technician.email}
                </option>
              ))}
            </SelectField>
          </div>
          <Button disabled={busy} onClick={() => onAssign(appointment.id, technicianId)}>
            <UserCheck className="h-4 w-4" aria-hidden="true" />
            Assign
          </Button>
        </div>
      ) : null}

      {allowedActions.has('CONFIRM_APPOINTMENT') ? (
        <div className="mt-3">
          <Button disabled={busy} onClick={() => onConfirm(appointment.id)}>
            <CheckCircle2 className="h-4 w-4" aria-hidden="true" />
            Confirm appointment
          </Button>
        </div>
      ) : null}
    </div>
  )
}
