import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { type CalendarEventDetailsResponse } from 'src/app/shared/api/calendar-events/calendar-events-service';
import { CalendarEventDraftState } from './calendar-event-draft.state';
import { testCalendarEventDetails } from './testing/calendar-event-details.fixture';

describe('CalendarEventDraftState', () => {
  it('applies full and partial suggestions only to the untouched create start', () => {
    const full = createState(false);
    full.applyDefaultStart({
      localDate: '2030-07-07',
      localTime: '10:30',
      timeZoneId: 'UTC',
    });
    expect(full.model().start).toEqual({ date: '2030-07-07', time: '10:30', timeZoneId: 'UTC' });

    const partial = createState(false);
    partial.applyDefaultStart({ localDate: null, localTime: '09:00', timeZoneId: null });
    expect(partial.model().start.date).toBe('');
    expect(partial.model().start.time).toBe('09:00');
  });

  it('rejects suggestions in edit mode or after operator start input', () => {
    const edit = createState(true);
    edit.applyDefaultStart({ localDate: '2030-07-07', localTime: '10:30', timeZoneId: 'UTC' });
    expect(edit.model().start.date).toBe('');

    const create = createState(false);
    create.model.update((model) => ({
      ...model,
      start: { ...model.start, date: '2030-08-01' },
    }));
    create.applyDefaultStart({ localDate: '2030-07-07', localTime: '10:30', timeZoneId: 'UTC' });
    expect(create.model().start).toEqual({
      date: '2030-08-01',
      time: '',
      timeZoneId: create.model().start.timeZoneId,
    });
  });

  it('preserves suggestion and fields regardless of response order', () => {
    const suggestionFirst = createState(false);
    suggestionFirst.applyDefaultStart({ localDate: '2030-07-07', localTime: '10:30', timeZoneId: 'UTC' });
    suggestionFirst.applyCurrentFields([
      { fieldKey: 'text1', label: 'Title', type: 'ShortText', maxLength: 50 },
    ]);

    const fieldsFirst = createState(false);
    fieldsFirst.applyCurrentFields([
      { fieldKey: 'text1', label: 'Title', type: 'ShortText', maxLength: 50 },
    ]);
    fieldsFirst.applyDefaultStart({ localDate: '2030-07-07', localTime: '10:30', timeZoneId: 'UTC' });

    expect(suggestionFirst.model()).toEqual(fieldsFirst.model());
  });
  it('applies current fields without replacing the selected create-mode time zone', () => {
    const state = createState(false);
    state.model.update((model) => ({
      ...model,
      start: { ...model.start, timeZoneId: 'UTC' },
    }));

    state.applyCurrentFields([
      { fieldKey: 'text1', label: 'Title', type: 'ShortText', maxLength: 50 },
    ]);

    expect(state.model()).toEqual({
      start: { date: '', time: '', timeZoneId: 'UTC' },
      texts: [
        {
          fieldKey: 'text1',
          label: 'Title',
          type: 'ShortText',
          maxLength: 50,
          value: '',
        },
      ],
    });
  });

  it('applies stored details as the clean edit baseline', () => {
    const state = createState(true);

    state.applyEventDetails(testEvent());

    expect(state.canUpdate()).toBe(true);
    expect(state.hasPendingChanges()).toBe(false);
    expect(state.updateRequest()).toEqual({
      start: { localDateTime: '2030-07-04T09:30:00', timeZoneId: 'Europe/London' },
      texts: [{ fieldKey: 'text1', value: 'English title' }],
    });
  });

  it('tracks normalized changes and replaces the baseline after save', () => {
    const state = createState(true);
    state.applyEventDetails(testEvent());
    state.form.texts[0].value().value.set('Updated title');

    expect(state.hasPendingChanges()).toBe(true);

    state.markSaved(state.updateRequest());

    expect(state.hasPendingChanges()).toBe(false);
  });

  it('does not report pending changes when updates are locked', () => {
    const state = createState(true);
    state.applyEventDetails(testEvent({ canUpdate: false }));
    state.model.update((model) => ({
      ...model,
      texts: model.texts.map((text) => ({ ...text, value: 'Changed' })),
    }));

    expect(state.hasPendingChanges()).toBe(false);
  });

  it('uses a live UTC preview for editable drafts and the stored instant when locked', () => {
    const editable = createState(true);
    editable.applyEventDetails(testEvent());
    const liveDisplay = editable.scheduledStartUtcDisplay();

    const locked = createState(true);
    locked.applyEventDetails(testEvent({ canUpdate: false }));

    expect(liveDisplay).not.toBe('');
    expect(locked.scheduledStartUtcDisplay()).toContain('2030');
  });

  it('marks invalid fields as touched during validation', () => {
    const state = createState(false);

    expect(state.validate()).toBe(false);
    expect(state.form.start.date().touched()).toBe(true);
  });

  it('maps valid create drafts through the form mapping module', () => {
    const state = createState(false);
    state.model.set({
      start: { date: '2999-01-01', time: '10:00', timeZoneId: 'UTC' },
      texts: [
        {
          fieldKey: 'text1',
          label: 'Title',
          type: 'ShortText',
          maxLength: 50,
          value: ' English title ',
        },
      ],
    });

    expect(state.validate()).toBe(true);
    expect(state.createRequest()).toEqual({
      start: { localDateTime: '2999-01-01T10:00:00', timeZoneId: 'UTC' },
      texts: [{ fieldKey: 'text1', value: 'English title' }],
    });
  });

  it('clears edit eligibility and baseline after load failure', () => {
    const state = createState(true);
    state.applyEventDetails(testEvent());

    state.resetAfterLoadFailure();

    expect(state.canUpdate()).toBe(false);
    expect(state.hasPendingChanges()).toBe(false);
    expect(state.scheduledStartUtcDisplay()).toBe('');
  });

  function createState(isEditMode: boolean): CalendarEventDraftState {
    return TestBed.runInInjectionContext(() => new CalendarEventDraftState(isEditMode));
  }

  function testEvent(
    overrides: Partial<CalendarEventDetailsResponse> = {},
  ): CalendarEventDetailsResponse {
    return testCalendarEventDetails({
      texts: [
        {
          fieldKey: 'text1',
          label: 'Title',
          type: 'ShortText',
          maxLength: 50,
          value: 'English title',
        },
      ],
      platforms: [],
      ...overrides,
    });
  }
});
