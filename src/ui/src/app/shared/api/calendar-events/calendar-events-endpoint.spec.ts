import { describe, expect, it } from 'vitest';

import { publishingContentUrl } from './calendar-events-endpoint';

describe('calendar events endpoint helpers', () => {
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
});
