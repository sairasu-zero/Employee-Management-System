import React from 'react';
import { TrendingUp, TrendingDown } from 'lucide-react';
import { MetricCardProps } from '../../types';

export const MetricCard: React.FC<MetricCardProps> = ({
  title,
  value,
  change,
  icon: Icon,
  iconColor,
  iconBg,
  trendLabel,
  description,
  className = '',
  onClick,
}) => {
  return (
    <div
      className={`metric-card ${onClick ? 'cursor-pointer' : ''} ${className}`}
      onClick={onClick}
      role={onClick ? 'button' : undefined}
      tabIndex={onClick ? 0 : undefined}
    >
      <div className="metric-card-top">
        <span className="metric-card-title">{title}</span>
        <div
          className="metric-card-icon-wrapper"
          style={{
            backgroundColor: iconBg || 'var(--primary-light)',
            color: iconColor || 'var(--primary)',
          }}
        >
          <Icon size={20} />
        </div>
      </div>

      <div className="metric-card-bottom">
        <div className="metric-card-value">{value}</div>

        {change && (
          <div
            className={`metric-card-change ${
              change.isPositive !== false ? 'positive' : 'negative'
            }`}
            title={trendLabel || undefined}
          >
            {change.isPositive !== false ? (
              <TrendingUp size={14} />
            ) : (
              <TrendingDown size={14} />
            )}
            <span>
              {typeof change.value === 'number' && change.value > 0 ? `+${change.value}%` : `${change.value}`}
            </span>
          </div>
        )}
      </div>

      {(description || change?.period) && (
        <div className="metric-card-subtext">
          {description || (change?.period ? `vs. ${change.period}` : '')}
        </div>
      )}
    </div>
  );
};

export default MetricCard;
