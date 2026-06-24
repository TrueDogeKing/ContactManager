import { api } from './client';
import type { ContactResponse } from './types';

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
