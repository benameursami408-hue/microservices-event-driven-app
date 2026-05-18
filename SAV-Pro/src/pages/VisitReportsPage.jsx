import { CalendarDays, ClipboardList, Eye, FileText, Hash, Send, UserRound } from 'lucide-react';
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
  const canPublishSelected = selected && !clientMode && !technicianMode && selected.status !== 'Published';

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
      {selected && (
        <Modal
          className="visit-report-preview-modal"
          title="Report Preview"
          onClose={() => setSelectedId('')}
          footer={(
            <>
              <Button onClick={() => setSelectedId('')}>Close</Button>
              {canPublishSelected && <Button variant="primary" icon={Send} onClick={() => handlePublish(selected.id)}>Publish Report</Button>}
            </>
          )}
        >
          <div className="report-preview">
            <div className="report-preview-hero">
              <span className="report-preview-mark"><FileText size={24} /></span>
              <div className="report-preview-heading">
                <div className="report-preview-eyebrow-row">
                  <span>Intervention report</span>
                  <Badge>{selected.status}</Badge>
                </div>
                <h3>{selected.client || 'Unassigned client'}</h3>
                <p>{selected.id}</p>
              </div>
            </div>

            <section className="report-preview-note" aria-label="Report summary">
              <span>Report summary</span>
              <p>{selected.summary || 'No summary has been recorded for this visit report yet.'}</p>
            </section>

            <div className="report-preview-grid">
              <div className="report-preview-detail">
                <span><UserRound size={18} /></span>
                <small>Technician</small>
                <strong>{selected.technicianName || '-'}</strong>
              </div>
              <div className="report-preview-detail">
                <span><Hash size={18} /></span>
                <small>Reclamation</small>
                <strong>{selected.reclamationId || '-'}</strong>
              </div>
              <div className="report-preview-detail">
                <span><CalendarDays size={18} /></span>
                <small>Created</small>
                <strong>{formatDate(selected.createdAt)}</strong>
              </div>
              <div className="report-preview-detail">
                <span><ClipboardList size={18} /></span>
                <small>Status</small>
                <strong>{selected.status}</strong>
              </div>
            </div>
          </div>
        </Modal>
      )}
    </section>
  );
}

function formatDate(value) { return value ? new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric' }).format(new Date(value)) : '-'; }
