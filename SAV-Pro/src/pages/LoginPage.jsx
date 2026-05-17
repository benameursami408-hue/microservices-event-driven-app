import {
  CalendarDays,
  CheckCircle,
  ClipboardList,
  Eye,
  Headphones,
  Lock,
  LogIn,
  Mail,
  MessageSquare,
  Monitor,
  ShieldCheck,
  Smartphone,
  Truck,
  UserPlus,
  UserRound,
  Users,
  Wrench
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { register as registerAccount } from '../api/authApi';
import { Button, Logo } from '../components/ui';
import { getFriendlyApiError } from '../utils/errorMessages';

const rolePanels = [
  {
    key: 'admin',
    label: 'Admin',
    title: 'Executive control',
    text: 'Global visibility for users, SLA risk, priorities, and operational decisions.',
    icon: ShieldCheck,
    stats: ['Users', 'SLA', 'Access']
  },
  {
    key: 'sav',
    label: 'SAV',
    title: 'Service desk flow',
    text: 'Triage reclamations, request planning, apply AI priority, and keep clients informed.',
    icon: Users,
    stats: ['Queue', 'Planning', 'Priority']
  },
  {
    key: 'st',
    label: 'ST',
    title: 'Technician workspace',
    text: 'See assigned interventions, report diagnostics, and close field work cleanly.',
    icon: Wrench,
    stats: ['Visits', 'Reports', 'Parts']
  },
  {
    key: 'client',
    label: 'Client',
    title: 'Client portal',
    text: 'Create requests, follow appointments, and review after-sales progress.',
    icon: UserRound,
    stats: ['Requests', 'Updates', 'Support']
  }
];

const ROLE_ROTATION_MS = 3600;

const blankRegisterForm = {
  firstName: '',
  lastName: '',
  phoneNumber: '',
  address: '',
  email: '',
  password: ''
};

function isValidEmail(value) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(String(value || '').trim());
}

export function LoginPage({ onLogin }) {
  const [mode, setMode] = useState('login');
  const [activeRole, setActiveRole] = useState(0);
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [registerForm, setRegisterForm] = useState(blankRegisterForm);
  const [fieldErrors, setFieldErrors] = useState({});
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [loading, setLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const role = rolePanels[activeRole];
  const RoleIcon = role.icon;

  useEffect(() => {
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      return undefined;
    }

    const rotation = window.setInterval(() => {
      setActiveRole(current => (current + 1) % rolePanels.length);
    }, ROLE_ROTATION_MS);

    return () => window.clearInterval(rotation);
  }, []);

  function clearMessages() {
    setError('');
    setNotice('');
    setFieldErrors({});
  }

  function validateLogin() {
    const nextErrors = {};
    if (!isValidEmail(email)) nextErrors.email = 'Email is invalid.';
    if (!password.trim()) nextErrors.password = 'Password is required.';
    setFieldErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  }

  function validateRegister() {
    const nextErrors = {};
    if (!registerForm.firstName.trim()) nextErrors.firstName = 'First name is required.';
    if (!registerForm.lastName.trim()) nextErrors.lastName = 'Last name is required.';
    if (!registerForm.phoneNumber.trim()) nextErrors.phoneNumber = 'Phone is required.';
    if (!isValidEmail(registerForm.email)) nextErrors.email = 'Email is invalid.';
    if (registerForm.password.length < 8) nextErrors.password = 'Use at least 8 characters.';
    setFieldErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  }

  async function submit(event) {
    event.preventDefault();
    setError('');
    setNotice('');
    if (!validateLogin()) return;

    setLoading(true);
    try {
      await onLogin(email.trim(), password);
    } catch (err) {
      const friendly = getFriendlyApiError(err);
      const authMessage = err?.status === 401 ? 'Email or password is invalid.' : friendly;
      setFieldErrors(err?.status === 401 ? { email: 'Check this email.', password: 'Check this password.' } : {});
      setError(authMessage);
    } finally {
      setLoading(false);
    }
  }

  async function submitRegister(event) {
    event.preventDefault();
    setError('');
    setNotice('');
    if (!validateRegister()) return;

    setLoading(true);
    try {
      await registerAccount({
        firstName: registerForm.firstName.trim(),
        lastName: registerForm.lastName.trim(),
        phoneNumber: registerForm.phoneNumber.trim(),
        address: registerForm.address.trim(),
        email: registerForm.email.trim(),
        password: registerForm.password
      });
      setEmail(registerForm.email.trim());
      setPassword('');
      setRegisterForm(blankRegisterForm);
      setMode('login');
      setNotice('Account created. Sign in with your new client account.');
    } catch (err) {
      setError(getFriendlyApiError(err));
    } finally {
      setLoading(false);
    }
  }

  function updateRegisterField(key, value) {
    setRegisterForm(current => ({ ...current, [key]: value }));
    if (fieldErrors[key]) {
      setFieldErrors(current => ({ ...current, [key]: '' }));
    }
  }

  function switchMode(nextMode) {
    clearMessages();
    setMode(nextMode);
  }

  return (
    <main className={`login-page role-${role.key}`}>
      <section className="login-hero">
        <div className="hero-content">
          <Logo />
          <div className="hero-copy">
            <h1>Smarter after-sales.<br />Happier customers.</h1>
            <p>SAV Pro helps you streamline reclamations, plan resources, manage interventions, and deliver exceptional service at every step.</p>
          </div>

          <div className="feature-list">
            <Feature icon={ClipboardList} title="Reclamations" text="Track, prioritize, and resolve customer issues efficiently with full visibility." />
            <Feature icon={CalendarDays} title="Planning" text="Schedule appointments, assign technicians, and optimize workloads." />
            <Feature icon={Wrench} title="Interventions" text="Manage on-site activities, capture evidence, and close the loop seamlessly." />
          </div>
        </div>

        <div className="hero-illustration" aria-hidden="true">
          <div className="city city-left" />
          <div className="city city-right" />
          <div className="orbit orbit-one" />
          <div className="orbit orbit-two" />
          <span className="float-icon chat"><MessageSquare size={28} /></span>
          <span className="float-icon tools"><Wrench size={34} /></span>
          <span className="float-icon calendar"><CalendarDays size={31} /></span>
          <div className="headset">
            <Headphones size={150} />
          </div>
          <div className="dashboard-device">
            <div className="device-top" />
            <Monitor size={210} strokeWidth={1.3} />
            <div className="mini-chart">
              <span />
              <span />
              <span />
              <strong />
            </div>
          </div>
          <div className="phone-device">
            <Smartphone size={98} strokeWidth={1.5} />
            <CheckCircle size={31} />
          </div>
          <div className="service-truck">
            <Truck size={118} strokeWidth={1.5} />
          </div>
        </div>
      </section>

      <section className="login-panel">
        <div className="login-card">
          <div className="role-showcase" aria-label={`${role.label} workspace highlight`} key={role.key}>
            <span className="role-showcase-icon"><RoleIcon size={24} /></span>
            <div>
              <small>{role.label}</small>
              <strong>{role.title}</strong>
              <p>{role.text}</p>
            </div>
            <div className="role-stat-row" aria-hidden="true">
              {role.stats.map(item => <span key={item}>{item}</span>)}
            </div>
          </div>

          <header>
            <h2>{mode === 'login' ? 'Sign in to SAV Pro' : 'Create a client account'}</h2>
            <p>{mode === 'login' ? 'Use your work account to continue.' : 'New client accounts can follow requests from the portal.'}</p>
          </header>

          {mode === 'login' ? (
            <form onSubmit={submit} noValidate>
              <AuthField id="login-email" label="Email" error={fieldErrors.email} icon={Mail}>
                <input id="login-email" value={email} onChange={event => setEmail(event.target.value)} type="text" inputMode="email" placeholder="name@company.com" autoComplete="email" />
              </AuthField>

              <AuthField id="login-password" label="Password" error={fieldErrors.password} icon={Lock}>
                <input id="login-password" value={password} onChange={event => setPassword(event.target.value)} type={showPassword ? 'text' : 'password'} placeholder="Enter your password" autoComplete="current-password" />
                <button type="button" className="password-eye" onClick={() => setShowPassword(current => !current)} aria-label={showPassword ? 'Hide password' : 'Show password'}>
                  <Eye size={21} />
                </button>
              </AuthField>

              <div className="auth-row">
                <label className="remember-me">
                  <input type="checkbox" defaultChecked />
                  <span>Remember me</span>
                </label>
                <button type="button" className="link-button" onClick={() => setNotice('Ask your administrator to reset your password.')}>Forgot password?</button>
              </div>

              {error && <p className="form-error">{error}</p>}
              {notice && <p className="form-success">{notice}</p>}

              <Button type="submit" variant="primary" size="lg" icon={LogIn} className="sign-in-btn role-sign-in" disabled={loading}>
                {loading ? 'Signing in...' : 'Sign in'}
              </Button>

              <button type="button" className="create-account-link" onClick={() => switchMode('register')}>
                <UserPlus size={18} />
                Create new account
              </button>
            </form>
          ) : (
            <form className="register-form" onSubmit={submitRegister} noValidate>
              <div className="register-grid">
                <AuthField id="firstName" label="First name" error={fieldErrors.firstName}>
                  <input id="firstName" value={registerForm.firstName} onChange={event => updateRegisterField('firstName', event.target.value)} placeholder="First name" autoComplete="given-name" />
                </AuthField>
                <AuthField id="lastName" label="Last name" error={fieldErrors.lastName}>
                  <input id="lastName" value={registerForm.lastName} onChange={event => updateRegisterField('lastName', event.target.value)} placeholder="Last name" autoComplete="family-name" />
                </AuthField>
                <AuthField id="register-phone" label="Phone" error={fieldErrors.phoneNumber}>
                  <input id="register-phone" value={registerForm.phoneNumber} onChange={event => updateRegisterField('phoneNumber', event.target.value)} placeholder="+216 00 000 000" autoComplete="tel" />
                </AuthField>
                <AuthField id="register-email" label="Email" error={fieldErrors.email}>
                  <input id="register-email" value={registerForm.email} onChange={event => updateRegisterField('email', event.target.value)} type="text" inputMode="email" placeholder="name@company.com" autoComplete="email" />
                </AuthField>
              </div>
              <AuthField id="register-address" label="Address">
                <input id="register-address" value={registerForm.address} onChange={event => updateRegisterField('address', event.target.value)} placeholder="Service address" autoComplete="street-address" />
              </AuthField>
              <AuthField id="register-password" label="Password" error={fieldErrors.password} icon={Lock}>
                <input id="register-password" value={registerForm.password} onChange={event => updateRegisterField('password', event.target.value)} type={showPassword ? 'text' : 'password'} placeholder="At least 8 characters" autoComplete="new-password" />
                <button type="button" className="password-eye" onClick={() => setShowPassword(current => !current)} aria-label={showPassword ? 'Hide password' : 'Show password'}>
                  <Eye size={21} />
                </button>
              </AuthField>

              {error && <p className="form-error">{error}</p>}
              {notice && <p className="form-success">{notice}</p>}

              <div className="auth-actions-row">
                <Button type="button" onClick={() => switchMode('login')}>Back to sign in</Button>
                <Button type="submit" variant="primary" icon={UserPlus} disabled={loading}>
                  {loading ? 'Creating...' : 'Create account'}
                </Button>
              </div>
            </form>
          )}

          <div className="role-dots" aria-hidden="true">
            {rolePanels.map((item, index) => <span key={item.key} className={index === activeRole ? `active ${item.key}` : ''} />)}
          </div>
        </div>
      </section>
    </main>
  );
}

function AuthField({ id, label, error, icon: Icon, children }) {
  return (
    <label className={`auth-field ${error ? 'has-error' : ''}`} htmlFor={id}>
      <span>{label}</span>
      <div>
        {Icon && <Icon size={22} />}
        {children}
      </div>
      {error && <small>{error}</small>}
    </label>
  );
}

function Feature({ icon: Icon, title, text }) {
  return (
    <div className="feature-item">
      <span><Icon size={34} /></span>
      <div>
        <strong>{title}</strong>
        <p>{text}</p>
      </div>
    </div>
  );
}
