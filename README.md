# Icon Replacer

Icon Replacer is a Windows utility for changing folder and shortcut icons from File Explorer. The target user flow is: right-click a folder or `.lnk`, choose `Change icon`, select an `.ico`, and get a reversible icon change.

Status: Core/CLI/AppModel foundations are implemented and verified through the pre-shell slices. The V1 shell integration decision is accepted as Modern MSIX plus `IExplorerCommand`, but Explorer registration is not configured yet.

## Product Direction

The app should feel like an Explorer feature. The WinUI app exists for setup, icon library management, recent changes, restore, and diagnostics.

Implementation direction:

1. Build and prove the core icon engine first.
2. Use a packaged WinUI 3 app plus a native `IExplorerCommand` shell extension for the real Windows 11 context menu path.
3. Keep a simpler `HKCU` classic-menu integration only as a prototype or fallback.

## Current Shell Path

V1 shell integration is Modern Shell Integration: MSIX package identity plus a native `IExplorerCommand` extension.

Current readiness: `NotConfigured`. The selected path still needs `winapp`, package identity, a native shell extension, Explorer registration, and install/uninstall proof.

Fallback: per-user `HKCU` context-menu verbs remain a prototype/recovery path only.

## Documentation

- [Docs Index](docs/INDEX.md)
- [Project Context](CONTEXT.md)
- [Product Requirements](docs/product/PRD.md)
- [Architecture](docs/architecture/ARCHITECTURE.md)
- [Development Workplan](docs/development/WORKPLAN.md)
- [Backlog](docs/tasks/BACKLOG.md)
- [QA Test Plan](docs/qa/TEST-PLAN.md)
- [Installation and Packaging](docs/operations/INSTALLATION.md)
- [Risks](docs/RISKS.md)
- [ADRs](docs/adr/)

## Current Implementation Proof

- `dotnet build IconReplacer.slnx`: passes.
- `dotnet test IconReplacer.slnx --no-build`: 114 tests pass.
- `dotnet run --no-build --project src\IconReplacer.Cli -- diagnostics`: previews the future WinUI diagnostics view and reports current blockers.
- `dotnet run --no-build --project src\IconReplacer.Cli -- shell-plan`: reports the accepted Modern shell integration plan and current prerequisites.
- `dotnet run --no-build --project src\IconReplacer.Cli -- home [filter]`: previews the future WinUI home state with readiness, actions, paths, menu counts, and restore-history counts.
- `dotnet run --no-build --project src\IconReplacer.Cli -- activate [change-icon --target <path> --target-kind <folder|shortcut>]`: previews packaged app activation routing.
- `dotnet run --no-build --project src\IconReplacer.Cli -- activate-preview <icon.ico> change-icon --target <path> --target-kind <folder|shortcut>`: previews the activated post-picker change flow.
- `dotnet run --no-build --project src\IconReplacer.Cli -- activate-apply <icon.ico> change-icon --target <path> --target-kind <folder|shortcut>`: applies the activated post-picker change flow through the shared AppModel path.
- `dotnet run --no-build --project src\IconReplacer.Cli -- browse [search] [--category <name>] [--max <count>]`: previews the future WinUI icon browser with search, category filters, and capped results.
- `dotnet run --no-build --project src\IconReplacer.Cli -- details <icon.ico>`: previews the future WinUI icon details panel.
- `dotnet run --no-build --project src\IconReplacer.Cli -- target <folder-or-shortcut>`: reports whether Explorer should show `Change icon...` for a selected target.
- `dotnet run --no-build --project src\IconReplacer.Cli -- preview-change <target> <icon.ico>`: previews the selected target and icon before any mutation.
- `dotnet run --no-build --project src\IconReplacer.Cli -- change <target> <icon.ico>`: runs the shared post-picker change workflow with shell-selection validation.
- `dotnet run --no-build --project src\IconReplacer.Cli -- catalog`: sees 7 categories and 182 icons in `C:\Users\cristian\.icons`.
- `dotnet run --no-build --project src\IconReplacer.Cli -- menu`: previews `Change icon...` plus 7 dynamic categories and 182 visible icons from `.icons`.
- `dotnet run --no-build --project src\IconReplacer.Cli -- import <icon.ico> [display-name]`: imports to `.icons\Imported` with content-hash dedupe.
- `dotnet run --no-build --project src\IconReplacer.Cli -- batch-import <icon.ico> [icon2.ico ...]`: previews multi-file import results for the future WinUI file picker.
- `dotnet run --no-build --project src\IconReplacer.Cli -- status`: reports first-run readiness, core feature status, and shell integration configuration state.
- `dotnet run --no-build --project src\IconReplacer.Cli -- paths`: reports Icon Library, Imported, AppData, and restore-state locations for app navigation.
- `dotnet run --no-build --project src\IconReplacer.Cli -- picker-request <folder-or-shortcut>`: previews the future `.ico` file-picker request for a supported target.
- `dotnet run --no-build --project src\IconReplacer.Cli -- launch-request <folder-or-shortcut>`: previews the future Explorer-to-app launch arguments for `Change icon...`.
- `dotnet run --no-build --project src\IconReplacer.Cli -- doctor`: reports catalog, restore-state, and missing-target counts.
- `dotnet run --no-build --project src\IconReplacer.Cli -- recent [all|restorable|applied|restored|stale]`: previews future WinUI recent-change rows with restore button state and reasons.
- `dotnet run --no-build --project src\IconReplacer.Cli -- history [all|restorable|applied|restored|stale]`: lists persisted restore records with target/icon health and UI-ready filters.
- CLI `apply <target> <icon.ico>` autodetects folder vs `.lnk`, applies the icon, and stores restore history through the AppModel service.
- CLI `apply-folder`/`restore` and `apply-shortcut`/`restore` were proven on temporary targets.
- `IconReplacer.AppModel` provides setup readiness, app locations, dashboard snapshots, WinUI-ready home, app-activation routing, activated post-picker change flow, icon-browser, icon-details, picker-request, launch-request, change-preview, shell-integration plan, recent-change, diagnostics, and batch-import snapshots, shell-selection evaluation, post-picker change orchestration, import/library management, filtered restore history, reusable apply/restore orchestration, and bounded dynamic menu snapshots for future WinUI and shell surfaces.

## Development Rule

Do not register Explorer integration until the selected Modern path has package identity, native extension, install/uninstall proof, and the missing WinUI tooling prerequisite is resolved. The core engine, CLI, AppModel, and tests may continue because they feed the selected path.
