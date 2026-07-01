import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';
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
} from 'src/app/shared/api/platforms/platforms-service';
import {
  TemplateListResponse,
  TemplatesService,
} from 'src/app/shared/api/templates/templates-service';
import { NotificationService } from 'src/app/shared/notifications/notification-service';
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
    delete: Mock<(type: Platform['type'], id: string) => Observable<void>>;
  };
  let templatesService: {
    list: Mock<(type?: Platform['type']) => Observable<TemplateListResponse>>;
  };
  let notifications: { showSuccess: Mock<(message: string) => void> };

  beforeEach(() => {
    service = {
      list: vi.fn<() => Observable<PlatformListResponse>>(),
      create: vi.fn(),
      update: vi.fn(),
      delete: vi.fn(),
    };
    templatesService = {
      list: vi.fn<(type?: Platform['type']) => Observable<TemplateListResponse>>(),
    };
    templatesService.list.mockReturnValue(of({ templates: [] }));
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
        },
        privacyStatus: 'private',
        selfDeclaredMadeForKids: false,
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
        applicationPasswordConfigured: true,
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
      youTubePrivacyStatus: 'private',
      youTubeMadeForKids: 'false',
      wordPressSiteUrl: '',
      wordPressUsername: '',
      wordPressApplicationPassword: '',
      wordPressPostStatus: 'draft',
      wordPressApplicationPasswordConfigured: 'false',
      ...overrides,
    };
  }

  function rows(): HTMLElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll('tr')).filter((row) => {
      const element = row as HTMLElement;
      // Exclude the Material no-data row, which is also a `tr > td`.
      return element.querySelector('td') !== null && !element.classList.contains('empty-row');
    }) as HTMLElement[];
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
    const field = Array.from(fixture.nativeElement.querySelectorAll('app-input')).find(
      (input) =>
        ((input as HTMLElement).querySelector('mat-label')?.textContent ?? '').trim() === label,
    ) as HTMLElement | undefined;

    if (field === undefined) {
      throw new Error(`Input with label '${label}' was not found.`);
    }

    return field.querySelector('input') as HTMLInputElement;
  }

  function buttonByText(text: string): HTMLButtonElement {
    return Array.from(fixture.nativeElement.querySelectorAll('app-button button')).find((button) =>
      ((button as HTMLElement).textContent ?? '').trim().includes(text),
    ) as HTMLButtonElement;
  }

  async function setValue(element: HTMLInputElement, value: string): Promise<void> {
    element.value = value;
    element.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    fixture.detectChanges();
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
      imports: [Platforms],
      providers: [
        provideZonelessChangeDetection(),
        { provide: PlatformsService, useValue: service },
        { provide: TemplatesService, useValue: templatesService },
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

  it('hides the editor until a platform is selected or New is clicked', async () => {
    service.list.mockReturnValue(of({ platforms: [youTubePlatform()] }));

    await createComponent();

    expect(editor()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Select a platform on the left');
  });

  it('renders a load error when platforms cannot be loaded', async () => {
    service.list.mockReturnValue(throwError(() => new Error('Request failed')));

    await createComponent();

    expect(fixture.nativeElement.textContent).toContain('Platforms could not be loaded.');
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
      fixture.nativeElement.querySelectorAll('app-input input'),
    ) as HTMLInputElement[];
    expect(inputs.some((input) => input.value === 'client-id')).toBe(true);
    expect(inputs.some((input) => input.value === 'client-secret')).toBe(false);
    expect(inputs.some((input) => input.value === 'refresh-token')).toBe(false);
    expect(inputs.some((input) => input.placeholder.includes('keep existing secret'))).toBe(true);
    expect(inputs.some((input) => input.placeholder.includes('keep existing token'))).toBe(true);
  });

  it('loads templates filtered by the editor platform type', async () => {
    service.list.mockReturnValue(of({ platforms: [] }));

    await createComponent();
    buttonByText('New Platform').click();
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
    buttonByText('New Platform').click();
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
      fixture.nativeElement.querySelectorAll('app-input input'),
    ) as HTMLInputElement[];

    expect(fixture.nativeElement.textContent).not.toContain('not available');
    expect(fixture.nativeElement.querySelector('.readonly-type')?.textContent).toContain(
      'WordPress',
    );
    expect(inputs.some((input) => input.value === 'https://blog.example.test/')).toBe(true);
    expect(inputs.some((input) => input.value === 'publisher')).toBe(true);
    expect(inputs.some((input) => input.value === 'application-password')).toBe(false);
    expect(inputs.some((input) => input.placeholder.includes('keep existing password'))).toBe(true);
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
    buttonByText('New Platform').click();
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
  });

  it('creates a WordPress platform with provider settings', async () => {
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
          postStatus: 'draft',
          applicationPasswordConfigured: true,
        },
        publishingContent: publishingContent({
          titleTemplateId: 'wordpress-title-template',
          descriptionTemplateId: 'wordpress-description-template',
        }),
      }),
    );

    await createComponent();
    buttonByText('New Platform').click();
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
      youTubePrivacyStatus: 'private',
      youTubeMadeForKids: 'false',
      wordPressSiteUrl: ' https://blog.example.test/ ',
      wordPressUsername: ' publisher ',
      wordPressApplicationPassword: 'local-test-password',
      wordPressPostStatus: 'draft',
      wordPressApplicationPasswordConfigured: 'false',
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
        postStatus: 'draft',
        applicationPassword: 'local-test-password',
      },
      publishingContent: {
        titleTemplateId: 'wordpress-title-template',
        descriptionTemplateId: 'wordpress-description-template',
      },
    });
    expect(rows()).toHaveLength(1);
  });

  it('updates a WordPress platform without sending a blank Application Password', async () => {
    service.list.mockReturnValue(of({ platforms: [wordPressPlatform()] }));
    service.update.mockReturnValue(
      of({
        id: 'id-2',
        name: 'Company blog',
        referenceKey: 'blog-1',
        type: 'WordPress',
        publishSettings: {
          siteUrl: 'https://blog.example.test/',
          username: 'publisher',
          postStatus: 'draft',
          applicationPasswordConfigured: true,
        },
        publishingContent: publishingContent({
          titleTemplateId: 'wordpress-title-template',
          descriptionTemplateId: 'wordpress-description-template',
        }),
      }),
    );

    await createComponent();
    await selectRow(0);

    await submitEditor();

    expect(service.update).toHaveBeenCalledWith('WordPress', 'id-2', {
      name: 'Company blog',
      referenceKey: 'blog-1',
      publishSettings: {
        siteUrl: 'https://blog.example.test/',
        username: 'publisher',
        postStatus: 'draft',
      },
      publishingContent: publishingContent({
        titleTemplateId: 'wordpress-title-template',
        descriptionTemplateId: 'wordpress-description-template',
      }),
    });
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
          applicationPasswordConfigured: true,
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
      youTubePrivacyStatus: 'private',
      youTubeMadeForKids: 'false',
      wordPressSiteUrl: 'https://blog.example.test/',
      wordPressUsername: 'publisher',
      wordPressApplicationPassword: 'replacement-local-password',
      wordPressPostStatus: 'publish',
      wordPressApplicationPasswordConfigured: 'true',
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
        applicationPassword: 'replacement-local-password',
      },
      publishingContent: publishingContent({
        titleTemplateId: 'wordpress-title-template',
        descriptionTemplateId: 'wordpress-description-template',
      }),
    });
  });

  it('surfaces a friendly message when the name is already taken', async () => {
    service.list.mockReturnValue(of({ platforms: [] }));
    service.create.mockReturnValue(throwError(() => new PlatformNameConflictError()));

    await createComponent();
    buttonByText('New Platform').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    await setValue(nameInput(), 'Main YouTube channel');
    await setValue(inputByLabel('Client ID'), 'client-id');
    await setValue(inputByLabel('Client secret'), 'client-secret');
    await setValue(inputByLabel('Refresh token'), 'refresh-token');
    setRequiredTemplateIds();

    await submitEditor();

    expect(fixture.nativeElement.textContent).toContain(
      'A platform with this name already exists.',
    );
  });

  it('requires both publishing content templates before saving', async () => {
    service.list.mockReturnValue(of({ platforms: [] }));

    await createComponent();
    buttonByText('New Platform').click();
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
    buttonByText('New Platform').click();
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
    buttonByText('New Platform').click();
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
    buttonByText('New Platform').click();
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

    expect(fixture.nativeElement.textContent).toContain(
      'A platform with this reference key already exists.',
    );
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
