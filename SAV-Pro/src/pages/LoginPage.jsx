import {
  CalendarDays,
  CheckCircle,
  ChevronDown,
  ClipboardList,
  Eye,
  Globe,
  Headphones,
  Lock,
  LogIn,
  Mail,
  MessageSquare,
  Monitor,
  ShieldCheck,
  Smartphone,
  Truck,
  UserRound,
  Users,
  Wrench
} from 'lucide-react';
import { useState } from 'react';
import { Button, Logo } from '../components/ui';
import { getFriendlyApiError } from '../utils/errorMessages';

const demoAccounts = [
  { label: 'Admin', email: 'admin@local', password: 'ChangeMe_Admin_2026!', icon: ShieldCheck },
  { label: 'SAV', email: 'youssef.trabelsi.sav@sav.local', password: 'SavAgent!123', icon: Users },
  { label: 'Technician', email: 'nour.benali.tech@sav.local', password: 'Tech!1234', icon: Wrench },
  { label: 'Client', email: 'sami.benameur.client@sav.local', password: 'Client!123', icon: UserRound }
];

export function LoginPage({ onLogin }) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [loading, setLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [language, setLanguage] = useState('English');

  async function submit(event) {
    event.preventDefault();
    setError('');
    setNotice('');
    setLoading(true);
    try {
      await new Promise(resolve => window.setTimeout(resolve, 250));
      await onLogin(email, password);
    } catch (err) {
      setError(getFriendlyApiError(err));
    } finally {
      setLoading(false);
    }
  }

  async function demo(account) {
    setEmail(account.email);
    setPassword(account.password);
    setError('');
    setNotice('');
    setLoading(true);
    try {
      await onLogin(account.email, account.password);
    } catch (err) {
      setError(getFriendlyApiError(err));
    } finally {
      setLoading(false);
    }
  }

  function showNotice(message) {
    setNotice(message);
    setError('');
  }

  return (
    <main className="login-page">
      <button
        type="button"
        className="language-select"
        onClick={() => {
          setLanguage(current => current === 'English' ? 'Français' : 'English');
          showNotice('Language preference updated.');
        }}
      >
        <Globe size={22} />
        {language}
        <ChevronDown size={18} />
      </button>

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
        <form className="login-card" onSubmit={submit}>
          <header>
            <h2>Welcome back 👋</h2>
            <p>Sign in to your SAV Pro account to continue</p>
          </header>

          <label className="auth-field">
            <span>Email</span>
            <div>
              <Mail size={23} />
              <input value={email} onChange={event => setEmail(event.target.value)} placeholder="Enter your email" />
            </div>
          </label>

          <label className="auth-field">
            <span>Password</span>
            <div>
              <Lock size={22} />
              <input value={password} onChange={event => setPassword(event.target.value)} type={showPassword ? 'text' : 'password'} placeholder="Enter your password" />
              <button type="button" className="password-eye" onClick={() => setShowPassword(current => !current)} aria-label={showPassword ? 'Hide password' : 'Show password'}>
                <Eye size={23} />
              </button>
            </div>
          </label>

          <div className="auth-row">
            <label className="remember-me">
              <input type="checkbox" defaultChecked />
              <span>Remember me</span>
            </label>
            <button type="button" className="link-button" onClick={() => showNotice('Password reset instructions sent for demo mode.')}>Forgot password?</button>
          </div>

          {error && <p className="form-error">{error}</p>}
          {notice && <p className="form-success">{notice}</p>}

          <Button type="submit" variant="primary" size="lg" icon={LogIn} className="sign-in-btn" disabled={loading}>
            {loading ? 'Signing In...' : 'Sign In'}
          </Button>

          <div className="demo-divider">
            <span />
            <small>Or try a demo account</small>
            <span />
          </div>

          <div className="demo-grid">
            {demoAccounts.map(account => {
              const Icon = account.icon;
              return (
                <button type="button" key={account.email} className="demo-card" onClick={() => demo(account)}>
                  <Icon size={22} />
                  <strong>{account.label}</strong>
                  <small>{account.email}</small>
                </button>
              );
            })}
          </div>
        </form>

        <footer className="login-footer">
          <span>© 2024 SAV Pro. All rights reserved.</span>
          <b>•</b>
          <button type="button" onClick={() => showNotice('Privacy policy opened in demo mode.')}>Privacy Policy</button>
          <b>•</b>
          <button type="button" onClick={() => showNotice('Terms of service opened in demo mode.')}>Terms of Service</button>
          <b>•</b>
          <button type="button" onClick={() => showNotice('Support contact opened in demo mode.')}>Support</button>
        </footer>
      </section>
    </main>
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
