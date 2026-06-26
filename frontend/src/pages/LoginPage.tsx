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
  const from =
    (location.state as { from?: { pathname?: string } } | null)?.from
      ?.pathname ?? "/";
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [attemptsLeft, setAttemptsLeft] = useState(MAX_ATTEMPTS);
  // Seconds until login is allowed again after hitting the rate limit (null = not blocked).
  const [retryAfter, setRetryAfter] = useState<number | null>(null);

  // Countdown while rate-limited; clears the block (and resets attempts) when it reaches zero.
  useEffect(() => {
    if (retryAfter === null) return;
    if (retryAfter <= 0) {
      setRetryAfter(null);
      setAttemptsLeft(MAX_ATTEMPTS);
      setError(null);
      return;
    }
    const timer = setTimeout(
      () => setRetryAfter((s) => (s === null ? null : s - 1)),
      1000,
    );
    return () => clearTimeout(timer);
  }, [retryAfter]);

  const blocked = retryAfter !== null;

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
        const seconds =
          Number.isFinite(header) && header > 0 ? header : DEFAULT_RETRY_SECONDS;
        setAttemptsLeft(0);
        setRetryAfter(seconds);
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

        {blocked && (
          <p role="alert">
            Too many attempts. Try again in {retryAfter}s.
          </p>
        )}

        {!blocked && attemptsLeft === 1 && (
          <p role="alert">Warning: 1 attempt left before a temporary lockout.</p>
        )}

        <button type="submit" disabled={submitting || blocked}>
          {blocked
            ? `Try again in ${retryAfter}s`
            : submitting
              ? "Logging in…"
              : "Log in"}
        </button>
      </form>
    </main>
  );
}
