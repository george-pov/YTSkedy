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
  testCalendarEventDefaultStart,
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
      getDefaultStart: vi.fn().mockReturnValue(of(testCalendarEventDefaultStart())),
      create: vi.fn().mockReturnValue(of({ calendarEventId: 'created-event' })),
      update: vi.fn().mockReturnValue(of({ calendarEventId: 'event-1' })),
      delete: vi.fn().mockReturnValue(of(undefined)),
      publishPlatform: vi.fn(),
      deletePlatformPublication: vi.fn(),
      recoverPlatformPublication: vi.fn(),
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
    expect(calendarEvents['getDefaultStart']).toHaveBeenCalledTimes(1);
    expect(calendarEvents['getById']).not.toHaveBeenCalled();
    expect(state.isEditMode).toBe(false);
    expect(state.draft.model().texts).toHaveLength(2);
    expect(state.loadFailed()).toBe(false);
  });

  it('applies the create suggestion and keeps suggestion failure nonfatal', () => {
    calendarEvents['getDefaultStart'].mockReturnValue(
      of(
        testCalendarEventDefaultStart({
          localDate: '2030-07-07',
          localTime: '10:30',
          timeZoneId: 'UTC',
        }),
      ),
    );
    const successful = createState();
    successful.initialize();
    expect(successful.draft.model().start).toEqual({
      date: '2030-07-07',
      time: '10:30',
      timeZoneId: 'UTC',
    });

    calendarEvents['getDefaultStart'].mockReturnValue(throwError(() => new Error('network')));
    const failed = createState();
    failed.initialize();
    expect(failed.loadFailed()).toBe(false);
    expect(failed.defaultStartErrorMessage()).toContain('Enter the start manually.');
  });

  it('loads edit details and applies root and child state', () => {
    calendarEvents['getById'].mockReturnValue(of(testEvent()));
    const state = createState('event-1');

    state.initialize();

    expect(calendarEvents['getById']).toHaveBeenCalledWith('event-1');
    expect(eventTextFields.get).not.toHaveBeenCalled();
    expect(calendarEvents['getDefaultStart']).not.toHaveBeenCalled();
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
    calendarEvents['uploadThumbnail'].mockReturnValue(of({}));
    router.navigateByUrl.mockImplementation(() => {
      expect(state.hasPendingChanges()).toBe(false);
      expect(state.thumbnailEditor.selectedFile()).toBeNull();
      expect(state.canDeactivateWithPendingChanges()).toBe(true);
      return Promise.resolve(true);
    });
    state.initialize();
    fillValidDraft(state);
    state.thumbnailEditor.selectThumbnail(
      new File(['thumbnail'], 'stream.png', {
        type: 'image/png',
      }),
    );

    state.submit();

    expect(calendarEvents['create']).toHaveBeenCalledWith({
      start: { localDateTime: '2999-01-01T10:00:00', timeZoneId: 'UTC' },
      texts: [{ fieldKey: 'text1', value: 'English title' }],
    });
    expect(notifications.showSuccess).toHaveBeenCalledWith('Calendar event created.');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/calendar-events');
    expect(state.cancelDisabled()).toBe(true);
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
    router.navigateByUrl.mockImplementation(() => {
      expect(state.hasPendingChanges()).toBe(false);
      expect(state.thumbnailEditor.selectedFile()).toBeNull();
      expect(state.canDeactivateWithPendingChanges()).toBe(true);
      return Promise.resolve(true);
    });

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

  it('preserves the selected create thumbnail when event creation fails', () => {
    calendarEvents['create'].mockReturnValue(throwError(() => new Error('network')));
    const state = createState();
    state.initialize();
    fillValidDraft(state);
    const file = new File(['thumbnail'], 'stream.png', {
      type: 'image/png',
    });
    state.thumbnailEditor.selectThumbnail(file);

    state.submit();

    expect(state.thumbnailEditor.selectedFile()).toBe(file);
    expect(state.hasPendingChanges()).toBe(true);
    expect(state.saveErrorMessage()).toContain('Check your connection');
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('updates an edited event and replaces the pending-change baseline', () => {
    calendarEvents['getById'].mockReturnValue(of(testEvent()));
    const state = createState('event-1');
    state.initialize();
    state.draft.form.texts[0].value().value.set('Updated title');
    state.platformsState.publishPlatform(state.platformsState.platforms()[0]);

    expect(state.platformsState.platformActionBlockedMessage()).not.toBeNull();

    state.submit();

    expect(calendarEvents['update']).toHaveBeenCalledWith(
      'event-1',
      expect.objectContaining({
        texts: expect.arrayContaining([{ fieldKey: 'text1', value: 'Updated title' }]),
      }),
    );
    expect(state.draft.hasPendingChanges()).toBe(false);
    expect(state.platformsState.platformActionBlockedMessage()).toBeNull();
    expect(notifications.showSuccess).toHaveBeenCalledWith('Calendar event updated.');
  });

  it('preserves the submitted baseline and later edits when an update completes', async () => {
    const firstUpdate = new Subject<{ calendarEventId: string }>();
    const secondUpdate = new Subject<{ calendarEventId: string }>();
    calendarEvents['getById'].mockReturnValue(of(testEvent()));
    calendarEvents['update']
      .mockReturnValueOnce(firstUpdate.asObservable())
      .mockReturnValueOnce(secondUpdate.asObservable());
    confirmation.confirm.mockReturnValue(of('keep-editing'));
    const state = createState('event-1');
    state.initialize();
    state.draft.form.texts[0].value().value.set('Submitted title');
    state.platformsState.publishPlatform(state.platformsState.platforms()[0]);

    state.submit();
    expect(state.draft.form.texts[0].value().disabled()).toBe(false);
    state.draft.form.texts[0].value().value.set('Changed while saving');
    firstUpdate.next({ calendarEventId: 'event-1' });
    firstUpdate.complete();

    expect(calendarEvents['update']).toHaveBeenCalledWith(
      'event-1',
      expect.objectContaining({
        texts: expect.arrayContaining([{ fieldKey: 'text1', value: 'Submitted title' }]),
      }),
    );
    expect(state.draft.model().texts[0].value).toBe('Changed while saving');
    expect(state.draft.hasPendingChanges()).toBe(true);
    expect(state.platformsState.platformActionBlockedMessage()).not.toBeNull();
    expect(state.saveDisabled()).toBe(false);
    expect(state.cancelDisabled()).toBe(false);

    const decision = state.canDeactivateWithPendingChanges();
    expect(typeof decision).not.toBe('boolean');
    expect(await firstValueFrom(decision as Observable<boolean>)).toBe(false);

    confirmation.confirm.mockReturnValue(of('discard'));
    state.cancel();

    expect(state.draft.model().texts[0].value).toBe('Submitted title');
    expect(state.draft.hasPendingChanges()).toBe(false);
    expect(state.platformsState.platformActionBlockedMessage()).toBeNull();

    state.draft.form.texts[0].value().value.set('Changed while saving');
    state.submit();
    secondUpdate.next({ calendarEventId: 'event-1' });
    secondUpdate.complete();

    expect(calendarEvents['update']).toHaveBeenNthCalledWith(
      2,
      'event-1',
      expect.objectContaining({
        texts: expect.arrayContaining([{ fieldKey: 'text1', value: 'Changed while saving' }]),
      }),
    );
    expect(state.draft.hasPendingChanges()).toBe(false);
  });

  it('maps update conflicts without changing the baseline or blocked guidance', () => {
    calendarEvents['getById'].mockReturnValue(of(testEvent()));
    calendarEvents['update'].mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 409 })),
    );
    confirmation.confirm.mockReturnValue(of('keep-editing'));
    const state = createState('event-1');
    state.initialize();
    state.draft.form.texts[0].value().value.set('Updated title');
    state.platformsState.publishPlatform(state.platformsState.platforms()[0]);

    state.submit();

    expect(state.saveErrorMessage()).toBe(
      'The event can no longer be updated. Reload the page and try again.',
    );
    expect(state.platformsState.platformActionBlockedMessage()).not.toBeNull();

    state.cancel();

    expect(state.draft.model().texts[0].value).toBe('Updated title');
    expect(state.draft.hasPendingChanges()).toBe(true);
    expect(state.platformsState.platformActionBlockedMessage()).not.toBeNull();

    confirmation.confirm.mockReturnValue(of('discard'));
    state.cancel();

    expect(state.draft.model().texts[0].value).toBe('English title');
    expect(state.draft.hasPendingChanges()).toBe(false);
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

  it('keeps clean Cancel disabled and inert', () => {
    const state = createState();
    state.initialize();

    state.cancel();

    expect(state.hasPendingChanges()).toBe(false);
    expect(state.cancelDisabled()).toBe(true);
    expect(confirmation.confirm).not.toHaveBeenCalled();
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('confirms and resets edit changes in place', () => {
    calendarEvents['getById'].mockReturnValue(of(testEvent()));
    confirmation.confirm.mockReturnValue(of('discard'));
    const state = createState('event-1');
    state.initialize();
    state.draft.form.texts[0].value().value.set('Updated title');
    state.draft.form.texts[0].value().markAsTouched();
    state.platformsState.publishPlatform(state.platformsState.platforms()[0]);

    expect(state.platformsState.platformActionBlockedMessage()).not.toBeNull();

    state.cancel();

    expect(confirmation.confirm).toHaveBeenCalledWith(
      expect.objectContaining({
        title: 'Discard unsaved event changes?',
        body: expect.stringContaining('thumbnail selected for a new event'),
        actions: [
          { id: 'keep-editing', label: 'Keep editing' },
          {
            id: 'discard',
            label: 'Discard changes',
            primary: true,
            variant: 'danger-filled',
          },
        ],
      }),
    );
    expect(state.draft.model().texts[0].value).toBe('English title');
    expect(state.draft.form.texts[0].value().touched()).toBe(false);
    expect(state.hasPendingChanges()).toBe(false);
    expect(state.platformsState.platformActionBlockedMessage()).toBeNull();
    expect(state.cancelDisabled()).toBe(true);
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('resets create changes, validation state, save error, and local thumbnail in place', () => {
    calendarEvents['create'].mockReturnValue(throwError(() => new Error('network')));
    confirmation.confirm.mockReturnValue(of('discard'));
    const state = createState();
    state.initialize();
    const baseline = state.draft.model();
    fillValidDraft(state);
    state.draft.form.start.date().markAsTouched();
    state.thumbnailEditor.selectThumbnail(
      new File(['thumbnail'], 'stream.png', {
        type: 'image/png',
      }),
    );
    state.submit();

    expect(state.saveErrorMessage()).not.toBeNull();
    expect(state.hasPendingChanges()).toBe(true);

    state.cancel();

    expect(state.draft.model()).toMatchObject(baseline);
    expect(state.draft.form.start.date().touched()).toBe(false);
    expect(state.thumbnailEditor.selectedFile()).toBeNull();
    expect(state.saveErrorMessage()).toBeNull();
    expect(state.hasPendingChanges()).toBe(false);
    expect(state.cancelDisabled()).toBe(true);
    expect(calendarEvents['uploadThumbnail']).not.toHaveBeenCalled();
    expect(calendarEvents['deleteThumbnail']).not.toHaveBeenCalled();
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('resets a thumbnail-only create change without calling thumbnail APIs', () => {
    confirmation.confirm.mockReturnValue(of('discard'));
    const state = createState();
    state.initialize();
    state.thumbnailEditor.selectThumbnail(
      new File(['thumbnail'], 'stream.png', {
        type: 'image/png',
      }),
    );

    expect(state.draft.hasPendingChanges()).toBe(false);
    expect(state.hasPendingChanges()).toBe(true);
    expect(state.cancelDisabled()).toBe(false);

    state.cancel();

    expect(state.thumbnailEditor.selectedFile()).toBeNull();
    expect(state.hasPendingChanges()).toBe(false);
    expect(calendarEvents['uploadThumbnail']).not.toHaveBeenCalled();
    expect(calendarEvents['deleteThumbnail']).not.toHaveBeenCalled();
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('preserves create values, validation state, and thumbnail when discard is rejected', () => {
    confirmation.confirm.mockReturnValue(of('keep-editing'));
    const state = createState();
    state.initialize();
    fillValidDraft(state);
    state.draft.form.start.date().markAsTouched();
    const file = new File(['thumbnail'], 'stream.png', {
      type: 'image/png',
    });
    state.thumbnailEditor.selectThumbnail(file);

    state.cancel();

    expect(state.draft.model().texts[0].value).toBe('English title');
    expect(state.draft.form.start.date().touched()).toBe(true);
    expect(state.thumbnailEditor.selectedFile()).toBe(file);
    expect(state.hasPendingChanges()).toBe(true);
    expect(router.navigateByUrl).not.toHaveBeenCalled();
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

  it('delegates thumbnail-only create route exit to the discard confirmation', async () => {
    confirmation.confirm.mockReturnValue(of('keep-editing'));
    const state = createState();
    state.initialize();
    state.thumbnailEditor.selectThumbnail(
      new File(['thumbnail'], 'stream.png', {
        type: 'image/png',
      }),
    );

    const decision = state.canDeactivateWithPendingChanges();

    expect(typeof decision).not.toBe('boolean');
    expect(await firstValueFrom(decision as Observable<boolean>)).toBe(false);
  });

  it('denies route deactivation while create and update mutations are active', () => {
    const create = new Subject<{ calendarEventId: string }>();
    calendarEvents['create'].mockReturnValue(create.asObservable());
    const creating = createState();
    creating.initialize();
    fillValidDraft(creating);
    creating.submit();

    expect(creating.canDeactivateWithPendingChanges()).toBe(false);

    calendarEvents['getById'].mockReturnValue(of(testEvent()));
    const update = new Subject<{ calendarEventId: string }>();
    calendarEvents['update'].mockReturnValue(update.asObservable());
    const updating = createState('event-1');
    updating.initialize();
    updating.draft.form.texts[0].value().value.set('Changed');
    updating.submit();

    expect(updating.canDeactivateWithPendingChanges()).toBe(false);

    create.error(new Error('create failed'));
    update.error(new Error('update failed'));
    expect(creating.hasActiveMutation()).toBe(false);
    expect(updating.hasActiveMutation()).toBe(false);
  });

  it('allows route deactivation while an initial details read is active', () => {
    calendarEvents['getById'].mockReturnValue(new Subject<CalendarEventDetailsResponse>());
    const state = createState('event-1');

    state.initialize();

    expect(state.canDeactivateWithPendingChanges()).toBe(true);
  });

  it.each([
    {
      scenario: 'successful delete',
      result: of(undefined),
      notification: 'Calendar event deleted.',
    },
    {
      scenario: 'missing event',
      result: throwError(() => new HttpErrorResponse({ status: 404 })),
      notification: 'Calendar event no longer exists.',
    },
  ])('clears pending changes before navigation after $scenario', ({ result, notification }) => {
    calendarEvents['getById'].mockReturnValue(of(testEvent()));
    calendarEvents['delete'].mockReturnValue(result);
    confirmation.confirm.mockReturnValueOnce(of('discard')).mockReturnValueOnce(of('delete'));
    const state = createState('event-1');
    router.navigateByUrl.mockImplementation(() => {
      expect(state.canDeactivateWithPendingChanges()).toBe(true);
      return Promise.resolve(true);
    });
    state.initialize();
    state.draft.form.texts[0].value().value.set('Unsaved title');

    state.deleteEvent();

    expect(calendarEvents['delete']).toHaveBeenCalledWith('event-1');
    expect(notifications.showSuccess).toHaveBeenCalledWith(notification);
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
