# Cache Catalog And Keep UI Navigation Non-Blocking

Status: accepted

The management app is an icon browser. Navigation and previews must feel
immediate even when the Icon Library contains hundreds of files.

## Decision

- Scan and validate the Icon Library once per refresh, off the UI thread.
- Keep an in-memory immutable catalog snapshot and derive category/search views
  from that snapshot without touching disk.
- Load icon thumbnails lazily for visible, virtualized elements and reuse a
  bounded cache.
- Load restore history independently from the catalog.
- Run packaging and developer-tooling probes only from an explicit diagnostics
  action. They are forbidden from startup, route changes, search, filtering,
  and theme changes.
- Route changes update visible state first and never launch child processes.

## Consequences

- The generic route snapshot composition is no longer the WinUI rendering
  architecture. AppModel snapshots remain useful for CLI, diagnostics, shell,
  and test contracts.
- Refresh is the explicit invalidation boundary until file-system watching is
  introduced.
- Performance proof records cold startup, first catalog render, route switch,
  search/filter, and theme-switch timings.

