import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, type Mock, vi } from 'vitest';

import {
  CalendarEventsService,
  CreateCalendarEventRequest,
  CreateCalendarEventResponse,
} from 'src/app/shared/api/calendar-events/calendar-events-service';
import { NotificationService } from 'src/app/shared/notifications/notification-service';
import { CalendarEventDetails } from './calendar-event-details';
import { CalendarEventDetailsForm } from './calendar-event-details.form';

describe('CalendarEventDetails', () => {
  let fixture: ComponentFixture<CalendarEventDetails>;
  let service: {
    create: Mock<
      (request: CreateCalendarEventRequest) => Observable<CreateCalendarEventResponse>
    >;
  };
  let notifications: { showSuccess: Mock<(message: string) => void> };
  let navigations: string[];

  beforeEach(() => {
    service = {
      create: vi.fn<
        (request: CreateCalendarEventRequest) => Observable<CreateCalendarEventResponse>
      >(),
    };
    notifications = { showSuccess: vi.fn<(message: string) => void>() };
    navigations = [];

    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: CalendarEventsService, useValue: service },
        { provide: NotificationService, useValue: notifications },
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

  function form(): CalendarEventDetailsForm {
    return (fixture.componentInstance as unknown as {
      form: CalendarEventDetailsForm;
    }).form;
  }

  function fillValidForm(): void {
    form().setValue({
      start: { date: '2999-01-01', time: '10:00', timeZoneId: 'UTC' },
      descriptions: {
        en: { title: 'English title', description: 'English description' },
        ru: { title: 'Russian title', description: 'Russian description' },
      },
    });
  }

  async function submitForm(): Promise<void> {
    fixture.nativeElement
      .querySelector('form')
      .dispatchEvent(new Event('submit'));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('blocks submit and reveals required errors when the form is empty', async () => {
    await submitForm();

    expect(service.create).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain(
      'Start date is required.',
    );
    expect(fixture.nativeElement.textContent).toContain(
      'English title is required.',
    );
    expect(navigations).toEqual([]);
  });

  it('blocks submit when a title exceeds the max length', async () => {
    fillValidForm();
    form().controls.descriptions.controls.en.controls.title.setValue(
      'a'.repeat(101),
    );

    await submitForm();

    expect(service.create).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain(
      'English title is too long.',
    );
  });

  it('blocks submit when the scheduled start is in the past', async () => {
    fillValidForm();
    form().controls.start.controls.date.setValue('2000-01-01');

    await submitForm();

    expect(service.create).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain(
      'Start must be in the future.',
    );
  });

  it('posts a contract-correct request and navigates to the list on success', async () => {
    service.create.mockReturnValue(of({ calendarEventId: '20990101T100000Z' }));
    fillValidForm();
    form().controls.descriptions.controls.en.controls.title.setValue(
      '  English title  ',
    );

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

    expect(notifications.showSuccess).toHaveBeenCalledWith(
      'Calendar event created.',
    );
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
});
