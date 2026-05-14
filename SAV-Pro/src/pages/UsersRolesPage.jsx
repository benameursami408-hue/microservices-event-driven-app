import { Edit, Plus, ShieldCheck, ToggleLeft, ToggleRight, Trash2 } from 'lucide-react';
import { useMemo, useState } from 'react';
import { ApiErrorState } from '../components/common/ApiErrorState';
import { Badge, Button, Card, DataTable, Field, Modal, SearchInput, SelectFilter } from '../components/ui';
import { useUsers } from '../hooks/useUsers';
import { getFriendlyApiError } from '../utils/errorMessages';
import { canManageUsers } from '../utils/roleAccess';

const emptyUser = { name: '', email: '', role: '', company: '', password: '' };

export function UsersRolesPage({ user, notify }) {
  const allowManageUsers = canManageUsers(user);
  const [query, setQuery] = useState('');
  const [role, setRole] = useState('All');
  const [modal, setModal] = useState(null);
  const [form, setForm] = useState(emptyUser);
  const userResource = useUsers(role === 'All' ? undefined : role);
  const users = userResource.users;
  const visibleUsers = useMemo(() => users.filter(item => `${item.name} ${item.email} ${item.role}`.toLowerCase().includes(query.toLowerCase())), [users, query]);

  function open(row = null) {
    if (!allowManageUsers) {
      notify('Cette action est réservée à l’administrateur.', 'error');
      return;
    }
    setForm(row || emptyUser);
    setModal(row ? 'edit' : 'new');
  }

  async function submit(event) {
    event.preventDefault();
    if (!allowManageUsers) return;
    try {
      if (modal === 'edit') await userResource.update(form.technicalId || form.id, form);
      else await userResource.create(form);
      setModal(null);
      notify(modal === 'edit' ? 'User updated in backend' : 'User created in backend');
    } catch (err) {
      notify(getFriendlyApiError(err), 'error');
    }
  }

  async function remove(row) {
    if (!allowManageUsers) {
      notify('Cette action est réservée à l’administrateur.', 'error');
      return;
    }
    try {
      await userResource.remove(row.technicalId || row.id);
      notify('User deleted in backend');
    } catch (err) {
      notify(getFriendlyApiError(err), 'error');
    }
  }

  if (userResource.error) {
    return <section className="page-shell"><Card title="Users & Roles" icon={ShieldCheck}><ApiErrorState status={userResource.errorStatus} message={userResource.error} onRetry={userResource.reload} /></Card></section>;
  }

  return (
    <section className="page-shell">
      <div className="page-title-row"><div><h1>Users & Roles</h1><p>Manage access, roles and account status through AuthService.</p></div>{allowManageUsers ? <Button variant="primary" icon={Plus} onClick={() => open()}>Add User</Button> : null}</div>
      <Card title="Users" icon={ShieldCheck}>
        {!allowManageUsers ? <p className="permission-note">Lecture seule : la création, modification et suppression des utilisateurs sont réservées à l’administrateur.</p> : null}
        <div className="table-toolbar"><SearchInput value={query} onChange={setQuery} placeholder="Search users..." /><SelectFilter label="All Roles" value={role === 'All' ? '' : role} onChange={value => setRole(value || 'All')} options={['Admin', 'SAV', 'Technician', 'Client']} /></div>
        <DataTable rows={visibleUsers} columns={[{ key: 'name', label: 'Name' }, { key: 'email', label: 'Email' }, { key: 'role', label: 'Role', render: row => <Badge>{row.role}</Badge> }, { key: 'company', label: 'Company' }, { key: 'isActive', label: 'Status', render: row => <Badge>{row.isActive ? 'Active' : 'Closed'}</Badge> }, { key: 'actions', label: 'Actions', render: row => allowManageUsers ? <span className="avatar-cell"><Button size="sm" icon={Edit} onClick={() => open(row)}>Edit</Button><Button size="sm" icon={row.isActive ? ToggleRight : ToggleLeft} onClick={() => notify('Use Edit to change status when backend DTO supports it')}>{row.isActive ? 'Disable' : 'Enable'}</Button><Button size="sm" icon={Trash2} onClick={() => remove(row)}>Delete</Button></span> : <span className="permission-note compact">Lecture seule</span> }]} />
      </Card>
      {modal && <Modal title={modal === 'edit' ? 'Edit User' : 'Add User'} onClose={() => setModal(null)} footer={<Button variant="primary" icon={Plus} onClick={submit}>Save User</Button>}><form className="form-grid" onSubmit={submit}><Field label="Name"><input value={form.name || ''} onChange={event => setForm(current => ({ ...current, name: event.target.value }))} /></Field><Field label="Email"><input value={form.email || ''} onChange={event => setForm(current => ({ ...current, email: event.target.value }))} /></Field><Field label="Role"><select value={form.role || ''} onChange={event => setForm(current => ({ ...current, role: event.target.value }))}><option value="">Select role</option><option>Admin</option><option>SAV</option><option>Technician</option><option>Client</option></select></Field>{modal === 'new' && <Field label="Temporary Password"><input value={form.password || ''} onChange={event => setForm(current => ({ ...current, password: event.target.value }))} placeholder="ChangeMe123!" /></Field>}<Field label="Company"><input value={form.company || ''} onChange={event => setForm(current => ({ ...current, company: event.target.value }))} /></Field></form></Modal>}
    </section>
  );
}
