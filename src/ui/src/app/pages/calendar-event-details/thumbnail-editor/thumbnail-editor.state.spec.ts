import { type DestroyRef } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, type Mock, vi } from 'vitest';

import {
  CalendarEventsService,
  CalendarEventThumbnail,
} from 'src/app/shared/api/calendar-events/calendar-events-service';
import { NotificationService } from 'src/app/shared/notifications/notification-service';
import {
  describeThumbnailError,
  isSupportedThumbnailFile,
  ThumbnailEditorState,
  thumbnailErrorFromNavigationState,
  thumbnailErrorNavigationStateKey,
} from './thumbnail-editor.state';

describe('ThumbnailEditorState', () => {
  let service: {
    uploadThumbnail: Mock<
      (calendarEventId: string, thumbnail: File) => Observable<CalendarEventThumbnail>
    >;
    getThumbnail: Mock<(calendarEventId: string) => Observable<Blob>>;
    deleteThumbnail: Mock<(calendarEventId: string) => Observable<void>>;
  };
  let notifications: { showSuccess: Mock<(message: string) => void> };
  let destroyRef: DestroyRef;
  let hasActiveMutation: boolean;

  beforeEach(() => {
    service = {
      uploadThumbnail:
        vi.fn<(calendarEventId: string, thumbnail: File) => Observable<CalendarEventThumbnail>>(),
      getThumbnail: vi.fn<(calendarEventId: string) => Observable<Blob>>(),
      deleteThumbnail: vi.fn<(calendarEventId: string) => Observable<void>>(),
    };
    notifications = { showSuccess: vi.fn<(message: string) => void>() };
    destroyRef = {
      destroyed: false,
      onDestroy: vi.fn(() => () => undefined),
    };
    hasActiveMutation = false;

    let objectUrlIndex = 0;
    Object.defineProperty(URL, 'createObjectURL', {
      configurable: true,
      value: vi.fn(() => `blob:thumbnail-${++objectUrlIndex}`),
    });
    Object.defineProperty(URL, 'revokeObjectURL', {
      configurable: true,
      value: vi.fn(),
    });
  });

  function createState(
    calendarEventId: string | null = 'event-1',
    isEditMode = true,
  ): ThumbnailEditorState {
    return new ThumbnailEditorState(
      service as unknown as CalendarEventsService,
      notifications as unknown as NotificationService,
      calendarEventId,
      isEditMode,
      destroyRef,
      () => hasActiveMutation,
    );
  }

  function imageFile(
    name = 'stream.png',
    type = 'image/png',
    sizeBytes = 11,
  ): File {
    return new File([new Uint8Array(sizeBytes)], name, { type });
  }

  function thumbnail(
    overrides: Partial<CalendarEventThumbnail> = {},
  ): CalendarEventThumbnail {
    return {
      fileName: 'stream.png',
      contentType: 'image/png',
      sizeBytes: 11,
      width: 1280,
      height: 720,
      updatedUtc: '2030-07-04T08:20:00+00:00',
      ...overrides,
    };
  }

  it('validates supported thumbnail files by browser type and file name', () => {
    expect(isSupportedThumbnailFile(imageFile('stream.png', 'image/png'))).toBe(true);
    expect(isSupportedThumbnailFile(imageFile('stream.jpg', 'image/jpeg'))).toBe(true);
    expect(isSupportedThumbnailFile(imageFile('stream.gif', 'image/png'))).toBe(false);
    expect(isSupportedThumbnailFile(imageFile('stream.png', 'image/gif'))).toBe(false);
  });

  it('maps thumbnail API errors to thumbnail-specific messages', () => {
    expect(describeThumbnailError(new HttpErrorResponse({ status: 400 }))).toContain(
      'JPEG or PNG image up to 2 MB',
    );
    expect(describeThumbnailError(new HttpErrorResponse({ status: 409 }))).toContain(
      'can no longer be changed',
    );
    expect(describeThumbnailError(new Error('network'))).toContain(
      'Check your connection and try again',
    );
  });

  it('reads thumbnail upload errors from router navigation state', () => {
    expect(
      thumbnailErrorFromNavigationState({
        [thumbnailErrorNavigationStateKey]: 'Thumbnail failed.',
      }),
    ).toBe('Thumbnail failed.');
    expect(thumbnailErrorFromNavigationState({ other: 'ignored' })).toBeNull();
    expect(thumbnailErrorFromNavigationState(null)).toBeNull();
  });

  it('ignores an older preview response after newer thumbnail metadata is applied', () => {
    const oldPreview = new Subject<Blob>();
    const newPreview = new Subject<Blob>();
    const oldContent = new Blob(['old-image'], { type: 'image/png' });
    const newContent = new Blob(['new-image'], { type: 'image/png' });
    service.getThumbnail
      .mockReturnValueOnce(oldPreview.asObservable())
      .mockReturnValueOnce(newPreview.asObservable());
    const state = createState();

    state.applyEventDetails({
      thumbnail: thumbnail({ updatedUtc: '2030-07-04T08:20:00+00:00' }),
      canUpdateThumbnail: true,
    });
    state.applyEventDetails({
      thumbnail: thumbnail({
        fileName: 'new-stream.png',
        updatedUtc: '2030-07-04T08:25:00+00:00',
      }),
      canUpdateThumbnail: true,
    });

    oldPreview.next(oldContent);
    expect(URL.createObjectURL).not.toHaveBeenCalledWith(oldContent);
    expect(state.previewUrl()).toBeNull();

    newPreview.next(newContent);
    expect(URL.createObjectURL).toHaveBeenCalledWith(newContent);
    expect(state.previewUrl()).toBe('blob:thumbnail-1');
  });

  it('ignores an older preview error after the thumbnail is cleared', () => {
    const oldPreview = new Subject<Blob>();
    service.getThumbnail.mockReturnValueOnce(oldPreview.asObservable());
    const state = createState();

    state.applyEventDetails({
      thumbnail: thumbnail(),
      canUpdateThumbnail: true,
    });
    state.applyEventDetails({
      thumbnail: null,
      canUpdateThumbnail: true,
    });

    oldPreview.error(new Error('stale'));

    expect(state.errorMessage()).toBeNull();
    expect(state.previewUrl()).toBeNull();
  });

  it('ignores an older preview response after thumbnail delete succeeds', () => {
    const oldPreview = new Subject<Blob>();
    const oldContent = new Blob(['old-image'], { type: 'image/png' });
    service.getThumbnail.mockReturnValueOnce(oldPreview.asObservable());
    service.deleteThumbnail.mockReturnValue(of<void>(undefined));
    const state = createState();

    state.applyEventDetails({
      thumbnail: thumbnail(),
      canUpdateThumbnail: true,
    });
    state.deleteThumbnail();
    oldPreview.next(oldContent);

    expect(service.deleteThumbnail).toHaveBeenCalledWith('event-1');
    expect(URL.createObjectURL).not.toHaveBeenCalledWith(oldContent);
    expect(state.thumbnail()).toBeNull();
    expect(state.previewUrl()).toBeNull();
    expect(notifications.showSuccess).toHaveBeenCalledWith('Thumbnail deleted.');
  });

  it('does not upload over an existing thumbnail', () => {
    const file = imageFile('replacement.png', 'image/png');
    service.getThumbnail.mockReturnValue(of(new Blob(['image-bytes'], { type: 'image/png' })));
    const state = createState();

    state.applyEventDetails({
      thumbnail: thumbnail(),
      canUpdateThumbnail: true,
    });
    state.uploadThumbnail(file);

    expect(service.uploadThumbnail).not.toHaveBeenCalled();
    expect(state.selectedFile()).toBeNull();
  });
});
