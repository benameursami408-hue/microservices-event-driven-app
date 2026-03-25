import api from './api.js'

export async function listReclamations() {
  const { data } = await api.get('/api/reclamations')
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

export async function deleteReclamation(id) {
  await api.delete(`/api/reclamations/${id}`)
}
