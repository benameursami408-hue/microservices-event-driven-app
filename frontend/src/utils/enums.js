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
