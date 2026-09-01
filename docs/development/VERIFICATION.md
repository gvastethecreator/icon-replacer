# Verification

## GitHub Release Candidate `v1.0.0-rc.1` (2026-07-15)

- MSIX package version: `1.0.0.6`.
- Managed Release suite: 289 passed, 0 failed.
- WinUI/native/package Release build: pass.
- Native shell smoke: pass with 187 commands and 78.1 ms query.
- Public bundle: MSIX, CER, metadata, and SHA-256 manifest only; no PFX.
- MSIX: 47,991,336 bytes, valid `CN=IconReplacerDev` signature, SHA-256
  `5D22F3D823924B876A7B5C687595FAB4F4813783CDE51E03C1FF48E621563563`.
- Certificate thumbprint: `F235B1142A11E383C0673599772334C0F0797F4A`.
- Verdict: suitable for a GitHub prerelease with explicit development-certificate
  instructions. Not suitable for a stable/production-trusted release until the
  remaining manual and signing gates are closed.

Status: proposed
Date: 2026-07-07

## Required Before Claiming Implemented

- Build succeeds.
- Focused tests pass.
- CLI integration proves folder apply/restore.
- CLI integration proves `.lnk` apply/restore.
- Explorer context-menu flow is manually proven.
- Invalid icon and permission failure leave target unchanged.
- Install/uninstall behavior is proven.

## Command Proof

Current pre-shell checks:

```powershell
dotnet build IconReplacer.slnx
dotnet test IconReplacer.slnx --no-build
dotnet build src\IconReplacer.App\IconReplacer.App.csproj -p:Platform=x64
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Build-NativeShellExtension.ps1 -Configuration Debug -Platform x64
.\BuildAndRun.ps1 src\IconReplacer.App\IconReplacer.App.csproj -Detach
winapp ui list-windows -a IconReplacer.App
winapp ui inspect -w <hwnd> --depth 6
winapp ui search "Import" -a IconReplacer.App
winapp ui search "StatusInfoBar" -a IconReplacer.App
dotnet run --no-build --project src\IconReplacer.Cli -- diagnostics
dotnet run --no-build --project src\IconReplacer.Cli -- shell-plan
dotnet run --no-build --project src\IconReplacer.Cli -- shell-manifest
dotnet run --no-build --project src\IconReplacer.Cli -- shell-bridge [target]
dotnet run --no-build --project src\IconReplacer.Cli -- package-plan
dotnet run --no-build --project src\IconReplacer.Cli -- release-readiness
dotnet run --no-build --project src\IconReplacer.Cli -- accessibility-plan
dotnet run --no-build --project src\IconReplacer.Cli -- navigation-plan
dotnet run --no-build --project src\IconReplacer.Cli -- app-window [activation args]
dotnet run --no-build --project src\IconReplacer.Cli -- app-commands [--route <route-id>] [--record <record-id>] [--filter <filter>] [--icon <icon.ico>] [--collection <name>] [activation args]
dotnet run --no-build --project src\IconReplacer.Cli -- app-command-request <command-id> [--route <route-id>] [--record <record-id>] [--filter <filter>] [--icon <icon.ico>] [activation args]
dotnet run --no-build --project src\IconReplacer.Cli -- app-view [--route <route-id>] [--record <record-id>] [--filter <filter>] [--icon <icon.ico>] [--collection <name>] [--shell-target <target>] [--search <text>] [--category <name>] [--max <count>] [activation args]
dotnet run --no-build --project src\IconReplacer.Cli -- change-icon-workflow [--icon <icon.ico>] change-icon --target <path> --target-kind <folder|shortcut>
dotnet run --no-build --project src\IconReplacer.Cli -- action-request <action-id>
dotnet run --no-build --project src\IconReplacer.Cli -- activate
dotnet run --no-build --project src\IconReplacer.Cli -- activate change-icon --target <target> --target-kind <folder|shortcut>
dotnet run --no-build --project src\IconReplacer.Cli -- activate menu-apply <target> <icon-from-library.ico>
dotnet run --no-build --project src\IconReplacer.Cli -- activate-preview <icon.ico> change-icon --target <target> --target-kind <folder|shortcut>
dotnet run --no-build --project src\IconReplacer.Cli -- activate-apply <icon.ico> change-icon --target <target> --target-kind <folder|shortcut>
dotnet run --no-build --project src\IconReplacer.Cli -- activate-menu-apply <target> <icon-from-library.ico>
dotnet run --no-build --project src\IconReplacer.Cli -- home
dotnet run --no-build --project src\IconReplacer.Cli -- home stale
dotnet run --no-build --project src\IconReplacer.Cli -- browse adobe --max 5
dotnet run --no-build --project src\IconReplacer.Cli -- browse --category "Developer Tools" --max 5
dotnet run --no-build --project src\IconReplacer.Cli -- details <icon.ico>
dotnet run --no-build --project src\IconReplacer.Cli -- preview-change <target> <icon.ico>
dotnet run --no-build --project src\IconReplacer.Cli -- recent
dotnet run --no-build --project src\IconReplacer.Cli -- recent stale
dotnet run --no-build --project src\IconReplacer.Cli -- restore-workflow [record-id] [all|restorable|applied|restored|stale]
dotnet run --no-build --project src\IconReplacer.Cli -- restore-preview <record-id>
dotnet run --no-build --project src\IconReplacer.Cli -- batch-import <icon.ico> [icon2.ico ...]
dotnet run --no-build --project src\IconReplacer.Cli -- import-picker-request [collection]
dotnet run --no-build --project src\IconReplacer.Cli -- catalog-warnings
dotnet run --no-build --project src\IconReplacer.Cli -- target <folder-or-shortcut>
dotnet run --no-build --project src\IconReplacer.Cli -- picker-request <folder-or-shortcut>
dotnet run --no-build --project src\IconReplacer.Cli -- launch-request <folder-or-shortcut>
dotnet run --no-build --project src\IconReplacer.Cli -- change <target> <icon.ico>
dotnet run --no-build --project src\IconReplacer.Cli -- catalog
dotnet run --no-build --project src\IconReplacer.Cli -- collections
dotnet run --no-build --project src\IconReplacer.Cli -- collection-create <name>
dotnet run --no-build --project src\IconReplacer.Cli -- collection-import <collection> <icon.ico> [icon2.ico ...]
dotnet run --no-build --project src\IconReplacer.Cli -- status
dotnet run --no-build --project src\IconReplacer.Cli -- paths
dotnet run --no-build --project src\IconReplacer.Cli -- open-request <icon-library|imported|appdata|restore-state>
dotnet run --no-build --project src\IconReplacer.Cli -- menu
dotnet run --no-build --project src\IconReplacer.Cli -- menu-commands
dotnet run --no-build --project src\IconReplacer.Cli -- menu-invoke-preview <command-id> [target]
dotnet run --no-build --project src\IconReplacer.Cli -- menu-apply <target> <icon-from-library.ico>
dotnet run --no-build --project src\IconReplacer.Cli -- import <icon.ico> [display-name]
dotnet run --no-build --project src\IconReplacer.Cli -- doctor
dotnet run --no-build --project src\IconReplacer.Cli -- history
dotnet run --no-build --project src\IconReplacer.Cli -- history stale
```

`menu` should show `Change icon...`, menu state/status, non-empty one-level `.icons` categories, visible/total icon counts, and explicit omitted counts when caps hide overflow. Empty, warning, truncated, and unavailable states should provide a recommended action without mutating files.

`menu-commands` should show shell-facing descriptors derived from the menu snapshot: stable `change-icon`, safe `icon:<hash>` ids for visible Icon Library entries, explicit argument templates, category names, state/status, and `open-app` when caps hide overflow or the menu needs empty/unavailable recovery.

`menu-invoke-preview` should resolve one current menu command id against an optional target. Target-required commands should validate the selected target through shell-selection rules and return final arguments only for supported folders or `.lnk` shortcuts. Non-target commands such as `open-app` should remain invocable without a target. Unknown command ids and unsupported targets should not produce launch/apply arguments.

`menu-apply` should model a direct dynamic-submenu choice: validate the selected shell target, require the icon path to be a valid entry in the current Icon Library catalog, then apply through the shared change workflow. External icons must be rejected before mutation.

`collections` should list one-level Icon Library folders, include `Imported`, and report per-collection valid icon counts.

`collection-create` should create a sanitized one-level Icon Library folder, be idempotent for existing folders, and reject empty names.

`collection-import` should import valid local `.ico` files into a one-level Icon Library collection, create that collection if needed, reuse duplicate content in that collection, and return per-file failures without aborting the whole batch.

`target` should show whether the shell command is enabled for a selected target. A single local folder or `.lnk` should be supported; existing non-`.lnk` files, missing paths, remote paths, and multi-select should be disabled with a specific reason before mutation.

`picker-request` should show the future file-picker contract for `Change icon...`: target status, target kind when supported, `.ico` file type filter, Icon Library initial directory, and single-select behavior. Unsupported targets must disable the picker before mutation.

`launch-request` should show the future Explorer-to-app handoff for `Change icon...`: `change-icon --target <path> --target-kind <folder|shortcut>` app arguments when the selected target is supported, and no arguments when unsupported.

`change` should run the post-picker flow: validate the selected target first, then apply the chosen icon through the shared AppModel path. Unsupported selections must fail before import or restore-state writes.

`shell-plan` should show the accepted packaged dual integration path: MSIX
identity, native modern `IExplorerCommand`, packaged classic handler, retired raw
HKCU verbs, required prerequisites, and missing `winapp` as a blocker when
absent.

`shell-manifest` should show the future packaged manifest contract without registering Explorer: `windows.comServer`, `windows.fileExplorerContextMenus`, one stable CLSID, `IconReplacer.ShellExtension.dll`, `STA`, `IExplorerCommand`/`IExplorerCommandState`, and `Directory` plus `.lnk` targets.

`shell-bridge` should show the future native `IExplorerCommand` bridge contract without registering Explorer: manifest CLSID, shell extension DLL, required interfaces, supported item types, menu state, target status, command counts, invocable command counts, resolved arguments, disabled reasons, and safety rules. With a supported folder or `.lnk`, target-required commands should resolve arguments; with an unsupported target, target-required commands should be disabled and non-target commands such as `open-app` should remain invocable.

`package-plan` should show install/uninstall readiness without installing anything: per-user MSIX mode, `winapp`, native build tools (`cl.exe` plus Visual Studio MSBuild), package identity, native extension, dev signing, installer, install proof, uninstall proof, shell integration removal, `.icons` preservation, and restore-history preservation.

`release-readiness` should show the release evidence state without installing anything: build proof, test proof, CLI proof, diagnostics, package plan, accessibility proof, manual Explorer proof, release evidence packet, blocking count, warning count, and evidence commands. With no proof flags on this machine, it should report missing package/signing/manual/install proof as not release-ready while WinUI/native tooling gates pass.

`accessibility-plan` should show the future WinUI accessibility acceptance contract without launching WinUI: keyboard reachability, focus return, icon/control names, persistent errors, high contrast, 200% scaling, long-path handling, manual proof items, and the app surfaces that must satisfy them.

`navigation-plan` should show the future WinUI route contract without launching WinUI: default and fallback routes, top-level screens, workflow routes, primary commands, selection requirements, and every `AppActionKind` target mapped to a registered route.

`app-window` should show the future main-window startup state without launching WinUI: selected route, window title, activation kind, route usability, route counts, diagnostic blocker/warning badges, shell status, and WinUI tooling state. Invalid activation arguments should fall back to the diagnostics route instead of opening a picker or mutating files.

`app-commands` should show the future WinUI command state without launching WinUI: route command ids, labels, kind, primary/secondary status, enabled/disabled state, target routes or app locations, and disabled reasons. The default route should use the current `app-window` route; `--route <route-id>` should inspect any registered route. For `restore-preview`, optional `--record` and `--filter` should enable `confirm-restore` only when the selected restore workflow can restore.

`app-command-request` should resolve one route command id into a future WinUI button intent without launching WinUI: navigation target for navigation commands, safe open-location request for location commands, refresh requirement for refresh commands, workflow id for workflow commands, and disabled reasons for unavailable commands. It must not open Explorer, open pickers, apply icons, restore records, or mutate state.

`app-view` should show the future WinUI route composition without launching WinUI: selected window route, requested route, content kind, content readiness, summary, command counts, and route-specific content counts. It should render concrete content for home, browser, icon details, import, collections, history, diagnostics, package, shell plan, shell bridge, accessibility, restore, and Explorer-launched change-icon routes. The `package-plan` route should use full packaging inputs when available and include native build-tooling blockers in its counts. The `shell-bridge` route should accept `--shell-target <target>` and report bridge protocol, CLSID, menu state, target status, command counts, invocable counts, visible/omitted icon commands, and safety-rule count without registration or mutation. The `icon-browser` route should respect `--search`, `--category`, and `--max`, and report search/category state, visible/matched/total counts, omitted count, and category count. `icon-details` should render selected-icon metadata when `--icon` is supplied and a non-ready selection placeholder when it is not. `import-icons` should render picker destination, allowed file type, multi-select state, and optional collection target without copying icons. `change-icon` should render non-mutating workflow content for waiting-for-icon, selected-ready, and blocked states. `restore-preview` should render restore workflow content for no-selection, selected-ready, and selected-blocked states without mutation.

`change-icon-workflow` should show the future Explorer-launched Change Icon route state without mutation: target readiness, picker readiness, selected icon preview readiness, apply readiness, selected icon details, and stable disabled reasons for unsupported targets or invalid icons.

`status` should show core feature readiness, setup actions, and explicit shell integration state. After IR-000, shell integration should be `NotConfigured` until the accepted Modern path is installed. When packaging/tooling blockers are detected, setup actions should include `review-package-plan` without installing anything.

`action-request` should map visible setup/home action ids to stable WinUI intents. `review-package-plan` should target the package-plan route when packaging blockers are visible. It should reject unknown action ids, disable known actions that are not currently visible, and avoid opening windows, pickers, shell locations, or mutating state.

Operation feedback should produce shared user-facing messages for apply, restore, import, batch import, and error paths. Apply/restore feedback should include a history action; batch-import feedback should distinguish partial failures from duplicate-only reuse.

`activate` should show packaged-app startup routing: no arguments return Home state, `change-icon` arguments return the validated launch/picker flow, `menu-apply` arguments return the direct submenu apply preview, and malformed arguments fail before picker or mutation logic.

`activate-preview` should show post-picker readiness for the future packaged app: activation arguments plus selected `.ico` path should validate target and icon together without mutation.

`activate-apply` should apply the activated post-picker flow through AppModel and return the same restore-record evidence as `apply`/`change`. Prefer temporary targets or isolated environment variables for CLI proof.

`activate-menu-apply` should apply a direct submenu activation through AppModel and return the same restore-record evidence as `menu-apply`. External icons and unsupported targets must be rejected before mutation.

`home` should show the composed first-screen state for the future WinUI app: readiness, setup actions, location targets, menu visible/total counts, and history counts. Filters should reuse the same history filter names as `history`, and package blockers should appear as setup actions when tooling status is known.

`browse` should show the composed catalog-browser state for the future WinUI app: search text, category filter, visible/matched/total counts, omitted counts, and warnings.

`details` should show the composed icon detail state for the future WinUI app: display name, category, library membership, byte length, image count, internal image entries, and recommended image.

`preview-change` should show the composed pre-apply state for the future WinUI picker flow: target status, target kind when supported, icon validation/details, `Can apply`, and the blocking reason when disabled. It must not import the icon, write target metadata, or create restore history.

`recent` should show the composed recent-change action state for the future WinUI app: filter, shown count, restore-enabled count, warning count, disabled count, and row-level reasons.

`restore-workflow` should show the future WinUI restore route state without mutation: loaded history, selected record when present, whether the route needs a record, is ready to restore, or is blocked, selected-record preview details, warnings, and stable disabled reasons. Missing selected records should produce a blocked workflow snapshot while keeping history visible.

`restore-preview` should show the composed restore confirmation state for the future WinUI app: current record status, target/icon availability, previous-state detail, restore action detail, `Can restore`, warning text when the applied icon file is missing, and a blocking reason when disabled. It must not mutate the target or restore history.

`diagnostics` should show the composed diagnostics state for the future WinUI app: blocking/warning counts, shell integration state, WinUI template availability, `winapp` availability, native build-tool availability, app locations, and health checks. Missing `winapp` or native build tools should produce exit code 2 and tell the user what setup path is needed.

`paths` should report Icon Library, Imported Icons, AppData, and Restore State locations with file/directory and exists/missing status.

`open-request` should return a safe request for known app locations only. Existing directories should use shell verb `open`, existing files should use `open-file`, and missing locations should be disabled without creating files.

`import` should return the stable `.icons\Imported` path and must not create duplicate files for the same icon bytes.

`batch-import` should return per-file status for a future multi-file picker flow. Duplicate bytes should report reused existing and must not increase catalog count.

`import-picker-request` should show the composed app import picker request for the future WinUI library-management flow: multi-select `.ico` filter, Icon Library initial directory, destination collection, and destination directory. Collection-targeted requests should create or reuse sanitized one-level collections; invalid collection names should fail before picker launch.

`catalog-warnings` should show the future catalog-warning review list for diagnostics/library cleanup: root and category context, skipped icon path, display name, validation code, message, and detail. It must not delete files, import icons, write target metadata, or create restore history.

For CLI mutation proof, prefer `apply <target> <icon.ico>` when the target exists and the test should exercise target autodetection. Keep `apply-folder` and `apply-shortcut` for explicit shell-selection paths.

For WinUI launch verification, use `BuildAndRun.ps1` from `winui-dev-workflow`; do not run the packaged `.exe` directly.

For WinUI action verification, inspect the launched packaged app with `winapp ui`. `ImportIconsButton`, `StatusInfoBar`, list content, and row-level `Restore` buttons should be visible through UI Automation. Do not open the native file picker in unattended automation unless the automation will also choose/cancel the dialog.

The WinUI app must ignore Explorer command arguments and must not read
`%AppData%\Icon Replacer\pending-activation.args`. Direct picker and menu-apply
proof belong to `IconReplacer.CommandHost`; normal app launch must show only the
management window.

`history` should show health markers such as `restorable`, `not-restorable`, `target-missing`, or `applied-icon-missing`. Filters should support `all`, `restorable`, `applied`, `restored`, and `stale`. Deleted temporary proof targets are expected to appear as `target-missing` while their restored history remains preserved.

## Manual Proof

Capture screenshots or notes for:

- context menu entry,
- file picker,
- folder before/apply/restore,
- `.lnk` before/apply/restore,
- invalid `.ico` error,
- protected folder or access denied error,
- empty Icon Library state,
- category from `.icons\<category>`,
- uninstall result.

## Evidence Storage

Recommended future path:

- `artifacts/manual/` for screenshots,
- `artifacts/logs/` for CLI and install logs,
- `artifacts/context-menu-recovery/` for normalized package, shell-registration, Explorer, and StartAllBack snapshots.

## Gallery First Verification (2026-07-13)

```powershell
dotnet build src\IconReplacer.App\IconReplacer.App.csproj -p:Platform=x64 -p:BuildNativeShellExtension=false
dotnet test IconReplacer.slnx --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File tests\ui\gallery-first-ui.ps1 -AppPid <pid> -ArtifactDir artifacts\ui-tests\gallery-first\automated-v18
powershell -NoProfile -ExecutionPolicy Bypass -File tests\ui\responsive-grid-ui.ps1 -AppPid <pid> -ArtifactDir artifacts\ui-tests\gallery-first\responsive-grid-v12
powershell -NoProfile -ExecutionPolicy Bypass -File tests\ui\accessibility-ui.ps1 -AppPid <pid> -ArtifactDir artifacts\ui-tests\gallery-first\accessibility-100 -MinimumScalePercent 100
# Launch on a 200% display before this second accessibility run.
powershell -NoProfile -ExecutionPolicy Bypass -File tests\ui\accessibility-ui.ps1 -AppPid <pid> -ArtifactDir artifacts\ui-tests\gallery-first\accessibility-200 -MinimumScalePercent 200
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Build-NativeShellExtension.ps1 -Configuration Release -Platform x64
artifacts\native\x64\Release\IconReplacer.ShellExtension.Smoke.exe artifacts\native\x64\Release\IconReplacer.ShellExtension.dll
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-MsixLifecycle.ps1 -ApproveExplorerRegistration
```

Expected result:

- App build passes with 0 warnings and 0 errors.
- Managed tests pass, including apply/restore compensation, concurrency, official assets, GitHub update parsing, guarded deployment evidence, WinUI/shell source contracts, and directory-link coverage.
- Gallery First UI Automation covers centered startup, Library/About navigation, and keyboard resize of the Collections pane.
- Accessibility source contracts cover Gallery First/About names, target-specific restore names, polite live regions, splitter semantics, and adaptive 200% toolbar states.
- Accessibility UI Automation at the machine scale must pass. The 200% and High Contrast runs remain open until captured.
- Responsive grid uses 3/6/8 columns across compact/reference/wide widths.
- Native COM smoke: modern commands stay enabled normally and hide after classic `IObjectWithSelection` dispatch. Previews, the 242-command cap, constrained 8-id range, and 250 ms full-catalog query budget must pass.
- Snapshot-only guard preserves unrelated handlers without package mutation.
- Official icon contract: canonical source hashes pass and deterministic regeneration produces rounded display PNGs, nine-frame ICOs, and taskbar variants.
- MSIX lifecycle: exact-package guarded upgrade, Explorer reload, Icon Library preservation, restore-state preservation, and unrelated-handler preservation. Clean uninstall proof remains open.
- Explorer registration: classic folder menu exposes one grouped `Change icon...` plus `Icon collections` pair with official app icons. Direct picker launch must pass. Remaining target/apply/restore matrix stays open until captured.
