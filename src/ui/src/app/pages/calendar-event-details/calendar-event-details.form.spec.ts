import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { describe, expect, it } from 'vitest';

import { CalendarEvent } from 'src/app/shared/api/calendar-events/calendar-events-service';
import { EventTextField } from 'src/app/shared/api/settings/event-text-fields-service';
import {
  applyCalendarEventDetailsRules,
  createCalendarEventDetailsModel,
  eventTextFieldsToModel,
  patchCalendarEventDetailsModel,
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

  function event(): CalendarEvent {
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
});
