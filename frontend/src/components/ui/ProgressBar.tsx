import React from 'react';
import { ProgressBarProps } from '../../types';

export const ProgressBar: React.FC<ProgressBarProps> = ({
  value,
  label,
  showPercentage = true,
  color,
  height = 8,
  animated = true,
  status = 'default',
  className = '',
}) => {
  // Clamp value between 0 and 100
  const clampedValue = Math.min(100, Math.max(0, value));

  return (
    <div
      className={`progress-bar-container ${className}`}
      role="progressbar"
      aria-valuenow={clampedValue}
      aria-valuemin={0}
      aria-valuemax={100}
    >
      {(label || showPercentage) && (
        <div className="progress-bar-header">
          {label && <span>{label}</span>}
          {showPercentage && <span>{Math.round(clampedValue)}%</span>}
        </div>
      )}

      <div
        className="progress-track"
        style={{ height: `${height}px` }}
      >
        <div
          className={`progress-fill ${status !== 'default' ? `status-${status}` : ''} ${
            animated ? 'animated' : ''
          }`}
          style={{
            width: `${clampedValue}%`,
            backgroundColor: color || undefined,
          }}
        />
      </div>
    </div>
  );
};

export default ProgressBar;
