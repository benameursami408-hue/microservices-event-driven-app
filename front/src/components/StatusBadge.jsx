import Badge from './Badge.jsx'
import {
  priorityBadgeClasses,
  priorityLabel,
  reclamationStatusBadgeClasses,
  reclamationStatusLabel,
  roleLabel,
  slaStatusBadgeClasses,
  slaStatusLabel,
} from '../utils/enums.js'

const ACTIVE_CLASSES = 'bg-emerald-50 text-emerald-700 ring-emerald-200'
const INACTIVE_CLASSES = 'bg-slate-100 text-slate-700 ring-slate-200'
const ROLE_CLASSES = {
  ADMIN: 'bg-slate-900 text-white ring-slate-900/10',
  SAV: 'bg-cyan-50 text-cyan-700 ring-cyan-200',
  ST: 'bg-amber-50 text-amber-800 ring-amber-200',
  CLIENT: 'bg-emerald-50 text-emerald-700 ring-emerald-200',
}
const TONE_CLASSES = {
  neutral: 'bg-slate-100 text-slate-700 ring-slate-200',
  info: 'bg-cyan-50 text-cyan-700 ring-cyan-200',
  success: 'bg-emerald-50 text-emerald-700 ring-emerald-200',
  warning: 'bg-amber-50 text-amber-800 ring-amber-200',
  danger: 'bg-rose-50 text-rose-700 ring-rose-200',
}

export default function StatusBadge({ kind = 'neutral', value, label, tone = 'neutral', className }) {
  let nextLabel = label
  let nextClassName = className

  if (kind === 'priority') {
    nextLabel = label ?? priorityLabel(value)
    nextClassName = priorityBadgeClasses(value)
  } else if (kind === 'status') {
    nextLabel = label ?? reclamationStatusLabel(value)
    nextClassName = reclamationStatusBadgeClasses(value)
  } else if (kind === 'sla') {
    nextLabel = label ?? slaStatusLabel(value)
    nextClassName = slaStatusBadgeClasses(value)
  } else if (kind === 'role') {
    const key = String(value ?? label ?? '').toUpperCase()
    nextLabel = label ?? roleLabel(value)
    nextClassName = ROLE_CLASSES[key] ?? TONE_CLASSES.info
  } else if (kind === 'active') {
    nextLabel = label ?? (value ? 'Actif' : 'Inactif')
    nextClassName = value ? ACTIVE_CLASSES : INACTIVE_CLASSES
  } else {
    nextLabel = label ?? String(value ?? '')
    nextClassName = TONE_CLASSES[tone] ?? TONE_CLASSES.neutral
  }

  return <Badge className={nextClassName}>{nextLabel}</Badge>
}
