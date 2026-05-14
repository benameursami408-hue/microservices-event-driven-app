// Backend enum contains MEDUIM typo; keep this mapping until backend enum is migrated.
export const priorityToApi = {
  Low: 'LOW',
  Medium: 'MEDUIM',
  High: 'HIGH',
  Urgent: 'URGENT',
  Critical: 'URGENT',
  LOW: 'LOW',
  MEDUIM: 'MEDUIM',
  MEDIUM: 'MEDUIM',
  HIGH: 'HIGH',
  URGENT: 'URGENT'
};

export const priorityFromApi = {
  LOW: 'Low',
  MEDUIM: 'Medium',
  MEDIUM: 'Medium',
  HIGH: 'High',
  URGENT: 'Urgent',
  Low: 'Low',
  Medium: 'Medium',
  High: 'High',
  Urgent: 'Urgent'
};

export function toApiPriority(priority) {
  return priorityToApi[priority] || 'MEDUIM';
}

export function fromApiPriority(priority) {
  return priorityFromApi[priority] || 'Medium';
}
