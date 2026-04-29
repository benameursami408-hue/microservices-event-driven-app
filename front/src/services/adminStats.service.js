import api from './api.js'

export async function fetchReclamationStats({ days = 14, latest = 8 } = {}) {
  const { data } = await api.get('/api/reclamations/stats', { params: { days, latest } })
  return data
}

export async function fetchUserStats() {
  const { data } = await api.get('/api/admin/stats/users')
  return data
}
