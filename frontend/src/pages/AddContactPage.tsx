import { useEffect, useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { getCategories } from '../api/categories';
import { createContact } from '../api/contacts';
import { getApiErrorMessage } from '../api/errors';
import CategorySubcategoryFields from '../components/CategorySubcategoryFields';
import type { CategoryResponse } from '../api/types';

interface FormState {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  phone: string;
  birthDate: string;
  categoryId: number;
  subcategoryId: number | null;
  customSubcategory: string;
}

const initialForm: FormState = {
  firstName: '',
  lastName: '',
  email: '',
  password: '',
  phone: '',
  birthDate: '',
  categoryId: 0,
  subcategoryId: null,
  customSubcategory: '',
};

export default function AddContactPage() {
  const navigate = useNavigate();
  const [categories, setCategories] = useState<CategoryResponse[]>([]);
  const [form, setForm] = useState<FormState>(initialForm);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    let active = true;
    getCategories()
      .then((data) => {
        if (active) setCategories(data);
      })
      .catch(() => {
        if (active) setError('Failed to load categories.');
      });
    return () => {
      active = false;
    };
  }, []);

  function setField<K extends keyof FormState>(key: K, value: FormState[K]) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  // Switching category invalidates any previously chosen subcategory / custom text.
  function handleCategoryChange(categoryId: number) {
    setForm((current) => ({
      ...current,
      categoryId,
      subcategoryId: null,
      customSubcategory: '',
    }));
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      const created = await createContact({
        firstName: form.firstName,
        lastName: form.lastName,
        email: form.email,
        password: form.password,
        phone: form.phone,
        birthDate: form.birthDate,
        categoryId: form.categoryId,
        subcategoryId: form.subcategoryId,
        customSubcategory: form.customSubcategory.trim() || null,
      });
      navigate(`/contacts/${created.id}`);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to create the contact.'));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <main className="page">
      <p>
        <Link to="/">← Back to list</Link>
      </p>

      <h1>Add contact</h1>

      <form className="form" onSubmit={handleSubmit}>
        <label>
          First name
          <input
            type="text"
            value={form.firstName}
            onChange={(e) => setField('firstName', e.target.value)}
            required
            maxLength={100}
          />
        </label>

        <label>
          Last name
          <input
            type="text"
            value={form.lastName}
            onChange={(e) => setField('lastName', e.target.value)}
            required
            maxLength={100}
          />
        </label>

        <label>
          Email
          <input
            type="email"
            value={form.email}
            onChange={(e) => setField('email', e.target.value)}
            required
            maxLength={256}
            autoComplete="off"
          />
        </label>

        <label>
          Password
          <input
            type="password"
            value={form.password}
            onChange={(e) => setField('password', e.target.value)}
            required
            minLength={8}
            // Mirrors the backend policy: min 8 chars, one uppercase letter, one special character.
            pattern="(?=.*[A-Z])(?=.*[^A-Za-z0-9]).{8,}"
            title="At least 8 characters, including one uppercase letter and one special character."
            autoComplete="new-password"
          />
          <small>At least 8 characters, including one uppercase letter and one special character.</small>
        </label>

        <label>
          Phone
          <input
            type="tel"
            value={form.phone}
            onChange={(e) => setField('phone', e.target.value)}
            required
            maxLength={32}
          />
        </label>

        <label>
          Date of birth
          <input
            type="date"
            value={form.birthDate}
            onChange={(e) => setField('birthDate', e.target.value)}
            required
          />
        </label>

        <CategorySubcategoryFields
          categories={categories}
          categoryId={form.categoryId}
          subcategoryId={form.subcategoryId}
          customSubcategory={form.customSubcategory}
          onCategoryChange={handleCategoryChange}
          onSubcategoryChange={(value) => setField('subcategoryId', value)}
          onCustomSubcategoryChange={(value) => setField('customSubcategory', value)}
        />

        {error && <p role="alert">{error}</p>}

        <button type="submit" disabled={submitting}>
          {submitting ? 'Saving…' : 'Save contact'}
        </button>
      </form>
    </main>
  );
}
