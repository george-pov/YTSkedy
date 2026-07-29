import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { type ButtonAppearance } from 'src/app/shared/components/button/button';
import { type IconName } from 'src/app/shared/components/icon/icon';
import { FileButton } from './file-button';

@Component({
  selector: 'app-file-button-host',
  imports: [FileButton],
  template: `
    <app-file-button
      [label]="label()"
      [accept]="accept()"
      [appearance]="appearance()"
      [icon]="icon()"
      [disabled]="disabled()"
      (fileSelected)="selectFile($event)"
    />
  `,
})
class FileButtonHost {
  readonly label = signal('Choose image');
  readonly accept = signal('image/png');
  readonly appearance = signal<ButtonAppearance>('filled');
  readonly icon = signal<IconName>('upload');
  readonly disabled = signal(false);
  readonly selectedFiles: File[] = [];

  selectFile(file: File): void {
    this.selectedFiles.push(file);
  }
}

function inputEl(fixture: ComponentFixture<FileButtonHost>): HTMLInputElement {
  return fixture.nativeElement.querySelector('input[type="file"]') as HTMLInputElement;
}

function buttonEl(fixture: ComponentFixture<FileButtonHost>): HTMLButtonElement {
  return fixture.nativeElement.querySelector('button') as HTMLButtonElement;
}

function chooseFile(
  fixture: ComponentFixture<FileButtonHost>,
  file: File | null,
): void {
  Object.defineProperty(inputEl(fixture), 'files', {
    configurable: true,
    value: {
      0: file,
      length: file === null ? 0 : 1,
      item: (index: number) => (index === 0 ? file : null),
    },
  });
  inputEl(fixture).dispatchEvent(new Event('change'));
  fixture.detectChanges();
}

describe('FileButton', () => {
  let fixture: ComponentFixture<FileButtonHost>;
  let host: FileButtonHost;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
    fixture = TestBed.createComponent(FileButtonHost);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renders a Material button and hidden file input', () => {
    expect(buttonEl(fixture).textContent).toContain('Choose image');
    expect(
      fixture.nativeElement.querySelector('app-icon mat-icon')?.textContent?.trim(),
    ).toBe('upload');
    expect(inputEl(fixture).accept).toBe('image/png');
  });

  it('opens the hidden file input from the visible button', () => {
    const clickInput = vi
      .spyOn(inputEl(fixture), 'click')
      .mockImplementation(() => undefined);

    buttonEl(fixture).click();

    expect(clickInput).toHaveBeenCalledTimes(1);
  });

  it('emits the selected file and resets the input', () => {
    const file = new File(['image'], 'stream.png', { type: 'image/png' });

    chooseFile(fixture, file);

    expect(host.selectedFiles).toEqual([file]);
    expect(inputEl(fixture).value).toBe('');
  });

  it('does not emit when no file is selected', () => {
    chooseFile(fixture, null);

    expect(host.selectedFiles).toEqual([]);
  });

  it('disables the visible button and hidden input together', () => {
    host.disabled.set(true);
    fixture.detectChanges();
    const clickInput = vi
      .spyOn(inputEl(fixture), 'click')
      .mockImplementation(() => undefined);

    buttonEl(fixture).click();

    expect(buttonEl(fixture).disabled).toBe(true);
    expect(inputEl(fixture).disabled).toBe(true);
    expect(clickInput).not.toHaveBeenCalled();
  });
});
