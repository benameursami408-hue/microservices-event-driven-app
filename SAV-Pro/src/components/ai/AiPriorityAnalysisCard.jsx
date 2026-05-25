import { Brain, Loader2, RefreshCw, Sparkles } from 'lucide-react';
import { Badge, Button, Card } from '../ui';
import { getFriendlyApiError } from '../../utils/errorMessages';

function formatCreatedAt(value) {
  if (!value) return '-';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '-';
  return date.toLocaleString();
}

export function AiPriorityAnalysisCard({
  reclamation,
  analysis,
  loading,
  error,
  onAnalyze,
  onApply,
  onRetry,
  canAnalyze = true,
  canApply = true,
  applyDisabledReason = 'Cette action est réservée au service SAV ou à l’administrateur.'
}) {
  const canApplyCurrentAnalysis = Boolean(reclamation && analysis?.suggestedPriority && canApply);
  const canAnalyzeCurrentReclamation = Boolean(reclamation && canAnalyze);
  const safeError = error ? getFriendlyApiError(error) : '';

  return (
    <Card title="Recherche intelligente" icon={Brain} className="ai-priority-card">
      {loading && (
        <div className="empty-state compact">
          <Loader2 size={18} className="spin" />
          <p>AI Priority Assistant is analyzing this reclamation...</p>
        </div>
      )}

      {!loading && safeError && (
        <div className="empty-state compact">
          <strong>Analysis unavailable</strong>
          <p>{safeError}</p>
          <Button icon={RefreshCw} onClick={onRetry || onAnalyze}>Retry</Button>
        </div>
      )}

      {!loading && !safeError && !analysis && (
        <div className="empty-state compact">
          <p>Analyze this reclamation to estimate priority and SLA risk. The assistant is rule-based and advisory; the SAV user applies the final decision.</p>
          {!canAnalyze ? <p className="permission-note">{applyDisabledReason}</p> : null}
          <Button icon={Sparkles} onClick={onAnalyze} disabled={!canAnalyzeCurrentReclamation}>Analyze Priority</Button>
        </div>
      )}

      {!loading && !safeError && analysis && (
        <div className="ai-priority-result">
          <div className="ai-analysis-grid">
            <span><small>Current Priority</small><Badge>{reclamation?.priority || 'Pending Review'}</Badge></span>
            <span><small>Suggested Priority</small><Badge>{analysis.suggestedPriority}</Badge></span>
            <span><small>SLA Risk</small><Badge>{analysis.slaRisk}</Badge></span>
            <span><small>Confidence Score</small><strong>{analysis.confidenceScore}%</strong></span>
            <span><small>Created</small><strong>{formatCreatedAt(analysis.createdAt)}</strong></span>
          </div>
          <div className="ai-priority-copy">
            <p><strong>Reason:</strong> {analysis.reason || '-'}</p>
            <p><strong>Recommended Action:</strong> {analysis.recommendedAction || '-'}</p>
            <p><strong>Detected Keywords:</strong> {analysis.detectedKeywords?.length ? analysis.detectedKeywords.join(', ') : 'None'}</p>
          </div>
          {!canApply ? <p className="permission-note">{applyDisabledReason}</p> : null}
          <div className="drawer-actions">
            {canApply ? <Button variant="primary" icon={Sparkles} onClick={onApply} disabled={!canApplyCurrentAnalysis}>Apply Suggestion</Button> : null}
            <Button icon={RefreshCw} onClick={onAnalyze} disabled={!canAnalyzeCurrentReclamation}>Re-analyze</Button>
          </div>
        </div>
      )}
    </Card>
  );
}
