import {
  Bell,
  BookOpen,
  CalendarDays,
  ChevronDown,
  FileText,
  Home,
  LayoutDashboard,
  LogOut,
  Settings,
  UserRound
} from 'lucide-react';
import { useState } from 'react';
import { Avatar, IconButton, Logo } from '../ui';

const clientNav = [
  { id: 'client', label: 'Home', icon: Home },
  { id: 'client', label: 'My Requests', icon: LayoutDashboard },
  { id: 'client-appointments', label: 'Appointments', icon: CalendarDays },
  { id: 'client-reports', label: 'Visit Reports', icon: FileText },
  { id: 'client-knowledge', label: 'Knowledge Base', icon: BookOpen }
];

export function ClientPortalLayout({ user, active = 'client', onNavigate, onLogout, unreadCount = 0, children }) {
  const [userMenuOpen, setUserMenuOpen] = useState(false);

  function choose(action) {
    setUserMenuOpen(false);
    action?.();
  }

  return (
    <div className="client-shell">
      <header className="client-topnav">
        <Logo portal />
        <nav aria-label="Client portal navigation">
          {clientNav.map((item, index) => {
            const Icon = item.icon;
            return (
              <button
                type="button"
                key={`${item.label}-${index}`}
                className={active === item.id && item.label === 'My Requests' ? 'active' : ''}
                onClick={() => onNavigate(item.id)}
              >
                <Icon size={20} />
                {item.label}
              </button>
            );
          })}
        </nav>
        <div className="client-user-tools">
          <IconButton icon={Bell} label="Notifications" className="bell-btn" badge={unreadCount || undefined} onClick={() => onNavigate('client-notifications')} />
          <Avatar name={user.name} initials={user.avatar} size="xl" />
          <div className="client-user-wrap">
            <button type="button" className="client-user-menu" onClick={() => setUserMenuOpen(current => !current)} aria-expanded={userMenuOpen}>
              <span>
                <strong>{user.name}</strong>
                <small>{user.company}</small>
              </span>
              <ChevronDown size={16} />
            </button>
            {userMenuOpen && (
              <div className="topbar-user-menu client-user-dropdown">
                <button type="button" onClick={() => choose(() => onNavigate('client-profile'))}><UserRound size={16} /> Profile</button>
                <button type="button" onClick={() => choose(() => onNavigate('client-settings'))}><Settings size={16} /> Settings</button>
                <button type="button" onClick={() => choose(onLogout)}><LogOut size={16} /> Logout</button>
              </div>
            )}
          </div>
        </div>
      </header>
      {children}
    </div>
  );
}
