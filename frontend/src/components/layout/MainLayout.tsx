import React from 'react';
import { Header } from '../common/Header';

interface MainLayoutProps {
  children: React.ReactNode;
}

export const MainLayout: React.FC<MainLayoutProps> = ({ children }) => {
  return (
    <div className="app-container">
      <Header />
      <main className="main-content">{children}</main>
      <footer className="erms-footer">
        <p>
          Employee Records Management System &copy; {new Date().getFullYear()} &mdash; Phase 1 Development Foundation
        </p>
      </footer>
    </div>
  );
};
