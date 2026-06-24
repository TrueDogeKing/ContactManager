import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { isAxiosError } from 'axios';
import { getContact } from '../api/contacts';
import type { ContactResponse } from '../api/types';

// Public page showing a single contact's details.
export default function ContactDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const [contact, setContact] = useState<ContactResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  // Tracks which id the current contact/error belongs to; loading is derived from it,
  // so we never call setState synchronously inside the effect when id changes.
  const [loadedId, setLoadedId] = useState<string | undefined>(undefined);

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
  }, [id]);

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

      <h1>
        {contact.firstName} {contact.lastName}
      </h1>

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
    </main>
  );
}
