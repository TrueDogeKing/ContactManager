import type { CategoryResponse } from '../api/types';

interface Props {
  categories: CategoryResponse[];
  categoryId: number;
  subcategoryId: number | null;
  customSubcategory: string;
  onCategoryChange: (categoryId: number) => void;
  onSubcategoryChange: (subcategoryId: number | null) => void;
  onCustomSubcategoryChange: (value: string) => void;
}

// Data-driven category/subcategory selector. Behaviour follows the API category flags:
// - category with dictionary subcategories (e.g. Służbowy) -> required subcategory dropdown
// - category with allowsCustomSubcategory (e.g. Inny)      -> optional free-text field
// - any other category (e.g. Prywatny)                     -> no subcategory field
export default function CategorySubcategoryFields({
  categories,
  categoryId,
  subcategoryId,
  customSubcategory,
  onCategoryChange,
  onSubcategoryChange,
  onCustomSubcategoryChange,
}: Props) {
  const selected = categories.find((c) => c.id === categoryId);
  const hasDictionary = (selected?.subcategories.length ?? 0) > 0;
  const allowsCustom = selected?.allowsCustomSubcategory ?? false;

  return (
    <>
      <label>
        Category
        <select
          value={categoryId}
          onChange={(e) => onCategoryChange(Number(e.target.value))}
          required
        >
          <option value={0} disabled>
            — Select a category —
          </option>
          {categories.map((category) => (
            <option key={category.id} value={category.id}>
              {category.name}
            </option>
          ))}
        </select>
      </label>

      {hasDictionary && (
        <label>
          Subcategory
          <select
            value={subcategoryId ?? 0}
            onChange={(e) => onSubcategoryChange(Number(e.target.value) || null)}
            required
          >
            <option value={0} disabled>
              — Select a subcategory —
            </option>
            {selected!.subcategories.map((subcategory) => (
              <option key={subcategory.id} value={subcategory.id}>
                {subcategory.name}
              </option>
            ))}
          </select>
        </label>
      )}

      {!hasDictionary && allowsCustom && (
        <label>
          Subcategory (optional)
          <input
            type="text"
            value={customSubcategory}
            onChange={(e) => onCustomSubcategoryChange(e.target.value)}
            maxLength={100}
          />
        </label>
      )}
    </>
  );
}
