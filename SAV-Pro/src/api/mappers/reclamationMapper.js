import { fromApiPriority, toApiPriority } from './priorityMapper';
import { fromApiReclamationStatus } from './statusMapper';

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
    priority: fromApiPriority(item.priority),
    status: fromApiReclamationStatus(item.status),
    created: formatDate(item.createdAt),
    createdShort: formatDate(item.createdAt).split(',')[0],
    assigned: item.technicianName || item.savName || '',
    assignedAvatar: item.technicianId || item.savId || '',
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
  return {
    clientId: payload.clientId ? Number(payload.clientId) : undefined,
    description: payload.description || '',
    priority: toApiPriority(payload.priority),
    isBlocking: ['Urgent', 'Critical'].includes(payload.priority),
    followUpCount: 0,
    productName: payload.product || '',
    brand: payload.brand || '',
    model: payload.model || '',
    serialNumber: payload.serial || '',
    productReference: payload.productReference || payload.serial || '',
    sellerName: payload.sellerName || '',
    purchaseProofUrl: payload.purchaseProofUrl || ''
  };
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
