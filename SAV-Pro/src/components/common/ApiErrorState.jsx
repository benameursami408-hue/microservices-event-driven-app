import { AlertTriangle, LockKeyhole, RefreshCw, ServerCrash, ShieldAlert, WifiOff } from 'lucide-react';
import { Button } from '../ui';
import { getApiErrorDescription, getApiErrorTitle } from '../../utils/errorMessages';

function iconForStatus(status) {
  if (status === 401 || status === 403) return ShieldAlert;
  if (status === 404) return LockKeyhole;
  if (status >= 500) return ServerCrash;
  if (!status) return WifiOff;
  return AlertTriangle;
}

export function ApiErrorState({ status = 0, title, message, onRetry, variant = 'card' }) {
  const Icon = iconForStatus(status);
  const safeTitle = title || getApiErrorTitle(status);
  const safeMessage = message || getApiErrorDescription(status);

  return (
    <div className={`api-error-state api-error-${variant}`} role="alert">
      <span className="api-error-icon"><Icon size={26} /></span>
      <div>
        <strong>{safeTitle}</strong>
        <p>{safeMessage}</p>
      </div>
      {onRetry && status !== 403 ? (
        <Button icon={RefreshCw} onClick={onRetry}>Réessayer</Button>
      ) : null}
    </div>
  );
}
