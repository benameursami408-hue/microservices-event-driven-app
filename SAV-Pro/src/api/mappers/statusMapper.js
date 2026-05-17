const reclamationToApi = {
  Open: 'Open',
  Assigned: 'Assigned',
  Planned: 'Planned',
  'In Progress': 'InProgress',
  InProgress: 'InProgress',
  Resolved: 'Resolved',
  Closed: 'Closed',
  Cancelled: 'Cancelled',
  Rejected: 'Rejected'
};

const reclamationFromApi = {
  0: 'Open',
  1: 'Assigned',
  2: 'Planned',
  3: 'In Progress',
  4: 'Resolved',
  5: 'Closed',
  6: 'Cancelled',
  7: 'Rejected',
  '0': 'Open',
  '1': 'Assigned',
  '2': 'Planned',
  '3': 'In Progress',
  '4': 'Resolved',
  '5': 'Closed',
  '6': 'Cancelled',
  '7': 'Rejected',
  Open: 'Open',
  Assigned: 'Assigned',
  Planned: 'Planned',
  InProgress: 'In Progress',
  Resolved: 'Resolved',
  Closed: 'Closed',
  Cancelled: 'Cancelled',
  Rejected: 'Rejected'
};

const interventionFromApi = {
  Ready: 'Ready',
  Started: 'In Progress',
  Paused: 'In Progress',
  Completed: 'Completed',
  Aborted: 'Cancelled'
};

const interventionToApi = {
  Ready: 'Ready',
  'In Progress': 'Started',
  Started: 'Started',
  Completed: 'Completed',
  Cancelled: 'Aborted'
};

export function toApiReclamationStatus(status) {
  return reclamationToApi[status] || status;
}

export function fromApiReclamationStatus(status) {
  return reclamationFromApi[status] || status || 'Open';
}

export function toApiInterventionStatus(status) {
  return interventionToApi[status] || status;
}

export function fromApiInterventionStatus(status) {
  return interventionFromApi[status] || status || 'Ready';
}

export function fromApiPlanningStatus(status) {
  if (status === 'InProgress') return 'Pending';
  if (status === 'Satisfied') return 'Confirmed';
  return status || 'Pending';
}

export function fromApiAppointmentStatus(status) {
  return status || 'Proposed';
}
