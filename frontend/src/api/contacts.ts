import { api } from './client';
import type {
  ChangeContactPasswordRequest,
  ContactResponse,
  CreateContactRequest,
  UpdateContactRequest,
} from './types';

// Public read endpoints for contacts.
export async function getContacts(): Promise<ContactResponse[]> {
  const { data } = await api.get<ContactResponse[]>('/contacts');
  // Guards against a misconfigured proxy returning HTML instead of the JSON array.
  if (!Array.isArray(data)) {
    throw new Error('Unexpected response for the contacts list (is the API reachable?).');
  }
  return data;
}

export function getContact(id: string): Promise<ContactResponse> {
  return api.get<ContactResponse>(`/contacts/${id}`).then((response) => response.data);
}

// Creates a contact. Requires authentication. Returns the created contact.
export async function createContact(request: CreateContactRequest): Promise<ContactResponse> {
  const { data } = await api.post<ContactResponse>('/contacts', request);
  return data;
}

// Updates a contact (optimistic concurrency via rowVersion). Requires authentication.
// A stale rowVersion results in a 409 Conflict. Returns nothing (204 No Content).
export async function updateContact(id: string, request: UpdateContactRequest): Promise<void> {
  await api.put(`/contacts/${id}`, request);
}

// Deletes a contact. Requires authentication. Returns nothing (204 No Content).
export async function deleteContact(id: string): Promise<void> {
  await api.delete(`/contacts/${id}`);
}

// Changes a contact's password. Only the signed-in owner (matching email) is allowed (else 403).
// Optimistic concurrency via rowVersion (a stale value yields a 409). Returns nothing (204).
export async function changeContactPassword(
  id: string,
  request: ChangeContactPasswordRequest,
): Promise<void> {
  await api.put(`/contacts/${id}/password`, request);
}
