import { provideZonelessChangeDetection, type WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideLuxonDateAdapter } from '@angular/material-luxon-adapter';
import { finalize, firstValueFrom, Observable, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, type Mock, vi } from 'vitest';

import {
  CalendarEventStartDefaultsResponse,
  CalendarEventStartDefaultsService,
  UpdateCalendarEventStartDefaultsRequest,
} from 'src/app/shared/api/settings/calendar-event-start-defaults-service';
import {
  EventTextFieldsResponse,
  EventTextFieldsService,
  UpdateEventTextFieldsRequest,
} from 'src/app/shared/api/settings/event-text-fields-service';
import { ConfirmationDialogService } from 'src/app/shared/components/confirmation-dialog/confirmation-dialog-service';
import { NotificationService } from 'src/app/shared/notifications/notification-service';
import { Settings } from './settings';
import { type StartDefaultsModel } from './start-defaults.form';

describe('Settings', () => {
  let fixture: ComponentFixture<Settings>;
  let service: {
    get: Mock<() => Observable<EventTextFieldsResponse>>;
    update: Mock<(request: UpdateEventTextFieldsRequest) => Observable<EventTextFieldsResponse>>;
  };
  let confirmation: { confirm: Mock<(data: unknown) => Observable<string | undefined>> };
  let notifications: { showSuccess: Mock<(message: string) => void> };
  let startDefaults: {
    get: Mock<() => Observable<CalendarEventStartDefaultsResponse>>;
    update: Mock<
      (request: UpdateCalendarEventStartDefaultsRequest) =>
        Observable<CalendarEventStartDefaultsResponse>
    >;
  };

  beforeEach(() => {
    service = {
      get: vi.fn<() => Observable<EventTextFieldsResponse>>(),
      update:
        vi.fn<(request: UpdateEventTextFieldsRequest) => Observable<EventTextFieldsResponse>>(),
    };
    service.get.mockReturnValue(of(defaultFields()));
    startDefaults = {
      get: vi.fn().mockReturnValue(
        of({ dayOfWeek: null, localTime: null, timeZoneId: null }),
      ),
      update: vi.fn(),
    };
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
        { provide: EventTextFieldsService, useValue: service },
        { provide: CalendarEventStartDefaultsService, useValue: startDefaults },
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

  function buttonByText(label: string): HTMLButtonElement {
    const button = Array.from(fixture.nativeElement.querySelectorAll('app-button button')).find(
      (entry) => ((entry as HTMLElement).textContent ?? '').includes(label),
    );

    if (button === undefined) {
      throw new Error(`Button '${label}' was not found.`);
    }

    return button as HTMLButtonElement;
  }

  function deleteButtons(): HTMLElement[] {
    return Array.from(
      fixture.nativeElement.querySelectorAll('.delete-field-button'),
    ) as HTMLElement[];
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

  it('loads and renders the default event text fields', async () => {
    await createComponent();

    expect(service.get).toHaveBeenCalledTimes(1);
    expect(startDefaults.get).toHaveBeenCalledTimes(1);
    expect(text()).toContain('text1');
    expect(text()).toContain('text2');
    expect(inputs().map((input) => input.value)).toEqual(['Title', '50', 'Description', '2500']);
  });

  it('renders and saves the independent new calendar event defaults section', async () => {
    startDefaults.get.mockReturnValue(
      of({ dayOfWeek: 'Friday', localTime: '09:05', timeZoneId: 'UTC' }),
    );
    startDefaults.update.mockReturnValue(
      of({ dayOfWeek: null, localTime: null, timeZoneId: null }),
    );
    await createComponent();

    expect(text()).toContain('New calendar event defaults');
    expect(text()).toContain('Default weekday');
    const state = fixture.componentInstance as unknown as {
      startDefaultsModel: WritableSignal<StartDefaultsModel>;
    };
    state.startDefaultsModel.set({ dayOfWeek: '', localTime: '', timeZoneId: '' });
    fixture.detectChanges();
    const forms = fixture.nativeElement.querySelectorAll('form');
    forms[1].dispatchEvent(new Event('submit'));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(startDefaults.update).toHaveBeenCalledWith({
      dayOfWeek: null,
      localTime: null,
      timeZoneId: null,
    });
    expect(service.update).not.toHaveBeenCalled();
    expect(notifications.showSuccess).toHaveBeenCalledWith(
      'New calendar event defaults saved.',
    );
  });

  it('unsubscribes from a pending load when destroyed', async () => {
    const response = new Subject<EventTextFieldsResponse>();
    const teardown = vi.fn();
    service.get.mockReturnValue(response.pipe(finalize(teardown)));

    await createComponent();

    expect(teardown).not.toHaveBeenCalled();
    fixture.destroy();
    expect(teardown).toHaveBeenCalledTimes(1);

    response.next(defaultFields());
    response.error(new Error('late failure'));
    expect(notifications.showSuccess).not.toHaveBeenCalled();
  });

  it('uses the approved footer action copy', async () => {
    await createComponent();

    expect(buttonByText('Cancel')).not.toBeNull();
    expect(buttonByText('Save changes')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.app-actions.app-actions-end')).not.toBeNull();
  });

  it('disables save until settings have pending changes', async () => {
    await createComponent();

    expect(buttonByText('Save changes').disabled).toBe(true);

    await setValue(inputAt(0), '  Title  ');

    expect(buttonByText('Save changes').disabled).toBe(true);

    await setValue(inputAt(0), 'Stream title');

    expect(buttonByText('Save changes').disabled).toBe(false);
  });

  it('does not save a clean settings submit', async () => {
    await createComponent();
    await submit();

    expect(service.update).not.toHaveBeenCalled();
  });

  it('appends a field with the next derived key immediately', async () => {
    await createComponent();

    buttonByText('Add field').click();
    fixture.detectChanges();

    expect(text()).toContain('text3');
    expect(inputs()).toHaveLength(6);
  });

  it('saves edited labels and max lengths', async () => {
    service.update.mockReturnValue(of(defaultFields()));
    await createComponent();

    await setValue(inputAt(0), ' Stream title ');
    await setValue(inputAt(1), '80');

    await submit();

    expect(service.update).toHaveBeenCalledWith({
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
    });
  });

  it('saves remaining fields after delete', async () => {
    service.get.mockReturnValue(
      of({
        fields: [
          { fieldKey: 'text1', label: 'Title', type: 'ShortText', maxLength: 50 },
          { fieldKey: 'text2', label: 'Summary', type: 'ShortText', maxLength: 100 },
          { fieldKey: 'text3', label: 'Description', type: 'LongText', maxLength: 2500 },
        ],
      }),
    );
    service.update.mockReturnValue(of(defaultFields()));
    await createComponent();

    deleteButtons()[1].dispatchEvent(new Event('click'));
    fixture.detectChanges();

    await submit();

    expect(service.update).toHaveBeenCalledWith({
      fields: [
        { fieldKey: 'text1', label: 'Title', type: 'ShortText', maxLength: 50 },
        { fieldKey: 'text2', label: 'Description', type: 'LongText', maxLength: 2500 },
      ],
    });
    expect(text()).not.toContain('text3');
  });

  it('replaces local fields with the backend-normalized save response', async () => {
    service.update.mockReturnValue(
      of({
        fields: [
          {
            fieldKey: 'text1',
            label: 'Normalized title',
            type: 'ShortText',
            maxLength: 90,
          },
        ],
      }),
    );
    await createComponent();
    await setValue(inputAt(0), 'Draft title');

    await submit();

    expect(service.update).toHaveBeenCalledWith({
      fields: [
        {
          fieldKey: 'text1',
          label: 'Draft title',
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
    });
    expect(inputs().map((input) => input.value)).toEqual(['Normalized title', '90']);
    expect(notifications.showSuccess).toHaveBeenCalledWith('Event text fields saved.');
    expect(await canDeactivate()).toBe(true);
    expect(confirmation.confirm).not.toHaveBeenCalled();
  });

  it('shows a save error and keeps the editor open when save fails', async () => {
    service.update.mockReturnValue(throwError(() => new Error('Request failed')));
    await createComponent();
    await setValue(inputAt(0), 'Stream title');

    await submit();

    expect(text()).toContain('Event text fields could not be saved.');
    expect(fixture.nativeElement.querySelector('form')).not.toBeNull();
  });

  it('blocks save when max length is invalid', async () => {
    await createComponent();

    await setValue(inputAt(1), '0');
    await submit();

    expect(service.update).not.toHaveBeenCalled();
    expect(text()).toContain('Max length must be a positive whole number.');
  });

  it('allows clean route exit without prompting', async () => {
    await createComponent();

    expect(await canDeactivate()).toBe(true);
    expect(confirmation.confirm).not.toHaveBeenCalled();
  });

  it('blocks route exit when dirty changes are kept', async () => {
    confirmation.confirm.mockReturnValue(of('keep-editing'));
    await createComponent();

    await setValue(inputAt(0), 'Stream title');

    expect(await canDeactivate()).toBe(false);
    expect(confirmation.confirm).toHaveBeenCalledWith(
      expect.objectContaining({
        title: 'Discard unsaved settings changes?',
        actions: [
          { id: 'keep-editing', label: 'Keep editing' },
          { id: 'discard', label: 'Discard changes', primary: true },
        ],
      }),
    );
  });

  it('allows route exit when dirty changes are discarded', async () => {
    confirmation.confirm.mockReturnValue(of('discard'));
    await createComponent();

    await setValue(inputAt(0), 'Stream title');

    expect(await canDeactivate()).toBe(true);
  });

  it('keeps editing when Cancel discard is rejected and restores saved fields when confirmed', async () => {
    confirmation.confirm.mockReturnValue(of('keep-editing'));
    await createComponent();

    await setValue(inputAt(0), 'Stream title');

    buttonByText('Cancel').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(inputAt(0).value).toBe('Stream title');

    confirmation.confirm.mockReturnValue(of('discard'));
    buttonByText('Cancel').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(inputs().map((input) => input.value)).toEqual(['Title', '50', 'Description', '2500']);
  });

  it('tracks add, delete, and renumber changes as pending', async () => {
    confirmation.confirm.mockReturnValue(of('keep-editing'));
    service.get.mockReturnValue(
      of({
        fields: [
          { fieldKey: 'text1', label: 'Title', type: 'ShortText', maxLength: 50 },
          { fieldKey: 'text2', label: 'Summary', type: 'ShortText', maxLength: 100 },
          { fieldKey: 'text3', label: 'Description', type: 'LongText', maxLength: 2500 },
        ],
      }),
    );
    await createComponent();

    buttonByText('Add field').click();
    fixture.detectChanges();

    expect(await canDeactivate()).toBe(false);

    confirmation.confirm.mockReturnValue(of('discard'));
    buttonByText('Cancel').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    confirmation.confirm.mockReturnValue(of('keep-editing'));

    deleteButtons()[1].dispatchEvent(new Event('click'));
    fixture.detectChanges();

    expect(text()).not.toContain('text3');
    expect(await canDeactivate()).toBe(false);
  });

  it('clears save errors when Cancel discards changes', async () => {
    service.update.mockReturnValue(throwError(() => new Error('Request failed')));
    confirmation.confirm.mockReturnValue(of('discard'));
    await createComponent();

    await setValue(inputAt(0), 'Stream title');
    await submit();

    expect(text()).toContain('Event text fields could not be saved.');

    buttonByText('Cancel').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(text()).not.toContain('Event text fields could not be saved.');
    expect(inputAt(0).value).toBe('Title');
  });

  function defaultFields(): EventTextFieldsResponse {
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
