import { Topbar } from './Topbar';

export function AppLayout({ activePage, user, onNavigate, onCreate, onLogout, counts, children }) {
  return (
    <div className="admin-shell">
      <main className="admin-main">
        <Topbar
          activePage={activePage}
          user={user}
          counts={counts}
          onNavigate={onNavigate}
          onCreate={onCreate}
          onLogout={onLogout}
          unreadCount={counts?.notifications || 0}
        />
        {children}
      </main>
    </div>
  );
}
