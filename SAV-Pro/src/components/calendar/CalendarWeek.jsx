import { ChevronRight } from 'lucide-react';
import { Avatar } from '../ui';

const days = ['Sun 12', 'Mon 13', 'Tue 14', 'Wed 15', 'Thu 16', 'Fri 17', 'Sat 18'];
const hours = ['All-day', '08:00', '09:00', '10:00', '11:00', '12:00', '13:00', '14:00', '15:00', '16:00', '17:00', '18:00'];

export function CalendarWeek({ appointments = [] }) {
  const dynamicEvents = appointments.map(appointmentToEvent);
  return (
    <div className="calendar-week">
      <div className="calendar-days">
        <span />
        {days.map(day => (
          <strong key={day} className={day.includes('Mon') ? 'today-pill' : day.includes('Thu') ? 'active-day' : ''}>
            {day}
          </strong>
        ))}
        <ChevronRight size={15} />
      </div>

      <div className="calendar-grid">
        <div className="time-column">
          {hours.map(hour => <span key={hour}>{hour}</span>)}
        </div>
        <div className="day-grid">
          {days.map(day => <div key={day} className="day-column" />)}
          {dynamicEvents.map((event, index) => {
            const top = 38 + (event.start - 8) * 58;
            const height = (event.end - event.start) * 58 - 8;
            return (
              <div
                key={`${event.client}-${index}`}
                className={`appointment-block block-${event.color}`}
                style={{
                  '--col': event.day,
                  top: `${top}px`,
                  height: `${height}px`
                }}
              >
                <strong>{event.time}</strong>
                <span>{event.client}</span>
                {event.product && <small>{event.product}</small>}
                {event.tech && <Avatar name={event.tech} initials={event.tech} size="xs" />}
              </div>
            );
          })}
          <div className="current-time-line"><span>10:15</span></div>
        </div>
      </div>
    </div>
  );
}

function appointmentToEvent(appointment) {
  const date = new Date(`${appointment.date}T00:00:00`);
  const day = Math.min(6, Math.max(0, date.getDay()));
  const [startHour, startMinute] = appointment.start.split(':').map(Number);
  const [endHour, endMinute] = appointment.end.split(':').map(Number);
  const start = startHour + startMinute / 60;
  const end = endHour + endMinute / 60;
  const colors = { MI: 'green', YT: 'orange', KE: 'purple', AB: 'blue', ZC: 'green' };
  return {
    day,
    start,
    end,
    color: `${colors[appointment.technicianId] || 'blue'} ${appointment.status === 'Confirmed' ? 'selected' : ''}`.trim(),
    time: `${appointment.start} - ${appointment.end}`,
    client: appointment.client,
    product: appointment.product,
    tech: appointment.technicianId || ''
  };
}
