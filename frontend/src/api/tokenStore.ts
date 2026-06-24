// In-memory store for the JWT access token. Kept out of localStorage on purpose
// (XSS-resistant); the token is re-acquired via the HttpOnly refresh cookie on boot and on 401.

type Listener = (token: string | null) => void;

let accessToken: string | null = null;
const listeners = new Set<Listener>();

export function getAccessToken(): string | null {
  return accessToken;
}

export function setAccessToken(token: string | null): void {
  accessToken = token;
  listeners.forEach((listener) => listener(token));
}

export function clearAccessToken(): void {
  setAccessToken(null);
}

// Subscribe to token changes (used by the auth state hook). Returns an unsubscribe function.
export function subscribeToken(listener: Listener): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}
