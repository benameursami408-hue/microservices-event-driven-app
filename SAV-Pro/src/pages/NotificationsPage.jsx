import { Bell, CheckCircle, Filter, MoreVertical, Trash2 } from 'lucide-react';
import { useMemo, useState } from 'react';
import { ApiErrorState } from '../components/common/ApiErrorState';
import { Badge, Button, Card, IconButton, NotificationItem, SearchInput } from '../components/ui';
import { useNotifications } from '../hooks/useNotifications';
import { getFriendlyApiError } from '../utils/errorMessages';

export function NotificationsPage({ notify, clientMode = false }) {
  const notificationResource = useNotifications(100);
  const [filter, setFilter] = useState('All');
  const [query, setQuery] = useState('');
  const notifications = useMemo(() => notificationResource.notifications
    .filter(item => filter === 'All' || (filter === 'Unread' ? !item.read : item.read))
    .filter(item => `${item.title} ${item.message} ${item.status}`.toLowerCase().includes(query.toLowerCase())), [notificationResource.notifications, filter, query]);
  const unreadCount = notificationResource.notifications.filter(item => !item.read).length;

  async function markAll() { try { await notificationResource.markAllRead(); notify('All notifications marked as read'); } catch (err) { notify(getFriendlyApiError(err), 'error'); } }
  async function markOne(item) { try { await notificationResource.markRead(item.technicalId || item.id); notify('Notification marked as read'); } catch (err) { notify(getFriendlyApiError(err), 'error'); } }
  async function removeOne(item) { try { await notificationResource.remove(item.technicalId || item.id); notify('Notification deleted'); } catch (err) { notify(getFriendlyApiError(err), 'error'); } }

  return (
    <section className={clientMode ? 'page-shell' : 'page-shell'}>
      <div className="page-title-row notifications-title-row"><div><span className="eyebrow">Workflow alerts</span><h1>Notifications</h1><p>Review workflow alerts and customer updates.</p></div><div className="page-title-kpis"><span><strong>{unreadCount}</strong><small>Unread</small></span><span><strong>{notificationResource.notifications.length}</strong><small>Total</small></span></div><Button icon={CheckCircle} onClick={markAll}>Mark all as read</Button></div>
      <Card title="Notifications" icon={Bell}>
        {notificationResource.error ? <ApiErrorState status={notificationResource.errorStatus} message={notificationResource.error} onRetry={notificationResource.reload} /> : null}
        <div className="table-toolbar"><SearchInput value={query} onChange={setQuery} placeholder="Search notifications..." /><Button icon={Filter} variant={filter === 'All' ? 'primary' : 'secondary'} onClick={() => setFilter('All')}>All</Button><Button icon={Bell} variant={filter === 'Unread' ? 'primary' : 'secondary'} onClick={() => setFilter('Unread')}>Unread</Button><Button icon={CheckCircle} variant={filter === 'Read' ? 'primary' : 'secondary'} onClick={() => setFilter('Read')}>Read</Button></div>
        <div className="notification-list">
          {notificationResource.loading && <div className="empty-panel">Loading notifications from backend...</div>}
          {notifications.map(item => <div className="notification-manage-row" key={item.id}><NotificationItem item={{ ...item, unread: !item.read }} /><div className="notification-actions"><Badge>{item.status}</Badge><IconButton icon={CheckCircle} label="Mark as read" onClick={() => markOne(item)} /><IconButton icon={Trash2} label="Delete notification" onClick={() => removeOne(item)} /><IconButton icon={MoreVertical} label="More notification actions" onClick={() => notify('Notification actions opened')} /></div></div>)}
          {!notificationResource.loading && !notifications.length && <div className="empty-panel">No notifications match the current filters.</div>}
        </div>
      </Card>
    </section>
  );
}
