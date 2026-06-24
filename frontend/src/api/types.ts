// Shared API response/request shapes mirroring the backend DTOs.

export interface LoginResponse {
  token: string;
  expiresAtUtc: string;
  email: string;
}
