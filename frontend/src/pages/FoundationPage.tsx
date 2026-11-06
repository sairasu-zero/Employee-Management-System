import React, { useEffect, useState, useCallback } from 'react';
import {
  Globe,
  Server,
  Database,
  RefreshCw,
  Cpu,
  Layers,
  Lock,
  Users,
  Building2,
  FileText,
} from 'lucide-react';
import { StatusCard } from '../components/common/StatusCard';
import { checkApiHealth, checkDatabaseHealth, API_BASE_URL } from '../services/api';
import { ConnectionStatus, HealthResponse, DatabaseHealthResponse } from '../types';

export const FoundationPage: React.FC = () => {
  const [frontendStatus] = useState<ConnectionStatus>('connected');
  const [backendStatus, setBackendStatus] = useState<ConnectionStatus>('pending');
  const [backendData, setBackendData] = useState<HealthResponse | null>(null);
  const [backendError, setBackendError] = useState<string | null>(null);

  const [dbStatus, setDbStatus] = useState<ConnectionStatus>('pending');
  const [dbData, setDbData] = useState<DatabaseHealthResponse | null>(null);
  const [dbError, setDbError] = useState<string | null>(null);

  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [lastChecked, setLastChecked] = useState<string>(new Date().toLocaleTimeString());

  const checkConnectivity = useCallback(async () => {
    setIsLoading(true);
    setBackendError(null);
    setDbError(null);

    // 1. Check Backend API Health
    try {
      const apiResult = await checkApiHealth();
      setBackendData(apiResult);
      setBackendStatus('connected');
    } catch (err) {
      setBackendStatus('error');
      setBackendError(err instanceof Error ? err.message : 'Backend unreachable');
    }

    // 2. Check PostgreSQL Database Health via Backend
    try {
      const dbResult = await checkDatabaseHealth();
      setDbData(dbResult);
      if (dbResult.connected) {
        setDbStatus('connected');
      } else {
        setDbStatus('error');
        setDbError(dbResult.message || 'Database reported not connected');
      }
    } catch (err) {
      setDbStatus('error');
      setDbError(
        err instanceof Error
          ? err.message
          : 'Could not communicate with PostgreSQL through backend.'
      );
    }

    setIsLoading(false);
    setLastChecked(new Date().toLocaleTimeString());
  }, []);

  useEffect(() => {
    checkConnectivity();
  }, [checkConnectivity]);

  return (
    <div>
      {/* Hero / System Foundation Banner */}
      <div className="info-banner">
        <div className="banner-header">
          <Cpu size={28} color="var(--primary)" />
          <div>
            <h2>System Foundation</h2>
            <p style={{ color: 'var(--text-muted)', fontSize: '0.875rem' }}>
              Phase 1 &bull; Technology stack configuration &amp; live connectivity verification
            </p>
          </div>
        </div>

        <p className="banner-description">
          The development foundation for the <strong>Employee Records Management System (ERMS)</strong> is
          established. This page verifies end-to-end communication between the React + TypeScript frontend,
          ASP.NET Core Web API backend, and the PostgreSQL database engine.
        </p>

        <div style={{ display: 'flex', gap: '1rem', alignItems: 'center', flexWrap: 'wrap' }}>
          <button
            onClick={checkConnectivity}
            disabled={isLoading}
            className="btn btn-primary"
          >
            <RefreshCw size={16} className={isLoading ? 'pulse-dot' : ''} />
            {isLoading ? 'Checking Connectivity...' : 'Refresh Status'}
          </button>
          <span style={{ fontSize: '0.8125rem', color: 'var(--text-muted)' }}>
            Last checked: {lastChecked}
          </span>
        </div>
      </div>

      {/* Primary Connectivity Status Grid */}
      <section>
        <div style={{ marginBottom: '1rem' }}>
          <h3 style={{ fontSize: '1.125rem', fontWeight: 600, color: 'var(--text-main)' }}>
            Core Infrastructure Connectivity
          </h3>
          <p style={{ fontSize: '0.8125rem', color: 'var(--text-muted)' }}>
            Status of all 3 foundation tiers in the ERMS architecture
          </p>
        </div>

        <div className="status-grid">
          {/* 1. Frontend Status */}
          <StatusCard
            title="Frontend Status"
            category="frontend"
            status={frontendStatus}
            statusLabel={frontendStatus === 'connected' ? 'Running' : 'Pending'}
            detail="React 18 + TypeScript + Vite running in Strict Mode."
            icon={<Globe size={22} />}
          />

          {/* 2. Backend Connection */}
          <StatusCard
            title="Backend Connection"
            category="backend"
            status={backendStatus}
            statusLabel={
              backendStatus === 'connected'
                ? 'Connected'
                : backendStatus === 'pending'
                ? 'Pending'
                : 'Error'
            }
            detail={
              backendStatus === 'connected'
                ? `Connected to ${backendData?.application || 'ERMS.API'} (${backendData?.environment || 'Development'})`
                : backendStatus === 'pending'
                ? 'Testing connection to ASP.NET Core Web API...'
                : `Error: ${backendError}`
            }
            icon={<Server size={22} />}
          />

          {/* 3. Database Connection */}
          <StatusCard
            title="Database Connection"
            category="database"
            status={dbStatus}
            statusLabel={
              dbStatus === 'connected'
                ? 'Connected'
                : dbStatus === 'pending'
                ? 'Pending'
                : 'Pending / Unreachable'
            }
            detail={
              dbStatus === 'connected'
                ? `Connected to ${dbData?.database || 'erms_db'} via ${dbData?.provider || 'Npgsql'}`
                : dbStatus === 'pending'
                ? 'Verifying PostgreSQL database connectivity...'
                : `Status: ${dbError || 'Database service check pending'}`
            }
            icon={<Database size={22} />}
          />
        </div>
      </section>

      {/* System Technical Specifications */}
      <div className="info-banner" style={{ marginTop: '1rem' }}>
        <div className="banner-header">
          <Layers size={22} color="var(--primary)" />
          <h3>Foundation Configuration &amp; Diagnostics</h3>
        </div>

        <table className="diagnostic-table">
          <thead>
            <tr>
              <th>Component</th>
              <th>Configured Value</th>
              <th>Role in ERMS</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td><strong>Frontend Client</strong></td>
              <td>React 18 &bull; TypeScript &bull; Vite (Port 5173)</td>
              <td>Client application, Strict Mode type checking</td>
            </tr>
            <tr>
              <td><strong>Backend Web API</strong></td>
              <td>ASP.NET Core Web API (.NET 8 LTS)</td>
              <td>RESTful API service, Controllers, Swagger, Dependency Injection</td>
            </tr>
            <tr>
              <td><strong>API Base URL</strong></td>
              <td><code>{API_BASE_URL}</code></td>
              <td>Environment-driven backend communication endpoint</td>
            </tr>
            <tr>
              <td><strong>Database Engine</strong></td>
              <td>PostgreSQL 18 &bull; Database: <code>erms_db</code></td>
              <td>Relational persistence layer via Npgsql EF Core provider</td>
            </tr>
            <tr>
              <td><strong>CORS Policy</strong></td>
              <td>Restricted to development origin <code>http://localhost:5173</code></td>
              <td>Secure cross-origin resource sharing policy</td>
            </tr>
            <tr>
              <td><strong>Health Check Endpoints</strong></td>
              <td>
                <code>GET /api/health</code><br />
                <code>GET /api/health/database</code>
              </td>
              <td>Automated readiness and connectivity diagnostics</td>
            </tr>
          </tbody>
        </table>
      </div>

      {/* Architecture Roadmap (Upcoming Phases) */}
      <div className="info-banner" style={{ marginTop: '1rem' }}>
        <div className="banner-header">
          <Layers size={22} color="var(--text-secondary)" />
          <h3>Upcoming Architectural Modules</h3>
        </div>
        <p style={{ color: 'var(--text-muted)', fontSize: '0.875rem' }}>
          Prepared entity architecture ready for implementation in subsequent phases:
        </p>

        <div className="roadmap-grid">
          <div className="roadmap-card">
            <span className="phase-tag">Phase 2</span>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <Lock size={16} color="var(--primary)" />
              <h4>Authentication &amp; MFA</h4>
            </div>
            <p>Users, Login Attempts, MFA Configurations, Recovery Codes, Refresh Tokens</p>
          </div>

          <div className="roadmap-card">
            <span className="phase-tag">Phase 3</span>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <Users size={16} color="var(--primary)" />
              <h4>RBAC &amp; Permissions</h4>
            </div>
            <p>Roles, Permissions, UserRoles, RolePermissions, Authorization policies</p>
          </div>

          <div className="roadmap-card">
            <span className="phase-tag">Phase 4</span>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <Building2 size={16} color="var(--primary)" />
              <h4>Departments &amp; Employees</h4>
            </div>
            <p>Employees, Departments, Employment Histories, Job Position lifecycle</p>
          </div>

          <div className="roadmap-card">
            <span className="phase-tag">Phase 5</span>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <FileText size={16} color="var(--primary)" />
              <h4>Documents &amp; Auditing</h4>
            </div>
            <p>Employee Documents, Audit Logs, Retention Policies, Compliance Reports</p>
          </div>
        </div>
      </div>
    </div>
  );
};
