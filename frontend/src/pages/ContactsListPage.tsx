import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { getContacts } from '../api/contacts';
import type { ContactResponse } from '../api/types';

// Public page listing all contacts.
export default function ContactsListPage() {
  const [contacts, setContacts] = useState<ContactResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    getContacts()
      .then((data) => {
        if (active) setContacts(data);
      })
      .catch(() => {
        if (active) setError('Nie udało się pobrać kontaktów.');
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, []);

  if (loading) return <p>Ładowanie kontaktów…</p>;
  if (error) return <p role="alert">{error}</p>;

  return (
    <main className="page">
      <h1>Kontakty</h1>

      {contacts.length === 0 ? (
        <p>Brak kontaktów.</p>
      ) : (
        <table className="contacts-table">
          <thead>
            <tr>
              <th>Imię i nazwisko</th>
              <th>E-mail</th>
              <th>Telefon</th>
              <th>Kategoria</th>
              <th>Podkategoria</th>
            </tr>
          </thead>
          <tbody>
            {contacts.map((contact) => (
              <tr key={contact.id}>
                <td>
                  <Link to={`/contacts/${contact.id}`}>
                    {contact.firstName} {contact.lastName}
                  </Link>
                </td>
                <td>{contact.email}</td>
                <td>{contact.phone}</td>
                <td>{contact.categoryName}</td>
                <td>{contact.subcategoryName ?? contact.customSubcategory ?? '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </main>
  );
}
