import {
  Component,
  computed,
  effect,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { type Field } from '@angular/forms/signals';
import { ErrorStateMatcher } from '@angular/material/core';
import { MatInput, MatInputModule } from '@angular/material/input';

type MaskedInputMode = 'secret' | 'password';
type FloatLabel = 'always' | 'auto';

const secretDisplayLength = 12;
const secretVisibleSuffixLength = 3;
const passwordDisplayLength = 7;
const maskCharacter = '*';

@Component({
  selector: 'app-masked-input',
  imports: [MatInputModule],
  templateUrl: './masked-input.html',
  styleUrl: './masked-input.scss'
})
export class MaskedInput {
  readonly field = input.required<Field<string>>();
  readonly label = input('');
  readonly placeholder = input('');
  readonly displayValue = input('');
  readonly maskMode = input<MaskedInputMode>('secret');

  private readonly input = viewChild(MatInput);
  private readonly focused = signal(false);

  protected readonly visibleValue = computed(() => {
    const value = this.field()().value();
    if (this.focused()) {
      return value;
    }

    return value.length > 0 ? maskValue(value, this.maskMode()) : this.displayValue();
  });

  protected readonly floatLabel = computed<FloatLabel>(() =>
    this.visibleValue().length > 0 ? 'always' : 'auto',
  );

  protected readonly maxLength = computed(
    () => this.field()().maxLength?.() ?? null,
  );

  protected readonly disabled = computed(() => this.field()().disabled());
  protected readonly readOnly = computed(() => this.field()().readonly());
  protected readonly required = computed(() => this.field()().required());

  protected readonly errorStateMatcher: ErrorStateMatcher = {
    isErrorState: () => this.errorMessage() !== null,
  };

  private readonly syncErrorState = effect(() => {
    this.errorMessage();
    this.input()?.updateErrorState();
  });

  protected readonly errorMessage = computed(() => {
    const state = this.field()();
    if (!state.touched()) {
      return null;
    }

    return state.errors()[0]?.message ?? null;
  });

  protected onFocus(): void {
    this.focused.set(true);
  }

  protected onInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    const state = this.field()();
    state.value.set(value);
    state.markAsDirty();
  }

  protected onBlur(): void {
    this.focused.set(false);
    this.field()().markAsTouched();
  }
}

function maskValue(value: string, mode: MaskedInputMode): string {
  if (mode === 'password') {
    return maskCharacter.repeat(passwordDisplayLength);
  }

  if (value.length < secretVisibleSuffixLength) {
    return maskCharacter.repeat(secretDisplayLength);
  }

  return (
    maskCharacter.repeat(secretDisplayLength - secretVisibleSuffixLength) +
    value.slice(-secretVisibleSuffixLength)
  );
}
