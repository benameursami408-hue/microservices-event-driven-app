function normalizeRoleValue(value) {
  return String(value || '')
    .trim()
    .replace(/[-_\s]+/g, '_')
    .toUpperCase();
}

export function getUserRoles(user) {
  const rawRoles = [
    user?.role,
    user?.roleName,
    user?.userRole,
    ...(Array.isArray(user?.roles) ? user.roles : [])
  ];

  return rawRoles
    .flatMap(role => typeof role === 'string' && role.includes(',') ? role.split(',') : [role])
    .map(normalizeRoleValue)
    .filter(Boolean);
}

function hasAnyRole(user, allowedRoles) {
  const roles = getUserRoles(user);
  return allowedRoles.some(role => roles.includes(normalizeRoleValue(role)));
}

export function isAdmin(user) {
  return hasAnyRole(user, ['ADMIN', 'Admin']);
}

export function isSav(user) {
  return hasAnyRole(user, ['SAV', 'SAV_MANAGER', 'SAV MANAGER']);
}

export function isTechnician(user) {
  return hasAnyRole(user, ['TECHNICIAN', 'ST', 'Technician']);
}

export function isClient(user) {
  return hasAnyRole(user, ['CLIENT', 'Client']);
}

export function canAccessDashboard(user) {
  return isAdmin(user) || isSav(user) || isTechnician(user);
}

export function canAccessDashboardSummary(user) {
  return isAdmin(user) || isSav(user);
}

export function canAccessClients(user) {
  return isAdmin(user) || isSav(user);
}

export function canAccessUsers(user) {
  return isAdmin(user) || isSav(user);
}

export function canManageUsers(user) {
  return isAdmin(user);
}

export function canAccessPlanningRequests(user) {
  return isAdmin(user) || isSav(user);
}

export function canCreateReclamation(user) {
  return isAdmin(user) || isSav(user) || isClient(user);
}

export function canAssignTechnician(user) {
  return isAdmin(user) || isSav(user);
}

export function canApplyAiPriority(user) {
  return isAdmin(user) || isSav(user);
}

export function canAccessReclamations(user) {
  return isAdmin(user) || isSav(user) || isTechnician(user);
}

export function canAccessInterventions(user) {
  return isAdmin(user) || isSav(user) || isTechnician(user);
}

export function canAccessVisitReports(user) {
  return isAdmin(user) || isSav(user) || isTechnician(user) || isClient(user);
}

export function canAccessNotifications(user) {
  return Boolean(user);
}

export function getDefaultBackOfficePath(user) {
  if (isTechnician(user)) return '/interventions';
  if (isAdmin(user) || isSav(user)) return '/dashboard';
  return '/login';
}

export function canAccessBackOfficeRoute(user, routeId) {
  const checks = {
    dashboard: canAccessDashboard,
    reclamations: canAccessReclamations,
    clients: canAccessClients,
    planning: canAccessPlanningRequests,
    interventions: canAccessInterventions,
    'visit-reports': canAccessVisitReports,
    reports: canAccessDashboard,
    notifications: canAccessNotifications,
    users: canAccessUsers,
    settings: () => Boolean(user),
    profile: () => Boolean(user)
  };

  return checks[routeId] ? checks[routeId](user) : false;
}
