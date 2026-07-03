import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, type Mock, vi } from 'vitest';

import {
  EventTextFieldsResponse,
  EventTextFieldsService,
  UpdateEventTextFieldsRequest,
} from 'src/app/shared/api/settings/event-text-fields-service';
import { NotificationService } from 'src/app/shared/notifications/notification-service';
import { Settings } from './settings';

describe('Settings', () => {
  let fixture: ComponentFixture<Settings>;
  let service: {
    get: Mock<() => Observable<EventTextFieldsResponse>>;
    update: Mock<(request: UpdateEventTextFieldsRequest) => Observable<EventTextFieldsResponse>>;
  };
  let notifications: { showSuccess: Mock<(message: string) => void> };

  beforeEach(() => {
    service = {
      get: vi.fn<() => Observable<EventTextFieldsResponse>>(),
      update:
        vi.fn<(request: UpdateEventTextFieldsRequest) => Observable<EventTextFieldsResponse>>(),
    };
    service.get.mockReturnValue(of(defaultFields()));
    notifications = { showSuccess: vi.fn<(message: string) => void>() };
  });

  async function createComponent(): Promise<void> {
    await TestBed.configureTestingModule({
      imports: [Settings],
      providers: [
        provideZonelessChangeDetection(),
        { provide: EventTextFieldsService, useValue: service },
        { provide: NotificationService, useValue: notifications },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Settings);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
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
    expect(text()).toContain('text1');
    expect(text()).toContain('text2');
    expect(inputs().map((input) => input.value)).toEqual(['Title', '50', 'Description', '2500']);
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

    await submit();

    expect(inputs().map((input) => input.value)).toEqual(['Normalized title', '90']);
    expect(notifications.showSuccess).toHaveBeenCalledWith('Event text fields saved.');
  });

  it('shows a save error and keeps the editor open when save fails', async () => {
    service.update.mockReturnValue(throwError(() => new Error('Request failed')));
    await createComponent();

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
