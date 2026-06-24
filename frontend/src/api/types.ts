// Shared API response/request shapes mirroring the backend DTOs.

export interface LoginRequest {
  email: string;
  password: string;
}

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

// Mirrors CreateContactRequestDto. birthDate is "YYYY-MM-DD".
export interface CreateContactRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  phone: string;
  birthDate: string;
  categoryId: number;
  subcategoryId: number | null;
  customSubcategory: string | null;
}

// Mirrors UpdateContactRequestDto. Password is not changed here. RowVersion is the
// optimistic-concurrency token read with the contact (a stale value yields a 409).
export interface UpdateContactRequest {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  birthDate: string;
  categoryId: number;
  subcategoryId: number | null;
  customSubcategory: string | null;
  rowVersion: number;
}

// Mirrors SubcategoryResponseDto.
export interface SubcategoryResponse {
  id: number;
  name: string;
}

// Mirrors CategoryResponseDto. allowsCustomSubcategory tells the client to offer a
// free-text field instead of the dictionary list.
export interface CategoryResponse {
  id: number;
  name: string;
  allowsCustomSubcategory: boolean;
  subcategories: SubcategoryResponse[];
}
