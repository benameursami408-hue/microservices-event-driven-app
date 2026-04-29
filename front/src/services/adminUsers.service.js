import api from './api.js'
import { UserRole } from '../utils/enums.js'

const ROLE_ALIASES = {
  'ADMIN SAV': 'SAV',
  ADMIN_SAV: 'SAV',
  'SERVICE APRES VENTE': 'SAV',
  'AFTER SALES': 'SAV',
  TECHNICIEN: 'ST',
  TECHNICIAN: 'ST',
}

function normalizeRole(role) {
  if (role == null) return role
  if (typeof role === 'number') return role

  const normalized = String(role).trim().toUpperCase()
  const roleKey = ROLE_ALIASES[normalized] ?? normalized
  return UserRole[roleKey]
}

function buildUserPayload(payload, { omitEmptyPassword = false } = {}) {
  const normalized = {
    firstName: payload.firstName?.trim() ?? '',
    lastName: payload.lastName?.trim() ?? '',
    email: payload.email?.trim() ?? '',
    phoneNumber: payload.phoneNumber?.trim() ?? '',
    address: payload.address?.trim() ?? '',
    role: normalizeRole(payload.role),
    isActive: Boolean(payload.isActive),
  }

  const password = typeof payload.password === 'string' ? payload.password : ''
  if (!omitEmptyPassword || password.trim() !== '') {
    normalized.password = password
  }

  return normalized
}

export async function listUsers({ role } = {}) {
  const params = {}
  if (role != null) {
    params.role = normalizeRole(role)
  }

  const { data } = await api.get('/api/admin/users', { params })
  return data
}

export async function createUser(payload) {
  const { data } = await api.post('/api/admin/users', buildUserPayload(payload), {
    headers: { 'Content-Type': 'application/json' },
  })
  return data
}

export async function updateUser(id, payload) {
  const { data } = await api.put(`/api/admin/users/${id}`, buildUserPayload(payload, { omitEmptyPassword: true }), {
    headers: { 'Content-Type': 'application/json' },
  })
  return data
}

export async function deleteUser(id) {
  await api.delete(`/api/admin/users/${id}`)
}
