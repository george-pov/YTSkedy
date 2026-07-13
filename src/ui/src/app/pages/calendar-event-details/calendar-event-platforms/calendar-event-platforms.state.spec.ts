import { HttpErrorResponse } from '@angular/common/http';
import { signal, type DestroyRef } from '@angular/core';
import { Observable, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, type Mock, vi } from 'vitest';

import {
  CalendarEventDetailsResponse,
  CalendarEventPlatform,
  CalendarEventsService,
  EventPlatformPublishingContent,
  PublishPlatformResponse,
} from 'src/app/shared/api/calendar-events/calendar-events-service';
import { ConfirmationDialogService } from 'src/app/shared/components/confirmation-dialog/confirmation-dialog-service';
import { NotificationService } from 'src/app/shared/notifications/notification-service';
import {
  testCalendarEventDetails,
  testCalendarEventPlatform,
} from '../testing/calendar-event-details.fixture';
import {
  CalendarEventPlatformsState,
  describeDeletePublicationError,
  describePreviewError,
  describePublishError,
  thumbnailStatusText,
} from './calendar-event-platforms.state';

describe('CalendarEventPlatformsState', () => {
  const calendarEventId = 'event-1';

  let service: {
    getById: Mock<(calendarEventId: string) => Observable<CalendarEventDetailsResponse>>;
    publishPlatform: Mock<
      (calendarEventId: string, platformId: string) => Observable<PublishPlatformResponse>
    >;
    deletePlatformPublication: Mock<
      (calendarEventId: string, platformId: string) => Observable<CalendarEventPlatform>
    >;
    getPublishingContent: Mock<
      (calendarEventId: string, platformId: string) => Observable<EventPlatformPublishingContent>
    >;
  };
  let confirmation: { confirm: Mock<(data: unknown) => Observable<string | undefined>> };
  let notifications: { showSuccess: Mock<(message: string) => void> };
  let destroyRef: DestroyRef;
  let activePageMutation = signal(false);
  let pendingEventChanges = signal(false);
  let appliedEvents: CalendarEventDetailsResponse[];
  let state: CalendarEventPlatformsState;

  beforeEach(() => {
    service = {
      getById: vi.fn<(calendarEventId: string) => Observable<CalendarEventDetailsResponse>>(),
      publishPlatform:
        vi.fn<
          (calendarEventId: string, platformId: string) => Observable<PublishPlatformResponse>
        >(),
      deletePlatformPublication:
        vi.fn<(calendarEventId: string, platformId: string) => Observable<CalendarEventPlatform>>(),
      getPublishingContent:
        vi.fn<
          (
            calendarEventId: string,
            platformId: string,
          ) => Observable<EventPlatformPublishingContent>
        >(),
    };
    confirmation = { confirm: vi.fn<(data: unknown) => Observable<string | undefined>>() };
    confirmation.confirm.mockReturnValue(of('delete'));
    notifications = { showSuccess: vi.fn<(message: string) => void>() };
    destroyRef = {
      destroyed: false,
      onDestroy: vi.fn(() => () => undefined),
    };
    activePageMutation = signal(false);
    pendingEventChanges = signal(false);
    appliedEvents = [];
    state = createState();
  });

  function createState(
    calendarEventIdOverride: string | null = calendarEventId,
  ): CalendarEventPlatformsState {
    let createdState!: CalendarEventPlatformsState;
    createdState = new CalendarEventPlatformsState(
      service as unknown as CalendarEventsService,
      confirmation as unknown as ConfirmationDialogService,
      notifications as unknown as NotificationService,
      calendarEventIdOverride,
      destroyRef,
      () => activePageMutation(),
      () => pendingEventChanges(),
      (event) => {
        appliedEvents.push(event);
        createdState.applyEventDetails(event);
      },
    );
    return createdState;
  }

  it('maps platform-specific API errors to user-facing messages', () => {
    expect(describePublishError(new HttpErrorResponse({ status: 403 }))).toContain('permission');
    expect(describePublishError(new HttpErrorResponse({ status: 409 }))).toContain(
      'can no longer publish',
    );
    expect(describePublishError(new HttpErrorResponse({ status: 502 }))).toContain(
      'Try again later',
    );
    expect(describePublishError(new Error('network'))).toContain('Check your connection');

    expect(describeDeletePublicationError(new HttpErrorResponse({ status: 409 }))).toContain(
      'can no longer be deleted',
    );
    expect(describeDeletePublicationError(new HttpErrorResponse({ status: 502 }))).toContain(
      'provider publication could not be deleted',
    );
    expect(describeDeletePublicationError(new Error('network'))).toContain('Check your connection');

    expect(describePreviewError(new HttpErrorResponse({ status: 404 }))).toContain(
      'no longer available',
    );
    expect(describePreviewError(new HttpErrorResponse({ status: 409 }))).toContain(
      'cannot be previewed',
    );
    expect(describePreviewError(new Error('network'))).toContain('Check your connection');
  });

  it('maps failed thumbnail publish status to a non-actionable warning', () => {
    expect(thumbnailStatusText(publishedPlatform({ thumbnailStatus: 'Failed' }))).toContain(
      'thumbnail was not applied',
    );
    expect(thumbnailStatusText(publishedPlatform({ thumbnailStatus: 'Applied' }))).toBeNull();
    expect(
      thumbnailStatusText(
        publishedPlatform({
          platformType: 'WordPress',
          thumbnailStatus: null,
        }),
      ),
    ).toBeNull();
  });

  it('applies and resets platform details', () => {
    state.applyEventDetails(sampleEvent({ platforms: [publishedPlatform()] }));

    expect(state.platforms()).toEqual([publishedPlatform()]);

    state.resetAfterLoadFailure();

    expect(state.platforms()).toEqual([]);
    expect(state.hasActiveMutation()).toBe(false);
    expect(state.publishErrorMessage()).toBeNull();
    expect(state.deletePublicationErrorMessage()).toBeNull();
    expect(state.previewedPublishingContent()).toBeNull();
  });

  it('loads publishing content preview without blocking pending event changes', () => {
    pendingEventChanges.set(true);
    service.getPublishingContent.mockReturnValue(
      of({
        type: 'Preview',
        title: 'Rendered title',
        description: 'Rendered description',
      }),
    );

    state.previewPublishingContent(draftPlatform());

    expect(service.getPublishingContent).toHaveBeenCalledWith(calendarEventId, 'platform-1');
    expect(state.previewedPublishingContent()).toEqual({
      platformId: 'platform-1',
      platformName: 'Main YouTube channel',
      type: 'Preview',
      title: 'Rendered title',
      description: 'Rendered description',
    });
    expect(state.showStoredValuesPreviewNote()).toBe(true);
  });

  it('tracks the active preview mutation until publishing content returns', () => {
    const preview = new Subject<EventPlatformPublishingContent>();
    service.getPublishingContent.mockReturnValue(preview.asObservable());

    state.previewPublishingContent(draftPlatform());

    expect(state.previewingPlatformId()).toBe('platform-1');
    expect(state.hasActiveMutation()).toBe(true);

    preview.next({ type: 'Preview', title: 'Rendered title', description: null });
    preview.complete();

    expect(state.previewingPlatformId()).toBeNull();
    expect(state.hasActiveMutation()).toBe(false);
  });

  it('maps preview errors onto state', () => {
    service.getPublishingContent.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 409 })),
    );

    state.previewPublishingContent(draftPlatform());

    expect(state.previewErrorMessage()).toBe(
      'Publishing content cannot be previewed. Reload the page and try again.',
    );
  });

  it('blocks platform publish when event changes are pending', () => {
    pendingEventChanges.set(true);

    state.publishPlatform(draftPlatform());

    expect(service.publishPlatform).not.toHaveBeenCalled();
    expect(state.platformActionBlockedMessage()).toBe(
      'Save or discard event changes before publishing.',
    );
  });

  it('refreshes full event details after publish and clears that platform preview', () => {
    service.getPublishingContent.mockReturnValue(
      of({ type: 'Preview', title: 'Rendered title', description: null }),
    );
    service.publishPlatform.mockReturnValue(of(publishedPlatform()));
    service.getById.mockReturnValue(
      of(
        sampleEvent({
          canUpdate: false,
          canDelete: false,
          platforms: [publishedPlatform()],
        }),
      ),
    );
    state.previewPublishingContent(draftPlatform());

    state.publishPlatform(draftPlatform());

    expect(service.publishPlatform).toHaveBeenCalledWith(calendarEventId, 'platform-1');
    expect(service.getById).toHaveBeenCalledWith(calendarEventId);
    expect(appliedEvents).toHaveLength(1);
    expect(state.platforms()).toEqual([publishedPlatform()]);
    expect(state.previewedPublishingContent()).toBeNull();
    expect(notifications.showSuccess).toHaveBeenCalledWith('Calendar event published.');
  });

  it('maps publish errors onto state', () => {
    service.publishPlatform.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 502 })),
    );

    state.publishPlatform(draftPlatform());

    expect(state.publishErrorMessage()).toBe(
      'The platform could not publish this event. Try again later.',
    );
  });

  it('reports successful publish when the follow-up details refresh fails', () => {
    service.getPublishingContent.mockReturnValue(
      of({ type: 'Preview', title: 'Rendered title', description: null }),
    );
    service.publishPlatform.mockReturnValue(of(publishedPlatform()));
    service.getById.mockReturnValue(throwError(() => new Error('network')));
    state.previewPublishingContent(draftPlatform());

    state.publishPlatform(draftPlatform());

    expect(state.publishErrorMessage()).toBe(
      'The event was published, but the latest calendar event details could not be loaded. Reload the page.',
    );
    expect(state.previewedPublishingContent()).toBeNull();
    expect(notifications.showSuccess).not.toHaveBeenCalled();
  });

  it('does not delete a platform publication when confirmation is cancelled', () => {
    confirmation.confirm.mockReturnValue(of('cancel'));

    state.deletePlatformPublication(publishedPlatform());

    expect(confirmation.confirm).toHaveBeenCalledTimes(1);
    expect(service.deletePlatformPublication).not.toHaveBeenCalled();
  });

  it('blocks platform publication delete when event changes are pending', () => {
    pendingEventChanges.set(true);

    state.deletePlatformPublication(publishedPlatform());

    expect(confirmation.confirm).not.toHaveBeenCalled();
    expect(service.deletePlatformPublication).not.toHaveBeenCalled();
    expect(state.platformActionBlockedMessage()).toBe(
      'Save or discard event changes before deleting a publication.',
    );
  });

  it('refreshes full event details after publication delete', () => {
    const unpublishedPlatform = draftPlatform();
    service.deletePlatformPublication.mockReturnValue(of(unpublishedPlatform));
    service.getById.mockReturnValue(
      of(
        sampleEvent({
          canUpdate: true,
          canDelete: true,
          platforms: [unpublishedPlatform],
        }),
      ),
    );

    state.deletePlatformPublication(publishedPlatform());

    expect(confirmation.confirm).toHaveBeenCalledWith(
      expect.objectContaining({
        kind: 'warning',
        title: 'Delete publication for Main YouTube channel?',
      }),
    );
    expect(service.deletePlatformPublication).toHaveBeenCalledWith(calendarEventId, 'platform-1');
    expect(service.getById).toHaveBeenCalledWith(calendarEventId);
    expect(appliedEvents).toHaveLength(1);
    expect(state.platforms()).toEqual([unpublishedPlatform]);
    expect(notifications.showSuccess).toHaveBeenCalledWith('Platform publication deleted.');
  });

  it('maps publication-delete errors onto state', () => {
    service.deletePlatformPublication.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 502 })),
    );

    state.deletePlatformPublication(publishedPlatform());

    expect(state.deletePublicationErrorMessage()).toBe(
      'The provider publication could not be deleted. Try again later.',
    );
  });

  it('reports successful publication delete when the follow-up details refresh fails', () => {
    service.getPublishingContent.mockReturnValue(
      of({ type: 'Snapshot', title: 'Stored title', description: null }),
    );
    service.deletePlatformPublication.mockReturnValue(of(draftPlatform()));
    service.getById.mockReturnValue(throwError(() => new Error('network')));
    state.previewPublishingContent(publishedPlatform());

    state.deletePlatformPublication(publishedPlatform());

    expect(state.deletePublicationErrorMessage()).toBe(
      'The publication was deleted, but the latest calendar event details could not be loaded. Reload the page.',
    );
    expect(state.previewedPublishingContent()).toBeNull();
    expect(notifications.showSuccess).not.toHaveBeenCalled();
  });

  it('ignores platform actions without an edit event id or while another page mutation is active', () => {
    activePageMutation.set(true);

    state.publishPlatform(draftPlatform());
    state.previewPublishingContent(draftPlatform());
    state.deletePlatformPublication(publishedPlatform());

    expect(service.publishPlatform).not.toHaveBeenCalled();
    expect(service.getPublishingContent).not.toHaveBeenCalled();
    expect(confirmation.confirm).not.toHaveBeenCalled();

    activePageMutation.set(false);
    const createModeState = createState(null);
    createModeState.publishPlatform(draftPlatform());

    expect(service.publishPlatform).not.toHaveBeenCalled();
  });

  function draftPlatform(overrides: Partial<CalendarEventPlatform> = {}): CalendarEventPlatform {
    return testCalendarEventPlatform({
      status: 'NotPublished',
      externalResourceId: null,
      thumbnailStatus: 'NotConfigured',
      publishedUtc: null,
      canPublish: true,
      canDeletePublication: false,
      ...overrides,
    });
  }

  function publishedPlatform(
    overrides: Partial<CalendarEventPlatform> = {},
  ): CalendarEventPlatform {
    return testCalendarEventPlatform(overrides);
  }

  function sampleEvent(
    overrides: Partial<CalendarEventDetailsResponse> = {},
  ): CalendarEventDetailsResponse {
    return testCalendarEventDetails({
      calendarEventId,
      platforms: [draftPlatform()],
      ...overrides,
    });
  }
});
