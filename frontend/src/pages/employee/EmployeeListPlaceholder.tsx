import React from 'react';
import { Link } from 'react-router-dom';
import { UserCheck, ArrowLeft } from 'lucide-react';

export const EmployeeListPlaceholder: React.FC = () => {
  return (
    <div className="info-banner" style={{ maxWidth: '600px', margin: '3rem auto' }}>
      <div className="banner-header">
        <UserCheck size={24} color="var(--primary)" />
        <h2>Employee Records Module</h2>
      </div>
      <p className="banner-description">
        Scheduled for Phase 4: Full employee directory, search, filtering, and record management.
      </p>
      <Link to="/" className="btn btn-secondary">
        <ArrowLeft size={16} /> Back to Foundation
      </Link>
    </div>
  );
};
