import { type ComponentFixture } from '@angular/core/testing';
import { firstValueFrom, isObservable, type Observable } from 'rxjs';

type TextControl = HTMLInputElement | HTMLTextAreaElement;

export function textContent(node: Node | null | undefined): string {
  return node?.textContent ?? '';
}

export function buttonByText(
  root: ParentNode,
  text: string,
  selector = 'app-button button',
): HTMLButtonElement {
  const button = Array.from(root.querySelectorAll(selector)).find((candidate) =>
    textContent(candidate).trim().includes(text),
  );

  if (button === undefined) {
    throw new Error(`Button containing text '${text}' was not found.`);
  }

  return button as HTMLButtonElement;
}

export async function setInputValue<T>(
  fixture: ComponentFixture<T>,
  element: TextControl,
  value: string,
): Promise<void> {
  element.value = value;
  element.dispatchEvent(new Event('input'));
  await fixture.whenStable();
  fixture.detectChanges();
}

export async function submitForm<T>(
  fixture: ComponentFixture<T>,
  selector = 'form',
): Promise<void> {
  const form = fixture.nativeElement.querySelector(selector) as HTMLFormElement | null;
  if (form === null) {
    throw new Error(`Form matching selector '${selector}' was not found.`);
  }

  form.dispatchEvent(new Event('submit'));
  fixture.detectChanges();
  await fixture.whenStable();
  fixture.detectChanges();
}

export function dataRows(root: ParentNode): HTMLElement[] {
  return Array.from(root.querySelectorAll('tr')).filter((row) => {
    const element = row as HTMLElement;
    return element.querySelector('td') !== null && !element.classList.contains('empty-row');
  }) as HTMLElement[];
}

export async function clickRow<T>(
  fixture: ComponentFixture<T>,
  index: number,
  root: ParentNode = fixture.nativeElement,
): Promise<HTMLElement> {
  const row = dataRows(root)[index];
  if (row === undefined) {
    throw new Error(`Data row at index ${index} was not found.`);
  }

  row.dispatchEvent(new Event('click'));
  fixture.detectChanges();
  await fixture.whenStable();
  fixture.detectChanges();

  return row;
}

export async function resolveCanDeactivate(
  result: boolean | Promise<boolean> | Observable<boolean>,
): Promise<boolean> {
  if (typeof result === 'boolean') {
    return result;
  }

  if (isObservable(result)) {
    return firstValueFrom(result);
  }

  return result;
}
