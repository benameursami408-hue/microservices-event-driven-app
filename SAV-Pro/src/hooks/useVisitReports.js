import { useCallback } from 'react';
import { getVisitReport, listMyVisitReports, listVisitReports, publishVisitReport, updateVisitReport } from '../api/reportsApi';
import { useApiResource } from './useApiResource';

export function useVisitReports({ clientMode = false, mine = false, enabled = true } = {}) {
  const loader = useCallback(() => {
    if (!enabled) return Promise.resolve([]);
    return clientMode || mine ? listMyVisitReports() : listVisitReports();
  }, [clientMode, mine, enabled]);
  const resource = useApiResource(loader, { initialData: [], immediate: enabled });

  async function mutate(action) {
    const result = await action();
    await resource.reload();
    return result;
  }

  return {
    reports: resource.data || [],
    loading: resource.loading,
    error: resource.error,
    errorStatus: resource.errorStatus,
    reload: resource.reload,
    getOne: getVisitReport,
    update: (id, payload) => mutate(() => updateVisitReport(id, payload)),
    publish: id => mutate(() => publishVisitReport(id))
  };
}
