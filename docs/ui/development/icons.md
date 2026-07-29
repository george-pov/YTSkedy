# UI Icons

The Angular UI uses the app-owned `app-icon` component for shared icons. The
component renders Angular Material `mat-icon` with one optimized, self-hosted
Material Symbols Outlined font.

## Ownership

- Source of truth for supported icon names:
  `src/ui/src/app/shared/components/icon/icon.ts`.
- Generated font:
  `src/ui/public/fonts/material-symbols-outlined/material-symbols-outlined.woff2`.
- Font provenance:
  `src/ui/public/fonts/material-symbols-outlined/SOURCE.md`.
- Generator:
  `scripts/ui/Update-MaterialSymbolsSubset.ps1`.

Pages and shared controls should use `app-icon` or an existing shared control's
icon input. Keep Angular Material font setup inside the shared icon boundary.

## Add An Icon

1. Confirm the required ligature name in the official Material Symbols
   catalog.
2. Add the name to `supportedIconNames` in `icon.ts`. Keep the array
   alphabetically sorted.
3. Use the new `IconName` through `app-icon` or a shared component icon input.
4. From the repository root, regenerate the font:

   ```powershell
   pwsh -NoLogo -NoProfile -File scripts/ui/Update-MaterialSymbolsSubset.ps1
   ```

5. Review the regenerated font and `SOURCE.md`. The script records the exact
   icon list, axes, Google Fonts CSS request, download date, and SHA-256 hash.

## Remove An Icon

1. Remove or replace every caller of the icon name.
2. Remove the name from `supportedIconNames`.
3. Run `Update-MaterialSymbolsSubset.ps1` again. The generated font will no
   longer contain the removed glyph.

The generator reads `supportedIconNames` directly and fails when the array is
empty, unsorted, duplicated, or uses unsupported syntax. Do not edit the WOFF2
file by hand.

## Validation

After changing the icon set:

```powershell
cd src/ui
npm run test -- -- --watch=false
npm run build
```

Return to the repository root and run:

```powershell
pwsh -NoLogo -NoProfile -File scripts/validate-docs.ps1
git diff --check
```

Unit tests verify the component and supported ligature names. Check affected
screens in a browser because jsdom does not render the font glyphs.
