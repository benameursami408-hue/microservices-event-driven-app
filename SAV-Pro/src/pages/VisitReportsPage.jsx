import { Eye, FileText, Send } from 'lucide-react';
import { useMemo, useState } from 'react';
import { ApiErrorState } from '../components/common/ApiErrorState';
import { Badge, Button, Card, DataTable, Modal, SearchInput } from '../components/ui';
import { useVisitReports } from '../hooks/useVisitReports';
import { getFriendlyApiError } from '../utils/errorMessages';
import { isTechnician } from '../utils/roleAccess';

export function VisitReportsPage({ notify, clientMode = false, user }) {
  const technicianMode = isTechnician(user);
  const reportResource = useVisitReports({ clientMode, mine: technicianMode });
  const [query, setQuery] = useState('');
  const [filter, setFilter] = useState('All');
  const [selectedId, setSelectedId] = useState('');
  const reports = useMemo(() => reportResource.reports
    .filter(item => filter === 'All' || item.status === filter)
    .filter(item => `${item.id} ${item.client} ${item.technicianName} ${item.summary}`.toLowerCase().includes(query.toLowerCase())), [reportResource.reports, filter, query]);
  const selected = reports.find(item => item.id === selectedId);

  async function handlePublish(reportId) {
    try {
      await reportResource.publish(reportId);
      notify('Report published in backend');
    } catch (err) {
      notify(getFriendlyApiError(err), 'error');
    }
  }

  return (
    <section className="page-shell visit-reports-page">
      <div className="page-title-row visit-reports-title-row">
        <div>
          <span className="eyebrow">Intervention records</span>
          <h1>Visit Reports</h1>
          <p>{clientMode || technicianMode ? 'Reports linked to your interventions.' : 'Draft and published intervention reports.'}</p>
        </div>
        <div className="page-title-kpis">
          <span><strong>{reports.length}</strong><small>Reports</small></span>
          <span><strong>{reports.filter(item => item.status === 'Draft').length}</strong><small>Draft</small></span>
        </div>
      </div>

      <Card title="Visit Reports" icon={FileText}>
        {reportResource.error ? <ApiErrorState status={reportResource.errorStatus} message={reportResource.error} onRetry={reportResource.reload} /> : null}
        <div className="table-toolbar"><SearchInput value={query} onChange={setQuery} placeholder="Search reports..." /><Button variant={filter === 'All' ? 'primary' : 'secondary'} onClick={() => setFilter('All')}>All</Button><Button variant={filter === 'Draft' ? 'primary' : 'secondary'} onClick={() => setFilter('Draft')}>Draft</Button><Button variant={filter === 'Published' ? 'primary' : 'secondary'} onClick={() => setFilter('Published')}>Published</Button></div>
        {reportResource.loading ? <div className="empty-panel">Loading visit reports from backend...</div> : <DataTable rows={reports} selectedId={selectedId} onRowClick={row => setSelectedId(row.id)} columns={[{ key: 'id', label: 'Report', render: row => <button type="button" className="table-link" onClick={event => { event.stopPropagation(); setSelectedId(row.id); }}>{row.id}</button> }, { key: 'reclamationId', label: 'Reclamation' }, { key: 'client', label: 'Client' }, { key: 'technicianName', label: 'Technician' }, { key: 'status', label: 'Status', render: row => <Badge>{row.status}</Badge> }, { key: 'createdAt', label: 'Created', render: row => formatDate(row.createdAt) }, { key: 'actions', label: 'Actions', render: row => <span className="avatar-cell"><Button size="sm" icon={Eye} onClick={event => { event.stopPropagation(); setSelectedId(row.id); }}>Preview</Button>{!clientMode && !technicianMode && row.status !== 'Published' && <Button size="sm" variant="primary" icon={Send} onClick={event => { event.stopPropagation(); handlePublish(row.id); }}>Publish</Button>}</span> }]} />}
      </Card>
      {selected && <Modal title={`Preview ${selected.id}`} onClose={() => setSelectedId('')} footer={!clientMode && !technicianMode && selected.status !== 'Published' ? <Button variant="primary" icon={Send} onClick={() => handlePublish(selected.id)}>Publish Report</Button> : null}><div className="report-preview"><Badge>{selected.status}</Badge><h3>{selected.client}</h3><p>{selected.summary}</p><p>Technician: <strong>{selected.technicianName}</strong></p><p>Reclamation: <strong>{selected.reclamationId}</strong></p></div></Modal>}
    </section>
  );
}

function formatDate(value) { return value ? new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric' }).format(new Date(value)) : '-'; }
