import { LockKeyhole, ShieldAlert } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { Button, Card } from '../components/ui';
import { canAccessDashboard } from '../utils/roleAccess';

export function AccessDeniedPage({ user }) {
  const navigate = useNavigate();
  const showDashboard = canAccessDashboard(user);

  return (
    <section className="page-shell access-denied-page">
      <Card className="access-denied-card">
        <span className="access-denied-icon"><ShieldAlert size={34} /></span>
        <h1>Accès non autorisé</h1>
        <p>Votre compte ne permet pas d’accéder à cette page.</p>
        <div className="access-denied-actions">
          <Button variant="primary" icon={LockKeyhole} onClick={() => navigate('/interventions', { replace: true })}>Retour à mes interventions</Button>
          {showDashboard ? <Button onClick={() => navigate('/dashboard', { replace: true })}>Retour au tableau de bord</Button> : null}
        </div>
      </Card>
    </section>
  );
}
