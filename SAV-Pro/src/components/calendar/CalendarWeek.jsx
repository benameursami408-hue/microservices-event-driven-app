import { ChevronLeft, ChevronRight } from 'lucide-react';
import { Avatar } from '../ui';

const START_HOUR = 5;
const END_HOUR = 18;
const SLOT_HEIGHT = 58;
const hours = ['All-day', ...Array.from({ length: END_HOUR - START_HOUR + 1 }, (_, index) => `${String(START_HOUR + index).padStart(2, '0')}:00`)];
const tonePalette = ['blue', 'teal', 'green', 'orange', 'purple', 'red'];

export function CalendarWeek({ appointments = [], technicians = [], referenceDate = new Date(), onSelect, onMoveWeek }) {
  const weekDays = buildWeekDays(referenceDate);
  const weekStart = weekDays[0].date;
  const dynamicEvents = appointments
    .map(appointment => appointmentToEvent(appointment, weekStart, technicians))
    .filter(Boolean);
  const currentTime = getCurrentTimeMarker(weekStart);

  return (
    <div className="calendar-week">
      <div className="calendar-days">
        <button type="button" className="calendar-week-nav" aria-label="Previous week" onClick={() => onMoveWeek?.(-1)}>
          <ChevronLeft size={16} />
        </button>
        {weekDays.map(day => (
          <strong key={day.key} className={`${day.isToday ? 'today-pill' : ''} ${day.isReference ? 'active-day' : ''}`.trim()}>
            <span>{day.weekday}</span>
            <small>{day.dayNumber}</small>
          </strong>
        ))}
        <button type="button" className="calendar-week-nav" aria-label="Next week" onClick={() => onMoveWeek?.(1)}>
          <ChevronRight size={16} />
        </button>
      </div>

      <div className="calendar-grid" style={{ '--slot-count': END_HOUR - START_HOUR + 1 }}>
        <div className="time-column">
          {hours.map(hour => <span key={hour}>{hour}</span>)}
        </div>
        <div className="day-grid">
          {weekDays.map(day => <div key={day.key} className="day-column" />)}
          {dynamicEvents.map(event => {
            const top = 38 + (event.start - START_HOUR) * SLOT_HEIGHT;
            const height = Math.max(42, (event.end - event.start) * SLOT_HEIGHT - 8);
            return (
              <button
                type="button"
                key={event.id}
                className={`appointment-block block-${event.tone} ${event.status === 'Confirmed' ? 'selected' : ''}`}
                onClick={() => onSelect?.(event.id)}
                style={{
                  '--col': event.day,
                  top: `${top}px`,
                  height: `${height}px`
                }}
              >
                <strong>{event.time}</strong>
                <span>{event.client}</span>
                {event.product && <small>{event.product}</small>}
                {event.tech && <Avatar name={event.techName || event.tech} size="xs" className={`avatar-${event.tone}`} />}
              </button>
            );
          })}
          {currentTime && <div className="current-time-line" style={{ '--current-top': `${currentTime.top}px` }}><span>{currentTime.label}</span></div>}
        </div>
      </div>
    </div>
  );
}

function getCurrentTimeMarker(weekStart) {
  const now = new Date();
  const day = Math.round((dateWithoutTime(now) - dateWithoutTime(weekStart)) / 86400000);
  const decimal = now.getHours() + now.getMinutes() / 60;
  if (day < 0 || day > 6 || decimal < START_HOUR || decimal > END_HOUR + 1) return null;
  return {
    top: 38 + (decimal - START_HOUR) * SLOT_HEIGHT,
    label: `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`
  };
}

export function getTechnicianTone(technicianId, technicians = [], fallback = '') {
  const technicianIndex = technicians.findIndex(tech => String(tech.id) === String(technicianId) || tech.name === technicianId);
  if (technicianIndex >= 0) return tonePalette[technicianIndex % tonePalette.length];
  const text = String(technicianId || fallback || 'unassigned');
  const hash = Array.from(text).reduce((acc, char) => acc + char.charCodeAt(0), 0);
  return tonePalette[hash % tonePalette.length];
}

export function buildWeekDays(referenceDate = new Date()) {
  const reference = coerceDate(referenceDate);
  const start = new Date(reference);
  start.setDate(reference.getDate() - reference.getDay());
  start.setHours(0, 0, 0, 0);
  const todayKey = toDateKey(new Date());
  const referenceKey = toDateKey(reference);

  return Array.from({ length: 7 }, (_, index) => {
    const date = new Date(start);
    date.setDate(start.getDate() + index);
    const key = toDateKey(date);
    return {
      date,
      key,
      weekday: date.toLocaleDateString('en-US', { weekday: 'short' }),
      dayNumber: date.toLocaleDateString('en-US', { day: '2-digit' }),
      isToday: key === todayKey,
      isReference: key === referenceKey
    };
  });
}

function appointmentToEvent(appointment, weekStart, technicians) {
  const date = coerceDate(appointment.date);
  const day = Math.round((dateWithoutTime(date) - dateWithoutTime(weekStart)) / 86400000);
  if (day < 0 || day > 6) return null;

  const [startHour = START_HOUR, startMinute = 0] = String(appointment.start || '09:00').split(':').map(Number);
  const [endHour = startHour + 1, endMinute = 0] = String(appointment.end || '10:00').split(':').map(Number);
  const start = Math.max(START_HOUR, startHour + startMinute / 60);
  const end = Math.min(END_HOUR + 1, endHour + endMinute / 60);
  const technicianName = appointment.technicianName || appointment.technicianId || '';
  const tone = getTechnicianTone(appointment.technicianId || technicianName, technicians, appointment.id);

  return {
    id: appointment.id || `${appointment.client}-${appointment.start}-${appointment.end}`,
    day,
    start,
    end: Math.max(end, start + 0.75),
    tone,
    time: `${appointment.start || '--:--'} - ${appointment.end || '--:--'}`,
    client: appointment.client,
    product: appointment.product,
    tech: appointment.technicianId || technicianName,
    techName: technicianName,
    status: appointment.status
  };
}

function coerceDate(value) {
  const date = value instanceof Date ? new Date(value) : new Date(value || Date.now());
  return Number.isNaN(date.getTime()) ? new Date() : date;
}

function dateWithoutTime(date) {
  const copy = new Date(date);
  copy.setHours(0, 0, 0, 0);
  return copy;
}

function toDateKey(date) {
  const copy = dateWithoutTime(coerceDate(date));
  return `${copy.getFullYear()}-${String(copy.getMonth() + 1).padStart(2, '0')}-${String(copy.getDate()).padStart(2, '0')}`;
}
