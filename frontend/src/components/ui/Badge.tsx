import React from 'react';
import { ConnectionStatus } from '../../types';

interface BadgeProps {
  status: ConnectionStatus | 'primary';
  children: React.ReactNode;
  pulse?: boolean;
}

export const Badge: React.FC<BadgeProps> = ({ status, children, pulse = false }) => {
  const getBadgeClass = () => {
    switch (status) {
      case 'connected':
        return 'badge badge-success';
      case 'pending':
        return 'badge badge-warning';
      case 'error':
        return 'badge badge-error';
      case 'primary':
      default:
        return 'badge badge-primary';
    }
  };

  return (
    <span className={getBadgeClass()}>
      <span className={`badge-dot ${pulse ? 'pulse-dot' : ''}`} />
      {children}
    </span>
  );
};
