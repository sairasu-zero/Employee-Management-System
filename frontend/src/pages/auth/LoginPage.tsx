import React, { useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import {
  Lock,
  Mail,
  Eye,
  EyeOff,
  Sparkles,
  ArrowRight,
  AlertCircle,
  ShieldCheck,
} from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';
import { FormInput } from '../../components/ui/FormInput';

export const LoginPage: React.FC = () => {
  const { login, isLoading } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [rememberMe, setRememberMe] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<{ email?: string; password?: string }>({});

  const redirectPath = (location.state as { from?: string })?.from || '/admin';

  const validate = (): boolean => {
    const errors: { email?: string; password?: string } = {};

    if (!email.trim()) {
      errors.email = 'Email address is required.';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) {
      errors.email = 'Please enter a valid email address.';
    }

    if (!password) {
      errors.password = 'Password is required.';
    } else if (password.length < 6) {
      errors.password = 'Password must be at least 6 characters.';
    }

    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);

    if (!validate()) return;

    try {
      const result = await login({
        email: email.trim(),
        password,
        rememberMe,
      });

      if (result.requiresMfa) {
        navigate('/auth/mfa', { state: { email: email.trim() } });
        return;
      }

      if (result.success) {
        navigate(redirectPath, { replace: true });
      } else {
        setFormError(result.message || 'Invalid credentials. Please try again.');
      }
    } catch (err: unknown) {
      setFormError(
        err instanceof Error ? err.message : 'An error occurred during authentication.'
      );
    }
  };

  const handleQuickFill = (roleEmail: string, rolePass: string) => {
    setEmail(roleEmail);
    setPassword(rolePass);
    setFieldErrors({});
    setFormError(null);
  };

  return (
    <div className="auth-wrapper">
      <div className="auth-card">
        {/* Header Branding */}
        <div className="auth-header">
          <div className="auth-brand-badge">
            <Sparkles size={16} />
            <span>ERMS Enterprise Portal</span>
          </div>
          <h2 className="auth-title">Welcome Back</h2>
          <p className="auth-subtitle">
            Sign in to access secure workforce records and system modules.
          </p>
        </div>

        {/* Global Error Banner */}
        {formError && (
          <div className="auth-alert danger" role="alert">
            <AlertCircle size={18} style={{ flexShrink: 0 }} />
            <span>{formError}</span>
          </div>
        )}

        <form onSubmit={handleSubmit} noValidate>
          {/* Email Input */}
          <FormInput
            label="Work Email"
            name="email"
            type="email"
            placeholder="name@organization.com"
            value={email}
            onChange={(e) => {
              setEmail(e.target.value);
              if (fieldErrors.email) setFieldErrors((prev) => ({ ...prev, email: undefined }));
            }}
            error={fieldErrors.email}
            icon={Mail}
            autoComplete="email"
            required
          />

          {/* Password Input */}
          <FormInput
            label="Password"
            name="password"
            type={showPassword ? 'text' : 'password'}
            placeholder="••••••••••••"
            value={password}
            onChange={(e) => {
              setPassword(e.target.value);
              if (fieldErrors.password) setFieldErrors((prev) => ({ ...prev, password: undefined }));
            }}
            error={fieldErrors.password}
            icon={Lock}
            autoComplete="current-password"
            required
            rightElement={
              <button
                type="button"
                onClick={() => setShowPassword((prev) => !prev)}
                aria-label={showPassword ? 'Hide password' : 'Show password'}
                style={{ background: 'none', border: 'none', cursor: 'pointer', display: 'flex', color: 'inherit' }}
              >
                {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
              </button>
            }
          />

          {/* Remember Me & Forgot Password Row */}
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              margin: '0.75rem 0 1.5rem',
              fontSize: '0.8125rem',
            }}
          >
            <label
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '0.45rem',
                cursor: 'pointer',
                color: 'var(--text-secondary)',
                userSelect: 'none',
              }}
            >
              <input
                type="checkbox"
                checked={rememberMe}
                onChange={(e) => setRememberMe(e.target.checked)}
                style={{
                  accentColor: 'var(--primary)',
                  width: '16px',
                  height: '16px',
                  borderRadius: 'var(--radius-xs)',
                }}
              />
              <span>Remember this session</span>
            </label>

            <Link
              to="/auth/forgot-password"
              style={{ color: 'var(--primary)', fontWeight: 500 }}
            >
              Forgot password?
            </Link>
          </div>

          {/* Submit Button */}
          <button
            type="submit"
            className="btn btn-primary btn-block btn-lg"
            disabled={isLoading}
          >
            {isLoading ? (
              <>
                <div className="spinner" />
                <span>Authenticating...</span>
              </>
            ) : (
              <>
                <span>Sign In to Dashboard</span>
                <ArrowRight size={18} />
              </>
            )}
          </button>
        </form>

        {/* Quick Demo Fill Buttons for reviewers/testers */}
        <div style={{ marginTop: '1.5rem', paddingTop: '1.25rem', borderTop: '1px solid var(--border-color)' }}>
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '0.4rem',
              fontSize: '0.75rem',
              fontWeight: 600,
              color: 'var(--text-secondary)',
              marginBottom: '0.625rem',
            }}
          >
            <ShieldCheck size={14} style={{ color: 'var(--primary)' }} />
            <span>Quick Test Credentials:</span>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.5rem' }}>
            <button
              type="button"
              className="btn btn-secondary btn-sm"
              onClick={() => handleQuickFill('admin@erms.local', 'AdminSecure!2026')}
            >
              Admin (with MFA)
            </button>
            <button
              type="button"
              className="btn btn-secondary btn-sm"
              onClick={() => handleQuickFill('hr@erms.local', 'HrPass!2026')}
            >
              HR Specialist
            </button>
            <button
              type="button"
              className="btn btn-secondary btn-sm"
              onClick={() => handleQuickFill('manager@erms.local', 'ManagerPass!2026')}
            >
              Manager
            </button>
            <button
              type="button"
              className="btn btn-secondary btn-sm"
              onClick={() => handleQuickFill('employee@erms.local', 'EmployeePass!2026')}
            >
              Employee
            </button>
          </div>
        </div>

        {/* Register Link */}
        <div className="auth-footer">
          <span>Need an enterprise account? </span>
          <Link to="/auth/register" style={{ fontWeight: 600 }}>
            Register new employee
          </Link>
        </div>
      </div>
    </div>
  );
};

export default LoginPage;
