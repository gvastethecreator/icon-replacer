# Gallery First Visual Direction

Status: selected
Date: 2026-07-13

## Visual Truth

- Light: `gallery-first-light.png`
- Dark: `gallery-first-dark.png`

The light image defines information architecture and density. The dark image
defines the dark palette, Mica/Acrylic treatment, and three-state theme control.

## Product Hierarchy

1. Icon previews are the primary content.
2. Collections are the primary filter and show small preview mosaics.
3. Search, import, open library, and preview size are persistent commands.
4. Recent changes are visible but secondary and always offer restore when safe.
5. Settings contains `System`, `Light`, and `Dark` theme choices.
6. Diagnostics and package state are not top-level destinations.

## Material Rules

- Use Mica for the window backdrop.
- Acrylic is limited to navigation, collection navigation, command surfaces,
  overlays, and the recent-changes surface.
- Keep the icon gallery substantially opaque for contrast and image clarity.
- High Contrast uses system brushes and disables decorative transparency.

## Interaction Rules

- All visible icon entries have a real `.ico` preview.
- Search and collection filtering update without rescanning disk.
- Preview size changes layout without changing the current selection.
- Switching Library, Recent, or Settings does not perform tooling probes.
- The app launches only from its normal shortcut; Explorer commands remain a
  zero-management-window workflow.

