import { HttpErrorResponse } from '@angular/common/http';
import { provideZonelessChangeDetection, type WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { type FieldTree } from '@angular/forms/signals';
import { MatDateFormats } from '@angular/material/core';
import { provideLuxonDateAdapter } from '@angular/material-luxon-adapter';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { type Observable, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, type Mock, vi } from 'vitest';

import {
  type CalendarEventDetailsResponse,
  type CalendarEventDefaultStart,
  CalendarEventsService,
  type CreateCalendarEventRequest,
  type CreateCalendarEventResponse,
  type UpdateCalendarEventRequest,
  type UpdateCalendarEventResponse,
} from 'src/app/shared/api/calendar-events/calendar-events-service';
import {
  type EventTextFieldsResponse,
  EventTextFieldsService,
} from 'src/app/shared/api/settings/event-text-fields-service';
import { ConfirmationDialogService } from 'src/app/shared/components/confirmation-dialog/confirmation-dialog-service';
import {
  DATE_INPUT_DISPLAY_FORMAT,
  DATE_INPUT_FORMAT,
  TIME_INPUT_FORMAT,
} from 'src/app/shared/date-time/date-time-format';
import { NotificationService } from 'src/app/shared/notifications/notification-service';
import {
  resolveCanDeactivate,
  submitForm as submitFixtureForm,
  textContent,
} from 'src/app/testing/dom-test-helpers';
import { CalendarEventDetails } from './calendar-event-details';
import { type CalendarEventDetailsModel } from './calendar-event-details.form';
import {
  testCalendarEventDetails,
  testCalendarEventPlatform,
  testEventTextFieldsResponse,
} from './testing/calendar-event-details.fixture';

const testDateFormats: MatDateFormats = {
  parse: { dateInput: DATE_INPUT_FORMAT, timeInput: TIME_INPUT_FORMAT },
  display: {
    dateInput: DATE_INPUT_DISPLAY_FORMAT,
    monthYearLabel: 'LLL yyyy',
    dateA11yLabel: 'DDD',
    monthYearA11yLabel: 'LLLL yyyy',
    timeInput: TIME_INPUT_FORMAT,
    timeOptionLabel: TIME_INPUT_FORMAT,
  },
};

describe('CalendarEventDetails', () => {
  const calendarEventId = 'event-1';

  let fixture: ComponentFixture<CalendarEventDetails>;
  let service: {
    create: Mock<(request: CreateCalendarEventRequest) => Observable<CreateCalendarEventResponse>>;
    getById: Mock<(id: string) => Observable<CalendarEventDetailsResponse>>;
    getDefaultStart: Mock<(fallbackTimeZoneId?: string) => Observable<CalendarEventDefaultStart>>;
    update: Mock<
      (id: string, request: UpdateCalendarEventRequest) => Observable<UpdateCalendarEventResponse>
    >;
    delete: Mock<(id: string) => Observable<void>>;
    publishPlatform: Mock;
    deletePlatformPublication: Mock;
    getPublishingContent: Mock;
    uploadThumbnail: Mock;
    getThumbnail: Mock;
    deleteThumbnail: Mock;
  };
  let eventTextFields: { get: Mock<() => Observable<EventTextFieldsResponse>> };
  let confirmation: { confirm: Mock<(data: unknown) => Observable<string | undefined>> };
  let notifications: { showSuccess: Mock<(message: string) => void> };
  let navigations: string[];

  beforeEach(() => {
    service = {
      create: vi.fn(),
      getById: vi.fn(),
      getDefaultStart: vi.fn().mockReturnValue(
        of({ localDate: null, localTime: null, timeZoneId: null }),
      ),
      update: vi.fn(),
      delete: vi.fn(),
      publishPlatform: vi.fn(),
      deletePlatformPublication: vi.fn(),
      getPublishingContent: vi.fn(),
      uploadThumbnail: vi.fn(),
      getThumbnail: vi.fn(),
      deleteThumbnail: vi.fn(),
    };
    eventTextFields = { get: vi.fn().mockReturnValue(of(testEventTextFieldsResponse())) };
    confirmation = { confirm: vi.fn().mockReturnValue(of('delete')) };
    notifications = { showSuccess: vi.fn() };
    navigations = [];

    Object.defineProperty(URL, 'createObjectURL', {
      configurable: true,
      value: vi.fn(() => 'blob:thumbnail'),
    });
    Object.defineProperty(URL, 'revokeObjectURL', {
      configurable: true,
      value: vi.fn(),
    });
  });

  it('loads current fields and renders create mode', () => {
    createComponent();

    expect(eventTextFields.get).toHaveBeenCalledTimes(1);
    expect(service.getById).not.toHaveBeenCalled();
    expect(textContent(fixture.nativeElement)).toContain('Add Calendar Event');
    expect(eventTextControls()).toHaveLength(2);
    expect(deleteButtonHost()).toBeNull();
  });

  it('reveals validation errors instead of creating an invalid event', async () => {
    createComponent();

    await submitForm();

    expect(service.create).not.toHaveBeenCalled();
    expect(textContent(fixture.nativeElement)).toContain('Start date is required.');
  });

  it('submits a contract-correct create request and navigates to the list', async () => {
    service.create.mockReturnValue(of({ calendarEventId: 'created-event' }));
    createComponent();
    fillValidForm();

    await submitForm();

    expect(service.create).toHaveBeenCalledWith({
      start: { localDateTime: '2999-01-01T10:00:00', timeZoneId: 'UTC' },
      texts: [
        { fieldKey: 'text1', value: 'English title' },
        { fieldKey: 'text2', value: 'English description' },
      ],
    });
    expect(notifications.showSuccess).toHaveBeenCalledWith('Calendar event created.');
    expect(navigations).toEqual(['/calendar-events']);
  });

  it('navigates to the list when clean create mode is cancelled', () => {
    createComponent();

    cancelButton().click();

    expect(navigations).toEqual(['/calendar-events']);
  });

  it('renders a live UTC preview from the create draft', () => {
    createComponent();
    fillValidForm();
    fixture.detectChanges();

    expect(textContent(fixture.nativeElement)).toContain('2999');
  });

  it('loads stored edit details and renders child platform state', () => {
    service.getById.mockReturnValue(of(testCalendarEventDetails()));
    createComponent(calendarEventId);

    expect(service.getById).toHaveBeenCalledWith(calendarEventId);
    expect(eventTextFields.get).not.toHaveBeenCalled();
    expect(textContent(fixture.nativeElement)).toContain('Edit Calendar Event');
    expect(textContent(fixture.nativeElement)).toContain('Main YouTube channel');
    expect(eventTextControls()[0].value).toBe('English title');
  });

  it('updates changed edit values and remains on the route', async () => {
    service.getById.mockReturnValue(of(testCalendarEventDetails()));
    service.update.mockReturnValue(of({ calendarEventId }));
    createComponent(calendarEventId);
    api().form.texts[0].value().value.set('Updated title');

    await submitForm();

    expect(service.update).toHaveBeenCalledWith(
      calendarEventId,
      expect.objectContaining({
        texts: expect.arrayContaining([{ fieldKey: 'text1', value: 'Updated title' }]),
      }),
    );
    expect(notifications.showSuccess).toHaveBeenCalledWith('Calendar event updated.');
    expect(navigations).toEqual([]);
    expect(saveButton().disabled).toBe(true);
  });

  it('delegates pending route exit to the state confirmation', async () => {
    service.getById.mockReturnValue(of(testCalendarEventDetails()));
    confirmation.confirm.mockReturnValue(of('keep-editing'));
    createComponent(calendarEventId);
    api().form.texts[0].value().value.set('Updated title');

    expect(await routeExitDecision()).toBe(false);
    expect(confirmation.confirm).toHaveBeenCalledWith(
      expect.objectContaining({ title: 'Discard unsaved event changes?' }),
    );
  });

  it('renders backend-provided edit locks', () => {
    service.getById.mockReturnValue(
      of(
        testCalendarEventDetails({
          canUpdate: false,
          canDelete: false,
          platforms: [testCalendarEventPlatform()],
        }),
      ),
    );
    createComponent(calendarEventId);

    expect(textContent(fixture.nativeElement)).toContain(
      'Delete platform publications before changing this event or deleting it.',
    );
    expect(startDateInput().disabled).toBe(true);
    expect(saveButton().disabled).toBe(true);
    expect(deleteButton().disabled).toBe(true);
  });

  it('applies refreshed root locks after publishing through the child state', () => {
    const published = testCalendarEventPlatform();
    service.getById.mockReturnValueOnce(of(testCalendarEventDetails())).mockReturnValueOnce(
      of(
        testCalendarEventDetails({
          canUpdate: false,
          canDelete: false,
          platforms: [published],
        }),
      ),
    );
    service.publishPlatform.mockReturnValue(of(published));
    createComponent(calendarEventId);

    platformPublishButton().click();
    fixture.detectChanges();

    expect(service.publishPlatform).toHaveBeenCalledWith(calendarEventId, 'platform-1');
    expect(service.getById).toHaveBeenCalledTimes(2);
    expect(startDateInput().disabled).toBe(true);
    expect(deleteButton().disabled).toBe(true);
  });

  it('renders load failures without the form', () => {
    service.getById.mockReturnValue(throwError(() => new Error('network')));
    createComponent(calendarEventId);

    expect(textContent(fixture.nativeElement)).toContain('The calendar event could not be loaded.');
    expect(fixture.nativeElement.querySelector('form')).toBeNull();
  });

  it('focuses save errors', async () => {
    service.getById.mockReturnValue(of(testCalendarEventDetails()));
    service.update.mockReturnValue(throwError(() => new Error('network')));
    createComponent(calendarEventId);
    api().form.texts[0].value().value.set('Updated title');

    await submitForm();

    const alert = fixture.nativeElement.querySelector('app-alert[tabindex="-1"]');
    expect(textContent(alert)).toContain('The event could not be saved.');
    expect(document.activeElement).toBe(alert);
  });

  it('focuses delete errors', async () => {
    service.getById.mockReturnValue(of(testCalendarEventDetails()));
    service.delete.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 502 })));
    createComponent(calendarEventId);

    deleteButton().click();
    await fixture.whenStable();
    fixture.detectChanges();

    const alert = fixture.nativeElement.querySelector('app-alert[tabindex="-1"]');
    expect(textContent(alert)).toContain('The event could not be deleted.');
    expect(document.activeElement).toBe(alert);
    expect(navigations).toEqual([]);
  });

  it('deletes a confirmed draft and navigates to the list', () => {
    service.getById.mockReturnValue(of(testCalendarEventDetails()));
    service.delete.mockReturnValue(of(undefined));
    createComponent(calendarEventId);

    deleteButton().click();

    expect(service.delete).toHaveBeenCalledWith(calendarEventId);
    expect(notifications.showSuccess).toHaveBeenCalledWith('Calendar event deleted.');
    expect(navigations).toEqual(['/calendar-events']);
  });

  function createComponent(
    id: string | null = null,
    navigationState?: Record<string, unknown>,
  ): void {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideLuxonDateAdapter(testDateFormats),
        { provide: CalendarEventsService, useValue: service },
        { provide: EventTextFieldsService, useValue: eventTextFields },
        { provide: ConfirmationDialogService, useValue: confirmation },
        { provide: NotificationService, useValue: notifications },
        { provide: ActivatedRoute, useValue: routeWithId(id) },
      ],
    });

    const router = TestBed.inject(Router);
    if (navigationState !== undefined) {
      vi.spyOn(router, 'getCurrentNavigation').mockReturnValue({
        extras: { state: navigationState },
      } as ReturnType<Router['getCurrentNavigation']>);
    }
    router.navigateByUrl = ((url: string) => {
      navigations.push(url);
      return Promise.resolve(true);
    }) as Router['navigateByUrl'];

    fixture = TestBed.createComponent(CalendarEventDetails);
    fixture.detectChanges();
  }

  function api(): {
    model: WritableSignal<CalendarEventDetailsModel>;
    form: FieldTree<CalendarEventDetailsModel>;
  } {
    const component = fixture.componentInstance as unknown as {
      state: {
        draft: {
          model: WritableSignal<CalendarEventDetailsModel>;
          form: FieldTree<CalendarEventDetailsModel>;
        };
      };
    };
    return component.state.draft;
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

  function eventTextControls(): Array<HTMLInputElement | HTMLTextAreaElement> {
    return Array.from(
      fixture.nativeElement.querySelectorAll('app-input input, app-input textarea'),
    );
  }

  function appButtonHosts(): HTMLElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll('app-button'));
  }

  function deleteButtonHost(): HTMLElement | null {
    return (
      appButtonHosts().find((host) => {
        const text = textContent(host);
        return text.includes('Delet') && !text.includes('thumbnail');
      }) ?? null
    );
  }

  function deleteButton(): HTMLButtonElement {
    return deleteButtonHost()!.querySelector('button')!;
  }

  function cancelButton(): HTMLButtonElement {
    return appButtonHosts()
      .find((host) => textContent(host).includes('Cancel'))!
      .querySelector('button')!;
  }

  function saveButton(): HTMLButtonElement {
    return fixture.nativeElement.querySelector('button[type="submit"]');
  }

  function startDateInput(): HTMLInputElement {
    return fixture.nativeElement.querySelector('app-date input');
  }

  function platformPublishButton(): HTMLButtonElement {
    return fixture.nativeElement.querySelector('.platform-publish-button button');
  }

  async function routeExitDecision(): Promise<boolean> {
    return resolveCanDeactivate(fixture.componentInstance.canDeactivateWithPendingChanges());
  }

  function routeWithId(id: string | null): ActivatedRoute {
    return {
      snapshot: { paramMap: convertToParamMap(id === null ? {} : { calendarEventId: id }) },
    } as ActivatedRoute;
  }
});
