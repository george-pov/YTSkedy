import {
  type CalendarEventDetailsResponse,
  type CalendarEventDefaultStart,
  type CalendarEventPlatform,
  type CalendarEventThumbnail,
} from 'src/app/shared/api/calendar-events/calendar-events-service';
import { type EventTextFieldsResponse } from 'src/app/shared/api/settings/event-text-fields-service';

export function testCalendarEventPlatform(
  overrides: Partial<CalendarEventPlatform> = {},
): CalendarEventPlatform {
  return {
    platformId: 'platform-1',
    platformName: 'Main YouTube channel',
    platformType: 'YouTube',
    status: 'Published',
    externalResourceId: 'broadcast-123',
    thumbnailStatus: 'Applied',
    publishedUtc: '2030-07-04T08:45:00+00:00',
    publicationUpdatedUtc: '2030-07-04T08:45:00+00:00',
    platformDeletedUtc: null,
    canPublish: false,
    canDeletePublication: true,
    canPreviewPublishingContent: true,
    canRecoverPublication: false,
    ...overrides,
  };
}

export function testCalendarEventDefaultStart(
  overrides: Partial<CalendarEventDefaultStart> = {},
): CalendarEventDefaultStart {
  return {
    localDate: null,
    localTime: null,
    timeZoneId: null,
    ...overrides,
  };
}

export function testCalendarEventThumbnail(
  overrides: Partial<CalendarEventThumbnail> = {},
): CalendarEventThumbnail {
  return {
    fileName: 'stream.png',
    contentType: 'image/png',
    sizeBytes: 11,
    width: 1280,
    height: 720,
    updatedUtc: '2030-07-04T08:20:00+00:00',
    ...overrides,
  };
}

export function testCalendarEventDetails(
  overrides: Partial<CalendarEventDetailsResponse> = {},
): CalendarEventDetailsResponse {
  return {
    calendarEventId: 'event-1',
    start: { localDateTime: '2030-07-04T09:30:00', timeZoneId: 'Europe/London' },
    scheduledStartUtc: '2030-07-04T08:30:00+00:00',
    displayTitle: 'English title',
    canUpdate: true,
    canDelete: true,
    thumbnail: null,
    canUpdateThumbnail: true,
    texts: [
      {
        fieldKey: 'text1',
        label: 'Title',
        type: 'ShortText',
        maxLength: 50,
        value: 'English title',
      },
      {
        fieldKey: 'text2',
        label: 'Description',
        type: 'LongText',
        maxLength: 2500,
        value: 'English description',
      },
    ],
    platforms: [
      testCalendarEventPlatform({
        status: 'NotPublished',
        externalResourceId: null,
        thumbnailStatus: 'NotConfigured',
        publishedUtc: null,
        canPublish: true,
        canDeletePublication: false,
      }),
    ],
    ...overrides,
  };
}

export function testEventTextFieldsResponse(): EventTextFieldsResponse {
  return {
    fields: [
      { fieldKey: 'text1', label: 'Title', type: 'ShortText', maxLength: 50 },
      { fieldKey: 'text2', label: 'Description', type: 'LongText', maxLength: 2500 },
    ],
  };
}
