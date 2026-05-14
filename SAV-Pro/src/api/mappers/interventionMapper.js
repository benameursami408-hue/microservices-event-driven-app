function normalize(value) {
  return String(value ?? '').trim().replace(/[-_\s]+/g, '_').toUpperCase();
}

const numericInterventionStatuses = {
  0: 'Planifiée',
  1: 'En cours',
  2: 'En cours',
  3: 'Terminée',
  4: 'Annulée'
};

const interventionStatusFromApi = {
  READY: 'Planifiée',
  PLANNED: 'Planifiée',
  SCHEDULED: 'Planifiée',
  PENDING: 'En attente',
  STARTED: 'En cours',
  IN_PROGRESS: 'En cours',
  PAUSED: 'En cours',
  COMPLETED: 'Terminée',
  DONE: 'Terminée',
  CANCELLED: 'Annulée',
  CANCELED: 'Annulée',
  ABORTED: 'Annulée'
};

const interventionStatusToApi = {
  'Planifiée': 'Ready',
  'En attente': 'Ready',
  'En cours': 'Started',
  'Terminée': 'Completed',
  'Annulée': 'Aborted',
  Ready: 'Ready',
  Started: 'Started',
  Paused: 'Paused',
  Completed: 'Completed',
  Aborted: 'Aborted',
  PLANNED: 'Ready',
  SCHEDULED: 'Ready',
  IN_PROGRESS: 'Started',
  DONE: 'Completed',
  CANCELLED: 'Aborted'
};

const numericOutcomes = {
  0: 'Solved',
  1: 'TemporaryFix',
  2: 'NeedsReplanning',
  3: 'NeedsPart',
  4: 'UnableToAccess',
  5: 'CustomerAbsent',
  6: 'NotRepairable'
};

const outcomeToApi = {
  Fixed: 'Solved',
  Solved: 'Solved',
  TemporaryFix: 'TemporaryFix',
  NeedsReplanning: 'NeedsReplanning',
  NeedsPart: 'NeedsPart',
  UnableToAccess: 'UnableToAccess',
  CustomerAbsent: 'CustomerAbsent',
  NotRepairable: 'NotRepairable',
  NotFixed: 'NotRepairable'
};

export function fromApiInterventionStatus(status) {
  if (typeof status === 'number') return numericInterventionStatuses[status] || 'Statut inconnu';
  return interventionStatusFromApi[normalize(status)] || status || 'Statut inconnu';
}

export function toApiInterventionStatus(status) {
  return interventionStatusToApi[status] || interventionStatusToApi[normalize(status)] || status || 'Ready';
}

export function getInterventionStatusTone(status, item = {}) {
  if (isInterventionLate(item)) return 'danger';
  const normalized = normalize(status);
  if (normalized.includes('COURS')) return 'warning';
  if (normalized.includes('TERMINE')) return 'success';
  if (normalized.includes('ANNULE')) return 'muted';
  if (normalized.includes('ATTENTE')) return 'warning';
  if (normalized.includes('PLANIF')) return 'info';
  return 'muted';
}

export function isInterventionLate(item = {}) {
  const dateValue = item.scheduledAt || item.startAt || item.plannedAt || item.date;
  if (!dateValue) return false;
  const scheduled = new Date(dateValue);
  if (Number.isNaN(scheduled.getTime())) return false;
  return scheduled.getTime() < Date.now() && !['Terminée', 'Annulée'].includes(item.status);
}

function toShortDate(value) {
  if (!value) return '';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '';
  return new Intl.DateTimeFormat('fr-FR', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  }).format(date);
}

function normalizeOutcome(outcome) {
  if (typeof outcome === 'number') return numericOutcomes[outcome] || 'Solved';
  return outcome || 'Solved';
}

export function mapInterventionFromApi(item = {}) {
  const status = fromApiInterventionStatus(item.status);
  const reference = item.reference || `INT-${String(item.id || '').slice(0, 8)}`;
  const scheduledAt = item.startAt || item.scheduledAt || item.plannedAt || item.appointmentStartAt || item.raw?.startAt || '';

  return {
    uid: String(item.id || reference),
    technicalId: item.id,
    id: reference,
    reference,
    appointmentId: item.appointmentId || '',
    reclamation: item.reclamationReference || item.reclamation || item.reference || item.reclamationId || '-',
    reclamationId: item.reclamationId,
    client: item.clientName || item.customerName || item.customer || item.client || 'Client non renseigné',
    clientId: item.clientId || '',
    address: item.serviceAddress || item.address || item.location || '',
    site: item.serviceAddress || item.address || item.location || '',
    technicianId: item.technicianId,
    technician: item.technicianName || 'Technicien',
    product: item.productName || item.product || item.equipmentName || item.reference || 'Équipement SAV',
    equipment: item.productName || item.product || item.equipmentName || 'Équipement SAV',
    model: item.model || item.productModel || item.brand || '-',
    serial: item.serialNumber || item.serial || '-',
    priority: item.priority || item.reclamationPriority || 'Medium',
    status,
    statusTone: getInterventionStatusTone(status, item),
    isLate: isInterventionLate({ ...item, status }),
    outcome: normalizeOutcome(item.outcome),
    scheduledAt,
    scheduledLabel: toShortDate(scheduledAt),
    createdAt: item.createdAt,
    startedAt: item.startedAt,
    endedAt: item.endedAt,
    description: item.description || item.issueDescription || item.latestReportSummary || 'Intervention assignée depuis le backend.',
    reportSummary: item.latestReportSummary || '',
    repairActions: item.repairActions || [],
    diagnostic: item.diagnostics?.map(diagnostic => diagnostic.summary || diagnostic.category).filter(Boolean) || [],
    parts: item.partsUsed || item.parts || [],
    evidence: item.evidences || item.evidence || [],
    allowedActions: item.allowedActions || [],
    raw: item
  };
}

export function mapDiagnosticToDto(input = {}) {
  return {
    category: input.category || 'General',
    summary: input.summary || input.description || 'Diagnostic ajouté depuis SAV Pro.',
    rootCause: input.rootCause || '',
    requiresParts: Boolean(input.requiresParts),
    requiresFollowUp: Boolean(input.requiresFollowUp)
  };
}

export function mapRepairActionToDto(input = {}) {
  return {
    actionType: input.actionType || 'Repair',
    description: input.description || input.label || 'Action ajoutée depuis SAV Pro.',
    success: input.success !== false
  };
}

export function mapPartToDto(input = {}) {
  return {
    partCode: input.code || input.partCode || 'N/A',
    label: input.description || input.label || 'Pièce utilisée',
    quantity: Number(input.quantity || 1),
    availabilityStatus: input.availabilityStatus || 'Used'
  };
}

export function mapCompletionToDto(input = {}) {
  const outcome = outcomeToApi[input.outcome] || input.outcome || 'Solved';
  return {
    outcome,
    needsReplanning: Boolean(input.needsReplanning) || outcome === 'NeedsReplanning'
  };
}

export function mapStatusToDto(status) {
  return {
    status: toApiInterventionStatus(status)
  };
}
