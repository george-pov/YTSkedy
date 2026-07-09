import { provideZonelessChangeDetection, signal, type DestroyRef } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Observable, of, Subject } from 'rxjs';
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
import { CalendarEventPlatformsState } from './calendar-event-platforms.state';
import { CalendarEventPlatforms } from './calendar-event-platforms';

describe('CalendarEventPlatforms', () => {
  const calendarEventId = 'event-1';

  let fixture: ComponentFixture<CalendarEventPlatforms>;
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
    state = createState();

    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
    fixture = TestBed.createComponent(CalendarEventPlatforms);
    fixture.componentRef.setInput('state', state);
  });

  it('renders the platform table and row actions from backend-provided flags', async () => {
    state.applyEventDetails(
      sampleEvent({
        platforms: [
          draftPlatform(),
          publishedPlatform({
            platformId: 'platform-2',
            platformName: 'Archive site',
            platformType: 'WordPress',
          }),
        ],
      }),
    );

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Type');
    expect(text).toContain('Name');
    expect(text).toContain('Status');
    expect(text).toContain('YouTube');
    expect(text).toContain('Main YouTube channel');
    expect(text).toContain('NotPublished');
    expect(text).toContain('WordPress');
    expect(text).toContain('Archive site');
    expect(text).toContain('Published');
    expect(platformPublishHosts()).toHaveLength(1);
    expect(platformPreviewHosts()).toHaveLength(2);
    expect(platformDeletePublicationHosts()).toHaveLength(1);
  });

  it('shows an empty platform state when no platforms are returned', async () => {
    state.applyEventDetails(sampleEvent({ platforms: [] }));

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No platforms found.');
  });

  it('hides actions when backend flags are false', async () => {
    state.applyEventDetails(
      sampleEvent({
        platforms: [
          publishedPlatform({
            canPublish: false,
            canDeletePublication: false,
            canPreviewPublishingContent: false,
          }),
        ],
      }),
    );

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(platformPublishHosts()).toHaveLength(0);
    expect(platformPreviewHosts()).toHaveLength(0);
    expect(platformDeletePublicationHosts()).toHaveLength(0);
  });

  it('shows a thumbnail failure warning without a retry action', async () => {
    state.applyEventDetails(
      sampleEvent({ platforms: [publishedPlatform({ thumbnailStatus: 'Failed' })] }),
    );

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain(
      'YouTube broadcast was created, but the thumbnail was not applied. Update it in YouTube Studio.',
    );
    expect(text).not.toContain('Retry');
  });

  it('renders publishing-content preview and stored-values notice', async () => {
    pendingEventChanges.set(true);
    state.applyEventDetails(sampleEvent());
    service.getPublishingContent.mockReturnValue(
      of({
        type: 'Preview',
        title: 'Rendered title',
        description: 'Rendered description',
      }),
    );
    fixture.detectChanges();
    await fixture.whenStable();

    platformPreviewHosts()[0].dispatchEvent(new Event('click'));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(service.getPublishingContent).toHaveBeenCalledWith(calendarEventId, 'platform-1');
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Main YouTube channel');
    expect(text).toContain('Preview');
    expect(text).toContain('Rendered title');
    expect(text).toContain('Rendered description');
    expect(text).toContain(
      'Preview uses stored event values. Unsaved event changes are not included.',
    );
  });

  it('renders No description for snapshot content without a description', async () => {
    state.applyEventDetails(sampleEvent({ platforms: [publishedPlatform()] }));
    service.getPublishingContent.mockReturnValue(
      of({
        type: 'Snapshot',
        title: 'Published title',
        description: null,
      }),
    );
    fixture.detectChanges();
    await fixture.whenStable();

    platformPreviewHosts()[0].dispatchEvent(new Event('click'));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Snapshot');
    expect(fixture.nativeElement.textContent).toContain('Published title');
    expect(fixture.nativeElement.textContent).toContain('No description');
  });

  it('shows pending-change blocking copy from platform publish', async () => {
    pendingEventChanges.set(true);
    state.applyEventDetails(sampleEvent());
    fixture.detectChanges();
    await fixture.whenStable();

    platformPublishHosts()[0].dispatchEvent(new Event('click'));
    fixture.detectChanges();

    expect(service.publishPlatform).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain(
      'Save or discard event changes before publishing.',
    );
  });

  it('disables platform action buttons while a platform mutation is active', async () => {
    const preview = new Subject<EventPlatformPublishingContent>();
    state.applyEventDetails(
      sampleEvent({
        platforms: [
          draftPlatform(),
          publishedPlatform({
            platformId: 'platform-2',
            platformName: 'Archive site',
            platformType: 'WordPress',
          }),
        ],
      }),
    );
    service.getPublishingContent.mockReturnValue(preview.asObservable());
    fixture.detectChanges();
    await fixture.whenStable();

    platformPreviewHosts()[0].dispatchEvent(new Event('click'));
    fixture.detectChanges();

    expect(platformPreviewButton()!.disabled).toBe(true);
    expect(platformPublishButton()!.disabled).toBe(true);
    expect(platformDeletePublicationButton()!.disabled).toBe(true);
  });

  it('disables and ignores platform actions while a page mutation is active', async () => {
    activePageMutation.set(true);
    state.applyEventDetails(
      sampleEvent({
        platforms: [
          draftPlatform(),
          publishedPlatform({
            platformId: 'platform-2',
            platformName: 'Archive site',
            platformType: 'WordPress',
          }),
        ],
      }),
    );
    fixture.detectChanges();
    await fixture.whenStable();

    expect(platformPreviewButton()!.disabled).toBe(true);
    expect(platformPublishButton()!.disabled).toBe(true);
    expect(platformDeletePublicationButton()!.disabled).toBe(true);

    platformPreviewHosts()[0].dispatchEvent(new Event('click'));
    platformPublishHosts()[0].dispatchEvent(new Event('click'));
    platformDeletePublicationHosts()[0].dispatchEvent(new Event('click'));

    expect(service.getPublishingContent).not.toHaveBeenCalled();
    expect(service.publishPlatform).not.toHaveBeenCalled();
    expect(confirmation.confirm).not.toHaveBeenCalled();
    expect(service.deletePlatformPublication).not.toHaveBeenCalled();
  });

  it('exposes the publication-delete action label', async () => {
    state.applyEventDetails(sampleEvent({ platforms: [publishedPlatform()] }));

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(platformDeletePublicationButton()?.getAttribute('aria-label')).toBe(
      'Delete publication for Main YouTube channel',
    );
  });

  function createState(): CalendarEventPlatformsState {
    return new CalendarEventPlatformsState(
      service as unknown as CalendarEventsService,
      confirmation as unknown as ConfirmationDialogService,
      notifications as unknown as NotificationService,
      calendarEventId,
      destroyRef,
      () => activePageMutation(),
      () => pendingEventChanges(),
      (event) => state.applyEventDetails(event),
    );
  }

  function platformPublishHosts(): HTMLElement[] {
    return Array.from(
      fixture.nativeElement.querySelectorAll('.platform-publish-button'),
    ) as HTMLElement[];
  }

  function platformPublishButton(): HTMLButtonElement | null {
    return platformPublishHosts()[0]?.querySelector('button') ?? null;
  }

  function platformPreviewHosts(): HTMLElement[] {
    return Array.from(
      fixture.nativeElement.querySelectorAll('.platform-preview-button'),
    ) as HTMLElement[];
  }

  function platformPreviewButton(): HTMLButtonElement | null {
    return platformPreviewHosts()[0]?.querySelector('button') ?? null;
  }

  function platformDeletePublicationHosts(): HTMLElement[] {
    return Array.from(
      fixture.nativeElement.querySelectorAll('.platform-delete-publication-button'),
    ) as HTMLElement[];
  }

  function platformDeletePublicationButton(): HTMLButtonElement | null {
    return platformDeletePublicationHosts()[0]?.querySelector('button') ?? null;
  }

  function draftPlatform(
    overrides: Partial<CalendarEventPlatform> = {},
  ): CalendarEventPlatform {
    return {
      platformId: 'platform-1',
      platformName: 'Main YouTube channel',
      platformType: 'YouTube',
      status: 'NotPublished',
      externalResourceId: null,
      thumbnailStatus: 'NotConfigured',
      publishedUtc: null,
      platformDeletedUtc: null,
      canPublish: true,
      canDeletePublication: false,
      canPreviewPublishingContent: true,
      ...overrides,
    };
  }

  function publishedPlatform(
    overrides: Partial<CalendarEventPlatform> = {},
  ): CalendarEventPlatform {
    return {
      ...draftPlatform({
        status: 'Published',
        externalResourceId: 'broadcast-123',
        thumbnailStatus: 'Applied',
        publishedUtc: '2030-07-04T08:45:00+00:00',
        canPublish: false,
        canDeletePublication: true,
      }),
      ...overrides,
    };
  }

  function sampleEvent(
    overrides: Partial<CalendarEventDetailsResponse> = {},
  ): CalendarEventDetailsResponse {
    return {
      calendarEventId,
      start: {
        localDateTime: '2030-07-04T09:30:00',
        timeZoneId: 'Europe/London',
      },
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
      ],
      platforms: [draftPlatform()],
      ...overrides,
    };
  }
});
