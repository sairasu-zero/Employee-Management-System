import React, { useState } from 'react';
import { Outlet } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { TopHeader } from './TopHeader';

interface DashboardLayoutProps {
  children?: React.ReactNode;
}

export const DashboardLayout: React.FC<DashboardLayoutProps> = ({ children }) => {
  const [mobileSidebarOpen, setMobileSidebarOpen] = useState(false);

  return (
    <div className="dashboard-layout">
      {/* Sidebar (~250px fixed left, white background) */}
      <Sidebar
        isOpen={mobileSidebarOpen}
        onClose={() => setMobileSidebarOpen(false)}
      />

      {/* Main Container */}
      <div className="dashboard-main">
        {/* Top Header */}
        <TopHeader onToggleSidebar={() => setMobileSidebarOpen((prev) => !prev)} />

        {/* Scrollable Main Content Area */}
        <main className="content-area">
          {children ? children : <Outlet />}
        </main>
      </div>
    </div>
  );
};

export default DashboardLayout;
