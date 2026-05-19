import { fromApiPriority, toApiPriority } from './priorityMapper';
import { fromApiReclamationStatus } from './statusMapper';

export function isPendingPrioritySource(prioritySource) {
  const normalized = String(prioritySource ?? '').replace(/[_\s-]/g, '').toLowerCase();
  return normalized === '2' || normalized === 'pendingreview';
}

function formatDate(value) {
  if (!value) return '';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '';
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  }).format(date);
}

function slaLabel(item) {
  const deadline = item.resolutionDeadline || item.planningDeadline || item.firstResponseDeadline;
  if (!deadline) return item.slaStatus || 'SLA tracked';
  return `${item.slaStatus || 'SLA'} due ${formatDate(deadline)}`;
}

export function mapReclamationFromApi(item = {}) {
  const reference = item.reference || `REC-${item.id}`;
  const product = item.productName || item.productReference || item.brand || 'Product';
  const pendingPriority = isPendingPrioritySource(item.prioritySource);
  return {
    id: reference,
    technicalId: item.id,
    reference,
    client: item.clientName || `Client ${item.clientId || ''}`.trim(),
    clientId: String(item.clientId || ''),
    contact: item.clientName || '',
    email: item.customerEmail || '',
    phone: item.customerPhone || '',
    product,
    productModel: item.model || product,
    model: item.model || item.brand || '-',
    serial: item.serialNumber || item.barcode || item.productReference || '-',
    priority: pendingPriority ? 'Pending Review' : fromApiPriority(item.priority),
    prioritySource: item.prioritySource,
    isPriorityPendingReview: pendingPriority,
    status: fromApiReclamationStatus(item.status),
    created: formatDate(item.createdAt),
    createdShort: formatDate(item.createdAt).split(',')[0],
    assigned: item.technicianName || item.savName || '',
    assignedAvatar: item.technicianId || item.savId || '',
    claimedBySavId: item.claimedBySavId || null,
    claimedBySavName: item.claimedBySavName || '',
    claimedAt: item.claimedAt || null,
    releasedAt: item.releasedAt || null,
    isClaimed: Boolean(item.isClaimed),
    isClaimedByCurrentUser: Boolean(item.isClaimedByCurrentUser),
    canCurrentUserWorkOnIt: Boolean(item.canCurrentUserWorkOnIt),
    ownershipLabel: item.ownershipLabel || (item.isClaimed ? `Taken by ${item.claimedBySavName || 'SAV'}` : 'Available'),
    planningRequestedAt: item.planningRequestedAt || null,
    technicianId: item.technicianId || '',
    description: item.description || '',
    source: 'Backend',
    warranty: 'Unknown',
    site: item.serviceAddress || '',
    location: item.serviceAddress || '',
    sla: slaLabel(item),
    slaGoal: item.activeTarget || 'Tracked',
    progress: item.slaStatus === 'Breached' ? 100 : item.slaStatus === 'NearBreach' ? 75 : 35,
    history: [],
    allowedActions: item.allowedActions || [],
    raw: item
  };
}

export function mapReclamationToCreateDto(payload = {}) {
  const dto = {
    clientId: payload.clientId ? Number(payload.clientId) : undefined,
    description: payload.description || '',
    followUpCount: 0,
    productName: payload.product || '',
    brand: payload.brand || '',
    model: payload.model || '',
    serialNumber: payload.serial || '',
    productReference: payload.productReference || payload.serial || '',
    sellerName: payload.sellerName || '',
    purchaseProofUrl: payload.purchaseProofUrl || ''
  };

  const priority = toApiPriority(payload.priority);
  if (priority) {
    dto.priority = priority;
    dto.isBlocking = ['Urgent', 'Critical'].includes(payload.priority);
  }

  return dto;
}

export function mapReclamationToUpdateDto(payload = {}) {
  const dto = mapReclamationToCreateDto(payload);
  delete dto.clientId;
  return dto;
}

export function getTechnicalReclamationId(reclamationOrId) {
  if (typeof reclamationOrId === 'object') {
    return reclamationOrId.technicalId || reclamationOrId.raw?.id || reclamationOrId.id;
  }
  return reclamationOrId;
}
