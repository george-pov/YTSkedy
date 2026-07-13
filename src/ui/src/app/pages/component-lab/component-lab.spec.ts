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
      fixture.nativeElement.querySelectorAll('nav[aria-label="Components"] button'),
    );
  }

  it('lists every registered component in the navigation', () => {
    const labels = navButtons().map((button) => button.textContent?.trim());

    expect(labels).toEqual(componentLabItems.map((item) => item.label));
  });

  it('renders the local menu icon', () => {
    const icon = fixture.nativeElement.querySelector('.lab-menu app-icon');

    expect(icon).not.toBeNull();
    expect(icon.querySelector('svg')).not.toBeNull();
  });

  it('shows the first registered component by default', () => {
    expect(fixture.nativeElement.querySelector('app-toolbar-lab')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('app-button-lab')).toBeNull();
  });

  it('swaps the displayed component when another name is selected', () => {
    const buttonNav = navButtons().find((button) => button.textContent?.trim() === 'Button');

    buttonNav?.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-button-lab')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('app-toolbar-lab')).toBeNull();
  });

  it('renders the registered Checkbox lab', () => {
    const checkboxNav = navButtons().find((button) => button.textContent?.trim() === 'Checkbox');

    checkboxNav?.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-checkbox-lab')).not.toBeNull();
  });

  it('renders the registered Chip List lab with Basic and Disabled examples', () => {
    const chipListNav = navButtons().find((button) => button.textContent?.trim() === 'Chip List');

    chipListNav?.click();
    fixture.detectChanges();

    const lab = fixture.nativeElement.querySelector('app-chip-list-lab') as HTMLElement;
    expect(lab).not.toBeNull();
    expect(lab.textContent).toContain('Basic');
    expect(lab.textContent).toContain('Disabled');
    expect(lab.querySelectorAll('app-chip-list')).toHaveLength(2);
  });
});
