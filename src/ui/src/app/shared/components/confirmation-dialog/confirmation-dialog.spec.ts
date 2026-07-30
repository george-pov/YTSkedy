import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { By } from '@angular/platform-browser';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { Button } from 'src/app/shared/components/button/button';
import { Icon } from 'src/app/shared/components/icon/icon';
import { ConfirmationDialog, ConfirmationDialogContent } from './confirmation-dialog';

function content(overrides: Partial<ConfirmationDialogContent> = {}): ConfirmationDialogContent {
  return {
    title: 'Delete calendar event?',
    body: 'This permanently removes the scheduled event.',
    kind: 'warning',
    bodyId: 'confirmation-dialog-body-test',
    actions: [
      { id: 'cancel', label: 'Cancel' },
      { id: 'delete', label: 'Delete', primary: true },
    ],
    ...overrides,
  };
}

describe('ConfirmationDialog', () => {
  let close: ReturnType<typeof vi.fn>;

  function setup(
    data: ConfirmationDialogContent = content(),
  ): ComponentFixture<ConfirmationDialog> {
    close = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        { provide: MAT_DIALOG_DATA, useValue: data },
        { provide: MatDialogRef, useValue: { close } },
      ],
    });
    const fixture = TestBed.createComponent(ConfirmationDialog);
    fixture.detectChanges();
    return fixture;
  }

  function actionButtons(fixture: ComponentFixture<ConfirmationDialog>): HTMLButtonElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll('app-button button'));
  }

  function actionButtonComponents(fixture: ComponentFixture<ConfirmationDialog>): Button[] {
    return fixture.debugElement
      .queryAll(By.directive(Button))
      .map((button) => button.componentInstance as Button);
  }

  it('renders the title and body', () => {
    const el = setup().nativeElement as HTMLElement;

    expect(el.querySelector('h2')?.textContent).toContain('Delete calendar event?');
    expect(el.querySelector('.body')?.textContent).toContain(
      'This permanently removes the scheduled event.',
    );
  });

  it('exposes the body id used for aria-describedby', () => {
    const el = setup().nativeElement as HTMLElement;

    expect(el.querySelector('.body')?.getAttribute('id')).toBe('confirmation-dialog-body-test');
  });

  it('renders the kind icon with the matching status class', () => {
    const fixture = setup(content({ kind: 'warning' }));
    const el = fixture.nativeElement as HTMLElement;
    const icon = el.querySelector('.icon');
    const iconComponent = fixture.debugElement.query(By.directive(Icon)).componentInstance as Icon;

    expect(icon?.classList).toContain('warning');
    expect(iconComponent.name()).toBe('warning');
  });

  it('defaults to the info kind when none is supplied', () => {
    const fixture = setup(content({ kind: undefined }));
    const el = fixture.nativeElement as HTMLElement;
    const icon = el.querySelector('.icon');
    const iconComponent = fixture.debugElement.query(By.directive(Icon)).componentInstance as Icon;

    expect(icon?.classList).toContain('info');
    expect(iconComponent.name()).toBe('info');
  });

  it('renders one button per action in safe-first order with default variants', () => {
    const fixture = setup();
    const labels = actionButtons(fixture).map((button) => button.textContent?.trim());

    expect(labels).toEqual(['Cancel', 'Delete']);
    expect(actionButtonComponents(fixture).map((button) => button.variant())).toEqual([
      'text',
      'filled',
    ]);
  });

  it('passes default and danger variants without changing action order', () => {
    const fixture = setup(
      content({
        actions: [
          { id: 'stay', label: 'Keep editing' },
          {
            id: 'discard',
            label: 'Discard changes',
            primary: true,
            variant: 'danger-filled',
          },
        ],
      }),
    );
    const buttons = actionButtonComponents(fixture);

    expect(actionButtons(fixture).map((button) => button.textContent?.trim())).toEqual([
      'Keep editing',
      'Discard changes',
    ]);
    expect(buttons.map((button) => button.variant())).toEqual(['text', 'danger-filled']);
    expect(actionButtons(fixture)[1].classList).toContain('is-danger');
  });

  it.each([
    [0, 'cancel'],
    [1, 'delete'],
  ] as const)('closes with action %s selected as %s', (actionIndex, actionId) => {
    const fixture = setup();

    actionButtons(fixture)[actionIndex].click();

    expect(close).toHaveBeenCalledWith(actionId);
  });

  it('does not close until an action is chosen', () => {
    setup();

    expect(close).not.toHaveBeenCalled();
  });
});
