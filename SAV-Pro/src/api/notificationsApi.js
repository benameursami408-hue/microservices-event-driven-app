import { apiRequest } from './apiClient';

function mapNotificationFromApi(item = {}) {
  return {
    id: String(item.id),
    technicalId: item.id,
    type: item.type || 'bell',
    title: item.title || item.type || 'Notification',
    message: item.message || '',
    recipientRole: item.recipientRole || '',
    userId: item.userId || '',
    read: Boolean(item.isRead || item.read),
    status: item.status || 'Sent',
    time: item.createdAt ? new Date(item.createdAt).toLocaleString() : 'now',
    createdAt: item.createdAt,
    raw: item
  };
}

export async function listNotifications(take = 50) {
  const rows = await apiRequest('/api/notifications', { query: { take } });
  return (rows || []).map(mapNotificationFromApi);
}

export async function markNotificationRead(id) {
  return apiRequest(`/api/notifications/${id}/read`, { method: 'PATCH' });
}

export async function markAllNotificationsRead() {
  return apiRequest('/api/notifications/read-all', { method: 'PATCH' });
}

export async function deleteNotification(id) {
  return apiRequest(`/api/notifications/${id}`, { method: 'DELETE' });
}
