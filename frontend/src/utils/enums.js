export const UserRole = {
  CLIENT: 0,
  SAV: 1,
  ADMIN: 2,
  ST: 3,
}

export function roleLabel(role) {
  if (role == null) return 'Unknown'

  if (typeof role === 'string') return role

  switch (role) {
    case 0:
      return 'CLIENT'
    case 1:
      return 'SAV'
    case 2:
      return 'ADMIN'
    case 3:
      return 'ST'
    default:
      return String(role)
  }
}

export const Priority = {
  LOW: 0,
  MEDUIM: 1,
  HIGH: 2,
  URGENT: 3,
}

export const ReclamationStatus = {
  Open: 0,
  Assigned: 1,
  Planned: 2,
  InProgress: 3,
  Resolved: 4,
  Closed: 5,
  Cancelled: 6,
  Rejected: 7,
}

export function reclamationStatusLabel(value) {
  if (value == null) return 'Unknown'

  if (typeof value === 'string') return value

  switch (value) {
    case 0:
      return 'OPEN'
    case 1:
      return 'ASSIGNED'
    case 2:
      return 'PLANNED'
    case 3:
      return 'IN_PROGRESS'
    case 4:
      return 'RESOLVED'
    case 5:
      return 'CLOSED'
    case 6:
      return 'CANCELLED'
    case 7:
      return 'REJECTED'
    default:
      return String(value)
  }
}

export function reclamationStatusBadgeClasses(value) {
  switch (value) {
    case 5:
      return 'bg-slate-50 text-slate-700 ring-slate-200'
    case 4:
      return 'bg-emerald-50 text-emerald-700 ring-emerald-200'
    case 3:
      return 'bg-amber-50 text-amber-800 ring-amber-200'
    case 2:
      return 'bg-sky-50 text-sky-700 ring-sky-200'
    case 1:
      return 'bg-cyan-50 text-cyan-800 ring-cyan-200'
    case 6:
    case 7:
      return 'bg-rose-50 text-rose-700 ring-rose-200'
    case 0:
    default:
      return 'bg-slate-50 text-slate-700 ring-slate-200'
  }
}

export function priorityLabel(value) {
  switch (value) {
    case 0:
      return 'LOW'
    case 1:
      return 'MEDUIM'
    case 2:
      return 'HIGH'
    case 3:
      return 'URGENT'
    default:
      return String(value)
  }
}

export function priorityBadgeClasses(value) {
  switch (value) {
    case 3:
      return 'bg-rose-50 text-rose-700 ring-rose-200'
    case 2:
      return 'bg-amber-50 text-amber-800 ring-amber-200'
    case 1:
      return 'bg-sky-50 text-sky-700 ring-sky-200'
    case 0:
    default:
      return 'bg-slate-50 text-slate-700 ring-slate-200'
  }
}

export function notificationStatusLabel(value) {
  switch (value) {
    case 0:
      return 'Pending'
    case 1:
      return 'Sent'
    case 2:
      return 'Failed'
    default:
      return String(value)
  }
}
