import api from './api.js'

export async function register(payload) {
  const { data } = await api.post('/api/auth/register', payload)
  return data
}

export async function login(payload) {
  const { data } = await api.post('/api/auth/login', payload)
  return data
}
