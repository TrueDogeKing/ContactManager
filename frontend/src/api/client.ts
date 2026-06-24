import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { clearAccessToken, getAccessToken, setAccessToken } from './tokenStore';
import type { LoginResponse } from './types';

// All requests go through the Vite dev proxy (/api -> backend), so cookies stay same-origin.
export const api = axios.create({
  baseURL: '/api',
  withCredentials: true,
});

// Attach the bearer token to every outgoing request.
api.interceptors.request.use((config) => {
  const token = getAccessToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Exchanges the HttpOnly refresh cookie for a fresh access token. Concurrent callers share one
// in-flight request via refreshPromise. A raw axios call is used to bypass the interceptors below.
let refreshPromise: Promise<string | null> | null = null;

export function refreshAccessToken(): Promise<string | null> {
  refreshPromise ??= axios
    .post<LoginResponse>('/api/auth/refresh', null, { withCredentials: true })
    .then((response) => {
      setAccessToken(response.data.token);
      return response.data.token;
    })
    .catch(() => {
      clearAccessToken();
      return null;
    })
    .finally(() => {
      refreshPromise = null;
    });

  return refreshPromise;
}

type RetriableConfig = InternalAxiosRequestConfig & { _retry?: boolean };

// On 401, try a silent refresh once and replay the original request.
api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as RetriableConfig | undefined;
    const isAuthCall =
      original?.url?.includes('/auth/login') || original?.url?.includes('/auth/refresh');

    if (error.response?.status === 401 && original && !original._retry && !isAuthCall) {
      original._retry = true;
      const token = await refreshAccessToken();
      if (token) {
        original.headers.Authorization = `Bearer ${token}`;
        return api(original);
      }
    }

    return Promise.reject(error);
  },
);
