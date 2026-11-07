import React from 'react';
import { StatusBadgeProps } from '../../types';

export const StatusBadge: React.FC<StatusBadgeProps> = ({
  status,
  label,
  pulse = false,
  size = 'md',
  className = '',
}) => {
  const displayLabel = label || status.charAt(0).toUpperCase() + status.slice(1);

  return (
    <span
      className={`status-badge status-${status} badge-${size} ${className}`}
      role="status"
    >
      <span className={`badge-dot ${pulse ? 'pulse' : ''}`} />
      <span>{displayLabel}</span>
    </span>
  );
};

export default StatusBadge;
