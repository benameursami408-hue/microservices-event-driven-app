import { useCallback } from 'react';
import {
  assignToSav,
  claimReclamation,
  createReclamation,
  deleteReclamation,
  getReclamation,
  getReclamationHistory,
  listReclamations,
  overridePriority,
  planReclamation,
  reassignReclamation,
  releaseReclamation,
  requestPlanning,
  updateReclamation
} from '../api/reclamationsApi';
import { analyzeReclamationPriority, applyAiPriorityAnalysis, getLatestAiPriorityAnalysis } from '../api/aiApi';
import { useApiResource } from './useApiResource';

export function useReclamations(filters = {}, enabled = true) {
  const loader = useCallback(() => enabled ? listReclamations(filters) : Promise.resolve([]), [JSON.stringify(filters), enabled]);
  const resource = useApiResource(loader, { initialData: [], immediate: enabled });

  return {
    reclamations: resource.data || [],
    loading: resource.loading,
    error: resource.error,
    errorStatus: resource.errorStatus,
    reload: resource.reload,
    create: async payload => {
      const result = await createReclamation(payload);
      await resource.reload();
      return result;
    },
    update: async (reclamation, payload) => {
      const result = await updateReclamation(reclamation, payload);
      await resource.reload();
      return result;
    },
    remove: async reclamation => {
      const result = await deleteReclamation(reclamation);
      await resource.reload();
      return result;
    },
    assignToSav: async (reclamation, sav) => {
      const result = await assignToSav(reclamation, sav);
      await resource.reload();
      return result;
    },
    claim: async reclamation => {
      const result = await claimReclamation(reclamation);
      await resource.reload();
      return result;
    },
    release: async reclamation => {
      const result = await releaseReclamation(reclamation);
      await resource.reload();
      return result;
    },
    reassignSav: async (reclamation, savUserId, savUserName) => {
      const result = await reassignReclamation(reclamation, savUserId, savUserName);
      await resource.reload();
      return result;
    },
    requestPlanning: async (reclamation, comment) => {
      const result = await requestPlanning(reclamation, comment);
      await resource.reload();
      return result;
    },
    planTechnician: async (reclamation, technician, payload) => {
      const result = await planReclamation(reclamation, technician, payload);
      await resource.reload();
      return result;
    },
    overridePriority: async (reclamation, priority, reason) => {
      const result = await overridePriority(reclamation, priority, reason);
      await resource.reload();
      return result;
    },
    getOne: getReclamation,
    getHistory: getReclamationHistory,
    analyzePriority: analyzeReclamationPriority,
    getLatestAiPriorityAnalysis,
    applyAiPriorityAnalysis: async (reclamation, analysisId, payload) => {
      const result = await applyAiPriorityAnalysis(reclamation, analysisId, payload);
      await resource.reload();
      return result;
    }
  };
}
