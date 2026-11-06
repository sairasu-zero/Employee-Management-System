/**
 * ERMS Phase 1 - Type Definitions
 */

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

// Future Module Entity Stubs (Phase 2+)
export interface UserStub {
  id: string;
  username: string;
  email: string;
  role: string;
}

export interface EmployeeStub {
  id: string;
  employeeNumber: string;
  firstName: string;
  lastName: string;
  departmentId: string;
  position: string;
}
