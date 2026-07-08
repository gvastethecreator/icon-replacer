# Verification

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
dotnet run --no-build --project src\IconReplacer.Cli -- diagnostics
dotnet run --no-build --project src\IconReplacer.Cli -- shell-plan
dotnet run --no-build --project src\IconReplacer.Cli -- activate
dotnet run --no-build --project src\IconReplacer.Cli -- activate change-icon --target <target> --target-kind <folder|shortcut>
dotnet run --no-build --project src\IconReplacer.Cli -- activate-preview <icon.ico> change-icon --target <target> --target-kind <folder|shortcut>
dotnet run --no-build --project src\IconReplacer.Cli -- home
dotnet run --no-build --project src\IconReplacer.Cli -- home stale
dotnet run --no-build --project src\IconReplacer.Cli -- browse adobe --max 5
dotnet run --no-build --project src\IconReplacer.Cli -- browse --category "Developer Tools" --max 5
dotnet run --no-build --project src\IconReplacer.Cli -- details <icon.ico>
dotnet run --no-build --project src\IconReplacer.Cli -- preview-change <target> <icon.ico>
dotnet run --no-build --project src\IconReplacer.Cli -- recent
dotnet run --no-build --project src\IconReplacer.Cli -- recent stale
dotnet run --no-build --project src\IconReplacer.Cli -- batch-import <icon.ico> [icon2.ico ...]
dotnet run --no-build --project src\IconReplacer.Cli -- target <folder-or-shortcut>
dotnet run --no-build --project src\IconReplacer.Cli -- picker-request <folder-or-shortcut>
dotnet run --no-build --project src\IconReplacer.Cli -- launch-request <folder-or-shortcut>
dotnet run --no-build --project src\IconReplacer.Cli -- change <target> <icon.ico>
dotnet run --no-build --project src\IconReplacer.Cli -- catalog
dotnet run --no-build --project src\IconReplacer.Cli -- status
dotnet run --no-build --project src\IconReplacer.Cli -- paths
dotnet run --no-build --project src\IconReplacer.Cli -- menu
dotnet run --no-build --project src\IconReplacer.Cli -- import <icon.ico> [display-name]
dotnet run --no-build --project src\IconReplacer.Cli -- doctor
dotnet run --no-build --project src\IconReplacer.Cli -- history
dotnet run --no-build --project src\IconReplacer.Cli -- history stale
```

`menu` should show `Change icon...`, non-empty one-level `.icons` categories, visible/total icon counts, and explicit omitted counts when caps hide overflow.

`target` should show whether the shell command is enabled for a selected target. A single local folder or `.lnk` should be supported; existing non-`.lnk` files, missing paths, remote paths, and multi-select should be disabled with a specific reason before mutation.

`picker-request` should show the future file-picker contract for `Change icon...`: target status, target kind when supported, `.ico` file type filter, Icon Library initial directory, and single-select behavior. Unsupported targets must disable the picker before mutation.

`launch-request` should show the future Explorer-to-app handoff for `Change icon...`: `change-icon --target <path> --target-kind <folder|shortcut>` app arguments when the selected target is supported, and no arguments when unsupported.

`change` should run the post-picker flow: validate the selected target first, then apply the chosen icon through the shared AppModel path. Unsupported selections must fail before import or restore-state writes.

`shell-plan` should show the accepted Modern shell integration path: MSIX package identity plus native `IExplorerCommand`, Classic HKCU fallback/prototype status, required prerequisites, and missing `winapp` as a blocker when absent.

`status` should show core feature readiness, setup actions, and explicit shell integration state. After IR-000, shell integration should be `NotConfigured` until the accepted Modern path is installed.

`activate` should show packaged-app startup routing: no arguments return Home state, `change-icon` arguments return the validated launch/picker flow, and malformed arguments fail before picker or mutation logic.

`activate-preview` should show post-picker readiness for the future packaged app: activation arguments plus selected `.ico` path should validate target and icon together without mutation. Unit tests must cover the paired apply operation because it mutates disk.

`home` should show the composed first-screen state for the future WinUI app: readiness, setup actions, location targets, menu visible/total counts, and history counts. Filters should reuse the same history filter names as `history`.

`browse` should show the composed catalog-browser state for the future WinUI app: search text, category filter, visible/matched/total counts, omitted counts, and warnings.

`details` should show the composed icon detail state for the future WinUI app: display name, category, library membership, byte length, image count, internal image entries, and recommended image.

`preview-change` should show the composed pre-apply state for the future WinUI picker flow: target status, target kind when supported, icon validation/details, `Can apply`, and the blocking reason when disabled. It must not import the icon, write target metadata, or create restore history.

`recent` should show the composed recent-change action state for the future WinUI app: filter, shown count, restore-enabled count, warning count, disabled count, and row-level reasons.

`diagnostics` should show the composed diagnostics state for the future WinUI app: blocking/warning counts, shell integration state, WinUI template availability, `winapp` availability, app locations, and health checks. Missing `winapp` should produce exit code 2 and tell the user to run `/winui-setup`.

`paths` should report Icon Library, Imported Icons, AppData, and Restore State locations with file/directory and exists/missing status.

`import` should return the stable `.icons\Imported` path and must not create duplicate files for the same icon bytes.

`batch-import` should return per-file status for a future multi-file picker flow. Duplicate bytes should report reused existing and must not increase catalog count.

For CLI mutation proof, prefer `apply <target> <icon.ico>` when the target exists and the test should exercise target autodetection. Keep `apply-folder` and `apply-shortcut` for explicit shell-selection paths.

For WinUI launch verification, use `BuildAndRun.ps1` from `winui-dev-workflow`; do not run the packaged `.exe` directly.

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
- `artifacts/reg/` for registry exports when Classic Shell Integration is used.
