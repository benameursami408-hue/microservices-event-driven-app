import api from './api.js'

export async function listPlanningRequests() {
  const { data } = await api.get('/api/planning/requests')
  return data
}

export async function listAppointments(params = {}) {
  const { data } = await api.get('/api/planning/appointments', { params })
  return data
}

export async function getTechnicianAgenda(technicianId, params = {}) {
  const { data } = await api.get(`/api/planning/technicians/${technicianId}/agenda`, { params })
  return data
}

export async function getTechnicianCapacity(technicianId, params = {}) {
  const { data } = await api.get(`/api/planning/technicians/${technicianId}/capacity`, { params })
  return data
}

export async function getAppointmentByReclamation(reclamationId) {
  const { data } = await api.get(`/api/planning/reclamations/${reclamationId}`)
  return data
}

export async function createAppointment(payload) {
  const { data } = await api.post('/api/planning/appointments', payload)
  return data
}

export async function assignTechnician(appointmentId, payload) {
  const { data } = await api.post(`/api/planning/appointments/${appointmentId}/assign-technician`, payload)
  return data
}

export async function confirmAppointment(appointmentId) {
  const { data } = await api.post(`/api/planning/appointments/${appointmentId}/confirm`)
  return data
}

export async function rescheduleAppointment(appointmentId, payload) {
  const { data } = await api.post(`/api/planning/appointments/${appointmentId}/reschedule`, payload)
  return data
}

export async function cancelAppointment(appointmentId, payload) {
  const { data } = await api.post(`/api/planning/appointments/${appointmentId}/cancel`, payload)
  return data
}

export async function listInterventions(params = {}) {
  const { data } = await api.get('/api/realisations/interventions', { params })
  return data
}

export async function listInterventionsByReclamation(reclamationId) {
  const { data } = await api.get(`/api/realisations/interventions/by-reclamation/${reclamationId}`)
  return data
}

export async function listMyInterventions() {
  const { data } = await api.get('/api/realisations/interventions/mine')
  return data
}

export async function startIntervention(id) {
  const { data } = await api.post(`/api/realisations/interventions/${id}/start`)
  return data
}

export async function pauseIntervention(id) {
  const { data } = await api.post(`/api/realisations/interventions/${id}/pause`)
  return data
}

export async function addDiagnostic(id, payload) {
  const { data } = await api.post(`/api/realisations/interventions/${id}/diagnostic`, payload)
  return data
}

export async function addRepairAction(id, payload) {
  const { data } = await api.post(`/api/realisations/interventions/${id}/repair-actions`, payload)
  return data
}

export async function addPartUsed(id, payload) {
  const { data } = await api.post(`/api/realisations/interventions/${id}/parts-used`, payload)
  return data
}

export async function addEvidence(id, payload) {
  const { data } = await api.post(`/api/realisations/interventions/${id}/evidences`, payload)
  return data
}

export async function completeIntervention(id, payload) {
  const { data } = await api.post(`/api/realisations/interventions/${id}/complete`, payload)
  return data
}

export async function publishReport(id, payload) {
  const { data } = await api.post(`/api/realisations/interventions/${id}/publish-report`, payload)
  return data
}

export async function requestReplanning(id, payload) {
  const { data } = await api.post(`/api/realisations/interventions/${id}/request-replanning`, payload)
  return data
}
