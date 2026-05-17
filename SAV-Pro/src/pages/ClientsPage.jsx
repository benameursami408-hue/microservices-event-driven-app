import { ArrowLeft, Building2, CalendarDays, ClipboardList, Edit, Eye, Mail, MapPin, Phone, Plus, UserRound } from 'lucide-react';
import { useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ApiErrorState } from '../components/common/ApiErrorState';
import { Badge, Button, Card, DataTable, Field, Modal, SearchInput } from '../components/ui';
import { useClients } from '../hooks/useClients';
import { usePlanning } from '../hooks/usePlanning';
import { useReclamations } from '../hooks/useReclamations';
import { getFriendlyApiError } from '../utils/errorMessages';

const emptyClient = { name: '', contact: '', email: '', phone: '', location: '' };

export function ClientsPage({ notify, detail = false }) {
  const routerNavigate = useNavigate();
  const { clientId } = useParams();
  const clientResource = useClients();
  const reclamationResource = useReclamations();
  const planningResource = usePlanning();
  const [query, setQuery] = useState('');
  const [modal, setModal] = useState(null);
  const [form, setForm] = useState(emptyClient);

  const clients = clientResource.clients;
  const visibleClients = useMemo(() => clients.filter(item => `${item.name} ${item.contact} ${item.email}`.toLowerCase().includes(query.toLowerCase())), [clients, query]);
  const selected = clients.find(item => String(item.id) === String(clientId));
  const related = selected ? {
    reclamations: reclamationResource.reclamations.filter(item => String(item.clientId) === String(selected.id) || item.client === selected.name),
    appointments: planningResource.appointments.filter(item => item.client === selected.name)
  } : { reclamations: [], appointments: [] };
  const openReclamations = related.reclamations.filter(item => !['Resolved', 'Closed'].includes(item.status)).length;
  const nextAppointment = related.appointments.find(item => item.status !== 'Completed') || related.appointments[0];
  const latestReclamation = related.reclamations[0];
  const completedAppointments = related.appointments.filter(item => item.status === 'Completed').length;

  function openEdit(client = null) {
    setForm(client || emptyClient);
    setModal(client ? 'edit' : 'new');
  }

  async function submit(event) {
    event.preventDefault();
    try {
      if (modal === 'edit') await clientResource.update(form.technicalId || form.id, form);
      else await clientResource.create(form);
      setModal(null);
      notify(modal === 'edit' ? 'Client updated in backend' : 'Client created in backend');
    } catch (err) {
      notify(getFriendlyApiError(err), 'error');
    }
  }

  if (clientResource.loading) return <section className="page-shell"><Card title="Clients" icon={UserRound}><p>Loading clients from backend...</p></Card></section>;
  if (clientResource.error) return <section className="page-shell"><Card title="Clients" icon={UserRound}><ApiErrorState status={clientResource.errorStatus} message={clientResource.error} onRetry={clientResource.reload} /></Card></section>;

  if (detail) {
    return (
      <section className="page-shell client-admin-detail-page">
        <div className="page-title-row client-detail-title-row">
          <div>
            <span className="eyebrow">Client file</span>
            <h1>Account overview</h1>
            <p>Contact details, service history, open work, and visit planning in one place.</p>
          </div>
          {selected && (
            <div className="page-actions-row client-detail-actions-row">
              <Button icon={ArrowLeft} onClick={() => routerNavigate('/clients')}>Back to Clients</Button>
              <Button variant="primary" icon={Edit} onClick={() => openEdit(selected)}>Edit Client</Button>
            </div>
          )}
        </div>

        {selected ? (
          <div className="client-detail-grid">
            <section className="client-detail-hero card">
              <div className="client-hero-main">
                <span className="client-company-mark"><Building2 size={30} /></span>
                <div>
                  <Badge tone="blue">Client Account</Badge>
                  <h2>{selected.name}</h2>
                  <p>{selected.contact || 'Primary contact'} manages service requests from {selected.location || 'the customer site'}.</p>
                </div>
              </div>
              <div className="client-hero-metrics">
                <Metric icon={ClipboardList} label="Reclamations" value={related.reclamations.length} />
                <Metric icon={CalendarDays} label="Appointments" value={related.appointments.length} />
                <Metric icon={UserRound} label="Open Cases" value={openReclamations} />
              </div>
            </section>

            <div className="client-detail-main">
              <Card title="Contact Information" icon={UserRound} className="client-contact-card">
                <div className="client-contact-grid">
                  <Contact icon={Building2} label="Company" value={selected.name} />
                  <Contact icon={UserRound} label="Primary Contact" value={selected.contact} />
                  <Contact icon={Mail} label="Email" value={selected.email} />
                  <Contact icon={Phone} label="Phone" value={selected.phone} />
                  <Contact icon={MapPin} label="Location" value={selected.location} />
                  <Contact icon={CalendarDays} label="Next Appointment" value={nextAppointment ? `${nextAppointment.date} at ${nextAppointment.start}` : 'Not scheduled'} />
                </div>
              </Card>
              <Card title="Service Snapshot" icon={ClipboardList} className="client-snapshot-card">
                <div className="client-snapshot-grid">
                  <Info label="Open reclamations" value={openReclamations} />
                  <Info label="Last reclamation" value={latestReclamation ? `${latestReclamation.id} - ${latestReclamation.status}` : 'No reclamations'} />
                  <Info label="Next visit" value={nextAppointment ? `${nextAppointment.date} at ${nextAppointment.start}` : 'Not scheduled'} />
                  <Info label="Completed visits" value={completedAppointments} />
                </div>
              </Card>
            </div>

            <div className="client-related-grid">
              <Card title="Reclamations Made By This Client" icon={CalendarDays} className="client-related-card">
                <DataTable rows={related.reclamations} columns={[{ key: 'id', label: 'Reference', render: row => <button type="button" className="table-link" onClick={() => routerNavigate('/reclamations')}>{row.id}</button> }, { key: 'product', label: 'Product' }, { key: 'model', label: 'Model' }, { key: 'priority', label: 'Priority', render: row => <Badge>{row.priority}</Badge> }, { key: 'status', label: 'Status', render: row => <Badge>{row.status}</Badge> }, { key: 'createdShort', label: 'Created At' }]} />
              </Card>
              <Card title="Appointments" icon={CalendarDays} className="client-related-card">
                <DataTable rows={related.appointments} columns={[{ key: 'id', label: 'Appointment' }, { key: 'reclamationId', label: 'Reclamation' }, { key: 'date', label: 'Date' }, { key: 'start', label: 'Start' }, { key: 'technicianName', label: 'Technician' }, { key: 'status', label: 'Status', render: row => <Badge>{row.status}</Badge> }]} />
              </Card>
            </div>
          </div>
        ) : (
          <Card title="Client not found" icon={UserRound} className="placeholder-card"><p>This client does not exist in the backend dataset.</p></Card>
        )}
        {modal && <ClientModal form={form} setForm={setForm} submit={submit} modal={modal} close={() => setModal(null)} />}
      </section>
    );
  }

  return (
    <section className="page-shell clients-page">
      <div className="page-title-row">
        <div>
          <span className="eyebrow">Customer directory</span>
          <h1>Clients</h1>
          <p>Manage customers, requests and appointments.</p>
        </div>
        <div className="page-title-kpis">
          <span><strong>{clients.length}</strong><small>Total clients</small></span>
          <span><strong>{visibleClients.length}</strong><small>Shown now</small></span>
        </div>
        <Button variant="primary" icon={Plus} onClick={() => openEdit()}>Add Client</Button>
      </div>
      <Card title="Clients" icon={UserRound} className="clients-list-card">
        <div className="table-toolbar clients-toolbar"><SearchInput value={query} onChange={setQuery} placeholder="Search clients..." /></div>
        <DataTable rows={visibleClients} onRowClick={row => routerNavigate(`/clients/${row.id}`)} columns={[{ key: 'name', label: 'Client', render: row => <button type="button" className="table-link" onClick={event => { event.stopPropagation(); routerNavigate(`/clients/${row.id}`); }}>{row.name}</button> }, { key: 'contact', label: 'Contact' }, { key: 'email', label: 'Email' }, { key: 'phone', label: 'Phone' }, { key: 'location', label: 'Location' }, { key: 'actions', label: 'Actions', render: row => <span className="table-action-group"><Button size="sm" icon={Eye} onClick={event => { event.stopPropagation(); routerNavigate(`/clients/${row.id}`); }}>View</Button><Button size="sm" icon={Edit} onClick={event => { event.stopPropagation(); openEdit(row); }}>Edit</Button></span> }]} />
      </Card>
      {modal && <ClientModal form={form} setForm={setForm} submit={submit} modal={modal} close={() => setModal(null)} />}
    </section>
  );
}

function ClientModal({ form, setForm, submit, modal, close }) {
  return <Modal title={modal === 'edit' ? 'Edit Client' : 'Add Client'} onClose={close} footer={<Button variant="primary" icon={Plus} onClick={submit}>Save Client</Button>}><form className="form-grid" onSubmit={submit}>{['name', 'contact', 'email', 'phone', 'location'].map(key => <Field label={key[0].toUpperCase() + key.slice(1)} key={key}><input value={form[key] || ''} onChange={event => setForm(current => ({ ...current, [key]: event.target.value }))} /></Field>)}</form></Modal>;
}

function Info({ label, value }) { return <div className="info-pair"><span>{label}</span><strong>{value}</strong></div>; }
function Metric({ icon: Icon, label, value }) { return <div className="client-metric-tile"><span><Icon size={20} /></span><strong>{value}</strong><small>{label}</small></div>; }
function Contact({ icon: Icon, label, value }) { return <div className="client-contact-item"><span><Icon size={18} /></span><div><small>{label}</small><strong>{value || '-'}</strong></div></div>; }
