import { HttpErrorResponse } from '@angular/common/http';
import { provideZonelessChangeDetection, type WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { type FieldTree } from '@angular/forms/signals';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { Observable, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, type Mock, vi } from 'vitest';

import {
  CalendarEvent,
  CalendarEventsService,
  CreateCalendarEventRequest,
  CreateCalendarEventResponse,
  UpdateCalendarEventRequest,
  UpdateCalendarEventResponse,
} from 'src/app/shared/api/calendar-events/calendar-events-service';
import { NotificationService } from 'src/app/shared/notifications/notification-service';
import { CalendarEventDetails } from './calendar-event-details';
import { CalendarEventDetailsModel } from './calendar-event-details.form';

describe('CalendarEventDetails', () => {
  let fixture: ComponentFixture<CalendarEventDetails>;
  let service: {
    create: Mock<(request: CreateCalendarEventRequest) => Observable<CreateCalendarEventResponse>>;
    getById: Mock<(calendarEventId: string) => Observable<CalendarEvent>>;
    update: Mock<
      (
        calendarEventId: string,
        request: UpdateCalendarEventRequest,
      ) => Observable<UpdateCalendarEventResponse>
    >;
    delete: Mock<(calendarEventId: string) => Observable<void>>;
  };
  let notifications: { showSuccess: Mock<(message: string) => void> };
  let navigations: string[];

  beforeEach(() => {
    service = {
      create:
        vi.fn<(request: CreateCalendarEventRequest) => Observable<CreateCalendarEventResponse>>(),
      getById: vi.fn<(calendarEventId: string) => Observable<CalendarEvent>>(),
      update:
        vi.fn<
          (
            calendarEventId: string,
            request: UpdateCalendarEventRequest,
          ) => Observable<UpdateCalendarEventResponse>
        >(),
      delete: vi.fn<(calendarEventId: string) => Observable<void>>(),
    };
    notifications = { showSuccess: vi.fn<(message: string) => void>() };
    navigations = [];

    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: CalendarEventsService, useValue: service },
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
      descriptions: {
        en: { title: 'English title', description: 'English description' },
        ru: { title: 'Russian title', description: 'Russian description' },
      },
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

  it('blocks submit and reveals required errors when the form is empty', async () => {
    await submitForm();

    expect(service.create).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('Start date is required.');
    expect(fixture.nativeElement.textContent).toContain('English title is required.');
    expect(navigations).toEqual([]);
  });

  it('blocks submit when a title exceeds the max length', async () => {
    fillValidForm();
    api().form.descriptions.en.title().value.set('a'.repeat(101));

    await submitForm();

    expect(service.create).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('English title is too long.');
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
    api().form.descriptions.en.title().value.set('  English title  ');

    await submitForm();

    expect(service.create).toHaveBeenCalledTimes(1);
    expect(service.create).toHaveBeenCalledWith({
      start: { localDateTime: '2999-01-01T10:00:00', timeZoneId: 'UTC' },
      descriptions: [
        {
          language: 'en',
          title: 'English title',
          description: 'English description',
        },
        {
          language: 'ru',
          title: 'Russian title',
          description: 'Russian description',
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
    const editId = '20260606T170000Z';

    function createEditComponent(): void {
      TestBed.resetTestingModule();
      TestBed.configureTestingModule({
        providers: [
          provideZonelessChangeDetection(),
          provideRouter([]),
          { provide: CalendarEventsService, useValue: service },
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

    it('loads the event by id and populates the form', () => {
      service.getById.mockReturnValue(of(sampleEvent()));

      createEditComponent();

      expect(service.getById).toHaveBeenCalledWith(editId);
      expect(api().model()).toEqual({
        start: {
          date: '2030-07-04',
          time: '09:30',
          timeZoneId: 'Europe/London',
        },
        descriptions: {
          en: { title: 'English title', description: 'English description' },
          ru: { title: 'Russian title', description: 'Russian description' },
        },
      });
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

    it('disables the scheduled start controls in edit mode', () => {
      service.getById.mockReturnValue(of(sampleEvent()));

      createEditComponent();

      expect(api().form.start().disabled()).toBe(true);
    });

    it('keeps Save enabled and updates descriptions on submit', async () => {
      service.getById.mockReturnValue(of(sampleEvent()));
      service.update.mockReturnValue(of({ calendarEventId: editId }));

      createEditComponent();

      const save = fixture.nativeElement.querySelector(
        'button[type="submit"]',
      ) as HTMLButtonElement;
      expect(save.disabled).toBe(false);

      api().form.descriptions.en.title().value.set('  Updated English title  ');

      fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));
      fixture.detectChanges();
      await fixture.whenStable();

      expect(service.create).not.toHaveBeenCalled();
      expect(service.update).toHaveBeenCalledTimes(1);
      expect(service.update).toHaveBeenCalledWith(editId, {
        descriptions: [
          {
            language: 'en',
            title: 'Updated English title',
            description: 'English description',
          },
          {
            language: 'ru',
            title: 'Russian title',
            description: 'Russian description',
          },
        ],
      });
      expect(notifications.showSuccess).toHaveBeenCalledWith('Calendar event updated.');
      expect(navigations).toEqual(['/calendar-events']);
    });

    it('disables Save when the loaded event is not updatable', () => {
      service.getById.mockReturnValue(of(sampleEvent({ canUpdate: false })));

      createEditComponent();

      const save = fixture.nativeElement.querySelector(
        'button[type="submit"]',
      ) as HTMLButtonElement;
      expect(save.disabled).toBe(true);
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
      service.getById.mockReturnValue(new Subject<CalendarEvent>());

      createEditComponent();

      expect(fixture.nativeElement.querySelector('app-progress-bar')).not.toBeNull();
      expect(fixture.nativeElement.querySelector('form')).toBeNull();
    });

    it('enables delete when the loaded event is deletable', () => {
      service.getById.mockReturnValue(of(sampleEvent({ canDelete: true })));

      createEditComponent();

      expect(deleteButton()).not.toBeNull();
      expect(deleteButton()!.disabled).toBe(false);
    });

    it('disables delete when the loaded event is not deletable', () => {
      service.getById.mockReturnValue(of(sampleEvent({ canDelete: false })));

      createEditComponent();

      expect(deleteButton()!.disabled).toBe(true);
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

    it('deletes a future published event immediately without confirmation', async () => {
      service.getById.mockReturnValue(of(sampleEvent({ status: 'Published', canDelete: true })));
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

    it('shows a conflict message and stays on the page when no longer deletable', async () => {
      service.getById.mockReturnValue(of(sampleEvent()));
      service.delete.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 409 })));

      createEditComponent();

      deleteButtonHost()!.dispatchEvent(new Event('click'));
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.textContent).toContain(
        'The event can no longer be deleted. Reload the page and try again.',
      );
      expect(navigations).toEqual([]);
      expect(notifications.showSuccess).not.toHaveBeenCalled();
    });

    it('shows a YouTube failure message and stays on the page on 502', async () => {
      service.getById.mockReturnValue(of(sampleEvent()));
      service.delete.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 502 })));

      createEditComponent();

      deleteButtonHost()!.dispatchEvent(new Event('click'));
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.textContent).toContain(
        'The YouTube broadcast could not be deleted. Try again later.',
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

  function sampleEvent(overrides: Partial<CalendarEvent> = {}): CalendarEvent {
    return {
      calendarEventId: '20260606T170000Z',
      start: {
        localDateTime: '2030-07-04T09:30:00',
        timeZoneId: 'Europe/London',
      },
      scheduledStartUtc: '2030-07-04T08:30:00+00:00',
      descriptions: [
        {
          language: 'en',
          title: 'English title',
          description: 'English description',
        },
        {
          language: 'ru',
          title: 'Russian title',
          description: 'Russian description',
        },
      ],
      status: 'Draft',
      canPublish: true,
      canUpdate: true,
      canDelete: true,
      ...overrides,
    };
  }
});
