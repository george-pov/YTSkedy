# UI Styling

YTSkedy uses SCSS for application and component styles.

## Goals

- Keep styling predictable under Angular component encapsulation.
- Keep global styles small and intentional.
- Use names that explain ownership without adding unnecessary ceremony.
- Avoid BEM-like `block__element` and `block--modifier` names for new styles.
- Prefer the simplest layout model that communicates intent. Flexbox is the
  default choice for component-local alignment and one-dimensional layout
  because it is easier to read and reason about than a broader display
  toolbox.
- Reduce cognitive complexity in styling the same way code should reduce
  cognitive complexity: fewer concepts, fewer special cases, and clearer local
  ownership.
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

Viewport breakpoint media queries for layout belong in the approved responsive
layout layer, not in component stylesheets. Use
[`responsive-layout.md`](responsive-layout.md) before adding responsive layout
behavior.

## Shared SCSS Partials

Keep shared partials narrow and semantic. Prefer mixins and tokens that
describe app-owned styling intent instead of selectors that emit CSS on import.

Use an accessibility partial for primitives such as visually hidden text only
when those primitives are used in more than one component.

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

Component SCSS should not add local viewport `@media` rules for responsive
layout. Use the shared `app-container`, `app-grid`, responsive `app-col-*`,
`app-field-row`, `app-actions`, gap, and spacing utilities instead. If those
utilities cannot express the layout, update the shared layout layer rather than
adding a one-off breakpoint.

For local component styles, avoid BEM-like class names such as
`.calendar-page__hero`, `.calendar-card--selected`, or
`.form-field__message`. Use short component-local structure and state names
instead.

Application chrome and global layout utilities should also use simple
app-prefixed names for new styles.

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

## Display Model Choice

Prefer flexbox for local component layout when the structure is one-dimensional:
horizontal or vertical alignment, centering, spacing between siblings, toolbars,
headers, button rows, inline labels, and compact visual marks.

Use other display modes only when they express the behavior more directly:

- Use the shared `app-grid` and column utilities for responsive page, shell,
  and form structure.
- Use CSS grid only for genuinely two-dimensional component-local layout.
- Use native block and inline flow when no layout primitive is needed.
- Avoid `inline-grid`, `grid`, floats, absolute positioning, or table display
  only to center or align simple content when flexbox is enough.

The goal is not to ban CSS features. The goal is to keep styling easy to read
by using a small, predictable layout toolset.

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

Angular Material and Angular CDK are available to support app-owned shared UI
components.

Do not style Angular Material internals such as `.mat-mdc-*` from component
styles. Treat internal Material DOM and classes as private implementation
details.

Prefer these options, in order:

1. App-owned shared component inputs and host classes.
2. Material component inputs and documented APIs inside shared wrappers.
3. Material theming and override mixins.
4. Design tokens or CSS custom properties exposed by Material.
5. A custom host class on the Material component or wrapper component.

When a form control, button, dialog, toolbar, or other repeated control needs
app-specific behavior or styling, prefer a shared wrapper component that owns
the app API and delegates to Angular Material internally.

Pages should style app-owned wrapper components and local page structure, not
third-party internals. Keep Material customization behind supported APIs:

- global theme setup in `src/ui/src/styles.scss`
- Material Sass APIs and CSS custom properties documented by Material
- app-owned wrapper inputs and host classes
- normal Angular component styles scoped by the component boundary

Avoid new `::ng-deep` usage. Angular keeps it only for compatibility, and it
breaks the normal component style boundary.

## Migration Rule

Do not rename existing classes only for style preference. When touching a
component for a feature or bug fix, move it toward the component-local pattern
if the change is low risk.
