import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { ProtectedRoute } from './ProtectedRoute';
import { DashboardLayout } from '../components/layout/DashboardLayout';

// Auth Pages
import { LoginPage } from '../pages/auth/LoginPage';
import { RegisterPage } from '../pages/auth/RegisterPage';
import { MfaVerificationPage } from '../pages/auth/MfaVerificationPage';
import { RecoveryCodePage } from '../pages/auth/RecoveryCodePage';
import { ForgotPasswordPage } from '../pages/auth/ForgotPasswordPage';
import { ResetPasswordPage } from '../pages/auth/ResetPasswordPage';

// Dashboard & Management Pages
import { AdminDashboardPlaceholder } from '../pages/admin/AdminDashboardPlaceholder';
import { HrDashboardPlaceholder } from '../pages/hr/HrDashboardPlaceholder';
import { ManagerDashboardPage } from '../pages/manager/ManagerDashboardPage';
import { EmployeeDashboardPage } from '../pages/employee/EmployeeDashboardPage';
import { DepartmentListPlaceholder } from '../pages/department/DepartmentListPlaceholder';
import { EmployeeListPlaceholder } from '../pages/employee/EmployeeListPlaceholder';
import { FoundationPage } from '../pages/FoundationPage';
import { UnauthorizedPage } from '../pages/UnauthorizedPage';

// Helper component to route "/" to the user's role-appropriate homepage
const DefaultRoleRedirect: React.FC = () => {
  const { role, isAuthenticated } = useAuth();

  if (!isAuthenticated) {
    return <Navigate to="/auth/login" replace />;
  }

  switch (role) {
    case 'Admin':
      return <Navigate to="/admin" replace />;
    case 'HR':
      return <Navigate to="/hr" replace />;
    case 'Manager':
      return <Navigate to="/manager" replace />;
    case 'Employee':
    default:
      return <Navigate to="/employee" replace />;
  }
};

export const AppRoutes: React.FC = () => {
  return (
    <Routes>
      {/* ========================================================
          Public Authentication Routes
          ======================================================== */}
      <Route path="/auth/login" element={<LoginPage />} />
      <Route path="/auth/register" element={<RegisterPage />} />
      <Route path="/auth/mfa" element={<MfaVerificationPage />} />
      <Route path="/auth/recovery-code" element={<RecoveryCodePage />} />
      <Route path="/auth/forgot-password" element={<ForgotPasswordPage />} />
      <Route path="/auth/reset-password" element={<ResetPasswordPage />} />

      {/* Access Denied Unauthorized Route */}
      <Route path="/unauthorized" element={<UnauthorizedPage />} />

      {/* ========================================================
          Protected Application Shell Routes (Wrapped in DashboardLayout)
          ======================================================== */}
      <Route
        element={
          <ProtectedRoute>
            <DashboardLayout />
          </ProtectedRoute>
        }
      >
        {/* Root Redirect based on User Role */}
        <Route path="/" element={<DefaultRoleRedirect />} />

        {/* 1. Admin Role Dashboard */}
        <Route
          path="/admin"
          element={
            <ProtectedRoute allowedRoles={['Admin']}>
              <AdminDashboardPlaceholder />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/audit"
          element={
            <ProtectedRoute allowedRoles={['Admin']}>
              <AdminDashboardPlaceholder />
            </ProtectedRoute>
          }
        />

        {/* 2. HR Role Dashboard & Sub-modules */}
        <Route
          path="/hr"
          element={
            <ProtectedRoute allowedRoles={['Admin', 'HR']}>
              <HrDashboardPlaceholder />
            </ProtectedRoute>
          }
        />
        <Route
          path="/hr/leaves"
          element={
            <ProtectedRoute allowedRoles={['Admin', 'HR']}>
              <HrDashboardPlaceholder />
            </ProtectedRoute>
          }
        />
        <Route
          path="/hr/documents"
          element={
            <ProtectedRoute allowedRoles={['Admin', 'HR']}>
              <HrDashboardPlaceholder />
            </ProtectedRoute>
          }
        />

        {/* 3. Manager Role Workspace */}
        <Route
          path="/manager"
          element={
            <ProtectedRoute allowedRoles={['Admin', 'Manager']}>
              <ManagerDashboardPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/manager/approvals"
          element={
            <ProtectedRoute allowedRoles={['Admin', 'Manager']}>
              <ManagerDashboardPage />
            </ProtectedRoute>
          }
        />

        {/* 4. Employee Self-Service Workspace */}
        <Route
          path="/employee"
          element={
            <ProtectedRoute allowedRoles={['Admin', 'HR', 'Manager', 'Employee']}>
              <EmployeeDashboardPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/employee/leaves"
          element={
            <ProtectedRoute allowedRoles={['Admin', 'HR', 'Manager', 'Employee']}>
              <EmployeeDashboardPage />
            </ProtectedRoute>
          }
        />

        {/* Shared Workspaces */}
        <Route
          path="/employees"
          element={
            <ProtectedRoute allowedRoles={['Admin', 'HR', 'Manager', 'Employee']}>
              <EmployeeListPlaceholder />
            </ProtectedRoute>
          }
        />
        <Route
          path="/departments"
          element={
            <ProtectedRoute allowedRoles={['Admin', 'HR', 'Manager', 'Employee']}>
              <DepartmentListPlaceholder />
            </ProtectedRoute>
          }
        />

        {/* Phase 1 Diagnostic System Foundation View */}
        <Route
          path="/foundation"
          element={
            <ProtectedRoute>
              <FoundationPage />
            </ProtectedRoute>
          }
        />
      </Route>

      {/* Global Fallback: Route to Root */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
};

export default AppRoutes;
