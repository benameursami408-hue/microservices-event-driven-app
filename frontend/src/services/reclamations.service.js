import api from './api.js'

export async function listReclamations() {
  const { data } = await api.get('/api/reclamations')
  return data
}

export async function queryReclamations({ page = 1, pageSize = 20, status, priority, search } = {}) {
  const params = { page, pageSize }
  if (status != null) params.status = status
  if (priority != null) params.priority = priority
  if (search) params.search = search

  const { data } = await api.get('/api/reclamations/query', { params })
  return data
}

export async function getReclamation(id) {
  const { data } = await api.get(`/api/reclamations/${id}`)
  return data
}

export async function createReclamation(payload) {
  const { data } = await api.post('/api/reclamations', payload)
  return data
}

export async function updateReclamation(id, payload) {
  const { data } = await api.put(`/api/reclamations/${id}`, payload)
  return data
}

export async function getReclamationHistory(id) {
  const { data } = await api.get(`/api/reclamations/${id}/history`)
  return data
}

export async function assignReclamation(id, payload = {}) {
  const { data } = await api.patch(`/api/reclamations/${id}/assign`, payload)
  return data
}

export async function planReclamation(id, payload) {
  const { data } = await api.patch(`/api/reclamations/${id}/plan`, payload)
  return data
}

export async function startReclamation(id) {
  const { data } = await api.patch(`/api/reclamations/${id}/start`)
  return data
}

export async function resolveReclamation(id, payload) {
  const { data } = await api.patch(`/api/reclamations/${id}/resolve`, payload)
  return data
}

export async function closeReclamation(id, payload = {}) {
  const { data } = await api.patch(`/api/reclamations/${id}/close`, payload)
  return data
}

export async function cancelReclamation(id) {
  const { data } = await api.patch(`/api/reclamations/${id}/cancel`)
  return data
}

export async function rejectReclamation(id, payload) {
  const { data } = await api.patch(`/api/reclamations/${id}/reject`, payload)
  return data
}

export async function deleteReclamation(id) {
  await api.delete(`/api/reclamations/${id}`)
}
