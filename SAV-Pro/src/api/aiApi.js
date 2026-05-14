import { ApiError, apiRequest } from './apiClient';
import { fromApiPriority } from './mappers/priorityMapper';
import { getTechnicalReclamationId } from './mappers/reclamationMapper';

function normalizeAiResponse(response = {}) {
  return {
    id: response.id || response.analysisId || null,
    analysisId: response.analysisId || response.id || null,
    reclamationId: response.reclamationId || null,
    suggestedPriority: fromApiPriority(response.suggestedPriority),
    confidenceScore: response.confidenceScore ?? response.confidence ?? 0,
    slaRisk: response.slaRisk || 'Low',
    reason: response.reason || '',
    recommendedAction: response.recommendedAction || '',
    detectedKeywords: Array.isArray(response.detectedKeywords) ? response.detectedKeywords : [],
    createdAt: response.createdAt || new Date().toISOString(),
    acceptedAt: response.acceptedAt || null,
    source: response.source || 'backend-rule-based'
  };
}

function requireAnalysisPayload(payload = {}) {
  if (!payload.reclamationId) {
    throw new ApiError('Cannot analyze priority: the selected reclamation has no technical backend id.');
  }

  if (!String(payload.description || '').trim()) {
    throw new ApiError('Cannot analyze priority: the reclamation description is empty. Add a useful issue description first.');
  }

  return {
    reclamationId: payload.reclamationId,
    reference: payload.reference || '',
    description: payload.description.trim(),
    productName: payload.productName || '',
    brand: payload.brand || '',
    model: payload.model || '',
    currentPriority: payload.currentPriority || 'Medium',
    clientImpact: payload.clientImpact || ''
  };
}

export async function analyzeReclamationPriority(payload) {
  const response = await apiRequest('/api/ai/reclamations/analyze-priority', {
    method: 'POST',
    body: requireAnalysisPayload(payload)
  });
  return normalizeAiResponse(response);
}

export async function getLatestAiPriorityAnalysis(reclamation) {
  const id = getTechnicalReclamationId(reclamation);
  if (!id) return null;

  try {
    const response = await apiRequest(`/api/reclamations/${id}/ai-priority-analysis/latest`);
    return response ? normalizeAiResponse(response) : null;
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) return null;
    throw error;
  }
}

export async function applyAiPriorityAnalysis(reclamation, analysisId, payload = {}) {
  const id = getTechnicalReclamationId(reclamation);
  if (!id || !analysisId) {
    throw new ApiError('Cannot apply AI priority suggestion: missing reclamation id or analysis id.');
  }

  return apiRequest(`/api/reclamations/${id}/ai-priority-analysis/${analysisId}/apply`, {
    method: 'POST',
    body: payload
  });
}
