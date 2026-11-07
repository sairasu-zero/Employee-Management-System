import React from 'react';
import { useNavigate } from 'react-router-dom';
import { ShieldAlert, ArrowLeft, Home } from 'lucide-react';
import { useAuth } from '../contexts/AuthContext';

export const UnauthorizedPage: React.FC = () => {
  const navigate = useNavigate();
  const { user, role } = useAuth();

  const handleReturnDashboard = () => {
    switch (role) {
      case 'Admin':
        navigate('/admin');
        break;
      case 'HR':
        navigate('/hr');
        break;
      case 'Manager':
        navigate('/manager');
        break;
      case 'Employee':
      default:
        navigate('/employee');
        break;
    }
  };

  return (
    <div
      style={{
        minHeight: '60vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '2rem 1rem',
      }}
    >
      <div
        className="table-card"
        style={{
          maxWidth: '520px',
          padding: '2.5rem',
          textAlign: 'center',
          alignItems: 'center',
        }}
      >
        <div
          style={{
            width: '64px',
            height: '64px',
            borderRadius: '50%',
            background: 'var(--status-danger-bg)',
            color: 'var(--status-danger)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            marginBottom: '1.25rem',
            border: '1px solid var(--status-danger-border)',
          }}
        >
          <ShieldAlert size={32} />
        </div>

        <h2 style={{ fontSize: '1.5rem', fontWeight: 700, color: 'var(--text-main)' }}>
          Access Restricted (403)
        </h2>

        <p
          style={{
            color: 'var(--text-secondary)',
            fontSize: '0.9375rem',
            marginTop: '0.5rem',
            lineHeight: 1.5,
          }}
        >
          You are currently signed in as <strong>{user?.fullName}</strong> with role{' '}
          <strong style={{ color: 'var(--primary)' }}>{role}</strong>. Your account
          does not possess sufficient privileges to view this resource.
        </p>

        <div
          style={{
            display: 'flex',
            gap: '0.75rem',
            marginTop: '1.75rem',
            width: '100%',
            justifyContent: 'center',
          }}
        >
          <button
            type="button"
            className="btn btn-secondary"
            onClick={() => navigate(-1)}
          >
            <ArrowLeft size={16} />
            <span>Go Back</span>
          </button>

          <button
            type="button"
            className="btn btn-primary"
            onClick={handleReturnDashboard}
          >
            <Home size={16} />
            <span>Go to My Dashboard</span>
          </button>
        </div>
      </div>
    </div>
  );
};

export default UnauthorizedPage;
