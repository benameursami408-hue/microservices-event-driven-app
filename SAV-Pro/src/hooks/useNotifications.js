import { useCallback } from 'react';
import { deleteNotification, listNotifications, markAllNotificationsRead, markNotificationRead } from '../api/notificationsApi';
import { useApiResource } from './useApiResource';

export function useNotifications(take = 50, enabled = true) {
  const loader = useCallback(() => listNotifications(take), [take]);
  const resource = useApiResource(loader, { initialData: [], immediate: enabled });

  async function mutate(action) {
    const result = await action();
    await resource.reload();
    return result;
  }

  return {
    notifications: resource.data || [],
    loading: resource.loading,
    error: resource.error,
    reload: resource.reload,
    unreadCount: (resource.data || []).filter(item => !item.read).length,
    markRead: id => mutate(() => markNotificationRead(id)),
    markAllRead: () => mutate(() => markAllNotificationsRead()),
    remove: id => mutate(() => deleteNotification(id))
  };
}
