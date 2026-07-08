import { HttpErrorResponse } from '@angular/common/http';
import { computed, signal, type Signal } from '@angular/core';
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
  readonly acceptedFileTypes = thumbnailAccept;
  readonly thumbnail = signal<CalendarEventThumbnail | null>(null);
  readonly previewUrl = signal<string | null>(null);
  readonly selectedFile = signal<File | null>(null);
  readonly selectedPreviewUrl = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly canUpdate = signal(true);
  readonly isUploading = signal(false);
  readonly isDeleting = signal(false);
  readonly canMutate: Signal<boolean>;

  private isDestroyed = false;
  private previewRequestId = 0;

  constructor(
    private readonly calendarEventsService: CalendarEventsService,
    private readonly notifications: NotificationService,
    private readonly calendarEventId: string | null,
    private readonly isEditMode: boolean,
    private readonly hasActiveMutation: () => boolean,
  ) {
    this.canUpdate.set(!isEditMode);
    this.canMutate = computed(
      () => !this.hasActiveMutation() && (!this.isEditMode || this.canUpdate()),
    );
  }

  applyEventDetails(
    event: Pick<CalendarEventDetailsResponse, 'thumbnail' | 'canUpdateThumbnail'>,
  ): void {
    this.thumbnail.set(event.thumbnail);
    this.canUpdate.set(event.canUpdateThumbnail);
    this.clearSelectedThumbnail();
    this.errorMessage.set(null);

    if (event.thumbnail === null) {
      this.cancelPreviewLoad();
      this.setPreviewUrl(null);
      return;
    }

    this.loadPreview();
  }

  resetAfterLoadFailure(): void {
    this.thumbnail.set(null);
    this.canUpdate.set(false);
    this.errorMessage.set(null);
    this.clearSelectedThumbnail();
    this.cancelPreviewLoad();
    this.setPreviewUrl(null);
  }

  setError(message: string | null): void {
    this.errorMessage.set(message);
  }

  selectThumbnail(file: File): void {
    this.setSelectedThumbnail(file);
  }

  clearSelectedThumbnail(): void {
    this.selectedFile.set(null);
    this.revokeSelectedPreview();
  }

  uploadAfterCreate(calendarEventId: string): Observable<string | null> {
    const file = this.selectedFile();
    if (file === null) {
      return of(null);
    }

    this.isUploading.set(true);

    return this.calendarEventsService.uploadThumbnail(calendarEventId, file).pipe(
      map(() => null),
      catchError((error: unknown) => of(describeThumbnailError(error))),
      finalize(() => this.isUploading.set(false)),
    );
  }

  replaceThumbnail(file: File): void {
    if (this.calendarEventId === null || !this.canMutate()) {
      return;
    }

    if (!this.setSelectedThumbnail(file)) {
      return;
    }

    this.isUploading.set(true);

    this.calendarEventsService
      .uploadThumbnail(this.calendarEventId, file)
      .pipe(finalize(() => this.isUploading.set(false)))
      .subscribe({
        next: (thumbnail) => {
          this.cancelPreviewLoad();
          this.thumbnail.set(thumbnail);
          this.promoteSelectedPreview();
          this.selectedFile.set(null);
          this.errorMessage.set(null);
          this.notifications.showSuccess('Thumbnail updated.');
        },
        error: (error: unknown) => {
          this.errorMessage.set(describeThumbnailError(error));
        },
      });
  }

  deleteThumbnail(): void {
    if (
      this.calendarEventId === null ||
      this.thumbnail() === null ||
      !this.canMutate()
    ) {
      return;
    }

    this.isDeleting.set(true);
    this.errorMessage.set(null);

    this.calendarEventsService
      .deleteThumbnail(this.calendarEventId)
      .pipe(finalize(() => this.isDeleting.set(false)))
      .subscribe({
        next: () => {
          this.cancelPreviewLoad();
          this.thumbnail.set(null);
          this.setPreviewUrl(null);
          this.clearSelectedThumbnail();
          this.notifications.showSuccess('Thumbnail deleted.');
        },
        error: (error: unknown) => {
          this.errorMessage.set(describeThumbnailError(error));
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
    this.errorMessage.set(null);

    const error = describeClientThumbnailError(file);
    if (error !== null) {
      this.clearSelectedThumbnail();
      this.errorMessage.set(error);
      return false;
    }

    this.selectedFile.set(file);
    this.revokeSelectedPreview();
    this.selectedPreviewUrl.set(URL.createObjectURL(file));
    return true;
  }

  private loadPreview(): void {
    const expectedThumbnail = this.thumbnail();
    if (this.calendarEventId === null || expectedThumbnail === null) {
      this.cancelPreviewLoad();
      this.setPreviewUrl(null);
      return;
    }

    const requestId = ++this.previewRequestId;
    const expectedUpdatedUtc = expectedThumbnail.updatedUtc;

    this.calendarEventsService.getThumbnail(this.calendarEventId).subscribe({
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
        this.errorMessage.set(
          'The thumbnail preview could not be loaded. Reload the page and try again.',
        );
      },
    });
  }

  private isCurrentPreviewRequest(requestId: number, updatedUtc: string): boolean {
    return (
      !this.isDestroyed &&
      requestId === this.previewRequestId &&
      this.thumbnail()?.updatedUtc === updatedUtc
    );
  }

  private cancelPreviewLoad(): void {
    this.previewRequestId += 1;
  }

  private promoteSelectedPreview(): void {
    const previewUrl = this.selectedPreviewUrl();
    this.revokePreview();
    this.previewUrl.set(previewUrl);
    this.selectedPreviewUrl.set(null);
  }

  private setPreviewUrl(value: string | null): void {
    this.revokePreview();
    this.previewUrl.set(value);
  }

  private revokeSelectedPreview(): void {
    const previewUrl = this.selectedPreviewUrl();
    if (previewUrl !== null) {
      URL.revokeObjectURL(previewUrl);
      this.selectedPreviewUrl.set(null);
    }
  }

  private revokePreview(): void {
    const previewUrl = this.previewUrl();
    if (previewUrl !== null) {
      URL.revokeObjectURL(previewUrl);
      this.previewUrl.set(null);
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
