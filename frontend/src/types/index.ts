/**
 * ERMS Type Definitions
 * Enterprise Records Management System
 */

import { LucideIcon } from 'lucide-react';
import React from 'react';

// ==========================================
// Phase 1 System Diagnostics Types
// ==========================================

export interface HealthResponse {
  status: string;
  application: string;
  timestamp?: string;
  environment?: string;
}

export interface DatabaseHealthResponse {
  status: string;
  application: string;
  database: string;
  provider: string;
  connected: boolean;
  message: string;
  timestamp?: string;
}

export type ConnectionStatus = 'pending' | 'connected' | 'error';

export interface SystemStatusState {
  frontend: {
    status: ConnectionStatus;
    label: string;
    detail: string;
  };
  backend: {
    status: ConnectionStatus;
    label: string;
    detail: string;
    latencyMs?: number;
    data?: HealthResponse;
  };
  database: {
    status: ConnectionStatus;
    label: string;
    detail: string;
    data?: DatabaseHealthResponse;
  };
}

// ==========================================
// Authentication & RBAC Types
// ==========================================

export type UserRole = 'Admin' | 'HR' | 'Manager' | 'Employee';

export interface User {
  id: string;
  employeeId: string;
  fullName: string;
  email: string;
  role: UserRole;
  department?: string;
  position?: string;
  avatarUrl?: string;
  avatarInitials?: string;
  permissions?: string[];
  isMfaEnabled?: boolean;
  createdAt?: string;
  lastLoginAt?: string;
}

export interface LoginCredentials {
  email: string;
  password: string;
  rememberMe?: boolean;
}

export interface RegisterData {
  fullName: string;
  employeeId: string;
  email: string;
  department: string;
  password: string;
  confirmPassword?: string;
}

export interface MfaVerificationData {
  email: string;
  code: string;
  isRecoveryCode?: boolean;
  rememberMe?: boolean;
}

export interface ForgotPasswordData {
  email: string;
}

export interface ResetPasswordData {
  token: string;
  email?: string;
  password: string;
  confirmPassword?: string;
}

export interface AuthResponse {
  success: boolean;
  message?: string;
  user?: User;
  token?: string;
  requiresMfa?: boolean;
  mfaType?: 'totp' | 'email';
}

export interface AuthContextType {
  user: User | null;
  role: UserRole | null;
  permissions: string[];
  isAuthenticated: boolean;
  isLoading: boolean;
  isMfaPending: boolean;
  pendingEmail: string | null;
  login: (credentials: LoginCredentials) => Promise<{ success: boolean; requiresMfa?: boolean; message?: string }>;
  verifyMfa: (code: string, isRecoveryCode?: boolean) => Promise<{ success: boolean; message?: string }>;
  logout: () => Promise<void>;
  register: (data: RegisterData) => Promise<{ success: boolean; message?: string }>;
  switchDemoRole: (role: UserRole) => void;
  checkAuthSession: () => Promise<void>;
}

// ==========================================
// UI Component Interfaces
// ==========================================

export interface MetricCardProps {
  title: string;
  value: string | number;
  change?: {
    value: string | number;
    isPositive?: boolean;
    period?: string;
  };
  icon: LucideIcon | React.ComponentType<{ size?: number; className?: string }>;
  iconColor?: string;
  iconBg?: string;
  trendLabel?: string;
  description?: string;
  className?: string;
  onClick?: () => void;
}

export type BadgeStatus =
  | 'success'
  | 'warning'
  | 'danger'
  | 'neutral'
  | 'info'
  | 'active'
  | 'inactive'
  | 'pending'
  | 'completed'
  | 'suspended';

export interface StatusBadgeProps {
  status: BadgeStatus;
  label?: string;
  pulse?: boolean;
  size?: 'sm' | 'md';
  className?: string;
}

export interface TableColumn<T> {
  key: keyof T | string;
  header: string;
  render?: (row: T, index: number) => React.ReactNode;
  sortable?: boolean;
  width?: string;
  align?: 'left' | 'center' | 'right';
}

export interface DataTableProps<T> {
  data: T[];
  columns: TableColumn<T>[];
  keyExtractor: (row: T) => string | number;
  searchPlaceholder?: string;
  searchKeys?: (keyof T)[];
  pageSize?: number;
  pageSizeOptions?: number[];
  onRowClick?: (row: T) => void;
  actions?: (row: T) => React.ReactNode;
  isLoading?: boolean;
  emptyMessage?: string;
  emptyIcon?: LucideIcon;
  title?: string;
  subtitle?: string;
  headerActions?: React.ReactNode;
}

export interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  subtitle?: string;
  children: React.ReactNode;
  footer?: React.ReactNode;
  size?: 'sm' | 'md' | 'lg' | 'xl';
  closeOnOverlayClick?: boolean;
}

export interface FormInputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label: string;
  name: string;
  error?: string;
  helperText?: string;
  icon?: LucideIcon;
  rightElement?: React.ReactNode;
  containerClassName?: string;
}

export interface ProgressBarProps {
  value: number; // 0 to 100
  label?: string;
  showPercentage?: boolean;
  color?: string;
  height?: number;
  animated?: boolean;
  status?: 'default' | 'success' | 'warning' | 'danger';
  className?: string;
}

// ==========================================
// Navigation & Breadcrumbs Types
// ==========================================

export interface NavItem {
  id: string;
  label: string;
  path: string;
  icon: LucideIcon;
  roles?: UserRole[];
  badge?: string | number;
}

export interface NavSection {
  title?: string;
  items: NavItem[];
}

export interface BreadcrumbItem {
  label: string;
  path?: string;
}

// ==========================================
// Workforce & Management Types
// ==========================================

export interface EmployeeRecord extends Record<string, unknown> {
  id: string;
  employeeNumber: string;
  firstName: string;
  middleName?: string;
  lastName: string;
  fullName: string;
  email: string;
  phone?: string;
  departmentId?: string;
  departmentName?: string;
  position: string;
  employmentType: string;
  hireDate: string;
  status: 'Active' | 'OnLeave' | 'Suspended' | 'Terminated' | 'active' | 'inactive' | 'pending';
  address?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface DepartmentRecord extends Record<string, unknown> {
  id: string;
  name: string;
  code: string;
  description?: string;
  managerEmployeeId?: string;
  managerName?: string;
  headcount: number;
  budget?: string;
  status?: 'active' | 'pending';
  createdAt: string;
}

export interface AuditLogItem extends Record<string, unknown> {
  id: string;
  userId?: string;
  userIdentity?: string;
  activityType: string;
  category: string;
  description: string;
  targetResource?: string;
  actionResult: string;
  ipAddress?: string;
  timestamp: string;
}

export interface AuditDashboardStats {
  totalAuditLogsToday: number;
  totalSecurityAlerts: number;
  failedLoginsToday: number;
  mfaComplianceRate: number;
  totalEmployees: number;
  totalDepartments: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// ==========================================
// Leave Management Types
// ==========================================

export interface LeaveRequestRecord extends Record<string, unknown> {
  id: string;
  employeeId: string;
  employeeNumber: string;
  employeeName: string;
  departmentId?: string;
  departmentName?: string;
  leaveType: string;
  startDate: string;
  endDate: string;
  days: number;
  reason: string;
  status: 'Pending' | 'Approved' | 'Rejected' | 'Cancelled';
  approvedBy?: string;
  approvedAt?: string;
  rejectionReason?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateLeaveData {
  leaveType: string;
  startDate: string;
  endDate: string;
  reason: string;
}

// ==========================================
// Employee Document Types
// ==========================================

export interface EmployeeDocumentRecord extends Record<string, unknown> {
  id: string;
  employeeId: string;
  documentName: string;
  documentType: string;
  originalFileName: string;
  contentType: string;
  fileSize: number;
  uploadedBy?: string;
  uploadedAt: string;
  updatedAt?: string;
}

// ==========================================
// Role & Permission RBAC Types
// ==========================================

export interface PermissionRecord {
  id: string;
  module: string;
  action: string;
  name: string;
  description?: string;
}

export interface RoleRecord {
  id: string;
  name: string;
  description?: string;
  isSystemRole: boolean;
  createdAt: string;
  permissions: PermissionRecord[];
}

export interface CreateRoleData {
  name: string;
  description?: string;
  permissionNames?: string[];
}

export interface UpdateRoleData {
  name: string;
  description?: string;
}

// ==========================================
// SignalR Real-Time Notification Types
// ==========================================

export interface NotificationMessageItem {
  id: string;
  title: string;
  description: string;
  category: string;
  severity: 'info' | 'warning' | 'success' | 'danger';
  timestamp: string;
  actionUrl?: string;
}
