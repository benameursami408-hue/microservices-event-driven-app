import api from './api.js'
import { UserRole } from '../utils/enums.js'

const ROLE_ALIASES = {
  'ADMIN SAV': 'SAV',
  ADMIN_SAV: 'SAV',
  'SERVICE APRES VENTE': 'SAV',
  'AFTER SALES': 'SAV',
  TECHNICIEN: 'ST',
  TECHNICIAN: 'ST',
  TECHNIQUE: 'ST',
}

function normalizeRole(role) {
  if (role == null || role === '') return undefined

  if (typeof role === 'number') {
    if (Object.values(UserRole).includes(role)) return role
    throw new Error(`Role invalide: ${role}`)
  }

  const normalized = String(role).trim().toUpperCase()
  const roleKey = ROLE_ALIASES[normalized] ?? normalized
  const roleValue = UserRole[roleKey]

  if (roleValue == null) {
    throw new Error(`Role invalide: ${role}`)
  }

  return roleValue
}

function normalizeBoolean(value) {
  if (typeof value === 'boolean') return value
  return String(value).toLowerCase() === 'true'
}

function buildUserPayload(payload, { omitEmptyPassword = false } = {}) {
  const normalized = {
    firstName: payload.firstName?.trim() ?? '',
    lastName: payload.lastName?.trim() ?? '',
    email: payload.email?.trim().toLowerCase() ?? '',
    phoneNumber: payload.phoneNumber?.trim() ?? '',
    address: payload.address?.trim() ?? '',
    role: normalizeRole(payload.role),
    isActive: normalizeBoolean(payload.isActive),
  }

  const password = typeof payload.password === 'string' ? payload.password.trim() : ''
  if (!omitEmptyPassword || password !== '') {
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
