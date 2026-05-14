import { fromApiPriority } from './priorityMapper';
import { fromApiAppointmentStatus, fromApiPlanningStatus } from './statusMapper';

function toDate(value) {
  if (!value) return '';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '';
  return date.toISOString().slice(0, 10);
}

function toTime(value) {
  if (!value) return '';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '';
  return date.toTimeString().slice(0, 5);
}

export function mapPlanningRequestFromApi(item = {}) {
  return {
    id: item.reference || String(item.id),
    technicalId: item.id,
    reclamationId: item.reclamationId,
    client: item.customerName || '',
    clientId: item.clientId,
    product: item.reference || `Reclamation ${item.reclamationId}`,
    location: item.serviceAddress || '',
    date: toDate(item.requestedAt),
    priority: fromApiPriority(item.priority),
    status: fromApiPlanningStatus(item.status),
    technicianId: '',
    technicianName: '',
    appointmentId: '',
    raw: item
  };
}

export function mapAppointmentFromApi(item = {}) {
  return {
    id: item.reference || String(item.id),
    technicalId: item.id,
    requestId: item.planningRequestId,
    reclamationId: item.reclamationId,
    client: item.customerName || item.reference || '',
    product: item.reference || `Reclamation ${item.reclamationId}`,
    location: item.serviceAddress || '',
    date: toDate(item.startAt),
    start: toTime(item.startAt),
    end: toTime(item.endAt),
    duration: `${item.estimatedDurationMinutes || 90} min`,
    technicianId: item.technicianId || '',
    technicianName: item.technicianName || '',
    status: fromApiAppointmentStatus(item.status),
    note: item.planningNote || '',
    customerPresence: item.customerPresenceRequired !== false,
    raw: item
  };
}

export function mapAppointmentToCreateDto(payload = {}) {
  const startAt = payload.startAt || `${payload.date}T${payload.start || '09:00'}:00`;
  const endAt = payload.endAt || `${payload.date}T${payload.end || '10:30'}:00`;
  return {
    planningRequestId: payload.technicalRequestId || payload.planningRequestId || payload.requestId,
    startAt,
    endAt,
    estimatedDurationMinutes: payload.estimatedDurationMinutes || 90,
    timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC',
    customerPresenceRequired: payload.customerPresenceRequired !== false,
    planningNote: payload.note || payload.planningNote || ''
  };
}

export function mapRescheduleToDto(payload = {}) {
  return {
    startAt: payload.startAt || `${payload.date}T${payload.start || '09:00'}:00`,
    endAt: payload.endAt || `${payload.date}T${payload.end || '10:30'}:00`,
    estimatedDurationMinutes: payload.estimatedDurationMinutes || 90,
    reasonCode: payload.reasonCode || 'UI_RESCHEDULE',
    reasonText: payload.reason || payload.reasonText || 'Requested from SAV Pro UI'
  };
}
