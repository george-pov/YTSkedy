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

Do not add a one-off media query when a shared grid, responsive column class,
field row, or action-row utility already expresses the behavior.

## Anti-Patterns

Do not use the layout layer as a Bootstrap-style component replacement. It
does not own buttons, cards, alerts, form controls, color, typography, or
component appearance.

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
