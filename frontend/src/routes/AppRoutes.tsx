import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { FoundationPage } from '../pages/FoundationPage';
import { LoginPage } from '../pages/auth/LoginPage';
import { AdminDashboardPlaceholder } from '../pages/admin/AdminDashboardPlaceholder';
import { HrDashboardPlaceholder } from '../pages/hr/HrDashboardPlaceholder';
import { DepartmentListPlaceholder } from '../pages/department/DepartmentListPlaceholder';
import { EmployeeListPlaceholder } from '../pages/employee/EmployeeListPlaceholder';

export const AppRoutes: React.FC = () => {
  return (
    <Routes>
      {/* Phase 1 Foundation Entry Point */}
      <Route path="/" element={<FoundationPage />} />

      {/* Future Module Route Definitions (Phase 2+) */}
      <Route path="/auth/login" element={<LoginPage />} />
      <Route path="/admin" element={<AdminDashboardPlaceholder />} />
      <Route path="/hr" element={<HrDashboardPlaceholder />} />
      <Route path="/departments" element={<DepartmentListPlaceholder />} />
      <Route path="/employees" element={<EmployeeListPlaceholder />} />

      {/* Fallback to Foundation */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
};
