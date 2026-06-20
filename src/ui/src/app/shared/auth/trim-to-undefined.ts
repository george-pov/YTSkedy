/**
 * Trims a string and collapses empty or whitespace-only values to `undefined`,
 * so blank token claims or identity fields do not leak through as empty strings.
 */
export function trimToUndefined(value: string | undefined): string | undefined {
  if (value === undefined) {
    return undefined;
  }

  const trimmed = value.trim();
  return trimmed.length === 0 ? undefined : trimmed;
}
