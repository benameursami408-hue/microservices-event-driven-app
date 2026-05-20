import { apiRequest } from './apiClient';
import { mapUserFromApi, mapUserToCreateDto, mapUserToUpdateDto } from './mappers/userMapper';
import { toApiRole } from './mappers/roleMapper';

export async function listUsers(role) {
  const users = await apiRequest('/api/admin/users', {
    query: role ? { role: toApiRole(role) } : undefined
  });
  return (users || []).map(mapUserFromApi);
}

export async function createUser(payload) {
  return mapUserFromApi(await apiRequest('/api/admin/users', {
    method: 'POST',
    body: mapUserToCreateDto(payload)
  }));
}

export async function updateUser(id, payload) {
  return mapUserFromApi(await apiRequest(`/api/admin/users/${id}`, {
    method: 'PUT',
    body: mapUserToUpdateDto(payload)
  }));
}

export async function setUserActive(id, payload, isActive) {
  return updateUser(id, { ...payload, isActive });
}

export async function deleteUser(id) {
  return apiRequest(`/api/admin/users/${id}`, { method: 'DELETE' });
}
