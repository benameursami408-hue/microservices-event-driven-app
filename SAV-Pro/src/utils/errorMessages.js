function readStatus(error) {
  return error?.status || error?.response?.status || error?.details?.status || 0;
}

function isNetworkError(error) {
  if (!error) return false;
  const text = `${error.message || ''} ${error.name || ''}`.toLowerCase();
  return !readStatus(error) && (
    text.includes('failed to fetch')
    || text.includes('network')
    || text.includes('unreachable')
    || text.includes('serveur est inaccessible')
    || text.includes('apigateway is unreachable')
  );
}

export function getFriendlyApiError(error) {
  const status = readStatus(error);

  if (status === 401) return 'Votre session a expiré.';
  if (status === 403) return 'Vous n’avez pas les droits nécessaires pour cette action.';
  if (status === 404) return 'La ressource demandée est introuvable.';
  if (status >= 500) return 'Erreur serveur. Réessayez plus tard.';
  if (isNetworkError(error)) return 'Le serveur est inaccessible.';

  if (typeof error === 'string') return error;
  return error?.userMessage || error?.message || 'Une erreur inattendue est survenue.';
}

export function getApiErrorTitle(status) {
  if (status === 401) return 'Session expirée';
  if (status === 403) return 'Accès non autorisé';
  if (status === 404) return 'Élément introuvable';
  if (status >= 500) return 'Erreur serveur';
  if (!status) return 'Serveur inaccessible';
  return 'Erreur API';
}

export function getApiErrorDescription(status) {
  if (status === 401) return 'Session expirée. Veuillez vous reconnecter.';
  if (status === 403) return 'Accès non autorisé pour ce compte.';
  if (status === 404) return 'Élément introuvable.';
  if (status >= 500) return 'Erreur serveur. Réessayez plus tard.';
  return 'Impossible de joindre le serveur.';
}
