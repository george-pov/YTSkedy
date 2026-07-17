import { describe, expect, it } from 'vitest';

import {
  YOUTUBE_AUDIO_LANGUAGE_OPTIONS,
  YOUTUBE_METADATA_LANGUAGE_OPTIONS,
  youtubeAudioLanguageOptionsFor,
  youtubeMetadataLanguageOptionsFor,
} from './youtube-languages';

describe('YouTube language options', () => {
  it('keeps separate reviewed catalogs with one stream-only value', () => {
    const audioOptions = YOUTUBE_AUDIO_LANGUAGE_OPTIONS.slice(1);
    const metadataOptions = YOUTUBE_METADATA_LANGUAGE_OPTIONS.slice(1);
    const metadataByValue = new Map(metadataOptions.map((option) => [option.value, option.label]));

    expect(YOUTUBE_AUDIO_LANGUAGE_OPTIONS[0]).toEqual({
      value: '',
      label: 'YouTube Default',
    });
    expect(YOUTUBE_METADATA_LANGUAGE_OPTIONS[0]).toEqual({
      value: '',
      label: 'YouTube Default',
    });
    expect(audioOptions).toHaveLength(239);
    expect(metadataOptions).toHaveLength(238);
    expect(audioOptions.filter((option) => !metadataByValue.has(option.value))).toEqual([
      { value: 'zxx', label: 'Not applicable' },
    ]);
    expect(
      metadataOptions.filter(
        (option) => !audioOptions.some((audio) => audio.value === option.value),
      ),
    ).toEqual([]);
    expect(
      audioOptions
        .filter((option) => option.value !== 'zxx')
        .every((option) => metadataByValue.get(option.value) === option.label),
    ).toBe(true);
  });

  it.each([
    ['audio', YOUTUBE_AUDIO_LANGUAGE_OPTIONS],
    ['metadata', YOUTUBE_METADATA_LANGUAGE_OPTIONS],
  ])('keeps %s values unique, sorted, and frozen', (_name, options) => {
    const providerOptions = options.slice(1);

    expect(providerOptions.every((option) => option.value.length > 0)).toBe(true);
    expect(new Set(providerOptions.map((option) => option.value)).size).toBe(
      providerOptions.length,
    );
    expect(providerOptions.map((option) => option.label)).toEqual(
      [...providerOptions.map((option) => option.label)].sort((left, right) =>
        left.localeCompare(right, 'en'),
      ),
    );
    expect(Object.isFrozen(options)).toBe(true);
    expect(options.every(Object.isFrozen)).toBe(true);
    expect(options).toContainEqual({
      value: 'en-US',
      label: 'English (United States)',
    });
  });

  it('exposes zxx only through the stream-language catalog', () => {
    expect(YOUTUBE_AUDIO_LANGUAGE_OPTIONS).toContainEqual({
      value: 'zxx',
      label: 'Not applicable',
    });
    expect(YOUTUBE_METADATA_LANGUAGE_OPTIONS.some((option) => option.value === 'zxx')).toBe(false);
  });

  it('returns the frozen catalogs for blank and known saved values', () => {
    expect(youtubeAudioLanguageOptionsFor('')).toBe(YOUTUBE_AUDIO_LANGUAGE_OPTIONS);
    expect(youtubeAudioLanguageOptionsFor(' en-US ')).toBe(YOUTUBE_AUDIO_LANGUAGE_OPTIONS);
    expect(youtubeMetadataLanguageOptionsFor('ru')).toBe(YOUTUBE_METADATA_LANGUAGE_OPTIONS);
  });

  it('appends trimmed unknown saved values without mutating either catalog', () => {
    const audioBefore = [...YOUTUBE_AUDIO_LANGUAGE_OPTIONS];
    const metadataBefore = [...YOUTUBE_METADATA_LANGUAGE_OPTIONS];

    const audioOptions = youtubeAudioLanguageOptionsFor(' x-private ');
    const metadataOptions = youtubeMetadataLanguageOptionsFor(' legacy-code ');

    expect(audioOptions.at(-1)).toEqual({
      value: 'x-private',
      label: 'Language code: x-private',
    });
    expect(metadataOptions.at(-1)).toEqual({
      value: 'legacy-code',
      label: 'Language code: legacy-code',
    });
    expect(YOUTUBE_AUDIO_LANGUAGE_OPTIONS).toEqual(audioBefore);
    expect(YOUTUBE_METADATA_LANGUAGE_OPTIONS).toEqual(metadataBefore);
  });
});
