import React from 'react';
import { Link } from 'react-router-dom';
import { Lock, ArrowLeft } from 'lucide-react';

export const LoginPage: React.FC = () => {
  return (
    <div className="info-banner" style={{ maxWidth: '500px', margin: '3rem auto' }}>
      <div className="banner-header">
        <Lock size={24} color="var(--primary)" />
        <h2>Authentication Module</h2>
      </div>
      <p className="banner-description">
        Scheduled for Phase 2: User authentication, MFA verification, and secure session management.
      </p>
      <Link to="/" className="btn btn-secondary">
        <ArrowLeft size={16} /> Back to Foundation
      </Link>
    </div>
  );
};
