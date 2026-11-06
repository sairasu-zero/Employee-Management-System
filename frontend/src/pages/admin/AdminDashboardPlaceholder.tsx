import React from 'react';
import { Link } from 'react-router-dom';
import { ShieldCheck, ArrowLeft } from 'lucide-react';

export const AdminDashboardPlaceholder: React.FC = () => {
  return (
    <div className="info-banner" style={{ maxWidth: '600px', margin: '3rem auto' }}>
      <div className="banner-header">
        <ShieldCheck size={24} color="var(--primary)" />
        <h2>Administrator Module</h2>
      </div>
      <p className="banner-description">
        Scheduled for Phase 3: Role-based access control, security policies, and user administration.
      </p>
      <Link to="/" className="btn btn-secondary">
        <ArrowLeft size={16} /> Back to Foundation
      </Link>
    </div>
  );
};
