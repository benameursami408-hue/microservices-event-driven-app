import { useEffect, useState } from 'react';
import { Bell, Clock, LogOut, Moon, ShieldCheck, SlidersHorizontal, SunMedium, UserRound } from 'lucide-react';
import { Button, Card } from '../components/ui';

const defaultPreferences = { theme: 'light', notifications: true, serviceWindow: '08:00 - 18:00' };

function loadPreferences() {
  try {
    return { ...defaultPreferences, ...JSON.parse(sessionStorage.getItem('sav-settings-preferences') || '{}') };
  } catch {
    return defaultPreferences;
  }
}

export function SettingsPage({ notify, user, onLogout }) {
  const [preferences, setPreferences] = useState(loadPreferences);

  useEffect(() => {
    sessionStorage.setItem('sav-settings-preferences', JSON.stringify(preferences));
    document.documentElement.dataset.theme = preferences.theme === 'dark' ? 'dark' : 'light';
  }, [preferences]);

  function updatePreference(key, value) {
    setPreferences(current => ({ ...current, [key]: value }));
  }

  return (
    <section className="page-shell settings-page">
      <div className="page-title-row settings-title-row">
        <div>
          <span className="eyebrow">Workspace control</span>
          <h1>Settings</h1>
          <p>Workspace preferences and display options.</p>
        </div>
        {onLogout && <Button icon={LogOut} onClick={onLogout}>Logout</Button>}
      </div>

      <Card title="Workspace settings" icon={SlidersHorizontal} className="settings-card settings-command-card">
        <div className="settings-command-panel">
          <div className="settings-profile settings-profile-hero">
            <div>
              <span>Signed in as</span>
              <strong>{user?.name || 'SAV Pro user'}</strong>
              <small>{user?.role || 'Back office'}</small>
            </div>
            <ShieldCheck size={34} />
          </div>

          <div className="settings-section-grid">
            <section className="settings-section">
              <header className="settings-section-head">
                <span><SlidersHorizontal size={18} /></span>
                <div>
                  <strong>Display</strong>
                  <p>Adjust the workspace look and service availability.</p>
                </div>
              </header>
              <div className="settings-form settings-form-grid">
                <div className="settings-field"><span>Theme mode</span><div className="theme-choice" role="group" aria-label="Theme mode"><button type="button" className={preferences.theme === 'light' ? 'active' : ''} onClick={() => updatePreference('theme', 'light')}><SunMedium size={17} />Light</button><button type="button" className={preferences.theme === 'dark' ? 'active' : ''} onClick={() => updatePreference('theme', 'dark')}><Moon size={17} />Dark</button></div></div>
                <label className="settings-field"><span>Service window</span><input value={preferences.serviceWindow} onChange={event => updatePreference('serviceWindow', event.target.value)} /></label>
              </div>
            </section>

            <section className="settings-section">
              <header className="settings-section-head">
                <span><Bell size={18} /></span>
                <div>
                  <strong>Alerts</strong>
                  <p>Choose which operational updates stay visible.</p>
                </div>
              </header>
              <div className="settings-toggle-row"><div><strong>Operational alerts</strong><p>Receive SLA, appointment and assignment updates.</p></div><button type="button" className={`toggle-switch ${preferences.notifications ? 'on' : ''}`} onClick={() => updatePreference('notifications', !preferences.notifications)} aria-pressed={preferences.notifications}><span /></button></div>
              <div className="settings-info-list settings-info-grid"><Info label="SLA alerts" value="High priority cases" /><Info label="Daily summary" value="Every morning" /><Info label="Planning updates" value="Visit reminders" /></div>
            </section>

            <section className="settings-section">
              <header className="settings-section-head">
                <span><UserRound size={18} /></span>
                <div>
                  <strong>Account</strong>
                  <p>Current workspace identity and access state.</p>
                </div>
              </header>
              <div className="settings-info-list settings-info-grid"><Info label="Workspace" value="SAV Pro" /><Info label="Access level" value={user?.role || 'Operator'} /><Info label="Session" value="HttpOnly cookie session" /></div>
            </section>

            <section className="settings-section settings-section-service">
              <header className="settings-section-head">
                <span><Clock size={18} /></span>
                <div>
                  <strong>Service Rhythm</strong>
                  <p>Used by the dashboard for appointment and SLA context.</p>
                </div>
              </header>
              <div className="settings-info-list settings-info-grid"><Info label="Availability" value={preferences.serviceWindow || '08:00 - 18:00'} /><Info label="Alert status" value={preferences.notifications ? 'Enabled' : 'Paused'} /></div>
            </section>
          </div>
        </div>
      </Card>
    </section>
  );
}

function Info({ label, value }) { return <div className="settings-info-item"><span>{label}</span><strong>{value}</strong></div>; }
