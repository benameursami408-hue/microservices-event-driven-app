import { AlertTriangle, Ban, CalendarClock, CheckCircle2, Save, Trash2, Upload, UserCheck, XCircle } from 'lucide-react'

import Badge from '../Badge.jsx'
import Button from '../Button.jsx'
import MetricCard from '../MetricCard.jsx'
import SelectField from '../SelectField.jsx'
import TextAreaField from '../TextAreaField.jsx'
import TextField from '../TextField.jsx'
import { formatDateTime } from '../../utils/format.js'
import {
  Priority,
  priorityBadgeClasses,
  priorityLabel,
  prioritySourceLabel,
  reclamationStatusBadgeClasses,
  reclamationStatusLabel,
  slaStatusBadgeClasses,
  slaStatusLabel,
} from '../../utils/enums.js'

function getAppointmentStatusLabel(appointment) {
  if (!appointment) return 'Non planifiee'
  return appointment.statusLabel || appointment.status || 'Planifiee'
}

function getInterventionSummary(intervention) {
  if (!intervention) return 'Aucune intervention'
  return intervention.outcome || intervention.status || 'En cours'
}

export function ReclamationKpiGrid({ item, priorityInfo, slaInfo, allowedActionsCount }) {
  return (
    <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">
      <MetricCard
        icon={CalendarClock}
        label="Priorite"
        value={priorityLabel(priorityInfo?.priority ?? item.priority)}
        helper={priorityInfo ? `Score ${priorityInfo.priorityScore}` : 'Calculee depuis le workflow'}
        tone={Number(priorityInfo?.priority ?? item.priority) >= 2 ? 'amber' : 'cyan'}
      />
      <MetricCard
        icon={AlertTriangle}
        label="SLA"
        value={slaInfo ? slaStatusLabel(slaInfo.slaStatus) : '—'}
        helper={slaInfo?.activeTarget ? `Objectif actif: ${slaInfo.activeTarget}` : 'Aucun objectif actif'}
        tone={Number(slaInfo?.slaStatus) === 2 ? 'rose' : Number(slaInfo?.slaStatus) === 1 ? 'amber' : 'emerald'}
      />
      <MetricCard
        icon={UserCheck}
        label="Affectation"
        value={item.savName || 'Non affectee'}
        helper={item.technicianName ? `Technicien: ${item.technicianName}` : 'Aucun technicien'}
        tone="slate"
      />
      <MetricCard
        icon={CheckCircle2}
        label="Actions autorisees"
        value={allowedActionsCount}
        helper="Controlees par le backend selon le role"
        tone="cyan"
      />
    </div>
  )
}

export function ReclamationDetailForm({
  canEditDetails,
  errors,
  handleSubmit,
  isSubmitting,
  onUpdate,
  productImageFile,
  productImageUrl,
  proofIsImage,
  purchaseProofFile,
  purchaseProofUrl,
  register,
  removeProductImage,
  removePurchaseProof,
  setProductImageFile,
  setPurchaseProofFile,
  setRemoveProductImage,
  setRemovePurchaseProof,
}) {
  return (
    <div className="surface-solid p-6">
      <div className="text-lg font-bold text-slate-900">Details</div>
      <form className="mt-4 space-y-4" onSubmit={handleSubmit(onUpdate)}>
        <TextAreaField
          label="Description"
          error={errors.description?.message}
          disabled={!canEditDetails}
          {...register('description')}
        />

        <SelectField
          label="Gravite declaree"
          error={errors.priority?.message}
          disabled={!canEditDetails}
          {...register('priority')}
        >
          <option value={Priority.LOW}>{priorityLabel(Priority.LOW)}</option>
          <option value={Priority.MEDUIM}>{priorityLabel(Priority.MEDUIM)}</option>
          <option value={Priority.HIGH}>{priorityLabel(Priority.HIGH)}</option>
          <option value={Priority.URGENT}>{priorityLabel(Priority.URGENT)}</option>
        </SelectField>

        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
          <TextField
            label="Nombre de relances"
            type="number"
            min="0"
            max="100"
            disabled={!canEditDetails}
            error={errors.followUpCount?.message}
            {...register('followUpCount')}
          />

          <label className="input-shell block">
            <div className="input-label">Blocage client</div>
            <div className="flex min-h-[52px] items-center rounded-[26px] border border-slate-200 bg-white px-4 py-3">
              <input
                type="checkbox"
                className="h-4 w-4 rounded border-slate-300 text-cyan-700 focus:ring-cyan-200"
                disabled={!canEditDetails}
                {...register('isBlocking')}
              />
              <span className="ml-3 text-sm text-slate-700">Escalader cette reclamation comme bloquante.</span>
            </div>
          </label>
        </div>

        <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
          <div className="text-sm font-semibold text-slate-900">Informations produit</div>
          <div className="mt-4 grid grid-cols-1 gap-4 md:grid-cols-2">
            <TextField label="Produit" disabled={!canEditDetails} {...register('productName')} />
            <TextField label="Code-barres" disabled={!canEditDetails} {...register('barcode')} />
            <TextField label="Marque" disabled={!canEditDetails} {...register('brand')} />
            <TextField label="Modele" disabled={!canEditDetails} {...register('model')} />
            <TextField label="Numero de serie" disabled={!canEditDetails} {...register('serialNumber')} />
            <TextField label="Reference produit" disabled={!canEditDetails} {...register('productReference')} />
            <TextField label="Vendeur / magasin" disabled={!canEditDetails} {...register('sellerName')} />
            <TextField label="Date d'achat" type="date" disabled={!canEditDetails} {...register('purchaseDate')} />
          </div>
        </div>

        <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
          <div className="text-sm font-semibold text-slate-900">Pieces jointes</div>
          <div className="mt-4 grid grid-cols-1 gap-4 md:grid-cols-2">
            <AttachmentEditor
              accept="image/*"
              alt="Produit"
              canEditDetails={canEditDetails}
              currentUrl={productImageUrl}
              file={productImageFile}
              isImage
              label="Image produit"
              removeCurrent={removeProductImage}
              removeLabel="Supprimer l'image actuelle"
              removedLabel="L'image actuelle sera supprimee a l'enregistrement."
              setFile={setProductImageFile}
              setRemoveCurrent={setRemoveProductImage}
            />
            <AttachmentEditor
              accept="image/*,application/pdf"
              alt="Preuve d'achat"
              canEditDetails={canEditDetails}
              currentUrl={purchaseProofUrl}
              file={purchaseProofFile}
              isImage={proofIsImage}
              label="Preuve d'achat"
              removeCurrent={removePurchaseProof}
              removeLabel="Supprimer la preuve actuelle"
              removedLabel="La preuve actuelle sera supprimee a l'enregistrement."
              setFile={setPurchaseProofFile}
              setRemoveCurrent={setRemovePurchaseProof}
            />
          </div>
          <div className="mt-4 inline-flex items-center gap-2 text-xs font-semibold text-slate-600">
            <Upload className="h-4 w-4" aria-hidden="true" />
            Ajouter un nouveau fichier remplace la piece jointe existante.
          </div>
        </div>

        {errors.root?.message ? (
          <div className="rounded-xl border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-800">
            {errors.root.message}
          </div>
        ) : null}

        <div className="flex justify-end">
          <Button type="submit" disabled={isSubmitting || !canEditDetails}>
            <Save className="h-4 w-4" aria-hidden="true" />
            {isSubmitting ? 'Enregistrement...' : 'Enregistrer'}
          </Button>
        </div>
      </form>
    </div>
  )
}

function AttachmentEditor({
  accept,
  alt,
  canEditDetails,
  currentUrl,
  file,
  isImage,
  label,
  removeCurrent,
  removeLabel,
  removedLabel,
  setFile,
  setRemoveCurrent,
}) {
  return (
    <div className="space-y-3">
      <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">{label}</div>
      {currentUrl && !removeCurrent ? (
        isImage ? (
          <div className="overflow-hidden rounded-xl border border-slate-200 bg-white">
            <img src={currentUrl} alt={alt} className="h-40 w-full object-cover" />
          </div>
        ) : (
          <div className="rounded-xl border border-slate-200 bg-white px-4 py-6 text-sm">
            <a className="link font-semibold" href={currentUrl} target="_blank" rel="noreferrer">
              Ouvrir le document actuel
            </a>
          </div>
        )
      ) : (
        <div className="rounded-xl border border-dashed border-slate-200 bg-white px-4 py-6 text-sm text-slate-500">
          Aucune piece jointe.
        </div>
      )}
      <input
        type="file"
        accept={accept}
        disabled={!canEditDetails}
        onChange={(event) => {
          const nextFile = event.target.files?.[0] || null
          setFile(nextFile)
          if (nextFile) setRemoveCurrent(false)
        }}
      />
      {file ? <div className="text-xs text-slate-500">{file.name}</div> : null}
      {currentUrl && !removeCurrent ? (
        <Button
          type="button"
          variant="secondary"
          disabled={!canEditDetails}
          onClick={() => {
            setFile(null)
            setRemoveCurrent(true)
          }}
        >
          {removeLabel}
        </Button>
      ) : null}
      {removeCurrent ? <div className="text-xs font-semibold text-rose-700">{removedLabel}</div> : null}
    </div>
  )
}

export function ReclamationPrioritySlaCard({ item, priorityInfo, slaInfo }) {
  return (
    <div className="surface-solid p-6">
      <div className="text-lg font-bold text-slate-900">Priorite et SLA</div>
      <div className="mt-4 space-y-4 text-sm">
        <div className="flex flex-wrap items-center gap-2">
          <Badge className={priorityBadgeClasses(priorityInfo?.priority ?? item.priority)}>
            {priorityLabel(priorityInfo?.priority ?? item.priority)}
          </Badge>
          {priorityInfo ? (
            <Badge className="bg-white text-slate-700 ring-slate-200">
              Score {priorityInfo.priorityScore} - {prioritySourceLabel(priorityInfo.prioritySource)}
            </Badge>
          ) : null}
          {slaInfo ? (
            <Badge className={slaStatusBadgeClasses(slaInfo.slaStatus)}>{slaStatusLabel(slaInfo.slaStatus)}</Badge>
          ) : null}
        </div>

        {priorityInfo?.priorityReasons?.length ? (
          <div className="flex flex-wrap gap-2">
            {priorityInfo.priorityReasons.map((reason) => (
              <Badge key={reason} className="bg-amber-50 text-amber-800 ring-amber-200">{reason}</Badge>
            ))}
          </div>
        ) : (
          <div className="text-slate-600">Aucune raison de priorite explicite pour le moment.</div>
        )}

        {slaInfo ? (
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
            <SlaDate label="Premiere reponse" value={slaInfo.firstResponseDeadline} />
            <SlaDate label="Planification" value={slaInfo.planningDeadline} />
            <SlaDate label="Resolution" value={slaInfo.resolutionDeadline} />
            <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
              <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Objectif actif</div>
              <div className="mt-1 font-semibold text-slate-900">
                {slaInfo.activeTarget || '—'}
                {slaInfo.activeDeadline ? ` - ${formatDateTime(slaInfo.activeDeadline)}` : ''}
              </div>
              {slaInfo.slaBreachedAt ? (
                <div className="mt-2 text-xs font-semibold text-rose-700">Depasse le {formatDateTime(slaInfo.slaBreachedAt)}</div>
              ) : null}
            </div>
          </div>
        ) : (
          <div className="text-slate-600">Aucun snapshot SLA disponible.</div>
        )}
      </div>
    </div>
  )
}

function SlaDate({ label, value }) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
      <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">{label}</div>
      <div className="mt-1 font-semibold text-slate-900">{value ? formatDateTime(value) : '—'}</div>
    </div>
  )
}

export function ReclamationWorkflowCard({
  actionBusy,
  canAssign,
  canCancel,
  canClose,
  canDelete,
  canOverridePriority,
  canRecalculatePriority,
  canReject,
  canRequestPlanning,
  closeComment,
  onAssign,
  onCancel,
  onClose,
  onDelete,
  onOverridePriority,
  onRecalculatePriority,
  onReject,
  onRequestPlanning,
  overridePriorityReason,
  overridePriorityValue,
  rejectReason,
  setCloseComment,
  setOverridePriorityReason,
  setOverridePriorityValue,
  setRejectReason,
}) {
  const hasAnyAction = canAssign || canRequestPlanning || canRecalculatePriority || canOverridePriority || canClose || canDelete || canCancel || canReject

  return (
    <div className="surface-solid p-6">
      <div className="text-lg font-bold text-slate-900">Workflow</div>
      <div className="mt-4 space-y-5">
        {canAssign ? (
          <ActionBox title="Affecter" description="Prendre le dossier en charge comme SAV ou ADMIN.">
            <Button onClick={onAssign} disabled={actionBusy}>
              <UserCheck className="h-4 w-4" aria-hidden="true" />
              M'affecter
            </Button>
          </ActionBox>
        ) : null}

        {canRequestPlanning ? (
          <ActionBox title="Demander la planification" description="Envoyer la reclamation affectee vers le workflow de planning.">
            <Button onClick={onRequestPlanning} disabled={actionBusy}>
              <CalendarClock className="h-4 w-4" aria-hidden="true" />
              Demander la planification
            </Button>
          </ActionBox>
        ) : null}

        {canRecalculatePriority ? (
          <ActionBox tone="amber" title="Recalculer la priorite" description="Recalculer le score, les raisons et le risque SLA depuis les regles backend.">
            <Button variant="secondary" disabled={actionBusy} onClick={onRecalculatePriority}>
              <AlertTriangle className="h-4 w-4" aria-hidden="true" />
              Recalculer
            </Button>
          </ActionBox>
        ) : null}

        {canOverridePriority ? (
          <ActionBox tone="amber" title="Override manuel de priorite" description="A utiliser seulement si le contexte metier impose une escalade explicite.">
            <form className="mt-3 space-y-3" onSubmit={onOverridePriority}>
              <SelectField label="Priorite" value={overridePriorityValue} onChange={(e) => setOverridePriorityValue(e.target.value)}>
                <option value={Priority.LOW}>LOW</option>
                <option value={Priority.MEDUIM}>MEDIUM</option>
                <option value={Priority.HIGH}>HIGH</option>
                <option value={Priority.URGENT}>CRITICAL</option>
              </SelectField>
              <TextField
                label="Raison de l'override"
                value={overridePriorityReason}
                onChange={(e) => setOverridePriorityReason(e.target.value)}
              />
              <div className="flex justify-end">
                <Button type="submit" disabled={actionBusy}>
                  <Save className="h-4 w-4" aria-hidden="true" />
                  Appliquer
                </Button>
              </div>
            </form>
          </ActionBox>
        ) : null}

        {canClose ? (
          <ActionBox title="Cloturer" description="Le SAV cloture une reclamation resolue.">
            <form className="mt-3 space-y-3" onSubmit={onClose}>
              <TextField
                label="Commentaire optionnel"
                value={closeComment}
                onChange={(e) => setCloseComment(e.target.value)}
              />
              <div className="flex justify-end">
                <Button type="submit" disabled={actionBusy}>
                  <CheckCircle2 className="h-4 w-4" aria-hidden="true" />
                  Cloturer
                </Button>
              </div>
            </form>
          </ActionBox>
        ) : null}

        {canDelete ? (
          <ActionBox tone="rose" title="Supprimer" description="Supprimer definitivement la reclamation si elle est encore ouverte ou annulee.">
            <Button variant="danger" disabled={actionBusy} onClick={onDelete}>
              <Trash2 className="h-4 w-4" aria-hidden="true" />
              Supprimer definitivement
            </Button>
          </ActionBox>
        ) : null}

        {canCancel ? (
          <ActionBox tone="rose" title="Annuler" description="Le client peut annuler uniquement quand le ticket est encore ouvert.">
            <Button variant="danger" onClick={onCancel} disabled={actionBusy}>
              <XCircle className="h-4 w-4" aria-hidden="true" />
              Annuler la reclamation
            </Button>
          </ActionBox>
        ) : null}

        {canReject ? (
          <ActionBox tone="rose" title="Rejeter" description="Rejeter avec une raison claire.">
            <form className="mt-3 space-y-3" onSubmit={onReject}>
              <TextField
                label="Raison"
                value={rejectReason}
                onChange={(e) => setRejectReason(e.target.value)}
              />
              <div className="flex justify-end">
                <Button variant="danger" type="submit" disabled={actionBusy}>
                  <Ban className="h-4 w-4" aria-hidden="true" />
                  Rejeter
                </Button>
              </div>
            </form>
          </ActionBox>
        ) : null}

        {!hasAnyAction ? (
          <div className="text-sm text-slate-600">Aucune action disponible pour votre role a ce statut.</div>
        ) : null}
      </div>
    </div>
  )
}

function ActionBox({ children, description, title, tone = 'slate' }) {
  const styles = {
    amber: 'border-amber-200 bg-amber-50 text-amber-900',
    rose: 'border-rose-200 bg-rose-50 text-rose-900',
    slate: 'border-slate-200 bg-slate-50 text-slate-900',
  }
  const textStyles = {
    amber: 'text-amber-800',
    rose: 'text-rose-800',
    slate: 'text-slate-600',
  }

  return (
    <div className={`rounded-2xl border p-4 ${styles[tone]}`}>
      <div className="text-sm font-semibold">{title}</div>
      <div className={`mt-1 text-sm ${textStyles[tone]}`}>{description}</div>
      <div className="mt-3">{children}</div>
    </div>
  )
}

export function ReclamationHistoryCard({ history }) {
  return (
    <div className="surface-solid p-6">
      <div className="text-lg font-bold text-slate-900">Historique</div>
      {history.length === 0 ? (
        <div className="mt-3 text-sm text-slate-600">Aucun historique pour le moment.</div>
      ) : (
        <div className="mt-4 space-y-3">
          {history.map((h) => (
            <div key={h.id} className="rounded-2xl border border-slate-200 bg-white p-4">
              <div className="flex flex-wrap items-center gap-2 text-sm">
                <Badge className={reclamationStatusBadgeClasses(h.fromStatus)}>
                  {reclamationStatusLabel(h.fromStatus)}
                </Badge>
                <span className="text-slate-400">&rarr;</span>
                <Badge className={reclamationStatusBadgeClasses(h.toStatus)}>
                  {reclamationStatusLabel(h.toStatus)}
                </Badge>
                <span className="text-slate-300">|</span>
                <span className="font-semibold text-slate-800">{h.actorRole}</span>
                <span className="text-slate-500">#{h.actorUserId}</span>
              </div>
              <div className="mt-2 text-xs text-slate-600">{formatDateTime(h.occurredAt)}</div>
              {h.comment ? <div className="mt-2 text-sm text-slate-700">{h.comment}</div> : null}
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

export function ReclamationSidePanel({
  appointment,
  item,
  latestIntervention,
  productImageUrl,
  proofIsImage,
  purchaseProofUrl,
}) {
  return (
    <div className="space-y-6">
      <ProductSnapshot
        item={item}
        productImageUrl={productImageUrl}
        proofIsImage={proofIsImage}
        purchaseProofUrl={purchaseProofUrl}
      />
      <OperationalSnapshot appointment={appointment} item={item} latestIntervention={latestIntervention} />
    </div>
  )
}

function ProductSnapshot({ item, productImageUrl, proofIsImage, purchaseProofUrl }) {
  return (
    <div className="surface-solid p-6">
      <div className="text-lg font-bold text-slate-900">Informations produit</div>
      <div className="mt-4 grid grid-cols-1 gap-3 text-sm">
        <SnapshotRow label="Produit" value={item.productName || '—'} />
        <SnapshotRow label="Marque / modele" value={[item.brand, item.model].filter(Boolean).join(' / ') || '—'} />
        <SnapshotRow label="Reference / serie" value={[item.productReference, item.serialNumber].filter(Boolean).join(' / ') || '—'} />
        <SnapshotRow label="Code-barres" value={item.barcode || '—'} />
        <SnapshotRow label="Date d'achat" value={item.purchaseDate ? new Date(item.purchaseDate).toLocaleDateString('fr-FR') : '—'} />
        <SnapshotRow label="Vendeur / magasin" value={item.sellerName || '—'} />

        {productImageUrl ? (
          <div>
            <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Image</div>
            <div className="mt-2 overflow-hidden rounded-xl border border-slate-200">
              <img src={productImageUrl} alt="Produit" className="h-48 w-full object-cover" />
            </div>
          </div>
        ) : null}

        {purchaseProofUrl ? (
          <div>
            <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Preuve d'achat</div>
            {proofIsImage ? (
              <div className="mt-2 overflow-hidden rounded-xl border border-slate-200">
                <img src={purchaseProofUrl} alt="Preuve d'achat" className="h-40 w-full object-cover" />
              </div>
            ) : (
              <div className="mt-2">
                <a className="link font-semibold" href={purchaseProofUrl} target="_blank" rel="noreferrer">
                  Voir la facture
                </a>
              </div>
            )}
          </div>
        ) : null}
      </div>
    </div>
  )
}

function OperationalSnapshot({ appointment, item, latestIntervention }) {
  return (
    <div className="surface-solid p-6">
      <div className="text-lg font-bold text-slate-900">Vue operationnelle</div>
      <div className="mt-4 space-y-3 text-sm">
        <SnapshotRow label="Creation" value={formatDateTime(item.createdAt)} />
        <SnapshotRow label="Derniere mise a jour" value={formatDateTime(item.updatedAt)} />
        <SnapshotRow label="Client" value={`${item.clientName} (ID: ${item.clientId})`} />
        <SnapshotRow label="SAV" value={`${item.savName || 'Non affecte'} ${item.savId ? `(ID: ${item.savId})` : ''}`} />
        <SnapshotRow label="Technicien" value={`${item.technicianName || 'Non affecte'} ${item.technicianId ? `(ID: ${item.technicianId})` : ''}`} />
        <SnapshotRow
          label="Prochain rendez-vous"
          value={`${item.nextAppointmentAt ? formatDateTime(item.nextAppointmentAt) : '—'}${item.nextAppointmentEndAt ? ` -> ${formatDateTime(item.nextAppointmentEndAt)}` : ''}`}
        />
        <SnapshotRow label="Statut planning" value={getAppointmentStatusLabel(appointment)} />
        <SnapshotRow label="Derniere intervention" value={getInterventionSummary(latestIntervention)} />
        <SnapshotRow label="Resultat derniere intervention" value={item.lastInterventionOutcome || '—'} />
        <SnapshotRow label="A replanifier" value={item.requiresReplanning ? 'Oui' : 'Non'} />

        {appointment ? (
          <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
            <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Rendez-vous actif</div>
            <div className="mt-2 text-sm text-slate-900">
              <div className="font-semibold">{formatDateTime(appointment.startAt)}</div>
              <div className="text-slate-600">
                {appointment.endAt ? `Fin: ${formatDateTime(appointment.endAt)}` : 'Heure de fin non renseignee'}
              </div>
              <div className="mt-2 text-slate-600">
                Technicien: {appointment.technicianName || 'Non affecte'}
              </div>
            </div>
          </div>
        ) : null}

        {latestIntervention ? (
          <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
            <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Details derniere intervention</div>
            <div className="mt-2 text-sm text-slate-900">
              <div className="font-semibold">{getInterventionSummary(latestIntervention)}</div>
              <div className="text-slate-600">
                Demarrage: {latestIntervention.startedAt ? formatDateTime(latestIntervention.startedAt) : '—'}
              </div>
              <div className="text-slate-600">
                Fin: {latestIntervention.endedAt ? formatDateTime(latestIntervention.endedAt) : '—'}
              </div>
            </div>
          </div>
        ) : null}

        {item.lastInterventionReportSummary ? (
          <div>
            <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">Dernier rapport de visite</div>
            <div className="mt-1 whitespace-pre-wrap text-slate-900">{item.lastInterventionReportSummary}</div>
          </div>
        ) : null}

        {item.rejectionReason ? (
          <div>
            <div className="text-xs font-semibold uppercase tracking-wider text-rose-700">Raison du rejet</div>
            <div className="mt-1 whitespace-pre-wrap text-rose-900">{item.rejectionReason}</div>
          </div>
        ) : null}
      </div>
    </div>
  )
}

function SnapshotRow({ label, value }) {
  return (
    <div>
      <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">{label}</div>
      <div className="mt-1 font-semibold text-slate-900">{value}</div>
    </div>
  )
}
