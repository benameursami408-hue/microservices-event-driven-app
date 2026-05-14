import { useEffect, useMemo, useState } from 'react';
import { Navigate, Route, Routes, useNavigate } from 'react-router-dom';
import { AppLayout } from './components/layout/AppLayout';
import { ClientPortalLayout } from './components/layout/ClientPortalLayout';
import { Toast } from './components/ui';
import { AuthProvider, useAuth } from './context/AuthContext';
import { canAccessBackOffice, canAccessClientPortal, getHomePath } from './services/authService';
import { useDashboard } from './hooks/useDashboard';
import { useNotifications } from './hooks/useNotifications';
import { ClientPortalPage } from './pages/ClientPortalPage';
import { DashboardPage } from './pages/DashboardPage';
import { InterventionsPage } from './pages/InterventionsPage';
import { LoginPage } from './pages/LoginPage';
import { PlanningPage } from './pages/PlanningPage';
import { ReclamationsPage } from './pages/ReclamationsPage';
import { NotificationsPage } from './pages/NotificationsPage';
import { VisitReportsPage } from './pages/VisitReportsPage';
import { ClientsPage } from './pages/ClientsPage';
import { UsersRolesPage } from './pages/UsersRolesPage';
import { ProfilePage } from './pages/ProfilePage';
import { SettingsPage } from './pages/SettingsPage';
import { AccessDeniedPage } from './pages/AccessDeniedPage';
import { canAccessBackOfficeRoute, canAccessDashboardSummary } from './utils/roleAccess';

const backOfficePaths = {
  dashboard: '/dashboard',
  reclamations: '/reclamations',
  clients: '/clients',
  planning: '/planning',
  interventions: '/interventions',
  'visit-reports': '/visit-reports',
  reports: '/reports',
  notifications: '/notifications',
  users: '/users',
  settings: '/settings',
  profile: '/profile'
};

const clientPaths = {
  client: '/client',
  'client-appointments': '/client/appointments',
  'client-reports': '/client/reports',
  'client-knowledge': '/client/knowledge',
  'client-notifications': '/client/notifications',
  'client-profile': '/client/profile',
  'client-settings': '/client/settings'
};

export default function App() {
  return (
    <AuthProvider>
      <AppRoutes />
    </AuthProvider>
  );
}

function AppRoutes() {
  const { user, initializing, login, logout } = useAuth();
  const [toast, setToast] = useState(null);
  const navigate = useNavigate();
  const dashboard = useDashboard(Boolean(user) && canAccessDashboardSummary(user));
  const notifications = useNotifications(20, Boolean(user));

  const counts = useMemo(() => ({
    reclamations: dashboard.summary?.openReclamations ?? dashboard.summary?.totalOpen ?? 0,
    planning: dashboard.summary?.plannedVisits ?? dashboard.summary?.upcomingAppointments?.length ?? 0,
    notifications: notifications.unreadCount
  }), [dashboard.summary, notifications.unreadCount]);

  function notify(message, type = 'success') {
    setToast({ message, type });
  }

  async function handleLogin(email, password) {
    const safeUser = await login(email, password);
    notify(`Signed in as ${safeUser.name || safeUser.email}`);
    navigate(getHomePath(safeUser), { replace: true });
  }

  async function handleLogout() {
    await logout();
    notify('Signed out');
  }

  function navigateById(id) {
    const path = backOfficePaths[id] || clientPaths[id] || getHomePath(user);
    navigate(path);
  }

  useEffect(() => {
    if (!toast) return undefined;
    const timer = window.setTimeout(() => setToast(null), 2600);
    return () => window.clearTimeout(timer);
  }, [toast]);

  if (initializing) {
    return <div className="page-loader">Restoring backend session...</div>;
  }

  const shared = { user, notify, navigate: navigateById };

  return (
    <>
      <Routes>
        <Route path="/login" element={user ? <Navigate to={getHomePath(user)} replace /> : <LoginPage onLogin={handleLogin} />} />
        <Route path="/" element={<Navigate to={user ? getHomePath(user) : '/login'} replace />} />

        <Route path="/dashboard" element={<BackOfficeFrame user={user} counts={counts} activePage="dashboard" onNavigate={navigateById} onLogout={handleLogout}><DashboardPage {...shared} /></BackOfficeFrame>} />
        <Route path="/reclamations" element={<BackOfficeFrame user={user} counts={counts} activePage="reclamations" onNavigate={navigateById} onLogout={handleLogout}><ReclamationsPage {...shared} /></BackOfficeFrame>} />
        <Route path="/planning" element={<BackOfficeFrame user={user} counts={counts} activePage="planning" onNavigate={navigateById} onLogout={handleLogout}><PlanningPage {...shared} /></BackOfficeFrame>} />
        <Route path="/interventions" element={<BackOfficeFrame user={user} counts={counts} activePage="interventions" onNavigate={navigateById} onLogout={handleLogout}><InterventionsPage {...shared} /></BackOfficeFrame>} />
        <Route path="/visit-reports" element={<BackOfficeFrame user={user} counts={counts} activePage="visit-reports" onNavigate={navigateById} onLogout={handleLogout}><VisitReportsPage {...shared} /></BackOfficeFrame>} />
        <Route path="/notifications" element={<BackOfficeFrame user={user} counts={counts} activePage="notifications" onNavigate={navigateById} onLogout={handleLogout}><NotificationsPage {...shared} /></BackOfficeFrame>} />
        <Route path="/clients" element={<BackOfficeFrame user={user} counts={counts} activePage="clients" onNavigate={navigateById} onLogout={handleLogout}><ClientsPage {...shared} /></BackOfficeFrame>} />
        <Route path="/clients/:clientId" element={<BackOfficeFrame user={user} counts={counts} activePage="clients" onNavigate={navigateById} onLogout={handleLogout}><ClientsPage {...shared} detail /></BackOfficeFrame>} />
        <Route path="/users" element={<BackOfficeFrame user={user} counts={counts} activePage="users" onNavigate={navigateById} onLogout={handleLogout}><UsersRolesPage {...shared} /></BackOfficeFrame>} />
        <Route path="/reports" element={<BackOfficeFrame user={user} counts={counts} activePage="reports" onNavigate={navigateById} onLogout={handleLogout}><DashboardPage {...shared} /></BackOfficeFrame>} />
        <Route path="/settings" element={<BackOfficeFrame user={user} counts={counts} activePage="settings" onNavigate={navigateById} onLogout={handleLogout}><SettingsPage {...shared} onLogout={handleLogout} /></BackOfficeFrame>} />
        <Route path="/profile" element={<BackOfficeFrame user={user} counts={counts} activePage="profile" onNavigate={navigateById} onLogout={handleLogout}><ProfilePage {...shared} onLogout={handleLogout} /></BackOfficeFrame>} />
        <Route path="/access-denied" element={<AccessDeniedFrame user={user} counts={counts} onNavigate={navigateById} onLogout={handleLogout}><AccessDeniedPage user={user} /></AccessDeniedFrame>} />

        <Route path="/client" element={<ClientFrame user={user} counts={counts} active="client" onNavigate={navigateById} onLogout={handleLogout}><ClientPortalPage {...shared} mode="home" /></ClientFrame>} />
        <Route path="/client/appointments" element={<ClientFrame user={user} counts={counts} active="client-appointments" onNavigate={navigateById} onLogout={handleLogout}><ClientPortalPage {...shared} mode="appointments" /></ClientFrame>} />
        <Route path="/client/reports" element={<ClientFrame user={user} counts={counts} active="client-reports" onNavigate={navigateById} onLogout={handleLogout}><VisitReportsPage {...shared} clientMode /></ClientFrame>} />
        <Route path="/client/knowledge" element={<ClientFrame user={user} counts={counts} active="client-knowledge" onNavigate={navigateById} onLogout={handleLogout}><ClientPortalPage {...shared} mode="knowledge" /></ClientFrame>} />
        <Route path="/client/notifications" element={<ClientFrame user={user} counts={counts} active="client-notifications" onNavigate={navigateById} onLogout={handleLogout}><NotificationsPage {...shared} clientMode /></ClientFrame>} />
        <Route path="/client/profile" element={<ClientFrame user={user} counts={counts} active="client-profile" onNavigate={navigateById} onLogout={handleLogout}><ProfilePage {...shared} onLogout={handleLogout} /></ClientFrame>} />
        <Route path="/client/settings" element={<ClientFrame user={user} counts={counts} active="client-settings" onNavigate={navigateById} onLogout={handleLogout}><SettingsPage {...shared} onLogout={handleLogout} /></ClientFrame>} />

        <Route path="*" element={<Navigate to={user ? getHomePath(user) : '/login'} replace />} />
      </Routes>
      <Toast toast={toast} onClose={() => setToast(null)} />
    </>
  );
}

function BackOfficeFrame({ user, counts, activePage, onNavigate, onLogout, children }) {
  if (!user) return <Navigate to="/login" replace />;
  if (!canAccessBackOffice(user)) return <Navigate to="/client" replace />;
  if (!canAccessBackOfficeRoute(user, activePage)) return <Navigate to="/access-denied" replace />;
  return (
    <AppLayout activePage={activePage} user={user} counts={counts} onNavigate={onNavigate} onCreate={() => onNavigate('reclamations')} onLogout={onLogout}>
      {children}
    </AppLayout>
  );
}

function AccessDeniedFrame({ user, counts, onNavigate, onLogout, children }) {
  if (!user) return <Navigate to="/login" replace />;
  if (!canAccessBackOffice(user)) return <Navigate to="/client" replace />;
  return (
    <AppLayout activePage="" user={user} counts={counts} onNavigate={onNavigate} onCreate={() => onNavigate('reclamations')} onLogout={onLogout}>
      {children}
    </AppLayout>
  );
}

function ClientFrame({ user, counts, active, onNavigate, onLogout, children }) {
  if (!user) return <Navigate to="/login" replace />;
  if (!canAccessClientPortal(user)) return <Navigate to={getHomePath(user)} replace />;
  return (
    <ClientPortalLayout user={user} active={active} unreadCount={counts.notifications} onNavigate={onNavigate} onLogout={onLogout}>
      {children}
    </ClientPortalLayout>
  );
}
