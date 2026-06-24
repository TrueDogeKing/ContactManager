import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { isAxiosError } from 'axios';
import { getContact } from '../api/contacts';
import type { ContactResponse } from '../api/types';

// Public page showing a single contact's details.
export default function ContactDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const [contact, setContact] = useState<ContactResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    let active = true;
    setLoading(true);
    getContact(id)
      .then((data) => {
        if (active) setContact(data);
      })
      .catch((err: unknown) => {
        if (!active) return;
        setError(
          isAxiosError(err) && err.response?.status === 404
            ? 'Nie znaleziono kontaktu.'
            : 'Nie udało się pobrać kontaktu.',
        );
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [id]);

  if (loading) return <p>Ładowanie kontaktu…</p>;
  if (error) {
    return (
      <main className="page">
        <p role="alert">{error}</p>
        <p>
          <Link to="/">← Powrót do listy</Link>
        </p>
      </main>
    );
  }
  if (!contact) return null;

  const subcategory = contact.subcategoryName ?? contact.customSubcategory ?? '—';

  return (
    <main className="page">
      <p>
        <Link to="/">← Powrót do listy</Link>
      </p>

      <h1>
        {contact.firstName} {contact.lastName}
      </h1>

      <dl className="details">
        <dt>E-mail</dt>
        <dd>{contact.email}</dd>

        <dt>Telefon</dt>
        <dd>{contact.phone}</dd>

        <dt>Data urodzenia</dt>
        <dd>{contact.birthDate}</dd>

        <dt>Kategoria</dt>
        <dd>{contact.categoryName}</dd>

        <dt>Podkategoria</dt>
        <dd>{subcategory}</dd>
      </dl>
    </main>
  );
}
