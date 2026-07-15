import { describe, expect, it } from 'vitest';

import {
  calendarEventDefaultStartUrl,
  calendarEventThumbnailUrl,
  publishingContentUrl,
  recoverPlatformPublicationUrl,
} from './calendar-events-endpoint';

describe('calendar events endpoint helpers', () => {
  it('builds the start suggestion URL', () => {
    expect(calendarEventDefaultStartUrl({ baseUrl: 'https://api.example.test/' })).toBe(
      'https://api.example.test/api/calendar-events/start-suggestion',
    );
  });
  it('builds the platform publishing-content URL with encoded ids', () => {
    const url = publishingContentUrl(
      { baseUrl: 'https://api.example.test/' },
      'event/id',
      'platform id',
    );

    expect(url).toBe(
      'https://api.example.test/api/calendar-events/event%2Fid/platforms/platform%20id/publishing-content',
    );
  });

  it('builds the calendar event thumbnail URL with encoded id', () => {
    const url = calendarEventThumbnailUrl(
      { baseUrl: 'https://api.example.test/' },
      'event/id',
    );

    expect(url).toBe('https://api.example.test/api/calendar-events/event%2Fid/thumbnail');
  });

  it('builds the recovery URL with encoded event and platform ids', () => {
    const url = recoverPlatformPublicationUrl(
      { baseUrl: 'https://api.example.test/' },
      'event/id',
      'platform id',
    );

    expect(url).toBe(
      'https://api.example.test/api/calendar-events/event%2Fid/platforms/platform%20id/publication/recover',
    );
  });
});
