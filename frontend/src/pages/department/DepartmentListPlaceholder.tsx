import React from 'react';
import { Link } from 'react-router-dom';
import { Building2, ArrowLeft } from 'lucide-react';

export const DepartmentListPlaceholder: React.FC = () => {
  return (
    <div className="info-banner" style={{ maxWidth: '600px', margin: '3rem auto' }}>
      <div className="banner-header">
        <Building2 size={24} color="var(--primary)" />
        <h2>Departments Module</h2>
      </div>
      <p className="banner-description">
        Scheduled for Phase 4: Department hierarchies, cost centers, and organizational mapping.
      </p>
      <Link to="/" className="btn btn-secondary">
        <ArrowLeft size={16} /> Back to Foundation
      </Link>
    </div>
  );
};
