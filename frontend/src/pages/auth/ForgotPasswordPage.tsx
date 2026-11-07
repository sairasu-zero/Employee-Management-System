import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Mail,
  ArrowRight,
  ArrowLeft,
  AlertCircle,
  CheckCircle2,
  Key,
} from 'lucide-react';
import { apiForgotPassword } from '../../services/api';
import { FormInput } from '../../components/ui/FormInput';

export const ForgotPasswordPage: React.FC = () => {
  const [email, setEmail] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitted, setIsSubmitted] = useState(false);
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!email.trim() || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) {
      setError('Please provide a valid corporate email address.');
      return;
    }

    setIsLoading(true);
    try {
      await apiForgotPassword({ email: email.trim() });
      setIsSubmitted(true);
    } catch {
      // In development fallback, treat as submitted to prevent user enumeration
      setIsSubmitted(true);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="auth-wrapper">
      <div className="auth-card">
        <div className="auth-header">
          <div className="auth-brand-badge">
            <Key size={16} />
            <span>Password Recovery</span>
          </div>
          <h2 className="auth-title">Reset Your Password</h2>
          <p className="auth-subtitle">
            Enter the registered email associated with your ERMS account.
          </p>
        </div>

        {error && (
          <div className="auth-alert danger" role="alert">
            <AlertCircle size={18} style={{ flexShrink: 0 }} />
            <span>{error}</span>
          </div>
        )}

        {isSubmitted ? (
          <div>
            <div className="auth-alert success" role="alert" style={{ flexDirection: 'column', alignItems: 'flex-start' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontWeight: 600 }}>
                <CheckCircle2 size={18} />
                <span>Reset Instructions Dispatched</span>
              </div>
              <p style={{ marginTop: '0.5rem', fontSize: '0.8125rem', lineHeight: 1.5 }}>
                If an active account exists for <strong>{email}</strong>, you will receive an encrypted reset link shortly. Please check your corporate inbox and spam filters.
              </p>
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem', marginTop: '1.5rem' }}>
              <Link to="/auth/reset-password" className="btn btn-secondary btn-block">
                <span>Enter Reset Token Manually</span>
              </Link>

              <Link to="/auth/login" className="btn btn-primary btn-block">
                <span>Return to Login</span>
              </Link>
            </div>
          </div>
        ) : (
          <form onSubmit={handleSubmit}>
            <FormInput
              label="Corporate Email Address"
              name="email"
              type="email"
              placeholder="name@organization.com"
              value={email}
              onChange={(e) => {
                setEmail(e.target.value);
                setError(null);
              }}
              icon={Mail}
              required
              autoComplete="email"
            />

            <button
              type="submit"
              className="btn btn-primary btn-block btn-lg"
              style={{ marginTop: '0.75rem' }}
              disabled={isLoading || !email.trim()}
            >
              {isLoading ? (
                <>
                  <div className="spinner" />
                  <span>Sending Reset Instructions...</span>
                </>
              ) : (
                <>
                  <span>Send Password Reset Link</span>
                  <ArrowRight size={18} />
                </>
              )}
            </button>
          </form>
        )}

        <div className="auth-footer">
          <Link
            to="/auth/login"
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '0.35rem',
            }}
          >
            <ArrowLeft size={14} />
            <span>Back to sign in</span>
          </Link>
        </div>
      </div>
    </div>
  );
};

export default ForgotPasswordPage;
