# Package Modern And Classic Explorer Integrations

Status: accepted

Icon Replacer V1 must expose the same two product commands in the Windows 11
context menu and in `Show more options` without replacing or rewriting unrelated
shell registrations.

## Decision

- Ship one signed per-user MSIX containing the WinUI management app, the native
  shell extension, and the zero-window command host.
- Register the Windows 11 path through `windows.fileExplorerContextMenus` and
  native `IExplorerCommand` implementations.
- Register the classic path through
  `windows.fileExplorerClassicContextMenuHandler` for `Directory` and `.lnk`.
- Keep both paths bounded and delegate mutations to `IconReplacer.CommandHost`.
- Do not create raw `HKCU\Software\Classes` verbs as a fallback.
- Do not launch the management window from either Explorer path.
- Treat visual Explorer registration as an explicit manual gate. The normal
  development state remains uninstalled unless a visual proof run is approved.

## Consequences

- The classic menu may coexist with StartAllBack and other shell extensions only
  when the lifecycle guard proves that every unrelated registration is unchanged
  and Explorer keeps the same process identity.
- The classic handler is capped at 8 collections, 30 icons per collection, and
  242 command IDs. Overflow stays available in the management app.
- Install, update, failure recovery, and uninstall must remove only Icon Replacer
  registrations while preserving `%USERPROFILE%\.icons` and restore history.
- Native smoke proof is necessary but does not replace the manual folder, `.lnk`,
  picker, preview, restore, and failure-state Explorer matrix.

## Supersedes

This ADR supersedes ADR-0002's Modern-only product target and raw-HKCU fallback.
