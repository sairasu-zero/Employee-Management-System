import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  User,
  BadgeAlert,
  Mail,
  Building,
  Lock,
  Eye,
  EyeOff,
  Sparkles,
  ArrowRight,
  AlertCircle,
  CheckCircle2,
} from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';
import { FormInput } from '../../components/ui/FormInput';

export const RegisterPage: React.FC = () => {
  const { register, isLoading } = useAuth();
  const navigate = useNavigate();

  const [formData, setFormData] = useState({
    fullName: '',
    employeeId: '',
    email: '',
    department: 'Engineering',
    password: '',
    confirmPassword: '',
  });

  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [serverError, setServerError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const departments = [
    'Executive Administration',
    'Human Resources',
    'Engineering',
    'Finance & Accounting',
    'Legal & Compliance',
    'Marketing',
    'Operations',
  ];

  const validate = (): boolean => {
    const errs: Record<string, string> = {};

    if (!formData.fullName.trim()) {
      errs.fullName = 'Full legal name is required.';
    }
    if (!formData.employeeId.trim()) {
      errs.employeeId = 'Employee ID is required (e.g. EMP-104).';
    }
    if (!formData.email.trim()) {
      errs.email = 'Work email address is required.';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email.trim())) {
      errs.email = 'Please provide a valid email format.';
    }
    if (!formData.password) {
      errs.password = 'Password is required.';
    } else if (formData.password.length < 8) {
      errs.password = 'Password must be at least 8 characters.';
    }
    if (formData.password !== formData.confirmPassword) {
      errs.confirmPassword = 'Passwords do not match.';
    }

    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
    if (errors[name]) {
      setErrors((prev) => ({ ...prev, [name]: '' }));
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setServerError(null);
    setSuccessMessage(null);

    if (!validate()) return;

    try {
      const result = await register({
        fullName: formData.fullName.trim(),
        employeeId: formData.employeeId.trim(),
        email: formData.email.trim(),
        department: formData.department,
        password: formData.password,
        confirmPassword: formData.confirmPassword,
      });

      if (result.success) {
        setSuccessMessage(result.message || 'Account registration successful!');
        setTimeout(() => {
          navigate('/auth/login');
        }, 1800);
      } else {
        setServerError(result.message || 'Failed to complete registration.');
      }
    } catch (err: unknown) {
      setServerError(
        err instanceof Error ? err.message : 'An error occurred during registration.'
      );
    }
  };

  return (
    <div className="auth-wrapper">
      <div className="auth-card" style={{ maxWidth: '480px' }}>
        <div className="auth-header">
          <div className="auth-brand-badge">
            <Sparkles size={16} />
            <span>ERMS Workforce Enrollment</span>
          </div>
          <h2 className="auth-title">Create Account</h2>
          <p className="auth-subtitle">
            Register your staff credentials to access ERMS services.
          </p>
        </div>

        {serverError && (
          <div className="auth-alert danger" role="alert">
            <AlertCircle size={18} style={{ flexShrink: 0 }} />
            <span>{serverError}</span>
          </div>
        )}

        {successMessage && (
          <div className="auth-alert success" role="alert">
            <CheckCircle2 size={18} style={{ flexShrink: 0 }} />
            <span>{successMessage} Redirecting to login...</span>
          </div>
        )}

        <form onSubmit={handleSubmit} noValidate>
          <FormInput
            label="Full Name"
            name="fullName"
            type="text"
            placeholder="e.g. Jane Doe"
            value={formData.fullName}
            onChange={handleChange}
            error={errors.fullName}
            icon={User}
            required
          />

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem' }}>
            <FormInput
              label="Employee ID"
              name="employeeId"
              type="text"
              placeholder="EMP-104"
              value={formData.employeeId}
              onChange={handleChange}
              error={errors.employeeId}
              icon={BadgeAlert}
              required
            />

            <div className="form-group">
              <label htmlFor="reg-department" className="form-label">
                <span>Department <span className="form-label-required">*</span></span>
              </label>
              <div className="input-container">
                <span className="input-icon-left">
                  <Building size={18} />
                </span>
                <select
                  id="reg-department"
                  name="department"
                  value={formData.department}
                  onChange={handleChange}
                  className="form-control"
                  style={{ background: 'transparent', cursor: 'pointer' }}
                >
                  {departments.map((dept) => (
                    <option key={dept} value={dept}>
                      {dept}
                    </option>
                  ))}
                </select>
              </div>
            </div>
          </div>

          <FormInput
            label="Work Email"
            name="email"
            type="email"
            placeholder="j.doe@erms.local"
            value={formData.email}
            onChange={handleChange}
            error={errors.email}
            icon={Mail}
            required
          />

          <FormInput
            label="Password"
            name="password"
            type={showPassword ? 'text' : 'password'}
            placeholder="Min. 8 characters"
            value={formData.password}
            onChange={handleChange}
            error={errors.password}
            icon={Lock}
            required
            rightElement={
              <button
                type="button"
                onClick={() => setShowPassword((prev) => !prev)}
                aria-label={showPassword ? 'Hide password' : 'Show password'}
                style={{ background: 'none', border: 'none', cursor: 'pointer', display: 'flex' }}
              >
                {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
              </button>
            }
          />

          <FormInput
            label="Confirm Password"
            name="confirmPassword"
            type={showConfirmPassword ? 'text' : 'password'}
            placeholder="Repeat password"
            value={formData.confirmPassword}
            onChange={handleChange}
            error={errors.confirmPassword}
            icon={Lock}
            required
            rightElement={
              <button
                type="button"
                onClick={() => setShowConfirmPassword((prev) => !prev)}
                aria-label={showConfirmPassword ? 'Hide password' : 'Show password'}
                style={{ background: 'none', border: 'none', cursor: 'pointer', display: 'flex' }}
              >
                {showConfirmPassword ? <EyeOff size={18} /> : <Eye size={18} />}
              </button>
            }
          />

          <button
            type="submit"
            className="btn btn-primary btn-block btn-lg"
            style={{ marginTop: '0.75rem' }}
            disabled={isLoading || !!successMessage}
          >
            {isLoading ? (
              <>
                <div className="spinner" />
                <span>Creating Account...</span>
              </>
            ) : (
              <>
                <span>Complete Registration</span>
                <ArrowRight size={18} />
              </>
            )}
          </button>
        </form>

        <div className="auth-footer">
          <span>Already have an account? </span>
          <Link to="/auth/login" style={{ fontWeight: 600 }}>
            Sign In
          </Link>
        </div>
      </div>
    </div>
  );
};

export default RegisterPage;
