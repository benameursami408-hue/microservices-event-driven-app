import {
  AlertTriangle,
  CalendarDays,
  CheckCircle,
  Clock,
  ClipboardList,
  Eye,
  FileText,
  MapPin,
  Play,
  RefreshCw,
  Search,
  UserRound,
  Wrench
} from 'lucide-react';
import { useMemo, useState } from 'react';
import { ApiErrorState } from '../components/common/ApiErrorState';
import { Badge, Button, Card, Field, Modal, SearchInput } from '../components/ui';
import { getInterventionStatusTone } from '../api/mappers/interventionMapper';
import { useInterventions } from '../hooks/useInterventions';
import { getFriendlyApiError } from '../utils/errorMessages';
import { isAdmin, isSav, isTechnician } from '../utils/roleAccess';

const filters = ['Tous', 'Planifiées', 'En cours', 'Terminées', 'En retard'];

export function InterventionsPage({ user, notify, navigate }) {
  const technicianMode = isTechnician(user);
  const savOrAdmin = isSav(user) || isAdmin(user);
  const interventionResource = useInterventions(user);
  const [query, setQuery] = useState('');
  const [filter, setFilter] = useState('Tous');
  const [selectedId, setSelectedId] = useState('');
  const [reportModal, setReportModal] = useState(null);
  const [reportForm, setReportForm] = useState({
    summary: '',
    outcome: 'Solved',
    nextStep: '',
    customerPresent: true,
    needsReplanning: false
  });

  const interventions = interventionResource.interventions || [];

  const visibleInterventions = useMemo(() => {
    const needle = query.trim().toLowerCase();
    return interventions
      .filter(item => {
        if (filter === 'En retard') return item.isLate;
        if (filter === 'Planifiées') return item.status === 'Planifiée' || item.status === 'En attente';
        if (filter === 'Terminées') return item.status === 'Terminée';
        if (filter === 'En cours') return item.status === 'En cours';
        return true;
      })
      .filter(item => {
        if (!needle) return true;
        return [
          item.reference,
          item.reclamation,
          item.reclamationId,
          item.client,
          item.address,
          item.product,
          item.equipment,
          item.description,
          item.status
        ].join(' ').toLowerCase().includes(needle);
      });
  }, [filter, interventions, query]);

  const selected = visibleInterventions.find(item => item.uid === selectedId || item.technicalId === selectedId)
    || interventions.find(item => item.uid === selectedId || item.technicalId === selectedId)
    || null;

  const stats = useMemo(() => {
    const today = new Date();
    const sameDay = value => {
      if (!value) return false;
      const date = new Date(value);
      if (Number.isNaN(date.getTime())) return false;
      return date.getFullYear() === today.getFullYear()
        && date.getMonth() === today.getMonth()
        && date.getDate() === today.getDate();
    };

    return {
      total: interventions.length,
      today: interventions.filter(item => sameDay(item.scheduledAt || item.startedAt || item.createdAt)).length,
      inProgress: interventions.filter(item => item.status === 'En cours').length,
      completed: interventions.filter(item => item.status === 'Terminée').length
    };
  }, [interventions]);

  function openCompleteModal(intervention) {
    setReportModal(intervention);
    setReportForm({
      summary: intervention.reportSummary || intervention.description || '',
      outcome: intervention.outcome || 'Solved',
      nextStep: '',
      customerPresent: true,
      needsReplanning: false
    });
  }

  function canStart(intervention) {
    const allowed = intervention.allowedActions || [];
    return allowed.includes('START_INTERVENTION') || ['Planifiée', 'En attente'].includes(intervention.status);
  }

  function canComplete(intervention) {
    const allowed = intervention.allowedActions || [];
    return allowed.includes('COMPLETE_INTERVENTION') || intervention.status === 'En cours';
  }

  async function handleStart(intervention) {
    try {
      await interventionResource.start(intervention.technicalId);
      notify('Intervention démarrée.');
    } catch (error) {
      notify(getFriendlyApiError(error), 'error');
    }
  }

  async function handleComplete(event) {
    event.preventDefault();
    if (!reportModal) return;

    try {
      const summary = reportForm.summary.trim() || 'Rapport technicien complété depuis SAV Pro.';
      await interventionResource.addDiagnostic(reportModal.technicalId, {
        category: 'Rapport technicien',
        summary,
        requiresFollowUp: Boolean(reportForm.needsReplanning)
      });

      await interventionResource.complete(reportModal.technicalId, {
        outcome: reportForm.outcome,
        needsReplanning: reportForm.needsReplanning
      });

      await interventionResource.createVisitReport({
        interventionId: reportModal.technicalId,
        summary,
        outcome: reportForm.outcome,
        customerPresent: reportForm.customerPresent,
        nextStep: reportForm.nextStep,
        needsReplanning: reportForm.needsReplanning
      });

      setReportModal(null);
      notify('Intervention terminée et rapport enregistré.');
    } catch (error) {
      notify(getFriendlyApiError(error), 'error');
    }
  }

  if (interventionResource.loading) {
    return (
      <section className="interventions-page">
        <InterventionsHeader technicianMode={technicianMode} />
        <div className="intervention-stats-grid skeleton-grid">
          {Array.from({ length: 4 }).map((_, index) => <div className="intervention-skeleton-card" key={index} />)}
        </div>
        <div className="intervention-skeleton-list">
          {Array.from({ length: 4 }).map((_, index) => <div className="intervention-skeleton-row" key={index} />)}
        </div>
      </section>
    );
  }

  if (interventionResource.error) {
    return (
      <section className="interventions-page">
        <InterventionsHeader technicianMode={technicianMode} />
        <ApiErrorState
          status={interventionResource.errorStatus}
          title={errorTitle(interventionResource.errorStatus)}
          message={errorMessage(interventionResource.errorStatus)}
          onRetry={interventionResource.reload}
        />
      </section>
    );
  }

  return (
    <section className="interventions-page">
      <InterventionsHeader technicianMode={technicianMode} />

      <div className="intervention-stats-grid">
        <InterventionStat icon={ClipboardList} label="Total interventions" value={stats.total} />
        <InterventionStat icon={CalendarDays} label="Aujourd’hui" value={stats.today} />
        <InterventionStat icon={Clock} label="En cours" value={stats.inProgress} />
        <InterventionStat icon={CheckCircle} label="Terminées" value={stats.completed} />
      </div>

      <Card className="interventions-list-card">
        <div className="interventions-toolbar">
          <SearchInput value={query} onChange={setQuery} placeholder="Rechercher par référence, client, réclamation..." />
          <div className="intervention-filter-tabs" role="tablist" aria-label="Filtrer les interventions">
            {filters.map(item => (
              <button type="button" key={item} className={filter === item ? 'active' : ''} onClick={() => setFilter(item)}>
                {item}
              </button>
            ))}
          </div>
          <Button icon={RefreshCw} onClick={interventionResource.reload}>Actualiser</Button>
        </div>

        {interventions.length === 0 ? (
          <EmptyInterventions technicianMode={technicianMode} />
        ) : visibleInterventions.length === 0 ? (
          <div className="intervention-empty-card compact">
            <Search size={28} />
            <h3>Aucune intervention ne correspond au filtre</h3>
            <p>Essayez une autre recherche ou affichez toutes les interventions.</p>
            <Button onClick={() => { setQuery(''); setFilter('Tous'); }}>Réinitialiser</Button>
          </div>
        ) : (
          <div className="intervention-card-list">
            {visibleInterventions.map(intervention => (
              <article className="technician-intervention-card" key={intervention.uid}>
                <div className="intervention-card-main">
                  <div className="intervention-reference-row">
                    <span className="intervention-ref">{intervention.reference}</span>
                    {intervention.isLate ? <Badge tone="danger">En retard</Badge> : <Badge tone={getInterventionStatusTone(intervention.status, intervention)}>{intervention.status}</Badge>}
                    <Badge>{intervention.priority}</Badge>
                  </div>
                  <h3>{intervention.equipment}</h3>
                  <p>{intervention.description}</p>
                  <div className="intervention-meta-grid">
                    <Meta icon={ClipboardList} label="Réclamation" value={intervention.reclamation || intervention.reclamationId || '-'} />
                    <Meta icon={UserRound} label="Client" value={intervention.client || '-'} />
                    <Meta icon={MapPin} label="Adresse" value={intervention.address || 'Adresse non renseignée'} />
                    <Meta icon={CalendarDays} label="Date prévue" value={intervention.scheduledLabel || 'Date non renseignée'} />
                  </div>
                </div>
                <div className="intervention-card-actions">
                  <Button icon={Eye} onClick={() => setSelectedId(intervention.uid)}>Voir détails</Button>
                  {technicianMode && canStart(intervention) ? <Button icon={Play} variant="primary" onClick={() => handleStart(intervention)}>Démarrer</Button> : null}
                  {technicianMode && canComplete(intervention) ? <Button icon={FileText} variant="primary" onClick={() => openCompleteModal(intervention)}>Terminer / Rapport</Button> : null}
                  {savOrAdmin ? <Button onClick={() => navigate('planning')}>Planning</Button> : null}
                </div>
              </article>
            ))}
          </div>
        )}
      </Card>

      {selected ? (
        <InterventionDetailsModal
          intervention={selected}
          technicianMode={technicianMode}
          onClose={() => setSelectedId('')}
          onStart={() => handleStart(selected)}
          onComplete={() => openCompleteModal(selected)}
          canStart={canStart(selected)}
          canComplete={canComplete(selected)}
        />
      ) : null}

      {reportModal ? (
        <Modal
          title={`Terminer ${reportModal.reference}`}
          onClose={() => setReportModal(null)}
          footer={<Button variant="primary" icon={CheckCircle} onClick={handleComplete}>Valider la fin d’intervention</Button>}
        >
          <form className="intervention-report-form" onSubmit={handleComplete}>
            <Field label="Résumé du rapport">
              <textarea
                value={reportForm.summary}
                onChange={event => setReportForm(current => ({ ...current, summary: event.target.value }))}
                placeholder="Décrivez le diagnostic, les actions réalisées et l’état final."
              />
            </Field>
            <Field label="Résultat">
              <select value={reportForm.outcome} onChange={event => setReportForm(current => ({ ...current, outcome: event.target.value }))}>
                <option value="Solved">Résolu</option>
                <option value="TemporaryFix">Correction temporaire</option>
                <option value="NeedsReplanning">Besoin de replanification</option>
                <option value="NeedsPart">Besoin de pièce</option>
                <option value="UnableToAccess">Accès impossible</option>
                <option value="CustomerAbsent">Client absent</option>
                <option value="NotRepairable">Non réparable</option>
              </select>
            </Field>
            <Field label="Prochaine étape">
              <input
                value={reportForm.nextStep}
                onChange={event => setReportForm(current => ({ ...current, nextStep: event.target.value }))}
                placeholder="Optionnel : prochaine action SAV."
              />
            </Field>
            <label className="intervention-checkbox">
              <input
                type="checkbox"
                checked={reportForm.customerPresent}
                onChange={event => setReportForm(current => ({ ...current, customerPresent: event.target.checked }))}
              />
              Client présent pendant l’intervention
            </label>
            <label className="intervention-checkbox">
              <input
                type="checkbox"
                checked={reportForm.needsReplanning}
                onChange={event => setReportForm(current => ({ ...current, needsReplanning: event.target.checked }))}
              />
              Nécessite une replanification
            </label>
          </form>
        </Modal>
      ) : null}
    </section>
  );
}

function InterventionsHeader({ technicianMode }) {
  return (
    <div className="page-title-row interventions-title-row">
      <div>
        <span className="eyebrow">{technicianMode ? 'Technician workspace' : 'Field operations'}</span>
        <h1>{technicianMode ? 'Mes interventions' : 'Interventions'}</h1>
        <p>{technicianMode ? 'Consultez et suivez les interventions qui vous sont assignées.' : 'Suivez les interventions planifiées et leur avancement terrain.'}</p>
      </div>
    </div>
  );
}

function InterventionStat({ icon: Icon, label, value }) {
  return (
    <Card className="intervention-stat-card">
      <span><Icon size={22} /></span>
      <div>
        <strong>{value}</strong>
        <small>{label}</small>
      </div>
    </Card>
  );
}

function EmptyInterventions({ technicianMode }) {
  return (
    <div className="intervention-empty-card">
      <span><CalendarDays size={34} /></span>
      <h3>{technicianMode ? 'Aucune intervention assignée' : 'Aucune intervention disponible'}</h3>
      <p>
        {technicianMode
          ? 'Vous n’avez pas encore d’intervention planifiée. Les nouvelles missions apparaîtront ici dès qu’elles seront assignées par le service SAV.'
          : 'Aucune intervention n’est actuellement disponible dans le backend.'}
      </p>
    </div>
  );
}

function Meta({ icon: Icon, label, value }) {
  return (
    <div className="intervention-meta-item">
      <Icon size={16} />
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function InterventionDetailsModal({ intervention, technicianMode, onClose, onStart, onComplete, canStart, canComplete }) {
  return (
    <Modal title={`Détail intervention ${intervention.reference}`} onClose={onClose} footer={(
      <>
        {technicianMode && canStart ? <Button icon={Play} onClick={onStart}>Démarrer intervention</Button> : null}
        {technicianMode && canComplete ? <Button variant="primary" icon={FileText} onClick={onComplete}>Marquer terminée / créer rapport</Button> : null}
        <Button onClick={onClose}>Retour liste</Button>
      </>
    )}>
      <div className="intervention-details-modal">
        <div className="intervention-reference-row">
          <Badge tone={getInterventionStatusTone(intervention.status, intervention)}>{intervention.status}</Badge>
          {intervention.isLate ? <Badge tone="danger">En retard</Badge> : null}
          <Badge>{intervention.priority}</Badge>
        </div>
        <div className="intervention-detail-grid">
          <Info label="Référence intervention" value={intervention.reference} />
          <Info label="Réclamation liée" value={intervention.reclamation || intervention.reclamationId || '-'} />
          <Info label="Client" value={intervention.client || '-'} />
          <Info label="Équipement / produit" value={intervention.equipment || '-'} />
          <Info label="Technicien" value={intervention.technician || '-'} />
          <Info label="Date prévue" value={intervention.scheduledLabel || '-'} />
          <Info label="Adresse" value={intervention.address || '-'} />
          <Info label="Appointment ID" value={intervention.appointmentId || '-'} />
        </div>
        <div className="intervention-note-card">
          <strong>Description / notes technicien</strong>
          <p>{intervention.description || 'Aucune note disponible.'}</p>
        </div>
        {intervention.allowedActions?.length ? (
          <div className="intervention-note-card muted">
            <strong>Actions autorisées</strong>
            <p>{intervention.allowedActions.join(', ')}</p>
          </div>
        ) : null}
        {intervention.reportSummary ? (
          <div className="intervention-note-card">
            <strong>Dernier rapport</strong>
            <p>{intervention.reportSummary}</p>
          </div>
        ) : null}
      </div>
    </Modal>
  );
}

function Info({ label, value }) {
  return <div className="intervention-info-pair"><span>{label}</span><strong>{value}</strong></div>;
}

function errorTitle(status) {
  if (status === 401) return 'Session expirée';
  if (status === 403) return 'Accès interventions refusé';
  if (status === 404) return 'Aucune intervention trouvée';
  if (status >= 500) return 'Erreur serveur';
  return 'Serveur inaccessible';
}

function errorMessage(status) {
  if (status === 401) return 'Session expirée. Veuillez vous reconnecter.';
  if (status === 403) return 'Vous n’avez pas accès à ces interventions.';
  if (status === 404) return 'Aucune intervention trouvée.';
  if (status >= 500) return 'Erreur serveur lors du chargement des interventions.';
  return 'Impossible de joindre le serveur.';
}
