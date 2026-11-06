import React from 'react';
import { Link } from 'react-router-dom';
import { Users, ArrowLeft } from 'lucide-react';

export const HrDashboardPlaceholder: React.FC = () => {
  return (
    <div className="info-banner" style={{ maxWidth: '600px', margin: '3rem auto' }}>
      <div className="banner-header">
        <Users size={24} color="var(--primary)" />
        <h2>Human Resources Module</h2>
      </div>
      <p className="banner-description">
        Scheduled for Phase 4: Employee profiles, onboarding, lifecycle events, and compliance.
      </p>
      <Link to="/" className="btn btn-secondary">
        <ArrowLeft size={16} /> Back to Foundation
      </Link>
    </div>
  );
};
