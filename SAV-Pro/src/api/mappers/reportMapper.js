export function mapReportFromApi(item = {}) {
  return {
    id: String(item.id),
    technicalId: item.id,
    interventionId: item.interventionId,
    reclamationId: item.reclamationId || '',
    client: item.clientName || '',
    clientId: item.clientId || '',
    technicianName: item.technicianName || '',
    status: item.status || 'Draft',
    summary: item.summary || '',
    rating: '',
    createdAt: item.createdAt,
    publishedAt: item.publishedAt,
    raw: item
  };
}

export function mapPublishReportToDto(report = {}) {
  return {
    summary: report.summary || 'Visit report published from SAV Pro UI.',
    outcome: report.outcome || 'Solved',
    customerPresent: report.customerPresent !== false,
    nextStep: report.nextStep || ''
  };
}
