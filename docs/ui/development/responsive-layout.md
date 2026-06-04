# UI Responsive Layout

The app should grow a small global layout utility layer only when repeated
responsive page structure appears. The current UI has only an initial
calendar-events route component, so this document defines production layout
rules before adding broad utilities.

## Source Of Truth

If repeated breakpoints, spacing, and container values are introduced, keep
them in shared SCSS partials under:

```text
src/ui/src/styles/
```

Do not copy breakpoint or spacing values into many component styles. If a
repeated layout need does not fit approved tokens, update the token set through
a deliberate design change instead of adding arbitrary local utility classes.

## Breakpoint Ownership

Viewport breakpoint rules belong in the shared responsive layout layer, not in
page or component stylesheets. New Angular component SCSS must not add local
`@media` rules to change layout across screen widths.

Use the approved responsive utilities instead:

- `app-container` for shared page width and horizontal padding
- `app-grid` for responsive 12-column structure
- `app-col-{1..12}` and `app-col-{breakpoint}-{1..12}` for column spans
- `app-field-row` for compact form-control wrapping
- `app-actions` for page and form action wrapping
- approved gap and spacing utilities for repeated layout spacing

If a responsive layout need cannot be expressed with these utilities, update
the shared layout system and this document instead of adding a local component
media query. The shared layout layer may use media queries internally because it
is the app-owned breakpoint abstraction.

## Suggested Utility Scope

If a layout layer is added, keep it small and layout-only. Suitable utilities:

- `app-container`
- `app-grid`
- `app-col-{1..12}`
- responsive column spans such as `app-col-md-6`
- approved gap and spacing utilities
- `app-field-row`
- `app-actions`

Do not add global utilities for colors, typography, visibility, order, offsets,
display, broad flex behavior, or component appearance unless a later design
decision approves that expansion.

## Containers

Use `app-container` for shared page width and horizontal padding. The app shell
should provide it when routed pages share the same content width.

Do not nest `app-container` inside another `app-container`. Do not use
`app-container` for vertical page spacing; keep page or shell block spacing in
the owning component.

```html
<main class="app-container">
  <router-outlet />
</main>
```

## Grids

Use `app-grid` for repeated 12-column page and form structures. Direct
children should span all columns by default on small screens and receive
`min-width: 0` so long stream titles cannot break the grid.

Use column classes on direct grid children when a wider breakpoint needs more
than one column.

```html
<form class="app-grid app-gap-6">
  <app-year-select class="app-col-md-3" />
  <app-month-select class="app-col-md-3" />
</form>
```

Avoid unnecessary `app-col-12` on direct grid children. It should already be
the mobile and default behavior.

## Field Rows

Use `app-field-row` for compact form-control rows where fixed-width app field
adapters should sit beside each other when space allows and wrap by available
width.

```html
<div class="app-field-row">
  <app-year-select />
  <app-month-select />
</div>
```

`app-field-row` is form-only. Do not use it as a generic flex-row utility for
layout, navigation, cards, action bars, or component appearance.

## Actions

Use `app-actions` for page or form action areas, not every button group.

```html
<div class="app-actions">
  <button type="button">Preview</button>
  <button type="submit">Schedule</button>
</div>
```

If a specific page needs different alignment or mobile stacking, put that
small rule in the owning component stylesheet. Do not add BEM-like global
modifier classes for action-row variants.

## Component Hosts

Shared form-control wrappers that can be direct grid children should expose a
grid-friendly host. Prefer `display: block` for field wrappers so column
classes apply to the Angular component host.

```scss
:host {
  display: block;
}

.field {
  width: 100%;
}
```

## Local SCSS

Use local component SCSS for:

- page titles and domain-specific visual hierarchy
- component state and validation display
- fixed-format field behavior
- table behavior specific to calendar events
- shell-specific vertical page padding
- visual treatment that belongs to one component

Do not add viewport breakpoint media queries in component SCSS for responsive
layout. Use the shared grid, responsive column classes, field row, action-row,
container, gap, and spacing utilities instead.

## Anti-Patterns

Do not use the layout layer as a Bootstrap-style component replacement. It
does not own buttons, cards, alerts, form controls, color, typography, or
component appearance.

Do not add a local component `@media` rule to rearrange page regions, cards,
headers, forms, fields, navigation, or action areas. Move repeated responsive
layout behavior into the shared layout system.

Do not use `app-field-row` as a generic flex utility. It is only for compact
form-control rows whose children have app-owned field adapter widths.

Do not install Tailwind or another broad styling system only to solve one
layout issue. Reconsider that separately if repeated real page migrations show
the small SCSS layer is insufficient.

## Responsive Review

For responsive changes, review:

- narrow, medium, and wide viewport behavior
- page shell width and horizontal padding
- controls wrapping without clipped labels or values
- tables with long English and Russian titles
- action rows at narrow and wide sizes
- long labels, validation text, and user-entered content
- keyboard focus order and reading order
- whether local SCSS remains limited to component-specific behavior
