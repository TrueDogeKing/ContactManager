import { api } from './client';
import { clearAccessToken, setAccessToken } from './tokenStore';
import type { LoginRequest, LoginResponse } from './types';

// Logs in and stores the access token in memory. The refresh token is set as an HttpOnly cookie.
export async function login(credentials: LoginRequest): Promise<void> {
  const { data } = await api.post<LoginResponse>('/auth/login', credentials);
  setAccessToken(data.token);
}

// Logs out (revokes the refresh cookie) and clears the in-memory token regardless of the result.
export async function logout(): Promise<void> {
  try {
    await api.post('/auth/logout');
  } finally {
    clearAccessToken();
  }
}
