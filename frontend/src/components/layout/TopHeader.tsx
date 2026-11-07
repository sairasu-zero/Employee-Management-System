import React, { useState, useEffect, useRef } from 'react';
import { useLocation, Link, useNavigate } from 'react-router-dom';
import {
  Bell,
  Clock,
  Menu,
  ChevronRight,
  ShieldCheck,
  AlertTriangle,
  FileText,
  Calendar,
  CheckCircle2,
  XCircle,
  Radio,
} from 'lucide-react';
import { signalRService } from '../../services/signalr';
import { NotificationMessageItem } from '../../types';

interface TopHeaderProps {
  onToggleSidebar?: () => void;
}

interface UiNotification extends NotificationMessageItem {
  read: boolean;
}

export const TopHeader: React.FC<TopHeaderProps> = ({ onToggleSidebar }) => {
  const location = useLocation();
  const navigate = useNavigate();
  const [currentTime, setCurrentTime] = useState<string>('');
  const [currentDate, setCurrentDate] = useState<string>('');
  const [notificationsOpen, setNotificationsOpen] = useState<boolean>(false);
  const [notifications, setNotifications] = useState<UiNotification[]>([]);
  const [connectionStatus, setConnectionStatus] = useState<
    'connected' | 'connecting' | 'disconnected' | 'reconnecting'
  >('disconnected');

  const notificationRef = useRef<HTMLDivElement>(null);

  // Dynamic live date & time
  useEffect(() => {
    const updateDateTime = () => {
      const now = new Date();
      const dateFormatted = now.toLocaleDateString('en-US', {
        weekday: 'short',
        month: 'short',
        day: 'numeric',
        year: 'numeric',
      });
      const timeFormatted = now.toLocaleTimeString('en-US', {
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
        hour12: true,
      });

      setCurrentDate(dateFormatted);
      setCurrentTime(timeFormatted);
    };

    updateDateTime();
    const interval = setInterval(updateDateTime, 1000);
    return () => clearInterval(interval);
  }, []);

  // Connect to SignalR Notification Hub
  useEffect(() => {
    signalRService.startConnection();

    const unsubNotification = signalRService.onNotification((item) => {
      setNotifications((prev) => [
        { ...item, read: false },
        ...prev.slice(0, 49), // retain last 50
      ]);
    });

    const unsubStatus = signalRService.onStatusChange((status) => {
      setConnectionStatus(status);
    });

    return () => {
      unsubNotification();
      unsubStatus();
    };
  }, []);

  // Close notifications dropdown on outside click
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (
        notificationRef.current &&
        !notificationRef.current.contains(event.target as Node)
      ) {
        setNotificationsOpen(false);
      }
    };

    if (notificationsOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [notificationsOpen]);

  // Determine breadcrumb and title from pathname
  const pathParts = location.pathname.split('/').filter(Boolean);

  const getModuleTitle = () => {
    if (location.pathname.startsWith('/admin/rbac')) return 'Access Control & RBAC Matrix';
    if (location.pathname.startsWith('/admin')) return 'Administration Console';
    if (location.pathname.startsWith('/hr')) return 'Human Resources Hub';
    if (location.pathname.startsWith('/manager')) return 'Manager Workspace';
    if (location.pathname.startsWith('/employee')) return 'Employee Self-Service';
    if (location.pathname.startsWith('/departments')) return 'Department Hierarchy';
    if (location.pathname.startsWith('/employees')) return 'Employee Directory';
    if (location.pathname.startsWith('/foundation')) return 'System Diagnostics';
    return 'Dashboard';
  };

  const unreadCount = notifications.filter((n) => !n.read).length;

  const markAllRead = () => {
    setNotifications((prev) => prev.map((n) => ({ ...n, read: true })));
  };

  const handleNotificationClick = (item: UiNotification) => {
    setNotifications((prev) =>
      prev.map((n) => (n.id === item.id ? { ...n, read: true } : n))
    );
    if (item.actionUrl) {
      setNotificationsOpen(false);
      navigate(item.actionUrl);
    }
  };

  const getIconForCategory = (category: string, severity: string) => {
    if (severity === 'danger') return <XCircle size={16} />;
    if (severity === 'success') return <CheckCircle2 size={16} />;
    if (severity === 'warning') return <AlertTriangle size={16} />;

    switch (category.toLowerCase()) {
      case 'leave':
        return <Calendar size={16} />;
      case 'document':
        return <FileText size={16} />;
      case 'security':
        return <ShieldCheck size={16} />;
      default:
        return <Radio size={16} />;
    }
  };

  const getSeverityColor = (severity: string) => {
    switch (severity) {
      case 'danger':
        return 'var(--status-danger)';
      case 'warning':
        return 'var(--status-warning)';
      case 'success':
        return 'var(--status-success)';
      default:
        return 'var(--primary)';
    }
  };

  const formatRelativeTime = (timestamp: string) => {
    try {
      const diffMs = Date.now() - new Date(timestamp).getTime();
      const diffSec = Math.floor(diffMs / 1000);
      if (diffSec < 60) return 'Just now';
      const diffMin = Math.floor(diffSec / 60);
      if (diffMin < 60) return `${diffMin}m ago`;
      const diffHrs = Math.floor(diffMin / 60);
      if (diffHrs < 24) return `${diffHrs}h ago`;
      return new Date(timestamp).toLocaleDateString();
    } catch {
      return timestamp;
    }
  };

  return (
    <header className="top-header">
      {/* Left: Hamburger + Breadcrumbs + Module Title */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
        {onToggleSidebar && (
          <button
            type="button"
            className="icon-btn"
            onClick={onToggleSidebar}
            aria-label="Toggle navigation menu"
            style={{ display: 'inline-flex' }}
          >
            <Menu size={18} />
          </button>
        )}

        <div className="header-left">
          <nav className="header-breadcrumbs" aria-label="Breadcrumb">
            <Link to="/">Home</Link>
            {pathParts.map((part, index) => {
              const url = `/${pathParts.slice(0, index + 1).join('/')}`;
              const isLast = index === pathParts.length - 1;
              const formattedPart =
                part.charAt(0).toUpperCase() + part.slice(1).replace(/-/g, ' ');

              return (
                <React.Fragment key={url}>
                  <ChevronRight size={12} style={{ color: 'var(--text-muted)' }} />
                  {isLast ? (
                    <span style={{ color: 'var(--text-main)', fontWeight: 600 }}>
                      {formattedPart}
                    </span>
                  ) : (
                    <Link to={url}>{formattedPart}</Link>
                  )}
                </React.Fragment>
              );
            })}
          </nav>

          <h1 className="header-title">{getModuleTitle()}</h1>
        </div>
      </div>

      {/* Right: Date/Time + Operational Status Pill + Notification Bell */}
      <div className="header-right">
        {/* Live Date/Time */}
        <div className="header-datetime" title="Live System Time">
          <Clock size={15} style={{ color: 'var(--primary)' }} />
          <span>
            {currentDate} • <strong style={{ fontWeight: 600 }}>{currentTime}</strong>
          </span>
        </div>

        {/* Real-time SignalR Operational Status Pill */}
        <div
          className="header-status-pill"
          title={`SignalR Socket Status: ${connectionStatus}`}
        >
          <span
            className="header-status-dot"
            style={{
              backgroundColor:
                connectionStatus === 'connected'
                  ? 'var(--status-success)'
                  : connectionStatus === 'connecting' || connectionStatus === 'reconnecting'
                  ? 'var(--status-warning)'
                  : 'var(--text-muted)',
            }}
          />
          <span style={{ textTransform: 'capitalize' }}>
            {connectionStatus === 'connected' ? 'Live Connected' : connectionStatus}
          </span>
        </div>

        {/* Notification Bell Dropdown */}
        <div style={{ position: 'relative' }} ref={notificationRef}>
          <button
            type="button"
            className="icon-btn"
            onClick={() => setNotificationsOpen((prev) => !prev)}
            aria-label="View notifications"
            aria-expanded={notificationsOpen}
          >
            <Bell size={18} />
            {unreadCount > 0 && <span className="icon-badge">{unreadCount}</span>}
          </button>

          {notificationsOpen && (
            <div className="notification-menu" style={{ width: '380px' }}>
              <div className="notification-header">
                <div>
                  <h4 style={{ margin: 0 }}>System Notifications</h4>
                  <span style={{ fontSize: '0.7rem', color: 'var(--text-muted)' }}>
                    SignalR Live Stream
                  </span>
                </div>
                {unreadCount > 0 && (
                  <span
                    onClick={markAllRead}
                    style={{
                      fontSize: '0.6875rem',
                      color: 'var(--primary)',
                      fontWeight: 600,
                      cursor: 'pointer',
                    }}
                  >
                    Mark all read
                  </span>
                )}
              </div>

              <div className="notification-list" style={{ maxHeight: '360px', overflowY: 'auto' }}>
                {notifications.length === 0 ? (
                  <div
                    style={{
                      padding: '2rem 1rem',
                      textAlign: 'center',
                      color: 'var(--text-muted)',
                      fontSize: '0.8125rem',
                    }}
                  >
                    No notifications yet. Real-time updates will appear here automatically.
                  </div>
                ) : (
                  notifications.map((item) => (
                    <div
                      key={item.id}
                      className="notification-item"
                      onClick={() => handleNotificationClick(item)}
                      style={{
                        cursor: item.actionUrl ? 'pointer' : 'default',
                        backgroundColor: item.read ? 'transparent' : 'rgba(59, 130, 246, 0.05)',
                        borderLeft: item.read ? '3px solid transparent' : '3px solid var(--primary)',
                        paddingLeft: '0.75rem',
                        transition: 'background-color 0.15s ease',
                      }}
                    >
                      <div
                        style={{
                          color: getSeverityColor(item.severity),
                          marginTop: '2px',
                          flexShrink: 0,
                        }}
                      >
                        {getIconForCategory(item.category, item.severity)}
                      </div>
                      <div className="notification-content" style={{ flex: 1 }}>
                        <p style={{ margin: 0, fontSize: '0.8125rem' }}>
                          <strong style={{ color: 'var(--text-main)' }}>{item.title}</strong>
                          <span style={{ color: 'var(--text-secondary)' }}> — {item.description}</span>
                        </p>
                        <span style={{ fontSize: '0.6875rem', color: 'var(--text-muted)', marginTop: '4px', display: 'block' }}>
                          {formatRelativeTime(item.timestamp)}
                        </span>
                      </div>
                    </div>
                  ))
                )}
              </div>

              <div
                style={{
                  borderTop: '1px solid var(--border-color)',
                  paddingTop: '0.5rem',
                  textAlign: 'center',
                }}
              >
                <Link
                  to="/admin"
                  onClick={() => setNotificationsOpen(false)}
                  style={{
                    fontSize: '0.75rem',
                    color: 'var(--primary)',
                    textDecoration: 'none',
                    fontWeight: 500,
                  }}
                >
                  View Complete Audit Trail
                </Link>
              </div>
            </div>
          )}
        </div>
      </div>
    </header>
  );
};

export default TopHeader;
