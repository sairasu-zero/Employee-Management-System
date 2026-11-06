import React from 'react';
import { ShieldCheck, Activity } from 'lucide-react';

export const Header: React.FC = () => {
  return (
    <header className="erms-header">
      <div className="header-content">
        <div className="header-brand">
          <div className="brand-icon-wrapper">
            <ShieldCheck size={24} />
          </div>
          <div>
            <h1 className="brand-title">EMPLOYEE RECORDS MANAGEMENT SYSTEM</h1>
            <p className="brand-subtitle">Enterprise Security & Compliance Core</p>
          </div>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
          <span className="badge badge-primary">
            <Activity size={12} />
            Phase 1 Foundation
          </span>
        </div>
      </div>
    </header>
  );
};
