import { CheckCheck, RefreshCw } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import toast from 'react-hot-toast'

import Badge from '../components/Badge.jsx'
import Button from '../components/Button.jsx'
import EmptyState from '../components/EmptyState.jsx'
import Spinner from '../components/Spinner.jsx'
import {
  getNotifications,
  markAllNotificationsAsRead,
  markNotificationAsRead,
} from '../services/notifications.service.js'
import { formatDateTime } from '../utils/format.js'
import { notificationStatusLabel } from '../utils/enums.js'

function statusClasses(status) {
  switch (status) {
    case 2:
      return 'bg-rose-50 text-rose-700 ring-rose-200'
    case 1:
      return 'bg-emerald-50 text-emerald-700 ring-emerald-200'
    case 0:
    default:
      return 'bg-slate-50 text-slate-700 ring-slate-200'
  }
}

export default function NotificationsPage() {
  const [items, setItems] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  async function load() {
    setLoading(true)
    setError(null)
    try {
      const data = await getNotifications({ take: 50 })
      setItems(Array.isArray(data) ? data : [])
    } catch (err) {
      const message = err?.response?.data?.detail || err?.message
      setError(message || 'Failed to load notifications.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    load()
  }, [])

  const unreadCount = useMemo(() => items.reduce((acc, x) => acc + (x.isRead ? 0 : 1), 0), [items])

  return (
    <div className="space-y-6">
      <div className="surface-solid p-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <div className="text-sm font-semibold text-cyan-800">Notifications</div>
            <div className="mt-1 text-2xl font-bold tracking-tight text-slate-900">Event feed</div>
            <div className="mt-1 text-sm text-slate-600">
              {unreadCount} unread - pulled from NotificationService through the API Gateway.
            </div>
          </div>

          <div className="flex flex-col gap-2 sm:flex-row">
            <Button variant="secondary" onClick={load} disabled={loading}>
              <RefreshCw className="h-4 w-4" aria-hidden="true" />
              Refresh
            </Button>
            <Button
              variant="secondary"
              onClick={async () => {
                try {
                  const result = await markAllNotificationsAsRead()
                  const updated = Number(result?.updated ?? 0)
                  if (updated > 0) {
                    setItems((current) =>
                      current.map((n) => (n.isRead ? n : { ...n, isRead: true, readAt: new Date().toISOString() })),
                    )
                  }
                  toast.success('Marked all as read.')
                } catch {
                  toast.error('Failed to mark notifications as read.')
                }
              }}
              disabled={items.length === 0}
            >
              <CheckCheck className="h-4 w-4" aria-hidden="true" />
              Mark all read
            </Button>
          </div>
        </div>
      </div>

      {loading ? (
        <div className="surface-solid p-8">
          <Spinner label="Loading notifications..." />
        </div>
      ) : error ? (
        <div className="surface-solid p-6">
          <div className="rounded-xl border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-800">
            {error}
          </div>
        </div>
      ) : items.length === 0 ? (
        <EmptyState title="No notifications" description="Create a user or a reclamation to generate events." />
      ) : (
        <div className="space-y-3">
          {items.map((n) => (
            <div key={n.id} className="surface-solid p-5">
              <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                <div className="min-w-0">
                  <div className="flex flex-wrap items-center gap-2">
                    <div
                      className={
                        n.isRead
                          ? 'h-2.5 w-2.5 rounded-full bg-slate-300'
                          : 'h-2.5 w-2.5 rounded-full bg-cyan-600'
                      }
                      aria-hidden="true"
                    />
                    <div className="text-sm font-semibold text-slate-900">{n.title || n.type}</div>
                    <Badge className={statusClasses(n.status)}>{notificationStatusLabel(n.status)}</Badge>
                    {n.type ? <Badge className="bg-slate-50 text-slate-700 ring-slate-200">{n.type}</Badge> : null}
                  </div>

                  <div className="mt-2 whitespace-pre-wrap text-sm text-slate-700">{n.message}</div>

                  <div className="mt-3 flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-slate-600">
                    <span>
                      <span className="font-semibold text-slate-800">Created:</span> {formatDateTime(n.createdAt)}
                    </span>
                    <span>
                      <span className="font-semibold text-slate-800">Sent:</span> {formatDateTime(n.sentAt)}
                    </span>
                    <span>
                      <span className="font-semibold text-slate-800">Read:</span> {formatDateTime(n.readAt)}
                    </span>
                    {n.recipientEmail ? (
                      <span>
                        <span className="font-semibold text-slate-800">To:</span> {n.recipientEmail}
                      </span>
                    ) : null}
                    {n.sourceEvent ? (
                      <span>
                        <span className="font-semibold text-slate-800">Event:</span> {n.sourceEvent}
                      </span>
                    ) : null}
                  </div>
                </div>

                <div className="shrink-0">
                  <Button
                    variant="ghost"
                    disabled={n.isRead}
                    onClick={async () => {
                      try {
                        await markNotificationAsRead(n.id)
                        setItems((current) =>
                          current.map((it) => (it.id === n.id ? { ...it, isRead: true, readAt: new Date().toISOString() } : it)),
                        )
                        toast.success('Marked as read.')
                      } catch {
                        toast.error('Failed to mark as read.')
                      }
                    }}
                  >
                    Mark read
                  </Button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
