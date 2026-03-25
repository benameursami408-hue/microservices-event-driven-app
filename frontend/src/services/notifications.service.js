import api from './api.js'

export async function getNotifications({ take = 50 } = {}) {
  const { data } = await api.get('/api/notifications', { params: { take } })
  return data
}
