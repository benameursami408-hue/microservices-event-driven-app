import api from './api.js'

export async function getNotifications({ take = 50 } = {}) {
  const { data } = await api.get('/api/notifications', { params: { take } })
  return data
}

export async function markNotificationAsRead(id) {
  await api.patch(`/api/notifications/${id}/read`)
}

export async function markAllNotificationsAsRead() {
  const { data } = await api.patch('/api/notifications/read-all')
  return data
}
