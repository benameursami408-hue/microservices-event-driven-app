import { useCallback } from 'react';
import { assignTechnician, cancelAppointment, confirmAppointment, createAppointment, listAppointments, listPlanningRequests, rescheduleAppointment } from '../api/planningApi';
import { useApiResource } from './useApiResource';

export function usePlanning({ enabled = true, includeRequests = true } = {}) {
  const loader = useCallback(async () => {
    if (!enabled) return { requests: [], appointments: [] };
    const [requests, appointments] = await Promise.all([
      includeRequests ? listPlanningRequests() : Promise.resolve([]),
      listAppointments()
    ]);
    return { requests, appointments };
  }, [enabled, includeRequests]);
  const resource = useApiResource(loader, { initialData: { requests: [], appointments: [] }, immediate: enabled });

  async function mutate(action) {
    const result = await action();
    await resource.reload();
    return result;
  }

  return {
    planningRequests: resource.data?.requests || [],
    appointments: resource.data?.appointments || [],
    loading: resource.loading,
    error: resource.error,
    errorStatus: resource.errorStatus,
    reload: resource.reload,
    createAppointment: payload => mutate(() => createAppointment(payload)),
    confirmAppointment: id => mutate(() => confirmAppointment(id)),
    rescheduleAppointment: (id, payload) => mutate(() => rescheduleAppointment(id, payload)),
    cancelAppointment: (id, reason) => mutate(() => cancelAppointment(id, reason)),
    assignTechnician: (id, tech) => mutate(() => assignTechnician(id, tech))
  };
}
