import { useCallback, useEffect, useState, type FormEvent } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { isAxiosError } from "axios";
import { getContact, updateContact } from "../api/contacts";
import { getCategories } from "../api/categories";
import { getApiErrorMessage } from "../api/errors";
import CategorySubcategoryFields from "../components/CategorySubcategoryFields";
import type { CategoryResponse, ContactResponse } from "../api/types";

interface FormState {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  birthDate: string;
  categoryId: number;
  subcategoryId: number | null;
  customSubcategory: string;
  rowVersion: number;
}

function toFormState(contact: ContactResponse): FormState {
  return {
    firstName: contact.firstName,
    lastName: contact.lastName,
    email: contact.email,
    phone: contact.phone,
    birthDate: contact.birthDate,
    categoryId: contact.categoryId,
    subcategoryId: contact.subcategoryId,
    customSubcategory: contact.customSubcategory ?? "",
    rowVersion: contact.rowVersion,
  };
}

export default function EditContactPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [categories, setCategories] = useState<CategoryResponse[]>([]);
  const [form, setForm] = useState<FormState | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  // True when the saved RowVersion was stale (someone else changed the contact first).
  const [conflict, setConflict] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  // Loads the contact (and categories) and resets the form to the latest server state.
  const load = useCallback(async () => {
    if (!id) return;
    setLoadError(null);
    setError(null);
    setConflict(false);
    try {
      const [contact, cats] = await Promise.all([getContact(id), getCategories()]);
      setCategories(cats);
      setForm(toFormState(contact));
    } catch (err) {
      setLoadError(
        isAxiosError(err) && err.response?.status === 404
          ? "Contact not found."
          : "Failed to load the contact.",
      );
    }
  }, [id]);

  useEffect(() => {
    // Data loader: setState runs after an await (not synchronously in the effect body).
    // eslint-disable-next-line react-hooks/set-state-in-effect
    load();
  }, [load]);

  function setField<K extends keyof FormState>(key: K, value: FormState[K]) {
    setForm((current) => (current ? { ...current, [key]: value } : current));
  }

  // Switching category invalidates any previously chosen subcategory / custom text.
  function handleCategoryChange(categoryId: number) {
    setForm((current) =>
      current ? { ...current, categoryId, subcategoryId: null, customSubcategory: "" } : current,
    );
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!id || !form) return;
    setError(null);
    setConflict(false);
    setSubmitting(true);
    try {
      await updateContact(id, {
        firstName: form.firstName,
        lastName: form.lastName,
        email: form.email,
        phone: form.phone,
        birthDate: form.birthDate,
        categoryId: form.categoryId,
        subcategoryId: form.subcategoryId,
        customSubcategory: form.customSubcategory.trim() || null,
        rowVersion: form.rowVersion,
      });
      navigate(`/contacts/${id}`);
    } catch (err) {
      if (isAxiosError(err) && err.response?.status === 409) {
        setConflict(true);
      } else {
        setError(getApiErrorMessage(err, "Failed to update the contact."));
      }
    } finally {
      setSubmitting(false);
    }
  }

  if (loadError) {
    return (
      <main className="page">
        <p role="alert">{loadError}</p>
        <p>
          <Link to="/">← Back to list</Link>
        </p>
      </main>
    );
  }

  if (!form) return <p>Loading contact…</p>;

  return (
    <main className="page">
      <p>
        <Link to={`/contacts/${id}`}>← Back to contact</Link>
      </p>

      <h1>Edit contact</h1>

      <form className="form" onSubmit={handleSubmit}>
        <label>
          First name
          <input
            type="text"
            value={form.firstName}
            onChange={(e) => setField("firstName", e.target.value)}
            required
            maxLength={100}
          />
        </label>

        <label>
          Last name
          <input
            type="text"
            value={form.lastName}
            onChange={(e) => setField("lastName", e.target.value)}
            required
            maxLength={100}
          />
        </label>

        <label>
          Email
          <input
            type="email"
            value={form.email}
            onChange={(e) => setField("email", e.target.value)}
            required
            maxLength={256}
          />
        </label>

        <label>
          Phone
          <input
            type="tel"
            value={form.phone}
            onChange={(e) => setField("phone", e.target.value)}
            required
            maxLength={32}
          />
        </label>

        <label>
          Date of birth
          <input
            type="date"
            value={form.birthDate}
            onChange={(e) => setField("birthDate", e.target.value)}
            required
          />
        </label>

        <CategorySubcategoryFields
          categories={categories}
          categoryId={form.categoryId}
          subcategoryId={form.subcategoryId}
          customSubcategory={form.customSubcategory}
          onCategoryChange={handleCategoryChange}
          onSubcategoryChange={(value) => setField("subcategoryId", value)}
          onCustomSubcategoryChange={(value) => setField("customSubcategory", value)}
        />

        {conflict && (
          <div role="alert" className="conflict">
            <p>
              This contact was changed by someone else since you opened it. Reload to get the latest
              version, then re-apply your changes.
            </p>
            <button type="button" onClick={load}>
              Reload latest
            </button>
          </div>
        )}

        {error && <p role="alert">{error}</p>}

        <button type="submit" disabled={submitting}>
          {submitting ? "Saving…" : "Save changes"}
        </button>
      </form>
    </main>
  );
}
