# Shell Integration

Status: proposed
Date: 2026-07-07

## Current Decision

Modern Shell Integration is the accepted V1 product direction. Classic Shell Integration remains documented as a prototype or fallback path only.

## Modern Shell Integration

Expected implementation:

- packaged app with package identity,
- `windows.comServer` manifest registration,
- `windows.fileExplorerContextMenus` manifest registration,
- native `IExplorerCommand` command extension,
- registration for folders and `.lnk`,
- `Change icon...` as the first command,
- one-level Icon Library categories from the AppModel menu snapshot and command descriptors when feasible,
- shared AppModel manifest contract for `windows.comServer`, `windows.fileExplorerContextMenus`, `Directory`, and `.lnk` before any Explorer registration.
- shared AppModel shell-bridge route content so WinUI can review bridge status, resolved command counts, target status, and safety rules before Explorer registration.
- debug activation bridge through `%AppData%\Icon Replacer\pending-activation.args` when direct `winapp --args` activation is unreliable during development.

Done means the command appears in the Windows 11 context menu path and works for folder and `.lnk` targets.

Current implementation status: the native x64 DLL builds, exports the COM class factory entry points, is copied into the packaged app layout, and the package manifest contains the COM/context-menu entries. Final Explorer registration, package identity/signing/install proof, uninstall proof, and dynamic submenu enumeration remain open.

## Classic Shell Integration

Expected implementation:

- per-user `HKCU\Software\Classes\Directory\shell\IconReplacer\command`,
- per-user `HKCU\Software\Classes\lnkfile\shell\IconReplacer\command`,
- quoted command path,
- launches CLI/app picker path,
- documented as classic/fallback behavior.

Done means exported registry keys show only Icon Replacer-owned entries and the command works from `Show more options` or the classic context menu path.

## Shared Requirements

- No admin required for normal use.
- No heavy work on Explorer UI paths.
- No deep catalog scan on every menu open without bounds; use `IconMenuService` limits.
- Native shell code should consume `IconMenuCommandService` descriptors instead of inventing command ids or app arguments.
- Native shell invocation should use `IconMenuCommandInvocationService` to resolve selected-target arguments before launching the app/command path.
- Native shell bridge proof should use `ShellExtensionBridgeService` so manifest identity, menu state, selected target status, resolved arguments, disabled reasons, and safety rules remain aligned before C++/COM implementation.
- Direct submenu icon choices should launch the packaged app with `menu-apply <target> <icon-from-library.ico>` and let `AppMenuApplyActivationService`/`ActivatedMenuApplyService` validate and apply through the shared AppModel path.
- The packaged manifest should follow `ShellManifestContractService`: one stable CLSID, `IconReplacer.ShellExtension.dll`, `STA`, and `desktop5:ItemType` entries for `Directory` and `.lnk`.
- Clear unsupported-target behavior.
- Install/uninstall proof required.
