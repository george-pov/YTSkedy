import { HttpErrorResponse } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, type Mock, vi } from 'vitest';

import {
  CreateTemplateRequest,
  CreateTemplateResponse,
  Template,
  TemplateListResponse,
  TemplatesService,
  UpdateTemplateRequest,
  UpdateTemplateResponse,
} from 'src/app/shared/api/templates/templates-service';
import { NotificationService } from 'src/app/shared/notifications/notification-service';
import { Templates } from './templates';

describe('Templates', () => {
  let fixture: ComponentFixture<Templates>;
  let service: {
    list: Mock<() => Observable<TemplateListResponse>>;
    create: Mock<(request: CreateTemplateRequest) => Observable<CreateTemplateResponse>>;
    update: Mock<
      (
        type: Template['type'],
        id: string,
        request: UpdateTemplateRequest,
      ) => Observable<UpdateTemplateResponse>
    >;
    delete: Mock<(type: Template['type'], id: string) => Observable<void>>;
  };
  let notifications: { showSuccess: Mock<(message: string) => void> };

  beforeEach(() => {
    service = {
      list: vi.fn<() => Observable<TemplateListResponse>>(),
      create: vi.fn(),
      update: vi.fn(),
      delete: vi.fn(),
    };
    notifications = { showSuccess: vi.fn<(message: string) => void>() };
  });

  function template(overrides: Partial<Template> = {}): Template {
    return {
      id: 'id-1',
      name: 'Weeknight stream',
      type: 'YouTube',
      content: 'Live at {{ localizedTime }}',
      ...overrides,
    };
  }

  function rows(): HTMLElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll('tr')).filter(
      (row) => {
        const element = row as HTMLElement;
        // Exclude the Material no-data row, which is also a `tr > td`.
        return (
          element.querySelector('td') !== null &&
          !element.classList.contains('empty-row')
        );
      },
    ) as HTMLElement[];
  }

  function editor(): HTMLElement | null {
    return fixture.nativeElement.querySelector('form.editor');
  }

  function nameInput(): HTMLInputElement {
    return fixture.nativeElement.querySelector('app-input input') as HTMLInputElement;
  }

  function contentTextarea(): HTMLTextAreaElement {
    return fixture.nativeElement.querySelector(
      'app-input textarea',
    ) as HTMLTextAreaElement;
  }

  function buttonByText(text: string): HTMLButtonElement {
    return Array.from(
      fixture.nativeElement.querySelectorAll('app-button button'),
    ).find((button) =>
      ((button as HTMLElement).textContent ?? '').trim().includes(text),
    ) as HTMLButtonElement;
  }

  async function setValue(
    element: HTMLInputElement | HTMLTextAreaElement,
    value: string,
  ): Promise<void> {
    element.value = value;
    element.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    fixture.detectChanges();
  }

  async function selectRow(index: number): Promise<void> {
    rows()[index].dispatchEvent(new Event('click'));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  async function submitEditor(): Promise<void> {
    editor()!.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  async function createComponent(): Promise<void> {
    await TestBed.configureTestingModule({
      imports: [Templates],
      providers: [
        provideZonelessChangeDetection(),
        { provide: TemplatesService, useValue: service },
        { provide: NotificationService, useValue: notifications },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Templates);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('loads templates on init and renders a row per template with type and name columns', async () => {
    service.list.mockReturnValue(
      of({
        templates: [
          template({ id: 'id-1', name: 'Weeknight stream', type: 'YouTube' }),
          template({ id: 'id-2', name: 'New blog post', type: 'WordPress' }),
        ],
      }),
    );

    await createComponent();

    expect(service.list).toHaveBeenCalledTimes(1);

    const headers = Array.from(fixture.nativeElement.querySelectorAll('th')).map(
      (th) => (th as HTMLElement).textContent?.trim(),
    );
    expect(headers).toEqual(['Type', 'Name']);
    expect(rows()).toHaveLength(2);
  });

  it('hides the editor until a template is selected or New is clicked', async () => {
    service.list.mockReturnValue(of({ templates: [template()] }));

    await createComponent();

    expect(editor()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain(
      'Select a template on the left',
    );
  });

  it('renders a load error when templates cannot be loaded', async () => {
    service.list.mockReturnValue(throwError(() => new Error('Request failed')));

    await createComponent();

    expect(fixture.nativeElement.textContent).toContain('Templates could not be loaded.');
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
    expect(rows()).toHaveLength(0);
  });

  it('opens the selected template in an edit form with a read-only type', async () => {
    service.list.mockReturnValue(
      of({ templates: [template({ id: 'id-1', name: 'Weeknight stream' })] }),
    );

    await createComponent();
    await selectRow(0);

    expect(editor()).not.toBeNull();
    // The type is immutable on edit, so it is shown as read-only text, not a select.
    expect(fixture.nativeElement.querySelector('app-select')).toBeNull();
    expect(nameInput().value).toBe('Weeknight stream');
    expect(contentTextarea().value).toContain('Live at');
  });

  it('opens an unsaved create form with an editable type and does not post', async () => {
    service.list.mockReturnValue(of({ templates: [template()] }));

    await createComponent();

    buttonByText('New Template').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(editor()).not.toBeNull();
    // The type is editable while creating.
    expect(fixture.nativeElement.querySelector('app-select')).not.toBeNull();
    expect(service.create).not.toHaveBeenCalled();
    // New does not add a row until the create succeeds.
    expect(rows()).toHaveLength(1);
  });

  it('creates a template on save and adds it to the list', async () => {
    service.list.mockReturnValue(of({ templates: [] }));
    service.create.mockReturnValue(
      of({ id: 'new-id', name: 'Weeknight stream', type: 'YouTube' }),
    );

    await createComponent();

    buttonByText('New Template').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    await setValue(nameInput(), 'Weeknight stream');
    await setValue(contentTextarea(), 'Live at 8');
    await submitEditor();

    expect(service.create).toHaveBeenCalledWith({
      name: 'Weeknight stream',
      type: 'YouTube',
      content: 'Live at 8',
    });
    expect(rows()).toHaveLength(1);
    expect(notifications.showSuccess).toHaveBeenCalledWith('Template created.');
  });

  it('updates the selected template without changing its type', async () => {
    service.list.mockReturnValue(
      of({
        templates: [
          template({ id: 'id-1', name: 'New blog post', type: 'WordPress' }),
        ],
      }),
    );
    service.update.mockReturnValue(
      of({ id: 'id-1', name: 'New blog post', type: 'WordPress' }),
    );

    await createComponent();
    await selectRow(0);

    await setValue(contentTextarea(), 'Updated body');
    await submitEditor();

    expect(service.update).toHaveBeenCalledWith('WordPress', 'id-1', {
      name: 'New blog post',
      content: 'Updated body',
    });
    expect(notifications.showSuccess).toHaveBeenCalledWith('Template saved.');
  });

  it('deletes the selected template and closes the editor', async () => {
    service.list.mockReturnValue(
      of({ templates: [template({ id: 'id-1', type: 'YouTube' })] }),
    );
    service.delete.mockReturnValue(of(undefined));

    await createComponent();
    await selectRow(0);

    buttonByText('Delete').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(service.delete).toHaveBeenCalledWith('YouTube', 'id-1');
    expect(rows()).toHaveLength(0);
    expect(editor()).toBeNull();
    expect(notifications.showSuccess).toHaveBeenCalledWith('Template deleted.');
  });

  it('shows a duplicate-name message when create returns 409', async () => {
    service.list.mockReturnValue(of({ templates: [] }));
    service.create.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 409, statusText: 'Conflict' })),
    );

    await createComponent();

    buttonByText('New Template').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    await setValue(nameInput(), 'Weeknight stream');
    await setValue(contentTextarea(), 'Live at 8');
    await submitEditor();

    expect(fixture.nativeElement.textContent).toContain('already exists');
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
    expect(rows()).toHaveLength(0);
  });
});

