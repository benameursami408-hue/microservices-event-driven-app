import { Edit, Plus, RefreshCw, Search, Trash2, Users } from 'lucide-react'
import { useCallback, useEffect, useMemo, useState } from 'react'
import toast from 'react-hot-toast'

import Button from '../components/Button.jsx'
import DataTable from '../components/DataTable.jsx'
import EmptyState from '../components/EmptyState.jsx'
import ErrorState from '../components/ErrorState.jsx'
import FormModal from '../components/FormModal.jsx'
import LoadingState from '../components/LoadingState.jsx'
import MetricCard from '../components/MetricCard.jsx'
import SelectField from '../components/SelectField.jsx'
import StatusBadge from '../components/StatusBadge.jsx'
import TextField from '../components/TextField.jsx'
import { useAuth } from '../hooks/useAuth.js'
import LayoutAdmin from '../layouts/LayoutAdmin.jsx'
import { createUser, deleteUser, listUsers, updateUser } from '../services/adminUsers.service.js'
import { roleLabel } from '../utils/enums.js'

const ROLE_OPTIONS = [
  { value: 'ADMIN', label: 'Admin' },
  { value: 'SAV', label: 'SAV' },
  { value: 'ST', label: 'Technique' },
  { value: 'CLIENT', label: 'Client' },
]

const FILTER_OPTIONS = [{ value: 'ALL', label: 'Tous les roles' }, ...ROLE_OPTIONS]

function buildInitialForm(fixedRole) {
  return {
    firstName: '',
    lastName: '',
    email: '',
    phoneNumber: '',
    address: '',
    password: '',
    isActive: 'true',
    role: fixedRole || 'SAV',
  }
}

function extractApiErrorMessage(err) {
  const detail = err?.response?.data?.detail
  if (detail) return detail

  const errors = err?.response?.data?.errors
  if (errors && typeof errors === 'object') {
    const messages = Object.values(errors)
      .flatMap((value) => (Array.isArray(value) ? value : [value]))
      .filter(Boolean)

    const filteredMessages = messages.filter((message) => message !== 'The dto field is required.')
    if (filteredMessages.length > 0) return filteredMessages.join(' ')
    if (messages.length > 0) return messages.join(' ')
  }

  return err?.response?.data?.title || err?.message
}

function roleCodeFromValue(value, fallback = 'SAV') {
  const normalized = String(value ?? '').toUpperCase()
  if (normalized === '0' || normalized === 'CLIENT') return 'CLIENT'
  if (normalized === '1' || normalized === 'SAV') return 'SAV'
  if (normalized === '2' || normalized === 'ADMIN') return 'ADMIN'
  if (normalized === '3' || normalized === 'ST' || normalized === 'TECHNIQUE') return 'ST'
  return fallback
}

export default function AdminTeamUsers({ roleKey, title, description }) {
  const { user } = useAuth()
  const role = String(user?.role ?? '').toUpperCase()
  const isAdmin = role === 'ADMIN'

  const [items, setItems] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [query, setQuery] = useState('')
  const [roleFilter, setRoleFilter] = useState(roleKey || 'ALL')
  const [statusFilter, setStatusFilter] = useState('ALL')
  const [editingId, setEditingId] = useState(null)
  const [form, setForm] = useState(buildInitialForm(roleKey))
  const [formError, setFormError] = useState('')
  const [formSuccess, setFormSuccess] = useState('')
  const [modalOpen, setModalOpen] = useState(false)
  const [submitting, setSubmitting] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)

    try {
      const data = await listUsers({ role: roleKey || (roleFilter !== 'ALL' ? roleFilter : undefined) })
      setItems(Array.isArray(data) ? data : [])
    } catch (err) {
      const message = extractApiErrorMessage(err)
      setError(message || 'Impossible de charger les utilisateurs.')
    } finally {
      setLoading(false)
    }
  }, [roleFilter, roleKey])

  useEffect(() => {
    void load()
  }, [load])

  const filteredItems = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase()

    return [...items]
      .filter((item) => {
        if (statusFilter === 'ACTIVE' && !item.isActive) return false
        if (statusFilter === 'INACTIVE' && item.isActive) return false
        return true
      })
      .filter((item) => {
        if (!normalizedQuery) return true

        return [item.firstName, item.lastName, item.email, item.phoneNumber]
          .filter(Boolean)
          .some((value) => String(value).toLowerCase().includes(normalizedQuery))
      })
      .sort((a, b) => `${a.lastName} ${a.firstName}`.localeCompare(`${b.lastName} ${b.firstName}`))
  }, [items, query, statusFilter])

  function resetForm() {
    setEditingId(null)
    setForm(buildInitialForm(roleKey))
    setFormError('')
    setFormSuccess('')
  }

  function openCreateModal() {
    resetForm()
    setModalOpen(true)
  }

  function closeModal() {
    setModalOpen(false)
    resetForm()
  }

  function startEdit(item) {
    setEditingId(item.id)
    setForm({
      firstName: item.firstName || '',
      lastName: item.lastName || '',
      email: item.email || '',
      phoneNumber: item.phoneNumber || '',
      address: item.address || '',
      password: '',
      isActive: String(item.isActive ?? true),
      role: roleCodeFromValue(item.role, roleKey || 'SAV'),
    })
    setFormError('')
    setFormSuccess('')
    setModalOpen(true)
  }

  function updateFormField(key, value) {
    setForm((prev) => ({ ...prev, [key]: value }))
  }

  async function handleSubmit(event) {
    event.preventDefault()
    setFormError('')
    setFormSuccess('')

    if (!form.firstName.trim() || !form.lastName.trim() || !form.email.trim() || !form.phoneNumber.trim()) {
      setFormError('Veuillez remplir tous les champs obligatoires.')
      return
    }

    if (!editingId && !form.password.trim()) {
      setFormError('Le mot de passe est obligatoire pour un nouvel utilisateur.')
      return
    }

    const payload = {
      firstName: form.firstName,
      lastName: form.lastName,
      email: form.email,
      phoneNumber: form.phoneNumber,
      address: form.address,
      password: form.password,
      role: roleKey || form.role,
      isActive: form.isActive === 'true',
    }

    setSubmitting(true)

    try {
      if (editingId) {
        await updateUser(editingId, payload)
        setFormSuccess('Utilisateur modifie avec succes.')
        toast.success('Utilisateur modifie.')
      } else {
        await createUser(payload)
        setFormSuccess('Utilisateur cree avec succes.')
        toast.success('Utilisateur cree.')
      }

      await load()
      window.setTimeout(() => {
        closeModal()
      }, 350)
    } catch (err) {
      const message = extractApiErrorMessage(err)
      setFormError(message || 'Operation impossible.')
      toast.error(message || 'Operation impossible.')
    } finally {
      setSubmitting(false)
    }
  }

  async function handleDelete(item) {
    const confirmed = window.confirm(`Supprimer ${item.firstName} ${item.lastName} ?`)
    if (!confirmed) return

    try {
      await deleteUser(item.id)
      toast.success('Utilisateur supprime.')
      await load()
    } catch (err) {
      const message = extractApiErrorMessage(err)
      toast.error(message || 'Suppression impossible.')
    }
  }

  if (!isAdmin) {
    return <ErrorState title="Acces reserve" message="Seul un compte Admin peut gerer les utilisateurs." />
  }

  const activeCount = filteredItems.filter((item) => item.isActive).length
  const inactiveCount = filteredItems.length - activeCount

  return (
    <LayoutAdmin
      title={title}
      description={description}
      meta={
        <>
          <span>{filteredItems.length} utilisateur(s) visibles</span>
          <span className="text-slate-300">|</span>
          <span>{roleKey ? `Role fixe: ${roleLabel(roleKey)}` : 'Tous les roles'}</span>
        </>
      }
      actions={
        <div className="flex flex-wrap gap-3">
          <Button variant="secondary" onClick={load} disabled={loading}>
            <RefreshCw className="h-4 w-4" aria-hidden="true" />
            Actualiser
          </Button>
          <Button onClick={openCreateModal}>
            <Plus className="h-4 w-4" aria-hidden="true" />
            Ajouter
          </Button>
        </div>
      }
    >
      <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
        <MetricCard icon={Users} label="Utilisateurs visibles" value={filteredItems.length} helper="Apres filtres" tone="cyan" />
        <MetricCard icon={Users} label="Actifs" value={activeCount} helper="Comptes actifs" tone="emerald" />
        <MetricCard icon={Users} label="Inactifs" value={inactiveCount} helper="Comptes desactives" tone="slate" />
      </div>

      <section className="surface-solid p-6">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <h2 className="text-lg font-bold text-slate-950">Rechercher et filtrer</h2>
            <p className="mt-1 text-sm text-slate-600">Retrouvez rapidement un utilisateur et gardez les actions principales visibles.</p>
          </div>

          <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3 xl:items-end">
            <TextField
              label="Recherche"
              placeholder="Nom, email, telephone..."
              value={query}
              onChange={(event) => setQuery(event.target.value)}
            />

            {!roleKey ? (
              <SelectField label="Role" value={roleFilter} onChange={(event) => setRoleFilter(event.target.value)}>
                {FILTER_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </SelectField>
            ) : null}

            <SelectField label="Statut" value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)}>
              <option value="ALL">Tous</option>
              <option value="ACTIVE">Actifs</option>
              <option value="INACTIVE">Inactifs</option>
            </SelectField>
          </div>
        </div>
      </section>

      {loading ? (
        <LoadingState title="Chargement des utilisateurs..." />
      ) : error ? (
        <ErrorState message={error} onAction={load} />
      ) : filteredItems.length === 0 ? (
        <EmptyState
          icon={Search}
          title="Aucun utilisateur trouve"
          description="Essayez un autre filtre ou ajoutez un nouveau compte."
          actionLabel="Ajouter un utilisateur"
          onAction={openCreateModal}
        />
      ) : (
        <DataTable
          columns={[
            {
              key: 'name',
              header: 'Utilisateur',
              render: (item) => (
                <div>
                  <div className="font-semibold text-slate-950">
                    {item.firstName} {item.lastName}
                  </div>
                  <div className="mt-1 text-xs text-slate-500">{item.email}</div>
                </div>
              ),
            },
            {
              key: 'phone',
              header: 'Contact',
              render: (item) => item.phoneNumber || '-',
            },
            {
              key: 'role',
              header: 'Role',
              render: (item) => <StatusBadge kind="role" value={item.role} />,
            },
            {
              key: 'status',
              header: 'Statut',
              render: (item) => <StatusBadge kind="active" value={item.isActive} />,
            },
            {
              key: 'actions',
              header: 'Actions',
              headerClassName: 'text-right',
              cellClassName: 'text-right',
              render: (item) => (
                <div className="flex justify-end gap-2">
                  <Button variant="secondary" size="sm" onClick={() => startEdit(item)}>
                    <Edit className="h-4 w-4" aria-hidden="true" />
                    Modifier
                  </Button>
                  <Button variant="danger" size="sm" onClick={() => handleDelete(item)}>
                    <Trash2 className="h-4 w-4" aria-hidden="true" />
                    Supprimer
                  </Button>
                </div>
              ),
            },
          ]}
          rows={filteredItems}
          getRowKey={(item) => item.id}
          renderMobileCard={(item) => (
            <div className="space-y-3">
              <div className="flex items-start justify-between gap-3">
                <div>
                  <div className="font-semibold text-slate-950">
                    {item.firstName} {item.lastName}
                  </div>
                  <div className="mt-1 text-sm text-slate-600">{item.email}</div>
                </div>
                <StatusBadge kind="active" value={item.isActive} />
              </div>

              <div className="flex flex-wrap gap-2">
                <StatusBadge kind="role" value={item.role} />
                {item.phoneNumber ? <StatusBadge label={item.phoneNumber} tone="neutral" /> : null}
              </div>

              <div className="flex flex-wrap gap-2">
                <Button variant="secondary" size="sm" onClick={() => startEdit(item)}>
                  <Edit className="h-4 w-4" aria-hidden="true" />
                  Modifier
                </Button>
                <Button variant="danger" size="sm" onClick={() => handleDelete(item)}>
                  <Trash2 className="h-4 w-4" aria-hidden="true" />
                  Supprimer
                </Button>
              </div>
            </div>
          )}
        />
      )}

      <FormModal
        open={modalOpen}
        onClose={closeModal}
        title={editingId ? 'Modifier un utilisateur' : 'Ajouter un utilisateur'}
        description="Les champs obligatoires sont clairement identifies et le payload est nettoye avant envoi."
      >
        <form className="grid grid-cols-1 gap-4 lg:grid-cols-2" onSubmit={handleSubmit}>
          <TextField
            label="Prenom"
            placeholder="Amina"
            value={form.firstName}
            onChange={(event) => updateFormField('firstName', event.target.value)}
            required
          />
          <TextField
            label="Nom"
            placeholder="Ben Salah"
            value={form.lastName}
            onChange={(event) => updateFormField('lastName', event.target.value)}
            required
          />
          <TextField
            label="Email"
            type="email"
            placeholder="sav@entreprise.tn"
            value={form.email}
            onChange={(event) => updateFormField('email', event.target.value)}
            required
          />
          <TextField
            label="Telephone"
            placeholder="+216 22 000 000"
            value={form.phoneNumber}
            onChange={(event) => updateFormField('phoneNumber', event.target.value)}
            required
          />
          <TextField
            label="Adresse"
            placeholder="Tunis"
            value={form.address}
            onChange={(event) => updateFormField('address', event.target.value)}
          />
          <TextField
            label={editingId ? 'Nouveau mot de passe' : 'Mot de passe'}
            type="password"
            placeholder={editingId ? 'Laisser vide pour ne pas changer' : 'Minimum 8 caracteres'}
            value={form.password}
            onChange={(event) => updateFormField('password', event.target.value)}
            required={!editingId}
            hint={editingId ? 'Le mot de passe vide n est pas envoye au backend.' : undefined}
          />

          {!roleKey ? (
            <SelectField
              label="Role"
              value={form.role}
              onChange={(event) => updateFormField('role', event.target.value)}
              required
            >
              {ROLE_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </SelectField>
          ) : null}

          <SelectField label="Statut" value={form.isActive} onChange={(event) => updateFormField('isActive', event.target.value)}>
            <option value="true">Actif</option>
            <option value="false">Inactif</option>
          </SelectField>

          <div className="lg:col-span-2">
            {formError ? <div className="notice-error">{formError}</div> : null}
            {!formError && formSuccess ? <div className="notice-info">{formSuccess}</div> : null}
          </div>

          <div className="flex flex-col gap-2 sm:flex-row sm:justify-end lg:col-span-2">
            <Button variant="secondary" type="button" onClick={closeModal}>
              Retour
            </Button>
            <Button type="submit" disabled={submitting}>
              {submitting ? 'Enregistrement...' : editingId ? 'Modifier' : 'Ajouter'}
            </Button>
          </div>
        </form>
      </FormModal>
    </LayoutAdmin>
  )
}
