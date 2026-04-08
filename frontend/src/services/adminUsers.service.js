import api from './api.js'

export async function listUsers({ role } = {}) {
  const { data } = await api.get('/api/admin/users', { params: role ? { role } : {} })
  return data
}
