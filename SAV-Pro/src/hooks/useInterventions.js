import { useCallback } from 'react';
import {
  addDiagnostic,
  addEvidence,
  addPart,
  addRepairAction,
  completeIntervention,
  getInterventionById,
  getMyInterventions,
  listInterventions,
  startIntervention,
  updateInterventionStatus
} from '../api/interventionApi';
import { createVisitReport } from '../api/interventionApi';
import { useApiResource } from './useApiResource';
import { isAdmin, isSav, isTechnician } from '../utils/roleAccess';

export function useInterventions(user) {
  const canLoad = Boolean(user) && (isTechnician(user) || isAdmin(user) || isSav(user));
  const technician = isTechnician(user);

  const loader = useCallback(() => {
    if (!canLoad) return Promise.resolve([]);
    return technician ? getMyInterventions() : listInterventions();
  }, [canLoad, technician]);

  const resource = useApiResource(loader, { initialData: [], immediate: canLoad });

  async function mutate(action) {
    const result = await action();
    await resource.reload();
    return result;
  }

  return {
    interventions: resource.data || [],
    loading: resource.loading,
    error: resource.error,
    errorStatus: resource.errorStatus,
    reload: resource.reload,
    getOne: getInterventionById,
    updateStatus: (id, status) => mutate(() => updateInterventionStatus(id, status)),
    start: id => mutate(() => startIntervention(id)),
    addDiagnostic: (id, diagnostic) => mutate(() => addDiagnostic(id, diagnostic)),
    addRepairAction: (id, action) => mutate(() => addRepairAction(id, action)),
    addPart: (id, part) => mutate(() => addPart(id, part)),
    addEvidence: (id, evidence) => mutate(() => addEvidence(id, evidence)),
    complete: (id, payload) => mutate(() => completeIntervention(id, payload)),
    createVisitReport: payload => mutate(() => createVisitReport(payload))
  };
}
