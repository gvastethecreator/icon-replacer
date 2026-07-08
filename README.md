# Icon Replacer

Icon Replacer is a Windows utility for changing folder and shortcut icons from File Explorer. The target user flow is: right-click a folder or `.lnk`, choose `Change icon`, select an `.ico`, and get a reversible icon change.

Status: Core/CLI/AppModel foundations are implemented, and the first packaged WinUI app shell now builds and launches. The V1 shell integration decision is accepted as Modern MSIX plus `IExplorerCommand`, but Explorer registration is not configured yet.

## Product Direction

The app should feel like an Explorer feature. The WinUI app exists for setup, icon library management, recent changes, restore, and diagnostics.

Implementation direction:

1. Build and prove the core icon engine first.
2. Use a packaged WinUI 3 app plus a native `IExplorerCommand` shell extension for the real Windows 11 context menu path.
3. Keep a simpler `HKCU` classic-menu integration only as a prototype or fallback.

## Current Shell Path

V1 shell integration is Modern Shell Integration: MSIX package identity plus a native `IExplorerCommand` extension.

Current readiness: `NotConfigured`. WinUI tooling and native build tools are now available; the selected path still needs package identity, a native shell extension, development signing, an install package, Explorer registration, and install/uninstall proof.

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
- `dotnet test IconReplacer.slnx`: 235 tests pass.
- `.\BuildAndRun.ps1 src\IconReplacer.App\IconReplacer.App.csproj -Detach`: builds and launches the packaged WinUI app through `winapp`; latest proof returned AUMID `IconReplacer_2wx6x5nenbha0!IconReplacer.App`.
- `dotnet run --no-build --project src\IconReplacer.Cli -- diagnostics`: previews the future WinUI diagnostics view; current proof has 0 blockers and 1 shell-integration warning.
- `dotnet run --no-build --project src\IconReplacer.Cli -- shell-plan`: reports the accepted Modern shell integration plan and current prerequisites.
- `dotnet run --no-build --project src\IconReplacer.Cli -- shell-manifest`: previews the future MSIX COM/context-menu manifest contract without registering Explorer.
- `dotnet run --no-build --project src\IconReplacer.Cli -- shell-bridge [target]`: previews the native `IExplorerCommand` bridge contract, resolved command arguments, disabled reasons, manifest identity, and safety rules without registering Explorer.
- `dotnet run --no-build --project src\IconReplacer.Cli -- package-plan`: previews install/uninstall gates, native build-tooling, signing/package needs, and user-data preservation policy without installing anything.
- `dotnet run --no-build --project src\IconReplacer.Cli -- release-readiness [proof flags]`: previews the release evidence gates, package blockers, diagnostics, accessibility proof, and manual Explorer proof status.
- `dotnet run --no-build --project src\IconReplacer.Cli -- accessibility-plan`: previews keyboard, semantics, visual adaptation, manual proof items, and WinUI surfaces for accessibility acceptance.
- `dotnet run --no-build --project src\IconReplacer.Cli -- navigation-plan`: previews future WinUI routes, top-level screens, workflow surfaces, and setup-action route targets.
- `dotnet run --no-build --project src\IconReplacer.Cli -- app-window [activation args]`: previews the future main-window startup state, selected route, activation result, and diagnostics badges.
- `dotnet run --no-build --project src\IconReplacer.Cli -- app-commands [--route <route-id>] [--record <record-id>] [--filter <filter>] [--icon <icon.ico>] [--collection <name>] [activation args]`: previews future WinUI route commands, enabled/disabled state, targets, and reasons.
- `dotnet run --no-build --project src\IconReplacer.Cli -- app-command-request <command-id> [--route <route-id>] [activation args]`: previews the future WinUI button intent without executing navigation, opening Explorer, or mutating state.
- `dotnet run --no-build --project src\IconReplacer.Cli -- app-view [--route <route-id>] [--record <record-id>] [--filter <filter>] [--icon <icon.ico>] [--collection <name>] [--shell-target <target>] [--search <text>] [--category <name>] [--max <count>] [activation args]`: previews future WinUI route content, commands, selected window route, and readiness.
- `dotnet run --no-build --project src\IconReplacer.Cli -- change-icon-workflow [--icon <icon.ico>] change-icon --target <path> --target-kind <folder|shortcut>`: previews the Explorer-launched Change Icon workflow before mutation.
- `dotnet run --no-build --project src\IconReplacer.Cli -- restore-workflow [record-id] [filter]`: previews the WinUI restore route state, history, selected-record preview, and blocked/ready reasons before mutation.
- `dotnet run --no-build --project src\IconReplacer.Cli -- action-request <action-id>`: resolves a setup/home action into a future WinUI navigation or workflow intent, including `review-package-plan` when packaging blockers are visible.
- `dotnet run --no-build --project src\IconReplacer.Cli -- home [filter]`: previews the future WinUI home state with readiness, package/setup actions, paths, menu counts, and restore-history counts.
- `dotnet run --no-build --project src\IconReplacer.Cli -- activate [change-icon --target <path> --target-kind <folder|shortcut>|menu-apply <target> <icon-from-library.ico>]`: previews packaged app activation routing.
- `dotnet run --no-build --project src\IconReplacer.Cli -- activate-preview <icon.ico> change-icon --target <path> --target-kind <folder|shortcut>`: previews the activated post-picker change flow.
- `dotnet run --no-build --project src\IconReplacer.Cli -- activate-apply <icon.ico> change-icon --target <path> --target-kind <folder|shortcut>`: applies the activated post-picker change flow through the shared AppModel path.
- `dotnet run --no-build --project src\IconReplacer.Cli -- activate-menu-apply <target> <icon-from-library.ico>`: applies a direct submenu activation through the packaged-app argument contract.
- `dotnet run --no-build --project src\IconReplacer.Cli -- browse [search] [--category <name>] [--max <count>]`: previews the future WinUI icon browser with search, category filters, and capped results.
- `dotnet run --no-build --project src\IconReplacer.Cli -- details <icon.ico>`: previews the future WinUI icon details panel.
- `dotnet run --no-build --project src\IconReplacer.Cli -- target <folder-or-shortcut>`: reports whether Explorer should show `Change icon...` for a selected target.
- `dotnet run --no-build --project src\IconReplacer.Cli -- import-picker-request [collection]`: previews the future WinUI multi-select `.ico` import picker.
- `dotnet run --no-build --project src\IconReplacer.Cli -- preview-change <target> <icon.ico>`: previews the selected target and icon before any mutation.
- `dotnet run --no-build --project src\IconReplacer.Cli -- change <target> <icon.ico>`: runs the shared post-picker change workflow with shell-selection validation.
- `dotnet run --no-build --project src\IconReplacer.Cli -- catalog`: sees 19 categories and 542 icons in `C:\Users\cristian\.icons`.
- `dotnet run --no-build --project src\IconReplacer.Cli -- collections`: lists Icon Library collections and per-collection icon counts.
- `dotnet run --no-build --project src\IconReplacer.Cli -- collection-create <name>`: creates a sanitized one-level Icon Library collection for future WinUI library management.
- `dotnet run --no-build --project src\IconReplacer.Cli -- collection-import <collection> <icon.ico> [icon2.ico ...]`: imports valid icons into a one-level collection with per-file results and content dedupe.
- `dotnet run --no-build --project src\IconReplacer.Cli -- catalog-warnings`: previews the future WinUI catalog-warning review list.
- `dotnet run --no-build --project src\IconReplacer.Cli -- menu`: previews `Change icon...` plus 19 dynamic categories and 542 visible icons from `.icons`.
- `dotnet run --no-build --project src\IconReplacer.Cli -- menu-commands`: previews stable command IDs and argument templates for the future shell extension.
- `dotnet run --no-build --project src\IconReplacer.Cli -- menu-invoke-preview <command-id> [target]`: resolves a shell menu command against a selected target without mutating anything.
- `dotnet run --no-build --project src\IconReplacer.Cli -- menu-apply <target> <icon-from-library.ico>`: applies a selected dynamic-menu icon after confirming it belongs to the Icon Library.
- `dotnet run --no-build --project src\IconReplacer.Cli -- import <icon.ico> [display-name]`: imports to `.icons\Imported` with content-hash dedupe.
- `dotnet run --no-build --project src\IconReplacer.Cli -- batch-import <icon.ico> [icon2.ico ...]`: previews multi-file import results for the future WinUI file picker.
- `dotnet run --no-build --project src\IconReplacer.Cli -- status`: reports first-run readiness, core feature status, package/setup actions, and shell integration configuration state.
- `dotnet run --no-build --project src\IconReplacer.Cli -- paths`: reports Icon Library, Imported, AppData, and restore-state locations for app navigation.
- `dotnet run --no-build --project src\IconReplacer.Cli -- open-request <icon-library|imported|appdata|restore-state>`: previews a safe app-location open request for future WinUI navigation.
- `dotnet run --no-build --project src\IconReplacer.Cli -- picker-request <folder-or-shortcut>`: previews the future `.ico` file-picker request for a supported target.
- `dotnet run --no-build --project src\IconReplacer.Cli -- launch-request <folder-or-shortcut>`: previews the future Explorer-to-app launch arguments for `Change icon...`.
- `dotnet run --no-build --project src\IconReplacer.Cli -- doctor`: reports catalog, restore-state, and missing-target counts.
- `dotnet run --no-build --project src\IconReplacer.Cli -- recent [all|restorable|applied|restored|stale]`: previews future WinUI recent-change rows with restore button state and reasons.
- `dotnet run --no-build --project src\IconReplacer.Cli -- history [all|restorable|applied|restored|stale]`: lists persisted restore records with target/icon health and UI-ready filters.
- `dotnet run --no-build --project src\IconReplacer.Cli -- restore-preview <record-id>`: previews a restore confirmation state before mutating the target or restore history.
- CLI `apply <target> <icon.ico>` autodetects folder vs `.lnk`, applies the icon, and stores restore history through the AppModel service.
- CLI `apply-folder`/`restore` and `apply-shortcut`/`restore` were proven on temporary targets.
- `IconReplacer.AppModel` provides setup readiness with package-plan actions, release readiness, app-action requests, operation feedback, app navigation plan, app window startup state, app command state, app command request routing, route view composition, filtered icon-browser route content, icon-details route content, import route content, collections route content, change-icon route content, shell-bridge route content, change-icon workflow state, restore workflow state, restore route view/command composition, app locations, dashboard snapshots, catalog-warning snapshots, accessibility acceptance plan, WinUI-ready home, app-activation routing, activated post-picker change flow, activated direct menu-apply flow, icon-browser, icon-details, import-picker, picker-request, launch-request, change-preview, restore-preview, shell-integration plan, shell manifest contract, shell extension bridge, package plan, recent-change, diagnostics, and batch-import snapshots, shell-selection evaluation, post-picker change orchestration, direct dynamic-menu icon application, shell-menu command descriptors and invocation previews, collection management, import/library management, filtered restore history, reusable apply/restore orchestration, and bounded dynamic menu snapshots with empty/truncated/unavailable states for future WinUI and shell surfaces.

## Development Rule

Do not register Explorer integration until the selected Modern path has package identity, native extension, signing, install/uninstall proof, and manual Explorer proof. The core engine, CLI, AppModel, WinUI app, native extension, and tests may continue because they feed the selected path.
