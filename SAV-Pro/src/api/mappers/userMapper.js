import { fromApiRole, toApiRole } from './roleMapper';

export function mapUserFromApi(user = {}) {
  const firstName = user.firstName || '';
  const lastName = user.lastName || '';
  const name = `${firstName} ${lastName}`.trim() || user.email || 'User';
  const role = fromApiRole(user.role);
  return {
    id: user.id,
    technicalId: user.id,
    firstName,
    lastName,
    name,
    email: user.email || '',
    phone: user.phoneNumber || user.phone || '',
    phoneNumber: user.phoneNumber || user.phone || '',
    address: user.address || '',
    role,
    isActive: user.isActive !== false,
    technicianId: role === 'Technician' ? user.id : undefined,
    clientId: role === 'Client' ? user.id : undefined
  };
}

export function mapUserToCreateDto(user = {}) {
  const [fallbackFirst, ...rest] = String(user.name || '').trim().split(' ');
  return {
    firstName: user.firstName || fallbackFirst || 'User',
    lastName: user.lastName || rest.join(' ') || user.role || 'Account',
    phoneNumber: user.phoneNumber || user.phone || '+216 00 000 000',
    address: user.address || user.location || '',
    email: user.email || '',
    password: user.password || 'ChangeMe123!',
    role: toApiRole(user.role),
    isActive: user.isActive !== false
  };
}

export function mapUserToUpdateDto(user = {}) {
  const dto = mapUserToCreateDto(user);
  delete dto.password;
  if (user.password) dto.password = user.password;
  return dto;
}
