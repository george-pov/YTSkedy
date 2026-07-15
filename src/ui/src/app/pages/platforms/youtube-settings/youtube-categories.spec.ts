import { describe, expect, it } from 'vitest';

import { youtubeCategoryOptions, youtubeCategoryOptionsFor } from './youtube-categories';

describe('YouTube category options', () => {
  it('keeps YouTube Default first and reviewed provider values valid', () => {
    expect(youtubeCategoryOptions[0]).toEqual({ value: '', label: 'YouTube Default' });

    const providerOptions = youtubeCategoryOptions.slice(1);
    expect(providerOptions.every((option) => option.value.length > 0)).toBe(true);
    expect(providerOptions.every((option) => option.label.length > 0)).toBe(true);
    expect(new Set(providerOptions.map((option) => option.value)).size).toBe(
      providerOptions.length,
    );
    expect(providerOptions.map((option) => option.label)).toEqual(
      [...providerOptions.map((option) => option.label)].sort((left, right) =>
        left.localeCompare(right),
      ),
    );
  });

  it('returns the immutable catalog for default and known ids', () => {
    expect(youtubeCategoryOptionsFor('')).toBe(youtubeCategoryOptions);
    expect(youtubeCategoryOptionsFor('27')).toBe(youtubeCategoryOptions);
    expect(Object.isFrozen(youtubeCategoryOptions)).toBe(true);
    expect(youtubeCategoryOptions.every(Object.isFrozen)).toBe(true);
  });

  it('appends an unknown stored id without mutating the catalog', () => {
    const before = [...youtubeCategoryOptions];

    const options = youtubeCategoryOptionsFor(' 999 ');

    expect(options.at(-1)).toEqual({ value: '999', label: 'Category #999' });
    expect(youtubeCategoryOptions).toEqual(before);
    expect(youtubeCategoryOptions).toHaveLength(before.length);
  });
});
