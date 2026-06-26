import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { isAxiosError } from 'axios';
import { deleteContact, getContact } from '../api/contacts';
import { getApiErrorMessage } from '../api/errors';
import { useAuth } from '../auth/AuthContext';
import ConfirmDialog from '../components/ConfirmDialog';
import ChangePasswordForm from '../components/ChangePasswordForm';
import type { ContactResponse } from '../api/types';

// Public page showing a single contact's details.
export default function ContactDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const { isAuthenticated, userEmail } = useAuth();
  const navigate = useNavigate();
  const [contact, setContact] = useState<ContactResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  // Bumped to re-fetch the contact (e.g. after a password change refreshes its rowVersion).
  const [reloadKey, setReloadKey] = useState(0);
  // Tracks which id the current contact/error belongs to; loading is derived from it,
  // so we never call setState synchronously inside the effect when id changes.
  const [loadedId, setLoadedId] = useState<string | undefined>(undefined);
  const [confirmingDelete, setConfirmingDelete] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  async function handleDelete() {
    if (!id) return;
    setDeleteError(null);
    setDeleting(true);
    try {
      await deleteContact(id);
      navigate('/');
    } catch (err) {
      setDeleteError(getApiErrorMessage(err, 'Failed to delete the contact.'));
      setConfirmingDelete(false);
    } finally {
      setDeleting(false);
    }
  }

  useEffect(() => {
    if (!id) return;
    let active = true;
    getContact(id)
      .then((data) => {
        if (!active) return;
        setContact(data);
        setError(null);
      })
      .catch((err: unknown) => {
        if (!active) return;
        setError(
          isAxiosError(err) && err.response?.status === 404
            ? 'Contact not found.'
            : 'Failed to fetch contact.',
        );
      })
      .finally(() => {
        if (active) setLoadedId(id);
      });
    return () => {
      active = false;
    };
  }, [id, reloadKey]);

  const loading = loadedId !== id;

  if (loading) return <p>Loading contact…</p>;
  if (error) {
    return (
      <main className="page">
        <p role="alert">{error}</p>
        <p>
          <Link to="/">← Back to list</Link>
        </p>
      </main>
    );
  }
  if (!contact) return null;

  const subcategory = contact.subcategoryName ?? contact.customSubcategory ?? '—';

  return (
    <main className="page">
      <p>
        <Link to="/">← Back to list</Link>
      </p>

      <div className="page-header">
        <h1>
          {contact.firstName} {contact.lastName}
        </h1>
        {isAuthenticated && (
          <div className="actions">
            <Link to={`/contacts/${contact.id}/edit`} className="button-link">
              Edit
            </Link>
            <button
              type="button"
              className="button-link danger"
              onClick={() => setConfirmingDelete(true)}
            >
              Delete
            </button>
          </div>
        )}
      </div>

      {deleteError && <p role="alert">{deleteError}</p>}

      <dl className="details">
        <dt>Email</dt>
        <dd>{contact.email}</dd>

        <dt>Phone</dt>
        <dd>{contact.phone}</dd>

        <dt>Date of birth</dt>
        <dd>{contact.birthDate}</dd>

        <dt>Category</dt>
        <dd>{contact.categoryName}</dd>

        <dt>Subcategory</dt>
        <dd>{subcategory}</dd>
      </dl>

      {isAuthenticated &&
        userEmail &&
        userEmail.toLowerCase() === contact.email.toLowerCase() && (
          <ChangePasswordForm
            contactId={contact.id}
            rowVersion={contact.rowVersion}
            onChanged={() => setReloadKey((k) => k + 1)}
          />
        )}

      {confirmingDelete && (
        <ConfirmDialog
          title="Delete contact"
          message={`Delete ${contact.firstName} ${contact.lastName}? This cannot be undone.`}
          confirmLabel="Delete"
          busy={deleting}
          onConfirm={handleDelete}
          onCancel={() => setConfirmingDelete(false)}
        />
      )}
    </main>
  );
}
