import { api } from "./client";
import type { CategoryResponse } from "./types";

// Fetches the category dictionary (with subcategories). Public endpoint.
export async function getCategories(): Promise<CategoryResponse[]> {
  const { data } = await api.get<CategoryResponse[]>("/categories");
  if (!Array.isArray(data)) {
    throw new Error("Unexpected response for the categories list (is the API reachable?)");
  }
  return data;
}
