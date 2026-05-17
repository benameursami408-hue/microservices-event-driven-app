import { LogOut, Mail, Save, ShieldCheck, UserRound } from 'lucide-react';
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

  return (
    <section className="page-shell profile-page">
      <div className="page-title-row profile-title-row">
        <div>
          <span className="eyebrow">Account center</span>
          <h1>Profile</h1>
          <p>Manage your account, role and contact information.</p>
        </div>
        <Button icon={LogOut} onClick={onLogout}>Logout</Button>
      </div>

      <div className="profile-page-grid">
        <Card title="Profile" icon={UserRound} className="profile-summary-card">
          <div className="profile-summary">
            <Avatar name={storedUser.name} initials={storedUser.avatar} size="xl" />
            <div>
              <h2>{storedUser.name}</h2>
              <Badge>{storedUser.role}</Badge>
              <p>{storedUser.title || 'SAV Pro user'}</p>
            </div>
          </div>
          <div className="profile-identity-strip">
            <span><Mail size={16} />{storedUser.email || '-'}</span>
            <span><ShieldCheck size={16} />{storedUser.isActive === false ? 'Inactive' : 'Active'}</span>
          </div>
          <div className="side-info-list">
            <Info label="Email" value={storedUser.email} />
            <Info label="Company" value={storedUser.company} />
            <Info label="Status" value={storedUser.isActive === false ? 'Inactive' : 'Active'} />
          </div>
          <div className="profile-actions"><Button variant="primary" icon={UserRound} onClick={() => setEditing(true)}>Edit Profile</Button></div>
        </Card>

        <Card title={editing ? 'Edit Profile' : 'Account Details'} icon={UserRound} className="profile-form-card">
          {editing ? (
            <form className="form-grid profile-edit-form" onSubmit={submit}>
              <Field label="Name"><input value={form.name} onChange={event => setForm(current => ({ ...current, name: event.target.value }))} /></Field>
              <Field label="Email"><input type="email" value={form.email} onChange={event => setForm(current => ({ ...current, email: event.target.value }))} /></Field>
              <Field label="Title"><input value={form.title} onChange={event => setForm(current => ({ ...current, title: event.target.value }))} /></Field>
              <Field label="Company"><input value={form.company} onChange={event => setForm(current => ({ ...current, company: event.target.value }))} /></Field>
              <Field label="Role"><input value={form.role} disabled /></Field>
              <div className="form-actions full"><Button type="button" onClick={() => setEditing(false)}>Cancel</Button><Button type="submit" variant="primary" icon={Save}>Save Profile</Button></div>
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

function Info({ label, value }) { return <div className="info-pair"><span>{label}</span><strong>{value || '-'}</strong></div>; }
