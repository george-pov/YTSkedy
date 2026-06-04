# UI Styling

YTSkedy uses SCSS for application and component styles.

## Goals

- Keep styling predictable under Angular component encapsulation.
- Keep global styles small and intentional.
- Use names that explain ownership without adding unnecessary ceremony.
- Avoid BEM-like `block__element` and `block--modifier` names for new styles.
- Keep UI-library customization behind supported APIs and app-owned wrapper
  components when a UI library is selected.

## Current File Structure

Current global style entry point:

```text
src/ui/src/styles.scss
```

Component `.scss` files live beside the component they style:

```text
src/ui/src/app/**/[component].scss
```

If shared SCSS grows, use:

```text
src/ui/src/styles/
  _a11y.scss
  _layout.scss
  _variables.scss
```

`src/ui/src/styles.scss` should stay limited to resets, base typography, design
token imports, and approved responsive layout utilities.

## Shared SCSS Partials

Keep shared partials narrow and semantic. Prefer mixins and tokens that
describe app-owned styling intent instead of selectors that emit CSS on import.

Use an accessibility partial for primitives such as visually hidden text only
after those primitives are used in more than one component.

Do not import demo-only or local-experiment partials from product pages or
shared UI components.

## Naming Rules

Use simple class names. Prefer component-local names, and keep global class
names rare, app-prefixed, and plain.

### Global Styles

Global classes must be app-prefixed because they are not protected by a
component boundary. Do not add BEM-like `__` element names or `--` modifier
names for new global styles.

```scss
.app-layout {
  min-height: 100dvh;
}

.app-container {
  width: min(100%, 72rem);
  margin-inline: auto;
}

.app-actions {
  display: flex;
  gap: 1rem;
}
```

Only create a global class when the style is truly cross-application. Prefer a
component when the style has behavior, structure, or domain meaning.

The approved exception is the app-owned responsive layout layer described in
[`responsive-layout.md`](responsive-layout.md). Keep that layer layout-only.

### Component Styles

Inside Angular component styles, prefer natural class names nested under a
component root class. Angular encapsulation already scopes the stylesheet, so
component CSS should not use BEM-like names.

For local component styles, avoid BEM-like class names such as
`.calendar-page__hero`, `.calendar-card--selected`, or
`.form-field__message`. Use short component-local structure and state names
instead.

Application chrome and global layout utilities should also use simple
app-prefixed names for new styles. Existing BEM-like shell classes may remain
until the owning component is touched.

```html
<section class="layout">
  <header class="header">
    <h1 class="title">Calendar events</h1>
  </header>

  <div class="content">
    ...
  </div>
</section>
```

```scss
:host {
  display: block;
}

.layout {
  display: flex;
  flex-direction: column;
  gap: 2rem;

  .header {
    display: flex;
    align-items: center;
  }

  .title {
    margin: 0;
  }

  .content {
    display: grid;
    gap: 1rem;
  }
}
```

Use names such as `.layout`, `.header`, `.content`, `.actions`, `.summary`,
`.field`, and `.message` when they describe local structure clearly.

Use `:host` for the component shell: display, sizing, layout participation, and
host-level state.

## Nesting Rules

Keep nesting shallow. Two levels is the normal limit.

Prefer this:

```scss
.form {
  display: grid;
  gap: 1rem;

  .actions {
    display: flex;
    justify-content: flex-end;
  }
}
```

Avoid this:

```scss
.page {
  .section {
    .form {
      .field {
        .label {
          font-weight: 600;
        }
      }
    }
  }
}
```

Deep nesting raises specificity, couples the stylesheet to markup depth, and
makes refactoring harder.

## Modifiers And State

For component-local state, prefer readable state classes or Angular class
bindings.

```html
<section class="summary" [class.is-expanded]="expanded()">
  ...
</section>
```

```scss
.summary {
  .details {
    display: none;
  }

  &.is-expanded {
    .details {
      display: block;
    }
  }
}
```

Do not add BEM-like modifier classes. In component styles, prefer readable
local state classes such as `.is-selected`, `.is-expanded`, or `.has-error`.

## UI Libraries

No UI component library has been selected yet. If the project later adopts
Angular Material or another library:

- prefer documented component inputs and APIs
- keep library-specific APIs behind shared wrapper components when usage
  repeats
- do not style generated internal DOM or private classes from page SCSS
- avoid new `::ng-deep` usage

Pages should style app-owned wrapper components and local page structure, not
third-party internals.

## Migration Rule

Do not rename existing classes only for style preference. When touching a
component for a feature or bug fix, move it toward the component-local pattern
if the change is low risk.
