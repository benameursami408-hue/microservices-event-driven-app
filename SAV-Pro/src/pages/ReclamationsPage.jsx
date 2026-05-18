import {
  AlertTriangle,
  Building2,
  CalendarDays,
  CalendarPlus,
  CheckCircle,
  ClipboardList,
  Download,
  FileText,
  Filter,
  Flag,
  Loader2,
  Mail,
  MapPin,
  Package,
  Pencil,
  Phone,
  Plus,
  Save,
  ShieldCheck,
  Trash2,
  User,
  UserPlus,
  X
} from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { Avatar, Badge, Button, Card, DataTable, DeleteConfirmModal, Field, IconButton, Modal, SearchInput, SelectFilter, Timeline } from '../components/ui';
import { AiPriorityAnalysisCard } from '../components/ai/AiPriorityAnalysisCard';
import { ApiErrorState } from '../components/common/ApiErrorState';
import { useClients } from '../hooks/useClients';
import { useNotifications } from '../hooks/useNotifications';
import { useReclamations } from '../hooks/useReclamations';
import { useUsers } from '../hooks/useUsers';
import { getFriendlyApiError } from '../utils/errorMessages';
import { canAccessPlanningRequests, canApplyAiPriority, canAssignTechnician, canCreateReclamation } from '../utils/roleAccess';
import { fromApiReclamationStatus } from '../api/mappers/statusMapper';

const tabs = ['All', 'Open', 'Assigned', 'In Progress', 'Resolved', 'Closed'];
const pageSize = 10;
const blankReclamationForm = { clientId: '', client: '', product: '', model: '', serial: '', priority: 'High', description: '', site: '' };

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

function clientDetailsFor(row, clients) {
  const byId = clients.find(client => String(client.id) === String(row?.clientId));
  const byName = clients.find(client => client.name === row?.client);
  const client = byId || byName || {};
  return {
    contact: client.contact || row?.contact || row?.client || 'Not provided',
    email: client.email || row?.email || 'Not provided',
    phone: client.phone || client.phoneNumber || row?.phone || 'Not provided',
    location: client.location || row?.location || row?.site || 'Not provided'
  };
}

function formFromReclamation(row = {}) {
  return {
    clientId: row.clientId || '',
    client: row.client || '',
    product: row.product || '',
    model: row.model === '-' ? '' : row.model || '',
    serial: row.serial === '-' ? '' : row.serial || '',
    priority: row.priority || 'High',
    description: row.description || '',
    site: row.site || row.location || ''
  };
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
  const [form, setForm] = useState(blankReclamationForm);
  const [editingReclamation, setEditingReclamation] = useState(null);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [pendingTechnicianId, setPendingTechnicianId] = useState('');
  const [errors, setErrors] = useState({});
  const [history, setHistory] = useState([]);
  const [historyError, setHistoryError] = useState('');
  const [aiState, setAiState] = useState({ loading: false, error: '', result: null });

  const filteredRows = useMemo(() => filterReclamations(reclamations, { ...filters, status, query }), [reclamations, filters, status, query]);
  const totalPages = Math.max(1, Math.ceil(filteredRows.length / pageSize));
  const visibleRows = filteredRows.slice((page - 1) * pageSize, page * pageSize);
  const selected = reclamations.find(item => item.id === selectedId) || filteredRows[0] || reclamations[0];
  const selectedClientDetails = selected ? clientDetailsFor(selected, clients) : null;
  const selectedFormClient = clients.find(client => String(client.id) === String(form.clientId));
  const formClientName = selectedFormClient?.name || form.client || editingReclamation?.client || 'Client not selected';
  const formProductLabel = form.product || editingReclamation?.product || 'Product pending';
  const formPriorityLabel = form.priority || editingReclamation?.priority || 'Priority';

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
          title: item.comment || `${fromApiReclamationStatus(item.fromStatus)} -> ${fromApiReclamationStatus(item.toStatus)}`,
          body: item.actorRole ? `By ${item.actorRole}` : '',
          time: item.occurredAt ? new Date(item.occurredAt).toLocaleString() : '',
          status: fromApiReclamationStatus(item.toStatus),
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

  function openNewReclamation() {
    setEditingReclamation(null);
    setErrors({});
    setForm(blankReclamationForm);
    setModal('new');
  }

  function openEditReclamation(row) {
    setEditingReclamation(row);
    setErrors({});
    setForm(formFromReclamation(row));
    setModal('edit');
  }

  function exportVisibleRows() {
    const columns = [
      ['Reference', 'id'],
      ['Client', 'client'],
      ['Product', 'product'],
      ['Model', 'model'],
      ['Serial Number', 'serial'],
      ['Priority', 'priority'],
      ['Status', 'status'],
      ['Created Date', 'created'],
      ['Assigned Technician', 'assigned']
    ];
    const escapeCsv = value => `"${String(value ?? '').replace(/"/g, '""')}"`;
    const csv = [
      columns.map(([label]) => escapeCsv(label)).join(','),
      ...filteredRows.map(row => columns.map(([, key]) => escapeCsv(row[key])).join(','))
    ].join('\n');
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = `reclamations-${new Date().toISOString().slice(0, 10)}.csv`;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(url);
    notify('CSV export downloaded');
  }

  async function submitReclamation(event) {
    event.preventDefault();
    if (!allowCreateReclamation) {
      notify('Cette action est réservée au service SAV ou à l’administrateur.', 'error');
      return;
    }

    const selectedClient = clients.find(client => String(client.id) === String(form.clientId));
    const nextErrors = {
      client: form.clientId || editingReclamation ? '' : 'Client is required.',
      product: form.product ? '' : 'Product is required.',
      priority: form.priority ? '' : 'Priority is required.'
    };
    setErrors(nextErrors);
    if (Object.values(nextErrors).some(Boolean)) return;
    try {
      const payload = {
        ...form,
        client: selectedClient?.name || form.client || editingReclamation?.client || '',
        clientId: form.clientId || editingReclamation?.clientId || ''
      };
      const reclamation = editingReclamation
        ? await reclamationResource.update(editingReclamation, payload)
        : await reclamationResource.create(payload);
      setSelectedId(reclamation.id);
      setModal(null);
      setForm(blankReclamationForm);
      setEditingReclamation(null);
      notify(editingReclamation ? 'Reclamation updated successfully' : 'Reclamation created successfully');
    } catch (err) {
      notify(getFriendlyApiError(err), 'error');
    }
  }

  async function confirmDeleteReclamation() {
    if (!deleteTarget) return;
    try {
      await reclamationResource.remove(deleteTarget);
      setDeleteTarget(null);
      setModal(null);
      const next = reclamations.find(item => item.id !== deleteTarget.id);
      setSelectedId(next?.id || '');
      notify('Reclamation deleted');
    } catch (err) {
      notify(getFriendlyApiError(err), 'error');
    }
  }

  async function ensureSavAssigned(row) {
    if (!row) return row;
    if (row.status !== 'Open') return row;
    return reclamationResource.assignToSav(row, user);
  }

  async function handleCreatePlanningRequest() {
    if (!selected) return;
    if (!allowPlanningRequest) {
      notify('Cette action est réservée au service SAV ou à l’administrateur.', 'error');
      return;
    }
    try {
      const assigned = await ensureSavAssigned(selected);
      await reclamationResource.requestPlanning(assigned, 'Planning requested from SAV Pro UI');
      notify('Planning request sent to backend');
    } catch (err) {
      notify(getFriendlyApiError(err), 'error');
    }
  }

  async function handleAssign() {
    if (!allowAssignTechnician) {
      notify('Cette action est réservée au service SAV ou à l’administrateur.', 'error');
      return;
    }
    const technician = technicians.find(item => String(item.id) === String(pendingTechnicianId));
    if (!technician || !selected) return;
    try {
      const assigned = await ensureSavAssigned(selected);
      await reclamationResource.planTechnician(assigned, technician, { planningNote: selected.technicianId ? 'Technician changed from SAV Pro UI' : 'Technician assigned from SAV Pro UI' });
      setSelectedId(selected.id);
      setModal(null);
      setPendingTechnicianId('');
      notify(selected.technicianId ? 'Technician changed' : 'Technician assigned');
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
        title: item.comment || `${fromApiReclamationStatus(item.fromStatus)} -> ${fromApiReclamationStatus(item.toStatus)}`,
        body: item.actorRole ? `By ${item.actorRole}` : '',
        time: item.occurredAt ? new Date(item.occurredAt).toLocaleString() : '',
        status: fromApiReclamationStatus(item.toStatus),
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
  const canRequestSelectedPlanning = Boolean(selected && allowPlanningRequest && ['Open', 'Assigned'].includes(selected.status));
  const canAssignSelectedTechnician = Boolean(selected && allowAssignTechnician && ['Open', 'Assigned', 'Planned'].includes(selected.status));

  return (
    <section className="reclamations-page split-admin-page">
      <div className="reclamation-content">
        <div className="page-title-row with-top-gap reclamations-title-row">
          <div>
            <span className="eyebrow">Service queue</span>
            <h1>Reclamations</h1>
            <p>Manage and track all customer reclamations</p>
          </div>
          <div className="page-title-kpis">
            <span><strong>{counts.Open || 0}</strong><small>Open</small></span>
            <span><strong>{counts['In Progress'] || 0}</strong><small>In progress</small></span>
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
            <button type="button" className="date-filter" onClick={() => notify('Date filter applies to the current result set')}>
              Date range
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
            <Button icon={Download} onClick={exportVisibleRows}>Export</Button>
            {allowCreateReclamation ? <Button variant="primary" icon={Plus} onClick={openNewReclamation}>New Reclamation</Button> : null}
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
                },
                {
                  key: 'actions',
                  label: 'Actions',
                  render: row => allowCreateReclamation ? (
                    <span className="row-actions">
                      <button type="button" className="table-icon-action" title="Edit reclamation" onClick={event => { event.stopPropagation(); openEditReclamation(row); }}>
                        <Pencil size={15} />
                      </button>
                      <button type="button" className="table-icon-action danger" title="Delete reclamation" onClick={event => { event.stopPropagation(); setDeleteTarget(row); setModal('delete'); }}>
                        <Trash2 size={15} />
                      </button>
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
              <div className="contact-data-grid">
                <InfoTile icon={User} label="Contact" value={selectedClientDetails.contact} />
                <InfoTile icon={Mail} label="Email" value={selectedClientDetails.email} />
                <InfoTile icon={Phone} label="Phone" value={selectedClientDetails.phone} />
                <InfoTile icon={MapPin} label="Location" value={selectedClientDetails.location} />
              </div>
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
            <span><small>Reported By</small><strong>{selected.contact}</strong></span>
          </div>

          {(canRequestSelectedPlanning || canAssignSelectedTechnician) && (
            <section className="drawer-section">
              <h3>Actions</h3>
              <div className="drawer-actions">
                {canRequestSelectedPlanning ? <Button icon={CalendarPlus} onClick={handleCreatePlanningRequest}>Create Planning Request</Button> : null}
                {canAssignSelectedTechnician ? <Button variant="primary" icon={UserPlus} onClick={() => { setPendingTechnicianId(String(selected.technicianId || '')); setModal('assign'); }}>{selected.technicianId ? 'Change Technician' : 'Assign Technician'}</Button> : null}
              </div>
            </section>
          )}

          <section className="drawer-section">
            <h3>Reclamation History</h3>
            {historyError ? <p>{historyError}</p> : <Timeline items={history} compact />}
          </section>
        </aside>
      )}

      {(modal === 'new' || modal === 'edit') && allowCreateReclamation && (
        <Modal className="form-modal-card" title={editingReclamation ? 'Edit Reclamation' : 'New Reclamation'} onClose={() => { setModal(null); setEditingReclamation(null); }} footer={(
          <>
            <Button onClick={() => { setModal(null); setEditingReclamation(null); }}>Cancel</Button>
            <Button variant="primary" icon={editingReclamation ? Save : Plus} onClick={submitReclamation}>{editingReclamation ? 'Save Changes' : 'Create Reclamation'}</Button>
          </>
        )}>
          <div className="structured-modal reclamation-entry-modal">
            <div className="modal-summary-strip">
              <div className="modal-summary-main">
                <span className="modal-summary-icon"><ClipboardList size={22} /></span>
                <div>
                  <span className="modal-summary-eyebrow">{editingReclamation ? 'Existing case' : 'New case'}</span>
                  <strong>{formClientName}</strong>
                  <p>{formProductLabel}</p>
                </div>
              </div>
              <div className="modal-summary-metrics">
                <span><Flag size={15} /><small>Priority</small><strong>{formPriorityLabel}</strong></span>
                <span><MapPin size={15} /><small>Site</small><strong>{form.site || 'Not set'}</strong></span>
              </div>
            </div>

            <form className="structured-form reclamation-form-grid" onSubmit={submitReclamation}>
              <section className="form-section form-section-wide">
                <div className="form-section-heading">
                  <span><Building2 size={16} /></span>
                  <h3>Client</h3>
                </div>
                <div className="structured-field-grid">
                  <Field label="Client" error={errors.client} className="full">
                    <select value={form.clientId} disabled={Boolean(editingReclamation)} onChange={event => {
                      const client = clients.find(item => String(item.id) === event.target.value);
                      setForm(current => ({ ...current, clientId: event.target.value, client: client?.name || '' }));
                    }}>
                      <option value="">Select client</option>
                      {clients.map(client => <option key={client.id} value={client.id}>{client.name}</option>)}
                    </select>
                  </Field>
                </div>
              </section>

              <section className="form-section">
                <div className="form-section-heading">
                  <span><Package size={16} /></span>
                  <h3>Product</h3>
                </div>
                <div className="structured-field-grid">
                  <Field label="Product" error={errors.product} className="full">
                    <input value={form.product} onChange={event => setForm(current => ({ ...current, product: event.target.value }))} placeholder="Product name" />
                  </Field>
                  <Field label="Model">
                    <input value={form.model} onChange={event => setForm(current => ({ ...current, model: event.target.value }))} placeholder="REF-2004" />
                  </Field>
                  <Field label="Serial Number">
                    <input value={form.serial} onChange={event => setForm(current => ({ ...current, serial: event.target.value }))} placeholder="REF-2004-0001" />
                  </Field>
                </div>
              </section>

              <section className="form-section">
                <div className="form-section-heading">
                  <span><ShieldCheck size={16} /></span>
                  <h3>Priority and site</h3>
                </div>
                <div className="structured-field-grid">
                  <Field label="Priority" error={errors.priority}>
                    <select value={form.priority} onChange={event => setForm(current => ({ ...current, priority: event.target.value }))}>
                      <option>Urgent</option>
                      <option>High</option>
                      <option>Medium</option>
                      <option>Low</option>
                    </select>
                  </Field>
                  <Field label="Site">
                    <input value={form.site} onChange={event => setForm(current => ({ ...current, site: event.target.value }))} placeholder="Installation site" />
                  </Field>
                </div>
              </section>

              <section className="form-section form-section-wide">
                <div className="form-section-heading">
                  <span><FileText size={16} /></span>
                  <h3>Issue details</h3>
                </div>
                <div className="structured-field-grid">
                  <Field label="Description (optional)" className="full">
                    <textarea value={form.description} onChange={event => setForm(current => ({ ...current, description: event.target.value }))} placeholder="Describe the issue" />
                  </Field>
                </div>
              </section>
            </form>
          </div>
        </Modal>
      )}

      {modal === 'delete' && deleteTarget && (
        <DeleteConfirmModal
          title="Delete reclamation?"
          subject={deleteTarget.id}
          description={`This will permanently remove the reclamation for ${deleteTarget.client || 'the selected client'} after backend verification.`}
          onClose={() => { setModal(null); setDeleteTarget(null); }}
          onConfirm={confirmDeleteReclamation}
        />
      )}

      {modal === 'assign' && allowAssignTechnician && (
        <Modal title={selected?.technicianId ? 'Change Technician' : 'Assign Technician'} onClose={() => { setModal(null); setPendingTechnicianId(''); }} footer={(
          <>
            <Button onClick={() => { setModal(null); setPendingTechnicianId(''); }}>Cancel</Button>
            <Button variant="primary" icon={CheckCircle} onClick={handleAssign} disabled={!pendingTechnicianId}>
              {selected?.technicianId ? 'Confirm Change' : 'Confirm Assignment'}
            </Button>
          </>
        )}>
          <div className="assign-summary">
            <ShieldCheck size={20} />
            <div>
              <strong>{selected?.id}</strong>
              <p>{selected?.assigned ? `Current technician: ${selected.assigned}` : 'No technician assigned yet.'}</p>
            </div>
          </div>
          <div className="request-card-list assign-technician-list">
            {technicians.map(technician => (
              <button
                type="button"
                key={technician.id}
                className={`planning-request-card ${String(pendingTechnicianId) === String(technician.id) ? 'active' : ''}`}
                onClick={() => setPendingTechnicianId(String(technician.id))}
              >
                <div className="request-card-head">
                  <strong>{technician.name}</strong>
                  {String(selected?.technicianId) === String(technician.id) ? <Badge tone="success">Current</Badge> : null}
                </div>
                <span className="request-line"><UserPlus size={16} />{technician.role}</span>
                <span className="request-line"><Mail size={16} />{technician.email}</span>
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

function InfoTile({ icon: Icon, label, value }) {
  return (
    <div className="info-tile">
      <Icon size={16} />
      <span>{label}</span>
      <strong>{value || 'Not provided'}</strong>
    </div>
  );
}
