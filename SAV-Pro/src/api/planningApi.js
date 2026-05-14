import { apiRequest } from './apiClient';
import { mapAppointmentFromApi, mapAppointmentToCreateDto, mapPlanningRequestFromApi, mapRescheduleToDto } from './mappers/planningMapper';

export async function listPlanningRequests() {
  const rows = await apiRequest('/api/planning/requests');
  return (rows || []).map(mapPlanningRequestFromApi);
}

export async function listAppointments(filters = {}) {
  const rows = await apiRequest('/api/planning/appointments', { query: filters });
  return (rows || []).map(mapAppointmentFromApi);
}

export async function createAppointment(payload) {
  return mapAppointmentFromApi(await apiRequest('/api/planning/appointments', {
    method: 'POST',
    body: mapAppointmentToCreateDto(payload)
  }));
}

export async function assignTechnician(appointmentId, technician) {
  return mapAppointmentFromApi(await apiRequest(`/api/planning/appointments/${appointmentId}/assign-technician`, {
    method: 'POST',
    body: {
      technicianId: technician?.technicalId || technician?.id,
      technicianName: technician?.name || `${technician?.firstName || ''} ${technician?.lastName || ''}`.trim()
    }
  }));
}

export async function confirmAppointment(id) {
  return mapAppointmentFromApi(await apiRequest(`/api/planning/appointments/${id}/confirm`, { method: 'POST' }));
}

export async function rescheduleAppointment(id, payload) {
  return mapAppointmentFromApi(await apiRequest(`/api/planning/appointments/${id}/reschedule`, {
    method: 'POST',
    body: mapRescheduleToDto(payload)
  }));
}

export async function cancelAppointment(id, reasonText = '') {
  return mapAppointmentFromApi(await apiRequest(`/api/planning/appointments/${id}/cancel`, {
    method: 'POST',
    body: { reasonCode: 'UI_CANCEL', reasonText }
  }));
}
