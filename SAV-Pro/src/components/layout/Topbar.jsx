import { useMemo, useState } from 'react';
import {
  Bell,
  CalendarDays,
  ChevronDown,
  ClipboardList,
  FileText,
  LayoutDashboard,
  LogOut,
  Settings,
  UserCog,
  UserRound,
  Users,
  Wrench
} from 'lucide-react';
import { Avatar, Badge, IconButton, Logo } from '../ui';
import { canAccessBackOfficeRoute } from '../../utils/roleAccess';

const navItems = [
  { id: 'dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { id: 'reclamations', label: 'Reclamations', icon: ClipboardList },
  { id: 'clients', label: 'Clients', icon: Users },
  { id: 'planning', label: 'Planning', icon: CalendarDays },
  { id: 'interventions', label: 'Interventions', icon: Wrench },
  { id: 'visit-reports', label: 'Reports', icon: FileText },
  { id: 'users', label: 'Users', icon: UserCog }
];

export function Topbar({ activePage, user, counts = {}, onCreate, onNavigate, onLogout, unreadCount = 0 }) {
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const [navOpen, setNavOpen] = useState(false);
  const visibleNavItems = useMemo(() => navItems.filter(item => canAccessBackOfficeRoute(user, item.id)), [user]);

  function choose(action) {
    setUserMenuOpen(false);
    action();
  }

  function navigate(id) {
    setNavOpen(false);
    onNavigate(id);
  }

  return (
    <header className="topbar admin-navbar">
      <div className="admin-navbar-main">
        <button type="button" className="admin-nav-logo" onClick={() => navigate('dashboard')} aria-label="Go to dashboard">
          <Logo />
        </button>

        <button type="button" className="admin-nav-toggle" onClick={() => setNavOpen(current => !current)} aria-expanded={navOpen}>
          <LayoutDashboard size={18} />
          Menu
          <ChevronDown size={16} />
        </button>

        <nav className={`admin-nav-links ${navOpen ? 'open' : ''}`} aria-label="SAV Pro navigation">
          {visibleNavItems.map(item => {
            const Icon = item.icon;
            const count = item.id === 'notifications' ? unreadCount : counts[item.id];
            return (
              <button
                type="button"
                key={item.id}
                className={activePage === item.id ? 'active' : ''}
                onClick={() => navigate(item.id)}
              >
                <Icon size={18} />
                <span>{item.label}</span>
                {count ? <Badge tone="count">{count}</Badge> : null}
              </button>
            );
          })}
        </nav>
      </div>

      <div className="admin-navbar-tools">
        <IconButton
          icon={Bell}
          label="Notifications"
          badge={unreadCount || undefined}
          className={`admin-notification-btn ${activePage === 'notifications' ? 'active-tool' : ''}`}
          onClick={() => onNavigate('notifications')}
        />
        <div className="topbar-user-wrap">
          <button type="button" className="topbar-user" onClick={() => setUserMenuOpen(current => !current)} aria-expanded={userMenuOpen}>
            <Avatar name={user.name} initials={user.avatar} size="lg" />
            <ChevronDown size={16} />
          </button>
          {userMenuOpen && (
            <div className="topbar-user-menu">
              <button type="button" onClick={() => choose(() => onNavigate('profile'))}><UserRound size={16} /> Profile</button>
              <button type="button" onClick={() => choose(() => onNavigate('settings'))}><Settings size={16} /> Settings</button>
              <button type="button" onClick={() => choose(onLogout)}><LogOut size={16} /> Logout</button>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}
