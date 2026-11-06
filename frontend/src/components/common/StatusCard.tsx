import React from 'react';
import { CheckCircle2, Clock, AlertCircle } from 'lucide-react';
import { ConnectionStatus } from '../../types';
import { Badge } from '../ui/Badge';

interface StatusCardProps {
  title: string;
  category: 'frontend' | 'backend' | 'database';
  status: ConnectionStatus;
  statusLabel: string;
  detail: string;
  icon: React.ReactNode;
  extraInfo?: React.ReactNode;
}

export const StatusCard: React.FC<StatusCardProps> = ({
  title,
  category,
  status,
  statusLabel,
  detail,
  icon,
  extraInfo,
}) => {
  const getStatusIcon = () => {
    switch (status) {
      case 'connected':
        return <CheckCircle2 size={18} color="var(--status-success)" />;
      case 'pending':
        return <Clock size={18} color="var(--status-warning)" />;
      case 'error':
        return <AlertCircle size={18} color="var(--status-error)" />;
    }
  };

  return (
    <div className="status-card">
      <div className="status-card-header">
        <div className="card-icon-container">
          <div className={`card-icon ${category}`}>{icon}</div>
          <div className="card-title-group">
            <h3>{title}</h3>
            <p>{category.toUpperCase()} COMPONENT</p>
          </div>
        </div>
        <Badge status={status} pulse={status === 'connected'}>
          {statusLabel}
        </Badge>
      </div>

      <div className="status-card-body">
        <div className="status-label">
          {getStatusIcon()}
          <span>{statusLabel}</span>
        </div>
        <div className="status-detail">{detail}</div>
        {extraInfo && <div style={{ marginTop: '0.75rem' }}>{extraInfo}</div>}
      </div>
    </div>
  );
};
