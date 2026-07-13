import { HttpErrorResponse } from '@angular/common/http';
import { type DestroyRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { type Router } from '@angular/router';
import { firstValueFrom, type Observable, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, type Mock, vi } from 'vitest';

import {
  type CalendarEventDetailsResponse,
  type CalendarEventsService,
} from 'src/app/shared/api/calendar-events/calendar-events-service';
import { type EventTextFieldsService } from 'src/app/shared/api/settings/event-text-fields-service';
import { type ConfirmationDialogService } from 'src/app/shared/components/confirmation-dialog/confirmation-dialog-service';
import { type NotificationService } from 'src/app/shared/notifications/notification-service';
import { CalendarEventDetailsState } from './calendar-event-details.state';
import {
  testCalendarEventDetails,
  testEventTextFieldsResponse,
} from './testing/calendar-event-details.fixture';
import { thumbnailErrorNavigationStateKey } from './thumbnail-editor/thumbnail-editor.state';

describe('CalendarEventDetailsState', () => {
  let calendarEvents: Record<string, Mock>;
  let eventTextFields: { get: Mock };
  let confirmation: { confirm: Mock };
  let notifications: { showSuccess: Mock };
  let router: { getCurrentNavigation: Mock; navigateByUrl: Mock };
  let destroyRef: DestroyRef;

  beforeEach(() => {
    calendarEvents = {
      getById: vi.fn(),
      create: vi.fn().mockReturnValue(of({ calendarEventId: 'created-event' })),
      update: vi.fn().mockReturnValue(of({ calendarEventId: 'event-1' })),
      delete: vi.fn().mockReturnValue(of(undefined)),
      publishPlatform: vi.fn(),
      deletePlatformPublication: vi.fn(),
      getPublishingContent: vi.fn(),
      uploadThumbnail: vi.fn(),
      getThumbnail: vi.fn(),
      deleteThumbnail: vi.fn(),
    };
    eventTextFields = {
      get: vi.fn().mockReturnValue(of(testEventTextFieldsResponse())),
    };
    confirmation = { confirm: vi.fn().mockReturnValue(of('delete')) };
    notifications = { showSuccess: vi.fn() };
    router = {
      getCurrentNavigation: vi.fn().mockReturnValue(null),
      navigateByUrl: vi.fn().mockResolvedValue(true),
    };
    destroyRef = {
      destroyed: false,
      onDestroy: vi.fn(() => () => undefined),
    };
  });

  it('initializes create mode once from the current event text fields', () => {
    const state = createState();

    state.initialize();
    state.initialize();

    expect(eventTextFields.get).toHaveBeenCalledTimes(1);
    expect(calendarEvents['getById']).not.toHaveBeenCalled();
    expect(state.isEditMode).toBe(false);
    expect(state.draft.model().texts).toHaveLength(2);
    expect(state.loadFailed()).toBe(false);
  });

  it('loads edit details and applies root and child state', () => {
    calendarEvents['getById'].mockReturnValue(of(testEvent()));
    const state = createState('event-1');

    state.initialize();

    expect(calendarEvents['getById']).toHaveBeenCalledWith('event-1');
    expect(eventTextFields.get).not.toHaveBeenCalled();
    expect(state.draft.model().texts[0].value).toBe('English title');
    expect(state.canDelete()).toBe(true);
    expect(state.platformsState.platforms()).toHaveLength(1);
  });

  it('resets coordinated state when edit loading fails', () => {
    calendarEvents['getById'].mockReturnValue(throwError(() => new Error('network')));
    const state = createState('event-1');

    state.initialize();

    expect(state.loadFailed()).toBe(true);
    expect(state.draft.canUpdate()).toBe(false);
    expect(state.canDelete()).toBe(false);
    expect(state.platformsState.platforms()).toEqual([]);
  });

  it('validates and creates an event before navigating to the list', () => {
    const state = createState();
    state.initialize();
    fillValidDraft(state);

    state.submit();

    expect(calendarEvents['create']).toHaveBeenCalledWith({
      start: { localDateTime: '2999-01-01T10:00:00', timeZoneId: 'UTC' },
      texts: [{ fieldKey: 'text1', value: 'English title' }],
    });
    expect(notifications.showSuccess).toHaveBeenCalledWith('Calendar event created.');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/calendar-events');
  });

  it('does not create when the draft is invalid', () => {
    const state = createState();
    state.initialize();

    state.submit();

    expect(calendarEvents['create']).not.toHaveBeenCalled();
    expect(state.draft.form.start.date().touched()).toBe(true);
  });

  it('keeps a created event and routes to edit when thumbnail upload fails', () => {
    calendarEvents['uploadThumbnail'].mockReturnValue(throwError(() => new Error('network')));
    const state = createState();
    state.initialize();
    fillValidDraft(state);
    state.thumbnailEditor.selectThumbnail(
      new File(['thumbnail'], 'stream.png', {
        type: 'image/png',
      }),
    );

    state.submit();

    expect(calendarEvents['uploadThumbnail']).toHaveBeenCalledWith(
      'created-event',
      expect.any(File),
    );
    expect(notifications.showSuccess).toHaveBeenCalledWith('Calendar event created.');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/calendar-events/created-event/edit', {
      state: {
        [thumbnailErrorNavigationStateKey]:
          'The thumbnail could not be changed. Check your connection and try again.',
      },
    });
  });

  it('updates an edited event and replaces the pending-change baseline', () => {
    calendarEvents['getById'].mockReturnValue(of(testEvent()));
    const state = createState('event-1');
    state.initialize();
    state.draft.form.texts[0].value().value.set('Updated title');

    state.submit();

    expect(calendarEvents['update']).toHaveBeenCalledWith(
      'event-1',
      expect.objectContaining({
        texts: expect.arrayContaining([{ fieldKey: 'text1', value: 'Updated title' }]),
      }),
    );
    expect(state.draft.hasPendingChanges()).toBe(false);
    expect(notifications.showSuccess).toHaveBeenCalledWith('Calendar event updated.');
  });

  it('maps update conflicts to the reload message', () => {
    calendarEvents['getById'].mockReturnValue(of(testEvent()));
    calendarEvents['update'].mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 409 })),
    );
    const state = createState('event-1');
    state.initialize();
    state.draft.form.texts[0].value().value.set('Updated title');

    state.submit();

    expect(state.saveErrorMessage()).toBe(
      'The event can no longer be updated. Reload the page and try again.',
    );
  });

  it('blocks other actions while create is active', () => {
    const create = new Subject<{ calendarEventId: string }>();
    calendarEvents['create'].mockReturnValue(create.asObservable());
    const state = createState();
    state.initialize();
    fillValidDraft(state);

    state.submit();
    state.submit();
    state.cancel();

    expect(calendarEvents['create']).toHaveBeenCalledTimes(1);
    expect(state.hasActiveMutation()).toBe(true);
    expect(state.cancelDisabled()).toBe(true);
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('confirms pending changes before cancel navigation', () => {
    calendarEvents['getById'].mockReturnValue(of(testEvent()));
    confirmation.confirm.mockReturnValue(of('discard'));
    const state = createState('event-1');
    state.initialize();
    state.draft.form.texts[0].value().value.set('Updated title');

    state.cancel();

    expect(confirmation.confirm).toHaveBeenCalledWith(
      expect.objectContaining({ title: 'Discard unsaved event changes?' }),
    );
    expect(router.navigateByUrl).toHaveBeenCalledWith('/calendar-events');
  });

  it('delegates pending route exit to the discard confirmation', async () => {
    calendarEvents['getById'].mockReturnValue(of(testEvent()));
    confirmation.confirm.mockReturnValue(of('keep-editing'));
    const state = createState('event-1');
    state.initialize();
    state.draft.form.texts[0].value().value.set('Updated title');

    const decision = state.canDeactivateWithPendingChanges();

    expect(typeof decision).not.toBe('boolean');
    expect(await firstValueFrom(decision as Observable<boolean>)).toBe(false);
  });

  it('deletes after confirmation and treats a missing event as success', () => {
    calendarEvents['getById'].mockReturnValue(of(testEvent()));
    calendarEvents['delete'].mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 404 })),
    );
    const state = createState('event-1');
    state.initialize();

    state.deleteEvent();

    expect(calendarEvents['delete']).toHaveBeenCalledWith('event-1');
    expect(notifications.showSuccess).toHaveBeenCalledWith('Calendar event no longer exists.');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/calendar-events');
  });

  it('keeps delete conflicts on the page with recovery guidance', () => {
    calendarEvents['getById'].mockReturnValue(of(testEvent()));
    calendarEvents['delete'].mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 409 })),
    );
    const state = createState('event-1');
    state.initialize();

    state.deleteEvent();

    expect(state.deleteErrorMessage()).toBe(
      'Delete platform publications before deleting this event.',
    );
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('destroys thumbnail-owned browser resources', () => {
    const state = createState();
    const destroy = vi.spyOn(state.thumbnailEditor, 'destroy');

    state.destroy();

    expect(destroy).toHaveBeenCalledTimes(1);
  });

  function createState(calendarEventId: string | null = null): CalendarEventDetailsState {
    return TestBed.runInInjectionContext(
      () =>
        new CalendarEventDetailsState({
          calendarEventId,
          calendarEvents: calendarEvents as unknown as CalendarEventsService,
          eventTextFields: eventTextFields as unknown as EventTextFieldsService,
          confirmation: confirmation as unknown as ConfirmationDialogService,
          notifications: notifications as unknown as NotificationService,
          router: router as unknown as Router,
          destroyRef,
        }),
    );
  }

  function fillValidDraft(state: CalendarEventDetailsState): void {
    state.draft.model.set({
      start: { date: '2999-01-01', time: '10:00', timeZoneId: 'UTC' },
      texts: [
        {
          fieldKey: 'text1',
          label: 'Title',
          type: 'ShortText',
          maxLength: 50,
          value: 'English title',
        },
      ],
    });
  }

  function testEvent(
    overrides: Partial<CalendarEventDetailsResponse> = {},
  ): CalendarEventDetailsResponse {
    return testCalendarEventDetails(overrides);
  }
});
