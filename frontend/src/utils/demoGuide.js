export const DEMO_WORKFLOW_STEPS = [
  {
    id: 'users',
    title: '1. Preparer les comptes',
    role: 'ADMIN',
    route: '/app/admin/users',
    goal: 'Creer ou verifier un compte SAV, un compte technique et un compte client.',
    checks: ['Creation utilisateur', 'Modification sans changer le mot de passe', 'Refus email duplique'],
  },
  {
    id: 'client-ticket',
    title: '2. Creer une reclamation',
    role: 'CLIENT / SAV / ADMIN',
    route: '/app/reclamations/new',
    goal: 'Verifier le formulaire, les champs obligatoires et la creation du ticket.',
    checks: ['Produit renseigne', 'Description claire', 'Reference creee apres sauvegarde'],
  },
  {
    id: 'ticket-list',
    title: '3. Suivre les tickets',
    role: 'SAV / ADMIN',
    route: '/app/reclamations',
    goal: 'Filtrer, rechercher et ouvrir le detail du ticket cree.',
    checks: ['Recherche par reference', 'Filtre statut', 'Filtre priorite'],
  },
  {
    id: 'planning',
    title: '4. Planifier le rendez-vous',
    role: 'SAV / ADMIN',
    route: '/app/planning',
    goal: 'Verifier la coordination entre ticket, rendez-vous et equipe technique.',
    checks: ['Rendez-vous visible', 'Technicien affecte', 'Conflit de planning refuse'],
  },
  {
    id: 'technician',
    title: '5. Executer l intervention',
    role: 'ST / ADMIN',
    route: '/app/interventions',
    goal: 'Tester le parcours terrain: demarrer, renseigner le diagnostic et terminer.',
    checks: ['Demarrage', 'Compte-rendu', 'Statut final coherent'],
  },
  {
    id: 'notifications',
    title: '6. Verifier les notifications',
    role: 'Tous roles',
    route: '/app/notifications',
    goal: 'Controler que les evenements importants sont visibles pour le bon utilisateur.',
    checks: ['Notification creee', 'Lecture possible', 'Role coherent'],
  },
]

export const ROLE_NEXT_STEPS = {
  ADMIN: {
    title: 'Parcours Admin conseille',
    summary: 'Commencez par les utilisateurs, puis validez les tickets et les statistiques.',
    actions: [
      { label: 'Gerer les utilisateurs', route: '/app/admin/users' },
      { label: 'Ouvrir les tickets SAV', route: '/app/reclamations' },
      { label: 'Suivre le guide complet', route: '/app/guide-test' },
    ],
  },
  SAV: {
    title: 'Parcours SAV conseille',
    summary: 'Creez ou reprenez un ticket, puis suivez son statut jusqu a la planification.',
    actions: [
      { label: 'Liste des tickets', route: '/app/reclamations' },
      { label: 'Nouvelle reclamation', route: '/app/reclamations/new' },
      { label: 'Guide de test', route: '/app/guide-test' },
    ],
  },
  ST: {
    title: 'Parcours Technicien conseille',
    summary: 'Concentrez-vous sur les interventions affectees et le compte-rendu terrain.',
    actions: [
      { label: 'Mes interventions', route: '/app/interventions' },
      { label: 'Planning', route: '/app/planning' },
      { label: 'Guide de test', route: '/app/guide-test' },
    ],
  },
  CLIENT: {
    title: 'Parcours Client conseille',
    summary: 'Creez une reclamation, puis consultez son suivi et les notifications.',
    actions: [
      { label: 'Nouvelle reclamation', route: '/app/reclamations/new' },
      { label: 'Mes tickets', route: '/app/reclamations' },
      { label: 'Guide de test', route: '/app/guide-test' },
    ],
  },
  DEFAULT: {
    title: 'Parcours conseille',
    summary: 'Ouvrez le guide pour savoir exactement quelle page tester en premier.',
    actions: [
      { label: 'Guide de test', route: '/app/guide-test' },
      { label: 'Tickets SAV', route: '/app/reclamations' },
    ],
  },
}

export function normalizeRole(role) {
  const value = String(role ?? '').toUpperCase()
  if (value === '0') return 'CLIENT'
  if (value === '1') return 'SAV'
  if (value === '2') return 'ADMIN'
  if (value === '3') return 'ST'
  return value || 'DEFAULT'
}

export function getRoleNextSteps(role) {
  return ROLE_NEXT_STEPS[normalizeRole(role)] ?? ROLE_NEXT_STEPS.DEFAULT
}
