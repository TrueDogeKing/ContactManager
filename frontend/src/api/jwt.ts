// Utility to decode JWT payload (no verification; we trust tokens from our API).
interface JWTPayload {
  email?: string;
  given_name?: string;
  family_name?: string;
  sub?: string;
  exp?: number;
  [key: string]: unknown;
}

export function decodeJWT(token: string): JWTPayload | null {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;
    const payload = JSON.parse(atob(parts[1]));
    return payload;
  } catch {
    return null;
  }
}

export function getUserNameFromToken(token: string | null): string | null {
  if (!token) return null;
  const payload = decodeJWT(token);
  const firstName = payload?.given_name || '';
  const lastName = payload?.family_name || '';
  const fullName = `${firstName} ${lastName}`.trim();
  return fullName || null;
}
