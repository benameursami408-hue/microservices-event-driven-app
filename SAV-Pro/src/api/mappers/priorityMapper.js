// Backend enum contains MEDUIM typo; keep this mapping until backend enum is migrated.
export const priorityToApi = {
  0: 'LOW',
  1: 'MEDUIM',
  2: 'HIGH',
  3: 'URGENT',
  '0': 'LOW',
  '1': 'MEDUIM',
  '2': 'HIGH',
  '3': 'URGENT',
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
  0: 'Low',
  1: 'Medium',
  2: 'High',
  3: 'Urgent',
  '0': 'Low',
  '1': 'Medium',
  '2': 'High',
  '3': 'Urgent',
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
  if (priority === undefined || priority === null || priority === '') return undefined;
  return priorityToApi[priority];
}

export function fromApiPriority(priority) {
  if (priority === undefined || priority === null || priority === '') return undefined;
  return priorityFromApi[priority];
}
