/**
 * ERMS API Client Service
 * Configured to communicate with the ASP.NET Core Web API backend
 * Supports cookie-based credentials and strong typing
 */

import {
  HealthResponse,
  DatabaseHealthResponse,
  User,
  LoginCredentials,
  RegisterData,
  MfaVerificationData,
  ForgotPasswordData,
  ResetPasswordData,
  AuthResponse,
  EmployeeRecord,
  DepartmentRecord,
  AuditLogItem,
  AuditDashboardStats,
  PagedResult,
  LeaveRequestRecord,
  CreateLeaveData,
  EmployeeDocumentRecord,
  RoleRecord,
  PermissionRecord,
  CreateRoleData,
  UpdateRoleData,
} from '../types';

const API_BASE_URL: string =
  (import.meta.env.VITE_API_BASE_URL as string) || 'http://localhost:5000';

export class ApiError extends Error {
  status?: number;
  data?: unknown;

  constructor(message: string, status?: number, data?: unknown) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.data = data;
  }
}

/**
 * Generic API request wrapper using the native Fetch API with credentials: 'include'
 */
export async function apiFetch<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  const url = `${API_BASE_URL}${endpoint.startsWith('/') ? endpoint : `/${endpoint}`}`;

  const defaultHeaders: HeadersInit = {
    Accept: 'application/json',
    'Content-Type': 'application/json',
  };

  const config: RequestInit = {
    credentials: 'include', // Ensure cookies are sent and received
    ...options,
    headers: {
      ...defaultHeaders,
      ...options.headers,
    },
  };

  try {
    const response = await fetch(url, config);

    // Try parsing json body
    let data: unknown = null;
    const contentType = response.headers.get('content-type');
    if (contentType && contentType.includes('application/json')) {
      try {
        data = await response.json();
      } catch {
        // If JSON parsing fails, retain null
      }
    }

    if (!response.ok) {
      const errorMessage =
        data &&
        typeof data === 'object' &&
        'message' in data &&
        typeof (data as { message: unknown }).message === 'string'
          ? (data as { message: string }).message
          : `HTTP ${response.status}: ${response.statusText}`;

      throw new ApiError(errorMessage, response.status, data);
    }

    return (data as T) ?? ({} as T);
  } catch (error: unknown) {
    if (error instanceof ApiError) {
      throw error;
    }

    const message =
      error instanceof Error
        ? error.message
        : 'Failed to connect to the server. Please ensure the backend is running.';

    throw new ApiError(message);
  }
}

// ==========================================
// System Health Endpoints
// ==========================================

export async function checkApiHealth(): Promise<HealthResponse> {
  return apiFetch<HealthResponse>('/api/health', {
    method: 'GET',
  });
}

export async function checkDatabaseHealth(): Promise<DatabaseHealthResponse> {
  return apiFetch<DatabaseHealthResponse>('/api/health/database', {
    method: 'GET',
  });
}

// ==========================================
// Authentication Endpoints
// ==========================================

export async function apiLogin(credentials: LoginCredentials): Promise<AuthResponse> {
  return apiFetch<AuthResponse>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify(credentials),
  });
}

export async function apiRegister(data: RegisterData): Promise<AuthResponse> {
  return apiFetch<AuthResponse>('/api/auth/register', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export async function apiVerifyMfa(data: MfaVerificationData): Promise<AuthResponse> {
  return apiFetch<AuthResponse>('/api/auth/mfa/verify', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export async function apiVerifyRecoveryCode(
  email: string,
  recoveryCode: string,
  rememberMe = false
): Promise<AuthResponse> {
  return apiFetch<AuthResponse>('/api/auth/mfa/recovery', {
    method: 'POST',
    body: JSON.stringify({ email, recoveryCode, rememberMe }),
  });
}

export async function apiForgotPassword(data: ForgotPasswordData): Promise<{ success: boolean; message: string }> {
  return apiFetch<{ success: boolean; message: string }>('/api/auth/forgot-password', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export async function apiResetPassword(data: ResetPasswordData): Promise<{ success: boolean; message: string }> {
  return apiFetch<{ success: boolean; message: string }>('/api/auth/reset-password', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export async function apiGetCurrentUser(): Promise<User> {
  return apiFetch<User>('/api/auth/me', {
    method: 'GET',
  });
}

export async function apiLogout(): Promise<void> {
  return apiFetch<void>('/api/auth/logout', {
    method: 'POST',
  });
}

// ==========================================
// Workforce & Employee Endpoints
// ==========================================

export async function apiGetEmployees(params?: {
  search?: string;
  departmentId?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}): Promise<PagedResult<EmployeeRecord>> {
  const query = new URLSearchParams();
  if (params?.search) query.append('search', params.search);
  if (params?.departmentId) query.append('departmentId', params.departmentId);
  if (params?.status) query.append('status', params.status);
  if (params?.page) query.append('page', params.page.toString());
  if (params?.pageSize) query.append('pageSize', params.pageSize.toString());

  const queryString = query.toString();
  return apiFetch<PagedResult<EmployeeRecord>>(`/api/employees${queryString ? `?${queryString}` : ''}`, {
    method: 'GET',
  });
}

export async function apiCreateEmployee(data: Partial<EmployeeRecord>): Promise<EmployeeRecord> {
  return apiFetch<EmployeeRecord>('/api/employees', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export async function apiUpdateEmployee(id: string, data: Partial<EmployeeRecord>): Promise<EmployeeRecord> {
  return apiFetch<EmployeeRecord>(`/api/employees/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });
}

export async function apiDeleteEmployee(id: string): Promise<void> {
  return apiFetch<void>(`/api/employees/${id}`, {
    method: 'DELETE',
  });
}

// ==========================================
// Department Endpoints
// ==========================================

export async function apiGetDepartments(): Promise<DepartmentRecord[]> {
  return apiFetch<DepartmentRecord[]>('/api/departments', {
    method: 'GET',
  });
}

export async function apiCreateDepartment(data: Partial<DepartmentRecord>): Promise<DepartmentRecord> {
  return apiFetch<DepartmentRecord>('/api/departments', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

// ==========================================
// Audit & Compliance Endpoints
// ==========================================

export async function apiGetAuditLogs(params?: {
  category?: string;
  activityType?: string;
  page?: number;
  pageSize?: number;
}): Promise<PagedResult<AuditLogItem>> {
  const query = new URLSearchParams();
  if (params?.category) query.append('category', params.category);
  if (params?.activityType) query.append('activityType', params.activityType);
  if (params?.page) query.append('page', params.page.toString());
  if (params?.pageSize) query.append('pageSize', params.pageSize.toString());

  const queryString = query.toString();
  return apiFetch<PagedResult<AuditLogItem>>(`/api/audit${queryString ? `?${queryString}` : ''}`, {
    method: 'GET',
  });
}

export async function apiGetAuditStats(): Promise<AuditDashboardStats> {
  return apiFetch<AuditDashboardStats>('/api/audit/stats', {
    method: 'GET',
  });
}

// ==========================================
// Leave Management Endpoints
// ==========================================

export async function apiGetLeaveRequests(params?: {
  status?: string;
  departmentId?: string;
}): Promise<LeaveRequestRecord[]> {
  const query = new URLSearchParams();
  if (params?.status) query.append('status', params.status);
  if (params?.departmentId) query.append('departmentId', params.departmentId);

  const qs = query.toString();
  return apiFetch<LeaveRequestRecord[]>(`/api/leaves${qs ? `?${qs}` : ''}`, {
    method: 'GET',
  });
}

export async function apiGetPendingLeaves(): Promise<LeaveRequestRecord[]> {
  return apiFetch<LeaveRequestRecord[]>('/api/leaves/pending', {
    method: 'GET',
  });
}

export async function apiGetLeaveById(id: string): Promise<LeaveRequestRecord> {
  return apiFetch<LeaveRequestRecord>(`/api/leaves/${id}`, {
    method: 'GET',
  });
}

export async function apiSubmitLeave(data: CreateLeaveData): Promise<LeaveRequestRecord> {
  return apiFetch<LeaveRequestRecord>('/api/leaves', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export async function apiApproveLeave(id: string): Promise<LeaveRequestRecord> {
  return apiFetch<LeaveRequestRecord>(`/api/leaves/${id}/approve`, {
    method: 'PUT',
  });
}

export async function apiRejectLeave(id: string, reason: string): Promise<LeaveRequestRecord> {
  return apiFetch<LeaveRequestRecord>(`/api/leaves/${id}/reject`, {
    method: 'PUT',
    body: JSON.stringify({ reason }),
  });
}

export async function apiCancelLeave(id: string): Promise<LeaveRequestRecord> {
  return apiFetch<LeaveRequestRecord>(`/api/leaves/${id}/cancel`, {
    method: 'PUT',
  });
}

// ==========================================
// Employee Document Management Endpoints
// ==========================================

export async function apiGetEmployeeDocuments(employeeId: string): Promise<EmployeeDocumentRecord[]> {
  return apiFetch<EmployeeDocumentRecord[]>(`/api/employees/${employeeId}/documents`, {
    method: 'GET',
  });
}

export async function apiUploadDocument(
  employeeId: string,
  formData: FormData
): Promise<EmployeeDocumentRecord> {
  const url = `${API_BASE_URL}/api/employees/${employeeId}/documents`;
  const response = await fetch(url, {
    method: 'POST',
    credentials: 'include',
    body: formData, // Browser automatically sets Content-Type to multipart/form-data with boundary
  });

  if (!response.ok) {
    let err = 'Upload failed';
    try {
      const json = await response.json();
      if (json && json.message) err = json.message;
    } catch {
      err = `HTTP ${response.status}: ${response.statusText}`;
    }
    throw new ApiError(err, response.status);
  }

  return response.json();
}

export async function apiDownloadDocument(documentId: string, fileName: string): Promise<void> {
  const url = `${API_BASE_URL}/api/documents/${documentId}/download`;
  const response = await fetch(url, {
    method: 'GET',
    credentials: 'include',
  });

  if (!response.ok) {
    throw new ApiError(`Download failed with HTTP ${response.status}`, response.status);
  }

  const blob = await response.blob();
  const downloadUrl = window.URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = downloadUrl;
  a.download = fileName;
  document.body.appendChild(a);
  a.click();
  a.remove();
  window.URL.revokeObjectURL(downloadUrl);
}

export async function apiDeleteDocument(documentId: string): Promise<void> {
  return apiFetch<void>(`/api/documents/${documentId}`, {
    method: 'DELETE',
  });
}

// ==========================================
// RBAC Roles & Permissions Endpoints
// ==========================================

export async function apiGetRoles(): Promise<RoleRecord[]> {
  return apiFetch<RoleRecord[]>('/api/roles', {
    method: 'GET',
  });
}

export async function apiGetAllPermissions(): Promise<PermissionRecord[]> {
  return apiFetch<PermissionRecord[]>('/api/roles/permissions', {
    method: 'GET',
  });
}

export async function apiGetRoleById(id: string): Promise<RoleRecord> {
  return apiFetch<RoleRecord>(`/api/roles/${id}`, {
    method: 'GET',
  });
}

export async function apiCreateRole(data: CreateRoleData): Promise<RoleRecord> {
  return apiFetch<RoleRecord>('/api/roles', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export async function apiUpdateRole(id: string, data: UpdateRoleData): Promise<RoleRecord> {
  return apiFetch<RoleRecord>(`/api/roles/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });
}

export async function apiUpdateRolePermissions(id: string, permissionNames: string[]): Promise<RoleRecord> {
  return apiFetch<RoleRecord>(`/api/roles/${id}/permissions`, {
    method: 'PUT',
    body: JSON.stringify({ permissionNames }),
  });
}

// ==========================================
// Audit Export Endpoints
// ==========================================

export async function apiExportAuditCsv(params?: {
  category?: string;
  activityType?: string;
  userId?: string;
}): Promise<void> {
  const query = new URLSearchParams();
  if (params?.category) query.append('category', params.category);
  if (params?.activityType) query.append('activityType', params.activityType);
  if (params?.userId) query.append('userId', params.userId);

  const qs = query.toString();
  const url = `${API_BASE_URL}/api/audit/export/csv${qs ? `?${qs}` : ''}`;
  const response = await fetch(url, {
    method: 'GET',
    credentials: 'include',
  });

  if (!response.ok) {
    throw new ApiError(`CSV Export failed with HTTP ${response.status}`, response.status);
  }

  const blob = await response.blob();
  const downloadUrl = window.URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = downloadUrl;
  a.download = `audit_trail_${new Date().toISOString().slice(0, 10)}.csv`;
  document.body.appendChild(a);
  a.click();
  a.remove();
  window.URL.revokeObjectURL(downloadUrl);
}

export async function apiExportAuditPdf(params?: {
  category?: string;
  activityType?: string;
  userId?: string;
}): Promise<void> {
  const query = new URLSearchParams();
  if (params?.category) query.append('category', params.category);
  if (params?.activityType) query.append('activityType', params.activityType);
  if (params?.userId) query.append('userId', params.userId);

  const qs = query.toString();
  const url = `${API_BASE_URL}/api/audit/export/pdf${qs ? `?${qs}` : ''}`;
  const response = await fetch(url, {
    method: 'GET',
    credentials: 'include',
  });

  if (!response.ok) {
    throw new ApiError(`PDF Export failed with HTTP ${response.status}`, response.status);
  }

  const blob = await response.blob();
  const downloadUrl = window.URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = downloadUrl;
  a.download = `audit_report_${new Date().toISOString().slice(0, 10)}.pdf`;
  document.body.appendChild(a);
  a.click();
  a.remove();
  window.URL.revokeObjectURL(downloadUrl);
}

export { API_BASE_URL };
