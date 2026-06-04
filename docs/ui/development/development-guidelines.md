# UI Development Guidelines

Development guidance for the YTSkedy Angular frontend.

## Project Defaults

- Languages and tools: TypeScript with strict compiler settings and SCSS.
- Framework: Angular v22.
- Package manager: npm.
- Test runner: Angular CLI unit test builder with Vitest and jsdom.
- UI library: none selected yet.
- Date handling: preserve API-provided local date/time and time-zone context.
  Add a dedicated date library only through an explicit feature decision.
- Principles: modularity, clear contracts, accessibility, auditability, and
  explicit scheduling semantics.
- Architecture direction: page-first, standalone Angular.

## Reference Baseline

Use the [official Angular style guide](https://angular.dev/style-guide) as the
primary upstream baseline for Angular naming, structure, dependency injection,
component member visibility, template simplicity, lifecycle hooks, and
class/style bindings.

Use
[Angular.love's component practices](https://angular.love/best-practices-of-working-with-angular-components)
as a secondary component-design reference for single-responsibility
components, `OnPush`, lazy loading, colocated files, and project organization.
Where that article differs from the official Angular style guide or this
repository's local conventions, this repository's UI docs decide the local
rule.

## Commands

Run commands from `src/ui/`:

```powershell
npm start
npm run build
npm test
```

Useful Angular CLI commands:

```powershell
npm run ng -- generate component pages/calendar-events/calendar-events-filter
npm run ng -- generate service pages/calendar-events/calendar-events-api
```

## TypeScript Standards

- Keep strict TypeScript and Angular template checking enabled. Do not weaken
  `tsconfig.json` settings to make feature code compile.
- Prefer type inference when the type is obvious from the assignment,
  initializer, or Angular API.
- Use explicit named types for app contracts, payloads, form models, and
  function boundaries where inference would hide intent.
- Do not use `any` for uncertain values. Use `unknown` and narrow it before
  reading properties or passing it into typed app code.
- Keep transformations pure when mapping form state, route data, runtime
  configuration, or API payloads.

## Angular Conventions

- Use standalone components, route-level lazy loading, and
  `ApplicationConfig` providers.
- Do not set `standalone: true` in Angular decorators. Standalone is the
  default for this Angular version.
- Do not introduce NgModules for new application code unless a compatibility
  requirement forces it.
- Keep `src/main.ts` as the bootstrap entry point.
- Keep root app setup in `src/ui/src/app/app.config.ts` and
  `src/ui/src/app/app.routes.ts`.
- Prefer `inject()` over constructor parameter injection for new code.
- Use `input()` and `output()` instead of `@Input()` and `@Output()` for new
  components and directives.
- Put host bindings and listeners in the `host` object of `@Component` or
  `@Directive`. Do not add new `@HostBinding` or `@HostListener` usage.
- Set `changeDetection: ChangeDetectionStrategy.OnPush` on new components.
- Use `protected` for component members that are only read by templates.
- Use `readonly` for injected dependencies, inputs, outputs, signals, and
  stable configuration values.
- Keep components focused on presentation and interaction orchestration.
- Move API calls, branching logic, validation rules, and transformations out
  of components.
- Prefer inline templates for small components when that keeps the component
  readable. Use external templates and styles when markup or styling needs its
  own file.
- When using external templates or styles, use paths relative to the component
  TypeScript file.
- Follow [`naming-conventions.md`](naming-conventions.md) for files, folders,
  selectors, and shared UI names.

## Runtime Configuration

Use runtime configuration for deploy-specific public settings when the first
deploy-specific setting is introduced. The target active file is
`src/ui/public/config/app-config.json`, and the app should load it as
`config/app-config.json` before Angular bootstrap.

When adding a setting that changes by environment:

- Add it to the typed contract under `src/ui/src/app/shared/config/`.
- Validate required values in the config loader.
- Consume it through an app-owned injection token or focused config
  abstraction.
- Update environment templates under `src/ui/config/environments/`.
- Document the setting in
  [`../architecture/runtime-configuration.md`](../architecture/runtime-configuration.md).

Do not hard-code environment-specific values in page services, components, or
routes. Runtime config is public browser data, so it must not contain secrets.

## Signals And RxJS

Use Angular signals as the default for user input state, local UI state, and
derived feature state.

Good signal use cases:

- selected calendar month and year
- current form input values
- validation display state
- loading flags
- error state
- selected stream template
- derived table rows or preview state

Use RxJS where it is the better tool for asynchronous streams and cancellation.

Good RxJS use cases:

- `HttpClient` request composition
- debounce and throttling
- search and typeahead
- request cancellation with `switchMap`
- retry and backoff behavior
- request chains where one response feeds another request
- shared streams consumed by multiple subscribers

Avoid converting everything to RxJS by default. Also avoid forcing complex
async request flows into signals when Observables make the behavior clearer.

Use `computed()` for derived signal state. Keep signal updates predictable and
immutable by using `set()` or `update()` rather than mutating existing arrays,
objects, or signal values in place.

## Forms

Prefer reactive forms over template-driven forms for complex validated forms.
Signals are acceptable for small explicit controls when state ownership is
clear.

For complex forms, keep implementation explicit:

- avoid `any` form references
- use named model types
- keep form creation in dedicated form or state files
- keep cross-field validation in named validator functions
- keep branching or conditional display rules in a dedicated state or service
  layer, not inline template conditionals

Do not duplicate backend scheduling rules in browser validation. UI validation
may improve responsiveness, but durable scheduling validation belongs in the
backend API.

## Templates

- Keep templates simple and move complex conditions, mapping, and derived
  values into named component members, computed signals, services, or pure
  functions.
- Use native Angular control flow: `@if`, `@for`, and `@switch` instead of
  `*ngIf`, `*ngFor`, and `*ngSwitch` for new template code.
- Use the `async` pipe for Observable values consumed directly by templates.
- Use `[class...]`, `[class]`, `[style...]`, and `[style]` bindings instead of
  `ngClass` and `ngStyle` for new code.
- Do not assume JavaScript globals such as `Date` or `new Date()` are
  available in templates. Expose needed values from TypeScript.

## Accessibility

- New and changed user-facing UI must meet WCAG AA minimums, including
  keyboard access, visible focus, color contrast, labels, errors, and ARIA
  usage.
- Prefer semantic HTML before ARIA. Add ARIA only when native elements and
  labels do not communicate the state or relationship.
- Keep keyboard focus order aligned with reading order.
- Manage focus explicitly after route changes, dialogs, validation failures,
  or submission results when default browser behavior would leave the user in
  the wrong context.
- Shared form-control wrappers must expose the accessibility inputs they own,
  such as labels, descriptions, required state, invalid state, and labelled-by
  relationships.

## Shared UI

Pages should consume app-owned shared UI components from
`src/ui/src/app/shared/components/` when a wrapper exists. Do not couple pages
to a UI library API when the interaction is repeated or app-owned.

Shared UI wrapper inputs and outputs should stay app-specific and
page-oriented. Repeated app-specific layout and behavior belong in shared UI
components.

Wrapper APIs should describe app intent:

- use names such as `variant`, `label`, `hint`, `required`, `disabled`, and
  `options`
- expose accessibility requirements through wrapper inputs when the wrapper
  owns the interactive element
- test the wrapper contract rather than private DOM structure

## Services And HTTP

- Design services around one responsibility and one clear owner.
- Use `providedIn: 'root'` for singleton services.
- Prefer `inject()` over constructor parameter injection for new services.
- Keep API calls, payload mapping, branching rules, and reusable
  transformations out of page components.
- Put page-flow services under the page flow that owns them. Put reused app
  services under `shared/services/`.
- Use `provideHttpClient(...)` in `app.config.ts` when the first HTTP client is
  introduced.
- Prefer functional interceptors with `withInterceptors(...)`.
- Do not import `HttpClientModule` into page components.

API services should receive base URLs from runtime configuration once runtime
configuration exists. Do not keep API URLs as private constants inside
page-flow services.

## Testing

Use [`unit-testing.md`](unit-testing.md) as the canonical unit testing policy.
Use [`end-to-end-testing.md`](end-to-end-testing.md) for optional browser-level
verification guidance.

Tests should live beside the code under test, but a file does not need a
`.spec.ts` only because it exists. Prioritize tests for behavior, app-owned
contracts, risky transformations, validation, payload mapping, request
building, and shared form-control logic.

Do not assert UI library internals, private component state, CSS layout, or
static markup that does not protect a behavior contract.

## Naming And Formatting

- Use [`naming-conventions.md`](naming-conventions.md) as the canonical UI
  naming reference.
- Follow existing formatting: Prettier print width 100 and single quotes.
- Use local `./models...` paths for application-local TypeScript imports when
  the target is in the same folder or a child folder. Use `src/app/...` import
  aliases only after the Angular workspace config supports them. Until then,
  prefer the shallowest clear relative path and avoid typo-prone long traversal
  paths such as `../../../shared/modles/...`.
- Keep file names kebab-case.
- Keep one primary concept per file.
- A `shared/utils/` folder may exist, but files inside it must be named by the
  behavior they own. Avoid generic `utils.ts`, `helpers.ts`, or `common.ts`.
- Do not introduce unrelated dependencies.
