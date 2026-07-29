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
    const initialStart = { ...create.model().start };
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
    expect(create.hasPendingChanges()).toBe(true);

    create.resetToBaseline();

    expect(create.model().start).toEqual(initialStart);
  });

  it('keeps both create initialization response orders clean', () => {
    const suggestionFirst = createState(false);
    suggestionFirst.applyDefaultStart({
      localDate: '2030-07-07',
      localTime: '10:30',
      timeZoneId: 'UTC',
    });
    suggestionFirst.applyCurrentFields([
      { fieldKey: 'text1', label: 'Title', type: 'ShortText', maxLength: 50 },
    ]);

    const fieldsFirst = createState(false);
    fieldsFirst.applyCurrentFields([
      { fieldKey: 'text1', label: 'Title', type: 'ShortText', maxLength: 50 },
    ]);
    fieldsFirst.applyDefaultStart({
      localDate: '2030-07-07',
      localTime: '10:30',
      timeZoneId: 'UTC',
    });

    expect(suggestionFirst.model()).toEqual(fieldsFirst.model());
    expect(suggestionFirst.hasPendingChanges()).toBe(false);
    expect(fieldsFirst.hasPendingChanges()).toBe(false);
  });

  it('does not absorb concurrent text edits when applying a default start', () => {
    const state = createState(false);
    state.applyCurrentFields([
      { fieldKey: 'text1', label: 'Title', type: 'ShortText', maxLength: 50 },
    ]);
    state.form.texts[0].value().value.set('Operator title');

    state.applyDefaultStart({
      localDate: '2030-07-07',
      localTime: '10:30',
      timeZoneId: 'UTC',
    });

    expect(state.model().start).toEqual({
      date: '2030-07-07',
      time: '10:30',
      timeZoneId: 'UTC',
    });
    expect(state.model().texts[0].value).toBe('Operator title');
    expect(state.hasPendingChanges()).toBe(true);

    state.resetToBaseline();

    expect(state.model().start).toEqual({
      date: '2030-07-07',
      time: '10:30',
      timeZoneId: 'UTC',
    });
    expect(state.model().texts[0].value).toBe('');
    expect(state.hasPendingChanges()).toBe(false);
  });

  it('applies current fields without replacing live or baseline create starts', () => {
    const state = createState(false);
    const initialStart = { ...state.model().start };
    state.model.update((model) => ({
      ...model,
      start: { ...model.start, timeZoneId: 'Pacific/Auckland' },
    }));

    state.applyCurrentFields([
      { fieldKey: 'text1', label: 'Title', type: 'ShortText', maxLength: 50 },
    ]);

    expect(state.model()).toEqual({
      start: { date: '', time: '', timeZoneId: 'Pacific/Auckland' },
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
    expect(state.hasPendingChanges()).toBe(true);

    state.resetToBaseline();

    expect(state.model().start).toEqual(initialStart);
    expect(state.model().texts[0].value).toBe('');
    expect(state.hasPendingChanges()).toBe(false);
  });

  it('tracks normalized create changes from the initialized baseline', () => {
    const state = createState(false);
    state.applyCurrentFields([
      { fieldKey: 'text1', label: 'Title', type: 'ShortText', maxLength: 50 },
    ]);

    expect(state.hasPendingChanges()).toBe(false);

    state.form.texts[0].value().value.set('   ');

    expect(state.hasPendingChanges()).toBe(false);

    state.form.texts[0].value().value.set('Operator title');

    expect(state.hasPendingChanges()).toBe(true);
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

  it('commits the captured update model without absorbing later normalized or nested edits', () => {
    const state = createState(true);
    state.applyEventDetails(testEvent());
    state.model.update((model) => ({
      start: { ...model.start, date: '2030-07-05' },
      texts: model.texts.map((text) => ({
        ...text,
        label: text.fieldKey === 'text1' ? 'Submitted title label' : text.label,
        value: text.fieldKey === 'text1' ? ' Submitted title ' : text.value,
      })),
    }));

    const submission = state.captureUpdateSubmission();

    expect(submission.request).toEqual({
      start: { localDateTime: '2030-07-05T09:30:00', timeZoneId: 'Europe/London' },
      texts: [{ fieldKey: 'text1', value: 'Submitted title' }],
    });
    state.model.update((model) => ({
      ...model,
      texts: model.texts.map((text) => ({
        ...text,
        label: text.fieldKey === 'text1' ? 'Later label' : text.label,
        value: text.fieldKey === 'text1' ? 'Submitted title' : text.value,
      })),
    }));

    expect(submission.submittedModel.start.date).toBe('2030-07-05');
    expect(submission.submittedModel.texts[0]).toMatchObject({
      label: 'Submitted title label',
      value: ' Submitted title ',
    });

    state.commitUpdateSubmission(submission);

    expect(state.hasPendingChanges()).toBe(false);

    state.model.update((model) => ({
      start: { ...model.start, date: '2030-07-06' },
      texts: model.texts.map((text) => ({
        ...text,
        value: text.fieldKey === 'text1' ? 'Changed while saving' : text.value,
      })),
    }));
    submission.submittedModel.start.date = '2030-07-07';
    submission.submittedModel.texts[0].label = 'Mutated submitted label';
    submission.submittedModel.texts[0].value = 'Mutated submitted value';

    expect(state.hasPendingChanges()).toBe(true);
    state.resetToBaseline();

    expect(state.model().start.date).toBe('2030-07-05');
    expect(state.model().texts[0]).toMatchObject({
      label: 'Submitted title label',
      value: ' Submitted title ',
    });
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

  it('keeps nested baseline values isolated from live and reset models', () => {
    const state = createState(false);
    state.applyCurrentFields([
      { fieldKey: 'text1', label: 'Title', type: 'ShortText', maxLength: 50 },
    ]);

    state.model().start.date = '2030-08-01';
    state.model().texts[0].label = 'Changed title';
    state.model().texts[0].value = 'Changed value';
    state.resetToBaseline();

    expect(state.model().start.date).toBe('');
    expect(state.model().texts[0].label).toBe('Title');
    expect(state.model().texts[0].value).toBe('');

    state.model().start.time = '12:00';
    state.model().texts[0].maxLength = 100;
    state.resetToBaseline();

    expect(state.model().start.time).toBe('');
    expect(state.model().texts[0].maxLength).toBe(50);
  });

  it('resets full model values and form interaction state to the baseline', () => {
    const state = createState(false);
    state.applyCurrentFields([
      { fieldKey: 'text1', label: 'Title', type: 'ShortText', maxLength: 50 },
    ]);
    const baseline = {
      start: { ...state.model().start },
      texts: state.model().texts.map((text) => ({ ...text })),
    };
    state.model.update((model) => ({
      start: { date: '2030-08-01', time: '12:00', timeZoneId: 'UTC' },
      texts: model.texts.map((text) => ({
        ...text,
        label: 'Changed title',
        type: 'LongText',
        maxLength: 100,
        value: 'Changed value',
      })),
    }));
    state.form().markAsTouched();
    state.form().markAsDirty();

    expect(state.form().touched()).toBe(true);
    expect(state.form().dirty()).toBe(true);

    state.resetToBaseline();

    expect({
      start: state.model().start,
      texts: state.model().texts.map((text) => ({
        fieldKey: text.fieldKey,
        label: text.label,
        type: text.type,
        maxLength: text.maxLength,
        value: text.value,
      })),
    }).toEqual(baseline);
    expect(state.form().touched()).toBe(false);
    expect(state.form().dirty()).toBe(false);
    expect(state.form.texts[0].value().touched()).toBe(false);
    expect(state.form.texts[0].value().dirty()).toBe(false);
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
    state.form.texts[0].value().value.set('Unsaved title');

    state.resetAfterLoadFailure();
    state.resetToBaseline();

    expect(state.canUpdate()).toBe(false);
    expect(state.hasPendingChanges()).toBe(false);
    expect(state.scheduledStartUtcDisplay()).toBe('');
    expect(state.model().texts[0].value).toBe('Unsaved title');
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
