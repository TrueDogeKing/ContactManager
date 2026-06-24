import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";

export default function Header() {
  const { isAuthenticated, userName, logout } = useAuth();
  const navigate = useNavigate();

  async function handleLogout() {
    await logout();
    navigate("/");
  }

  return (
    <header className="app-header">
      <Link to="/" className="brand">
        Contact Book
      </Link>
      <nav>
        {isAuthenticated ? (
          <div className="auth-section">
            <span className="user-name">{userName}</span>
            <button type="button" onClick={handleLogout}>
              Log out
            </button>
          </div>
        ) : (
          <Link to="/login">Log in</Link>
        )}
      </nav>
    </header>
  );
}
