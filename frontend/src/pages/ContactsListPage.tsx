import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getContacts } from "../api/contacts";
import { useAuth } from "../auth/AuthContext";
import type { ContactResponse } from "../api/types";

// Public page listing all contacts.
export default function ContactsListPage() {
  const { isAuthenticated } = useAuth();
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
        if (active) setError("Failed to fetch contacts.");
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, []);

  if (loading) return <p>Loading contacts…</p>;
  if (error) return <p role="alert">{error}</p>;

  return (
    <main className="page">
      <div className="page-header">
        <h1>Contacts</h1>
        {isAuthenticated && (
          <Link to="/contacts/new" className="button-link">
            Add contact
          </Link>
        )}
      </div>

      {contacts.length === 0 ? (
        <p>No contacts available.</p>
      ) : (
        <table className="contacts-table">
          <thead>
            <tr>
              <th>First and Last Name</th>
              <th>Email</th>
              <th>Phone</th>
              <th>Category</th>
              <th>Subcategory</th>
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
                <td>
                  {contact.subcategoryName ?? contact.customSubcategory ?? "—"}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </main>
  );
}
