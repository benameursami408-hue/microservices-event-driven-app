import { apiRequest } from './apiClient';

function normalizeNotificationType(item = {}) {
  const text = `${item.type || ''} ${item.title || ''} ${item.message || ''}`.toLowerCase();
  if (text.includes('sla')) return 'sla';
  if (text.includes('planning') || text.includes('appointment') || text.includes('visit')) return 'calendar';
  if (text.includes('assign') || text.includes('technician')) return 'assignment';
  if (text.includes('priority')) return 'warning';
  if (text.includes('report')) return 'report';
  if (text.includes('created') || text.includes('resolved') || text.includes('closed')) return 'success';
  return item.type || 'bell';
}

function mapNotificationFromApi(item = {}) {
  return {
    id: String(item.id),
    technicalId: item.id,
    type: normalizeNotificationType(item),
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
