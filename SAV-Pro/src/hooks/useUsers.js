import { useCallback } from 'react';
import { createUser, deleteUser, listUsers, setUserActive, updateUser } from '../api/usersApi';
import { useApiResource } from './useApiResource';

export function useUsers(role, enabled = true) {
  const loader = useCallback(() => enabled ? listUsers(role) : Promise.resolve([]), [role, enabled]);
  const resource = useApiResource(loader, { initialData: [], immediate: enabled });

  async function mutate(action) {
    const result = await action();
    await resource.reload();
    return result;
  }

  return {
    users: resource.data || [],
    loading: resource.loading,
    error: resource.error,
    errorStatus: resource.errorStatus,
    reload: resource.reload,
    create: payload => mutate(() => createUser(payload)),
    update: (id, payload) => mutate(() => updateUser(id, payload)),
    setActive: (id, payload, isActive) => mutate(() => setUserActive(id, payload, isActive)),
    remove: id => mutate(() => deleteUser(id))
  };
}
