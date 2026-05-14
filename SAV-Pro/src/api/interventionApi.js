import { apiRequest } from './apiClient';
import {
  mapCompletionToDto,
  mapDiagnosticToDto,
  mapInterventionFromApi,
  mapPartToDto,
  mapRepairActionToDto,
  mapStatusToDto
} from './mappers/interventionMapper';
import { mapPublishReportToDto, mapReportFromApi } from './mappers/reportMapper';

export async function getMyInterventions() {
  const rows = await apiRequest('/api/interventions/my');
  return (rows || []).map(mapInterventionFromApi);
}

export async function listMyInterventions() {
  return getMyInterventions();
}

export async function listInterventions(filters = {}) {
  const rows = await apiRequest('/api/interventions', { query: filters });
  return (rows || []).map(mapInterventionFromApi);
}

export async function getInterventionById(id) {
  return mapInterventionFromApi(await apiRequest(`/api/interventions/${id}`));
}

export async function updateInterventionStatus(id, status) {
  return mapInterventionFromApi(await apiRequest(`/api/interventions/${id}/status`, {
    method: 'PATCH',
    body: mapStatusToDto(status)
  }));
}

export async function startIntervention(id) {
  return mapInterventionFromApi(await apiRequest(`/api/interventions/${id}/start`, { method: 'POST' }));
}

export async function addDiagnostic(id, diagnostic) {
  return mapInterventionFromApi(await apiRequest(`/api/interventions/${id}/diagnostic`, {
    method: 'POST',
    body: mapDiagnosticToDto(diagnostic)
  }));
}

export async function addRepairAction(id, action) {
  return mapInterventionFromApi(await apiRequest(`/api/interventions/${id}/repair-actions`, {
    method: 'POST',
    body: mapRepairActionToDto(action)
  }));
}

export async function addPart(id, part) {
  return mapInterventionFromApi(await apiRequest(`/api/interventions/${id}/parts-used`, {
    method: 'POST',
    body: mapPartToDto(part)
  }));
}

export async function addEvidence(id, evidence) {
  return mapInterventionFromApi(await apiRequest(`/api/interventions/${id}/evidences`, {
    method: 'POST',
    body: {
      kind: evidence?.kind || 'Photo',
      url: evidence?.url || evidence?.fileUrl || 'uploaded-from-ui'
    }
  }));
}

export async function completeIntervention(id, payload) {
  return mapInterventionFromApi(await apiRequest(`/api/interventions/${id}/complete`, {
    method: 'POST',
    body: mapCompletionToDto(payload)
  }));
}

export async function createVisitReport(payload = {}) {
  if (!payload.interventionId) {
    throw new Error('interventionId is required to create a visit report.');
  }

  return apiRequest(`/api/interventions/${payload.interventionId}/publish-report`, {
    method: 'POST',
    body: mapPublishReportToDto(payload)
  });
}

export async function getVisitReports({ mine = false } = {}) {
  const rows = await apiRequest(mine ? '/api/visit-reports/mine' : '/api/visit-reports');
  return (rows || []).map(mapReportFromApi);
}
