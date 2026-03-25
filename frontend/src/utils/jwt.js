import { jwtDecode } from 'jwt-decode'

const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

export function parseUserFromToken(token) {
  const payload = jwtDecode(token)

  const role = payload.role ?? payload[ROLE_CLAIM] ?? null

  return {
    id: payload.sub ?? null,
    email: payload.email ?? null,
    role,
    firstName: payload.firstName ?? null,
    lastName: payload.lastName ?? null,
    exp: payload.exp ?? null,
  }
}

export function isTokenExpired(token) {
  try {
    const payload = jwtDecode(token)
    if (!payload?.exp) return false
    return Date.now() >= payload.exp * 1000
  } catch {
    return true
  }
}
