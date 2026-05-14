export const roleToApi = {
  Admin: 'ADMIN',
  SAV: 'SAV',
  Technician: 'ST',
  Client: 'CLIENT',

  ADMIN: 'ADMIN',
  ST: 'ST',
  CLIENT: 'CLIENT',

  0: 'CLIENT',
  1: 'SAV',
  2: 'ADMIN',
  3: 'ST',
  '0': 'CLIENT',
  '1': 'SAV',
  '2': 'ADMIN',
  '3': 'ST'
};

export const roleFromApi = {
  ADMIN: 'Admin',
  SAV: 'SAV',
  ST: 'Technician',
  CLIENT: 'Client',

  Admin: 'Admin',
  Technician: 'Technician',
  Client: 'Client',

  0: 'Client',
  1: 'SAV',
  2: 'Admin',
  3: 'Technician',
  '0': 'Client',
  '1': 'SAV',
  '2': 'Admin',
  '3': 'Technician'
};

export function toApiRole(role) {
  return roleToApi[role] || role;
}

export function fromApiRole(role) {
  return roleFromApi[role] || role;
}
