import { HttpErrorResponse } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { finalize, Observable, of, Subject, throwError } from 'rxjs';
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
import { ConfirmationDialogService } from 'src/app/shared/components/confirmation-dialog/confirmation-dialog-service';
import { NotificationService } from 'src/app/shared/notifications/notification-service';
import {
  buttonByText as findButtonByText,
  clickRow,
  dataRows,
  resolveCanDeactivate,
  setInputValue,
  submitForm,
} from 'src/app/testing/dom-test-helpers';
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
  let confirmation: { confirm: Mock<(data: unknown) => Observable<string | undefined>> };
  let notifications: { showSuccess: Mock<(message: string) => void> };

  beforeEach(() => {
    service = {
      list: vi.fn<() => Observable<TemplateListResponse>>(),
      create: vi.fn(),
      update: vi.fn(),
      delete: vi.fn(),
    };
    confirmation = {
      confirm: vi.fn<(data: unknown) => Observable<string | undefined>>(),
    };
    confirmation.confirm.mockReturnValue(of('discard'));
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
    return dataRows(fixture.nativeElement);
  }

  function editor(): HTMLElement | null {
    return fixture.nativeElement.querySelector('form.editor');
  }

  function nameInput(): HTMLInputElement {
    return fixture.nativeElement.querySelector('app-input input') as HTMLInputElement;
  }

  function contentTextarea(): HTMLTextAreaElement {
    return fixture.nativeElement.querySelector('app-input textarea') as HTMLTextAreaElement;
  }

  function buttonByText(text: string): HTMLButtonElement {
    return findButtonByText(fixture.nativeElement, text);
  }

  async function setValue(
    element: HTMLInputElement | HTMLTextAreaElement,
    value: string,
  ): Promise<void> {
    await setInputValue(fixture, element, value);
  }

  async function selectRow(index: number): Promise<void> {
    await clickRow(fixture, index);
  }

  async function submitEditor(): Promise<void> {
    await submitForm(fixture, 'form.editor');
  }

  async function canDeactivate(): Promise<boolean> {
    return resolveCanDeactivate(fixture.componentInstance.canDeactivateWithPendingChanges());
  }

  async function createComponent(): Promise<void> {
    await TestBed.configureTestingModule({
      imports: [Templates],
      providers: [
        provideZonelessChangeDetection(),
        { provide: TemplatesService, useValue: service },
        { provide: ConfirmationDialogService, useValue: confirmation },
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

    const headers = Array.from(fixture.nativeElement.querySelectorAll('th')).map((th) =>
      (th as HTMLElement).textContent?.trim(),
    );
    expect(headers).toEqual(['Type', 'Name']);
    expect(rows()).toHaveLength(2);
  });

  it('unsubscribes from a pending template load when destroyed', async () => {
    const response = new Subject<TemplateListResponse>();
    const teardown = vi.fn();
    service.list.mockReturnValue(response.pipe(finalize(teardown)));

    await createComponent();

    expect(teardown).not.toHaveBeenCalled();
    fixture.destroy();
    expect(teardown).toHaveBeenCalledTimes(1);

    response.next({ templates: [template()] });
    response.error(new Error('late failure'));
    expect(notifications.showSuccess).not.toHaveBeenCalled();
  });

  it('preselects the first sorted template on init', async () => {
    service.list.mockReturnValue(
      of({
        templates: [
          template({ id: 'id-1', name: 'Weeknight stream', type: 'YouTube' }),
          template({ id: 'id-2', name: 'New blog post', type: 'WordPress' }),
        ],
      }),
    );

    await createComponent();

    expect(editor()).not.toBeNull();
    expect(nameInput().value).toBe('New blog post');
    expect(contentTextarea().value).toContain('Live at');
  });

  it('keeps the editor closed when no templates exist', async () => {
    service.list.mockReturnValue(of({ templates: [] }));

    await createComponent();

    expect(editor()).toBeNull();
    expect(rows()).toHaveLength(0);
    expect(buttonByText('Add Template')).not.toBeNull();
  });

  it('renders a load error when templates cannot be loaded', async () => {
    service.list.mockReturnValue(throwError(() => new Error('Request failed')));

    await createComponent();

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

    buttonByText('Add Template').click();
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
    service.create.mockReturnValue(of({ id: 'new-id', name: 'Weeknight stream', type: 'YouTube' }));

    await createComponent();

    buttonByText('Add Template').click();
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
    expect(await canDeactivate()).toBe(true);
    expect(confirmation.confirm).not.toHaveBeenCalled();
  });

  it('updates the selected template without changing its type', async () => {
    service.list.mockReturnValue(
      of({
        templates: [template({ id: 'id-1', name: 'New blog post', type: 'WordPress' })],
      }),
    );
    service.update.mockReturnValue(of({ id: 'id-1', name: 'New blog post', type: 'WordPress' }));

    await createComponent();
    await selectRow(0);

    await setValue(contentTextarea(), 'Updated body');
    await submitEditor();

    expect(service.update).toHaveBeenCalledWith('WordPress', 'id-1', {
      name: 'New blog post',
      content: 'Updated body',
    });
    expect(notifications.showSuccess).toHaveBeenCalledWith('Template saved.');
    expect(await canDeactivate()).toBe(true);
    expect(confirmation.confirm).not.toHaveBeenCalled();
  });

  it('deletes the selected template and closes the editor', async () => {
    service.list.mockReturnValue(of({ templates: [template({ id: 'id-1', type: 'YouTube' })] }));
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

    buttonByText('Add Template').click();
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

  it('uses the approved action copy', async () => {
    service.list.mockReturnValue(of({ templates: [template()] }));

    await createComponent();

    expect(buttonByText('Add Template')).not.toBeNull();

    buttonByText('Add Template').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(buttonByText('Cancel')).not.toBeNull();
    expect(buttonByText('Save template')).not.toBeNull();

    await selectRow(0);

    expect(buttonByText('Delete')).not.toBeNull();
    expect(buttonByText('Save changes')).not.toBeNull();
  });

  it('disables save until a template editor has pending changes', async () => {
    service.list.mockReturnValue(of({ templates: [template()] }));

    await createComponent();

    buttonByText('Add Template').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(buttonByText('Save template').disabled).toBe(true);

    await setValue(nameInput(), 'New template');

    expect(buttonByText('Save template').disabled).toBe(false);

    await selectRow(0);

    expect(buttonByText('Save changes').disabled).toBe(true);

    await setValue(nameInput(), '  Weeknight stream  ');

    expect(buttonByText('Save changes').disabled).toBe(true);

    await setValue(contentTextarea(), 'Changed content');

    expect(buttonByText('Save changes').disabled).toBe(false);
  });

  it('does not save a clean template editor submit', async () => {
    service.list.mockReturnValue(of({ templates: [template()] }));

    await createComponent();
    await selectRow(0);
    await submitEditor();

    expect(service.update).not.toHaveBeenCalled();
  });

  it('allows clean route exit without prompting', async () => {
    service.list.mockReturnValue(of({ templates: [template()] }));

    await createComponent();
    await selectRow(0);

    expect(await canDeactivate()).toBe(true);
    expect(confirmation.confirm).not.toHaveBeenCalled();
  });

  it('blocks route exit when dirty changes are kept', async () => {
    service.list.mockReturnValue(of({ templates: [template()] }));
    confirmation.confirm.mockReturnValue(of('keep-editing'));

    await createComponent();
    await selectRow(0);
    await setValue(contentTextarea(), 'Changed content');

    expect(await canDeactivate()).toBe(false);
    expect(confirmation.confirm).toHaveBeenCalledWith(
      expect.objectContaining({
        title: 'Discard unsaved template changes?',
        actions: [
          { id: 'keep-editing', label: 'Keep editing' },
          { id: 'discard', label: 'Discard changes', primary: true },
        ],
      }),
    );
  });

  it('allows route exit when dirty changes are discarded', async () => {
    service.list.mockReturnValue(of({ templates: [template()] }));
    confirmation.confirm.mockReturnValue(of('discard'));

    await createComponent();
    await selectRow(0);
    await setValue(contentTextarea(), 'Changed content');

    expect(await canDeactivate()).toBe(true);
  });

  it('blocks route exit while template save and delete mutations are active', async () => {
    const update = new Subject<UpdateTemplateResponse>();
    service.list.mockReturnValue(of({ templates: [template()] }));
    service.update.mockReturnValue(update.asObservable());

    await createComponent();
    await selectRow(0);
    await setValue(contentTextarea(), 'Changed content');
    await submitEditor();

    expect(await canDeactivate()).toBe(false);
    update.error(new Error('save failed'));

    confirmation.confirm.mockReturnValue(of('discard'));
    const deletion = new Subject<void>();
    service.delete.mockReturnValue(deletion.asObservable());
    (
      fixture.componentInstance as unknown as {
        deleteSelected(): void;
      }
    ).deleteSelected();

    expect(await canDeactivate()).toBe(false);
    deletion.error(new Error('delete failed'));
    expect((fixture.componentInstance as unknown as { isDeleting: () => boolean }).isDeleting()).toBe(
      false,
    );
  });

  it('allows route exit while the initial template read is active', async () => {
    service.list.mockReturnValue(new Subject<TemplateListResponse>());

    await createComponent();

    expect(await canDeactivate()).toBe(true);
  });

  it('guards row switching until dirty changes are discarded', async () => {
    service.list.mockReturnValue(
      of({
        templates: [
          template({ id: 'id-1', name: 'First template', content: 'First content' }),
          template({ id: 'id-2', name: 'Second template', content: 'Second content' }),
        ],
      }),
    );
    confirmation.confirm.mockReturnValue(of('keep-editing'));

    await createComponent();
    await selectRow(0);
    await setValue(contentTextarea(), 'Changed first content');
    await selectRow(1);

    expect(contentTextarea().value).toBe('Changed first content');

    confirmation.confirm.mockReturnValue(of('discard'));
    await selectRow(1);

    expect(nameInput().value).toBe('Second template');
    expect(contentTextarea().value).toBe('Second content');
  });

  it('guards Add Template until dirty changes are discarded', async () => {
    service.list.mockReturnValue(of({ templates: [template()] }));
    confirmation.confirm.mockReturnValue(of('keep-editing'));

    await createComponent();
    await selectRow(0);
    await setValue(nameInput(), 'Dirty name');

    buttonByText('Add Template').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(nameInput().value).toBe('Dirty name');

    confirmation.confirm.mockReturnValue(of('discard'));
    buttonByText('Add Template').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-select')).not.toBeNull();
    expect(nameInput().value).toBe('');
  });

  it('keeps editing when Cancel discard is rejected and closes when it is confirmed', async () => {
    service.list.mockReturnValue(of({ templates: [template()] }));
    confirmation.confirm.mockReturnValue(of('keep-editing'));

    await createComponent();
    await selectRow(0);
    await setValue(contentTextarea(), 'Dirty content');

    buttonByText('Cancel').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(editor()).not.toBeNull();
    expect(contentTextarea().value).toBe('Dirty content');

    confirmation.confirm.mockReturnValue(of('discard'));
    buttonByText('Cancel').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(editor()).toBeNull();
  });

  it('guards dirty editor delete until discard is confirmed', async () => {
    service.list.mockReturnValue(of({ templates: [template()] }));
    service.delete.mockReturnValue(of(undefined));
    confirmation.confirm.mockReturnValue(of('keep-editing'));

    await createComponent();
    await selectRow(0);
    await setValue(contentTextarea(), 'Dirty content');

    buttonByText('Delete').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(service.delete).not.toHaveBeenCalled();
    expect(editor()).not.toBeNull();

    confirmation.confirm.mockReturnValue(of('discard'));
    buttonByText('Delete').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(service.delete).toHaveBeenCalledWith('YouTube', 'id-1');
    expect(editor()).toBeNull();
  });
});
