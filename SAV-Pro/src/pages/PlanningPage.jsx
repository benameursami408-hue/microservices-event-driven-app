import {
  CalendarDays,
  Check,
  ChevronLeft,
  ChevronRight,
  Clock,
  ListChecks,
  Loader2,
  MapPin,
  Phone,
  Plus,
  Send,
  UserPlus,
  Wrench
} from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { CalendarWeek, getTechnicianTone } from '../components/calendar/CalendarWeek';
import { ApiErrorState } from '../components/common/ApiErrorState';
import { Avatar, Badge, Button, Card, Field, IconButton, Modal } from '../components/ui';
import { usePlanning } from '../hooks/usePlanning';
import { useUsers } from '../hooks/useUsers';
import { getFriendlyApiError } from '../utils/errorMessages';

const requestTabs = ['Pending', 'Proposed'];
const calendarViews = ['week', 'month', 'agenda'];

export function PlanningPage({ notify }) {
  const planning = usePlanning();
  const technicianResource = useUsers('Technician');
  const planningRequests = planning.planningRequests;
  const appointments = planning.appointments;
  const technicians = technicianResource.users;
  const [filter, setFilter] = useState('Pending');
  const [requestQuery, setRequestQuery] = useState('');
  const [selectedRequestId, setSelectedRequestId] = useState('');
  const [selectedAppointmentId, setSelectedAppointmentId] = useState('');
  const [calendarFocused, setCalendarFocused] = useState(false);
  const [calendarView, setCalendarView] = useState('week');
  const [calendarDate, setCalendarDate] = useState(new Date());
  const [modal, setModal] = useState(null);
  const [form, setForm] = useState({ date: formatInputDate(new Date()), start: '09:00', end: '10:30', technicianId: '', status: 'Proposed', note: '' });
  const [errors, setErrors] = useState({});

  const visibleRequests = useMemo(() => planningRequests.filter(row => {
    const matchesTab = row.status === filter;
    const haystack = `${row.id} ${row.client} ${row.product} ${row.location} ${row.priority} ${row.status}`.toLowerCase();
    return matchesTab && haystack.includes(requestQuery.toLowerCase());
  }), [planningRequests, filter, requestQuery]);
  const selectedRequest = planningRequests.find(item => item.id === selectedRequestId) || visibleRequests[0] || planningRequests[0];
  const selectedAppointment = appointments.find(item => item.id === selectedAppointmentId) || appointments[0];
  const sortedAppointments = useMemo(() => sortAppointments(appointments), [appointments]);
  const activeTechnicians = useMemo(() => technicians.filter(tech => appointments.some(item => String(item.technicianId) === String(tech.id)) || technicians.length <= 6), [appointments, technicians]);

  useEffect(() => {
    if (!selectedRequestId && planningRequests[0]) setSelectedRequestId(planningRequests[0].id);
  }, [planningRequests, selectedRequestId]);

  useEffect(() => {
    if (!selectedAppointmentId && appointments[0]) setSelectedAppointmentId(appointments[0].id);
  }, [appointments, selectedAppointmentId]);

  function openAppointmentModal() {
    setForm(current => ({
      ...current,
      date: formatInputDate(calendarDate),
      requestId: selectedRequest?.id || '',
      technicalRequestId: selectedRequest?.technicalId || '',
      technicianId: technicians[0]?.id || ''
    }));
    setErrors({});
    setModal('appointment');
  }

  async function submitAppointment(event) {
    event.preventDefault();
    const nextErrors = {
      requestId: form.requestId ? '' : 'Planning request is required.',
      date: form.date ? '' : 'Date is required.',
      start: form.start ? '' : 'Start time is required.',
      end: form.end ? '' : 'End time is required.',
      technicianId: form.technicianId ? '' : 'Technician is required.'
    };
    setErrors(nextErrors);
    if (Object.values(nextErrors).some(Boolean)) return;
    try {
      const request = planningRequests.find(row => row.id === form.requestId) || selectedRequest;
      const appointment = await planning.createAppointment({ ...form, technicalRequestId: request?.technicalId || form.technicalRequestId });
      if (form.technicianId) {
        const tech = technicians.find(row => String(row.id) === String(form.technicianId));
        await planning.assignTechnician(appointment.technicalId || appointment.id, tech);
      }
      setSelectedAppointmentId(appointment.id);
      setModal(null);
      notify('Appointment created successfully');
    } catch (err) {
      notify(getFriendlyApiError(err), 'error');
    }
  }

  async function changeStatus(status) {
    if (!selectedAppointment) return;
    try {
      if (status === 'Confirmed') await planning.confirmAppointment(selectedAppointment.technicalId || selectedAppointment.id);
      else if (status === 'Cancelled') await planning.cancelAppointment(selectedAppointment.technicalId || selectedAppointment.id, 'Cancelled from SAV Pro UI');
      else notify(`${status} requires its dedicated backend workflow`, 'error');
      notify(`Appointment marked ${status}`);
    } catch (err) {
      notify(getFriendlyApiError(err), 'error');
    }
  }

  async function submitReschedule(event) {
    event.preventDefault();
    try {
      await planning.rescheduleAppointment(selectedAppointment.technicalId || selectedAppointment.id, form);
      setModal(null);
      notify('Appointment rescheduled');
    } catch (err) {
      notify(getFriendlyApiError(err), 'error');
    }
  }

  function publishNotification() {
    notify('Notifications are now generated by backend events after appointment operations.');
  }

  async function assignTechnician(technicianId) {
    const technician = technicians.find(item => String(item.id) === String(technicianId));
    if (!selectedAppointment) return;
    try {
      await planning.assignTechnician(selectedAppointment.technicalId || selectedAppointment.id, technician);
      setModal(null);
      notify('Technician assigned to appointment');
    } catch (err) {
      notify(getFriendlyApiError(err), 'error');
    }
  }

  function moveCalendar(direction) {
    setCalendarDate(current => {
      const next = new Date(current);
      if (calendarView === 'month') next.setMonth(next.getMonth() + direction);
      else next.setDate(next.getDate() + direction * 7);
      return next;
    });
  }

  const loadError = planning.error || technicianResource.error;
  const loadErrorStatus = planning.errorStatus || technicianResource.errorStatus;
  const loading = planning.loading || technicianResource.loading;
  const modals = (
    <>
      {modal === 'appointment' && <AppointmentModal title="New Appointment" planningRequests={planningRequests} technicians={technicians} form={form} setForm={setForm} errors={errors} onClose={() => setModal(null)} onSubmit={submitAppointment} />}
      {modal === 'reschedule' && <AppointmentModal title="Reschedule Appointment" planningRequests={planningRequests} technicians={technicians} form={form} setForm={setForm} errors={errors} onClose={() => setModal(null)} onSubmit={submitReschedule} hideRequest />}
      {modal === 'assign' && (
        <Modal title="Assign Technician" onClose={() => setModal(null)}>
          <div className="request-card-list">
            {technicians.map((technician, index) => (
              <button type="button" key={technician.id} className="planning-request-card" onClick={() => assignTechnician(technician.id)}>
                <div className="request-card-head">
                  <strong>{technician.name}</strong>
                  <Avatar name={technician.name} size="sm" className={`avatar-${getTechnicianTone(technician.id, technicians, index)}`} />
                </div>
                <RequestLine icon={Wrench}>{technician.role}</RequestLine>
                <RequestLine icon={Phone}>{technician.email}</RequestLine>
              </button>
            ))}
          </div>
        </Modal>
      )}
    </>
  );

  return (
    <section className={`page-shell planning-page planning-workspace-page ${calendarFocused ? 'calendar-focus-mode' : ''}`}>
      <div className="page-title-row planning-title-row">
        <div>
          <span className="eyebrow">Planning workspace</span>
          <h1>Planning Center</h1>
          <p>Prioritize requests, confirm technicians, and open the calendar when you need schedule-level detail.</p>
        </div>
        <div className="page-actions-row">
          <Button icon={CalendarDays} onClick={() => setCalendarFocused(current => !current)}>{calendarFocused ? 'Exit Focus' : 'Focus Calendar'}</Button>
          <Button variant="primary" icon={Plus} onClick={openAppointmentModal}>New Appointment</Button>
        </div>
      </div>

      {loadError && <Card><ApiErrorState status={loadErrorStatus} message={loadError} onRetry={() => { planning.reload(); technicianResource.reload(); }} /></Card>}

      <div className={`planning-workspace-layout planning-command-layout ${calendarFocused ? 'is-calendar-focused' : ''}`}>
        <Card className="planning-requests-panel planning-queue-panel" title="Request Queue" icon={ListChecks} actions={<Badge tone="count">{visibleRequests.length}/{planningRequests.length}</Badge>}>
          <div className="planning-queue-summary">
            <span><strong>{planningRequests.filter(item => item.status === 'Pending').length}</strong> Pending</span>
            <span><strong>{planningRequests.filter(item => item.status === 'Proposed').length}</strong> Proposed</span>
          </div>
          <div className="planning-queue-toolbar">
            <input value={requestQuery} onChange={event => setRequestQuery(event.target.value)} placeholder="Search client, request, priority..." />
          </div>
          <div className="mini-tabs planning-queue-tabs">
            {requestTabs.map(tab => (
              <button type="button" key={tab} className={filter === tab ? 'active' : ''} onClick={() => setFilter(tab)}>
                {tab}
                <span>{planningRequests.filter(item => item.status === tab).length}</span>
              </button>
            ))}
          </div>
          <div className="planning-request-queue">
            {loading && <div className="empty-state"><Loader2 size={18} className="spin" /> Loading planning data...</div>}
            {!loading && visibleRequests.map(request => (
              <PlanningRequestQueueItem key={request.id} request={request} selected={selectedRequest?.id === request.id} onSelect={() => setSelectedRequestId(request.id)} />
            ))}
            {!loading && !visibleRequests.length && <div className="empty-panel compact">No requests match this view.</div>}
          </div>
          <div className="panel-footer">Queue prioritized by status, date and priority.</div>
        </Card>

        <div className="planning-center-stack planning-calendar-stack">
          <Card className="planning-schedule-card calendar-panel">
            <div className="calendar-toolbar planning-schedule-toolbar">
              <div className="segmented">
                {calendarViews.map(view => (
                  <button type="button" key={view} className={calendarView === view ? 'active' : ''} onClick={() => setCalendarView(view)}>
                    {view[0].toUpperCase() + view.slice(1)}
                  </button>
                ))}
              </div>
              <div className="calendar-nav">
                <IconButton icon={ChevronLeft} label="Previous period" onClick={() => moveCalendar(-1)} />
                <Button size="sm" onClick={() => setCalendarDate(new Date())}>Today</Button>
              <IconButton icon={ChevronRight} label="Next period" onClick={() => moveCalendar(1)} />
            </div>
            <h2>{formatCalendarTitle(calendarDate, calendarView)}</h2>
          </div>

            <TechnicianAvailability technicians={activeTechnicians} allTechnicians={technicians} compact />

            {calendarView === 'week' && <CalendarWeek appointments={appointments} technicians={technicians} referenceDate={calendarDate} onSelect={setSelectedAppointmentId} onMoveWeek={moveCalendar} />}
            {calendarView === 'month' && <CalendarMonth appointments={appointments} technicians={technicians} referenceDate={calendarDate} onSelectDate={date => { setCalendarDate(date); setCalendarView('week'); }} />}
            {calendarView === 'agenda' && <CalendarAgenda appointments={sortedAppointments} technicians={technicians} onSelect={id => setSelectedAppointmentId(id)} />}
          </Card>

        </div>

        {selectedAppointment && <AppointmentDetails appointment={selectedAppointment} technicians={technicians} onAssign={() => setModal('assign')} onReschedule={() => { setForm({ ...form, date: selectedAppointment.date, start: selectedAppointment.start, end: selectedAppointment.end }); setModal('reschedule'); }} onStatus={changeStatus} onPublish={publishNotification} />}
      </div>
      {modals}
    </section>
  );
}

function TechnicianAvailability({ technicians, allTechnicians = technicians, compact = false }) {
  return (
    <section className={`availability-strip ${compact ? 'compact' : ''}`}>
      <div className="availability-strip-head">
        <h3>Technician Availability</h3>
        <span>{technicians.length} active</span>
      </div>
      <div className="availability-list">
        {technicians.map((tech, index) => {
          const tone = getTechnicianTone(tech.id, allTechnicians, index);
          return (
            <div className={`availability-chip tech-tone-${tone}`} key={tech.id}>
              <Avatar name={tech.name} size="md" className={`avatar-${tone}`} />
              <span><strong>{tech.name}</strong><small>{tech.email}</small></span>
              <em>{availabilityLabel(index)}</em>
            </div>
          );
        })}
      </div>
    </section>
  );
}

function PlanningStat({ label, value, icon: Icon, tone }) {
  return (
    <Card className={`planning-stat-card planning-stat-${tone}`}>
      <span><Icon size={20} /></span>
      <div>
        <strong>{value}</strong>
        <small>{label}</small>
      </div>
    </Card>
  );
}

function PlanningRequestQueueItem({ request, selected, onSelect }) {
  return (
    <button type="button" className={`planning-queue-item ${selected ? 'active' : ''}`} onClick={onSelect}>
      <time>{formatQueueDate(request.date)}</time>
      <div className="planning-queue-copy">
        <div className="planning-queue-head">
          <strong>{request.id}</strong>
          <span className="planning-queue-badges">
            <Badge>{request.priority}</Badge>
            <Badge>{request.status}</Badge>
          </span>
        </div>
        <span>{request.client}</span>
        <small>{request.location || request.product}</small>
      </div>
    </button>
  );
}

function AppointmentDetails({ appointment, technicians, onAssign, onReschedule, onStatus, onPublish, compact = false }) {
  const tone = getTechnicianTone(appointment.technicianId || appointment.technicianName, technicians, appointment.id);
  const statusSteps = ['Proposed', 'Confirmed', 'Rescheduled', 'Completed'];
  const statusIndex = Math.max(statusSteps.indexOf(appointment.status), 0);
  return (
    <Card className={`appointment-details-panel planning-inspector-panel ${compact ? 'compact' : ''}`} title="Appointment Details">
      <div className="appointment-detail-body">
        <div className="appointment-id-row"><Badge>{appointment.status}</Badge><strong>#{appointment.id}</strong></div>
        <div className="appointment-status-track">
          {statusSteps.map((step, index) => <span key={step} className={index <= statusIndex ? 'done' : ''}>{step}</span>)}
        </div>
        <h2>{appointment.client}</h2>
        <p>{appointment.product}</p>
        <div className="detail-list"><RequestLine icon={CalendarDays}>{appointment.date}</RequestLine><RequestLine icon={Clock}>{appointment.start} - {appointment.end}</RequestLine><RequestLine icon={Clock}>Duration <strong>{appointment.duration}</strong></RequestLine></div>
        <section className="detail-section"><h3>Client</h3><p>{appointment.client}</p><RequestLine icon={Phone}>Backend contact</RequestLine></section>
        <section className="detail-section"><h3>Service Address</h3><p>{appointment.location}</p><button type="button" className="link-button"><MapPin size={15} /> View on map</button></section>
        <section className={`technician-mini-card tech-tone-${tone}`}><Avatar name={appointment.technicianName || 'Unassigned'} size="lg" className={`avatar-${tone}`} /><div><strong>{appointment.technicianName || 'Unassigned'}</strong><span>{availabilityLabel(technicians.findIndex(tech => String(tech.id) === String(appointment.technicianId)))}</span></div><Badge tone="success">Ready</Badge></section>
        <section className="detail-section note"><h3>Planning Note</h3><p>{appointment.note || 'No planning note yet.'}</p></section>
      </div>
      <div className="appointment-actions planning-inspector-actions"><Button size="sm" icon={Check} variant="success" onClick={() => onStatus('Confirmed')}>Confirm</Button><Button size="sm" icon={UserPlus} onClick={onAssign}>Assign</Button><Button size="sm" icon={CalendarDays} onClick={onReschedule}>Reschedule</Button><Button size="sm" variant="primary" icon={Send} rightIcon={ChevronRight} onClick={onPublish}>Notify</Button></div>
    </Card>
  );
}

function CalendarMonth({ appointments, technicians, referenceDate, onSelectDate }) {
  const days = getMonthDays(referenceDate);
  const appointmentMap = appointments.reduce((acc, appointment) => {
    const key = appointment.date;
    acc[key] = [...(acc[key] || []), appointment];
    return acc;
  }, {});

  return (
    <div className="calendar-month-view">
      {['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'].map(day => <strong key={day}>{day}</strong>)}
      {days.map(day => {
        const rows = appointmentMap[formatInputDate(day.date)] || [];
        return (
          <button type="button" key={day.key} className={`${day.inMonth ? '' : 'muted'} ${day.isToday ? 'today' : ''}`} onClick={() => onSelectDate(day.date)}>
            <span>{day.date.getDate()}</span>
            <div>
              {rows.slice(0, 3).map(item => <i key={item.id} className={`month-event-dot dot-${getTechnicianTone(item.technicianId, technicians, item.id)}`}>{item.start} {item.client}</i>)}
              {rows.length > 3 && <small>+{rows.length - 3} more</small>}
            </div>
          </button>
        );
      })}
    </div>
  );
}

function CalendarAgenda({ appointments, technicians, onSelect }) {
  return (
    <div className="planning-agenda-list">
      {appointments.map(item => <AgendaItem key={item.id} appointment={item} technicians={technicians} onSelect={() => onSelect(item.id)} />)}
      {!appointments.length && <div className="empty-panel">No appointments match the current calendar.</div>}
    </div>
  );
}

function AgendaItem({ appointment, technicians, onSelect }) {
  const tone = getTechnicianTone(appointment.technicianId || appointment.technicianName, technicians, appointment.id);
  return (
    <button type="button" className={`agenda-item tech-tone-${tone}`} onClick={onSelect}>
      <span className={`agenda-tone-dot dot-${tone}`} />
      <div>
        <strong>{appointment.client}</strong>
        <small>{appointment.product}</small>
      </div>
      <time>{appointment.date} | {appointment.start} - {appointment.end}</time>
      <Badge>{appointment.status}</Badge>
    </button>
  );
}

function AppointmentModal({ title, planningRequests, technicians, form, setForm, errors, onClose, onSubmit, hideRequest = false }) {
  return <Modal title={title} onClose={onClose} footer={<><Button onClick={onClose}>Cancel</Button><Button variant="primary" icon={CalendarDays} onClick={onSubmit}>Save Appointment</Button></>}><form className="form-grid" onSubmit={onSubmit}>{!hideRequest && <Field label="Planning Request" error={errors.requestId}><select value={form.requestId || ''} onChange={event => setForm(current => ({ ...current, requestId: event.target.value }))}><option value="">Select request</option>{planningRequests.map(request => <option key={request.id} value={request.id}>{request.id} - {request.client}</option>)}</select></Field>}<Field label="Technician" error={errors.technicianId}><select value={form.technicianId || ''} onChange={event => setForm(current => ({ ...current, technicianId: event.target.value }))}><option value="">Select technician</option>{technicians.map(technician => <option key={technician.id} value={technician.id}>{technician.name}</option>)}</select></Field><Field label="Date" error={errors.date}><input type="date" value={form.date || ''} onChange={event => setForm(current => ({ ...current, date: event.target.value }))} /></Field><Field label="Start" error={errors.start}><input type="time" value={form.start || ''} onChange={event => setForm(current => ({ ...current, start: event.target.value }))} /></Field><Field label="End" error={errors.end}><input type="time" value={form.end || ''} onChange={event => setForm(current => ({ ...current, end: event.target.value }))} /></Field><Field label="Status"><select value={form.status || 'Proposed'} onChange={event => setForm(current => ({ ...current, status: event.target.value }))}><option>Proposed</option><option>Confirmed</option><option>Rescheduled</option><option>Completed</option></select></Field><Field label="Planning Note"><textarea value={form.note || ''} onChange={event => setForm(current => ({ ...current, note: event.target.value }))} /></Field></form></Modal>;
}

function RequestLine({ icon: Icon, children }) { return <span className="request-line"><Icon size={15} />{children}</span>; }

function sortAppointments(rows) {
  return [...rows].sort((a, b) => new Date(`${a.date}T${a.start || '00:00'}`) - new Date(`${b.date}T${b.start || '00:00'}`));
}

function formatInputDate(value) {
  const date = value instanceof Date ? value : new Date(value || Date.now());
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}

function formatQueueDate(value) {
  const date = new Date(value || Date.now());
  if (Number.isNaN(date.getTime())) return 'Date';
  return date.toLocaleDateString('en-US', { month: 'short', day: '2-digit' });
}

function formatCalendarTitle(date, view) {
  if (view === 'month') return date.toLocaleDateString('en-US', { month: 'long', year: 'numeric' });
  if (view === 'agenda') return `Agenda from ${date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}`;
  const start = new Date(date);
  start.setDate(date.getDate() - date.getDay());
  const end = new Date(start);
  end.setDate(start.getDate() + 6);
  return `${start.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} - ${end.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}`;
}

function getMonthDays(referenceDate) {
  const first = new Date(referenceDate.getFullYear(), referenceDate.getMonth(), 1);
  const start = new Date(first);
  start.setDate(1 - first.getDay());
  const today = formatInputDate(new Date());
  return Array.from({ length: 42 }, (_, index) => {
    const date = new Date(start);
    date.setDate(start.getDate() + index);
    const key = formatInputDate(date);
    return {
      date,
      key,
      inMonth: date.getMonth() === referenceDate.getMonth(),
      isToday: key === today
    };
  });
}

function availabilityLabel(index) {
  const labels = ['Available', 'On duty', 'Field visit', 'Standby', 'Remote', 'Busy'];
  return labels[Math.max(0, index) % labels.length];
}
