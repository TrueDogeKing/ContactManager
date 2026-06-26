import { useEffect, useState, type FormEvent } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { isAxiosError } from "axios";
import { useAuth } from "../auth/AuthContext";

// Mirrors the server policy (RateLimiting:Auth:PermitLimit). Used only to warn the user
// before the lockout; the server's 429 + Retry-After response is the authoritative source.
const MAX_ATTEMPTS = 5;
const DEFAULT_RETRY_SECONDS = 30;

export default function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: { pathname?: string } } | null)?.from?.pathname ?? "/";
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [attemptsLeft, setAttemptsLeft] = useState(MAX_ATTEMPTS);
  // Timestamp (ms) until which login is blocked after hitting the rate limit (null = not blocked).
  const [lockedUntil, setLockedUntil] = useState<number | null>(null);
  const [now, setNow] = useState(() => Date.now());

  // While blocked, tick `now` to re-render the countdown; the interval clears the lock once it passes.
  useEffect(() => {
    if (lockedUntil === null) return;
    const timer = setInterval(() => {
      if (Date.now() >= lockedUntil) {
        setLockedUntil(null);
        setAttemptsLeft(MAX_ATTEMPTS);
        setError(null);
      } else {
        setNow(Date.now());
      }
    }, 500);
    return () => clearInterval(timer);
  }, [lockedUntil]);

  const remainingSeconds = lockedUntil ? Math.max(0, Math.ceil((lockedUntil - now) / 1000)) : 0;
  const blocked = lockedUntil !== null && remainingSeconds > 0;

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (blocked) return;
    setError(null);
    setSubmitting(true);
    try {
      await login({ email, password });
      setAttemptsLeft(MAX_ATTEMPTS);
      navigate(from, { replace: true });
    } catch (err) {
      if (isAxiosError(err) && err.response?.status === 429) {
        // Rate limit exceeded: start the retry countdown from the Retry-After header.
        const header = Number(err.response.headers["retry-after"]);
        const seconds = Number.isFinite(header) && header > 0 ? header : DEFAULT_RETRY_SECONDS;
        setAttemptsLeft(0);
        setNow(Date.now());
        setLockedUntil(Date.now() + seconds * 1000);
      } else {
        setAttemptsLeft((left) => Math.max(0, left - 1));
        setError("Incorrect email or password.");
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <main className="page">
      <h1>Log in</h1>

      <form className="form" onSubmit={handleSubmit}>
        <label>
          Email
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            autoComplete="username"
            disabled={blocked}
          />
        </label>

        <label>
          Password
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            autoComplete="current-password"
            disabled={blocked}
          />
        </label>

        {error && <p role="alert">{error}</p>}

        {blocked && <p role="alert">Too many attempts. Try again in {remainingSeconds}s.</p>}

        {!blocked && attemptsLeft === 1 && (
          <p role="alert">Warning: 1 attempt left before a temporary lockout.</p>
        )}

        <button type="submit" disabled={submitting || blocked}>
          {blocked ? `Try again in ${remainingSeconds}s` : submitting ? "Logging in…" : "Log in"}
        </button>
      </form>
    </main>
  );
}
