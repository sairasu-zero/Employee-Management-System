/**
 * ERMS API Client Service
 * Configured to communicate with the ASP.NET Core Web API backend
 */

import { HealthResponse, DatabaseHealthResponse } from '../types';

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
 * Generic API request wrapper using the native Fetch API
 */
export async function apiFetch<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  const url = `${API_BASE_URL}${endpoint.startsWith('/') ? endpoint : `/${endpoint}`}`;

  const defaultHeaders: HeadersInit = {
    'Accept': 'application/json',
    'Content-Type': 'application/json',
  };

  const config: RequestInit = {
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
        (data && typeof data === 'object' && 'message' in data && typeof (data as { message: unknown }).message === 'string')
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

/**
 * Checks backend Web API health
 * GET /api/health
 */
export async function checkApiHealth(): Promise<HealthResponse> {
  return apiFetch<HealthResponse>('/api/health', {
    method: 'GET',
  });
}

/**
 * Checks PostgreSQL database connectivity via backend
 * GET /api/health/database
 */
export async function checkDatabaseHealth(): Promise<DatabaseHealthResponse> {
  return apiFetch<DatabaseHealthResponse>('/api/health/database', {
    method: 'GET',
  });
}

export { API_BASE_URL };
