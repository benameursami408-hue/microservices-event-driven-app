export const UserRole = {
  CLIENT: 0,
  SAV: 1,
  ADMIN: 2,
  ST: 3,
}

export function roleLabel(role) {
  if (role == null) return 'Unknown'

  if (typeof role === 'string') {
    const normalized = role.toUpperCase()
    if (normalized === '0') return 'Client'
    if (normalized === '1') return 'SAV'
    if (normalized === '2') return 'Admin'
    if (normalized === '3') return 'Technique'
    if (normalized === 'ST') return 'Technique'
    if (normalized === 'SAV') return 'SAV'
    if (normalized === 'ADMIN') return 'Admin'
    if (normalized === 'CLIENT') return 'Client'
    return role
  }

  switch (role) {
    case 0:
      return 'Client'
    case 1:
      return 'SAV'
    case 2:
      return 'Admin'
    case 3:
      return 'Technique'
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

export const SlaStatus = {
  OnTrack: 0,
  NearBreach: 1,
  Breached: 2,
  Completed: 3,
}

export function reclamationStatusLabel(value) {
  if (value == null) return 'Unknown'

  if (typeof value === 'string') {
    const normalized = value.toUpperCase()
    if (normalized === 'OPEN') return 'Nouveau'
    if (normalized === 'ASSIGNED') return 'Affecte'
    if (normalized === 'PLANNED') return 'Planifie'
    if (normalized === 'IN_PROGRESS') return 'En cours'
    if (normalized === 'RESOLVED') return 'Traite'
    if (normalized === 'CLOSED') return 'Ferme'
    if (normalized === 'CANCELLED') return 'Annule'
    if (normalized === 'REJECTED') return 'Rejete'
    return value
  }

  switch (value) {
    case 0:
      return 'Nouveau'
    case 1:
      return 'Affecte'
    case 2:
      return 'Planifie'
    case 3:
      return 'En cours'
    case 4:
      return 'Traite'
    case 5:
      return 'Ferme'
    case 6:
      return 'Annule'
    case 7:
      return 'Rejete'
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
      return 'Faible'
    case 1:
      return 'Moyenne'
    case 2:
      return 'Haute'
    case 3:
      return 'Urgente'
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

export function prioritySourceLabel(value) {
  switch (value) {
    case 1:
    case 'ManualOverride':
      return 'Manuel'
    case 0:
    case 'Rules':
    default:
      return 'Automatique'
  }
}

export function slaStatusLabel(value) {
  switch (value) {
    case 0:
    case 'OnTrack':
      return 'Dans les delais'
    case 1:
    case 'NearBreach':
      return 'Attention SLA'
    case 2:
    case 'Breached':
      return 'SLA depasse'
    case 3:
    case 'Completed':
      return 'Termine'
    default:
      return String(value)
  }
}

export function slaStatusBadgeClasses(value) {
  switch (value) {
    case 2:
    case 'Breached':
      return 'bg-rose-50 text-rose-700 ring-rose-200'
    case 1:
    case 'NearBreach':
      return 'bg-amber-50 text-amber-800 ring-amber-200'
    case 3:
    case 'Completed':
      return 'bg-emerald-50 text-emerald-700 ring-emerald-200'
    case 0:
    case 'OnTrack':
    default:
      return 'bg-sky-50 text-sky-700 ring-sky-200'
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
