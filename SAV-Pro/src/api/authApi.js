import { apiRequest } from './apiClient';
import { mapUserFromApi } from './mappers/userMapper';

export async function login(email, password) {
  const response = await apiRequest('/api/auth/login', {
    method: 'POST',
    body: { email, password },
    auth: false
  });
  return mapUserFromApi(response?.user || response || {});
}

export async function me() {
  const response = await apiRequest('/api/auth/me');
  return mapUserFromApi(response?.user || response || {});
}

export async function logout() {
  await apiRequest('/api/auth/logout', { method: 'POST', auth: false });
}

export async function register(payload) {
  const response = await apiRequest('/api/auth/register', {
    method: 'POST',
    body: payload,
    auth: false
  });
  return mapUserFromApi(response?.user || response || {});
}
