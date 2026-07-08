import { HttpErrorResponse } from '@angular/common/http';
import { computed, signal, type DestroyRef, type Signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, finalize, map, Observable, of } from 'rxjs';

import {
  CalendarEventsService,
  type CalendarEventDetailsResponse,
  type CalendarEventThumbnail,
} from 'src/app/shared/api/calendar-events/calendar-events-service';
import { NotificationService } from 'src/app/shared/notifications/notification-service';

export const thumbnailErrorNavigationStateKey = 'thumbnailErrorMessage';

const thumbnailMaxSizeBytes = 2 * 1024 * 1024;
const thumbnailAccept = 'image/jpeg,image/png,.jpg,.jpeg,.png';
const supportedThumbnailTypes = new Set(['image/jpeg', 'image/png']);
const supportedThumbnailExtensions = new Set(['.jpg', '.jpeg', '.png']);

export class ThumbnailEditorState {
  private readonly _thumbnail = signal<CalendarEventThumbnail | null>(null);
  private readonly _previewUrl = signal<string | null>(null);
  private readonly _selectedFile = signal<File | null>(null);
  private readonly _selectedPreviewUrl = signal<string | null>(null);
  private readonly _errorMessage = signal<string | null>(null);
  private readonly _canUpdate = signal(true);
  private readonly _isUploading = signal(false);
  private readonly _isDeleting = signal(false);
  private isDestroyed = false;
  private previewRequestId = 0;

  readonly acceptedFileTypes = thumbnailAccept;
  readonly thumbnail = this._thumbnail.asReadonly();
  readonly previewUrl = this._previewUrl.asReadonly();
  readonly selectedFile = this._selectedFile.asReadonly();
  readonly selectedPreviewUrl = this._selectedPreviewUrl.asReadonly();
  readonly errorMessage = this._errorMessage.asReadonly();
  readonly canUpdate = this._canUpdate.asReadonly();
  readonly isUploading = this._isUploading.asReadonly();
  readonly isDeleting = this._isDeleting.asReadonly();
  readonly canMutate: Signal<boolean>;

  constructor(
    private readonly calendarEventsService: CalendarEventsService,
    private readonly notifications: NotificationService,
    private readonly calendarEventId: string | null,
    private readonly isEditMode: boolean,
    private readonly destroyRef: DestroyRef,
    private readonly hasActiveMutation: () => boolean,
  ) {
    this._canUpdate.set(!isEditMode);
    this.canMutate = computed(
      () => !this.hasActiveMutation() && (!this.isEditMode || this._canUpdate()),
    );
  }

  applyEventDetails(
    event: Pick<CalendarEventDetailsResponse, 'thumbnail' | 'canUpdateThumbnail'>,
  ): void {
    this._thumbnail.set(event.thumbnail);
    this._canUpdate.set(event.canUpdateThumbnail);
    this.clearSelectedThumbnail();
    this._errorMessage.set(null);

    if (event.thumbnail === null) {
      this.cancelPreviewLoad();
      this.setPreviewUrl(null);
      return;
    }

    this.loadPreview();
  }

  resetAfterLoadFailure(): void {
    this._thumbnail.set(null);
    this._canUpdate.set(false);
    this._errorMessage.set(null);
    this.clearSelectedThumbnail();
    this.cancelPreviewLoad();
    this.setPreviewUrl(null);
  }

  setError(message: string | null): void {
    this._errorMessage.set(message);
  }

  selectThumbnail(file: File): void {
    this.setSelectedThumbnail(file);
  }

  clearSelectedThumbnail(): void {
    this._selectedFile.set(null);
    this.revokeSelectedPreview();
  }

  uploadAfterCreate(calendarEventId: string): Observable<string | null> {
    const file = this._selectedFile();
    if (file === null) {
      return of(null);
    }

    this._isUploading.set(true);

    return this.calendarEventsService.uploadThumbnail(calendarEventId, file).pipe(
      map(() => null),
      catchError((error: unknown) => of(describeThumbnailError(error))),
      finalize(() => this._isUploading.set(false)),
      takeUntilDestroyed(this.destroyRef),
    );
  }

  uploadThumbnail(file: File): void {
    if (
      this.calendarEventId === null ||
      this._thumbnail() !== null ||
      !this.canMutate()
    ) {
      return;
    }

    if (!this.setSelectedThumbnail(file)) {
      return;
    }

    this._isUploading.set(true);

    this.calendarEventsService
      .uploadThumbnail(this.calendarEventId, file)
      .pipe(
        finalize(() => this._isUploading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (thumbnail) => {
          this.cancelPreviewLoad();
          this._thumbnail.set(thumbnail);
          this.promoteSelectedPreview();
          this._selectedFile.set(null);
          this._errorMessage.set(null);
          this.notifications.showSuccess('Thumbnail uploaded.');
        },
        error: (error: unknown) => {
          this._errorMessage.set(describeThumbnailError(error));
        },
      });
  }

  deleteThumbnail(): void {
    if (
      this.calendarEventId === null ||
      this._thumbnail() === null ||
      !this.canMutate()
    ) {
      return;
    }

    this._isDeleting.set(true);
    this._errorMessage.set(null);

    this.calendarEventsService
      .deleteThumbnail(this.calendarEventId)
      .pipe(
        finalize(() => this._isDeleting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.cancelPreviewLoad();
          this._thumbnail.set(null);
          this.setPreviewUrl(null);
          this.clearSelectedThumbnail();
          this.notifications.showSuccess('Thumbnail deleted.');
        },
        error: (error: unknown) => {
          this._errorMessage.set(describeThumbnailError(error));
        },
      });
  }

  destroy(): void {
    this.isDestroyed = true;
    this.cancelPreviewLoad();
    this.revokeSelectedPreview();
    this.revokePreview();
  }

  private setSelectedThumbnail(file: File): boolean {
    this._errorMessage.set(null);

    const error = describeClientThumbnailError(file);
    if (error !== null) {
      this.clearSelectedThumbnail();
      this._errorMessage.set(error);
      return false;
    }

    this._selectedFile.set(file);
    this.revokeSelectedPreview();
    this._selectedPreviewUrl.set(URL.createObjectURL(file));
    return true;
  }

  private loadPreview(): void {
    const expectedThumbnail = this._thumbnail();
    if (this.calendarEventId === null || expectedThumbnail === null) {
      this.cancelPreviewLoad();
      this.setPreviewUrl(null);
      return;
    }

    const requestId = ++this.previewRequestId;
    const expectedUpdatedUtc = expectedThumbnail.updatedUtc;

    this.calendarEventsService
      .getThumbnail(this.calendarEventId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (content) => {
          if (!this.isCurrentPreviewRequest(requestId, expectedUpdatedUtc)) {
            return;
          }

          this.setPreviewUrl(URL.createObjectURL(content));
        },
        error: () => {
          if (!this.isCurrentPreviewRequest(requestId, expectedUpdatedUtc)) {
            return;
          }

          this.setPreviewUrl(null);
          this._errorMessage.set(
            'The thumbnail preview could not be loaded. Reload the page and try again.',
          );
        },
      });
  }

  private isCurrentPreviewRequest(requestId: number, updatedUtc: string): boolean {
    return (
      !this.isDestroyed &&
      requestId === this.previewRequestId &&
      this._thumbnail()?.updatedUtc === updatedUtc
    );
  }

  private cancelPreviewLoad(): void {
    this.previewRequestId += 1;
  }

  private promoteSelectedPreview(): void {
    const previewUrl = this._selectedPreviewUrl();
    this.revokePreview();
    this._previewUrl.set(previewUrl);
    this._selectedPreviewUrl.set(null);
  }

  private setPreviewUrl(value: string | null): void {
    this.revokePreview();
    this._previewUrl.set(value);
  }

  private revokeSelectedPreview(): void {
    const previewUrl = this._selectedPreviewUrl();
    if (previewUrl !== null) {
      URL.revokeObjectURL(previewUrl);
      this._selectedPreviewUrl.set(null);
    }
  }

  private revokePreview(): void {
    const previewUrl = this._previewUrl();
    if (previewUrl !== null) {
      URL.revokeObjectURL(previewUrl);
      this._previewUrl.set(null);
    }
  }
}

export function isSupportedThumbnailFile(file: File): boolean {
  return (
    supportedThumbnailTypes.has(file.type.toLowerCase()) &&
    supportedThumbnailExtensions.has(fileExtension(file.name))
  );
}

export function describeThumbnailError(error: unknown): string {
  if (error instanceof HttpErrorResponse && error.status === 400) {
    return 'The thumbnail must be a JPEG or PNG image up to 2 MB.';
  }

  if (error instanceof HttpErrorResponse && error.status === 409) {
    return 'The thumbnail can no longer be changed. Reload the page and try again.';
  }

  return 'The thumbnail could not be changed. Check your connection and try again.';
}

export function thumbnailErrorFromNavigationState(state: unknown): string | null {
  if (state === null || typeof state !== 'object') {
    return null;
  }

  const value = (state as Record<string, unknown>)[thumbnailErrorNavigationStateKey];
  return typeof value === 'string' ? value : null;
}

function describeClientThumbnailError(file: File): string | null {
  if (file.size > thumbnailMaxSizeBytes) {
    return 'Thumbnail file size must be 2 MB or smaller.';
  }

  if (!isSupportedThumbnailFile(file)) {
    return 'Thumbnail file must be a JPEG or PNG image.';
  }

  return null;
}

function fileExtension(fileName: string): string {
  const dotIndex = fileName.lastIndexOf('.');

  return dotIndex < 0 ? '' : fileName.slice(dotIndex).toLowerCase();
}
