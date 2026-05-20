import { AlertTriangle, ArrowRight, BarChart3, CalendarDays, ClipboardList, FileText, Loader2, Wrench } from 'lucide-react';
import { useMemo, useState } from 'react';
import {
  CartesianGrid,
  Cell,
  Legend,
  Line,
  LineChart,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis
} from 'recharts';
import { ApiErrorState } from '../components/common/ApiErrorState';
import { Avatar, Badge, Button, Card, DataTable, NotificationItem, StatCard } from '../components/ui';
import { useDashboard } from '../hooks/useDashboard';
import { useNotifications } from '../hooks/useNotifications';
import { usePlanning } from '../hooks/usePlanning';
import { useReclamations } from '../hooks/useReclamations';
import { useInterventions } from '../hooks/useInterventions';
import { useVisitReports } from '../hooks/useVisitReports';
import { canAccessDashboardSummary, canAccessPlanningRequests, isTechnician } from '../utils/roleAccess';

const statIcons = { FileText, CalendarDays, Wrench, AlertTriangle };
const statusOrder = [
  { label: 'Open', color: '#1167ff', includes: ['Open', 'Assigned'] },
  { label: 'In Progress', color: '#22a6b8', includes: ['In Progress'] },
  { label: 'Planned', color: '#facc15', includes: ['Planned'] },
  { label: 'Resolved', color: '#55d68a', includes: ['Resolved'] },
  { label: 'Closed', color: '#d1d5db', includes: ['Closed'] },
  { label: 'Cancelled', color: '#ef4444', includes: ['Cancelled', 'Rejected'] }
];
const chartRanges = { week: 7, month: 4, year: 12 };
const chartRangeLabels = { week: 'This Week', month: 'This Month', year: 'This Year' };
const RADIAN = Math.PI / 180;

function formatPercent(value) {
  return `${Number(value || 0).toFixed(1)}%`;
}

function withStatusPercentages(rows = []) {
  const total = rows.reduce((sum, item) => sum + Number(item.value || 0), 0);
  return rows.map(item => ({
    ...item,
    name: item.label,
    percentValue: total ? Number(item.value || 0) * 100 / total : 0
  }));
}

function renderStatusPieLabel({ cx, cy, midAngle, outerRadius, value, percentValue }) {
  if (!value || percentValue < 5) return null;
  const radius = outerRadius + 18;
  const x = cx + radius * Math.cos(-midAngle * RADIAN);
  const y = cy + radius * Math.sin(-midAngle * RADIAN);

  return (
    <text x={x} y={y} fill="#334155" textAnchor={x > cx ? 'start' : 'end'} dominantBaseline="central" fontSize={11} fontWeight={700}>
      {value} ({formatPercent(percentValue)})
    </text>
  );
}

function formatStatusTooltip(value, name, props) {
  return [`${value} (${formatPercent(props?.payload?.percentValue)})`, name];
}

function formatStatusLegend(value, entry) {
  const payload = entry?.payload || {};
  return `${value}: ${payload.value || 0} (${formatPercent(payload.percentValue)})`;
}

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
  const notifications = notificationsResource.notifications.slice(0, 3).map(item => ({
    ...item,
    message: item.message || `${item.status || 'Sent'} update${item.recipientRole ? ` for ${item.recipientRole}` : ''}`
  }));

  const openReclamations = dashboard.summary?.openReclamations ?? reclamations.filter(item => ['Open', 'Assigned', 'Planned', 'In Progress'].includes(item.status)).length;
  const plannedVisits = dashboard.summary?.plannedVisits ?? appointments.filter(item => ['Proposed', 'Confirmed', 'Rescheduled'].includes(item.status)).length;
  const activeInterventions = dashboard.summary?.activeInterventions ?? interventions.filter(item => item.status !== 'Completed').length;
  const slaRisk = dashboard.summary?.slaRisk ?? reclamations.filter(item => ['High', 'Urgent'].includes(item.priority) && !['Resolved', 'Closed'].includes(item.status)).length;

  const dynamicStats = [
    { label: 'Open Reclamations', value: openReclamations, tone: 'blue', icon: 'FileText', values: buildSpark(openReclamations, [6, 8, 7, 10, 12, 11]) },
    { label: 'Planned Visits', value: plannedVisits, tone: 'green', icon: 'CalendarDays', values: buildSpark(plannedVisits, [2, 3, 4, 4, 5, 6]) },
    { label: 'Active Interventions', value: activeInterventions, tone: 'purple', icon: 'Wrench', values: buildSpark(activeInterventions, [1, 2, 3, 3, 4, 5]) },
    { label: 'SLA Risk', value: slaRisk, tone: 'orange', icon: 'AlertTriangle', values: buildSpark(slaRisk, [4, 4, 5, 6, 5, 7]) }
  ].map(stat => ({ ...stat, ...trendFromSpark(stat.values) }));

  const chartData = useMemo(() => {
    const count = chartRanges[chartRange];
    const now = new Date();
    return Array.from({ length: count }, (_, index) => {
      const age = count - index - 1;
      const date = new Date(now);
      if (chartRange === 'year') date.setMonth(index, 1);
      if (chartRange === 'week') date.setDate(now.getDate() - age);
      const label = chartRange === 'year'
        ? date.toLocaleDateString('en-US', { month: 'short' })
        : chartRange === 'month'
          ? `W${index + 1}`
          : date.toLocaleDateString('en-US', { weekday: 'short' });
      return {
        label,
        reclamations: Math.max(0, Math.round((reclamations.length * (0.58 + index / (count * 1.8))) + index)),
        interventions: Math.max(0, Math.round((interventions.length * (0.5 + index / (count * 2.2))) + Math.floor(index / 2)))
      };
    });
  }, [chartRange, reclamations.length, interventions.length]);

  const chartMax = Math.max(...chartData.flatMap(item => [item.reclamations, item.interventions]), 1);
  const chartDomainMax = Math.max(10, Math.ceil(chartMax / 10) * 10);

  const breakdown = withStatusPercentages(statusOrder.map(item => ({
    ...item,
    value: reclamations.filter(row => item.includes.includes(row.status)).length
  })));
  const totalReclamations = breakdown.reduce((sum, item) => sum + item.value, 0);
  const upcomingAppointments = appointments.slice(0, 3).map(item => ({ time: item.start, client: item.client, technician: item.technicianName, ref: item.reclamationId, type: item.product, status: item.status }));
  const loadError = dashboard.error || reclamationResource.error || planning.error || interventionResource.error || notificationsResource.error;
  const loadErrorStatus = dashboard.errorStatus || reclamationResource.errorStatus || planning.errorStatus || interventionResource.errorStatus || notificationsResource.errorStatus;

  return (
    <section className="page-shell dashboard-page">
      <div className="page-title-row dashboard-title-row">
        <div>
          <span className="eyebrow">Live operations</span>
          <h1>{user?.role || 'SAV'} command center</h1>
          <p>Track reclamations, planned work, interventions, and SLA pressure from one clean overview.</p>
        </div>
        <div className="ops-hero-panel" aria-label="Live command center summary">
          <span><strong>{openReclamations}</strong><small>Open</small></span>
          <span><strong>{plannedVisits}</strong><small>Planned</small></span>
          <span><strong>{slaRisk}</strong><small>SLA risk</small></span>
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
        {dynamicStats.map(stat => <StatCard key={stat.label} label={stat.label} value={stat.value} trend={stat.trend} trendTone={stat.trendTone} tone={stat.tone} spark={stat.values} icon={statIcons[stat.icon]} />)}
      </div>

      <div className="dashboard-main-grid">
        <Card title="Activity Overview" icon={BarChart3} className="chart-card activity-overview-card" actions={<label className="chart-period-select"><select aria-label="Activity range" value={chartRange} onChange={event => { setChartRange(event.target.value); notify(`Activity range set to ${event.target.value}`); }}>{Object.entries(chartRangeLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>}>
          <div className="activity-chart-shell">
            <ResponsiveContainer width="100%" height={276}>
              <LineChart data={chartData} margin={{ top: 12, right: 22, left: 2, bottom: 8 }}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} />
                <XAxis dataKey="label" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
                <YAxis allowDecimals={false} domain={[0, chartDomainMax]} tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
                <Tooltip formatter={(value, name) => [value, name === 'reclamations' ? 'Reclamations' : 'Interventions']} />
                <Legend iconType="circle" wrapperStyle={{ fontSize: 12, fontWeight: 700 }} />
                <Line type="monotone" dataKey="reclamations" name="Reclamations" stroke="#1167ff" strokeWidth={3} dot={{ r: 3 }} activeDot={{ r: 5 }} />
                <Line type="monotone" dataKey="interventions" name="Interventions" stroke="#11b7a5" strokeWidth={3} dot={{ r: 3 }} activeDot={{ r: 5 }} />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </Card>

        <Card title="Reclamation Status" icon={Wrench} className="donut-card">
          <div className="dashboard-status-chart">
            <ResponsiveContainer width="100%" height={276}>
              <PieChart>
                <Pie
                  data={breakdown}
                  dataKey="value"
                  nameKey="name"
                  innerRadius={48}
                  outerRadius={78}
                  paddingAngle={2}
                  label={renderStatusPieLabel}
                  labelLine={false}
                  isAnimationActive
                  animationBegin={120}
                  animationDuration={900}
                  animationEasing="ease-out"
                >
                  {breakdown.map(item => <Cell key={item.label} fill={item.color} />)}
                </Pie>
                <Tooltip formatter={formatStatusTooltip} />
                <Legend formatter={formatStatusLegend} iconType="square" wrapperStyle={{ fontSize: 12, fontWeight: 700 }} />
                <text x="50%" y="43%" textAnchor="middle" dominantBaseline="central" className="dashboard-status-total">{totalReclamations}</text>
                <text x="50%" y="53%" textAnchor="middle" dominantBaseline="central" className="dashboard-status-caption">Total</text>
              </PieChart>
            </ResponsiveContainer>
          </div>
        </Card>

        <Card title="Upcoming Appointments" icon={CalendarDays} className="appointments-card" actions={<button type="button" className="link-button" onClick={() => navigate('planning')}>View all</button>}>
          <div className="appointment-list">{upcomingAppointments.length ? upcomingAppointments.map(item => <div className="appointment-row" key={`${item.time}-${item.client}`}><div className="time-box"><strong>{item.time}</strong><small /></div><div className="appointment-meta"><strong>{item.client}</strong><span>{item.technician}</span></div><div className="appointment-status"><Badge>{item.status}</Badge></div></div>) : <EmptyMiniState text="No upcoming appointment." />}</div>
        </Card>

        <Card title="Recent Reclamations" icon={FileText} className="recent-table-card" actions={<button type="button" className="link-button" onClick={() => navigate('reclamations')}>View all</button>}>
          <DataTable rows={reclamations.slice(0, 5)} columns={[{ key: 'id', label: 'Reference', render: row => <button type="button" className="table-link" onClick={() => navigate('reclamations')}>{row.id}</button> }, { key: 'client', label: 'Client' }, { key: 'productModel', label: 'Product / Model' }, { key: 'status', label: 'Status', render: row => <Badge>{row.status}</Badge> }, { key: 'priority', label: 'Priority', render: row => <Badge>{row.priority}</Badge> }, { key: 'assigned', label: 'Assigned Technician', render: row => <span className="avatar-cell"><Avatar name={row.assigned || 'Unassigned'} initials={row.assignedAvatar || '--'} size="sm" />{row.assigned || '-'}</span> }, { key: 'createdShort', label: 'Created At' }]} />
          <div className="table-footer"><span>Showing 1 to {Math.min(5, reclamations.length)} of {reclamations.length} results</span></div>
        </Card>

        <Card title="Latest notifications" icon={AlertTriangle} className="notifications-card" actions={<button type="button" className="link-button" onClick={() => navigate('notifications')}>View all <ArrowRight size={15} /></button>}>
          <div className="notification-list">
            {notifications.length ? notifications.map(item => <NotificationItem key={item.id} item={{ ...item, unread: !item.read }} compact />) : <EmptyMiniState text="No recent notification." />}
          </div>
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

function EmptyMiniState({ icon: Icon = ClipboardList, text }) {
  return <div className="empty-panel compact"><Icon size={22} /><p>{text}</p></div>;
}

function buildSpark(currentValue, baseline) {
  const safeCurrent = Math.max(0, Number(currentValue) || 0);
  const previous = baseline[baseline.length - 1] || safeCurrent || 1;
  return [...baseline.slice(0, -1), Math.max(1, previous), Math.max(0, safeCurrent)];
}

function trendFromSpark(values) {
  const current = values[values.length - 1] || 0;
  const previous = values[values.length - 2] || 1;
  const delta = current - previous;
  const percent = Math.round((Math.abs(delta) / Math.max(previous, 1)) * 100);
  return {
    trend: `${percent}% ${delta >= 0 ? 'higher' : 'lower'} than last week`,
    trendTone: delta >= 0 ? 'up' : 'down'
  };
}

function formatDate(value) {
  if (!value) return '-';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return String(value);
  return new Intl.DateTimeFormat('fr-FR', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' }).format(date);
}
