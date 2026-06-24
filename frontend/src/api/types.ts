// Shared API response/request shapes mirroring the backend DTOs.

export interface LoginResponse {
  token: string;
  expiresAtUtc: string;
  email: string;
}

// Mirrors ContactResponseDto. Dates are ISO strings; birthDate is "YYYY-MM-DD".
export interface ContactResponse {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  birthDate: string;
  categoryId: number;
  categoryName: string;
  subcategoryId: number | null;
  subcategoryName: string | null;
  customSubcategory: string | null;
  createdAt: string;
  updatedAt: string | null;
  rowVersion: number;
}
