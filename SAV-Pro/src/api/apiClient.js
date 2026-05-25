const DEFAULT_API_BASE_URL = 'http://localhost:5005';

let unauthorizedHandler = null;

export class ApiError extends Error {
  constructor(message, { status = 0, details = null, path = '', userMessage = '' } = {}) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.details = details;
    this.path = path;
    this.userMessage = userMessage;
  }
}

export function setUnauthorizedHandler(handler) {
  unauthorizedHandler = typeof handler === 'function' ? handler : null;
}

export function getApiBaseUrl() {
  return (import.meta.env.VITE_API_BASE_URL || DEFAULT_API_BASE_URL).replace(/\/$/, '');
}

function isAbsoluteUrl(value) {
  return /^https?:\/\//i.test(value);
}

function trimTrailingSlash(value) {
  return String(value || '').replace(/\/$/, '');
}

function normalizeApiPath(baseUrl, path) {
  const normalizedPath = path.startsWith('/') ? path : `/${path}`;

  if (trimTrailingSlash(baseUrl).endsWith('/api') && normalizedPath.startsWith('/api/')) {
    return normalizedPath.slice(4);
  }

  return normalizedPath;
}

function buildUrl(path, query) {
  const baseUrl = trimTrailingSlash(getApiBaseUrl());
  const normalizedPath = normalizeApiPath(baseUrl, path);
  const rawUrl = `${baseUrl}${normalizedPath}` || normalizedPath;

  if (isAbsoluteUrl(rawUrl)) {
    const url = new URL(rawUrl);
    if (query) {
      Object.entries(query).forEach(([key, value]) => {
        if (value !== undefined && value !== null && value !== '') {
          url.searchParams.set(key, value);
        }
      });
    }
    return url.toString();
  }

  const searchParams = new URLSearchParams();
  if (query) {
    Object.entries(query).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        searchParams.set(key, value);
      }
    });
  }

  const queryString = searchParams.toString();
  return queryString ? `${rawUrl}?${queryString}` : rawUrl;
}

async function parseResponse(response) {
  if (response.status === 204) return null;

  const text = await response.text();
  if (!text) return null;

  const contentType = response.headers.get('content-type') || '';
  if (contentType.includes('application/json')) {
    try {
      return JSON.parse(text);
    } catch {
      return text;
    }
  }

  return text;
}

function getErrorMessage(payload, fallback) {
  if (!payload) return fallback;
  if (typeof payload === 'string') return payload;
  if (payload.title) return payload.title;
  if (payload.message) return payload.message;
  if (payload.detail) return payload.detail;
  if (payload.errors) {
    const values = Object.values(payload.errors).flat().filter(Boolean);
    if (values.length) return values.join(' ');
  }
  return fallback;
}

function getUserMessage(status) {
  if (status === 401) return 'Session expirée. Veuillez vous reconnecter.';
  if (status === 403) return 'Accès non autorisé pour ce compte.';
  if (status === 404) return 'Élément introuvable.';
  if (status >= 500) return 'Erreur serveur. Réessayez plus tard.';
  return 'Impossible de joindre le serveur.';
}

function logDevError(error) {
  if (import.meta.env.DEV) {
    // Keep technical details available during development without leaking them to production users.
    console.warn('[SAV API]', error);
  }
}

export async function apiRequest(path, { method = 'GET', body, query, headers = {}, auth = true } = {}) {
  const requestHeaders = {
    Accept: 'application/json',
    ...headers
  };

  const init = {
    method,
    headers: requestHeaders,
    credentials: 'include'
  };

  if (body !== undefined) {
    requestHeaders['Content-Type'] = 'application/json';
    init.body = JSON.stringify(body);
  }

  let response;
  try {
    response = await fetch(buildUrl(path, query), init);
  } catch (error) {
    const apiError = new ApiError(`ApiGateway is unreachable at ${getApiBaseUrl()}.`, {
      status: 0,
      details: error,
      path,
      userMessage: 'Impossible de joindre le serveur.'
    });
    logDevError(apiError);
    throw apiError;
  }

  const payload = await parseResponse(response);

  if (response.status === 401 && auth) {
    unauthorizedHandler?.();
  }

  if (!response.ok) {
    const apiError = new ApiError(getErrorMessage(payload, `Gateway request failed with status ${response.status} for ${path}.`), {
      status: response.status,
      details: payload,
      path,
      userMessage: getUserMessage(response.status)
    });
    logDevError(apiError);
    throw apiError;
  }

  return payload;
}

export function structuredApiError(error) {
  if (error instanceof ApiError) {
    return {
      message: error.userMessage || error.message,
      status: error.status,
      details: error.details,
      path: error.path
    };
  }
  return {
    message: error?.message || 'Unexpected error.',
    status: 0,
    details: error,
    path: ''
  };
}
