import React, { useState, useRef, useEffect } from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import {
  LayoutDashboard,
  Users,
  Building2,
  ShieldCheck,
  ClipboardList,
  Activity,
  FileText,
  CalendarCheck,
  CheckSquare,
  UserCheck,
  LogOut,
  ChevronDown,
  User as UserIcon,
  Sparkles,
  ShieldAlert,
} from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';
import { UserRole } from '../../types';

interface SidebarProps {
  isOpen?: boolean;
  onClose?: () => void;
}

export const Sidebar: React.FC<SidebarProps> = ({ isOpen = false, onClose }) => {
  const { user, role, logout, switchDemoRole } = useAuth();
  const [profileMenuOpen, setProfileMenuOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();

  // Close profile dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setProfileMenuOpen(false);
      }
    };

    if (profileMenuOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [profileMenuOpen]);

  const handleLogout = async () => {
    await logout();
    navigate('/auth/login');
  };

  const handleRoleSwitch = (newRole: UserRole) => {
    switchDemoRole(newRole);
    setProfileMenuOpen(false);
    // Route to new role default dashboard
    if (newRole === 'Admin') navigate('/admin');
    else if (newRole === 'HR') navigate('/hr');
    else if (newRole === 'Manager') navigate('/manager');
    else navigate('/employee');
  };

  const effectiveRole: UserRole = role || 'Admin';

  // Define navigation sections based on current role
  const getNavSections = () => {
    switch (effectiveRole) {
      case 'HR':
        return [
          {
            title: 'Overview',
            items: [
              { label: 'HR Dashboard', path: '/hr', icon: LayoutDashboard },
            ],
          },
          {
            title: 'Workforce',
            items: [
              { label: 'Employee Records', path: '/employees', icon: Users, badge: '142' },
              { label: 'Departments', path: '/departments', icon: Building2 },
              { label: 'Leave Requests', path: '/hr/leaves', icon: CalendarCheck, badge: '5' },
              { label: 'HR Documents', path: '/hr/documents', icon: FileText },
            ],
          },
          {
            title: 'Diagnostics',
            items: [
              { label: 'System Health', path: '/foundation', icon: Activity },
            ],
          },
        ];

      case 'Manager':
        return [
          {
            title: 'Overview',
            items: [
              { label: 'Manager Hub', path: '/manager', icon: LayoutDashboard },
            ],
          },
          {
            title: 'Team Management',
            items: [
              { label: 'Team Directory', path: '/employees', icon: Users },
              { label: 'Leave Approvals', path: '/manager/approvals', icon: CheckSquare, badge: '3' },
              { label: 'Department View', path: '/departments', icon: Building2 },
            ],
          },
          {
            title: 'Diagnostics',
            items: [
              { label: 'System Health', path: '/foundation', icon: Activity },
            ],
          },
        ];

      case 'Employee':
        return [
          {
            title: 'My Workspace',
            items: [
              { label: 'My Profile & Hub', path: '/employee', icon: UserCheck },
              { label: 'Colleague Directory', path: '/employees', icon: Users },
              { label: 'Leave & Attendance', path: '/employee/leaves', icon: CalendarCheck },
            ],
          },
          {
            title: 'Company',
            items: [
              { label: 'Departments', path: '/departments', icon: Building2 },
              { label: 'System Health', path: '/foundation', icon: Activity },
            ],
          },
        ];

      case 'Admin':
      default:
        return [
          {
            title: 'Platform Control',
            items: [
              { label: 'Admin Dashboard', path: '/admin', icon: LayoutDashboard },
              { label: 'Employee Directory', path: '/employees', icon: Users, badge: '142' },
              { label: 'Departments', path: '/departments', icon: Building2, badge: '8' },
            ],
          },
          {
            title: 'Administration',
            items: [
              { label: 'HR Portal', path: '/hr', icon: ShieldCheck },
              { label: 'Security & Audit Logs', path: '/admin/audit', icon: ClipboardList },
            ],
          },
          {
            title: 'Infrastructure',
            items: [
              { label: 'System Health', path: '/foundation', icon: Activity },
            ],
          },
        ];
    }
  };

  const navSections = getNavSections();
  const userInitials = user?.avatarInitials || 'AM';
  const userName = user?.fullName || 'Alexander Mitchell';
  const userRole = user?.role || 'Admin';

  return (
    <aside className={`sidebar ${isOpen ? 'open' : ''}`}>
      {/* Brand Header */}
      <div className="sidebar-header">
        <div className="sidebar-brand">
          <div className="sidebar-logo-icon">
            <Sparkles size={20} />
          </div>
          <div className="sidebar-brand-text">
            <span className="sidebar-brand-title">ERMS</span>
            <span className="sidebar-brand-subtitle">Enterprise Portal</span>
          </div>
        </div>
      </div>

      {/* Navigation Sections */}
      <div className="sidebar-nav-container">
        {navSections.map((section, idx) => (
          <div key={idx} className="sidebar-section">
            <div className="sidebar-section-title">{section.title}</div>
            <ul className="sidebar-nav-list">
              {section.items.map((item) => {
                const Icon = item.icon;
                return (
                  <li key={item.path}>
                    <NavLink
                      to={item.path}
                      onClick={() => onClose && onClose()}
                      className={({ isActive }) =>
                        `sidebar-nav-item ${isActive ? 'active' : ''}`
                      }
                    >
                      <div className="sidebar-nav-item-left">
                        <Icon size={18} />
                        <span>{item.label}</span>
                      </div>
                      {item.badge && (
                        <span className="sidebar-nav-badge">{item.badge}</span>
                      )}
                    </NavLink>
                  </li>
                );
              })}
            </ul>
          </div>
        ))}
      </div>

      {/* User Profile Footer */}
      <div className="sidebar-footer" ref={dropdownRef}>
        {profileMenuOpen && (
          <div className="profile-dropdown-menu">
            <div style={{ padding: '0.5rem 0.75rem', borderBottom: '1px solid var(--border-color)', marginBottom: '0.35rem' }}>
              <div style={{ fontWeight: 600, fontSize: '0.84375rem', color: 'var(--text-main)' }}>
                {userName}
              </div>
              <div style={{ fontSize: '0.71875rem', color: 'var(--text-secondary)' }}>
                {user?.email || 'admin@erms.local'}
              </div>
            </div>

            {/* Quick Demo Role Switcher */}
            <div style={{ padding: '0.35rem 0.75rem', fontSize: '0.6875rem', fontWeight: 700, color: 'var(--text-muted)', textTransform: 'uppercase' }}>
              Switch Role View
            </div>

            {(['Admin', 'HR', 'Manager', 'Employee'] as UserRole[]).map((r) => (
              <button
                key={r}
                type="button"
                className="dropdown-item"
                style={{
                  fontWeight: effectiveRole === r ? 600 : 400,
                  color: effectiveRole === r ? 'var(--primary)' : 'var(--text-main)',
                }}
                onClick={() => handleRoleSwitch(r)}
              >
                <ShieldAlert size={15} />
                <span>{r} View</span>
                {effectiveRole === r && (
                  <span style={{ marginLeft: 'auto', fontSize: '0.6875rem', color: 'var(--primary)' }}>• Active</span>
                )}
              </button>
            ))}

            <div className="dropdown-divider" />

            <button
              type="button"
              className="dropdown-item"
              onClick={() => {
                setProfileMenuOpen(false);
                navigate('/employee');
              }}
            >
              <UserIcon size={16} />
              <span>User Profile</span>
            </button>

            <button
              type="button"
              className="dropdown-item danger"
              onClick={handleLogout}
            >
              <LogOut size={16} />
              <span>Sign Out</span>
            </button>
          </div>
        )}

        <div
          className="sidebar-user-card"
          onClick={() => setProfileMenuOpen((prev) => !prev)}
          role="button"
          tabIndex={0}
          aria-haspopup="true"
          aria-expanded={profileMenuOpen}
        >
          <div className="sidebar-user-info">
            <div className="avatar">{userInitials}</div>
            <div className="sidebar-user-text">
              <span className="sidebar-user-name">{userName}</span>
              <span className="sidebar-user-role">{userRole}</span>
            </div>
          </div>
          <ChevronDown
            size={16}
            style={{
              color: 'var(--text-muted)',
              transform: profileMenuOpen ? 'rotate(180deg)' : 'none',
              transition: 'transform 0.15s ease',
            }}
          />
        </div>
      </div>
    </aside>
  );
};

export default Sidebar;
