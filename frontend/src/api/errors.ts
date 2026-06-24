import { isAxiosError } from 'axios';

interface ProblemDetails {
  title?: string;
  detail?: string;
  // FluentValidation / ASP.NET ValidationProblem: field name -> messages.
  errors?: Record<string, string[]>;
}

// Extracts a human-readable message from an API error. Handles RFC7807 ProblemDetails:
// field-level validation errors, the `detail` message (business rules / conflicts), or the title.
export function getApiErrorMessage(error: unknown, fallback = 'Something went wrong.'): string {
  if (isAxiosError(error)) {
    const data = error.response?.data as ProblemDetails | undefined;
    if (data?.errors) {
      const messages = Object.values(data.errors).flat();
      if (messages.length > 0) {
        return messages.join(' ');
      }
    }
    if (data?.detail) return data.detail;
    if (data?.title) return data.title;
  }
  return fallback;
}
