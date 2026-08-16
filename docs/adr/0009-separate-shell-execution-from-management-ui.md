# Separate Shell Execution From Management UI

Status: accepted

Explorer commands and the full WinUI management app are separate product surfaces.
Invoking `Change icon...` or a library icon from the Explorer context menu must
not navigate to, reveal, or depend on the management window.

## Decision

- `Change icon...` opens only the native `.ico` picker owned by the Explorer
  command path. After selection, a lightweight out-of-process command host
  performs the mutation and records restore state.
- Dynamic collection and icon submenu commands invoke the same command host
  directly and apply immediately, without confirmation or visible app UI.
- The WinUI app starts only from its normal shortcut or explicit user launch.
- The management app owns visual browsing, importing, history, restore, theme,
  settings, and troubleshooting.
- The shell extension remains bounded: selection validation, menu enumeration,
  picker ownership, argument construction, and command-host launch only.

## Consequences

- The existing packaged-app activation bridge is transitional and must be
  removed from the final Explorer path.
- Apply and restore behavior remains owned by the shared Core/AppModel layer so
  shell and WinUI do not diverge.
- Manual proof must show that no `Icon Replacer` management window appears for
  either context-menu workflow.

