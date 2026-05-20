import { ApiError, apiRequest } from './apiClient';
import { listUsers } from './usersApi';

const endpoints = {
  reclamationStats: '/api/reclamations/admin/statistics',
  savAgents: '/api/reclamations/admin/statistics/sav-agents',
  interventionStats: '/api/interventions/admin/statistics',
  technicians: '/api/interventions/admin/statistics/technicians'
};

const emptyReclamationStats = {
  totalReclamations: 0,
  openReclamations: 0,
  inProgressReclamations: 0,
  resolvedReclamations: 0,
  closedReclamations: 0,
  byStatus: [],
  trend: []
};

const emptyInterventionStats = {
  totalInterventions: 0,
  plannedInterventions: 0,
  inProgressInterventions: 0,
  completedInterventions: 0,
  cancelledInterventions: 0,
  byStatus: []
};

const reclamationStatusLabels = {
  0: 'Ouvertes',
  1: 'Assignees',
  2: 'Planifiees',
  3: 'En cours',
  4: 'Resolues',
  5: 'Cloturees',
  6: 'Annulees',
  7: 'Rejetees',
  Open: 'Ouvertes',
  Assigned: 'Assignees',
  Planned: 'Planifiees',
  InProgress: 'En cours',
  Resolved: 'Resolues',
  Closed: 'Cloturees',
  Cancelled: 'Annulees',
  Rejected: 'Rejetees'
};

const interventionStatusLabels = {
  0: 'Planifiees',
  1: 'En cours',
  2: 'En pause',
  3: 'Terminees',
  4: 'Annulees',
  Ready: 'Planifiees',
  Started: 'En cours',
  Paused: 'En pause',
  Completed: 'Terminees',
  Aborted: 'Annulees'
};

function labelStatus(value, labels) {
  return labels[value] || labels[String(value)] || String(value || 'Inconnu');
}

function mapStatusCounts(rows = [], labels) {
  return rows.map(item => ({
    status: item.status,
    name: labelStatus(item.status, labels),
    value: Number(item.count || 0)
  }));
}

function normalizeDate(value) {
  return value ? new Intl.DateTimeFormat('fr-FR', { day: '2-digit', month: 'short' }).format(new Date(value)) : '-';
}

function normalizeDateTime(value) {
  if (!value) return '-';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '-';
  return new Intl.DateTimeFormat('fr-FR', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  }).format(date);
}

function mergeUserStats(users = [], stats = [], idKey) {
  const usersById = new Map(users.map(user => [String(user.technicalId || user.id), user]));
  const statsById = new Map(stats.map(item => [String(item[idKey]), item]));
  const ids = new Set([...usersById.keys(), ...statsById.keys()]);

  return Array.from(ids).map(id => {
    const user = usersById.get(id);
    const item = statsById.get(id) || {};
    return {
      ...item,
      [idKey]: Number(item[idKey] || user?.technicalId || user?.id || id),
      fullName: user?.name || item.fullName || 'Non renseigne',
      email: user?.email || item.email || '',
      isActive: user?.isActive !== false,
      lastActivityLabel: normalizeDateTime(item.lastActivityAt || item.lastInterventionAt)
    };
  });
}

function readSettled(result, fallback) {
  return result.status === 'fulfilled' ? (result.value ?? fallback) : fallback;
}

function readSettledArray(result) {
  return result.status === 'fulfilled' && Array.isArray(result.value) ? result.value : [];
}

function readFailureStatus(...results) {
  return results.find(result => result.status === 'rejected' && result.reason?.status)?.reason.status || 0;
}

export async function getAdminStatistics() {
  const [reclamationResult, savResult, interventionResult, technicianResult, savUsersResult, technicianUsersResult] = await Promise.allSettled([
    apiRequest(endpoints.reclamationStats),
    apiRequest(endpoints.savAgents),
    apiRequest(endpoints.interventionStats),
    apiRequest(endpoints.technicians),
    listUsers('SAV'),
    listUsers('Technician')
  ]);

  if (reclamationResult.status === 'rejected' && interventionResult.status === 'rejected') {
    throw new ApiError('Unable to load admin statistics.', {
      status: readFailureStatus(reclamationResult, interventionResult),
      details: {
        reclamations: reclamationResult.reason,
        interventions: interventionResult.reason
      },
      path: `${endpoints.reclamationStats}, ${endpoints.interventionStats}`,
      userMessage: 'Impossible de charger les statistiques.'
    });
  }

  const reclamationStats = readSettled(reclamationResult, emptyReclamationStats);
  const savStats = readSettledArray(savResult);
  const interventionStats = readSettled(interventionResult, emptyInterventionStats);
  const technicianStats = readSettledArray(technicianResult);
  const savUsers = readSettledArray(savUsersResult);
  const technicianUsers = readSettledArray(technicianUsersResult);

  const savUsersForStats = savResult.status === 'fulfilled' ? savUsers : [];
  const technicianUsersForStats = technicianResult.status === 'fulfilled' ? technicianUsers : [];

  const savAgents = mergeUserStats(savUsersForStats, savStats || [], 'userId')
    .map(item => ({
      ...item,
      assignedCount: Number(item.assignedCount || 0),
      handledCount: Number(item.handledCount || 0),
      resolvedCount: Number(item.resolvedCount || 0),
      closedCount: Number(item.closedCount || 0),
      resolutionRate: Number(item.resolutionRate || 0)
    }))
    .sort((a, b) => b.handledCount - a.handledCount || a.fullName.localeCompare(b.fullName));

  const technicians = mergeUserStats(technicianUsersForStats, technicianStats || [], 'technicianId')
    .map(item => ({
      ...item,
      assignedCount: Number(item.assignedCount || 0),
      completedCount: Number(item.completedCount || 0),
      inProgressCount: Number(item.inProgressCount || 0),
      completionRate: Number(item.completionRate || 0)
    }))
    .sort((a, b) => b.completedCount - a.completedCount || a.fullName.localeCompare(b.fullName));

  return {
    reclamations: {
      ...reclamationStats,
      byStatus: mapStatusCounts(reclamationStats?.byStatus, reclamationStatusLabels),
      trend: (reclamationStats?.trend || []).map(item => ({
        date: item.date,
        name: normalizeDate(item.date),
        value: Number(item.count || 0)
      }))
    },
    interventions: {
      ...interventionStats,
      byStatus: mapStatusCounts(interventionStats?.byStatus, interventionStatusLabels)
    },
    savAgents,
    technicians,
    activeSavCount: savUsers.filter(user => user.isActive !== false).length,
    activeTechnicianCount: technicianUsers.filter(user => user.isActive !== false).length,
    loadedAt: new Date().toISOString()
  };
}
