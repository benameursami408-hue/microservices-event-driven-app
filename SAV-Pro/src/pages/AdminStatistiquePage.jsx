import {
  Activity,
  BarChart3,
  CheckCircle,
  ClipboardList,
  PieChart as PieChartIcon,
  RefreshCw,
  TrendingUp,
  UserRound,
  Users,
  Wrench
} from 'lucide-react';
import {
  Bar,
  BarChart,
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
import { Badge, Button, Card, DataTable } from '../components/ui';
import { useAdminStatistics } from '../hooks/useAdminStatistics';
import { isAdmin } from '../utils/roleAccess';

const chartColors = ['#1167ff', '#11b7a5', '#7c3aed', '#f97316', '#ef4444', '#64748b', '#0f8a60', '#c27200'];
const RADIAN = Math.PI / 180;

function formatRate(value) {
  return `${Number(value || 0).toFixed(1)}%`;
}

function formatPercent(value) {
  return `${Number(value || 0).toFixed(1)}%`;
}

function hasData(rows = []) {
  return rows.some(item => Number(item.value || item.handledCount || item.completedCount || 0) > 0);
}

function withPercentages(rows = []) {
  const total = rows.reduce((sum, item) => sum + Number(item.value || 0), 0);
  return rows.map(item => ({
    ...item,
    percentValue: total ? Number(item.value || 0) * 100 / total : 0
  }));
}

function renderPieLabel({ cx, cy, midAngle, outerRadius, value, percentValue }) {
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

function formatPieTooltip(value, _name, props) {
  return [`${value} (${formatPercent(props?.payload?.percentValue)})`, 'Total'];
}

function formatPieLegend(value, entry) {
  const payload = entry?.payload || {};
  return `${value}: ${payload.value || 0} (${formatPercent(payload.percentValue)})`;
}

export function AdminStatistiquePage({ user }) {
  const canLoad = isAdmin(user);
  const statsResource = useAdminStatistics(canLoad);
  const stats = statsResource.statistics;

  if (statsResource.loading) {
    return (
      <section className="page-shell statistics-page">
        <Card title="Statistique" icon={BarChart3}>
          <div className="statistics-state">Chargement des statistiques...</div>
        </Card>
      </section>
    );
  }

  if (statsResource.error) {
    return (
      <section className="page-shell statistics-page">
        <Card title="Statistique" icon={BarChart3}>
          <ApiErrorState
            status={statsResource.errorStatus}
            title="Erreur lors du chargement des statistiques"
            message={statsResource.error}
            onRetry={statsResource.reload}
          />
        </Card>
      </section>
    );
  }

  if (!stats) {
    return (
      <section className="page-shell statistics-page">
        <Card title="Statistique" icon={BarChart3}>
          <div className="statistics-state">Aucune donnee disponible</div>
        </Card>
      </section>
    );
  }

  const reclamations = stats.reclamations || {};
  const interventions = stats.interventions || {};
  const savAgents = stats.savAgents || [];
  const technicians = stats.technicians || [];

  const kpis = [
    { label: 'Total reclamations', value: reclamations.totalReclamations || 0, icon: ClipboardList, tone: 'blue' },
    { label: 'Reclamations ouvertes', value: reclamations.openReclamations || 0, icon: Activity, tone: 'green' },
    { label: 'Reclamations en cours', value: reclamations.inProgressReclamations || 0, icon: TrendingUp, tone: 'orange' },
    { label: 'Resolues / cloturees', value: (reclamations.resolvedReclamations || 0) + (reclamations.closedReclamations || 0), icon: CheckCircle, tone: 'purple' },
    { label: 'Total interventions', value: interventions.totalInterventions || 0, icon: Wrench, tone: 'blue' },
    { label: 'Interventions planifiees', value: interventions.plannedInterventions || 0, icon: ClipboardList, tone: 'green' },
    { label: 'Interventions en cours', value: interventions.inProgressInterventions || 0, icon: Activity, tone: 'orange' },
    { label: 'Interventions terminees', value: interventions.completedInterventions || 0, icon: CheckCircle, tone: 'purple' },
    { label: 'Agents SAV actifs', value: stats.activeSavCount || 0, icon: Users, tone: 'green' },
    { label: 'Techniciens ST actifs', value: stats.activeTechnicianCount || 0, icon: UserRound, tone: 'blue' }
  ];

  const savBarData = savAgents.map(agent => ({ name: agent.fullName, value: agent.handledCount || 0 }));
  const technicianBarData = technicians.map(technician => ({ name: technician.fullName, value: technician.completedCount || 0 }));
  const reclamationStatusData = withPercentages(reclamations.byStatus);
  const interventionStatusData = withPercentages(interventions.byStatus);

  return (
    <section className="page-shell statistics-page">
      <div className="page-title-row statistics-title-row">
        <div>
          <span className="eyebrow">Admin</span>
          <h1>Statistique</h1>
          <p>Performance SAV, activite technicien et repartition operationnelle.</p>
        </div>
        <Button icon={RefreshCw} onClick={statsResource.reload}>Actualiser</Button>
      </div>

      <div className="statistics-kpi-grid">
        {kpis.map(item => <StatTile key={item.label} {...item} />)}
      </div>

      <div className="statistics-chart-grid">
        <ChartCard title="Reclamations traitees par agent SAV" icon={BarChart3} empty={!hasData(savBarData)}>
          <ResponsiveContainer width="100%" height={260}>
            <BarChart data={savBarData} margin={{ top: 8, right: 12, left: 0, bottom: 42 }}>
              <CartesianGrid strokeDasharray="3 3" vertical={false} />
              <XAxis dataKey="name" angle={-20} textAnchor="end" interval={0} height={64} tick={{ fontSize: 11 }} />
              <YAxis allowDecimals={false} />
              <Tooltip />
              <Bar dataKey="value" name="Traitees" radius={[6, 6, 0, 0]} fill="#1167ff" />
            </BarChart>
          </ResponsiveContainer>
        </ChartCard>

        <ChartCard title="Interventions realisees par technicien ST" icon={Wrench} empty={!hasData(technicianBarData)}>
          <ResponsiveContainer width="100%" height={260}>
            <BarChart data={technicianBarData} margin={{ top: 8, right: 12, left: 0, bottom: 42 }}>
              <CartesianGrid strokeDasharray="3 3" vertical={false} />
              <XAxis dataKey="name" angle={-20} textAnchor="end" interval={0} height={64} tick={{ fontSize: 11 }} />
              <YAxis allowDecimals={false} />
              <Tooltip />
              <Bar dataKey="value" name="Terminees" radius={[6, 6, 0, 0]} fill="#11b7a5" />
            </BarChart>
          </ResponsiveContainer>
        </ChartCard>

        <ChartCard title="Reclamations par statut" icon={PieChartIcon} empty={!hasData(reclamationStatusData)}>
          <ResponsiveContainer width="100%" height={260}>
            <PieChart>
              <Pie data={reclamationStatusData} dataKey="value" nameKey="name" innerRadius={50} outerRadius={78} paddingAngle={2} label={renderPieLabel} labelLine={false}>
                {reclamationStatusData.map((entry, index) => <Cell key={entry.name} fill={chartColors[index % chartColors.length]} />)}
              </Pie>
              <Tooltip formatter={formatPieTooltip} />
              <Legend formatter={formatPieLegend} />
            </PieChart>
          </ResponsiveContainer>
        </ChartCard>

        <ChartCard title="Interventions par statut" icon={PieChartIcon} empty={!hasData(interventionStatusData)}>
          <ResponsiveContainer width="100%" height={260}>
            <PieChart>
              <Pie data={interventionStatusData} dataKey="value" nameKey="name" innerRadius={50} outerRadius={78} paddingAngle={2} label={renderPieLabel} labelLine={false}>
                {interventionStatusData.map((entry, index) => <Cell key={entry.name} fill={chartColors[index % chartColors.length]} />)}
              </Pie>
              <Tooltip formatter={formatPieTooltip} />
              <Legend formatter={formatPieLegend} />
            </PieChart>
          </ResponsiveContainer>
        </ChartCard>

        <ChartCard title="Evolution des reclamations" icon={TrendingUp} empty={!hasData(reclamations.trend)} wide>
          <ResponsiveContainer width="100%" height={260}>
            <LineChart data={reclamations.trend || []} margin={{ top: 8, right: 18, left: 0, bottom: 12 }}>
              <CartesianGrid strokeDasharray="3 3" vertical={false} />
              <XAxis dataKey="name" tick={{ fontSize: 11 }} />
              <YAxis allowDecimals={false} />
              <Tooltip />
              <Line type="monotone" dataKey="value" name="Reclamations" stroke="#1167ff" strokeWidth={3} dot={{ r: 3 }} />
            </LineChart>
          </ResponsiveContainer>
        </ChartCard>
      </div>

      <div className="statistics-table-grid">
        <Card title="Performance des agents SAV" icon={Users}>
          {savAgents.length ? (
            <DataTable rows={savAgents.map(item => ({ ...item, id: item.userId }))} columns={[
              { key: 'fullName', label: 'Nom' },
              { key: 'email', label: 'Email', render: row => row.email || '-' },
              { key: 'assignedCount', label: 'Assignees' },
              { key: 'handledCount', label: 'Traitees' },
              { key: 'closedCount', label: 'Cloturees' },
              { key: 'resolutionRate', label: 'Taux resolution', render: row => <Badge tone="blue">{formatRate(row.resolutionRate)}</Badge> },
              { key: 'lastActivityLabel', label: 'Derniere activite' }
            ]} />
          ) : <div className="statistics-state compact">Aucune donnee disponible</div>}
        </Card>

        <Card title="Performance des techniciens ST" icon={Wrench}>
          {technicians.length ? (
            <DataTable rows={technicians.map(item => ({ ...item, id: item.technicianId }))} columns={[
              { key: 'fullName', label: 'Nom' },
              { key: 'email', label: 'Email', render: row => row.email || '-' },
              { key: 'assignedCount', label: 'Assignees' },
              { key: 'inProgressCount', label: 'En cours' },
              { key: 'completedCount', label: 'Terminees' },
              { key: 'completionRate', label: 'Taux realisation', render: row => <Badge tone="success">{formatRate(row.completionRate)}</Badge> },
              { key: 'lastActivityLabel', label: 'Derniere intervention' }
            ]} />
          ) : <div className="statistics-state compact">Aucune donnee disponible</div>}
        </Card>
      </div>
    </section>
  );
}

function StatTile({ label, value, icon: Icon, tone }) {
  return (
    <div className={`statistics-kpi-tile tone-${tone}`}>
      <span><Icon size={20} /></span>
      <strong>{value}</strong>
      <small>{label}</small>
    </div>
  );
}

function ChartCard({ title, icon: Icon, empty, wide = false, children }) {
  return (
    <Card title={title} icon={Icon} className={`statistics-chart-card ${wide ? 'wide' : ''}`}>
      {empty ? <div className="statistics-state compact">Aucune donnee disponible</div> : children}
    </Card>
  );
}
