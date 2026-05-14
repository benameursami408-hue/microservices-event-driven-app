import { useCallback } from 'react';
import { createClient, listClients, updateClient } from '../api/clientsApi';
import { useApiResource } from './useApiResource';

export function useClients(enabled = true) {
  const loader = useCallback(() => enabled ? listClients() : Promise.resolve([]), [enabled]);
  const resource = useApiResource(loader, { initialData: [], immediate: enabled });

  async function mutate(action) {
    const result = await action();
    await resource.reload();
    return result;
  }

  return {
    clients: resource.data || [],
    loading: resource.loading,
    error: resource.error,
    errorStatus: resource.errorStatus,
    reload: resource.reload,
    create: payload => mutate(() => createClient(payload)),
    update: (id, payload) => mutate(() => updateClient(id, payload))
  };
}
