import { AlertTriangle, BarChart3, CalendarDays, ClipboardList, FileText, Loader2, RefreshCw, Wrench } from 'lucide-react';
import { useMemo, useState } from 'react';
import { ApiErrorState } from '../components/common/ApiErrorState';
import { Avatar, Badge, Button, Card, DataTable, StatCard } from '../components/ui';
import { useDashboard } from '../hooks/useDashboard';
import { useNotifications } from '../hooks/useNotifications';
import { usePlanning } from '../hooks/usePlanning';
import { useReclamations } from '../hooks/useReclamations';
import { useInterventions } from '../hooks/useInterventions';
import { useVisitReports } from '../hooks/useVisitReports';
import { canAccessDashboardSummary, canAccessPlanningRequests, isTechnician } from '../utils/roleAccess';

const statIcons = { FileText, CalendarDays, Wrench, AlertTriangle };
const statusOrder = ['Open', 'Assigned', 'In Progress', 'Planned', 'Resolved', 'Closed', 'Cancelled'];
const chartRanges = { week: 7, month: 4, year: 12 };

export function DashboardPage({ user, navigate, notify }) {
  const technicianMode = isTechnician(user);
  const canLoadAdminSummary = canAccessDashboardSummary(user);
  const canLoadPlanningRequests = canAccessPlanningRequests(user);

  const [chartRange, setChartRange] = useState('year');
  const dashboard = useDashboard(canLoadAdminSummary);
  const reclamationResource = useReclamations({}, !technicianMode);
  const planning = usePlanning({ enabled: !technicianMode && canLoadPlanningRequests, includeRequests: canLoadPlanningRequests });
  const interventionResource = useInterventions(user);
  const reportResource = useVisitReports({ enabled: technicianMode });
  const notificationsResource = useNotifications(10);

  if (technicianMode) {
    return (
      <TechnicianDashboard
        user={user}
        navigate={navigate}
        interventions={interventionResource.interventions}
        interventionResource={interventionResource}
        reports={reportResource.reports}
        reportResource={reportResource}
        notifications={notificationsResource.notifications.slice(0, 4)}
        notificationsResource={notificationsResource}
      />
    );
  }

  const reclamations = reclamationResource.reclamations;
  const appointments = planning.appointments;
  const interventions = interventionResource.interventions;
  const notifications = notificationsResource.notifications.slice(0, 3);

  const dynamicStats = [
    { label: 'Open Reclamations', value: dashboard.summary?.openReclamations ?? reclamations.filter(item => ['Open', 'Assigned', 'Planned', 'In Progress'].includes(item.status)).length, trend: 'Backend source', tone: 'blue', icon: 'FileText', values: [4, 8, 6, 10, 9] },
    { label: 'Planned Visits', value: dashboard.summary?.plannedVisits ?? appointments.filter(item => ['Proposed', 'Confirmed', 'Rescheduled'].includes(item.status)).length, trend: 'Gateway API', tone: 'teal', icon: 'CalendarDays', values: [3, 5, 4, 7, 6] },
    { label: 'Active Interventions', value: dashboard.summary?.activeInterventions ?? interventions.filter(item => item.status !== 'Completed').length, trend: 'Live backend', tone: 'purple', icon: 'Wrench', values: [2, 4, 5, 5, 7] },
    { label: 'SLA Risk', value: dashboard.summary?.slaRisk ?? reclamations.filter(item => ['High', 'Urgent'].includes(item.priority) && !['Resolved', 'Closed'].includes(item.status)).length, trend: 'Priority/SLA', tone: 'orange', icon: 'AlertTriangle', values: [1, 2, 2, 3, 4] }
  ];

  const chartData = useMemo(() => {
    const count = chartRanges[chartRange];
    return Array.from({ length: count }, (_, index) => ({
      label: chartRange === 'year' ? `M${index + 1}` : chartRange === 'month' ? `W${index + 1}` : ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'][index],
      reclamations: Math.max(0, reclamations.length - index),
      interventions: Math.max(0, interventions.length - Math.floor(index / 2))
    })).reverse();
  }, [chartRange, reclamations.length, interventions.length]);

  const chartMax = Math.max(...chartData.flatMap(item => [item.reclamations, item.interventions]), 1);
  const point = (item, index, key) => `${(index / Math.max(chartData.length - 1, 1)) * 100},${100 - (item[key] / chartMax) * 100}`;
  const recPoints = chartData.map((item, index) => point(item, index, 'reclamations')).join(' ');
  const intPoints = chartData.map((item, index) => point(item, index, 'interventions')).join(' ');

  const breakdown = statusOrder.map(label => ({ label, value: reclamations.filter(row => row.status === label).length }));
  const totalReclamations = Math.max(1, reclamations.length);
  const visibleBreakdown = breakdown.filter(item => item.value > 0);
  const upcomingAppointments = appointments.slice(0, 3).map(item => ({ time: item.start, client: item.client, technician: item.technicianName, ref: item.reclamationId, type: item.product, status: item.status }));
  const loadError = dashboard.error || reclamationResource.error || planning.error || interventionResource.error || notificationsResource.error;
  const loadErrorStatus = dashboard.errorStatus || reclamationResource.errorStatus || planning.errorStatus || interventionResource.errorStatus || notificationsResource.errorStatus;

  return (
    <section className="page-shell dashboard-page">
      <div className="page-title-row">
        <div>
          <h1>Welcome back, {user?.firstName || user?.name || 'SAV user'} 👋</h1>
          <p>Here's what's happening with your after-sales operations today.</p>
        </div>
      </div>

      {loadError ? (
        <Card>
          <ApiErrorState status={loadErrorStatus} message={loadError} onRetry={() => {
            dashboard.reload();
            reclamationResource.reload();
            planning.reload();
            interventionResource.reload();
            notificationsResource.reload();
          }} />
        </Card>
      ) : null}

      <div className="kpi-grid">
        {dynamicStats.map(stat => <StatCard key={stat.label} label={stat.label} value={stat.value} trend={stat.trend} tone={stat.tone} spark={stat.values} icon={statIcons[stat.icon]} />)}
      </div>

      <div className="dashboard-main-grid">
        <Card title="Activity Overview" icon={BarChart3} className="chart-card" actions={<div className="range-switch" aria-label="Activity range">{['week', 'month', 'year'].map(range => <button type="button" key={range} className={chartRange === range ? 'active' : ''} onClick={() => { setChartRange(range); notify(`Activity range set to ${range}`); }}>{range[0].toUpperCase() + range.slice(1)}</button>)}</div>}>
          <div className="chart-legend"><span><i className="legend-dot blue" />Reclamations</span><span><i className="legend-dot teal" />Interventions</span></div>
          <div className="line-chart"><div className="chart-axis">{[chartMax, Math.round(chartMax * 0.75), Math.round(chartMax * 0.5), Math.round(chartMax * 0.25), 0].map(value => <span key={value}>{value}</span>)}</div><svg viewBox="-2 -4 104 110" preserveAspectRatio="none">{[0, 25, 50, 75, 100].map(value => <line key={value} x1="0" x2="100" y1={value} y2={value} />)}<polyline points={recPoints} className="line-blue" /><polyline points={intPoints} className="line-teal" /></svg><div className="month-axis" style={{ gridTemplateColumns: `repeat(${chartData.length}, 1fr)` }}>{chartData.map(item => <span key={item.label}>{item.label}</span>)}</div></div>
        </Card>

        <Card title="Reclamation Status" icon={Wrench} className="donut-card">
          <div className="donut-layout"><div className="donut-chart"><div><strong>{reclamations.length}</strong><span>Total</span></div></div><div className="donut-legend">{(visibleBreakdown.length ? visibleBreakdown : breakdown).map(item => <div className="donut-legend-item" key={item.label}><span><i />{item.label}</span><strong>{item.value}</strong><small>{((item.value / totalReclamations) * 100).toFixed(1)}%</small></div>)}</div></div>
        </Card>

        <Card title="Upcoming Appointments" icon={CalendarDays} className="appointments-card" actions={<button type="button" className="link-button" onClick={() => navigate('planning')}>View all</button>}>
          <div className="appointment-list">{upcomingAppointments.length ? upcomingAppointments.map(item => <div className="appointment-row" key={`${item.time}-${item.client}`}><div className="time-box"><strong>{item.time}</strong><small /></div><div className="appointment-meta"><strong>{item.client}</strong><span>{item.technician}</span></div><div className="appointment-status"><Badge>{item.status}</Badge></div></div>) : <EmptyMiniState text="No upcoming appointment." />}</div>
        </Card>

        <Card title="Recent Reclamations" icon={FileText} className="recent-table-card" actions={<button type="button" className="link-button" onClick={() => navigate('reclamations')}>View all</button>}>
          <DataTable rows={reclamations.slice(0, 5)} columns={[{ key: 'id', label: 'Reference', render: row => <button type="button" className="table-link" onClick={() => navigate('reclamations')}>{row.id}</button> }, { key: 'client', label: 'Client' }, { key: 'productModel', label: 'Product / Model' }, { key: 'status', label: 'Status', render: row => <Badge>{row.status}</Badge> }, { key: 'priority', label: 'Priority', render: row => <Badge>{row.priority}</Badge> }, { key: 'assigned', label: 'Assigned Technician', render: row => <span className="avatar-cell"><Avatar name={row.assigned || 'Unassigned'} initials={row.assignedAvatar || '--'} size="sm" />{row.assigned || '-'}</span> }, { key: 'createdShort', label: 'Created At' }]} />
          <div className="table-footer"><span>Showing 1 to {Math.min(5, reclamations.length)} of {reclamations.length} results</span></div>
        </Card>

        <Card title="Notifications" icon={AlertTriangle} className="notifications-card">
          {notifications.length ? notifications.map(item => <NotificationItem key={item.id} item={{ ...item, unread: !item.read }} compact />) : <EmptyMiniState text="No recent notification." />}
        </Card>
      </div>
    </section>
  );
}

function TechnicianDashboard({ user, navigate, interventions, interventionResource, reports, reportResource, notifications, notificationsResource }) {
  const today = new Date().toISOString().slice(0, 10);
  const todaysInterventions = interventions.filter(item => String(item.date || item.plannedStartAt || item.start || '').startsWith(today));
  const pendingInterventions = interventions.filter(item => !['Completed', 'Closed', 'Resolved'].includes(item.status));
  const reportsToComplete = reports.filter(item => item.status !== 'Published');
  const loading = interventionResource.loading || reportResource.loading || notificationsResource.loading;
  const loadError = interventionResource.error || reportResource.error || notificationsResource.error;
  const loadErrorStatus = interventionResource.errorStatus || reportResource.errorStatus || notificationsResource.errorStatus;

  return (
    <section className="page-shell dashboard-page technician-dashboard-page">
      <div className="page-title-row">
        <div>
          <h1>Bonjour, {user?.firstName || user?.name || 'technicien'} 👋</h1>
          <p>Espace technicien : interventions, rapports et notifications sans appels Admin/SAV.</p>
        </div>
      </div>

      {loadError ? (
        <Card>
          <ApiErrorState status={loadErrorStatus} message={loadError} onRetry={() => {
            interventionResource.reload();
            reportResource.reload();
            notificationsResource.reload();
          }} />
        </Card>
      ) : null}

      {loading ? <Card><div className="empty-state"><Loader2 size={18} className="spin" /> Chargement de votre espace technicien...</div></Card> : null}

      <div className="kpi-grid">
        <StatCard label="Mes interventions aujourd’hui" value={todaysInterventions.length} trend="Planning personnel" tone="blue" icon={CalendarDays} spark={[1, 1, 2, todaysInterventions.length || 1]} />
        <StatCard label="Interventions en attente" value={pendingInterventions.length} trend="À traiter" tone="orange" icon={Wrench} spark={[2, 3, 2, pendingInterventions.length || 1]} />
        <StatCard label="Rapports à compléter" value={reportsToComplete.length} trend="Avant publication" tone="purple" icon={FileText} spark={[1, 2, 3, reportsToComplete.length || 1]} />
        <StatCard label="Notifications récentes" value={notifications.length} trend="Alertes workflow" tone="teal" icon={AlertTriangle} spark={[1, 1, 2, notifications.length || 1]} />
      </div>

      <div className="two-card-row">
        <Card title="Mes interventions" icon={Wrench} actions={<Button icon={Wrench} onClick={() => navigate('interventions')}>Ouvrir</Button>}>
          {interventions.length ? (
            <DataTable rows={interventions.slice(0, 5)} columns={[{ key: 'id', label: 'Intervention' }, { key: 'client', label: 'Client' }, { key: 'product', label: 'Produit' }, { key: 'status', label: 'Statut', render: row => <Badge>{row.status}</Badge> }, { key: 'date', label: 'Date', render: row => formatDate(row.date || row.plannedStartAt || row.start) }]} />
          ) : <EmptyMiniState icon={ClipboardList} text="Aucune intervention assignée pour le moment." />}
        </Card>

        <Card title="Notifications récentes" icon={AlertTriangle} actions={<Button icon={AlertTriangle} onClick={() => navigate('notifications')}>Voir tout</Button>}>
          {notifications.length ? notifications.map(item => <NotificationItem key={item.id} item={{ ...item, unread: !item.read }} />) : <EmptyMiniState text="Aucune notification récente." />}
        </Card>
      </div>

      <Card title="Rapports à compléter" icon={FileText} actions={<Button icon={FileText} onClick={() => navigate('visit-reports')}>Rapports</Button>}>
        {reportsToComplete.length ? (
          <DataTable rows={reportsToComplete.slice(0, 5)} columns={[{ key: 'id', label: 'Rapport' }, { key: 'reclamationId', label: 'Réclamation' }, { key: 'client', label: 'Client' }, { key: 'status', label: 'Statut', render: row => <Badge>{row.status}</Badge> }, { key: 'createdAt', label: 'Créé', render: row => formatDate(row.createdAt) }]} />
        ) : <EmptyMiniState icon={FileText} text="Aucun rapport à compléter." />}
      </Card>
    </section>
  );
}

function NotificationItem({ item }) {
  return <div className="notification-item compact"><div><strong>{item.title}</strong><p>{item.message}</p></div><time>{item.time}</time>{!item.read && <span className="unread-dot" />}</div>;
}

function EmptyMiniState({ icon: Icon = ClipboardList, text }) {
  return <div className="empty-panel compact"><Icon size={22} /><p>{text}</p></div>;
}

function formatDate(value) {
  if (!value) return '-';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return String(value);
  return new Intl.DateTimeFormat('fr-FR', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' }).format(date);
}
