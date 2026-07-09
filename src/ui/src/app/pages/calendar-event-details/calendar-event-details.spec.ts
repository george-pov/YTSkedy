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
  CalendarEventThumbnail,
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
import {
  resolveCanDeactivate,
  submitForm as submitFixtureForm,
  textContent,
} from 'src/app/testing/dom-test-helpers';
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
    uploadThumbnail: Mock<
      (calendarEventId: string, thumbnail: File) => Observable<CalendarEventThumbnail>
    >;
    getThumbnail: Mock<(calendarEventId: string) => Observable<Blob>>;
    deleteThumbnail: Mock<(calendarEventId: string) => Observable<void>>;
  };
  let eventTextFieldsService: {
    get: Mock<() => Observable<EventTextFieldsResponse>>;
  };
  let confirmation: { confirm: Mock<(data: unknown) => Observable<string | undefined>> };
  let notifications: { showSuccess: Mock<(message: string) => void> };
  let navigations: string[];
  let navigationStates: Array<Record<string, unknown> | undefined>;

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
      uploadThumbnail:
        vi.fn<(calendarEventId: string, thumbnail: File) => Observable<CalendarEventThumbnail>>(),
      getThumbnail: vi.fn<(calendarEventId: string) => Observable<Blob>>(),
      deleteThumbnail: vi.fn<(calendarEventId: string) => Observable<void>>(),
    };
    eventTextFieldsService = {
      get: vi.fn<() => Observable<EventTextFieldsResponse>>(),
    };
    eventTextFieldsService.get.mockReturnValue(of(defaultEventTextFields()));
    confirmation = { confirm: vi.fn<(data: unknown) => Observable<string | undefined>>() };
    confirmation.confirm.mockReturnValue(of('delete'));
    notifications = { showSuccess: vi.fn<(message: string) => void>() };
    navigations = [];
    navigationStates = [];
    let objectUrlIndex = 0;
    Object.defineProperty(URL, 'createObjectURL', {
      configurable: true,
      value: vi.fn(() => `blob:thumbnail-${++objectUrlIndex}`),
    });
    Object.defineProperty(URL, 'revokeObjectURL', {
      configurable: true,
      value: vi.fn(),
    });

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
    router.navigateByUrl = ((url: string, extras?: { state?: Record<string, unknown> }) => {
      navigations.push(url);
      navigationStates.push(extras?.state);
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
    await submitFixtureForm(fixture);
  }

  function appButtonHosts(): HTMLElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll('app-button')) as HTMLElement[];
  }

  function deleteButtonHost(): HTMLElement | null {
    // The label is 'Delete' or 'Deleting...'; both contain 'Delet' with a
    // capital D, which the lowercase 'delete' icon ligature does not.
    return appButtonHosts().find((host) => {
      const text = textContent(host);
      return text.includes('Delet') && !text.includes('thumbnail');
    }) ?? null;
  }

  function deleteButton(): HTMLButtonElement | null {
    return deleteButtonHost()?.querySelector('button') ?? null;
  }

  function cancelButton(): HTMLButtonElement | null {
    const host = appButtonHosts().find((button) => textContent(button).includes('Cancel'));
    return host?.querySelector('button') ?? null;
  }

  function saveButton(): HTMLButtonElement {
    return fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
  }

  function statusPills(): HTMLElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll('app-status-pill'));
  }

  async function routeExitDecision(): Promise<boolean> {
    return resolveCanDeactivate(fixture.componentInstance.canDeactivateWithPendingChanges());
  }

  function eventTextControls(): Array<HTMLInputElement | HTMLTextAreaElement> {
    return Array.from(
      fixture.nativeElement.querySelectorAll('app-input input, app-input textarea'),
    ) as Array<HTMLInputElement | HTMLTextAreaElement>;
  }

  function startDateInput(): HTMLInputElement {
    return fixture.nativeElement.querySelector('app-date input') as HTMLInputElement;
  }

  function startTimeInput(): HTMLInputElement {
    return fixture.nativeElement.querySelector('app-time input') as HTMLInputElement;
  }

  function platformPublishHosts(): HTMLElement[] {
    return Array.from(
      fixture.nativeElement.querySelectorAll('.platform-publish-button'),
    ) as HTMLElement[];
  }

  function platformPublishButton(): HTMLButtonElement | null {
    return platformPublishHosts()[0]?.querySelector('button') ?? null;
  }

  function platformDeletePublicationHosts(): HTMLElement[] {
    return Array.from(
      fixture.nativeElement.querySelectorAll('.platform-delete-publication-button'),
    ) as HTMLElement[];
  }

  function platformDeletePublicationButton(): HTMLButtonElement | null {
    return platformDeletePublicationHosts()[0]?.querySelector('button') ?? null;
  }

  function thumbnailSelectInput(): HTMLInputElement | null {
    return fixture.nativeElement.querySelector(
      '.thumbnail-select-input input[type="file"]',
    );
  }

  function thumbnailClearButtonHost(): HTMLElement | null {
    return fixture.nativeElement.querySelector('.thumbnail-clear-button');
  }

  function thumbnailDeleteButtonHost(): HTMLElement | null {
    return appButtonHosts().find((host) =>
      textContent(host).includes('Delete thumbnail'),
    ) ?? null;
  }

  function thumbnailDeleteButton(): HTMLButtonElement | null {
    return thumbnailDeleteButtonHost()?.querySelector('button') ?? null;
  }

  function chooseThumbnail(input: HTMLInputElement, file: File): void {
    Object.defineProperty(input, 'files', {
      configurable: true,
      value: {
        0: file,
        length: 1,
        item: (index: number) => (index === 0 ? file : null),
      },
    });
    input.dispatchEvent(new Event('change'));
    fixture.detectChanges();
  }

  function imageFile(
    name = 'stream.png',
    type = 'image/png',
    sizeBytes = 11,
  ): File {
    return new File([new Uint8Array(sizeBytes)], name, { type });
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

  it('selects, previews, and clears a thumbnail in create mode', () => {
    const file = imageFile('stream.png', 'image/png');

    chooseThumbnail(thumbnailSelectInput()!, file);

    expect(URL.createObjectURL).toHaveBeenCalledWith(file);
    expect(fixture.nativeElement.querySelector('.thumbnail-preview')?.getAttribute('src')).toBe(
      'blob:thumbnail-1',
    );
    expect(fixture.nativeElement.textContent).toContain('stream.png');

    thumbnailClearButtonHost()!.dispatchEvent(new Event('click'));
    fixture.detectChanges();

    expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:thumbnail-1');
    expect(fixture.nativeElement.querySelector('.thumbnail-preview')).toBeNull();
  });

  it('rejects unsupported thumbnail files before upload', () => {
    chooseThumbnail(thumbnailSelectInput()!, imageFile('stream.gif', 'image/gif'));

    expect(fixture.nativeElement.textContent).toContain(
      'Thumbnail file must be a JPEG or PNG image.',
    );
    expect(URL.createObjectURL).not.toHaveBeenCalled();
  });

  it('rejects thumbnail files over 2 MB before upload', () => {
    chooseThumbnail(
      thumbnailSelectInput()!,
      imageFile('stream.png', 'image/png', 2 * 1024 * 1024 + 1),
    );

    expect(fixture.nativeElement.textContent).toContain(
      'Thumbnail file size must be 2 MB or smaller.',
    );
    expect(URL.createObjectURL).not.toHaveBeenCalled();
  });

  it('uploads the selected thumbnail after creating the event', async () => {
    const file = imageFile('stream.png', 'image/png');
    service.create.mockReturnValue(of({ calendarEventId: 'created-event' }));
    service.uploadThumbnail.mockReturnValue(of(thumbnailResponse({ fileName: 'stream.png' })));
    fillValidForm();
    chooseThumbnail(thumbnailSelectInput()!, file);

    await submitForm();

    expect(service.create).toHaveBeenCalledTimes(1);
    expect(service.uploadThumbnail).toHaveBeenCalledWith('created-event', file);
    expect(notifications.showSuccess).toHaveBeenCalledWith('Calendar event created.');
    expect(navigations).toEqual(['/calendar-events']);
  });

  it('opens the created event edit page when create-mode thumbnail upload fails', async () => {
    const file = imageFile('stream.png', 'image/png');
    service.create.mockReturnValue(of({ calendarEventId: 'created-event' }));
    service.uploadThumbnail.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 409 })),
    );
    fillValidForm();
    chooseThumbnail(thumbnailSelectInput()!, file);

    await submitForm();

    expect(service.create).toHaveBeenCalledTimes(1);
    expect(service.uploadThumbnail).toHaveBeenCalledWith('created-event', file);
    expect(notifications.showSuccess).toHaveBeenCalledWith('Calendar event created.');
    expect(navigations).toEqual(['/calendar-events/created-event/edit']);
    expect(navigationStates).toEqual([
      {
        thumbnailErrorMessage:
          'The thumbnail can no longer be changed. Reload the page and try again.',
      },
    ]);
  });

  it('disables create-mode thumbnail selection while saving the event', () => {
    service.create.mockReturnValue(new Subject<CreateCalendarEventResponse>());
    fillValidForm();

    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(thumbnailSelectInput()!.disabled).toBe(true);
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
    cancelButton()!.click();
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
    expect(text).toContain('Thursday, July 4, 2030 5:00 PM');
  });

  it('does not show a delete button in create mode', () => {
    expect(deleteButtonHost()).toBeNull();
  });

  describe('edit mode', () => {
    const editId = calendarEventId;

    function createEditComponent(navigationState?: Record<string, unknown>): void {
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

      const router = TestBed.inject(Router);
      if (navigationState !== undefined) {
        vi.spyOn(router, 'getCurrentNavigation').mockReturnValue({
          extras: { state: navigationState },
        } as ReturnType<Router['getCurrentNavigation']>);
      }

      fixture = TestBed.createComponent(CalendarEventDetails);

      router.navigateByUrl = ((url: string, extras?: { state?: Record<string, unknown> }) => {
        navigations.push(url);
        navigationStates.push(extras?.state);
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

    it('shows the create-mode thumbnail upload error carried by navigation state', () => {
      service.getById.mockReturnValue(of(sampleEvent()));

      createEditComponent({
        thumbnailErrorMessage:
          'The thumbnail can no longer be changed. Reload the page and try again.',
      });

      expect(fixture.nativeElement.textContent).toContain(
        'The thumbnail can no longer be changed. Reload the page and try again.',
      );
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
      expect(text).toContain('Thursday, July 4, 2030 8:30 AM');
    });

    it('does not show edit-mode save scope copy for an editable event', () => {
      service.getById.mockReturnValue(of(sampleEvent()));

      createEditComponent();

      expect(fixture.nativeElement.textContent).not.toContain(
        'Save changes updates scheduled start and event text only.',
      );
      expect(fixture.nativeElement.textContent).not.toContain(
        'Delete platform publications before changing this event or deleting it.',
      );
      expect(statusPills()).toHaveLength(0);
    });

    it('enables Save without showing a status pill when event text changes from the saved baseline', () => {
      service.getById.mockReturnValue(of(sampleEvent()));

      createEditComponent();
      api().form.texts[0].value().value.set('Updated English title');
      fixture.detectChanges();

      expect(saveButton().disabled).toBe(false);
      expect(statusPills()).toHaveLength(0);
    });

    it('disables Save while an edit-mode save is in flight without showing a status pill', () => {
      service.getById.mockReturnValue(of(sampleEvent()));
      service.update.mockReturnValue(new Subject<UpdateCalendarEventResponse>());

      createEditComponent();
      api().form.texts[0].value().value.set('Updated English title');

      fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));
      fixture.detectChanges();

      expect(saveButton().disabled).toBe(true);
      expect(statusPills()).toHaveLength(0);
    });

    it('shows save failures with the existing save error alert', async () => {
      service.getById.mockReturnValue(of(sampleEvent()));
      service.update.mockReturnValue(throwError(() => new Error('boom')));

      createEditComponent();
      api().form.texts[0].value().value.set('Updated English title');

      await submitForm();

      const alert = fixture.nativeElement.querySelector('[role="alert"]');
      expect(alert).not.toBeNull();
      expect(alert.textContent).toContain('The event could not be saved.');
      expect(statusPills()).toHaveLength(0);
    });

    it('shows the locked-state alert without a status pill when event updates are locked', () => {
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

      const text = fixture.nativeElement.textContent;
      const lockMessage = 'Delete platform publications before changing this event or deleting it.';
      expect(text).toContain(lockMessage);
      expect(text.indexOf(lockMessage)).toBeLessThan(text.indexOf('Scheduled start'));
      expect(statusPills()).toHaveLength(0);
    });

    it('navigates away on clean edit-mode Cancel without confirmation', async () => {
      service.getById.mockReturnValue(of(sampleEvent()));

      createEditComponent();

      cancelButton()!.click();
      await fixture.whenStable();

      expect(confirmation.confirm).not.toHaveBeenCalled();
      expect(navigations).toEqual(['/calendar-events']);
    });

    it('keeps editing on pending edit-mode Cancel when discard is rejected', async () => {
      confirmation.confirm.mockReturnValueOnce(of('keep-editing'));
      service.getById.mockReturnValue(of(sampleEvent()));

      createEditComponent();
      api().form.texts[0].value().value.set('Updated English title');
      fixture.detectChanges();

      cancelButton()!.click();
      await fixture.whenStable();

      expect(confirmation.confirm).toHaveBeenCalledWith(
        expect.objectContaining({
          kind: 'warning',
          title: 'Discard unsaved event changes?',
          body: 'Scheduled start and event text changes have not been saved.',
          actions: [
            { id: 'keep-editing', label: 'Keep editing' },
            { id: 'discard', label: 'Discard changes', primary: true },
          ],
        }),
      );
      expect(navigations).toEqual([]);
    });

    it('navigates away on pending edit-mode Cancel when discard is confirmed', async () => {
      confirmation.confirm.mockReturnValueOnce(of('discard'));
      service.getById.mockReturnValue(of(sampleEvent()));

      createEditComponent();
      api().form.texts[0].value().value.set('Updated English title');
      fixture.detectChanges();

      cancelButton()!.click();
      await fixture.whenStable();

      expect(navigations).toEqual(['/calendar-events']);
    });

    it('allows clean route exit without confirmation', async () => {
      service.getById.mockReturnValue(of(sampleEvent()));

      createEditComponent();

      await expect(routeExitDecision()).resolves.toBe(true);
      expect(confirmation.confirm).not.toHaveBeenCalled();
    });

    it('blocks pending route exit when discard is rejected', async () => {
      confirmation.confirm.mockReturnValueOnce(of('keep-editing'));
      service.getById.mockReturnValue(of(sampleEvent()));

      createEditComponent();
      api().form.texts[0].value().value.set('Updated English title');

      await expect(routeExitDecision()).resolves.toBe(false);
      expect(confirmation.confirm).toHaveBeenCalledWith(
        expect.objectContaining({
          title: 'Discard unsaved event changes?',
        }),
      );
    });

    it('allows pending route exit when discard is confirmed', async () => {
      confirmation.confirm.mockReturnValueOnce(of('discard'));
      service.getById.mockReturnValue(of(sampleEvent()));

      createEditComponent();
      api().form.texts[0].value().value.set('Updated English title');

      await expect(routeExitDecision()).resolves.toBe(true);
    });

    it('allows route exit during an active mutation without confirmation', async () => {
      service.getById.mockReturnValue(of(sampleEvent()));
      service.update.mockReturnValue(new Subject<UpdateCalendarEventResponse>());

      createEditComponent();
      api().form.texts[0].value().value.set('Updated English title');
      fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));
      fixture.detectChanges();

      await expect(routeExitDecision()).resolves.toBe(true);
      expect(confirmation.confirm).not.toHaveBeenCalled();
    });

    it('loads the current thumbnail preview through the API client', () => {
      service.getById.mockReturnValue(of(sampleEvent({ thumbnail: thumbnailResponse() })));
      const content = new Blob(['image-bytes'], { type: 'image/png' });
      service.getThumbnail.mockReturnValue(of(content));

      createEditComponent();

      expect(service.getThumbnail).toHaveBeenCalledWith(editId);
      expect(URL.createObjectURL).toHaveBeenCalledWith(content);
      expect(fixture.nativeElement.querySelector('.thumbnail-preview')?.getAttribute('src')).toBe(
        'blob:thumbnail-1',
      );
      expect(fixture.nativeElement.textContent).toContain('stream.png');
      expect(fixture.nativeElement.textContent).toContain('1280 x 720');
      expect(fixture.nativeElement.textContent).not.toContain('Current thumbnail');
      expect(thumbnailSelectInput()).toBeNull();
      expect(statusPills()).toHaveLength(0);
    });

    it('uploads a thumbnail when no current thumbnail is stored without discarding unsaved event text', async () => {
      const selected = imageFile('selected.png', 'image/png');
      service.getById.mockReturnValue(of(sampleEvent({ thumbnail: null })));
      service.uploadThumbnail.mockReturnValue(
        of(thumbnailResponse({ fileName: 'selected.png' })),
      );

      createEditComponent();
      api().form.texts[0].value().value.set('Unsaved English title');
      chooseThumbnail(thumbnailSelectInput()!, selected);
      await fixture.whenStable();
      fixture.detectChanges();

      expect(service.uploadThumbnail).toHaveBeenCalledWith(editId, selected);
      expect(fixture.nativeElement.textContent).toContain('selected.png');
      expect(fixture.nativeElement.querySelector('.thumbnail-preview')?.getAttribute('src')).toBe(
        'blob:thumbnail-1',
      );
      expect(eventTextControls()[0].value).toBe('Unsaved English title');
      expect(notifications.showSuccess).toHaveBeenCalledWith('Thumbnail uploaded.');
      expect(statusPills()).toHaveLength(0);
    });

    it('deletes the thumbnail without discarding unsaved event text', async () => {
      service.getById.mockReturnValue(of(sampleEvent({ thumbnail: thumbnailResponse() })));
      service.getThumbnail.mockReturnValue(of(new Blob(['image-bytes'], { type: 'image/png' })));
      service.deleteThumbnail.mockReturnValue(of<void>(undefined));

      createEditComponent();
      api().form.texts[0].value().value.set('Unsaved English title');
      thumbnailDeleteButtonHost()!.dispatchEvent(new Event('click'));
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(service.deleteThumbnail).toHaveBeenCalledWith(editId);
      expect(fixture.nativeElement.textContent).toContain('No thumbnail selected.');
      expect(fixture.nativeElement.textContent).not.toContain('stream.png');
      expect(eventTextControls()[0].value).toBe('Unsaved English title');
      expect(notifications.showSuccess).toHaveBeenCalledWith('Thumbnail deleted.');
    });

    it('uses canUpdateThumbnail from the API without locking event form controls', () => {
      service.getById.mockReturnValue(
        of(
          sampleEvent({
            canUpdate: true,
            canDelete: true,
            canUpdateThumbnail: false,
            thumbnail: thumbnailResponse(),
            platforms: [],
          }),
        ),
      );
      service.getThumbnail.mockReturnValue(of(new Blob(['image-bytes'], { type: 'image/png' })));

      createEditComponent();

      expect(fixture.nativeElement.textContent).not.toContain(
        'Delete platform publications before changing this thumbnail.',
      );
      expect(thumbnailSelectInput()).toBeNull();
      expect(thumbnailDeleteButton()!.disabled).toBe(true);
      expect(saveButton().disabled).toBe(true);
      api().form.texts[0].value().value.set('Updated English title');
      fixture.detectChanges();
      expect(saveButton().disabled).toBe(false);
      expect(deleteButton()!.disabled).toBe(false);
    });

    it('disables edit-mode thumbnail controls while another mutation is active', () => {
      service.getById.mockReturnValue(of(sampleEvent({ thumbnail: null })));
      service.update.mockReturnValue(new Subject<UpdateCalendarEventResponse>());

      createEditComponent();
      api().form.texts[0].value().value.set('Updated English title');
      fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));
      fixture.detectChanges();

      expect(thumbnailSelectInput()!.disabled).toBe(true);
    });

    it('shows a thumbnail error when edit-mode upload is rejected by the backend', async () => {
      service.getById.mockReturnValue(of(sampleEvent({ thumbnail: null })));
      service.uploadThumbnail.mockReturnValue(
        throwError(() => new HttpErrorResponse({ status: 400 })),
      );

      createEditComponent();
      chooseThumbnail(thumbnailSelectInput()!, imageFile('selected.png', 'image/png'));
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.textContent).toContain(
        'The thumbnail must be a JPEG or PNG image up to 2 MB.',
      );
    });

    it('wires loaded platform rows into the platform child state', () => {
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
                thumbnailStatus: 'NotConfigured',
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
                thumbnailStatus: null,
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

      const platforms = fixture.nativeElement.querySelector('app-calendar-event-platforms');
      expect(platforms).not.toBeNull();
      expect(textContent(platforms)).toContain('Main YouTube channel');
      expect(textContent(platforms)).toContain('Archive site');
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
      expect(eventTextControls().every((control) => control.disabled)).toBe(true);
      expect(eventTextControls()[0].value).toBe('English title');
      expect(notifications.showSuccess).toHaveBeenCalledWith('Calendar event published.');
    });

    it('refreshes event details after publication delete and unlocks event update and delete from API flags', async () => {
      const unpublishedPlatform: CalendarEventPlatform = {
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

      platformDeletePublicationHosts()[0].dispatchEvent(new Event('click'));
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.textContent).toContain('NotPublished');
      expect(service.getById).toHaveBeenCalledTimes(2);
      expect(platformDeletePublicationHosts()).toHaveLength(0);
      expect(platformPublishHosts()).toHaveLength(1);
      expect(saveButton().disabled).toBe(true);
      expect(deleteButton()!.disabled).toBe(false);
      expect(eventTextControls().every((control) => !control.disabled)).toBe(true);
      expect(eventTextControls()[0].value).toBe('English title');
      expect(notifications.showSuccess).toHaveBeenCalledWith('Platform publication deleted.');
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

      expect(startDateInput().disabled).toBe(false);
      expect(startTimeInput().disabled).toBe(false);
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

      expect(startDateInput().disabled).toBe(true);
      expect(startTimeInput().disabled).toBe(true);
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
      expect(fixture.nativeElement.textContent).toContain(
        'Friday, July 5, 2030 5:00 PM',
      );
    });

    it('disables unchanged edit-mode Save', () => {
      service.getById.mockReturnValue(of(sampleEvent()));

      createEditComponent();

      expect(saveButton().disabled).toBe(true);
      expect(statusPills()).toHaveLength(0);
    });

    it('does not update when an unchanged edit form is submitted programmatically', async () => {
      service.getById.mockReturnValue(of(sampleEvent()));

      createEditComponent();

      await submitForm();

      expect(service.update).not.toHaveBeenCalled();
      expect(navigations).toEqual([]);
    });

    it('enables changed edit-mode Save and updates start and text values on submit', async () => {
      service.getById.mockReturnValue(of(sampleEvent()));
      service.update.mockReturnValue(of({ calendarEventId: editId }));

      createEditComponent();

      expect(saveButton().disabled).toBe(true);

      api().form.texts[0].value().value.set('  Updated English title  ');
      api().form.start().value.set({
        date: '2030-07-05',
        time: '10:00',
        timeZoneId: 'America/Vancouver',
      });
      fixture.detectChanges();

      expect(saveButton().disabled).toBe(false);

      await submitForm();

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
      expect(navigations).toEqual([]);
      expect(saveButton().disabled).toBe(true);
      expect(statusPills()).toHaveLength(0);
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
      expect(startDateInput().disabled).toBe(true);
      expect(startTimeInput().disabled).toBe(true);
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

      await submitForm();

      expect(service.update).not.toHaveBeenCalled();
      expect(navigations).toEqual([]);
    });

    it('shows a save error and does not navigate when the update fails', async () => {
      service.getById.mockReturnValue(of(sampleEvent()));
      service.update.mockReturnValue(throwError(() => new Error('boom')));

      createEditComponent();
      api().form.texts[0].value().value.set('Updated English title');

      await submitForm();

      const alert = fixture.nativeElement.querySelector('[role="alert"]');
      expect(alert).not.toBeNull();
      expect(alert.textContent).toContain('The event could not be saved.');
      expect(navigations).toEqual([]);
    });

    it('maps an update 409 to a reload message and stays on the page', async () => {
      service.getById.mockReturnValue(of(sampleEvent()));
      service.update.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 409 })));

      createEditComponent();
      api().form.texts[0].value().value.set('Updated English title');

      await submitForm();

      expect(fixture.nativeElement.textContent).toContain(
        'The event can no longer be updated. Reload the page and try again.',
      );
      expect(navigations).toEqual([]);
    });

    it('shows an error and no form when the event cannot be loaded', () => {
      service.getById.mockReturnValue(throwError(() => new Error('boom')));

      createEditComponent();

      const alert = fixture.nativeElement.querySelector('[role="alert"]');
      expect(alert).not.toBeNull();
      expect(alert.textContent).toContain('could not be loaded');
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
      expect(confirmation.confirm).not.toHaveBeenCalled();
      expect(navigations).toEqual([]);
    });

    it('does not delete a draft when delete confirmation is cancelled', async () => {
      confirmation.confirm.mockReturnValueOnce(of('cancel'));
      service.getById.mockReturnValue(of(sampleEvent()));
      service.delete.mockReturnValue(of<void>(undefined));

      createEditComponent();

      deleteButtonHost()!.dispatchEvent(new Event('click'));
      fixture.detectChanges();
      await fixture.whenStable();

      expect(confirmation.confirm).toHaveBeenCalledWith(
        expect.objectContaining({
          kind: 'warning',
          title: 'Delete calendar event?',
          body: 'This removes the calendar event from YTSkedy. Published provider resources are not removed by this action.',
          actions: [
            { id: 'cancel', label: 'Cancel' },
            { id: 'delete', label: 'Delete event', primary: true },
          ],
        }),
      );
      expect(service.delete).not.toHaveBeenCalled();
      expect(navigations).toEqual([]);
    });

    it('deletes a draft after delete confirmation, notifies, and navigates to the list', async () => {
      service.getById.mockReturnValue(of(sampleEvent()));
      service.delete.mockReturnValue(of<void>(undefined));

      createEditComponent();

      deleteButtonHost()!.dispatchEvent(new Event('click'));
      fixture.detectChanges();
      await fixture.whenStable();

      expect(confirmation.confirm).toHaveBeenCalledWith(
        expect.objectContaining({
          kind: 'warning',
          title: 'Delete calendar event?',
          body: 'This removes the calendar event from YTSkedy. Published provider resources are not removed by this action.',
          actions: [
            { id: 'cancel', label: 'Cancel' },
            { id: 'delete', label: 'Delete event', primary: true },
          ],
        }),
      );
      expect(service.delete).toHaveBeenCalledWith(editId);
      expect(notifications.showSuccess).toHaveBeenCalledWith('Calendar event deleted.');
      expect(navigations).toEqual(['/calendar-events']);
    });

    it('does not show delete confirmation when pending changes are kept', async () => {
      confirmation.confirm.mockReturnValueOnce(of('keep-editing'));
      service.getById.mockReturnValue(of(sampleEvent()));

      createEditComponent();
      api().form.texts[0].value().value.set('Updated English title');

      deleteButtonHost()!.dispatchEvent(new Event('click'));
      fixture.detectChanges();
      await fixture.whenStable();

      expect(confirmation.confirm).toHaveBeenCalledTimes(1);
      expect(confirmation.confirm).toHaveBeenCalledWith(
        expect.objectContaining({
          kind: 'warning',
          title: 'Discard unsaved event changes?',
        }),
      );
      expect(service.delete).not.toHaveBeenCalled();
      expect(navigations).toEqual([]);
    });

    it('deletes after pending changes are discarded and delete is confirmed', async () => {
      confirmation.confirm
        .mockReturnValueOnce(of('discard'))
        .mockReturnValueOnce(of('delete'));
      service.getById.mockReturnValue(of(sampleEvent()));
      service.delete.mockReturnValue(of<void>(undefined));

      createEditComponent();
      api().form.texts[0].value().value.set('Updated English title');

      deleteButtonHost()!.dispatchEvent(new Event('click'));
      fixture.detectChanges();
      await fixture.whenStable();

      expect(confirmation.confirm).toHaveBeenNthCalledWith(
        1,
        expect.objectContaining({
          kind: 'warning',
          title: 'Discard unsaved event changes?',
        }),
      );
      expect(confirmation.confirm).toHaveBeenNthCalledWith(
        2,
        expect.objectContaining({
          kind: 'warning',
          title: 'Delete calendar event?',
        }),
      );
      expect(service.delete).toHaveBeenCalledWith(editId);
      expect(notifications.showSuccess).toHaveBeenCalledWith('Calendar event deleted.');
      expect(navigations).toEqual(['/calendar-events']);
    });

    it('does not delete after pending changes are discarded and delete is cancelled', async () => {
      confirmation.confirm
        .mockReturnValueOnce(of('discard'))
        .mockReturnValueOnce(of('cancel'));
      service.getById.mockReturnValue(of(sampleEvent()));

      createEditComponent();
      api().form.texts[0].value().value.set('Updated English title');

      deleteButtonHost()!.dispatchEvent(new Event('click'));
      fixture.detectChanges();
      await fixture.whenStable();

      expect(confirmation.confirm).toHaveBeenNthCalledWith(
        1,
        expect.objectContaining({
          title: 'Discard unsaved event changes?',
        }),
      );
      expect(confirmation.confirm).toHaveBeenNthCalledWith(
        2,
        expect.objectContaining({
          title: 'Delete calendar event?',
        }),
      );
      expect(service.delete).not.toHaveBeenCalled();
      expect(navigations).toEqual([]);
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
      api().form.texts[0].value().value.set('Updated English title');

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
      thumbnailStatus: 'Applied',
      publishedUtc: '2030-07-04T08:45:00+00:00',
      platformDeletedUtc: null,
      canPublish: false,
      canDeletePublication: true,
      canPreviewPublishingContent: true,
      ...overrides,
    };
  }

  function thumbnailResponse(
    overrides: Partial<CalendarEventThumbnail> = {},
  ): CalendarEventThumbnail {
    return {
      fileName: 'stream.png',
      contentType: 'image/png',
      sizeBytes: 11,
      width: 1280,
      height: 720,
      updatedUtc: '2030-07-04T08:20:00+00:00',
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
          thumbnailStatus: 'NotConfigured',
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
