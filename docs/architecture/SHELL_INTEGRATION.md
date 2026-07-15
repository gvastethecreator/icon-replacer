# Shell Integration

Status: implementation complete; current-package lifecycle and visual proof pending
Date: 2026-07-14

## Current Decision

V1 ships both packaged Explorer paths from the same MSIX: modern `IExplorerCommand` entries for the Windows 11 menu and a packaged classic handler for `Show more options`. Both delegate mutations to the zero-window command host; the management UI is never launched by a context-menu command.

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

Done means the command appears in the Windows 11 context menu path and works for folder and `.lnk` targets.

Current implementation status: the native x64 DLL builds and exports the COM class factories; dynamic collection and icon commands are registered for `Directory` and `.lnk`; the complete package is signed and installs successfully; and a collection command has applied a real icon to a folder from Explorer. Final manual acceptance still needs clean evidence for the direct picker, `.lnk` apply/restore, and failure states.

## Classic Shell Integration

Implemented compatibility path:

- packaged `windows.fileExplorerClassicContextMenuHandler` registration for `Directory` and `.lnk`,
- one native handler implementing `IShellExtInit`, `IContextMenu`, `IContextMenu2`, and `IContextMenu3`,
- a separator followed by `Change icon...` and `Icon collections`, both using the application icon,
- one representative `.ico` preview for every collection and the actual `.ico` preview for every icon command,
- eager preview attachment while the bounded menu is built, so rendering does not depend on `WM_INITMENUPOPUP` forwarding from Explorer or StartAllBack,
- WIC decoding into premultiplied 32-bit ARGB DIB sections, which gives the classic menu real alpha-bearing bitmaps rather than raw device-dependent icon color planes,
- direct invocation through `IconReplacer.CommandHost.exe`, without opening the management window.

The earlier raw `HKCU` verb prototype is no longer the active classic integration.

The classic handler is deliberately bounded to 8 visible collections, 30 icons per collection, and 242 command ids including the two root commands. If the library is larger, it adds a disabled overflow note while the management app continues to show the complete catalog. This prevents Icon Replacer from consuming Explorer's command-id range or delaying unrelated shell extensions.

Explorer also instantiates packaged `IExplorerCommand` verbs while constructing
the classic menu. To prevent the modern verbs and the preview-capable classic
handler from producing duplicate entries, each modern root implements
`IObjectWithSelection`: the normal modern state query remains enabled, while a
selection injected through the classic dispatch path makes `GetState` return
`ECS_HIDDEN`. Native smoke locks both sides of this coexistence contract before
validating the classic previews.

## Shared Requirements

- No admin required for normal use.
- Keep Explorer work bounded and enforce a measured 250 ms query budget against the real catalog.
- Enumerate only `%USERPROFILE%\.icons` and its immediate collection folders; do not recurse.
- Attach previews before returning from `QueryContextMenu`; do not rely on optional submenu-open notifications.
- Native shell code should consume `IconMenuCommandService` descriptors instead of inventing command ids or app arguments.
- Native shell invocation should use `IconMenuCommandInvocationService` to resolve selected-target arguments before launching the app/command path.
- Native shell bridge proof should use `ShellExtensionBridgeService` so manifest identity, menu state, selected target status, resolved arguments, disabled reasons, and safety rules remain aligned before C++/COM implementation.
- Direct submenu icon choices should launch `IconReplacer.CommandHost` with a validated target and current Icon Library icon; the host applies through the shared Core/AppModel path without opening the management app.
- The packaged manifest should follow `ShellManifestContractService`: one stable CLSID, `IconReplacer.ShellExtension.dll`, `STA`, and `desktop5:ItemType` entries for `Directory` and `.lnk`.
- Clear unsupported-target behavior.
- Install/uninstall proof required.
- Install/uninstall proof must hash every non-Icon-Replacer `Directory`, `Folder`, `AllFilesystemObjects`, and `lnkfile` shell registration before and after the lifecycle.
- The lifecycle test must fail if Explorer restarts or crashes; it never force-restarts Explorer.

Current machine state: the previous development candidate is installed, with
shell DLL SHA-256 `9B96CA24BF044667C9EB79DAF163564800659ED8B806E4C0EFFC7F34FE5551D3`.
The duplicate-menu fix candidate DLL has SHA-256
`3C3DF9AA78F5830ABE53E3FC6DF5198FF36C3AD4FDDCF95C918983BFE9F5EB40`,
so the running Explorer has not loaded this fix. Snapshot-only proof still
normalizes the same 57 non-Icon-Replacer entries and StartAllBack remains loaded.
Updating the package and performing visual proof require an approved guarded run.

The earlier `pending-activation.args` bridge has been removed from the WinUI app.
Explorer commands are accepted only by `IconReplacer.CommandHost`; launching the
management shortcut always opens the management experience.
