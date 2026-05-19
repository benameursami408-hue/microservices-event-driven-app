import { apiRequest } from './apiClient';
import { mapReclamationFromApi, mapReclamationToCreateDto, mapReclamationToUpdateDto, getTechnicalReclamationId } from './mappers/reclamationMapper';
import { toApiPriority } from './mappers/priorityMapper';

export async function listReclamations(filters = {}) {
  const rows = await apiRequest('/api/reclamations', { query: filters });
  return (rows || []).map(mapReclamationFromApi);
}

export async function queryReclamations(filters = {}) {
  const result = await apiRequest('/api/reclamations/query', { query: filters });
  return {
    ...result,
    items: (result?.items || []).map(mapReclamationFromApi)
  };
}

export async function getReclamation(id) {
  return mapReclamationFromApi(await apiRequest(`/api/reclamations/${getTechnicalReclamationId(id)}`));
}

export async function createReclamation(payload) {
  return mapReclamationFromApi(await apiRequest('/api/reclamations', {
    method: 'POST',
    body: mapReclamationToCreateDto(payload)
  }));
}

export async function updateReclamation(id, payload) {
  return mapReclamationFromApi(await apiRequest(`/api/reclamations/${getTechnicalReclamationId(id)}`, {
    method: 'PUT',
    body: mapReclamationToUpdateDto(payload)
  }));
}

export async function deleteReclamation(id) {
  return apiRequest(`/api/reclamations/${getTechnicalReclamationId(id)}`, {
    method: 'DELETE'
  });
}

export async function claimReclamation(id) {
  return mapReclamationFromApi(await apiRequest(`/api/reclamations/${getTechnicalReclamationId(id)}/claim`, {
    method: 'POST'
  }));
}

export async function releaseReclamation(id) {
  return mapReclamationFromApi(await apiRequest(`/api/reclamations/${getTechnicalReclamationId(id)}/release`, {
    method: 'POST'
  }));
}

export async function reassignReclamation(id, savUserId, savUserName = '') {
  return mapReclamationFromApi(await apiRequest(`/api/reclamations/${getTechnicalReclamationId(id)}/reassign-sav`, {
    method: 'POST',
    body: { savUserId, savUserName }
  }));
}

export async function requestPlanning(id, comment = '') {
  return mapReclamationFromApi(await apiRequest(`/api/reclamations/${getTechnicalReclamationId(id)}/request-planning`, {
    method: 'PATCH',
    body: { comment }
  }));
}

export async function assignToSav(id, sav) {
  return mapReclamationFromApi(await apiRequest(`/api/reclamations/${getTechnicalReclamationId(id)}/assign`, {
    method: 'PATCH',
    body: {
      savId: sav?.technicalId || sav?.id,
      savName: sav?.name || `${sav?.firstName || ''} ${sav?.lastName || ''}`.trim(),
      comment: 'Assigned from SAV Pro UI'
    }
  }));
}

export async function planReclamation(id, technician, payload = {}) {
  return mapReclamationFromApi(await apiRequest(`/api/reclamations/${getTechnicalReclamationId(id)}/plan`, {
    method: 'PATCH',
    body: {
      technicianId: technician?.technicalId || technician?.id,
      technicianName: technician?.name || `${technician?.firstName || ''} ${technician?.lastName || ''}`.trim(),
      plannedStartAt: payload.plannedStartAt || new Date().toISOString(),
      plannedEndAt: payload.plannedEndAt || null,
      planningNote: payload.planningNote || 'Planned from SAV Pro UI'
    }
  }));
}

export async function overridePriority(id, priority, reason = 'Priority updated from SAV Pro UI') {
  return apiRequest(`/api/reclamations/${getTechnicalReclamationId(id)}/override-priority`, {
    method: 'POST',
    body: { priority: toApiPriority(priority), reason }
  });
}

export async function getReclamationHistory(id) {
  return apiRequest(`/api/reclamations/${getTechnicalReclamationId(id)}/history`);
}
