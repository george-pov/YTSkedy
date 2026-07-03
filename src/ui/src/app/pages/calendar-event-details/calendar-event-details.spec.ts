import { HttpErrorResponse } from '@angular/common/http';
import { provideZonelessChangeDetection, type WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { type FieldTree } from '@angular/forms/signals';
import { MatDateFormats } from '@angular/material/core';
import { provideLuxonDateAdapter } from '@angular/material-luxon-adapter';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { Observable, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, type Mock, vi } from 'vitest';

import {
  CalendarEventDetailsResponse,
  CalendarEventPlatform,
  CalendarEventsService,
  CreateCalendarEventRequest,
  CreateCalendarEventResponse,
  EventPlatformPublishingContent,
  PublishPlatformResponse,
  UpdateCalendarEventRequest,
  UpdateCalendarEventResponse,
} from 'src/app/shared/api/calendar-events/calendar-events-service';
import {
  EventTextFieldsResponse,
  EventTextFieldsService,
} from 'src/app/shared/api/settings/event-text-fields-service';
import { ConfirmationDialogService } from 'src/app/shared/components/confirmation-dialog/confirmation-dialog-service';
import { NotificationService } from 'src/app/shared/notifications/notification-service';
import { CalendarEventDetails } from './calendar-event-details';
import { CalendarEventDetailsModel } from './calendar-event-details.form';

const testDateFormats: MatDateFormats = {
  parse: {
    dateInput: 'yyyy-MM-dd',
    timeInput: 'HH:mm',
  },
  display: {
    dateInput: 'yyyy-MM-dd',
    monthYearLabel: 'LLL yyyy',
    dateA11yLabel: 'DDD',
    monthYearA11yLabel: 'LLLL yyyy',
    timeInput: 'HH:mm',
    timeOptionLabel: 'HH:mm',
  },
};

describe('CalendarEventDetails', () => {
  const calendarEventId = '6f9619ff8b864fb5bdfd4f5c2f2f16a1';

  let fixture: ComponentFixture<CalendarEventDetails>;
  let service: {
    create: Mock<(request: CreateCalendarEventRequest) => Observable<CreateCalendarEventResponse>>;
    getById: Mock<(calendarEventId: string) => Observable<CalendarEventDetailsResponse>>;
    update: Mock<
      (
        calendarEventId: string,
        request: UpdateCalendarEventRequest,
      ) => Observable<UpdateCalendarEventResponse>
    >;
    delete: Mock<(calendarEventId: string) => Observable<void>>;
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
  let eventTextFieldsService: {
    get: Mock<() => Observable<EventTextFieldsResponse>>;
  };
  let confirmation: { confirm: Mock<(data: unknown) => Observable<string | undefined>> };
  let notifications: { showSuccess: Mock<(message: string) => void> };
  let navigations: string[];

  beforeEach(() => {
    service = {
      create:
        vi.fn<(request: CreateCalendarEventRequest) => Observable<CreateCalendarEventResponse>>(),
      getById: vi.fn<(calendarEventId: string) => Observable<CalendarEventDetailsResponse>>(),
      update:
        vi.fn<
          (
            calendarEventId: string,
            request: UpdateCalendarEventRequest,
          ) => Observable<UpdateCalendarEventResponse>
        >(),
      delete: vi.fn<(calendarEventId: string) => Observable<void>>(),
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
    eventTextFieldsService = {
      get: vi.fn<() => Observable<EventTextFieldsResponse>>(),
    };
    eventTextFieldsService.get.mockReturnValue(of(defaultEventTextFields()));
    confirmation = { confirm: vi.fn<(data: unknown) => Observable<string | undefined>>() };
    confirmation.confirm.mockReturnValue(of('delete'));
    notifications = { showSuccess: vi.fn<(message: string) => void>() };
    navigations = [];

    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideLuxonDateAdapter(testDateFormats),
        { provide: CalendarEventsService, useValue: service },
        { provide: EventTextFieldsService, useValue: eventTextFieldsService },
        { provide: ConfirmationDialogService, useValue: confirmation },
        { provide: NotificationService, useValue: notifications },
        { provide: ActivatedRoute, useValue: routeWithId(null) },
      ],
    });

    fixture = TestBed.createComponent(CalendarEventDetails);

    const router = TestBed.inject(Router);
    router.navigateByUrl = ((url: string) => {
      navigations.push(url);
      return Promise.resolve(true);
    }) as Router['navigateByUrl'];

    fixture.detectChanges();
  });

  function api(): {
    model: WritableSignal<CalendarEventDetailsModel>;
    form: FieldTree<CalendarEventDetailsModel>;
  } {
    return fixture.componentInstance as unknown as {
      model: WritableSignal<CalendarEventDetailsModel>;
      form: FieldTree<CalendarEventDetailsModel>;
    };
  }

  function fillValidForm(): void {
    api().model.set({
      start: { date: '2999-01-01', time: '10:00', timeZoneId: 'UTC' },
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
  }

  async function submitForm(): Promise<void> {
    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  function appButtonHosts(): HTMLElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll('app-button')) as HTMLElement[];
  }

  function deleteButtonHost(): HTMLElement | null {
    // The label is 'Delete' or 'Deleting...'; both contain 'Delet' with a
    // capital D, which the lowercase 'delete' icon ligature does not.
    return appButtonHosts().find((host) => (host.textContent ?? '').includes('Delet')) ?? null;
  }

  function deleteButton(): HTMLButtonElement | null {
    return deleteButtonHost()?.querySelector('button') ?? null;
  }

  function cancelButton(): HTMLButtonElement | null {
    const host = appButtonHosts().find((b) => (b.textContent ?? '').includes('Cancel'));
    return host?.querySelector('button') ?? null;
  }

  function saveButton(): HTMLButtonElement {
    return fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
  }

  function eventTextControls(): Array<HTMLInputElement | HTMLTextAreaElement> {
    return Array.from(
      fixture.nativeElement.querySelectorAll('app-input input, app-input textarea'),
    ) as Array<HTMLInputElement | HTMLTextAreaElement>;
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

  it('blocks submit and reveals required errors when the form is empty', async () => {
    await submitForm();

    expect(service.create).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('Start date is required.');
    expect(fixture.nativeElement.textContent).toContain('Title is required.');
    expect(navigations).toEqual([]);
  });

  it('loads current event text fields in create mode before rendering text controls', () => {
    expect(eventTextFieldsService.get).toHaveBeenCalledTimes(1);
    expect(service.getById).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('Title');
    expect(fixture.nativeElement.textContent).toContain('Description');
    expect(fixture.nativeElement.querySelector('textarea')).not.toBeNull();
  });

  it('blocks submit when a text field exceeds its max length', async () => {
    fillValidForm();
    api().form.texts[0].value().value.set('a'.repeat(51));

    await submitForm();

    expect(service.create).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('Title is too long.');
  });

  it('blocks submit when the scheduled start is in the past', async () => {
    fillValidForm();
    api().form.start.date().value.set('2000-01-01');

    await submitForm();

    expect(service.create).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('Start must be in the future.');
  });

  it('posts a contract-correct request and navigates to the list on success', async () => {
    service.create.mockReturnValue(of({ calendarEventId: '20990101T100000Z' }));
    fillValidForm();
    api().form.texts[0].value().value.set('  English title  ');

    await submitForm();

    expect(service.create).toHaveBeenCalledTimes(1);
    expect(service.create).toHaveBeenCalledWith({
      start: { localDateTime: '2999-01-01T10:00:00', timeZoneId: 'UTC' },
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
    expect(navigations).toEqual(['/calendar-events']);
  });

  it('shows a success notification on a successful create', async () => {
    service.create.mockReturnValue(of({ calendarEventId: '20990101T100000Z' }));
    fillValidForm();

    await submitForm();

    expect(notifications.showSuccess).toHaveBeenCalledWith('Calendar event created.');
  });

  it('shows a generic error and does not navigate when the create fails', async () => {
    service.create.mockReturnValue(throwError(() => new Error('boom')));
    fillValidForm();

    await submitForm();

    const alert = fixture.nativeElement.querySelector('[role="alert"]');
    expect(alert).not.toBeNull();
    expect(alert.textContent).toContain('The event could not be saved.');
    expect(navigations).toEqual([]);
  });

  it('navigates to the list when cancel is clicked', async () => {
    const cancelButton = fixture.nativeElement.querySelectorAll('app-button')[0];
    cancelButton.dispatchEvent(new Event('click'));
    await fixture.whenStable();

    expect(navigations).toEqual(['/calendar-events']);
    expect(service.create).not.toHaveBeenCalled();
  });

  it('previews the UTC instant for the entered local start in create mode', () => {
    // The default component is in create mode (no route id).
    api().form.start().value.set({
      date: '2030-07-04',
      time: '10:00',
      timeZoneId: 'America/Vancouver',
    });
    fixture.detectChanges();

    // 10:00 in America/Vancouver (PDT, UTC-7) is 17:00 UTC.
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Scheduled start (UTC)');
    expect(text).toContain('2030-07-04 17:00');
  });

  it('does not show a delete button in create mode', () => {
    expect(deleteButtonHost()).toBeNull();
  });

  describe('edit mode', () => {
    const editId = calendarEventId;

    function createEditComponent(): void {
      eventTextFieldsService.get.mockClear();
      TestBed.resetTestingModule();
      TestBed.configureTestingModule({
        providers: [
          provideZonelessChangeDetection(),
          provideRouter([]),
          provideLuxonDateAdapter(testDateFormats),
          { provide: CalendarEventsService, useValue: service },
          { provide: EventTextFieldsService, useValue: eventTextFieldsService },
          { provide: ConfirmationDialogService, useValue: confirmation },
          { provide: NotificationService, useValue: notifications },
          { provide: ActivatedRoute, useValue: routeWithId(editId) },
        ],
      });

      fixture = TestBed.createComponent(CalendarEventDetails);

      const router = TestBed.inject(Router);
      router.navigateByUrl = ((url: string) => {
        navigations.push(url);
        return Promise.resolve(true);
      }) as Router['navigateByUrl'];

      fixture.detectChanges();
    }

    it('loads the event by id without reloading current text field settings', () => {
      service.getById.mockReturnValue(of(sampleEvent()));

      createEditComponent();

      expect(service.getById).toHaveBeenCalledWith(editId);
      expect(eventTextFieldsService.get).not.toHaveBeenCalled();
      const fieldValues = Array.from(fixture.nativeElement.querySelectorAll('input, textarea')).map(
        (input) => (input as HTMLInputElement | HTMLTextAreaElement).value,
      );
      expect(fieldValues).toContain('English title');
    });

    it('shows the edit heading', () => {
      service.getById.mockReturnValue(of(sampleEvent()));

      createEditComponent();

      expect(fixture.nativeElement.querySelector('h1').textContent).toContain(
        'Edit Calendar Event',
      );
    });

    it('shows the stored UTC instant in edit mode', () => {
      service.getById.mockReturnValue(of(sampleEvent()));

      createEditComponent();

      // Local start is 09:30 Europe/London in July (BST, UTC+1) = 08:30 UTC.
      const text = fixture.nativeElement.textContent;
      expect(text).toContain('Scheduled start (UTC)');
      expect(text).toContain('2030-07-04 08:30');
    });

    it('shows the loaded platform publishing status table', () => {
      service.getById.mockReturnValue(
        of(
          sampleEvent({
            platforms: [
              {
                platformId: 'platform-1',
                platformName: 'Main YouTube channel',
                platformType: 'YouTube',
                status: 'NotPublished',
                externalResourceId: null,
                publishedUtc: null,
                platformDeletedUtc: null,
                canPublish: true,
                canDeletePublication: false,
                canPreviewPublishingContent: true,
              },
              {
                platformId: 'platform-2',
                platformName: 'Archive site',
                platformType: 'WordPress',
                status: 'Published',
                externalResourceId: 'post-123',
                publishedUtc: '2030-07-04T08:45:00+00:00',
                platformDeletedUtc: null,
                canPublish: false,
                canDeletePublication: true,
                canPreviewPublishingContent: true,
              },
            ],
          }),
        ),
      );

      createEditComponent();

      const text = fixture.nativeElement.textContent;
      expect(text).toContain('Platforms');
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
    });

    it('shows an empty platform state when no platforms are returned', () => {
      service.getById.mockReturnValue(of(sampleEvent({ platforms: [] })));

      createEditComponent();

      expect(fixture.nativeElement.textContent).toContain('No platforms found.');
    });

    it('does not show a platform publish action when canPublish is false', () => {
      service.getById.mockReturnValue(
        of(
          sampleEvent({
            platforms: [
              {
                platformId: 'platform-1',
                platformName: 'Main YouTube channel',
                platformType: 'YouTube',
                status: 'Published',
                externalResourceId: 'broadcast-123',
                publishedUtc: '2030-07-04T08:45:00+00:00',
                platformDeletedUtc: null,
                canPublish: false,
                canDeletePublication: false,
                canPreviewPublishingContent: false,
              },
            ],
          }),
        ),
      );

      createEditComponent();

      expect(platformPublishHosts()).toHaveLength(0);
    });

    it('does not show a publishing-content preview action when canPreviewPublishingContent is false', () => {
      service.getById.mockReturnValue(
        of(
          sampleEvent({
            platforms: [
              {
                platformId: 'platform-1',
                platformName: 'Main YouTube channel',
                platformType: 'YouTube',
                status: 'Published',
                externalResourceId: 'broadcast-123',
                publishedUtc: '2030-07-04T08:45:00+00:00',
                platformDeletedUtc: null,
                canPublish: false,
                canDeletePublication: false,
                canPreviewPublishingContent: false,
              },
            ],
          }),
        ),
      );

      createEditComponent();

      expect(platformPreviewHosts()).toHaveLength(0);
    });

    it('loads row-level publishing content without overwriting unsaved text values', async () => {
      service.getById.mockReturnValue(of(sampleEvent()));
      service.getPublishingContent.mockReturnValue(
        of({
          type: 'Preview',
          title: 'Rendered title',
          description: 'Rendered description',
        }),
      );

      createEditComponent();
      api().form.texts[0].value().value.set('Unsaved English title');

      platformPreviewHosts()[0].dispatchEvent(new Event('click'));
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(service.getPublishingContent).toHaveBeenCalledWith(editId, 'platform-1');
      expect(fixture.nativeElement.textContent).toContain('Main YouTube channel');
      expect(fixture.nativeElement.textContent).toContain('Preview');
      expect(fixture.nativeElement.textContent).toContain('Rendered title');
      expect(fixture.nativeElement.textContent).toContain('Rendered description');
      expect(api().model().texts[0].value).toBe('Unsaved English title');
    });

    it('shows published snapshot publishing content on demand', async () => {
      service.getById.mockReturnValue(of(sampleEvent({ platforms: [publishedPlatform()] })));
      service.getPublishingContent.mockReturnValue(
        of({
          type: 'Snapshot',
          title: 'Published title',
          description: null,
        }),
      );

      createEditComponent();

      platformPreviewHosts()[0].dispatchEvent(new Event('click'));
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(service.getPublishingContent).toHaveBeenCalledWith(editId, 'platform-1');
      expect(fixture.nativeElement.textContent).toContain('Snapshot');
      expect(fixture.nativeElement.textContent).toContain('Published title');
      expect(fixture.nativeElement.textContent).toContain('No description');
    });

    it('shows a row preview conflict message when publishing content cannot be loaded', async () => {
      service.getById.mockReturnValue(of(sampleEvent()));
      service.getPublishingContent.mockReturnValue(
        throwError(() => new HttpErrorResponse({ status: 409 })),
      );

      createEditComponent();

      platformPreviewHosts()[0].dispatchEvent(new Event('click'));
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.textContent).toContain(
        'Publishing content cannot be previewed. Reload the page and try again.',
      );
      expect(navigations).toEqual([]);
    });

    it('disables event and platform mutations while publishing content preview is in flight', () => {
      service.getById.mockReturnValue(
        of(
          sampleEvent({
            platforms: [
              sampleEvent().platforms[0],
              publishedPlatform({
                platformId: 'platform-2',
                platformName: 'Archive site',
                platformType: 'WordPress',
              }),
            ],
          }),
        ),
      );
      service.getPublishingContent.mockReturnValue(new Subject<EventPlatformPublishingContent>());

      createEditComponent();

      platformPreviewHosts()[0].dispatchEvent(new Event('click'));
      fixture.detectChanges();

      const save = fixture.nativeElement.querySelector(
        'button[type="submit"]',
      ) as HTMLButtonElement;
      expect(save.disabled).toBe(true);
      expect(deleteButton()!.disabled).toBe(true);
      expect(cancelButton()!.disabled).toBe(true);
      expect(platformPreviewButton()!.disabled).toBe(true);
      expect(platformPublishButton()!.disabled).toBe(true);
      expect(platformDeletePublicationButton()!.disabled).toBe(true);
    });

    it('refreshes event details after publish and locks event update and delete from API flags', async () => {
      service.getById
        .mockReturnValueOnce(of(sampleEvent()))
        .mockReturnValueOnce(
          of(
            sampleEvent({
              canUpdate: false,
              canDelete: false,
              platforms: [publishedPlatform()],
            }),
          ),
        );
      service.publishPlatform.mockReturnValue(
        of(publishedPlatform()),
      );

      createEditComponent();
      api().form.texts[0].value().value.set('Unsaved English title');

      platformPublishHosts()[0].dispatchEvent(new Event('click'));
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(service.publishPlatform).toHaveBeenCalledWith(editId, 'platform-1');
      expect(service.getById).toHaveBeenCalledTimes(2);
      expect(fixture.nativeElement.textContent).toContain('Published');
      expect(platformPublishHosts()).toHaveLength(0);
      expect(platformDeletePublicationButton()).not.toBeNull();
      expect(saveButton().disabled).toBe(true);
      expect(deleteButton()!.disabled).toBe(true);
      expect(api().form.start().disabled()).toBe(true);
      expect(eventTextControls().every((control) => control.disabled)).toBe(true);
      expect(api().model().texts[0].value).toBe('English title');
      expect(notifications.showSuccess).toHaveBeenCalledWith('Calendar event published.');
    });

    it('publishes a WordPress platform row through the existing table action', async () => {
      const wordpressDraft: CalendarEventPlatform = {
        platformId: 'wordpress-platform',
        platformName: 'Company blog',
        platformType: 'WordPress',
        status: 'NotPublished',
        externalResourceId: null,
        publishedUtc: null,
        platformDeletedUtc: null,
        canPublish: true,
        canDeletePublication: false,
        canPreviewPublishingContent: true,
      };
      const wordpressPublished: CalendarEventPlatform = {
        ...wordpressDraft,
        status: 'Published',
        externalResourceId: '123',
        publishedUtc: '2030-07-04T08:45:00+00:00',
        canPublish: false,
        canDeletePublication: true,
      };
      service.getById
        .mockReturnValueOnce(of(sampleEvent({ platforms: [wordpressDraft] })))
        .mockReturnValueOnce(
          of(
            sampleEvent({
              canUpdate: false,
              canDelete: false,
              platforms: [wordpressPublished],
            }),
          ),
        );
      service.publishPlatform.mockReturnValue(
        of(wordpressPublished),
      );

      createEditComponent();

      const text = fixture.nativeElement.textContent;
      expect(text).toContain('WordPress');
      expect(text).toContain('Company blog');
      expect(platformPublishHosts()).toHaveLength(1);

      platformPublishHosts()[0].dispatchEvent(new Event('click'));
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(service.publishPlatform).toHaveBeenCalledWith(editId, 'wordpress-platform');
      expect(fixture.nativeElement.textContent).toContain('Published');
    });

    it('shows a platform publish error and stays on the page when publish fails', async () => {
      service.getById.mockReturnValue(of(sampleEvent()));
      service.publishPlatform.mockReturnValue(
        throwError(() => new HttpErrorResponse({ status: 502 })),
      );

      createEditComponent();

      platformPublishHosts()[0].dispatchEvent(new Event('click'));
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.textContent).toContain(
        'The platform could not publish this event. Try again later.',
      );
      expect(navigations).toEqual([]);
    });

    it('does not show a platform publication delete action when canDeletePublication is false', () => {
      service.getById.mockReturnValue(
        of(
          sampleEvent({
            platforms: [publishedPlatform({ canDeletePublication: false })],
          }),
        ),
      );

      createEditComponent();

      expect(platformDeletePublicationHosts()).toHaveLength(0);
    });

    it('does not delete a platform publication when confirmation is cancelled', async () => {
      confirmation.confirm.mockReturnValue(of('cancel'));
      service.getById.mockReturnValue(of(sampleEvent({ platforms: [publishedPlatform()] })));

      createEditComponent();

      platformDeletePublicationHosts()[0].dispatchEvent(new Event('click'));
      fixture.detectChanges();
      await fixture.whenStable();

      expect(confirmation.confirm).toHaveBeenCalledTimes(1);
      expect(service.deletePlatformPublication).not.toHaveBeenCalled();
    });

    it('deletes a platform publication after confirmation', async () => {
      service.getById.mockReturnValue(of(sampleEvent({ platforms: [publishedPlatform()] })));
      service.deletePlatformPublication.mockReturnValue(new Subject<CalendarEventPlatform>());

      createEditComponent();

      expect(platformDeletePublicationButton()?.getAttribute('aria-label')).toBe(
        'Delete publication for Main YouTube channel',
      );

      platformDeletePublicationHosts()[0].dispatchEvent(new Event('click'));
      fixture.detectChanges();
      await fixture.whenStable();

      expect(confirmation.confirm).toHaveBeenCalledWith(
        expect.objectContaining({
          kind: 'warning',
          title: 'Delete publication for Main YouTube channel?',
        }),
      );
      expect(service.deletePlatformPublication).toHaveBeenCalledWith(editId, 'platform-1');
    });

    it('refreshes event details after publication delete and unlocks event update and delete from API flags', async () => {
      const unpublishedPlatform: CalendarEventPlatform = {
        platformId: 'platform-1',
        platformName: 'Main YouTube channel',
        platformType: 'YouTube',
        status: 'NotPublished',
        externalResourceId: null,
        publishedUtc: null,
        platformDeletedUtc: null,
        canPublish: true,
        canDeletePublication: false,
        canPreviewPublishingContent: true,
      };
      service.getById
        .mockReturnValueOnce(
          of(
            sampleEvent({
              canUpdate: false,
              canDelete: false,
              platforms: [publishedPlatform()],
            }),
          ),
        )
        .mockReturnValueOnce(
          of(
            sampleEvent({
              canUpdate: true,
              canDelete: true,
              platforms: [unpublishedPlatform],
            }),
          ),
        );
      service.deletePlatformPublication.mockReturnValue(
        of(unpublishedPlatform),
      );

      createEditComponent();
      api().form.texts[0].value().value.set('Unsaved English title');

      platformDeletePublicationHosts()[0].dispatchEvent(new Event('click'));
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.textContent).toContain('NotPublished');
      expect(service.getById).toHaveBeenCalledTimes(2);
      expect(platformDeletePublicationHosts()).toHaveLength(0);
      expect(platformPublishHosts()).toHaveLength(1);
      expect(saveButton().disabled).toBe(false);
      expect(deleteButton()!.disabled).toBe(false);
      expect(api().form.start().disabled()).toBe(false);
      expect(eventTextControls().every((control) => !control.disabled)).toBe(true);
      expect(api().model().texts[0].value).toBe('English title');
      expect(notifications.showSuccess).toHaveBeenCalledWith('Platform publication deleted.');
    });

    it('disables save, event delete, cancel, publish, and delete publication while deleting a publication', () => {
      service.getById.mockReturnValue(
        of(
          sampleEvent({
            platforms: [
              publishedPlatform(),
              {
                platformId: 'platform-2',
                platformName: 'Archive site',
                platformType: 'WordPress',
                status: 'NotPublished',
                externalResourceId: null,
                publishedUtc: null,
                platformDeletedUtc: null,
                canPublish: true,
                canDeletePublication: false,
                canPreviewPublishingContent: true,
              },
            ],
          }),
        ),
      );
      service.deletePlatformPublication.mockReturnValue(new Subject<CalendarEventPlatform>());

      createEditComponent();

      platformDeletePublicationHosts()[0].dispatchEvent(new Event('click'));
      fixture.detectChanges();

      const save = fixture.nativeElement.querySelector(
        'button[type="submit"]',
      ) as HTMLButtonElement;
      expect(save.disabled).toBe(true);
      expect(deleteButton()!.disabled).toBe(true);
      expect(cancelButton()!.disabled).toBe(true);
      expect(platformPreviewButton()!.disabled).toBe(true);
      expect(platformPublishButton()!.disabled).toBe(true);
      expect(platformDeletePublicationButton()!.disabled).toBe(true);
    });

    it('shows a stale-state message when platform publication delete returns 409', async () => {
      service.getById.mockReturnValue(of(sampleEvent({ platforms: [publishedPlatform()] })));
      service.deletePlatformPublication.mockReturnValue(
        throwError(() => new HttpErrorResponse({ status: 409 })),
      );

      createEditComponent();

      platformDeletePublicationHosts()[0].dispatchEvent(new Event('click'));
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.textContent).toContain(
        'The publication can no longer be deleted. Reload the page and try again.',
      );
      expect(navigations).toEqual([]);
    });

    it('shows a provider cleanup message when platform publication delete returns 502', async () => {
      service.getById.mockReturnValue(of(sampleEvent({ platforms: [publishedPlatform()] })));
      service.deletePlatformPublication.mockReturnValue(
        throwError(() => new HttpErrorResponse({ status: 502 })),
      );

      createEditComponent();

      platformDeletePublicationHosts()[0].dispatchEvent(new Event('click'));
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.textContent).toContain(
        'The provider publication could not be deleted. Try again later.',
      );
      expect(navigations).toEqual([]);
    });

    it('disables save, delete, and the clicked platform action while publishing', () => {
      service.getById.mockReturnValue(of(sampleEvent()));
      service.publishPlatform.mockReturnValue(new Subject<PublishPlatformResponse>());

      createEditComponent();

      platformPublishHosts()[0].dispatchEvent(new Event('click'));
      fixture.detectChanges();

      const save = fixture.nativeElement.querySelector(
        'button[type="submit"]',
      ) as HTMLButtonElement;
      expect(save.disabled).toBe(true);
      expect(deleteButton()!.disabled).toBe(true);
      expect(platformPublishButton()!.disabled).toBe(true);
    });

    it('enables scheduled start controls in edit mode when API canUpdate is true', () => {
      service.getById.mockReturnValue(of(sampleEvent()));

      createEditComponent();

      expect(api().form.start().disabled()).toBe(false);
      expect(api().form.start.date().disabled()).toBe(false);
      expect(api().form.start.time().disabled()).toBe(false);
      expect(api().form.start.timeZoneId().disabled()).toBe(false);
    });

    it('disables scheduled start controls in edit mode when API canUpdate is false', () => {
      service.getById.mockReturnValue(
        of(
          sampleEvent({
            canUpdate: false,
            canDelete: false,
            platforms: [publishedPlatform()],
          }),
        ),
      );

      createEditComponent();

      expect(api().form.start().disabled()).toBe(true);
      expect(api().form.start.date().disabled()).toBe(true);
      expect(api().form.start.time().disabled()).toBe(true);
      expect(api().form.start.timeZoneId().disabled()).toBe(true);
    });

    it('previews the edited scheduled start UTC in edit mode when API canUpdate is true', () => {
      service.getById.mockReturnValue(of(sampleEvent()));

      createEditComponent();

      api().form.start().value.set({
        date: '2030-07-05',
        time: '10:00',
        timeZoneId: 'America/Vancouver',
      });
      fixture.detectChanges();

      expect(fixture.nativeElement.textContent).toContain('Scheduled start (UTC)');
      expect(fixture.nativeElement.textContent).toContain('2030-07-05 17:00');
    });

    it('keeps Save enabled and updates start and text values on submit', async () => {
      service.getById.mockReturnValue(of(sampleEvent()));
      service.update.mockReturnValue(of({ calendarEventId: editId }));

      createEditComponent();

      const save = fixture.nativeElement.querySelector(
        'button[type="submit"]',
      ) as HTMLButtonElement;
      expect(save.disabled).toBe(false);

      api().form.texts[0].value().value.set('  Updated English title  ');
      api().form.start().value.set({
        date: '2030-07-05',
        time: '10:00',
        timeZoneId: 'America/Vancouver',
      });

      fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));
      fixture.detectChanges();
      await fixture.whenStable();

      expect(service.create).not.toHaveBeenCalled();
      expect(service.update).toHaveBeenCalledTimes(1);
      expect(service.update).toHaveBeenCalledWith(
        editId,
        expect.objectContaining({
          start: {
            localDateTime: '2030-07-05T10:00:00',
            timeZoneId: 'America/Vancouver',
          },
          texts: expect.arrayContaining([
            {
              fieldKey: 'text1',
              value: 'Updated English title',
            },
          ]),
        }),
      );
      expect(notifications.showSuccess).toHaveBeenCalledWith('Calendar event updated.');
      expect(navigations).toEqual(['/calendar-events']);
    });

    it('disables event text save and delete when API canUpdate and canDelete are false', () => {
      service.getById.mockReturnValue(
        of(
          sampleEvent({
            canUpdate: false,
            canDelete: false,
            platforms: [publishedPlatform()],
          }),
        ),
      );

      createEditComponent();

      expect(saveButton().disabled).toBe(true);
      expect(deleteButton()!.disabled).toBe(true);
      expect(api().form.start().disabled()).toBe(true);
      expect(eventTextControls().every((control) => control.disabled)).toBe(true);
      expect(fixture.nativeElement.textContent).toContain(
        'Delete platform publications before changing this event or deleting it.',
      );
    });

    it('does not call update when API canUpdate is false', async () => {
      service.getById.mockReturnValue(
        of(
          sampleEvent({
            canUpdate: false,
            canDelete: false,
            platforms: [publishedPlatform()],
          }),
        ),
      );

      createEditComponent();

      fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));
      fixture.detectChanges();
      await fixture.whenStable();

      expect(service.update).not.toHaveBeenCalled();
      expect(navigations).toEqual([]);
    });

    it('shows a save error and does not navigate when the update fails', async () => {
      service.getById.mockReturnValue(of(sampleEvent()));
      service.update.mockReturnValue(throwError(() => new Error('boom')));

      createEditComponent();

      fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));
      fixture.detectChanges();
      await fixture.whenStable();

      const alert = fixture.nativeElement.querySelector('[role="alert"]');
      expect(alert).not.toBeNull();
      expect(alert.textContent).toContain('The event could not be saved.');
      expect(navigations).toEqual([]);
    });

    it('maps an update 409 to a reload message and stays on the page', async () => {
      service.getById.mockReturnValue(of(sampleEvent()));
      service.update.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 409 })));

      createEditComponent();

      fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.textContent).toContain(
        'The event can no longer be updated. Reload the page and try again.',
      );
      expect(navigations).toEqual([]);
    });

    it('shows an error and no form when the event cannot be loaded', () => {
      service.getById.mockReturnValue(throwError(() => new Error('boom')));

      createEditComponent();

      expect(fixture.nativeElement.textContent).toContain(
        'The calendar event could not be loaded.',
      );
      expect(fixture.nativeElement.querySelector('form')).toBeNull();
    });

    it('shows a progress bar while the event is loading', () => {
      vi.useFakeTimers();
      try {
        service.getById.mockReturnValue(new Subject<CalendarEventDetailsResponse>());

        createEditComponent();

        // The progress bar has a short appear delay so quick loads do not flash
        // it; advance past the delay before asserting it is shown.
        vi.advanceTimersByTime(200);
        fixture.detectChanges();

        expect(fixture.nativeElement.querySelector('app-progress-bar')).not.toBeNull();
        expect(fixture.nativeElement.querySelector('form')).toBeNull();
      } finally {
        vi.useRealTimers();
      }
    });

    it('enables delete in edit mode when API canDelete is true', () => {
      service.getById.mockReturnValue(of(sampleEvent()));

      createEditComponent();

      expect(deleteButton()).not.toBeNull();
      expect(deleteButton()!.disabled).toBe(false);
    });

    it('does not call delete when API canDelete is false', async () => {
      service.getById.mockReturnValue(
        of(
          sampleEvent({
            canUpdate: false,
            canDelete: false,
            platforms: [publishedPlatform()],
          }),
        ),
      );

      createEditComponent();

      deleteButtonHost()!.dispatchEvent(new Event('click'));
      fixture.detectChanges();
      await fixture.whenStable();

      expect(service.delete).not.toHaveBeenCalled();
      expect(navigations).toEqual([]);
    });

    it('deletes a draft, notifies, and navigates to the list', async () => {
      service.getById.mockReturnValue(of(sampleEvent()));
      service.delete.mockReturnValue(of<void>(undefined));

      createEditComponent();

      deleteButtonHost()!.dispatchEvent(new Event('click'));
      fixture.detectChanges();
      await fixture.whenStable();

      expect(service.delete).toHaveBeenCalledWith(editId);
      expect(notifications.showSuccess).toHaveBeenCalledWith('Calendar event deleted.');
      expect(navigations).toEqual(['/calendar-events']);
    });

    it('deletes through the backend without event-level publish state', async () => {
      service.getById.mockReturnValue(of(sampleEvent()));
      service.delete.mockReturnValue(of<void>(undefined));

      createEditComponent();

      deleteButtonHost()!.dispatchEvent(new Event('click'));
      fixture.detectChanges();
      await fixture.whenStable();

      expect(service.delete).toHaveBeenCalledWith(editId);
      expect(notifications.showSuccess).toHaveBeenCalledWith('Calendar event deleted.');
      expect(navigations).toEqual(['/calendar-events']);
    });

    it('treats a 404 as already deleted and navigates to the list', async () => {
      service.getById.mockReturnValue(of(sampleEvent()));
      service.delete.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 404 })));

      createEditComponent();

      deleteButtonHost()!.dispatchEvent(new Event('click'));
      fixture.detectChanges();
      await fixture.whenStable();

      expect(notifications.showSuccess).toHaveBeenCalledWith('Calendar event no longer exists.');
      expect(navigations).toEqual(['/calendar-events']);
    });

    it('shows a conflict message and stays on the page when platform publications exist', async () => {
      service.getById.mockReturnValue(of(sampleEvent()));
      service.delete.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 409 })));

      createEditComponent();

      deleteButtonHost()!.dispatchEvent(new Event('click'));
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.textContent).toContain(
        'Delete platform publications before deleting this event.',
      );
      expect(navigations).toEqual([]);
      expect(notifications.showSuccess).not.toHaveBeenCalled();
    });

    it('shows a generic delete error and stays on the page on 502', async () => {
      service.getById.mockReturnValue(of(sampleEvent()));
      service.delete.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 502 })));

      createEditComponent();

      deleteButtonHost()!.dispatchEvent(new Event('click'));
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.textContent).toContain(
        'The event could not be deleted. Check your connection and try again.',
      );
      expect(navigations).toEqual([]);
    });

    it('shows a generic delete error for other failures', async () => {
      service.getById.mockReturnValue(of(sampleEvent()));
      service.delete.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 500 })));

      createEditComponent();

      deleteButtonHost()!.dispatchEvent(new Event('click'));
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.textContent).toContain(
        'The event could not be deleted. Check your connection and try again.',
      );
      expect(navigations).toEqual([]);
    });

    it('disables save while a delete is in flight', () => {
      service.getById.mockReturnValue(of(sampleEvent()));
      service.delete.mockReturnValue(new Subject<void>());

      createEditComponent();

      deleteButtonHost()!.dispatchEvent(new Event('click'));
      fixture.detectChanges();

      const save = fixture.nativeElement.querySelector(
        'button[type="submit"]',
      ) as HTMLButtonElement;
      expect(save.disabled).toBe(true);
    });

    it('disables delete while a save is in flight', () => {
      service.getById.mockReturnValue(of(sampleEvent()));
      service.update.mockReturnValue(new Subject<UpdateCalendarEventResponse>());

      createEditComponent();

      fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));
      fixture.detectChanges();

      expect(deleteButton()!.disabled).toBe(true);
    });

    it('disables cancel while a delete is in flight', () => {
      service.getById.mockReturnValue(of(sampleEvent()));
      service.delete.mockReturnValue(new Subject<void>());

      createEditComponent();

      deleteButtonHost()!.dispatchEvent(new Event('click'));
      fixture.detectChanges();

      expect(cancelButton()!.disabled).toBe(true);
    });

    it('shows a Deleting... label while the delete is in flight', () => {
      service.getById.mockReturnValue(of(sampleEvent()));
      service.delete.mockReturnValue(new Subject<void>());

      createEditComponent();

      deleteButtonHost()!.dispatchEvent(new Event('click'));
      fixture.detectChanges();

      expect(deleteButtonHost()!.textContent).toContain('Deleting...');
    });
  });

  function routeWithId(calendarEventId: string | null): ActivatedRoute {
    return {
      snapshot: {
        paramMap: convertToParamMap(calendarEventId === null ? {} : { calendarEventId }),
      },
    } as ActivatedRoute;
  }

  function publishedPlatform(
    overrides: Partial<CalendarEventPlatform> = {},
  ): CalendarEventPlatform {
    return {
      platformId: 'platform-1',
      platformName: 'Main YouTube channel',
      platformType: 'YouTube',
      status: 'Published',
      externalResourceId: 'broadcast-123',
      publishedUtc: '2030-07-04T08:45:00+00:00',
      platformDeletedUtc: null,
      canPublish: false,
      canDeletePublication: true,
      canPreviewPublishingContent: true,
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
        {
          platformId: 'platform-1',
          platformName: 'Main YouTube channel',
          platformType: 'YouTube',
          status: 'NotPublished',
          externalResourceId: null,
          publishedUtc: null,
          platformDeletedUtc: null,
          canPublish: true,
          canDeletePublication: false,
          canPreviewPublishingContent: true,
        },
      ],
      ...overrides,
    };
  }

  function defaultEventTextFields(): EventTextFieldsResponse {
    return {
      fields: [
        {
          fieldKey: 'text1',
          label: 'Title',
          type: 'ShortText',
          maxLength: 50,
        },
        {
          fieldKey: 'text2',
          label: 'Description',
          type: 'LongText',
          maxLength: 2500,
        },
      ],
    };
  }
});
