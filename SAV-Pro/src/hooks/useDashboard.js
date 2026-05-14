import { useCallback } from 'react';
import { apiRequest } from '../api/apiClient';
import { useApiResource } from './useApiResource';

export function useDashboard(enabled = true) {
  const loader = useCallback(() => enabled ? apiRequest('/api/dashboard/summary') : Promise.resolve({}), [enabled]);
  const resource = useApiResource(loader, { initialData: {}, immediate: enabled });
  return {
    summary: resource.data || {},
    loading: resource.loading,
    error: resource.error,
    errorStatus: resource.errorStatus,
    reload: resource.reload
  };
}
