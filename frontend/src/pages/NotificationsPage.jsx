import { Bell, CheckCheck, MailWarning, RefreshCw } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import toast from 'react-hot-toast'

import Badge from '../components/Badge.jsx'
import Button from '../components/Button.jsx'
import EmptyState from '../components/EmptyState.jsx'
import MetricCard from '../components/MetricCard.jsx'
import PageHeader from '../components/PageHeader.jsx'
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
      setError(message || 'Impossible de charger les notifications.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    load()
  }, [])

  const unreadCount = useMemo(() => items.reduce((acc, x) => acc + (x.isRead ? 0 : 1), 0), [items])
  const failedCount = useMemo(() => items.reduce((acc, x) => acc + (x.status === 2 ? 1 : 0), 0), [items])

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Centre de notifications"
        title="Flux d'evenements et alertes"
        description="Le fil de notifications devient plus lisible avec une meilleure hierarchie des cartes, des badges plus explicites et des actions de lecture plus visibles."
        meta={
          <>
            <span>{items.length} notifications chargees</span>
            <span className="text-slate-300">|</span>
            <span>{unreadCount} non lues</span>
          </>
        }
        actions={
          <>
            <Button variant="secondary" onClick={load} disabled={loading}>
              <RefreshCw className="h-4 w-4" aria-hidden="true" />
              Actualiser
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
                  toast.success('Toutes les notifications sont marquees comme lues.')
                } catch {
                  toast.error('Impossible de marquer les notifications comme lues.')
                }
              }}
              disabled={items.length === 0}
            >
              <CheckCheck className="h-4 w-4" aria-hidden="true" />
              Tout marquer lu
            </Button>
          </>
        }
      />

      <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
        <MetricCard icon={Bell} label="Total" value={items.length} helper="Dernieres notifications recues" tone="cyan" />
        <MetricCard icon={CheckCheck} label="Non lues" value={unreadCount} helper="A traiter ou consulter" tone={unreadCount ? 'amber' : 'emerald'} />
        <MetricCard icon={MailWarning} label="Echecs" value={failedCount} helper="Notifications non envoyees" tone={failedCount ? 'rose' : 'slate'} />
      </div>

      {loading ? (
        <div className="surface-solid p-8">
          <Spinner label="Chargement des notifications..." />
        </div>
      ) : error ? (
        <div className="surface-solid p-6">
          <div className="notice-error">{error}</div>
        </div>
      ) : items.length === 0 ? (
        <EmptyState
          icon={Bell}
          title="Aucune notification"
          description="Creez un utilisateur ou une reclamation pour generer des evenements."
        />
      ) : (
        <div className="space-y-4">
          {items.map((n) => (
            <article key={n.id} className="surface-solid fade-in-up overflow-hidden p-5">
              <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
                <div className="min-w-0">
                  <div className="flex flex-wrap items-center gap-2">
                    <div
                      className={n.isRead ? 'h-2.5 w-2.5 rounded-full bg-slate-300' : 'h-2.5 w-2.5 rounded-full bg-cyan-600'}
                      aria-hidden="true"
                    />
                    <div className="font-display text-xl font-bold text-slate-950">{n.title || n.type}</div>
                    <Badge className={statusClasses(n.status)}>{notificationStatusLabel(n.status)}</Badge>
                    {!n.isRead ? <Badge className="bg-cyan-50 text-cyan-800 ring-cyan-200">NON LUE</Badge> : null}
                    {n.type ? <Badge className="bg-slate-50 text-slate-700 ring-slate-200">{n.type}</Badge> : null}
                  </div>

                  <div className="mt-4 whitespace-pre-wrap text-sm leading-7 text-slate-700">{n.message}</div>

                  <div className="mt-4 grid grid-cols-1 gap-3 text-xs text-slate-600 sm:grid-cols-2 xl:grid-cols-4">
                    <MetaItem label="Creation" value={formatDateTime(n.createdAt)} />
                    <MetaItem label="Envoi" value={formatDateTime(n.sentAt)} />
                    <MetaItem label="Lecture" value={formatDateTime(n.readAt)} />
                    <MetaItem label="Destinataire" value={n.recipientEmail || '-'} />
                  </div>

                  {n.sourceEvent ? (
                    <div className="mt-4 rounded-2xl border border-slate-200/80 bg-slate-50/80 px-4 py-3 text-xs text-slate-600">
                      <span className="font-semibold text-slate-900">Evenement source:</span> {n.sourceEvent}
                    </div>
                  ) : null}
                </div>

                <div className="shrink-0">
                  <Button
                    variant={n.isRead ? 'ghost' : 'secondary'}
                    disabled={n.isRead}
                    onClick={async () => {
                      try {
                        await markNotificationAsRead(n.id)
                        setItems((current) =>
                          current.map((it) => (it.id === n.id ? { ...it, isRead: true, readAt: new Date().toISOString() } : it)),
                        )
                        toast.success('Notification marquee comme lue.')
                      } catch {
                        toast.error('Impossible de marquer comme lue.')
                      }
                    }}
                  >
                    Marquer lu
                  </Button>
                </div>
              </div>
            </article>
          ))}
        </div>
      )}
    </div>
  )
}

function MetaItem({ label, value }) {
  return (
    <div className="rounded-2xl border border-slate-200/80 bg-white/80 px-3 py-3">
      <div className="text-[11px] font-extrabold uppercase tracking-[0.18em] text-slate-500">{label}</div>
      <div className="mt-1 text-sm font-semibold text-slate-900">{value || '-'}</div>
    </div>
  )
}
