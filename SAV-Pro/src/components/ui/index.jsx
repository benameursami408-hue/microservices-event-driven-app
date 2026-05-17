import {
  AlertTriangle,
  Bell,
  BookOpen,
  CalendarDays,
  CheckCircle,
  ChevronDown,
  Clock,
  FileText,
  Globe,
  HelpCircle,
  Image as ImageIcon,
  LayoutDashboard,
  Mail,
  MoreVertical,
  Package,
  Search,
  ShieldAlert,
  ShieldCheck,
  User,
  UserCog,
  UserRound,
  Wrench,
  XCircle
} from 'lucide-react';
import { useEffect, useRef, useState } from 'react';

export function Logo({ portal = false, compact = false }) {
  return (
    <div className={`logo ${compact ? 'logo-compact' : ''}`}>
      <span className="logo-mark">
        <ShieldCheck size={compact ? 18 : 24} strokeWidth={2.4} />
      </span>
      {!compact && (
        <span className="logo-copy">
          <strong>SAV <em>Pro</em></strong>
          <small>{portal ? 'Client Portal' : 'After-sales Service Management'}</small>
        </span>
      )}
    </div>
  );
}

export function Button({ children, icon: Icon, rightIcon: RightIcon, variant = 'secondary', size = 'md', className = '', ...props }) {
  return (
    <button className={`btn btn-${variant} btn-${size} ${className}`.trim()} {...props}>
      {Icon && <Icon size={size === 'sm' ? 15 : 17} />}
      <span>{children}</span>
      {RightIcon && <RightIcon size={size === 'sm' ? 15 : 17} />}
    </button>
  );
}

export function IconButton({ icon: Icon = MoreVertical, label, className = '', badge, ...props }) {
  return (
    <button className={`icon-btn ${className}`.trim()} aria-label={label} title={label} {...props}>
      <Icon size={18} />
      {badge ? <span className="icon-badge">{badge}</span> : null}
    </button>
  );
}

export function Badge({ children, tone = '', className = '' }) {
  const value = String(children || '').trim().toLowerCase().replace(/\s+/g, '-');
  return <span className={`badge badge-${tone || value} ${className}`.trim()}>{children}</span>;
}

export function Card({ title, icon: Icon, actions, children, className = '' }) {
  return (
    <section className={`card ${className}`.trim()}>
      {(title || actions) && (
        <header className="card-header">
          <div className="card-title-wrap">
            {Icon && <span className="section-icon"><Icon size={18} /></span>}
            {title && <h3>{title}</h3>}
          </div>
          {actions && <div className="card-actions">{actions}</div>}
        </header>
      )}
      {children}
    </section>
  );
}

export function StatCard({ label, value, trend, trendTone = 'neutral', icon: Icon = LayoutDashboard, tone = 'blue', spark = [] }) {
  const points = spark.length ? spark : [18, 30, 24, 44, 30, 28, 38, 52];
  const max = Math.max(...points);
  const min = Math.min(...points);
  const polyline = points.map((point, index) => {
    const x = (index / (points.length - 1)) * 92 + 4;
    const y = 48 - ((point - min) / Math.max(max - min, 1)) * 34;
    return `${x},${y}`;
  }).join(' ');

  return (
    <Card className={`stat-card stat-card-${tone}`}>
      <div className={`stat-icon stat-${tone}`}>
        <Icon size={32} />
      </div>
      <div className="stat-copy">
        <span>{label}</span>
        <strong>{value}</strong>
        <small className={`stat-trend stat-trend-${trendTone}`}>{trend}</small>
      </div>
      <svg className={`spark spark-${tone}`} viewBox="0 0 100 54" role="img" aria-label={`${label} trend`}>
        <defs>
          <linearGradient id={`spark-${label.replace(/\s+/g, '-')}`} x1="0" x2="0" y1="0" y2="1">
            <stop offset="0%" stopColor="currentColor" stopOpacity="0.28" />
            <stop offset="100%" stopColor="currentColor" stopOpacity="0" />
          </linearGradient>
        </defs>
        {[16, 30, 44].map(y => <line key={y} x1="4" x2="96" y1={y} y2={y} />)}
        <polyline points={`4,52 ${polyline} 96,52`} fill={`url(#spark-${label.replace(/\s+/g, '-')})`} stroke="none" />
        <polyline points={polyline} fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round" />
        {points.map((point, index) => {
          const x = (index / (points.length - 1)) * 92 + 4;
          const y = 48 - ((point - min) / Math.max(max - min, 1)) * 34;
          return <circle key={`${point}-${index}`} cx={x} cy={y} r={index === points.length - 1 ? '3.2' : '2.1'} fill="currentColor" />;
        })}
      </svg>
    </Card>
  );
}

export function SearchInput({ value, onChange, placeholder = 'Search...', className = '' }) {
  return (
    <label className={`search-input ${className}`.trim()}>
      <Search size={18} />
      <input value={value} onChange={event => onChange?.(event.target.value)} placeholder={placeholder} />
    </label>
  );
}

export function SelectFilter({ label, icon: Icon = ChevronDown, options = [], value, onChange }) {
  const [open, setOpen] = useState(false);
  const wrapperRef = useRef(null);
  const selectedLabel = value || label;

  useEffect(() => {
    function closeMenu(event) {
      if (!wrapperRef.current?.contains(event.target)) setOpen(false);
    }
    document.addEventListener('mousedown', closeMenu);
    return () => document.removeEventListener('mousedown', closeMenu);
  }, []);

  function choose(nextValue) {
    onChange?.(nextValue);
    setOpen(false);
  }

  return (
    <div className={`select-filter ${open ? 'open' : ''}`} ref={wrapperRef}>
      <button type="button" className="select-filter-trigger" onClick={() => setOpen(current => !current)} aria-haspopup="listbox" aria-expanded={open}>
        <span>{selectedLabel}</span>
        <Icon size={16} />
      </button>
      {open && (
        <div className="select-filter-menu" role="listbox">
          <button type="button" className={!value ? 'selected' : ''} onClick={() => choose('')}>{label}</button>
          {options.map(option => (
            <button type="button" key={option} className={value === option ? 'selected' : ''} onClick={() => choose(option)}>{option}</button>
          ))}
        </div>
      )}
      <select value={value || ''} onChange={event => onChange?.(event.target.value)} tabIndex={-1} aria-hidden="true">
        <option value="">{label}</option>
        {options.map(option => (
          <option key={option} value={option}>{option}</option>
        ))}
      </select>
    </div>
  );
}

export function Avatar({ name = 'User', initials, size = 'md', className = '' }) {
  const fallback = initials || name.split(' ').map(part => part[0]).join('').slice(0, 2).toUpperCase();
  return (
    <span className={`avatar avatar-${size} ${className}`.trim()} aria-label={name}>
      {fallback}
    </span>
  );
}

export function DataTable({ columns, rows, selectedId, onRowClick, className = '' }) {
  return (
    <div className={`table-wrap ${className}`.trim()}>
      <table className="data-table">
        <thead>
          <tr>
            {columns.map(column => <th key={column.key}>{column.label}</th>)}
          </tr>
        </thead>
        <tbody>
          {rows.map(row => (
            <tr
              key={row.id}
              className={selectedId === row.id ? 'selected' : ''}
              onClick={onRowClick ? () => onRowClick(row) : undefined}
            >
              {columns.map(column => (
                <td key={column.key}>{column.render ? column.render(row) : row[column.key]}</td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export function Timeline({ items, compact = false }) {
  return (
    <div className={`timeline ${compact ? 'timeline-compact' : ''}`}>
      {items.map((item, index) => (
        <div className="timeline-item" key={`${item.title}-${index}`}>
          <span className={`timeline-dot dot-${item.color || 'blue'}`} />
          <div className="timeline-time">{item.time}</div>
          <div className="timeline-body">
            <strong>{item.title}</strong>
            {item.body && <p>{item.body}</p>}
          </div>
          {item.status && <Badge>{item.status}</Badge>}
        </div>
      ))}
    </div>
  );
}

export function Stepper({ steps, current, horizontal = false }) {
  const currentIndex = typeof current === 'number' ? current : steps.findIndex(step => step === current);
  return (
    <div className={`stepper ${horizontal ? 'stepper-horizontal' : ''}`}>
      {steps.map((step, index) => {
        const done = index < currentIndex;
        const active = index === currentIndex;
        return (
          <div className={`step ${done ? 'done' : ''} ${active ? 'active' : ''}`} key={step.label || step}>
            <span className="step-node">
              {step.icon ? <step.icon size={16} /> : done ? <CheckCircle size={16} /> : active ? <span /> : null}
            </span>
            <strong>{step.label || step}</strong>
            {step.meta && <small>{step.meta}</small>}
          </div>
        );
      })}
    </div>
  );
}

export function Toast({ toast, onClose }) {
  if (!toast) return null;
  return (
    <div className={`toast toast-${toast.type || 'success'}`}>
      <CheckCircle size={18} />
      <span>{toast.message}</span>
      <button type="button" onClick={onClose} aria-label="Dismiss notification"><XCircle size={16} /></button>
    </div>
  );
}

const notificationIconMap = {
  warning: AlertTriangle,
  sla: ShieldAlert,
  calendar: CalendarDays,
  success: CheckCircle,
  report: FileText,
  user: UserRound,
  assignment: UserCog,
  clock: Clock,
  bell: Bell,
  book: BookOpen,
  image: ImageIcon,
  package: Package,
  wrench: Wrench,
  mail: Mail,
  help: HelpCircle,
  globe: Globe,
  default: Bell
};

export function NotificationItem({ item, compact = false }) {
  const Icon = notificationIconMap[item.type] || notificationIconMap.default;
  return (
    <div className={`notification-item notification-${item.type || 'default'} ${compact ? 'compact' : ''}`}>
      <span className="notification-icon"><Icon size={20} /></span>
      <div>
        <strong>{item.title}</strong>
        {item.message && <p>{item.message}</p>}
      </div>
      <time>{item.time}</time>
      {item.unread && <span className="unread-dot" />}
    </div>
  );
}

export function Modal({ title, children, footer, onClose }) {
  return (
    <div className="modal-backdrop" role="presentation">
      <section className="modal-card" role="dialog" aria-modal="true" aria-label={title}>
        <header className="modal-header">
          <h2>{title}</h2>
          <IconButton icon={XCircle} label="Close modal" className="ghost" onClick={onClose} />
        </header>
        <div className="modal-body">{children}</div>
        {footer && <footer className="modal-footer">{footer}</footer>}
      </section>
    </div>
  );
}

export function Field({ label, error, children }) {
  return (
    <label className="form-field">
      <span>{label}</span>
      {children}
      {error && <small>{error}</small>}
    </label>
  );
}
