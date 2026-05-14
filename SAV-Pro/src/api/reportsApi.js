import { apiRequest } from './apiClient';
import { mapPublishReportToDto, mapReportFromApi } from './mappers/reportMapper';

export async function publishInterventionReport(interventionId, report) {
  return apiRequest(`/api/interventions/${interventionId}/publish-report`, {
    method: 'POST',
    body: mapPublishReportToDto(report)
  });
}

export async function listVisitReports() {
  const rows = await apiRequest('/api/visit-reports');
  return (rows || []).map(mapReportFromApi);
}

export async function listMyVisitReports() {
  const rows = await apiRequest('/api/visit-reports/mine');
  return (rows || []).map(mapReportFromApi);
}

export async function getVisitReport(id) {
  return mapReportFromApi(await apiRequest(`/api/visit-reports/${id}`));
}

export async function updateVisitReport(id, payload) {
  return mapReportFromApi(await apiRequest(`/api/visit-reports/${id}`, {
    method: 'PATCH',
    body: mapPublishReportToDto(payload)
  }));
}

export async function publishVisitReport(id) {
  return mapReportFromApi(await apiRequest(`/api/visit-reports/${id}/publish`, { method: 'POST' }));
}
