import { useState, type FormEvent } from "react";
import { isAxiosError } from "axios";
import { changeContactPassword } from "../api/contacts";
import { getApiErrorMessage } from "../api/errors";

interface Props {
  contactId: string;
  rowVersion: number;
  // Called after a successful change so the parent can refresh the contact (and its rowVersion).
  onChanged: () => void;
}

// Lets the signed-in owner change their own contact's password.
// Mirrors the backend complexity rules; the API enforces the same-email ownership check.
export default function ChangePasswordForm({ contactId, rowVersion, onChanged }: Props) {
  const [open, setOpen] = useState(false);
  const [newPassword, setNewPassword] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await changeContactPassword(contactId, { newPassword, rowVersion });
      setNewPassword("");
      setSuccess(true);
      setOpen(false);
      onChanged();
    } catch (err) {
      if (isAxiosError(err) && err.response?.status === 409) {
        setError("This contact was changed elsewhere. Reload the page and try again.");
      } else {
        setError(getApiErrorMessage(err, "Failed to change the password."));
      }
    } finally {
      setSubmitting(false);
    }
  }

  if (!open) {
    return (
      <div className="actions">
        {success && <p role="status">Password changed.</p>}
        <button
          type="button"
          className="button-link"
          onClick={() => {
            setOpen(true);
            setSuccess(false);
          }}
        >
          Change password
        </button>
      </div>
    );
  }

  return (
    <form className="form" onSubmit={handleSubmit}>
      <label>
        New password
        <input
          type="password"
          value={newPassword}
          onChange={(e) => setNewPassword(e.target.value)}
          required
          minLength={8}
          pattern="(?=.*[A-Z])(?=.*[^A-Za-z0-9]).{8,}"
          title="At least 8 characters, including one uppercase letter and one special character."
          autoComplete="new-password"
        />
        <small>
          At least 8 characters, including one uppercase letter and one special character.
        </small>
      </label>

      {error && <p role="alert">{error}</p>}

      <div className="actions">
        <button type="submit" disabled={submitting}>
          {submitting ? "Saving…" : "Save password"}
        </button>
        <button
          type="button"
          className="button-link"
          onClick={() => {
            setOpen(false);
            setError(null);
            setNewPassword("");
          }}
        >
          Cancel
        </button>
      </div>
    </form>
  );
}
