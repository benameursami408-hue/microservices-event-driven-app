import {
  BookOpen,
  CalendarDays,
  CheckCircle,
  Clock,
  FileText,
  Filter,
  Plus,
  RefreshCw,
  Send,
  Wrench
} from 'lucide-react';
import { useMemo, useState } from 'react';
import { Badge, Button, Card, DataTable, Field, Modal, NotificationItem, SearchInput } from '../components/ui';
import { useNotifications } from '../hooks/useNotifications';
import { usePlanning } from '../hooks/usePlanning';
import { useReclamations } from '../hooks/useReclamations';
import { getFriendlyApiError } from '../utils/errorMessages';

const emptyForm = { product: '', model: '', serial: '', priority: 'Medium', description: '', site: '' };
const workflowSteps = ['Open', 'Assigned', 'Planned', 'In Progress', 'Resolved', 'Closed'];

function statusIndex(status) {
  return Math.max(workflowSteps.indexOf(status), 0);
}

function sortAppointments(rows) {
  return [...rows].sort((left, right) => {
    const leftKey = `${left.date || ''} ${left.start || ''}`;
    const rightKey = `${right.date || ''} ${right.start || ''}`;
    return leftKey.localeCompare(rightKey);
  });
}

export function ClientPortalPage({ user, notify, navigate, mode = 'home' }) {
  const reclamationResource = useReclamations();
  const planning = usePlanning();
  const notificationResource = useNotifications(50);
  const [selectedId, setSelectedId] = useState('');
  const [query, setQuery] = useState('');
  const [modal, setModal] = useState(null);
  const [form, setForm] = useState({ ...emptyForm, client: user?.company || user?.name || '' });

  const clientName = user?.company || user?.firstName || user?.name || 'Client';
  const requests = useMemo(
    () => reclamationResource.reclamations.filter(row => (
      String(row.clientId) === String(user?.clientId || user?.id)
      || row.client === user?.company
      || row.email === user?.email
    )),
    [reclamationResource.reclamations, user]
  );
  const visibleRequests = useMemo(
    () => requests.filter(request => `${request.id} ${request.product} ${request.client}`.toLowerCase().includes(query.toLowerCase())),
    [requests, query]
  );
  const selected = requests.find(item => item.id === selectedId) || requests[0];
  const appointments = useMemo(
    () => sortAppointments(planning.appointments.filter(row => requests.some(req => req.technicalId === row.reclamationId || req.id === row.reclamationId))),
    [planning.appointments, requests]
  );
  const notifications = notificationResource.notifications;
  const resolvedCount = requests.filter(item => ['Resolved', 'Closed'].includes(item.status)).length;
  const activeCount = requests.filter(item => !['Resolved', 'Closed'].includes(item.status)).length;
  const nextAppointment = appointments.find(item => !['Completed', 'Cancelled'].includes(item.status)) || appointments[0];
  const loadError = reclamationResource.error || planning.error || notificationResource.error;

  async function submitRequest(event) {
    event.preventDefault();
    if (!form.product || !form.description) {
      notify('Product and description are required.', 'error');
      return;
    }

    try {
      const reclamation = await reclamationResource.create({ ...form, client: user?.company || user?.name || form.client });
      setSelectedId(reclamation.id);
      setModal(null);
      setForm({ ...emptyForm, client: user?.company || user?.name || '' });
      notify('Reclamation submitted to backend');
    } catch (err) {
      notify(getFriendlyApiError(err), 'error');
    }
  }

  async function markAllRead() {
    try {
      await notificationResource.markAllRead();
      notify('Notifications marked as read');
    } catch (err) {
      notify(getFriendlyApiError(err), 'error');
    }
  }

  async function requestReschedule(nextAppointment) {
    try {
      await planning.rescheduleAppointment(nextAppointment.technicalId || nextAppointment.id, { ...nextAppointment, reason: 'Client requested reschedule' });
      notify('Reschedule request sent to backend');
    } catch (err) {
      notify(getFriendlyApiError(err), 'error');
    }
  }

  function retryLoad() {
    reclamationResource.reload();
    planning.reload();
    notificationResource.reload();
  }

  if (mode === 'knowledge') {
    return (
      <SimpleClientPage title="Knowledge Base" description="Self-service resources and warranty documentation.">
        <div className="knowledge-card">
          <BookOpen size={24} />
          <strong>Maintenance Guide</strong>
          <p>Documentation can be connected from backend CMS later.</p>
        </div>
        <div className="knowledge-card">
          <Wrench size={24} />
          <strong>Before the technician arrives</strong>
          <p>Prepare access to the equipment and serial number.</p>
        </div>
      </SimpleClientPage>
    );
  }

  if (mode === 'appointments') {
    return (
      <main className="page-shell client-appointments-page">
        <ClientHero
          title="My Appointments"
          description="Confirmed and proposed service visits, organized by date."
          primaryAction={<Button variant="primary" icon={Plus} onClick={() => navigate?.('client')}>New Request</Button>}
          metrics={[
            { label: 'Scheduled', value: appointments.length, icon: CalendarDays },
            { label: 'Next Visit', value: nextAppointment ? nextAppointment.date : 'None', icon: Clock },
            { label: 'Active Cases', value: activeCount, icon: FileText }
          ]}
        />

        {loadError && <LoadErrorCard message={loadError} onRetry={retryLoad} />}

        <Card title="Appointment Schedule" icon={CalendarDays} className="client-appointments-board">
          <div className="client-appointment-list">
            {appointments.length ? appointments.map(item => (
              <AppointmentCard key={item.id} appointment={item} onReschedule={() => requestReschedule(item)} />
            )) : <EmptyClientState title="No appointments planned" text="Your scheduled service visits will appear here once a planning request is confirmed." />}
          </div>
        </Card>
      </main>
    );
  }

  return (
    <main className="page-shell client-portal-page">
      <ClientHero
        title={`Welcome, ${clientName}`}
        description="Track service requests, planned visits and important updates in one ordered workspace."
        primaryAction={<Button variant="primary" icon={Plus} onClick={() => setModal('new')}>New Request</Button>}
        metrics={[
          { label: 'Requests', value: requests.length, icon: FileText },
          { label: 'Appointments', value: appointments.length, icon: CalendarDays },
          { label: 'Resolved', value: resolvedCount, icon: CheckCircle }
        ]}
      />

      {loadError && <LoadErrorCard message={loadError} onRetry={retryLoad} />}

      <div className="client-dashboard-grid">
        <Card
          title="My Reclamations"
          icon={FileText}
          className="client-requests-card"
          actions={<Button icon={Filter} onClick={() => notify('Filters applied to backend rows')}>Filter</Button>}
        >
          <div className="client-card-toolbar">
            <SearchInput value={query} onChange={setQuery} placeholder="Search your requests..." />
            <span>{visibleRequests.length} shown</span>
          </div>
          <DataTable
            rows={visibleRequests}
            selectedId={selected?.id}
            onRowClick={row => setSelectedId(row.id)}
            columns={[
              { key: 'id', label: 'Reference' },
              { key: 'product', label: 'Product' },
              { key: 'priority', label: 'Priority', render: row => <Badge>{row.priority}</Badge> },
              { key: 'status', label: 'Status', render: row => <Badge>{row.status}</Badge> },
              { key: 'createdShort', label: 'Created' }
            ]}
          />
        </Card>

        <div className="client-side-stack">
          <RequestDetails request={selected} />

          <Card
            title="Notifications"
            icon={Send}
            className="client-notification-card"
            actions={<button type="button" className="link-button" onClick={markAllRead}>Mark all read</button>}
          >
            {notifications.slice(0, 4).map(item => <NotificationItem key={item.id} item={{ ...item, unread: !item.read }} compact />)}
            {!notifications.length && <EmptyClientState title="No notifications" text="Important updates will appear here." compact />}
          </Card>
        </div>
      </div>

      {modal === 'new' && (
        <Modal
          title="New Service Request"
          onClose={() => setModal(null)}
          footer={(
            <>
              <Button onClick={() => setModal(null)}>Cancel</Button>
              <Button variant="primary" icon={Plus} onClick={submitRequest}>Submit Request</Button>
            </>
          )}
        >
          <form className="form-grid" onSubmit={submitRequest}>
            <Field label="Product"><input value={form.product} onChange={event => setForm(current => ({ ...current, product: event.target.value }))} /></Field>
            <Field label="Model"><input value={form.model} onChange={event => setForm(current => ({ ...current, model: event.target.value }))} /></Field>
            <Field label="Serial"><input value={form.serial} onChange={event => setForm(current => ({ ...current, serial: event.target.value }))} /></Field>
            <Field label="Priority">
              <select value={form.priority} onChange={event => setForm(current => ({ ...current, priority: event.target.value }))}>
                <option>Low</option>
                <option>Medium</option>
                <option>High</option>
                <option>Urgent</option>
              </select>
            </Field>
            <Field label="Site"><input value={form.site} onChange={event => setForm(current => ({ ...current, site: event.target.value }))} /></Field>
            <Field label="Description"><textarea value={form.description} onChange={event => setForm(current => ({ ...current, description: event.target.value }))} /></Field>
          </form>
        </Modal>
      )}
    </main>
  );
}

function ClientHero({ title, description, metrics, primaryAction }) {
  return (
    <section className="client-portal-hero">
      <div className="client-hero-copy">
        <span>Client Portal</span>
        <h1>{title}</h1>
        <p>{description}</p>
      </div>
      <div className="client-hero-panel">
        <div className="client-summary-grid">
          {metrics.map(metric => {
            const Icon = metric.icon;
            return (
              <div className="client-summary-tile" key={metric.label}>
                <span><Icon size={18} /></span>
                <small>{metric.label}</small>
                <strong>{metric.value}</strong>
              </div>
            );
          })}
        </div>
        {primaryAction}
      </div>
    </section>
  );
}

function LoadErrorCard({ message, onRetry }) {
  return (
    <Card className="client-load-error">
      <p>{message}</p>
      <Button icon={RefreshCw} onClick={onRetry}>Retry</Button>
    </Card>
  );
}

function RequestDetails({ request }) {
  if (!request) {
    return (
      <Card title="Request Details" icon={Wrench} className="client-detail-summary-card">
        <EmptyClientState title="No request selected" text="Select a reclamation to view its status, SLA and product details." compact />
      </Card>
    );
  }

  const currentStep = statusIndex(request.status);

  return (
    <Card title="Request Details" icon={Wrench} className="client-detail-summary-card">
      <div className="client-detail-content">
        <div className="client-status-stepper">
          {workflowSteps.map((step, index) => (
            <span key={step} className={index <= currentStep ? 'done' : ''}>{step}</span>
          ))}
        </div>
        <div className="client-request-heading">
          <span>{request.id}</span>
          <Badge>{request.status}</Badge>
        </div>
        <h2>{request.product}</h2>
        <p>{request.description}</p>
        <div className="client-detail-meta">
          <span><small>Priority</small><Badge>{request.priority}</Badge></span>
          <span><small>SLA</small><strong>{request.sla}</strong></span>
          <span><small>Created</small><strong>{request.createdShort || '-'}</strong></span>
        </div>
      </div>
    </Card>
  );
}

function AppointmentCard({ appointment, onReschedule }) {
  return (
    <button type="button" className="client-appointment-card" onClick={onReschedule}>
      <time>
        <strong>{appointment.date || 'Date pending'}</strong>
        <span>{appointment.start || '--:--'} - {appointment.end || '--:--'}</span>
      </time>
      <div>
        <div className="client-appointment-head">
          <strong>{appointment.id}</strong>
          <Badge>{appointment.status}</Badge>
        </div>
        <p>{appointment.reclamationId ? `Request ${appointment.reclamationId}` : 'Service visit'}</p>
        <span className="request-line"><Wrench size={15} />{appointment.technicianName || 'Technician pending'}</span>
      </div>
      <span className="client-appointment-action">Request reschedule</span>
    </button>
  );
}

function EmptyClientState({ title, text, compact = false }) {
  return (
    <div className={`client-empty-state ${compact ? 'compact' : ''}`}>
      <strong>{title}</strong>
      <p>{text}</p>
    </div>
  );
}

function SimpleClientPage({ title, description, children }) {
  return (
    <main className="page-shell client-simple-page">
      <ClientHero
        title={title}
        description={description}
        metrics={[
          { label: 'Resources', value: 2, icon: BookOpen },
          { label: 'Coverage', value: '24/7', icon: CheckCircle },
          { label: 'Support', value: 'Ready', icon: Wrench }
        ]}
      />
      <Card title={title} icon={FileText} className="client-simple-card">
        <div className="request-card-list">{children}</div>
      </Card>
    </main>
  );
}
