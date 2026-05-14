import {
  AlertTriangle,
  CalendarDays,
  CalendarPlus,
  Download,
  Filter,
  Loader2,
  Package,
  Plus,
  RefreshCw,
  User,
  UserPlus,
  X
} from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { Avatar, Badge, Button, Card, DataTable, Field, IconButton, Modal, SearchInput, SelectFilter, Timeline } from '../components/ui';
import { AiPriorityAnalysisCard } from '../components/ai/AiPriorityAnalysisCard';
import { ApiErrorState } from '../components/common/ApiErrorState';
import { useClients } from '../hooks/useClients';
import { useNotifications } from '../hooks/useNotifications';
import { useReclamations } from '../hooks/useReclamations';
import { useUsers } from '../hooks/useUsers';
import { getFriendlyApiError } from '../utils/errorMessages';
import { canAccessPlanningRequests, canApplyAiPriority, canAssignTechnician, canCreateReclamation } from '../utils/roleAccess';

const tabs = ['All', 'Open', 'Assigned', 'In Progress', 'Resolved', 'Closed'];
const pageSize = 10;

function filterReclamations(rows, { status, query, client, product, model, priority }) {
  return rows.filter(item => {
    const haystack = `${item.id} ${item.reference} ${item.client} ${item.product} ${item.model} ${item.serial} ${item.description}`.toLowerCase();
    return (status === 'All' || item.status === status)
      && (!query || haystack.includes(query.toLowerCase()))
      && (!client || item.client === client)
      && (!product || item.product === product)
      && (!model || item.model === model)
      && (!priority || item.priority === priority);
  });
}

function technicalIdOf(row) {
  return row?.technicalId || row?.raw?.id || row?.id;
}

export function ReclamationsPage({ user, notify }) {
  const allowCreateReclamation = canCreateReclamation(user);
  const allowPlanningRequest = canAccessPlanningRequests(user);
  const allowAssignTechnician = canAssignTechnician(user);
  const allowApplyAiPriority = canApplyAiPriority(user);

  const reclamationResource = useReclamations();
  const clientsResource = useClients(allowCreateReclamation);
  const techniciansResource = useUsers('Technician', allowAssignTechnician);
  const notificationsResource = useNotifications(20, allowApplyAiPriority);
  const reclamations = reclamationResource.reclamations;
  const clients = clientsResource.clients;
  const technicians = techniciansResource.users;

  const [selectedId, setSelectedId] = useState('');
  const [status, setStatus] = useState('All');
  const [query, setQuery] = useState('');
  const [filters, setFilters] = useState({ client: '', product: '', model: '', priority: '' });
  const [page, setPage] = useState(1);
  const [modal, setModal] = useState(null);
  const [form, setForm] = useState({ client: '', product: '', model: '', serial: '', priority: 'High', description: '', site: '' });
  const [errors, setErrors] = useState({});
  const [history, setHistory] = useState([]);
  const [historyError, setHistoryError] = useState('');
  const [aiState, setAiState] = useState({ loading: false, error: '', result: null });

  const filteredRows = useMemo(() => filterReclamations(reclamations, { ...filters, status, query }), [reclamations, filters, status, query]);
  const totalPages = Math.max(1, Math.ceil(filteredRows.length / pageSize));
  const visibleRows = filteredRows.slice((page - 1) * pageSize, page * pageSize);
  const selected = reclamations.find(item => item.id === selectedId) || filteredRows[0] || reclamations[0];

  const counts = useMemo(() => {
    return tabs.reduce((acc, tab) => {
      acc[tab] = tab === 'All' ? reclamations.length : reclamations.filter(item => item.status === tab).length;
      return acc;
    }, {});
  }, [reclamations]);

  const options = useMemo(() => ({
    clients: [...new Set(reclamations.map(item => item.client).filter(Boolean))],
    products: [...new Set(reclamations.map(item => item.product).filter(Boolean))],
    models: [...new Set(reclamations.map(item => item.model).filter(Boolean))],
    priorities: ['Urgent', 'High', 'Medium', 'Low']
  }), [reclamations]);

  useEffect(() => {
    setPage(1);
  }, [status, query, filters]);

  useEffect(() => {
    if (!selectedId && reclamations[0]) setSelectedId(reclamations[0].id);
  }, [reclamations, selectedId]);

  useEffect(() => {
    if (selected && !filteredRows.some(item => item.id === selected.id) && filteredRows[0]) {
      setSelectedId(filteredRows[0].id);
    }
  }, [filteredRows, selected]);

  useEffect(() => {
    if (!selected) return undefined;
    let active = true;
    setHistoryError('');
    setAiState(current => ({ ...current, result: null, error: '' }));
    reclamationResource.getHistory(selected)
      .then(rows => {
        if (!active) return;
        setHistory((rows || []).map(item => ({
          title: item.comment || `${item.fromStatus} → ${item.toStatus}`,
          body: item.actorRole ? `By ${item.actorRole}` : '',
          time: item.occurredAt ? new Date(item.occurredAt).toLocaleString() : '',
          status: String(item.toStatus || ''),
          color: 'blue'
        })));
      })
      .catch(error => {
        if (active) setHistoryError(getFriendlyApiError(error));
      });
    reclamationResource.getLatestAiPriorityAnalysis(selected)
      .then(result => {
        if (active && result) setAiState(current => ({ ...current, result }));
      })
      .catch(() => {});
    return () => { active = false; };
  }, [selected?.id]);

  function updateFilter(key, value) {
    setFilters(current => ({ ...current, [key]: value }));
  }

  async function submitReclamation(event) {
    event.preventDefault();
    if (!allowCreateReclamation) {
      notify('Cette action est réservée au service SAV ou à l’administrateur.', 'error');
      return;
    }

    const nextErrors = {
      client: form.client ? '' : 'Client is required.',
      product: form.product ? '' : 'Product is required.',
      description: form.description ? '' : 'Description is required.',
      priority: form.priority ? '' : 'Priority is required.'
    };
    setErrors(nextErrors);
    if (Object.values(nextErrors).some(Boolean)) return;
    try {
      const reclamation = await reclamationResource.create(form);
      setSelectedId(reclamation.id);
      setModal(null);
      setForm({ client: '', product: '', model: '', serial: '', priority: 'High', description: '', site: '' });
      notify('Reclamation created successfully');
    } catch (err) {
      notify(getFriendlyApiError(err), 'error');
    }
  }

  async function handleCreatePlanningRequest() {
    if (!selected) return;
    if (!allowPlanningRequest) {
      notify('Cette action est réservée au service SAV ou à l’administrateur.', 'error');
      return;
    }
    try {
      await reclamationResource.requestPlanning(selected, 'Planning requested from SAV Pro UI');
      notify('Planning request sent to backend');
    } catch (err) {
      notify(getFriendlyApiError(err), 'error');
    }
  }

  async function handleAssign(technicianId) {
    if (!allowAssignTechnician) {
      notify('Cette action est réservée au service SAV ou à l’administrateur.', 'error');
      return;
    }
    const technician = technicians.find(item => String(item.id) === String(technicianId));
    if (!technician || !selected) return;
    try {
      await reclamationResource.planTechnician(selected, technician, { planningNote: 'Technician planned from SAV Pro UI' });
      setSelectedId(selected.id);
      setModal(null);
      notify('Technician assignment sent to backend');
    } catch (err) {
      notify(getFriendlyApiError(err), 'error');
    }
  }

  async function analyzePriority() {
    if (!selected) return;
    setAiState({ loading: true, error: '', result: null });
    try {
      const result = await reclamationResource.analyzePriority({
        reclamationId: technicalIdOf(selected),
        reference: selected.reference || selected.id,
        description: selected.description,
        productName: selected.product,
        brand: selected.raw?.brand || '',
        model: selected.model,
        currentPriority: selected.priority,
        clientImpact: selected.priority === 'Urgent' ? 'critical' : ''
      });
      setAiState({ loading: false, error: '', result });
    } catch (err) {
      setAiState({ loading: false, error: getFriendlyApiError(err), result: null });
    }
  }

  async function applyAiSuggestion() {
    if (!selected || !aiState.result) return;
    if (!allowApplyAiPriority) {
      notify('Cette action est réservée au service SAV ou à l’administrateur.', 'error');
      return;
    }
    const reason = `AI priority suggestion applied: ${selected.priority} -> ${aiState.result.suggestedPriority}. Reason: ${aiState.result.reason}`;
    try {
      if (aiState.result.analysisId) {
        await reclamationResource.applyAiPriorityAnalysis(selected, aiState.result.analysisId, { reason });
      } else {
        await reclamationResource.overridePriority(selected, aiState.result.suggestedPriority, reason);
      }
      await notificationsResource.reload();
      const rows = await reclamationResource.getHistory(selected);
      setHistory((rows || []).map(item => ({
        title: item.comment || `${item.fromStatus} → ${item.toStatus}`,
        body: item.actorRole ? `By ${item.actorRole}` : '',
        time: item.occurredAt ? new Date(item.occurredAt).toLocaleString() : '',
        status: String(item.toStatus || ''),
        color: 'blue'
      })));
      notify('AI priority suggestion applied in backend');
    } catch (err) {
      notify(getFriendlyApiError(err), 'error');
    }
  }

  const isLoading = reclamationResource.loading || clientsResource.loading || techniciansResource.loading;
  const loadError = reclamationResource.error || clientsResource.error || techniciansResource.error;
  const loadErrorStatus = reclamationResource.errorStatus || clientsResource.errorStatus || techniciansResource.errorStatus;

  return (
    <section className="reclamations-page split-admin-page">
      <div className="reclamation-content">
        <div className="page-title-row with-top-gap">
          <div>
            <h1>Reclamations</h1>
            <p>Manage and track all customer reclamations</p>
          </div>
        </div>

        {loadError && (
          <Card className="filter-card">
            <ApiErrorState status={loadErrorStatus} message={loadError} onRetry={reclamationResource.reload} />
          </Card>
        )}

        <Card className="filter-card">
          <div className="filter-row">
            <SelectFilter label="All Clients" options={options.clients} value={filters.client} onChange={value => updateFilter('client', value)} />
            <SelectFilter label="All Products" options={options.products} value={filters.product} onChange={value => updateFilter('product', value)} />
            <SelectFilter label="All Models" options={options.models} value={filters.model} onChange={value => updateFilter('model', value)} />
            <SelectFilter label="All Priorities" options={options.priorities} value={filters.priority} onChange={value => updateFilter('priority', value)} />
            <button type="button" className="date-filter" onClick={() => notify('Date filter applies to backend results when supported')}>
              Backend range
              <CalendarDays size={16} />
            </button>
            <Button icon={Filter} onClick={() => notify('Filters applied')}>Filters</Button>
          </div>
        </Card>

        <Card className="reclamation-table-card">
          <div className="status-tabs">
            {tabs.map(tab => (
              <button type="button" key={tab} className={status === tab ? 'active' : ''} onClick={() => setStatus(tab)}>
                {tab}
                <span>{counts[tab] || 0}</span>
              </button>
            ))}
          </div>

          <div className="table-toolbar">
            <SearchInput value={query} onChange={setQuery} placeholder="Search reclamations..." />
            <Button icon={Download} onClick={() => notify('Export prepared from current backend rows')}>Export</Button>
            {allowCreateReclamation ? <Button variant="primary" icon={Plus} onClick={() => setModal('new')}>New Reclamation</Button> : null}
          </div>

          {isLoading ? (
            <div className="empty-state"><Loader2 size={18} /> Loading reclamations from backend...</div>
          ) : (
            <DataTable
              rows={visibleRows}
              selectedId={selected?.id}
              onRowClick={row => setSelectedId(row.id)}
              columns={[
                { key: 'id', label: 'Reference', render: row => <button type="button" className="table-link" onClick={event => { event.stopPropagation(); setSelectedId(row.id); }}>{row.id}</button> },
                { key: 'client', label: 'Client' },
                { key: 'product', label: 'Product' },
                { key: 'model', label: 'Model' },
                { key: 'serial', label: 'Serial Number' },
                { key: 'priority', label: 'Priority', render: row => <Badge>{row.priority}</Badge> },
                { key: 'status', label: 'Status', render: row => <Badge>{row.status}</Badge> },
                { key: 'created', label: 'Created Date' },
                {
                  key: 'assigned',
                  label: 'Assigned Technician',
                  render: row => row.assigned ? (
                    <span className="avatar-cell">
                      <Avatar name={row.assigned} initials={row.assignedAvatar} size="sm" />
                      {row.assigned}
                    </span>
                  ) : '-'
                }
              ]}
            />
          )}

          <div className="table-footer">
            <span>Showing {visibleRows.length ? (page - 1) * pageSize + 1 : 0} to {Math.min(page * pageSize, filteredRows.length)} of {filteredRows.length} results</span>
            <div className="pagination">
              <button type="button" onClick={() => setPage(current => Math.max(1, current - 1))}>‹</button>
              {Array.from({ length: Math.min(3, totalPages) }, (_, index) => index + 1).map(item => (
                <button type="button" key={item} className={page === item ? 'active' : ''} onClick={() => setPage(item)}>{item}</button>
              ))}
              {totalPages > 3 && <button type="button" onClick={() => notify('More pages available')}>...</button>}
              {totalPages > 3 && <button type="button" onClick={() => setPage(totalPages)}>{totalPages}</button>}
              <button type="button" onClick={() => setPage(current => Math.min(totalPages, current + 1))}>›</button>
            </div>
          </div>
        </Card>
      </div>

      {selected && (
        <aside className="details-drawer">
          <header className="drawer-header">
            <div>
              <h2>{selected.id}</h2>
              <Badge>{selected.status}</Badge>
            </div>
            <div>
              <IconButton icon={X} label="Close details" className="ghost" onClick={() => notify('Details panel kept open for desktop layout')} />
            </div>
          </header>

          <div className="sla-box">
            <AlertTriangle size={24} />
            <div>
              <strong>SLA at risk</strong>
              <span>{selected.sla}</span>
              <div className="progress-track"><span style={{ width: `${selected.progress}%` }} /></div>
            </div>
            <aside>
              <span>SLA Goal</span>
              <strong>{selected.slaGoal}</strong>
            </aside>
          </div>

          <section className="drawer-section">
            <h3>Description</h3>
            <p>{selected.description}</p>
          </section>

          <AiPriorityAnalysisCard
            reclamation={selected}
            analysis={aiState.result}
            loading={aiState.loading}
            error={aiState.error}
            onAnalyze={analyzePriority}
            onApply={applyAiSuggestion}
            onRetry={analyzePriority}
            canApply={allowApplyAiPriority}
          />

          <section className="info-columns">
            <div>
              <h3>Client Information</h3>
              <InfoLine icon={User} label={selected.client} />
              <dl>
                <dt>Contact</dt><dd>{selected.contact}</dd>
                <dt>Email</dt><dd>{selected.email}</dd>
                <dt>Phone</dt><dd>{selected.phone}</dd>
                <dt>Location</dt><dd>{selected.location}</dd>
              </dl>
            </div>
            <div>
              <h3>Product Information</h3>
              <InfoLine icon={Package} label={selected.product} />
              <dl>
                <dt>Model</dt><dd>{selected.model}</dd>
                <dt>Serial Number</dt><dd>{selected.serial}</dd>
                <dt>Warranty</dt><dd><Badge tone="success">{selected.warranty}</Badge></dd>
                <dt>Installation Date</dt><dd>Backend product data</dd>
              </dl>
            </div>
          </section>

          <div className="meta-grid">
            <span><small>Priority</small><Badge>{selected.priority}</Badge></span>
            <span><small>Created Date</small><strong>{selected.created}</strong></span>
            <span><small>Source</small><strong>{selected.source}</strong></span>
            <span><small>Reported By</small><strong>{selected.contact}</strong></span>
          </div>

          {(allowPlanningRequest || allowAssignTechnician) && (
            <section className="drawer-section">
              <h3>Actions</h3>
              <div className="drawer-actions">
                {allowPlanningRequest ? <Button icon={CalendarPlus} onClick={handleCreatePlanningRequest}>Create Planning Request</Button> : null}
                {allowAssignTechnician ? <Button variant="primary" icon={UserPlus} onClick={() => setModal('assign')}>Assign Technician</Button> : null}
              </div>
            </section>
          )}

          <section className="drawer-section">
            <h3>Reclamation History</h3>
            {historyError ? <p>{historyError}</p> : <Timeline items={history} compact />}
          </section>
        </aside>
      )}

      {modal === 'new' && allowCreateReclamation && (
        <Modal title="New Reclamation" onClose={() => setModal(null)} footer={(
          <>
            <Button onClick={() => setModal(null)}>Cancel</Button>
            <Button variant="primary" icon={Plus} onClick={submitReclamation}>Create Reclamation</Button>
          </>
        )}>
          <form className="form-grid" onSubmit={submitReclamation}>
            <Field label="Client" error={errors.client}>
              <select value={form.client} onChange={event => setForm(current => ({ ...current, client: event.target.value }))}>
                <option value="">Select client</option>
                {clients.map(client => <option key={client.id} value={client.name}>{client.name}</option>)}
              </select>
            </Field>
            <Field label="Product" error={errors.product}>
              <input value={form.product} onChange={event => setForm(current => ({ ...current, product: event.target.value }))} placeholder="Installation" />
            </Field>
            <Field label="Model">
              <input value={form.model} onChange={event => setForm(current => ({ ...current, model: event.target.value }))} placeholder="REF-2004" />
            </Field>
            <Field label="Priority" error={errors.priority}>
              <select value={form.priority} onChange={event => setForm(current => ({ ...current, priority: event.target.value }))}>
                <option>Urgent</option>
                <option>High</option>
                <option>Medium</option>
                <option>Low</option>
              </select>
            </Field>
            <Field label="Serial Number">
              <input value={form.serial} onChange={event => setForm(current => ({ ...current, serial: event.target.value }))} placeholder="REF-2004-0001" />
            </Field>
            <Field label="Site">
              <input value={form.site} onChange={event => setForm(current => ({ ...current, site: event.target.value }))} placeholder="Installation site" />
            </Field>
            <Field label="Description" error={errors.description}>
              <textarea value={form.description} onChange={event => setForm(current => ({ ...current, description: event.target.value }))} placeholder="Describe the issue" />
            </Field>
          </form>
        </Modal>
      )}

      {modal === 'assign' && allowAssignTechnician && (
        <Modal title="Assign Technician" onClose={() => setModal(null)}>
          <div className="request-card-list">
            {technicians.map(technician => (
              <button type="button" key={technician.id} className="planning-request-card" onClick={() => handleAssign(technician.id)}>
                <div className="request-card-head">
                  <strong>{technician.name}</strong>
                  <Badge tone="success">Backend</Badge>
                  <UserPlus size={17} />
                </div>
                <span className="request-line">{technician.role}</span>
                <span className="request-line">{technician.email}</span>
              </button>
            ))}
          </div>
        </Modal>
      )}
    </section>
  );
}

function InfoLine({ icon: Icon, label }) {
  return (
    <div className="info-line">
      <Icon size={19} />
      <strong>{label}</strong>
    </div>
  );
}
