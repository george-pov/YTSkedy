import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideLuxonDateAdapter } from '@angular/material-luxon-adapter';
import { finalize, firstValueFrom, Observable, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, type Mock, vi } from 'vitest';

import {
  CalendarEventDefaultsResponse,
  CalendarEventDefaultsService,
  UpdateCalendarEventDefaultsRequest,
} from 'src/app/shared/api/settings/calendar-event-defaults-service';
import { ConfirmationDialogService } from 'src/app/shared/components/confirmation-dialog/confirmation-dialog-service';
import { NotificationService } from 'src/app/shared/notifications/notification-service';
import { Settings } from './settings';

describe('Settings', () => {
  let fixture: ComponentFixture<Settings>;
  let service: {
    get: Mock<() => Observable<CalendarEventDefaultsResponse>>;
    update: Mock<
      (request: UpdateCalendarEventDefaultsRequest) => Observable<CalendarEventDefaultsResponse>
    >;
  };
  let confirmation: { confirm: Mock<(data: unknown) => Observable<string | undefined>> };
  let notifications: { showSuccess: Mock<(message: string) => void> };

  beforeEach(() => {
    service = {
      get: vi.fn<() => Observable<CalendarEventDefaultsResponse>>(),
      update:
        vi.fn<
          (request: UpdateCalendarEventDefaultsRequest) => Observable<CalendarEventDefaultsResponse>
        >(),
    };
    service.get.mockReturnValue(of(defaultSettings()));
    service.update.mockImplementation((request) => of(request));
    confirmation = {
      confirm: vi.fn<(data: unknown) => Observable<string | undefined>>(),
    };
    confirmation.confirm.mockReturnValue(of('discard'));
    notifications = { showSuccess: vi.fn<(message: string) => void>() };
  });

  async function createComponent(): Promise<void> {
    await TestBed.configureTestingModule({
      imports: [Settings],
      providers: [
        provideZonelessChangeDetection(),
        provideLuxonDateAdapter(),
        { provide: CalendarEventDefaultsService, useValue: service },
        { provide: ConfirmationDialogService, useValue: confirmation },
        { provide: NotificationService, useValue: notifications },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Settings);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  async function canDeactivate(): Promise<boolean> {
    const result = fixture.componentInstance.canDeactivateWithPendingChanges();
    return typeof result === 'boolean' ? result : firstValueFrom(result);
  }

  function text(): string {
    return fixture.nativeElement.textContent;
  }

  function inputs(): HTMLInputElement[] {
    return Array.from(
      fixture.nativeElement.querySelectorAll('app-input input'),
    ) as HTMLInputElement[];
  }

  function inputAt(index: number): HTMLInputElement {
    const input = inputs()[index];
    if (input === undefined) {
      throw new Error(`Input at index ${index} was not found.`);
    }

    return input;
  }

  function buttonsByText(label: string): HTMLButtonElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll('app-button button')).filter((entry) =>
      ((entry as HTMLElement).textContent ?? '').includes(label),
    ) as HTMLButtonElement[];
  }

  function buttonByText(label: string): HTMLButtonElement {
    const button = buttonsByText(label)[0];
    if (button === undefined) {
      throw new Error(`Button '${label}' was not found.`);
    }

    return button;
  }

  function deleteButtons(): HTMLElement[] {
    return Array.from(
      fixture.nativeElement.querySelectorAll('.delete-field-button'),
    ) as HTMLElement[];
  }

  function timeInput(): HTMLInputElement {
    const input = fixture.nativeElement.querySelector('app-time input') as HTMLInputElement | null;
    if (input === null) {
      throw new Error('Default time input was not found.');
    }

    return input;
  }

  function startDefaultsSection(): HTMLElement {
    const section = fixture.nativeElement.querySelector(
      '[aria-labelledby="start-defaults-heading"]',
    ) as HTMLElement | null;
    if (section === null) {
      throw new Error('New calendar event defaults section was not found.');
    }

    return section;
  }

  async function setValue(element: HTMLInputElement, value: string): Promise<void> {
    element.value = value;
    element.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    fixture.detectChanges();
  }

  async function submit(): Promise<void> {
    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('loads and renders both settings sections with one request', async () => {
    service.get.mockReturnValue(
      of(
        defaultSettings({
          startDefaults: {
            dayOfWeek: 'Friday',
            localTime: '09:05',
            timeZoneId: 'UTC',
          },
        }),
      ),
    );

    await createComponent();

    expect(service.get).toHaveBeenCalledTimes(1);
    expect(text()).toContain('Event text fields');
    expect(text()).toContain('New calendar event defaults');
    expect(text()).toContain('text1');
    expect(text()).toContain('text2');
    expect(inputs().map((input) => input.value)).toEqual(['Title', '50', 'Description', '2500']);
    expect(startDefaultsSection().textContent).toContain('Friday');
    expect(timeInput().value).toContain('9:05');
    expect(startDefaultsSection().textContent).toContain('UTC');
  });

  it('shows one Save changes and one Cancel action', async () => {
    await createComponent();

    expect(buttonsByText('Save changes')).toHaveLength(1);
    expect(buttonsByText('Save defaults')).toHaveLength(0);
    expect(buttonsByText('Cancel')).toHaveLength(1);
    expect(fixture.nativeElement.querySelectorAll('form')).toHaveLength(1);
  });

  it('disables save and Cancel until either section has pending changes', async () => {
    await createComponent();

    expect(buttonByText('Save changes').disabled).toBe(true);
    expect(buttonByText('Cancel').disabled).toBe(true);

    await setValue(inputAt(0), '  Title  ');
    expect(buttonByText('Save changes').disabled).toBe(true);
    expect(buttonByText('Cancel').disabled).toBe(true);

    await setValue(inputAt(0), 'Stream title');
    expect(buttonByText('Save changes').disabled).toBe(false);
    expect(buttonByText('Cancel').disabled).toBe(false);

    await setValue(inputAt(0), 'Title');
    expect(buttonByText('Save changes').disabled).toBe(true);
    expect(buttonByText('Cancel').disabled).toBe(true);

    await setValue(timeInput(), '10:00');
    expect(buttonByText('Save changes').disabled).toBe(false);
    expect(buttonByText('Cancel').disabled).toBe(false);
  });

  it('does not confirm or restore a normalized-clean Cancel request', async () => {
    await createComponent();
    await setValue(inputAt(0), '  Title  ');

    expect(buttonByText('Cancel').disabled).toBe(true);

    buttonByText('Cancel').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(confirmation.confirm).not.toHaveBeenCalled();
    expect(inputAt(0).value).toBe('  Title  ');
    expect(service.update).not.toHaveBeenCalled();
  });

  it('does not save a clean settings submit', async () => {
    await createComponent();

    await submit();

    expect(service.update).not.toHaveBeenCalled();
  });

  it('saves both sections in one request', async () => {
    await createComponent();
    await setValue(inputAt(0), ' Stream title ');
    await setValue(inputAt(1), '80');
    await setValue(timeInput(), '10:00');

    await submit();

    expect(service.update).toHaveBeenCalledTimes(1);
    expect(service.update).toHaveBeenCalledWith({
      eventTextFields: {
        fields: [
          {
            fieldKey: 'text1',
            label: 'Stream title',
            type: 'ShortText',
            maxLength: 80,
          },
          {
            fieldKey: 'text2',
            label: 'Description',
            type: 'LongText',
            maxLength: 2500,
          },
        ],
      },
      startDefaults: {
        dayOfWeek: null,
        localTime: '10:00',
        timeZoneId: null,
      },
    });
    expect(notifications.showSuccess).toHaveBeenCalledWith('Settings saved.');
  });

  it('appends and deletes fields using the same pending settings request', async () => {
    confirmation.confirm.mockReturnValue(of('keep-editing'));
    service.get.mockReturnValue(
      of(
        defaultSettings({
          eventTextFields: {
            fields: [
              { fieldKey: 'text1', label: 'Title', type: 'ShortText', maxLength: 50 },
              { fieldKey: 'text2', label: 'Summary', type: 'ShortText', maxLength: 100 },
              { fieldKey: 'text3', label: 'Description', type: 'LongText', maxLength: 2500 },
            ],
          },
        }),
      ),
    );
    await createComponent();

    buttonByText('Add field').click();
    fixture.detectChanges();
    expect(text()).toContain('text4');
    expect(await canDeactivate()).toBe(false);

    confirmation.confirm.mockReturnValue(of('discard'));
    buttonByText('Cancel').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    deleteButtons()[1].dispatchEvent(new Event('click'));
    fixture.detectChanges();
    await submit();

    expect(service.update).toHaveBeenCalledWith(
      expect.objectContaining({
        eventTextFields: {
          fields: [
            { fieldKey: 'text1', label: 'Title', type: 'ShortText', maxLength: 50 },
            { fieldKey: 'text2', label: 'Description', type: 'LongText', maxLength: 2500 },
          ],
        },
      }),
    );
  });

  it('applies the complete backend-normalized save response', async () => {
    service.update.mockReturnValue(
      of({
        eventTextFields: {
          fields: [
            {
              fieldKey: 'text1',
              label: 'Normalized title',
              type: 'ShortText',
              maxLength: 90,
            },
          ],
        },
        startDefaults: {
          dayOfWeek: 'Monday',
          localTime: '11:30',
          timeZoneId: 'UTC',
        },
      }),
    );
    await createComponent();
    await setValue(inputAt(0), 'Draft title');
    await setValue(timeInput(), '08:15');

    await submit();

    expect(inputs().map((input) => input.value)).toEqual(['Normalized title', '90']);
    expect(startDefaultsSection().textContent).toContain('Monday');
    expect(timeInput().value).toContain('11:30');
    expect(startDefaultsSection().textContent).toContain('UTC');
    expect(buttonByText('Cancel').disabled).toBe(true);
    expect(buttonByText('Save changes').disabled).toBe(true);
    expect(await canDeactivate()).toBe(true);
  });

  it('shows one save error and keeps all pending changes when save fails', async () => {
    service.update.mockReturnValue(throwError(() => new Error('Request failed')));
    await createComponent();
    await setValue(inputAt(0), 'Stream title');
    await setValue(timeInput(), '10:00');

    await submit();

    expect(text()).toContain('Settings could not be saved.');
    expect(inputAt(0).value).toBe('Stream title');
    expect(timeInput().value).toContain('10:00');
    expect(buttonByText('Save changes').disabled).toBe(false);
  });

  it('resets invalid dirty settings and validation interaction state after confirmed Cancel', async () => {
    await createComponent();
    await setValue(inputAt(1), '0');
    await setValue(timeInput(), '10:00');

    await submit();

    expect(service.update).not.toHaveBeenCalled();
    expect(text()).toContain('Max length must be a positive whole number.');
    expect(timeInput().value).toContain('10:00');
    expect(buttonByText('Cancel').disabled).toBe(false);

    buttonByText('Cancel').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(inputs().map((input) => input.value)).toEqual(['Title', '50', 'Description', '2500']);
    expect(timeInput().value).toBe('');
    expect(text()).not.toContain('Max length must be a positive whole number.');
    expect(buttonByText('Cancel').disabled).toBe(true);
    expect(buttonByText('Save changes').disabled).toBe(true);
    expect(service.update).not.toHaveBeenCalled();
  });

  it('prompts before route exit when either section has pending changes', async () => {
    confirmation.confirm.mockReturnValue(of('keep-editing'));
    await createComponent();
    await setValue(timeInput(), '10:00');

    expect(await canDeactivate()).toBe(false);
    expect(confirmation.confirm).toHaveBeenCalledWith(
      expect.objectContaining({
        title: 'Discard unsaved settings changes?',
        body: 'Unsaved event text field and new calendar event default changes will be lost and cannot be recovered.',
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
  });

  it('blocks route exit while settings save is active and clears the flag on error', async () => {
    const update = new Subject<CalendarEventDefaultsResponse>();
    service.update.mockReturnValue(update.asObservable());
    await createComponent();
    await setValue(inputAt(0), 'Changed title');

    await submit();

    expect(await canDeactivate()).toBe(false);
    expect(buttonByText('Saving...').disabled).toBe(true);
    expect(buttonByText('Cancel').disabled).toBe(true);

    buttonByText('Cancel').click();
    fixture.detectChanges();

    expect(confirmation.confirm).not.toHaveBeenCalled();
    expect(inputAt(0).value).toBe('Changed title');
    update.error(new Error('save failed'));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(buttonByText('Save changes').disabled).toBe(false);
    expect(buttonByText('Cancel').disabled).toBe(false);
    expect(text()).toContain('Settings could not be saved.');
  });

  it('allows route exit while the initial settings read is active', async () => {
    service.get.mockReturnValue(new Subject<CalendarEventDefaultsResponse>());

    await createComponent();

    expect(await canDeactivate()).toBe(true);
    expect(buttonByText('Cancel').disabled).toBe(true);
    expect(buttonByText('Save changes').disabled).toBe(true);
    expect(service.update).not.toHaveBeenCalled();
    expect(confirmation.confirm).not.toHaveBeenCalled();
  });

  it('Cancel restores both settings sections and clears a superseded save error', async () => {
    service.update.mockReturnValue(throwError(() => new Error('Request failed')));
    confirmation.confirm.mockReturnValue(of('discard'));
    await createComponent();
    await setValue(inputAt(0), 'Stream title');
    await setValue(timeInput(), '10:00');
    await submit();

    expect(text()).toContain('Settings could not be saved.');
    expect(inputAt(0).value).toBe('Stream title');
    expect(timeInput().value).toContain('10:00');
    expect(buttonByText('Cancel').disabled).toBe(false);
    const originalUrl = window.location.href;
    window.history.replaceState({}, '', '/settings');

    try {
      buttonByText('Cancel').click();
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(inputs().map((input) => input.value)).toEqual(['Title', '50', 'Description', '2500']);
      expect(timeInput().value).toBe('');
      expect(text()).not.toContain('Settings could not be saved.');
      expect(buttonByText('Cancel').disabled).toBe(true);
      expect(buttonByText('Save changes').disabled).toBe(true);
      expect(window.location.pathname).toBe('/settings');
      expect(service.update).toHaveBeenCalledTimes(1);
    } finally {
      window.history.replaceState({}, '', originalUrl);
    }
  });

  it('Cancel keeps both settings sections when discard is rejected', async () => {
    confirmation.confirm.mockReturnValue(of('keep-editing'));
    await createComponent();
    await setValue(inputAt(0), 'Stream title');
    await setValue(timeInput(), '10:00');

    buttonByText('Cancel').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(inputAt(0).value).toBe('Stream title');
    expect(timeInput().value).toContain('10:00');
    expect(buttonByText('Cancel').disabled).toBe(false);
    expect(buttonByText('Save changes').disabled).toBe(false);
    expect(service.update).not.toHaveBeenCalled();
  });

  it('unsubscribes from a pending combined load when destroyed', async () => {
    const response = new Subject<CalendarEventDefaultsResponse>();
    const teardown = vi.fn();
    service.get.mockReturnValue(response.pipe(finalize(teardown)));

    await createComponent();

    expect(teardown).not.toHaveBeenCalled();
    fixture.destroy();
    expect(teardown).toHaveBeenCalledTimes(1);

    response.next(defaultSettings());
    response.error(new Error('late failure'));
    expect(notifications.showSuccess).not.toHaveBeenCalled();
  });

  function defaultSettings(
    overrides: Partial<CalendarEventDefaultsResponse> = {},
  ): CalendarEventDefaultsResponse {
    return {
      eventTextFields: {
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
      },
      startDefaults: { dayOfWeek: null, localTime: null, timeZoneId: null },
      ...overrides,
    };
  }
});
