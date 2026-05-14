import { apiRequest } from './apiClient';
import { mapClientFromApi, mapClientToDto } from './mappers/clientMapper';

export async function listClients() {
  const clients = await apiRequest('/api/clients');
  return (clients || []).map(mapClientFromApi);
}

export async function getClient(id) {
  return mapClientFromApi(await apiRequest(`/api/clients/${id}`));
}

export async function createClient(payload) {
  return mapClientFromApi(await apiRequest('/api/clients', {
    method: 'POST',
    body: mapClientToDto(payload)
  }));
}

export async function updateClient(id, payload) {
  return mapClientFromApi(await apiRequest(`/api/clients/${id}`, {
    method: 'PUT',
    body: mapClientToDto(payload)
  }));
}
