import { type SelectOption } from 'src/app/shared/components/select/select';

/**
 * Reviewed assignable US YouTube video categories. The empty value delegates
 * category selection to YouTube and is never sent as a provider category id.
 */
export const youtubeCategoryOptions: readonly SelectOption[] = Object.freeze([
  Object.freeze({ value: '', label: 'YouTube Default' }),
  Object.freeze({ value: '2', label: 'Autos & Vehicles' }),
  Object.freeze({ value: '23', label: 'Comedy' }),
  Object.freeze({ value: '27', label: 'Education' }),
  Object.freeze({ value: '24', label: 'Entertainment' }),
  Object.freeze({ value: '1', label: 'Film & Animation' }),
  Object.freeze({ value: '20', label: 'Gaming' }),
  Object.freeze({ value: '26', label: 'Howto & Style' }),
  Object.freeze({ value: '10', label: 'Music' }),
  Object.freeze({ value: '25', label: 'News & Politics' }),
  Object.freeze({ value: '29', label: 'Nonprofits & Activism' }),
  Object.freeze({ value: '22', label: 'People & Blogs' }),
  Object.freeze({ value: '15', label: 'Pets & Animals' }),
  Object.freeze({ value: '28', label: 'Science & Technology' }),
  Object.freeze({ value: '17', label: 'Sports' }),
  Object.freeze({ value: '19', label: 'Travel & Events' }),
]);

/** Keeps an unknown stored provider id visible until the operator changes it. */
export function youtubeCategoryOptionsFor(categoryId: string): readonly SelectOption[] {
  const value = categoryId.trim();
  if (value.length === 0 || youtubeCategoryOptions.some((option) => option.value === value)) {
    return youtubeCategoryOptions;
  }

  return [...youtubeCategoryOptions, { value, label: `Category #${value}` }];
}
