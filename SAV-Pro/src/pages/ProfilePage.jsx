import { Building2, BriefcaseBusiness, IdCard, LogOut, Mail, Save, ShieldCheck, UserRound } from 'lucide-react';
import { useState } from 'react';
import { Avatar, Badge, Button, Card, Field } from '../components/ui';

export function ProfilePage({ user, notify, onLogout }) {
  const [editing, setEditing] = useState(false);
  const [form, setForm] = useState({ name: user?.name || '', email: user?.email || '', title: user?.title || '', company: user?.company || '', role: user?.role || '' });

  function submit(event) {
    event.preventDefault();
    notify('Profile editing requires a backend /api/auth/me update endpoint.', 'error');
    setEditing(false);
  }

  const storedUser = { ...user, ...form };
  const statusLabel = storedUser.isActive === false ? 'Inactive' : 'Active';

  return (
    <section className="page-shell profile-page">
      <div className="page-title-row profile-title-row">
        <div>
          <span className="eyebrow">Account center</span>
          <h1>Profile</h1>
          <p>Manage your account, role and contact information.</p>
        </div>
      </div>

      <section className="profile-hero-card card">
        <div className="profile-hero-main">
          <Avatar name={storedUser.name} initials={storedUser.avatar} size="xl" />
          <div>
            <span className="eyebrow">Signed in as</span>
            <h2>{storedUser.name || 'SAV Pro user'}</h2>
            <p>{storedUser.title || 'After-sales service workspace user'}</p>
          </div>
        </div>
        <div className="profile-hero-badges">
          <Badge>{storedUser.role || 'User'}</Badge>
          <Badge tone={statusLabel === 'Active' ? 'success' : 'closed'}>{statusLabel}</Badge>
        </div>
        <div className="profile-hero-actions">
          <Button variant="primary" icon={UserRound} onClick={() => setEditing(true)}>Edit Profile</Button>
          <Button icon={LogOut} onClick={onLogout}>Logout</Button>
        </div>
      </section>

      <div className="profile-page-grid">
        <Card title="Account Snapshot" icon={UserRound} className="profile-summary-card">
          <div className="profile-snapshot-hero">
            <span className="profile-snapshot-icon"><IdCard size={22} /></span>
            <div>
              <small>Account owner</small>
              <strong>{storedUser.name || 'User profile'}</strong>
              <p>{storedUser.title || 'SAV Pro workspace access'}</p>
            </div>
          </div>

          <div className="profile-snapshot-grid">
            <SnapshotItem icon={Mail} label="Email" value={storedUser.email || '-'} />
            <SnapshotItem icon={BriefcaseBusiness} label="Role" value={storedUser.role || '-'} tone="purple" />
            <SnapshotItem icon={Building2} label="Company" value={storedUser.company || 'Not set'} tone="teal" />
            <SnapshotItem icon={ShieldCheck} label="Status" value={statusLabel} tone={statusLabel === 'Active' ? 'green' : 'gray'} />
          </div>
        </Card>

        <Card title={editing ? 'Edit Profile' : 'Account Details'} icon={editing ? Save : UserRound} className="profile-form-card">
          {editing ? (
            <form className="profile-edit-form" onSubmit={submit}>
              <div className="profile-edit-summary">
                <span><ShieldCheck size={20} /></span>
                <div>
                  <strong>Profile changes</strong>
                  <p>Name, email, title and company are editable here. Role stays controlled by admin permissions.</p>
                </div>
              </div>

              <div className="profile-edit-sections">
                <section className="form-section">
                  <div className="form-section-heading">
                    <span><UserRound size={16} /></span>
                    <h3>Identity</h3>
                  </div>
                  <div className="structured-field-grid">
                    <Field label="Name" className="full"><input value={form.name} onChange={event => setForm(current => ({ ...current, name: event.target.value }))} /></Field>
                    <Field label="Email" className="full"><input type="email" value={form.email} onChange={event => setForm(current => ({ ...current, email: event.target.value }))} /></Field>
                  </div>
                </section>

                <section className="form-section">
                  <div className="form-section-heading">
                    <span><BriefcaseBusiness size={16} /></span>
                    <h3>Organization & access</h3>
                  </div>
                  <div className="structured-field-grid">
                    <Field label="Title"><input value={form.title} onChange={event => setForm(current => ({ ...current, title: event.target.value }))} /></Field>
                    <Field label="Company"><input value={form.company} onChange={event => setForm(current => ({ ...current, company: event.target.value }))} /></Field>
                    <Field label="Role" className="full"><input value={form.role} disabled /></Field>
                  </div>
                </section>
              </div>

              <div className="profile-form-footer">
                <Button type="button" onClick={() => setEditing(false)}>Cancel</Button>
                <Button type="submit" variant="primary" icon={Save}>Save Profile</Button>
              </div>
            </form>
          ) : (
            <div className="details-matrix profile-details-matrix">
              <Info label="Name" value={storedUser.name} />
              <Info label="Email" value={storedUser.email} />
              <Info label="Role" value={storedUser.role} />
              <Info label="Title" value={storedUser.title} />
              <Info label="Company" value={storedUser.company} />
              <Info label="User ID" value={storedUser.id} />
            </div>
          )}
        </Card>
      </div>
    </section>
  );
}

function SnapshotItem({ icon: Icon, label, value, tone = 'blue' }) {
  return (
    <div className={`profile-snapshot-item snapshot-${tone}`}>
      <span><Icon size={17} /></span>
      <small>{label}</small>
      <strong>{value}</strong>
    </div>
  );
}

function Info({ label, value }) { return <div className="info-pair"><span>{label}</span><strong>{value || '-'}</strong></div>; }
