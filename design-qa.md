# Design QA: Gallery First

final result: passed

## Comparison Target

- Source visual truth: `docs/design/gallery-first-light.png` and `docs/design/gallery-first-dark.png`
- Implementation: `artifacts/design-qa/implementation-light-final.png` and `artifacts/design-qa/implementation-dark-final.png`
- Viewport: implementation captured at its centered startup size of 1684 x 1067 physical pixels; the 1487 x 1058 source was normalized to that viewport for comparison.
- State: Library, All icons, empty search, 64 px default preview size, with equivalent Light and Dark theme states.

## Evidence

- Full view, Light: `artifacts/design-qa/light-full-comparison-final.png`
- Full view, Dark: `artifacts/design-qa/dark-full-comparison-final.png`
- Toolbar focus, Light: `artifacts/design-qa/light-toolbar-comparison-final.png`
- Toolbar focus, Dark: `artifacts/design-qa/dark-toolbar-comparison-final.png`
- Gallery focus, Light: `artifacts/design-qa/light-gallery-comparison-final.png`
- Gallery focus, Dark: `artifacts/design-qa/dark-gallery-comparison-final.png`
- Automated interaction evidence: `artifacts/ui-tests/gallery-first/automated-v18/results.json`
- Responsive grid evidence: `artifacts/ui-tests/gallery-first/responsive-grid-v12/results.json`

Focused comparisons were required because toolbar density, icon image bounds, two-line labels, and collection mosaics were too small to judge reliably from the full view alone.

## Findings

No actionable P0, P1, or P2 differences remain.

- Fonts and typography: Segoe UI follows the native WinUI visual language; compact command text, collection hierarchy, two-line icon labels, wrapping, and ellipsis remain readable at the reference viewport.
- Spacing and layout rhythm: the command surface is one compact row; the gallery distributes complete cells across the available width; collection and recent panes remain subordinate to the gallery.
- Colors and visual tokens: Light and Dark preserve clear surface hierarchy, system contrast, Mica/Acrylic intent, and the real icons provide the primary color variety.
- Image quality and asset fidelity: every visible catalog and history preview is decoded from a real `.ico`; images use uniform scaling with no clipping, stretching, transparency halo, placeholder art, or code-drawn substitute.
- Copy and content: labels are short and task-oriented. Actual collection names and restore history intentionally replace the illustrative mock data.
- Icons and affordances: native WinUI symbols are aligned in stable square hit targets and have accessible names/tooltips. The implementation intentionally uses icon commands instead of the mock's text commands to preserve compactness.
- Responsiveness and accessibility: adaptive side panes remain available at wide size and collapse at narrower widths; UI Automation exposes navigation, search, size, theme, grid, and restore controls.

Accepted implementation differences:

- The compact icon-only navigation rail is intentional and uses accessible names/tooltips.
- Gallery item surfaces provide predictable hit areas and a stable two-line label region for real-world filenames.
- Counts, collection names, icons, and recent history are live user data rather than static mock content.

## Comparison History

### Iteration 1

- Earlier P2: the top command surface was taller than the selected visual direction.
  Fix: reduced vertical padding from 6 to 4 px, controls from 34/36 to 32 px, and the title to the native strong-body style.
- Earlier P2: four large cards per row made the icon library feel sparse and reduced scan efficiency.
  Fix: changed the initial preview from 72 to 64 px, reduced cell width overhead from 40 to 24 px, and narrowed the side panes to reveal six complete previews per row.
- Earlier P2: collection names could split inside a word.
  Fix: reduced collection mosaics to 20 px and enabled whole-word wrapping.

Post-fix evidence: all six `*-comparison-final.png` artifacts listed above. No icon image is clipped and no persistent control overlaps or overflows.

### Iteration 2

- Earlier P2: fixed-width gallery cells left inconsistent horizontal remainder space at different window widths.
  Fix: calculate column count from the live viewport, distribute cell width across the row, and let the rendered preview grow by up to 8 px when spare width is available.
- Earlier P2: forced `SymbolIcon` width/height clipped Refresh, Import, Open, and Restore glyphs.
  Fix: removed glyph-level clipping constraints and retained stable 34/36 px button hit targets.
- Earlier P2: collection titles competed horizontally with four small previews.
  Fix: moved title and count to one line above four 24 px collection previews.

Post-fix evidence: `responsive-grid-v2/results.json` reports 3, 6, and 8 columns; cell-width spread is at most 1 px and gap spread is at most 2 px. Updated full and focused Light/Dark comparisons show complete command glyphs and collection previews.

### Iteration 3

- Earlier P2: the Collections pane was fixed-width and its persistent divider competed with the gallery.
  Fix: widened the default pane to 252 px, added a 220-380 px splitter, and kept its neutral-gray handle hidden until a deliberate hover. The handle fades in after 140 ms, fades out smoothly, and shows a horizontal-resize glyph while pressed.
- Earlier P2: one-line and two-line icon names did not share a stable reading baseline.
  Fix: reserved a fixed 36 px two-line label region and top-aligned every label inside it.
- Earlier P2: resizing the Collections pane repeatedly recalculated three layout properties on every icon model.
  Fix: suppress gallery recalculation during drag, coalesce pane-width writes to one per render frame, and publish final cell metrics through one shared observable object instead of 545 individual models.
- Reverted direction: the darker command-surface experiment was rejected after review.
  Fix: restored the toolbar to the same Acrylic surface used before that experiment while preserving its compact height and nearly transparent borders.

Post-fix evidence: `automated-v18/results.json` includes centered startup and keyboard splitter proof and reports 14 passing interactions. `responsive-grid-v12/results.json` deterministically resets the preview to 64 px and reports 3, 6, and 8 columns with at most 1 px cell-width and gap spread.

### Iteration 4

- Earlier P2: the expanded navigation pane used more width than its three short destinations required.
  Fix: set its open width to 184 px while preserving the 48 px compact rail.
- Earlier P2: the pressed splitter indicator could be clipped by the gallery surface.
  Fix: reserve a dedicated 20 px interaction channel, place the splitter above neighboring surfaces, and keep the neutral line and horizontal-resize glyph inside that channel.
- Earlier P2: the main window inherited a platform-default launch position.
  Fix: size and center the window in one DPI-aware `MoveAndResize` operation against the primary monitor work area before activation.

Post-fix evidence: UI Automation measures the expanded navigation pane at 184 logical px and the splitter at 20 logical px. The startup-centering assertion compares the real window bounds with the monitor work area and passes within a 2 px tolerance.

## Primary Interactions Tested

- Library startup and catalog population
- Real icon preview grid
- In-memory search across different collections
- Preview size adjustment
- Recent and Settings navigation
- Light and Dark theme switching
- Theme preference persistence
- Centered startup within the monitor work area
- Compact and expanded navigation pane sizing
- Return to Library

Result: 14 passed, 0 failed, plus responsive layout proof at three widths. Browser console checks are not applicable to this native WinUI application; build output is clean and UI Automation completed without application errors.

## Follow-up Polish

- P3: a future release may offer a list-density mode alongside the preview-size slider, but it is not needed for the current core workflow.
