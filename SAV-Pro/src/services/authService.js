import { getDefaultBackOfficePath, isAdmin, isClient, isSav, isTechnician } from '../utils/roleAccess';

export const roleHome = {
  Admin: '/dashboard',
  SAV: '/dashboard',
  Technician: '/interventions',
  Client: '/client'
};

export function getHomePath(user) {
  if (isClient(user)) return '/client';
  return getDefaultBackOfficePath(user);
}

export function isBackOfficeUser(user) {
  return isAdmin(user) || isSav(user) || isTechnician(user);
}

export function isClientUser(user) {
  return isClient(user);
}

export function canAccessBackOffice(user) {
  return isBackOfficeUser(user);
}

export function canAccessClientPortal(user) {
  return isClientUser(user);
}
