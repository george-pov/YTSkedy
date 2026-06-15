import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { ComponentLab } from './component-lab';
import { componentLabItems } from './component-lab.registry';

describe('ComponentLab', () => {
  let fixture: ComponentFixture<ComponentLab>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
    fixture = TestBed.createComponent(ComponentLab);
    fixture.detectChanges();
  });

  function navButtons(): HTMLButtonElement[] {
    return Array.from(
      fixture.nativeElement.querySelectorAll('mat-action-list button'),
    );
  }

  it('lists every registered component in the navigation', () => {
    const labels = navButtons().map((button) => button.textContent?.trim());

    expect(labels).toEqual(componentLabItems.map((item) => item.label));
  });

  it('shows the first registered component by default', () => {
    expect(
      fixture.nativeElement.querySelector('app-toolbar-lab'),
    ).not.toBeNull();
    expect(fixture.nativeElement.querySelector('app-button-lab')).toBeNull();
  });

  it('marks the selected component in the navigation as activated', () => {
    const [first] = navButtons();

    expect(first.classList).toContain('mdc-list-item--activated');
  });

  it('swaps the displayed component when another name is selected', () => {
    const buttonNav = navButtons().find(
      (button) => button.textContent?.trim() === 'Button',
    );

    buttonNav?.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-button-lab')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('app-toolbar-lab')).toBeNull();
  });
});
