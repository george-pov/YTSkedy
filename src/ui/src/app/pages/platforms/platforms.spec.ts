import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { finalize, Observable, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, type Mock, vi } from 'vitest';

import {
  CreatePlatformRequest,
  CreatePlatformResponse,
  Platform,
  PlatformListResponse,
  PlatformNameConflictError,
  PlatformReferenceKeyConflictError,
  PlatformsService,
  UpdatePlatformRequest,
  UpdatePlatformResponse,
  WordPressCategoryListResponse,
  WordPressCategoryQuery,
} from 'src/app/shared/api/platforms/platforms-service';
import {
  TemplateListResponse,
  TemplatesService,
} from 'src/app/shared/api/templates/templates-service';
import { ConfirmationDialogService } from 'src/app/shared/components/confirmation-dialog/confirmation-dialog-service';
import { Input } from 'src/app/shared/components/input/input';
import { MaskedInput } from 'src/app/shared/components/masked-input/masked-input';
import { NotificationService } from 'src/app/shared/notifications/notification-service';
import {
  buttonByText as findButtonByText,
  clickRow,
  dataRows,
  resolveCanDeactivate,
  setInputValue,
  submitForm,
} from 'src/app/testing/dom-test-helpers';
import { Platforms } from './platforms';
import { PlatformFormModel, referenceKeyMaxLength } from './platforms.form';

describe('Platforms', () => {
  let fixture: ComponentFixture<Platforms>;
  let service: {
    list: Mock<() => Observable<PlatformListResponse>>;
    create: Mock<(request: CreatePlatformRequest) => Observable<CreatePlatformResponse>>;
    update: Mock<
      (
        type: Platform['type'],
        id: string,
        request: UpdatePlatformRequest,
      ) => Observable<UpdatePlatformResponse>
    >;
    listWordPressCategories: Mock<
      (
        platformId: string,
        query: WordPressCategoryQuery,
      ) => Observable<WordPressCategoryListResponse>
    >;
    delete: Mock<(type: Platform['type'], id: string) => Observable<void>>;
  };
  let templatesService: {
    list: Mock<(type?: Platform['type']) => Observable<TemplateListResponse>>;
  };
  let confirmation: { confirm: Mock<(data: unknown) => Observable<string | undefined>> };
  let notifications: { showSuccess: Mock<(message: string) => void> };

  beforeEach(() => {
    service = {
      list: vi.fn<() => Observable<PlatformListResponse>>(),
      create: vi.fn(),
      update: vi.fn(),
      listWordPressCategories: vi.fn(),
      delete: vi.fn(),
    };
    service.listWordPressCategories.mockReturnValue(
      of({ items: [], page: 1, pageSize: 25, total: 0, totalPages: 0 }),
    );
    templatesService = {
      list: vi.fn<(type?: Platform['type']) => Observable<TemplateListResponse>>(),
    };
    templatesService.list.mockReturnValue(of({ templates: [] }));
    confirmation = {
      confirm: vi.fn<(data: unknown) => Observable<string | undefined>>(),
    };
    confirmation.confirm.mockReturnValue(of('discard'));
    notifications = { showSuccess: vi.fn<(message: string) => void>() };
  });

  function youTubePlatform(overrides: Partial<Platform> = {}): Platform {
    return {
      id: 'id-1',
      name: 'Main YouTube channel',
      referenceKey: 'youTube1',
      type: 'YouTube',
      publishingContent: publishingContent(),
      publishSettings: {
        credentials: {
          clientId: 'client-id',
          clientSecretConfigured: true,
          refreshTokenConfigured: true,
          clientSecretDisplayValue: '*********A3B',
          refreshTokenDisplayValue: '*********Z9Y',
        },
        privacyStatus: 'private',
        selfDeclaredMadeForKids: false,
        categoryId: null,
        containsSyntheticMedia: false,
      },
      ...overrides,
    };
  }

  function wordPressPlatform(overrides: Partial<Platform> = {}): Platform {
    return {
      id: 'id-2',
      name: 'Company blog',
      referenceKey: 'blog-1',
      type: 'WordPress',
      publishingContent: publishingContent({
        titleTemplateId: 'wordpress-title-template',
        descriptionTemplateId: 'wordpress-description-template',
      }),
      publishSettings: {
        siteUrl: 'https://blog.example.test/',
        username: 'publisher',
        postStatus: 'draft',
        categoryIds: [],
        sticky: false,
        applicationPasswordConfigured: true,
        passwordDisplayValue: '*******',
      },
      ...overrides,
    };
  }

  function componentModel(): {
    set: (model: PlatformFormModel) => void;
    get: () => PlatformFormModel;
  } {
    const model = (
      fixture.componentInstance as unknown as {
        model: {
          set: (model: PlatformFormModel) => void;
          (): PlatformFormModel;
        };
      }
    ).model;

    return {
      set: (value) => model.set(value),
      get: () => model(),
    };
  }

  function validFormModel(overrides: Partial<PlatformFormModel>): PlatformFormModel {
    return {
      type: 'YouTube',
      name: 'Main YouTube channel',
      referenceKey: '',
      titleTemplateId: 'youtube-title-template',
      descriptionTemplateId: 'youtube-description-template',
      youTubeClientId: 'client-id',
      youTubeClientSecret: 'client-secret',
      youTubeRefreshToken: 'refresh-token',
      youTubeClientSecretConfigured: 'false',
      youTubeRefreshTokenConfigured: 'false',
      youTubeClientSecretDisplayValue: '',
      youTubeRefreshTokenDisplayValue: '',
      youTubePrivacyStatus: 'private',
      youTubeMadeForKids: 'false',
      youTubeCategoryId: '',
      youTubeContainsSyntheticMedia: 'false',
      wordPressSiteUrl: '',
      wordPressUsername: '',
      wordPressApplicationPassword: '',
      wordPressPostStatus: 'draft',
      wordPressCategoryIds: [],
      wordPressSticky: false,
      wordPressScheduleOffsetHours: '',
      wordPressApplicationPasswordConfigured: 'false',
      wordPressPasswordDisplayValue: '',
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
    return inputByLabel('Name');
  }

  function referenceKeyInput(): HTMLInputElement {
    return inputByLabel('Reference key');
  }

  function inputByLabel(label: string): HTMLInputElement {
    const field = [
      ...fixture.debugElement.queryAll(By.directive(Input)),
      ...fixture.debugElement.queryAll(By.directive(MaskedInput)),
    ].find((entry) => (entry.componentInstance as Input | MaskedInput).label() === label);

    if (field === undefined) {
      throw new Error(`Input with label '${label}' was not found.`);
    }

    return field.nativeElement.querySelector('input') as HTMLInputElement;
  }

  function buttonByText(text: string): HTMLButtonElement {
    return findButtonByText(fixture.nativeElement, text);
  }

  async function setValue(element: HTMLInputElement, value: string): Promise<void> {
    await setInputValue(fixture, element, value);
  }

  function setRequiredTemplateIds(
    titleTemplateId = 'youtube-title-template',
    descriptionTemplateId = 'youtube-description-template',
  ): void {
    componentModel().set({
      ...componentModel().get(),
      titleTemplateId,
      descriptionTemplateId,
    });
    fixture.detectChanges();
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
      imports: [Platforms],
      providers: [
        provideZonelessChangeDetection(),
        { provide: PlatformsService, useValue: service },
        { provide: TemplatesService, useValue: templatesService },
        { provide: ConfirmationDialogService, useValue: confirmation },
        { provide: NotificationService, useValue: notifications },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Platforms);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('loads platforms on init and renders a row per platform with type and name columns', async () => {
    service.list.mockReturnValue(of({ platforms: [youTubePlatform(), wordPressPlatform()] }));

    await createComponent();

    expect(service.list).toHaveBeenCalledTimes(1);

    const headers = Array.from(fixture.nativeElement.querySelectorAll('th')).map((th) =>
      (th as HTMLElement).textContent?.trim(),
    );
    expect(headers).toEqual(['Type', 'Name', 'Reference key']);
    expect(rows()).toHaveLength(2);
  });

  it('unsubscribes from pending platform and template loads when destroyed', async () => {
    const platformResponse = new Subject<PlatformListResponse>();
    const templateResponse = new Subject<TemplateListResponse>();
    const platformTeardown = vi.fn();
    const templateTeardown = vi.fn();
    service.list.mockReturnValue(platformResponse.pipe(finalize(platformTeardown)));
    templatesService.list.mockReturnValue(templateResponse.pipe(finalize(templateTeardown)));

    await createComponent();
    platformResponse.next({ platforms: [youTubePlatform()] });
    fixture.detectChanges();
    await fixture.whenStable();

    expect(templatesService.list).toHaveBeenCalledTimes(1);
    expect(platformTeardown).not.toHaveBeenCalled();
    expect(templateTeardown).not.toHaveBeenCalled();

    fixture.destroy();

    expect(platformTeardown).toHaveBeenCalledTimes(1);
    expect(templateTeardown).toHaveBeenCalledTimes(1);
    platformResponse.next({ platforms: [wordPressPlatform()] });
    platformResponse.error(new Error('late platform failure'));
    templateResponse.next({ templates: [] });
    templateResponse.error(new Error('late template failure'));
    expect(templatesService.list).toHaveBeenCalledTimes(1);
    expect(notifications.showSuccess).not.toHaveBeenCalled();
  });

  it('preselects the first sorted platform on init', async () => {
    service.list.mockReturnValue(
      of({
        platforms: [
          youTubePlatform({ id: 'id-1', name: 'Main YouTube channel', type: 'YouTube' }),
          wordPressPlatform({ id: 'id-2', name: 'Company blog', type: 'WordPress' }),
        ],
      }),
    );

    await createComponent();

    expect(editor()).not.toBeNull();
    expect(nameInput().value).toBe('Company blog');
  });

  it('keeps the editor closed when no platforms exist', async () => {
    service.list.mockReturnValue(of({ platforms: [] }));

    await createComponent();

    expect(editor()).toBeNull();
    expect(rows()).toHaveLength(0);
    expect(buttonByText('Add Platform')).not.toBeNull();
  });

  it('uses the approved action copy', async () => {
    service.list.mockReturnValue(of({ platforms: [youTubePlatform()] }));

    await createComponent();

    expect(buttonByText('Add Platform')).not.toBeNull();

    buttonByText('Add Platform').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(buttonByText('Cancel')).not.toBeNull();
    expect(buttonByText('Save platform')).not.toBeNull();

    await selectRow(0);

    expect(buttonByText('Delete')).not.toBeNull();
    expect(buttonByText('Save changes')).not.toBeNull();
  });

  it('disables save until a platform editor has pending changes', async () => {
    service.list.mockReturnValue(of({ platforms: [youTubePlatform()] }));

    await createComponent();

    buttonByText('Add Platform').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(buttonByText('Save platform').disabled).toBe(true);

    await setValue(nameInput(), 'Second channel');

    expect(buttonByText('Save platform').disabled).toBe(false);

    await selectRow(0);

    expect(buttonByText('Save changes').disabled).toBe(true);

    await setValue(nameInput(), '  Main YouTube channel  ');
    await setValue(inputByLabel('Client secret'), '   ');

    expect(buttonByText('Save changes').disabled).toBe(true);

    await setValue(inputByLabel('Client secret'), 'replacement-client-secret');

    expect(buttonByText('Save changes').disabled).toBe(false);
  });

  it('compares normalized WordPress sticky and scheduled offset settings', async () => {
    service.list.mockReturnValue(of({ platforms: [wordPressPlatform()] }));

    await createComponent();

    expect(buttonByText('Save changes').disabled).toBe(true);

    componentModel().set({ ...componentModel().get(), wordPressSticky: true });
    fixture.detectChanges();
    expect(buttonByText('Save changes').disabled).toBe(false);

    componentModel().set({
      ...componentModel().get(),
      wordPressSticky: false,
      wordPressScheduleOffsetHours: '24',
    });
    fixture.detectChanges();
    expect(buttonByText('Save changes').disabled).toBe(true);

    componentModel().set({
      ...componentModel().get(),
      wordPressPostStatus: 'future',
    });
    fixture.detectChanges();
    expect(buttonByText('Save changes').disabled).toBe(false);
  });

  it('does not save a clean platform editor submit', async () => {
    service.list.mockReturnValue(of({ platforms: [youTubePlatform()] }));

    await createComponent();
    await selectRow(0);
    await submitEditor();

    expect(service.update).not.toHaveBeenCalled();
  });

  it('renders a load error when platforms cannot be loaded', async () => {
    service.list.mockReturnValue(throwError(() => new Error('Request failed')));

    await createComponent();

    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
    expect(rows()).toHaveLength(0);
  });

  it('opens a YouTube platform in an edit form with a read-only type and its settings', async () => {
    service.list.mockReturnValue(of({ platforms: [youTubePlatform()] }));

    await createComponent();
    await selectRow(0);

    expect(editor()).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.readonly-type')?.textContent).toContain('YouTube');
    expect(nameInput().value).toBe('Main YouTube channel');
    expect(referenceKeyInput().value).toBe('youTube1');
    const inputs = Array.from(
      fixture.nativeElement.querySelectorAll('app-input input, app-masked-input input'),
    ) as HTMLInputElement[];
    expect(inputs.some((input) => input.value === 'client-id')).toBe(true);
    expect(inputs.some((input) => input.value === 'client-secret')).toBe(false);
    expect(inputs.some((input) => input.value === 'refresh-token')).toBe(false);
    expect(fixture.nativeElement.textContent).not.toContain('*********A3B');
    expect(fixture.nativeElement.textContent).not.toContain('*********Z9Y');
    expect(inputByLabel('Client secret').value).toBe('*********A3B');
    expect(inputByLabel('Refresh token').value).toBe('*********Z9Y');
  });

  it('loads templates filtered by the editor platform type', async () => {
    service.list.mockReturnValue(of({ platforms: [] }));

    await createComponent();
    buttonByText('Add Platform').click();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(templatesService.list).toHaveBeenCalledWith('YouTube');
  });

  it('opens a platform edit form with its selected publishing content templates', async () => {
    service.list.mockReturnValue(
      of({
        platforms: [
          youTubePlatform({
            publishingContent: {
              titleTemplateId: 'youtube-title-template',
              descriptionTemplateId: 'youtube-description-template',
            },
          }),
        ],
      }),
    );

    await createComponent();
    await selectRow(0);

    expect(componentModel().get().titleTemplateId).toBe('youtube-title-template');
    expect(componentModel().get().descriptionTemplateId).toBe('youtube-description-template');
  });

  it('resets incompatible create-mode template ids when the platform type changes', async () => {
    service.list.mockReturnValue(of({ platforms: [] }));
    templatesService.list.mockImplementation((type) =>
      of({
        templates:
          type === 'WordPress'
            ? [
                {
                  id: 'wordpress-description-template',
                  name: 'WordPress description',
                  type: 'WordPress',
                  content: '{{ title }}',
                },
              ]
            : [
                {
                  id: 'youtube-title-template',
                  name: 'YouTube title',
                  type: 'YouTube',
                  content: '{{ title }}',
                },
              ],
      }),
    );

    await createComponent();
    buttonByText('Add Platform').click();
    fixture.detectChanges();
    await fixture.whenStable();

    componentModel().set(
      validFormModel({
        titleTemplateId: 'youtube-title-template',
        descriptionTemplateId: 'youtube-description-template',
      }),
    );
    fixture.detectChanges();

    componentModel().set(
      validFormModel({
        type: 'WordPress',
        titleTemplateId: 'youtube-title-template',
        descriptionTemplateId: 'youtube-description-template',
      }),
    );
    fixture.detectChanges();
    await fixture.whenStable();

    expect(componentModel().get().titleTemplateId).toBe('');
    expect(componentModel().get().descriptionTemplateId).toBe('');
    expect(templatesService.list).toHaveBeenLastCalledWith('WordPress');
  });

  it('opens a WordPress platform in an edit form with redacted settings', async () => {
    service.list.mockReturnValue(of({ platforms: [wordPressPlatform()] }));

    await createComponent();
    await selectRow(0);

    const inputs = Array.from(
      fixture.nativeElement.querySelectorAll('app-input input, app-masked-input input'),
    ) as HTMLInputElement[];

    expect(fixture.nativeElement.textContent).not.toContain('not available');
    expect(fixture.nativeElement.querySelector('.readonly-type')?.textContent).toContain(
      'WordPress',
    );
    expect(inputs.some((input) => input.value === 'https://blog.example.test/')).toBe(true);
    expect(inputs.some((input) => input.value === 'publisher')).toBe(true);
    expect(inputs.some((input) => input.value === 'application-password')).toBe(false);
    expect(fixture.nativeElement.textContent).not.toContain('*******');
    expect(inputByLabel('Application Password').value).toBe('*******');
    expect(componentModel().get().wordPressPostStatus).toBe('draft');
    expect(
      (fixture.nativeElement.querySelector('app-checkbox input') as HTMLInputElement).checked,
    ).toBe(false);
  });

  it('restores Scheduled settings from an edit response', async () => {
    service.list.mockReturnValue(
      of({
        platforms: [
          wordPressPlatform({
            publishSettings: {
              siteUrl: 'https://blog.example.test/',
              username: 'publisher',
              postStatus: 'future',
              categoryIds: [],
              sticky: true,
              scheduleOffsetHours: 24,
              applicationPasswordConfigured: true,
              passwordDisplayValue: '*******',
            },
          }),
        ],
      }),
    );

    await createComponent();

    expect(componentModel().get().wordPressPostStatus).toBe('future');
    expect(inputByLabel('Hours before event start').value).toBe('24');
    expect(
      (fixture.nativeElement.querySelector('app-checkbox input') as HTMLInputElement).checked,
    ).toBe(true);
  });

  it('creates a platform and adds it to the list', async () => {
    service.list.mockReturnValue(of({ platforms: [] }));
    service.create.mockReturnValue(
      of({
        id: 'new-id',
        name: 'Second channel',
        referenceKey: 'youTube1',
        type: 'YouTube',
        publishSettings: {
          credentials: {
            clientId: 'second-client-id',
            clientSecretConfigured: true,
            refreshTokenConfigured: true,
            clientSecretDisplayValue: '*********S3C',
            refreshTokenDisplayValue: '*********T0K',
          },
          privacyStatus: 'private',
          selfDeclaredMadeForKids: false,
        },
        publishingContent: publishingContent({
          titleTemplateId: 'youtube-title-template',
          descriptionTemplateId: 'youtube-description-template',
        }),
      }),
    );

    await createComponent();
    buttonByText('Add Platform').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    await setValue(nameInput(), 'Second channel');
    await setValue(referenceKeyInput(), ' youTube1 ');
    await setValue(inputByLabel('Client ID'), 'second-client-id');
    await setValue(inputByLabel('Client secret'), 'second-client-secret');
    await setValue(inputByLabel('Refresh token'), 'second-refresh-token');
    componentModel().set({
      ...componentModel().get(),
      titleTemplateId: 'youtube-title-template',
      descriptionTemplateId: 'youtube-description-template',
    });
    fixture.detectChanges();

    await submitEditor();

    expect(service.create).toHaveBeenCalledTimes(1);
    const request = service.create.mock.calls[0][0];
    expect(request).toMatchObject({
      name: 'Second channel',
      referenceKey: 'youTube1',
      type: 'YouTube',
      publishSettings: {
        credentials: {
          clientId: 'second-client-id',
          clientSecret: 'second-client-secret',
          refreshToken: 'second-refresh-token',
        },
        privacyStatus: 'private',
        selfDeclaredMadeForKids: false,
      },
      publishingContent: {
        titleTemplateId: 'youtube-title-template',
        descriptionTemplateId: 'youtube-description-template',
      },
    });
    expect(rows()).toHaveLength(1);
    expect(notifications.showSuccess).toHaveBeenCalledWith('Platform created.');
    expect(await canDeactivate()).toBe(true);
    expect(confirmation.confirm).not.toHaveBeenCalled();
  });

  it('creates a WordPress platform with Scheduled provider settings', async () => {
    service.list.mockReturnValue(of({ platforms: [] }));
    templatesService.list.mockImplementation((type) =>
      of({
        templates:
          type === 'WordPress'
            ? [
                {
                  id: 'wordpress-description-template',
                  name: 'WordPress description',
                  type: 'WordPress',
                  content: '{{ title }}',
                },
                {
                  id: 'wordpress-title-template',
                  name: 'WordPress title',
                  type: 'WordPress',
                  content: '{{ title }}',
                },
              ]
            : [],
      }),
    );
    service.create.mockReturnValue(
      of({
        id: 'new-id',
        name: 'Company blog',
        referenceKey: 'blog-1',
        type: 'WordPress',
        publishSettings: {
          siteUrl: 'https://blog.example.test/',
          username: 'publisher',
          postStatus: 'future',
          categoryIds: [],
          sticky: true,
          scheduleOffsetHours: 24,
          applicationPasswordConfigured: true,
          passwordDisplayValue: '*******',
        },
        publishingContent: publishingContent({
          titleTemplateId: 'wordpress-title-template',
          descriptionTemplateId: 'wordpress-description-template',
        }),
      }),
    );

    await createComponent();
    buttonByText('Add Platform').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    componentModel().set({
      type: 'WordPress',
      name: 'Company blog',
      referenceKey: ' blog-1 ',
      titleTemplateId: 'wordpress-title-template',
      descriptionTemplateId: 'wordpress-description-template',
      youTubeClientId: '',
      youTubeClientSecret: '',
      youTubeRefreshToken: '',
      youTubeClientSecretConfigured: 'false',
      youTubeRefreshTokenConfigured: 'false',
      youTubeClientSecretDisplayValue: '',
      youTubeRefreshTokenDisplayValue: '',
      youTubePrivacyStatus: 'private',
      youTubeMadeForKids: 'false',
      youTubeCategoryId: '',
      youTubeContainsSyntheticMedia: 'false',
      wordPressSiteUrl: ' https://blog.example.test/ ',
      wordPressUsername: ' publisher ',
      wordPressApplicationPassword: 'local-test-password',
      wordPressPostStatus: 'future',
      wordPressCategoryIds: [],
      wordPressSticky: true,
      wordPressScheduleOffsetHours: '24',
      wordPressApplicationPasswordConfigured: 'false',
      wordPressPasswordDisplayValue: '',
    });
    fixture.detectChanges();

    await submitEditor();

    expect(service.create).toHaveBeenCalledWith({
      name: 'Company blog',
      referenceKey: 'blog-1',
      type: 'WordPress',
      publishSettings: {
        siteUrl: 'https://blog.example.test/',
        username: 'publisher',
        postStatus: 'future',
        categoryIds: [],
        sticky: true,
        scheduleOffsetHours: 24,
        applicationPassword: 'local-test-password',
      },
      publishingContent: {
        titleTemplateId: 'wordpress-title-template',
        descriptionTemplateId: 'wordpress-description-template',
      },
    });
    expect(rows()).toHaveLength(1);
    expect(fixture.nativeElement.textContent).not.toContain(
      'Save the WordPress platform before choosing categories.',
    );
    expect(fixture.nativeElement.querySelector('app-chip-list')).not.toBeNull();
  });

  it('requires Hours before event start for Scheduled WordPress settings', async () => {
    service.list.mockReturnValue(
      of({
        platforms: [
          wordPressPlatform({
            publishSettings: {
              siteUrl: 'https://blog.example.test/',
              username: 'publisher',
              postStatus: 'future',
              categoryIds: [],
              sticky: false,
              applicationPasswordConfigured: true,
              passwordDisplayValue: '*******',
            },
          }),
        ],
      }),
    );

    await createComponent();
    await setValue(nameInput(), 'Company blog updated');
    await submitEditor();

    expect(service.update).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain(
      'Hours before event start is required for Scheduled posts.',
    );
  });

  it('updates a WordPress platform without sending a blank Application Password', async () => {
    service.list.mockReturnValue(of({ platforms: [wordPressPlatform()] }));
    service.update.mockReturnValue(
      of({
        id: 'id-2',
        name: 'Company blog updated',
        referenceKey: 'blog-1',
        type: 'WordPress',
        publishSettings: {
          siteUrl: 'https://blog.example.test/',
          username: 'publisher',
          postStatus: 'future',
          categoryIds: [],
          sticky: true,
          scheduleOffsetHours: 24,
          applicationPasswordConfigured: true,
          passwordDisplayValue: '*******',
        },
        publishingContent: publishingContent({
          titleTemplateId: 'wordpress-title-template',
          descriptionTemplateId: 'wordpress-description-template',
        }),
      }),
    );

    await createComponent();
    await selectRow(0);
    await setValue(nameInput(), 'Company blog updated');
    componentModel().set({
      ...componentModel().get(),
      wordPressPostStatus: 'future',
      wordPressCategoryIds: [],
      wordPressSticky: true,
      wordPressScheduleOffsetHours: '24',
    });
    fixture.detectChanges();

    await submitEditor();

    expect(service.update).toHaveBeenCalledWith('WordPress', 'id-2', {
      name: 'Company blog updated',
      referenceKey: 'blog-1',
      publishSettings: {
        siteUrl: 'https://blog.example.test/',
        username: 'publisher',
        postStatus: 'future',
        categoryIds: [],
        sticky: true,
        scheduleOffsetHours: 24,
      },
      publishingContent: publishingContent({
        titleTemplateId: 'wordpress-title-template',
        descriptionTemplateId: 'wordpress-description-template',
      }),
    });
  });

  it('treats category edits as dirty, saves them, and resets the saved baseline', async () => {
    service.list.mockReturnValue(
      of({
        platforms: [
          wordPressPlatform({
            publishSettings: {
              siteUrl: 'https://blog.example.test/',
              username: 'publisher',
              postStatus: 'draft',
              categoryIds: [12],
              sticky: false,
              applicationPasswordConfigured: true,
              passwordDisplayValue: '*******',
            },
          }),
        ],
      }),
    );
    service.update.mockReturnValue(
      of(
        wordPressPlatform({
          publishSettings: {
            siteUrl: 'https://blog.example.test/',
            username: 'publisher',
            postStatus: 'draft',
            categoryIds: [12, 34],
            sticky: false,
            applicationPasswordConfigured: true,
            passwordDisplayValue: '*******',
          },
        }),
      ),
    );

    await createComponent();
    componentModel().set({ ...componentModel().get(), wordPressCategoryIds: [12, 34] });
    fixture.detectChanges();

    expect(buttonByText('Save changes').disabled).toBe(false);
    await submitEditor();

    expect(service.update).toHaveBeenCalledWith(
      'WordPress',
      'id-2',
      expect.objectContaining({
        publishSettings: expect.objectContaining({ categoryIds: [12, 34] }),
      }),
    );
    expect(componentModel().get().wordPressCategoryIds).toEqual([12, 34]);
    expect(buttonByText('Save changes').disabled).toBe(true);
  });

  it('restores saved category IDs after discarding category edits and reopening the row', async () => {
    service.list.mockReturnValue(
      of({
        platforms: [
          wordPressPlatform({
            publishSettings: {
              siteUrl: 'https://blog.example.test/',
              username: 'publisher',
              postStatus: 'draft',
              categoryIds: [12],
              sticky: false,
              applicationPasswordConfigured: true,
              passwordDisplayValue: '*******',
            },
          }),
        ],
      }),
    );
    confirmation.confirm.mockReturnValue(of('discard'));

    await createComponent();
    componentModel().set({ ...componentModel().get(), wordPressCategoryIds: [12, 34] });
    fixture.detectChanges();
    buttonByText('Cancel').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(editor()).toBeNull();
    await selectRow(0);
    expect(componentModel().get().wordPressCategoryIds).toEqual([12]);
  });

  it('updates a WordPress platform with a replacement Application Password when supplied', async () => {
    service.list.mockReturnValue(of({ platforms: [wordPressPlatform()] }));
    service.update.mockReturnValue(
      of({
        id: 'id-2',
        name: 'Company blog',
        referenceKey: null,
        type: 'WordPress',
        publishSettings: {
          siteUrl: 'https://blog.example.test/',
          username: 'publisher',
          postStatus: 'publish',
          categoryIds: [],
          sticky: false,
          applicationPasswordConfigured: true,
          passwordDisplayValue: '*******',
        },
        publishingContent: publishingContent({
          titleTemplateId: 'wordpress-title-template',
          descriptionTemplateId: 'wordpress-description-template',
        }),
      }),
    );

    await createComponent();
    await selectRow(0);

    componentModel().set({
      type: 'WordPress',
      name: 'Company blog',
      referenceKey: '',
      titleTemplateId: 'wordpress-title-template',
      descriptionTemplateId: 'wordpress-description-template',
      youTubeClientId: '',
      youTubeClientSecret: '',
      youTubeRefreshToken: '',
      youTubeClientSecretConfigured: 'false',
      youTubeRefreshTokenConfigured: 'false',
      youTubeClientSecretDisplayValue: '',
      youTubeRefreshTokenDisplayValue: '',
      youTubePrivacyStatus: 'private',
      youTubeMadeForKids: 'false',
      youTubeCategoryId: '',
      youTubeContainsSyntheticMedia: 'false',
      wordPressSiteUrl: 'https://blog.example.test/',
      wordPressUsername: 'publisher',
      wordPressApplicationPassword: 'replacement-local-password',
      wordPressPostStatus: 'publish',
      wordPressCategoryIds: [],
      wordPressSticky: false,
      wordPressScheduleOffsetHours: '24',
      wordPressApplicationPasswordConfigured: 'true',
      wordPressPasswordDisplayValue: '*******',
    });
    fixture.detectChanges();

    await submitEditor();

    expect(service.update).toHaveBeenCalledWith('WordPress', 'id-2', {
      name: 'Company blog',
      referenceKey: null,
      publishSettings: {
        siteUrl: 'https://blog.example.test/',
        username: 'publisher',
        postStatus: 'publish',
        categoryIds: [],
        sticky: false,
        applicationPassword: 'replacement-local-password',
      },
      publishingContent: publishingContent({
        titleTemplateId: 'wordpress-title-template',
        descriptionTemplateId: 'wordpress-description-template',
      }),
    });
  });

  it('refreshes redacted YouTube values from the update response', async () => {
    service.list.mockReturnValue(of({ platforms: [youTubePlatform()] }));
    service.update.mockReturnValue(
      of({
        id: 'id-1',
        name: 'Main YouTube channel',
        referenceKey: 'youTube1',
        type: 'YouTube',
        publishSettings: {
          credentials: {
            clientId: 'client-id',
            clientSecretConfigured: true,
            refreshTokenConfigured: true,
            clientSecretDisplayValue: '*********N3W',
            refreshTokenDisplayValue: '*********Z9Y',
          },
          privacyStatus: 'private',
          selfDeclaredMadeForKids: false,
        },
        publishingContent: publishingContent(),
      }),
    );

    await createComponent();
    await selectRow(0);

    await setValue(inputByLabel('Client secret'), 'stored-client-secret-N3W');
    await submitEditor();

    expect(service.update).toHaveBeenCalledWith('YouTube', 'id-1', {
      name: 'Main YouTube channel',
      referenceKey: 'youTube1',
      publishSettings: {
        credentials: {
          clientId: 'client-id',
          clientSecret: 'stored-client-secret-N3W',
        },
        privacyStatus: 'private',
        selfDeclaredMadeForKids: false,
        categoryId: null,
        containsSyntheticMedia: false,
      },
      publishingContent: publishingContent(),
    });
    expect(inputByLabel('Client secret').value).toBe('*********N3W');
    expect(await canDeactivate()).toBe(true);
    expect(confirmation.confirm).not.toHaveBeenCalled();
  });

  it('surfaces a friendly message when the name is already taken', async () => {
    service.list.mockReturnValue(of({ platforms: [] }));
    service.create.mockReturnValue(throwError(() => new PlatformNameConflictError()));

    await createComponent();
    buttonByText('Add Platform').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    await setValue(nameInput(), 'Main YouTube channel');
    await setValue(inputByLabel('Client ID'), 'client-id');
    await setValue(inputByLabel('Client secret'), 'client-secret');
    await setValue(inputByLabel('Refresh token'), 'refresh-token');
    setRequiredTemplateIds();

    await submitEditor();

    const alert = fixture.nativeElement.querySelector('[role="alert"]');
    expect(alert).not.toBeNull();
    expect(alert.textContent).toContain('already exists');
  });

  it('requires both publishing content templates before saving', async () => {
    service.list.mockReturnValue(of({ platforms: [] }));

    await createComponent();
    buttonByText('Add Platform').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    await setValue(nameInput(), 'Second channel');
    await setValue(inputByLabel('Client ID'), 'client-id');
    await setValue(inputByLabel('Client secret'), 'client-secret');
    await setValue(inputByLabel('Refresh token'), 'refresh-token');

    await submitEditor();

    expect(service.create).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('Title template is required.');
    expect(fixture.nativeElement.textContent).toContain('Description template is required.');
  });

  it.each([
    ['bad_key', 'Reference key must use only letters, numbers, or hyphen.'],
    ['bad key', 'Reference key must use only letters, numbers, or hyphen.'],
    ['abcdefghijklmnop', `Reference key must be at most ${referenceKeyMaxLength} characters.`],
  ])('rejects invalid reference key value %s', async (referenceKey, message) => {
    service.list.mockReturnValue(of({ platforms: [] }));

    await createComponent();
    buttonByText('Add Platform').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    await setValue(nameInput(), 'Second channel');
    await setValue(referenceKeyInput(), referenceKey);
    await setValue(inputByLabel('Client ID'), 'client-id');
    await setValue(inputByLabel('Client secret'), 'client-secret');
    await setValue(inputByLabel('Refresh token'), 'refresh-token');

    await submitEditor();

    expect(service.create).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain(message);
  });

  it('accepts uppercase letters, digits, and hyphen in the reference key', async () => {
    service.list.mockReturnValue(of({ platforms: [] }));
    service.create.mockReturnValue(
      of({
        id: 'new-id',
        name: 'Second channel',
        referenceKey: 'YT-1',
        type: 'YouTube',
        publishSettings: {
          credentials: {
            clientId: 'client-id',
            clientSecretConfigured: true,
            refreshTokenConfigured: true,
          },
          privacyStatus: 'private',
          selfDeclaredMadeForKids: false,
        },
        publishingContent: publishingContent(),
      }),
    );

    await createComponent();
    buttonByText('Add Platform').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    await setValue(nameInput(), 'Second channel');
    await setValue(referenceKeyInput(), 'YT-1');
    await setValue(inputByLabel('Client ID'), 'client-id');
    await setValue(inputByLabel('Client secret'), 'client-secret');
    await setValue(inputByLabel('Refresh token'), 'refresh-token');
    setRequiredTemplateIds();

    await submitEditor();

    expect(service.create).toHaveBeenCalledWith(
      expect.objectContaining({
        referenceKey: 'YT-1',
      }),
    );
  });

  it('surfaces a friendly message when the reference key is already taken', async () => {
    service.list.mockReturnValue(of({ platforms: [] }));
    service.create.mockReturnValue(throwError(() => new PlatformReferenceKeyConflictError()));

    await createComponent();
    buttonByText('Add Platform').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    await setValue(nameInput(), 'Second channel');
    await setValue(referenceKeyInput(), 'youTube1');
    await setValue(inputByLabel('Client ID'), 'client-id');
    await setValue(inputByLabel('Client secret'), 'client-secret');
    await setValue(inputByLabel('Refresh token'), 'refresh-token');
    setRequiredTemplateIds();

    await submitEditor();

    const alert = fixture.nativeElement.querySelector('[role="alert"]');
    expect(alert).not.toBeNull();
    expect(alert.textContent).toContain('already exists');
  });

  it('deletes the selected platform and removes its row', async () => {
    service.list.mockReturnValue(of({ platforms: [youTubePlatform()] }));
    service.delete.mockReturnValue(of(undefined));

    await createComponent();
    await selectRow(0);

    buttonByText('Delete').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(service.delete).toHaveBeenCalledWith('YouTube', 'id-1');
    expect(rows()).toHaveLength(0);
    expect(notifications.showSuccess).toHaveBeenCalledWith('Platform deleted.');
  });

  it('allows clean route exit without prompting', async () => {
    service.list.mockReturnValue(of({ platforms: [youTubePlatform()] }));

    await createComponent();
    await selectRow(0);

    expect(await canDeactivate()).toBe(true);
    expect(confirmation.confirm).not.toHaveBeenCalled();
  });

  it('blocks route exit when dirty changes are kept', async () => {
    service.list.mockReturnValue(of({ platforms: [youTubePlatform()] }));
    confirmation.confirm.mockReturnValue(of('keep-editing'));

    await createComponent();
    await selectRow(0);
    await setValue(nameInput(), 'Changed channel');

    expect(await canDeactivate()).toBe(false);
    expect(confirmation.confirm).toHaveBeenCalledWith(
      expect.objectContaining({
        title: 'Discard unsaved platform changes?',
        actions: [
          { id: 'keep-editing', label: 'Keep editing' },
          { id: 'discard', label: 'Discard changes', primary: true },
        ],
      }),
    );
  });

  it('allows route exit when dirty changes are discarded', async () => {
    service.list.mockReturnValue(of({ platforms: [youTubePlatform()] }));
    confirmation.confirm.mockReturnValue(of('discard'));

    await createComponent();
    await selectRow(0);
    await setValue(nameInput(), 'Changed channel');

    expect(await canDeactivate()).toBe(true);
  });

  it('guards row switching until dirty changes are discarded', async () => {
    service.list.mockReturnValue(
      of({
        platforms: [
          youTubePlatform({ id: 'id-1', name: 'Main YouTube channel' }),
          youTubePlatform({
            id: 'id-2',
            name: 'Second YouTube channel',
            referenceKey: 'youTube2',
          }),
        ],
      }),
    );
    confirmation.confirm.mockReturnValue(of('keep-editing'));

    await createComponent();
    await selectRow(0);
    await setValue(nameInput(), 'Dirty channel');
    await selectRow(1);

    expect(nameInput().value).toBe('Dirty channel');

    confirmation.confirm.mockReturnValue(of('discard'));
    await selectRow(1);

    expect(nameInput().value).toBe('Second YouTube channel');
  });

  it('guards Add Platform until dirty changes are discarded', async () => {
    service.list.mockReturnValue(of({ platforms: [youTubePlatform()] }));
    confirmation.confirm.mockReturnValue(of('keep-editing'));

    await createComponent();
    await selectRow(0);
    await setValue(nameInput(), 'Dirty channel');

    buttonByText('Add Platform').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(nameInput().value).toBe('Dirty channel');

    confirmation.confirm.mockReturnValue(of('discard'));
    buttonByText('Add Platform').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-select')).not.toBeNull();
    expect(nameInput().value).toBe('');
  });

  it('keeps editing when Cancel discard is rejected and closes when it is confirmed', async () => {
    service.list.mockReturnValue(of({ platforms: [youTubePlatform()] }));
    confirmation.confirm.mockReturnValue(of('keep-editing'));

    await createComponent();
    await selectRow(0);
    await setValue(nameInput(), 'Dirty channel');

    buttonByText('Cancel').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(editor()).not.toBeNull();
    expect(nameInput().value).toBe('Dirty channel');

    confirmation.confirm.mockReturnValue(of('discard'));
    buttonByText('Cancel').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(editor()).toBeNull();
  });

  it('guards dirty editor delete until discard is confirmed', async () => {
    service.list.mockReturnValue(of({ platforms: [youTubePlatform()] }));
    service.delete.mockReturnValue(of(undefined));
    confirmation.confirm.mockReturnValue(of('keep-editing'));

    await createComponent();
    await selectRow(0);
    await setValue(nameInput(), 'Dirty channel');

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

  it('keeps blank replacement secrets clean in edit mode', async () => {
    service.list.mockReturnValue(of({ platforms: [youTubePlatform(), wordPressPlatform()] }));

    await createComponent();
    await selectRow(0);

    expect(await canDeactivate()).toBe(true);
    expect(confirmation.confirm).not.toHaveBeenCalled();

    await selectRow(1);

    expect(await canDeactivate()).toBe(true);
    expect(confirmation.confirm).not.toHaveBeenCalled();
  });

  it('treats replacement secret values as dirty', async () => {
    service.list.mockReturnValue(of({ platforms: [youTubePlatform()] }));
    confirmation.confirm.mockReturnValue(of('keep-editing'));

    await createComponent();
    await selectRow(0);
    await setValue(inputByLabel('Client secret'), 'replacement-client-secret');

    expect(await canDeactivate()).toBe(false);
  });

  it('treats title and description template changes as dirty', async () => {
    service.list.mockReturnValue(of({ platforms: [youTubePlatform()] }));
    confirmation.confirm.mockReturnValue(of('keep-editing'));

    await createComponent();
    await selectRow(0);

    componentModel().set({
      ...componentModel().get(),
      titleTemplateId: 'changed-title-template',
    });
    fixture.detectChanges();

    expect(await canDeactivate()).toBe(false);

    confirmation.confirm.mockClear();
    componentModel().set({
      ...componentModel().get(),
      titleTemplateId: 'title-template',
      descriptionTemplateId: 'changed-description-template',
    });
    fixture.detectChanges();

    expect(await canDeactivate()).toBe(false);
  });

  function publishingContent(
    overrides: Partial<{ titleTemplateId: string; descriptionTemplateId: string }> = {},
  ) {
    return {
      titleTemplateId: 'title-template',
      descriptionTemplateId: 'description-template',
      ...overrides,
    };
  }
});
