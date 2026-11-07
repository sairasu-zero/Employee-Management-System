import React, { useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import {
  KeyRound,
  ShieldAlert,
  ArrowRight,
  ArrowLeft,
  AlertCircle,
} from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';
import { FormInput } from '../../components/ui/FormInput';

export const RecoveryCodePage: React.FC = () => {
  const { verifyMfa, pendingEmail, isLoading } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const email = (location.state as { email?: string })?.email || pendingEmail || 'admin@erms.local';
  const [recoveryCode, setRecoveryCode] = useState('');
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    const formattedCode = recoveryCode.trim();
    if (!formattedCode) {
      setError('Please provide your emergency recovery code.');
      return;
    }

    try {
      const result = await verifyMfa(formattedCode, true);
      if (result.success) {
        navigate('/admin', { replace: true });
      } else {
        setError(result.message || 'Invalid or expired recovery code.');
      }
    } catch (err: unknown) {
      setError(
        err instanceof Error ? err.message : 'Failed to verify recovery code.'
      );
    }
  };

  return (
    <div className="auth-wrapper">
      <div className="auth-card">
        <div className="auth-header">
          <div className="auth-brand-badge" style={{ background: '#FFFBEB', color: '#D97706', borderColor: '#FDE68A' }}>
            <ShieldAlert size={16} />
            <span>Emergency Account Recovery</span>
          </div>
          <h2 className="auth-title">Recovery Code</h2>
          <p className="auth-subtitle">
            Enter one of your 10 saved emergency backup codes to regain access for{' '}
            <strong style={{ color: 'var(--text-main)' }}>{email}</strong>.
          </p>
        </div>

        {error && (
          <div className="auth-alert danger" role="alert">
            <AlertCircle size={18} style={{ flexShrink: 0 }} />
            <span>{error}</span>
          </div>
        )}

        <div
          style={{
            background: 'var(--bg-subtle)',
            border: '1px solid var(--border-color)',
            borderRadius: 'var(--radius-md)',
            padding: '0.875rem 1rem',
            fontSize: '0.8125rem',
            color: 'var(--text-secondary)',
            marginBottom: '1.25rem',
            lineHeight: 1.5,
          }}
        >
          <strong>Security Notice:</strong> Each recovery code can only be used once. Once signed in, you should regenerate your backup recovery keys in Security Settings.
        </div>

        <form onSubmit={handleSubmit}>
          <FormInput
            label="Emergency Recovery Code"
            name="recoveryCode"
            type="text"
            placeholder="e.g. ABCD-EFGH-1234"
            value={recoveryCode}
            onChange={(e) => {
              setRecoveryCode(e.target.value);
              setError(null);
            }}
            icon={KeyRound}
            required
            autoComplete="off"
            helperText="Formed of alphanumeric segments saved during MFA activation"
          />

          <button
            type="submit"
            className="btn btn-primary btn-block btn-lg"
            style={{ marginTop: '0.5rem' }}
            disabled={isLoading || !recoveryCode.trim()}
          >
            {isLoading ? (
              <>
                <div className="spinner" />
                <span>Verifying Recovery Code...</span>
              </>
            ) : (
              <>
                <span>Use Recovery Code & Sign In</span>
                <ArrowRight size={18} />
              </>
            )}
          </button>
        </form>

        <div
          style={{
            marginTop: '1.5rem',
            paddingTop: '1.25rem',
            borderTop: '1px solid var(--border-color)',
            textAlign: 'center',
          }}
        >
          <Link
            to="/auth/mfa"
            state={{ email }}
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '0.35rem',
              fontSize: '0.8125rem',
              color: 'var(--text-secondary)',
            }}
          >
            <ArrowLeft size={14} />
            <span>Return to Authenticator App (TOTP)</span>
          </Link>
        </div>
      </div>
    </div>
  );
};

export default RecoveryCodePage;
