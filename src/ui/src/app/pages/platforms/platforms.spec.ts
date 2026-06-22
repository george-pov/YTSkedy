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
  PlatformsService,
  UpdatePlatformRequest,
  UpdatePlatformResponse,
} from 'src/app/shared/api/platforms/platforms-service';
import { NotificationService } from 'src/app/shared/notifications/notification-service';
import { Platforms } from './platforms';

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
  let notifications: { showSuccess: Mock<(message: string) => void> };

  beforeEach(() => {
    service = {
      list: vi.fn<() => Observable<PlatformListResponse>>(),
      create: vi.fn(),
      update: vi.fn(),
      delete: vi.fn(),
    };
    notifications = { showSuccess: vi.fn<(message: string) => void>() };
  });

  function youTubePlatform(overrides: Partial<Platform> = {}): Platform {
    return {
      id: 'id-1',
      name: 'Main YouTube channel',
      type: 'YouTube',
      publishSettings: {
        credentials: 'main-youtube-channel',
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
      type: 'WordPress',
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
    return fixture.nativeElement.querySelector(
      'app-input input',
    ) as HTMLInputElement;
  }

  function buttonByText(text: string): HTMLButtonElement {
    return Array.from(
      fixture.nativeElement.querySelectorAll('app-button button'),
    ).find((button) =>
      ((button as HTMLElement).textContent ?? '').trim().includes(text),
    ) as HTMLButtonElement;
  }

  async function setValue(
    element: HTMLInputElement,
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
      imports: [Platforms],
      providers: [
        provideZonelessChangeDetection(),
        { provide: PlatformsService, useValue: service },
        { provide: NotificationService, useValue: notifications },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Platforms);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('loads platforms on init and renders a row per platform with type and name columns', async () => {
    service.list.mockReturnValue(
      of({ platforms: [youTubePlatform(), wordPressPlatform()] }),
    );

    await createComponent();

    expect(service.list).toHaveBeenCalledTimes(1);

    const headers = Array.from(fixture.nativeElement.querySelectorAll('th')).map(
      (th) => (th as HTMLElement).textContent?.trim(),
    );
    expect(headers).toEqual(['Type', 'Name']);
    expect(rows()).toHaveLength(2);
  });

  it('hides the editor until a platform is selected or New is clicked', async () => {
    service.list.mockReturnValue(of({ platforms: [youTubePlatform()] }));

    await createComponent();

    expect(editor()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain(
      'Select a platform on the left',
    );
  });

  it('renders a load error when platforms cannot be loaded', async () => {
    service.list.mockReturnValue(throwError(() => new Error('Request failed')));

    await createComponent();

    expect(fixture.nativeElement.textContent).toContain(
      'Platforms could not be loaded.',
    );
    expect(rows()).toHaveLength(0);
  });

  it('opens a YouTube platform in an edit form with a read-only type and its settings', async () => {
    service.list.mockReturnValue(of({ platforms: [youTubePlatform()] }));

    await createComponent();
    await selectRow(0);

    expect(editor()).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.readonly-type')?.textContent).toContain(
      'YouTube',
    );
    expect(nameInput().value).toBe('Main YouTube channel');
    // The YouTube credentials input is rendered with the stored value.
    const inputs = Array.from(
      fixture.nativeElement.querySelectorAll('app-input input'),
    ) as HTMLInputElement[];
    expect(inputs.some((input) => input.value === 'main-youtube-channel')).toBe(
      true,
    );
  });

  it('shows a not-available notice instead of settings for a WordPress platform', async () => {
    service.list.mockReturnValue(of({ platforms: [wordPressPlatform()] }));

    await createComponent();
    await selectRow(0);

    expect(fixture.nativeElement.textContent).toContain(
      'Settings for WordPress platforms are not available yet.',
    );
  });

  it('creates a platform and adds it to the list', async () => {
    service.list.mockReturnValue(of({ platforms: [] }));
    service.create.mockReturnValue(
      of({ id: 'new-id', name: 'Second channel', type: 'YouTube' }),
    );

    await createComponent();
    buttonByText('New Platform').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    await setValue(nameInput(), 'Second channel');
    const credentials = Array.from(
      fixture.nativeElement.querySelectorAll('app-input input'),
    )[1] as HTMLInputElement;
    await setValue(credentials, 'second-channel');

    await submitEditor();

    expect(service.create).toHaveBeenCalledTimes(1);
    const request = service.create.mock.calls[0][0];
    expect(request).toMatchObject({
      name: 'Second channel',
      type: 'YouTube',
      publishSettings: {
        credentials: 'second-channel',
        privacyStatus: 'private',
        selfDeclaredMadeForKids: false,
      },
    });
    expect(rows()).toHaveLength(1);
    expect(notifications.showSuccess).toHaveBeenCalledWith('Platform created.');
  });

  it('surfaces a friendly message when the name is already taken', async () => {
    service.list.mockReturnValue(of({ platforms: [] }));
    service.create.mockReturnValue(
      throwError(() => new PlatformNameConflictError()),
    );

    await createComponent();
    buttonByText('New Platform').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    await setValue(nameInput(), 'Main YouTube channel');
    const credentials = Array.from(
      fixture.nativeElement.querySelectorAll('app-input input'),
    )[1] as HTMLInputElement;
    await setValue(credentials, 'main-youtube-channel');

    await submitEditor();

    expect(fixture.nativeElement.textContent).toContain(
      'A platform with this name already exists.',
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
});
