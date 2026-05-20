import { useCallback } from 'react';
import { getAdminStatistics } from '../api/adminStatsApi';
import { useApiResource } from './useApiResource';

export function useAdminStatistics(enabled = true) {
  const loader = useCallback(() => enabled ? getAdminStatistics() : Promise.resolve(null), [enabled]);
  const resource = useApiResource(loader, { initialData: null, immediate: enabled });

  return {
    statistics: resource.data,
    loading: resource.loading,
    error: resource.error,
    errorStatus: resource.errorStatus,
    reload: resource.reload
  };
}
