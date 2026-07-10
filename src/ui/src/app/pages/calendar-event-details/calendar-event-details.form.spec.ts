import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { describe, expect, it } from 'vitest';

import { CalendarEventFields } from 'src/app/shared/api/calendar-events/calendar-events-service';
import { EventTextField } from 'src/app/shared/api/settings/event-text-fields-service';
import {
  applyCalendarEventDetailsRules,
  createCalendarEventDetailsModel,
  eventTextFieldsToModel,
  patchCalendarEventDetailsModel,
  sameUpdateCalendarEventRequest,
  toCreateCalendarEventRequest,
  toUpdateCalendarEventRequest,
  type CalendarEventDetailsModel,
} from './calendar-event-details.form';

describe('calendar event details form mapping', () => {
  it('maps current event text fields to blank editable text values', () => {
    expect(eventTextFieldsToModel(eventTextFields())).toEqual([
      {
        fieldKey: 'text1',
        label: 'Title',
        type: 'ShortText',
        maxLength: 50,
        value: '',
      },
      {
        fieldKey: 'text2',
        label: 'Description',
        type: 'LongText',
        maxLength: 2500,
        value: '',
      },
    ]);
  });

  it('maps create model values to a trimmed create request', () => {
    expect(toCreateCalendarEventRequest(model())).toEqual({
      start: {
        localDateTime: '2999-01-01T10:00:00',
        timeZoneId: 'UTC',
      },
      texts: [
        {
          fieldKey: 'text1',
          value: 'English title',
        },
        {
          fieldKey: 'text2',
          value: 'English description',
        },
      ],
    });
  });

  it('patches edit model values from the stored event text snapshot', () => {
    const modelSignal = signal(createCalendarEventDetailsModel());

    patchCalendarEventDetailsModel(modelSignal, event());

    expect(modelSignal()).toEqual({
      start: {
        date: '2030-07-04',
        time: '09:30',
        timeZoneId: 'Europe/London',
      },
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
    });
  });

  it('maps update model values to start and trimmed text values', () => {
    expect(toUpdateCalendarEventRequest(model())).toEqual({
      start: {
        localDateTime: '2999-01-01T10:00:00',
        timeZoneId: 'UTC',
      },
      texts: [
        {
          fieldKey: 'text1',
          value: 'English title',
        },
        {
          fieldKey: 'text2',
          value: 'English description',
        },
      ],
    });
  });

  it('treats equal update requests as the same', () => {
    expect(sameUpdateCalendarEventRequest(updateRequest(), updateRequest())).toBe(true);
  });

  it('detects a changed scheduled start in update requests', () => {
    expect(
      sameUpdateCalendarEventRequest(updateRequest(), {
        ...updateRequest(),
        start: {
          ...updateRequest().start,
          localDateTime: '2999-01-02T10:00:00',
        },
      }),
    ).toBe(false);
  });

  it('detects a changed time zone in update requests', () => {
    expect(
      sameUpdateCalendarEventRequest(updateRequest(), {
        ...updateRequest(),
        start: {
          ...updateRequest().start,
          timeZoneId: 'America/Vancouver',
        },
      }),
    ).toBe(false);
  });

  it('detects a changed text value in update requests', () => {
    expect(
      sameUpdateCalendarEventRequest(updateRequest(), {
        ...updateRequest(),
        texts: [
          {
            fieldKey: 'text1',
            value: 'Updated English title',
          },
          {
            fieldKey: 'text2',
            value: 'English description',
          },
        ],
      }),
    ).toBe(false);
  });

  it('detects changed text order in update requests', () => {
    expect(
      sameUpdateCalendarEventRequest(updateRequest(), {
        ...updateRequest(),
        texts: [...updateRequest().texts].reverse(),
      }),
    ).toBe(false);
  });

  it('treats whitespace-only raw form changes as unchanged after update mapping', () => {
    const stored = toUpdateCalendarEventRequest(model());
    const edited = toUpdateCalendarEventRequest({
      ...model(),
      texts: [
        {
          ...model().texts[0],
          value: ' English title ',
        },
        {
          ...model().texts[1],
          value: '  English description  ',
        },
      ],
    });

    expect(sameUpdateCalendarEventRequest(edited, stored)).toBe(true);
  });

  it('enables start controls in edit mode when canUpdate returns true', () => {
    const detailsForm = TestBed.runInInjectionContext(() =>
      form(signal(model()), (path) =>
        applyCalendarEventDetailsRules(path, () => true, () => true),
      ),
    );

    expect(detailsForm.start().disabled()).toBe(false);
    expect(detailsForm.start.date().disabled()).toBe(false);
    expect(detailsForm.start.time().disabled()).toBe(false);
    expect(detailsForm.start.timeZoneId().disabled()).toBe(false);
  });

  it('disables start controls in edit mode when canUpdate returns false', () => {
    const detailsForm = TestBed.runInInjectionContext(() =>
      form(signal(model()), (path) =>
        applyCalendarEventDetailsRules(path, () => true, () => false),
      ),
    );

    expect(detailsForm.start().disabled()).toBe(true);
    expect(detailsForm.start.date().disabled()).toBe(true);
    expect(detailsForm.start.time().disabled()).toBe(true);
    expect(detailsForm.start.timeZoneId().disabled()).toBe(true);
  });

  function model(): CalendarEventDetailsModel {
    return {
      start: {
        date: '2999-01-01',
        time: '10:00',
        timeZoneId: 'UTC',
      },
      texts: [
        {
          fieldKey: 'text1',
          label: 'Title',
          type: 'ShortText',
          maxLength: 50,
          value: '  English title  ',
        },
        {
          fieldKey: 'text2',
          label: 'Description',
          type: 'LongText',
          maxLength: 2500,
          value: 'English description',
        },
      ],
    };
  }

  function event(): CalendarEventFields {
    return {
      calendarEventId: '6f9619ff8b864fb5bdfd4f5c2f2f16a1',
      start: {
        localDateTime: '2030-07-04T09:30:00',
        timeZoneId: 'Europe/London',
      },
      scheduledStartUtc: '2030-07-04T08:30:00+00:00',
      displayTitle: 'English title',
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
    };
  }

  function eventTextFields(): EventTextField[] {
    return event().texts.map(({ fieldKey, label, type, maxLength }) => ({
      fieldKey,
      label,
      type,
      maxLength,
    }));
  }

  function updateRequest() {
    return toUpdateCalendarEventRequest(model());
  }
});
