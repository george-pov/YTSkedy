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
import {
  testCalendarEventDetails,
  testCalendarEventPlatform,
} from '../testing/calendar-event-details.fixture';
import { CalendarEventPlatformsState } from './calendar-event-platforms.state';
import {
  CalendarEventPlatforms,
  publicationLink,
  wordpressPublicationUrl,
  youtubePublicationUrl,
} from './calendar-event-platforms';

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
    recoverPlatformPublication: Mock<
      (calendarEventId: string, platformId: string) => Observable<void>
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

  it('builds an encoded YouTube Studio URL from a trimmed published resource id', () => {
    const platform = testCalendarEventPlatform({
      externalResourceId: ' broadcast/id?part=one&next=two ',
      platformDeletedUtc: '2030-07-05T08:45:00+00:00',
      canPublish: true,
      canDeletePublication: false,
    });

    expect(youtubePublicationUrl(platform)).toBe(
      'https://studio.youtube.com/video/broadcast%2Fid%3Fpart%3Done%26next%3Dtwo',
    );
  });

  it('rejects ineligible provider, publication status, and resource id values', () => {
    expect(
      youtubePublicationUrl(testCalendarEventPlatform({ platformType: 'WordPress' })),
    ).toBeNull();
    expect(youtubePublicationUrl(testCalendarEventPlatform({ status: 'NotPublished' }))).toBeNull();
    expect(youtubePublicationUrl(testCalendarEventPlatform({ status: 'Publishing' }))).toBeNull();
    expect(youtubePublicationUrl(testCalendarEventPlatform({ status: 'Failed' }))).toBeNull();
    expect(
      youtubePublicationUrl(testCalendarEventPlatform({ externalResourceId: null })),
    ).toBeNull();
    expect(
      youtubePublicationUrl(testCalendarEventPlatform({ externalResourceId: '   ' })),
    ).toBeNull();
  });

  it.each([
    [
      'https://example.com/wp-admin/post.php?post=74&action=edit',
      'https://example.com/wp-admin/post.php?post=74&action=edit',
    ],
    [
      ' http://localhost:8080/blog/wp-admin/post.php?post=74&action=edit ',
      'http://localhost:8080/blog/wp-admin/post.php?post=74&action=edit',
    ],
    [
      'http://127.0.0.1/wp-admin/post.php?post=74&action=edit',
      'http://127.0.0.1/wp-admin/post.php?post=74&action=edit',
    ],
  ])('accepts a safe backend-provided WordPress editor URL', (externalResourceUrl, expected) => {
    expect(
      wordpressPublicationUrl(
        testCalendarEventPlatform({
          platformType: 'WordPress',
          externalResourceId: '74',
          externalResourceUrl,
        }),
      ),
    ).toBe(expected);
  });

  it.each([
    [undefined],
    [null],
    [''],
    ['/wp-admin/post.php?post=74&action=edit'],
    ['https://example.com/posts/74'],
    ['https://user:password@example.com/wp-admin/post.php?post=74&action=edit'],
    ['http://example.com/wp-admin/post.php?post=74&action=edit'],
    ['ftp://example.com/wp-admin/post.php?post=74&action=edit'],
    ['https://example.com/wp-admin/post.php?post=75&action=edit'],
    ['https://example.com/wp-admin/post.php?action=edit&post=74'],
    ['https://example.com/wp-admin/post.php?post=74&action=edit&extra=1'],
    ['https://example.com/wp-admin/post.php?post=74&action=edit#section'],
    [`https://example.com/${'a'.repeat(2048)}`],
  ])(
    'rejects an unsafe, non-editor, mismatched, or missing WordPress URL',
    (externalResourceUrl) => {
      expect(
        wordpressPublicationUrl(
          testCalendarEventPlatform({
            platformType: 'WordPress',
            externalResourceId: '74',
            externalResourceUrl,
          }),
        ),
      ).toBeNull();
    },
  );

  it('does not derive WordPress links from the resource id or wrong status', () => {
    expect(
      wordpressPublicationUrl(
        testCalendarEventPlatform({
          platformType: 'WordPress',
          externalResourceId: '74',
          externalResourceUrl: undefined,
        }),
      ),
    ).toBeNull();
    expect(
      wordpressPublicationUrl(
        testCalendarEventPlatform({
          platformType: 'WordPress',
          status: 'Failed',
          externalResourceId: '74',
          externalResourceUrl: 'https://example.com/wp-admin/post.php?post=74&action=edit',
        }),
      ),
    ).toBeNull();
  });

  it('maps provider-specific publication links and labels', () => {
    expect(
      publicationLink(
        testCalendarEventPlatform({
          platformName: 'Company blog',
          platformType: 'WordPress',
          externalResourceId: '74',
          externalResourceUrl: 'https://example.com/wp-admin/post.php?post=74&action=edit',
        }),
      ),
    ).toEqual({
      href: 'https://example.com/wp-admin/post.php?post=74&action=edit',
      ariaLabel: 'Edit WordPress post for Company blog (opens in a new tab)',
    });
  });

  beforeEach(() => {
    service = {
      getById: vi.fn<(calendarEventId: string) => Observable<CalendarEventDetailsResponse>>(),
      publishPlatform:
        vi.fn<
          (calendarEventId: string, platformId: string) => Observable<PublishPlatformResponse>
        >(),
      deletePlatformPublication:
        vi.fn<(calendarEventId: string, platformId: string) => Observable<CalendarEventPlatform>>(),
      recoverPlatformPublication:
        vi.fn<(calendarEventId: string, platformId: string) => Observable<void>>(),
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
    expect(text).toContain('Not published');
    expect(text).toContain('WordPress');
    expect(text).toContain('Archive site');
    expect(text).toContain('Published');
    expect(platformPublishHosts()).toHaveLength(1);
    expect(platformPreviewHosts()).toHaveLength(2);
    expect(platformDeletePublicationHosts()).toHaveLength(1);
  });

  it('renders a native View link for a published YouTube platform', async () => {
    state.applyEventDetails(
      sampleEvent({
        platforms: [publishedPlatform({ externalResourceId: 'broadcast/id?part=one&next=two' })],
      }),
    );

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const links = platformViewLinks();
    expect(links).toHaveLength(1);
    expect(links[0].getAttribute('href')).toBe(
      'https://studio.youtube.com/video/broadcast%2Fid%3Fpart%3Done%26next%3Dtwo',
    );
    expect(links[0].getAttribute('target')).toBe('_blank');
    expect(links[0].getAttribute('rel')).toBe('noopener noreferrer');
    expect(links[0].textContent?.trim()).toBe('View');
    expect(links[0].getAttribute('aria-label')).toBe(
      'View published stream for Main YouTube channel in YouTube Studio (opens in a new tab)',
    );
  });

  it('renders the View link for orphaned published YouTube history', async () => {
    state.applyEventDetails(
      sampleEvent({
        platforms: [
          publishedPlatform({
            platformDeletedUtc: '2030-07-05T08:45:00+00:00',
            canDeletePublication: false,
          }),
        ],
      }),
    );

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(platformViewLinks()).toHaveLength(1);
  });

  it('renders a native View link for orphaned published WordPress history', async () => {
    state.applyEventDetails(
      sampleEvent({
        platforms: [
          publishedPlatform({
            platformName: 'Company blog',
            platformType: 'WordPress',
            externalResourceId: '74',
            externalResourceUrl: 'https://example.com/wp-admin/post.php?post=74&action=edit',
            platformDeletedUtc: '2030-07-05T08:45:00+00:00',
            canDeletePublication: false,
          }),
        ],
      }),
    );

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const links = platformViewLinks();
    expect(links).toHaveLength(1);
    const link = links[0];
    expect(link.getAttribute('href')).toBe(
      'https://example.com/wp-admin/post.php?post=74&action=edit',
    );
    expect(link.getAttribute('target')).toBe('_blank');
    expect(link.getAttribute('rel')).toBe('noopener noreferrer');
    expect(link.textContent?.trim()).toBe('View');
    expect(link.getAttribute('aria-label')).toBe(
      'Edit WordPress post for Company blog (opens in a new tab)',
    );
  });

  it('does not render View links for failed, missing, or unsafe WordPress publications', async () => {
    state.applyEventDetails(
      sampleEvent({
        platforms: [
          publishedPlatform({ platformType: 'WordPress', externalResourceUrl: undefined }),
          publishedPlatform({
            platformId: 'platform-2',
            status: 'Failed',
            platformType: 'WordPress',
            externalResourceId: '74',
            externalResourceUrl: 'https://example.com/wp-admin/post.php?post=74&action=edit',
          }),
          publishedPlatform({
            platformId: 'platform-3',
            platformType: 'WordPress',
            externalResourceId: '74',
            externalResourceUrl: 'http://example.com/wp-admin/post.php?post=74&action=edit',
          }),
        ],
      }),
    );

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(platformViewLinks()).toHaveLength(0);
  });

  it('keeps the View link available during pending edits and an active page mutation', async () => {
    pendingEventChanges.set(true);
    state.applyEventDetails(sampleEvent({ platforms: [publishedPlatform()] }));

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(platformViewLinks()).toHaveLength(1);

    activePageMutation.set(true);
    fixture.detectChanges();

    expect(state.actionsDisabled()).toBe(true);
    expect(platformViewLinks()).toHaveLength(1);
  });

  it('renders Failed with the backend-enabled publish retry action', async () => {
    state.applyEventDetails(
      sampleEvent({
        platforms: [
          draftPlatform({
            status: 'Failed',
            externalResourceId: 'uncertain-provider-id',
          }),
        ],
      }),
    );

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Failed');
    expect(platformPublishHosts()).toHaveLength(1);
    expect(platformDeletePublicationHosts()).toHaveLength(0);
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

  it('shows recovery only for a backend-eligible publication', async () => {
    state.applyEventDetails(
      sampleEvent({
        platforms: [
          recoverablePlatform(),
          recoverablePlatform({
            platformId: 'platform-2',
            platformName: 'Recent attempt',
            canRecoverPublication: false,
          }),
        ],
      }),
    );

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(platformRecoverPublicationHosts()).toHaveLength(1);
    expect(platformRecoverPublicationButton()?.getAttribute('aria-label')).toBe(
      'Mark publication attempt as failed for Main YouTube channel',
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

  function platformRecoverPublicationHosts(): HTMLElement[] {
    return Array.from(
      fixture.nativeElement.querySelectorAll('.platform-recover-publication-button'),
    ) as HTMLElement[];
  }

  function platformRecoverPublicationButton(): HTMLButtonElement | null {
    return platformRecoverPublicationHosts()[0]?.querySelector('button') ?? null;
  }

  function platformViewLinks(): HTMLAnchorElement[] {
    return Array.from(
      fixture.nativeElement.querySelectorAll('.platform-view-link'),
    ) as HTMLAnchorElement[];
  }

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

  function recoverablePlatform(
    overrides: Partial<CalendarEventPlatform> = {},
  ): CalendarEventPlatform {
    return testCalendarEventPlatform({
      status: 'Publishing',
      externalResourceId: null,
      thumbnailStatus: 'NotConfigured',
      publishedUtc: null,
      canPublish: false,
      canDeletePublication: false,
      canRecoverPublication: true,
      ...overrides,
    });
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
