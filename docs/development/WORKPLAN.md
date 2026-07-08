# Development Workplan

Status: proposed
Date: 2026-07-07

## Mission Control

Objective: develop Icon Replacer into a complete Windows app that can change and restore folder and `.lnk` shortcut icons from Explorer.

Current loop: IR-001 through IR-005 complete, IR-000 accepted as Modern Shell Integration, plus AppModel setup readiness with package-plan actions, release readiness, app-action requests, operation feedback, app navigation plan, app window startup state, app command state, app command request routing, route view composition, filtered icon-browser route content, icon-details route content, import route content, collections route content, change-icon route content, change-icon workflow state, restore workflow state, restore route view/command composition, shell-bridge route content, app locations and open-location requests, dashboard, catalog-warning snapshots, accessibility acceptance plan, WinUI-ready home, app-activation routing, activated post-picker change flow, activated direct menu-apply flow, icon-browser, icon-details, import-picker, picker-request, launch-request, change-preview, restore-preview, shell-integration plan, shell manifest contract, shell extension bridge, packaging/install plan, recent-change action, diagnostics, and batch-import snapshots, shell-selection evaluation, post-picker change orchestration, Icon Library import/status, collection management and collection import, filtered restore history, shared apply/restore operations, dynamic menu-snapshot foundations, empty/truncated/unavailable menu states, shell-menu command descriptors/invocation previews, direct dynamic-menu icon application, a packaged WinUI app shell connected to AppModel snapshots, and a native x64 `IExplorerCommand` DLL wired into the packaged app manifest.

Highest-leverage next item: turn the native `IExplorerCommand` slice into installable package proof with signing/package identity, then add dynamic submenu enumeration and manual Explorer proof.

Expected proof: package identity, signing, install/uninstall proof, dynamic submenu proof, and manual Explorer context-menu proof before Explorer registration is considered complete.

WinUI/native preflight: .NET 10, WinUI templates, Developer Mode, `winapp` 0.4.0, CMake, and Visual Studio C++ Build Tools are available. Use `scripts\Initialize-NativeToolchain.ps1 -PassThru` when a native build shell needs `cl.exe` and MSBuild on PATH.

Accepted decision: Modern Shell Integration with MSIX plus native `IExplorerCommand` for V1. Classic HKCU remains a fallback/prototype only.

## Execution Gate

Implementation may start on Core Engine, CLI, and tests before the shell decision is resolved.

Do not register Explorer integration until the selected Modern package/native-extension path is built and verified.

## Slices

### Slice 1: Core Project Scaffold

- Create solution and projects.
- Add core domain types for Target, Icon Library, Icon Category, Icon Mutation, Restore Record.
- Add test project.
- Add CLI project.

Done. `dotnet build IconReplacer.slnx` and `dotnet test IconReplacer.slnx --no-build` pass.

### Slice 2: Icon Validation and Catalog

- Parse `.ico` headers and entries.
- Reject invalid, truncated, oversized, non-local, or renamed files.
- Scan `%USERPROFILE%\.icons` without following reparse points.
- Dedupe imported icons.

Done. Unit tests cover valid, invalid, duplicate, and category scenarios.

### Slice 3: Folder Target Engine

- Merge/create `desktop.ini`.
- Preserve unrelated keys.
- Set attributes.
- Create Restore Records.
- Restore previous folder state.

Done. Temp-folder integration tests apply and restore folder icons while preserving unrelated `desktop.ini` keys.

### Slice 4: Shortcut Target Engine

- Load `.lnk`.
- Read and store current icon location.
- Set icon location.
- Restore previous icon location.

Done. Temp `.lnk` tests apply and restore an icon without changing target metadata.

### Slice 5: CLI and Diagnostics

- Implement `catalog`, `apply-folder`, `apply-shortcut`, `restore`, `doctor`.
- Add stable exit codes and human-readable errors.
- Add `apply <target> <icon.ico>` autodetection for folder vs `.lnk`.

Done. CLI proof applies and restores temporary folder and `.lnk` targets.

### Slice 6: Shell Integration

- Create packaged WinUI app and native `IExplorerCommand` extension.
- Keep Classic HKCU registry verbs as fallback/prototype only, with explicit fallback status if used.
- Use AppModel shell-manifest contract for the MSIX COM/context-menu entries before packaging.
- Use AppModel shell-extension bridge snapshots so the future native `IExplorerCommand` can follow one tested manifest/menu/selection/argument contract.
- Use AppModel shell-selection evaluation so `Change icon...` is enabled only for exactly one local folder or `.lnk`.
- Use AppModel launch requests for the Explorer-to-app handoff instead of embedding argument construction in the native extension.
- Use AppModel picker requests so `Change icon...` opens a single-select `.ico` picker rooted at the Icon Library.
- Use AppModel change preview after the user picks an icon and before applying it, so target/icon readiness can be shown without mutation.
- Use AppModel post-picker change orchestration after the user chooses an `.ico`.
- Use AppModel menu snapshots for bounded Icon Library categories and entries.
- Use AppModel menu state/status for empty libraries, large catalogs, warnings, and scan failures.
- Use AppModel menu command descriptors for stable shell command ids and argument templates.
- Use AppModel menu invocation previews to resolve selected-target arguments before launching app/command paths.
- Use AppModel direct menu apply so generated submenu entries can invoke one shared, validated mutation path.
- Use AppModel `menu-apply` activation so the packaged app can consume direct submenu commands without duplicating shell logic.

Done when right-click launches `Change icon...` for folder and `.lnk`.

### Slice 7: WinUI App

- Build compact management UI.
- Add import, open library, refresh, recent changes, restore, and diagnostics.
- Add accessibility basics.
- Use AppModel change-icon workflow snapshots to drive picker, preview, and disabled target states.
- Use AppModel app-view snapshots to compose selected route, commands, and route content for rendering.
- Use AppModel app-command snapshots to drive buttons, enabled/disabled states, targets, and reasons.
- Use AppModel app-command request snapshots to route button clicks into navigation, open-location, refresh, workflow, or disabled states without duplicating UI logic.
- Use AppModel app-window snapshots to initialize selected route, activation state, and diagnostic badges.
- Use AppModel navigation-plan snapshots for top-level routes, workflow routes, and setup-action targets.
- Use AppModel accessibility-plan snapshots for keyboard reachability, accessible names, visual adaptation, and manual proof gates.
- Use AppModel home snapshots as the first screen's state source.
- Use AppModel activation snapshots so normal startup and Explorer `change-icon` activation route through one tested entry point.
- Use AppModel activated change flow so the icon returned by the picker can be previewed and then applied without duplicating workflow logic.
- Use AppModel activated menu-apply flow so direct submenu choices are validated and applied through the same product path.
- Use AppModel icon-browser snapshots for catalog search and category filters.
- Use AppModel filtered icon-browser route views so search/category/max UI state renders through the same route composition used by CLI proof.
- Use AppModel catalog-warning snapshots for invalid icon review/cleanup guidance.
- Use AppModel icon-details snapshots for selected-icon preview/details.
- Use AppModel icon-details route views to render selected-icon metadata and copy/open command readiness.
- Use AppModel picker-request snapshots for the direct `.ico` picker launched from shell/app flows.
- Use AppModel launch-request parsing so packaged app activation arguments land in the same validated flow.
- Use AppModel change-preview snapshots for target/icon readiness before calling the mutation flow.
- Use AppModel setup readiness to drive first-run state and integration-status messaging.
- Use AppModel package-plan setup actions so first-run setup can point to packaging blockers before shell integration is installed.
- Use AppModel app-action requests so setup/home buttons route through tested workflow intents.
- Use AppModel operation feedback so apply/restore/import results have consistent user-facing messages.
- Use AppModel app-location targets for Open Library, Imported, AppData, and restore-state diagnostics.
- Use AppModel app-location open requests so WinUI navigation buttons can validate known paths before launching them.
- Use AppModel diagnostics snapshots for readiness, blockers, shell status, and WinUI tooling state.
- Use AppModel import-picker requests for multi-select `.ico` import actions.
- Use AppModel import route views to render picker destination, multi-select constraints, and collection target before opening a native picker.
- Use AppModel Icon Library import/status operations for first-run and import flows.
- Use AppModel collection-management operations for creating and listing one-level Icon Library folders.
- Use AppModel collection route views to render current one-level collections, Imported, and per-collection icon counts.
- Use AppModel collection-import operations for filling user-created one-level collections from picker selections.
- Use AppModel batch import results for multi-file picker feedback.
- Use AppModel filtered restore-history snapshots so stale history cannot be presented as safely restorable.
- Use AppModel recent-change action snapshots to enable/disable restore buttons with reasons.
- Use AppModel restore-preview snapshots to confirm a restore before mutating disk or restore history.
- Use AppModel restore-workflow snapshots to compose history, selected-record preview, ready-to-restore, need-selection, and blocked states.
- Use AppModel restore route views and command snapshots so confirm/cancel/reason commands reflect selected restore workflow state.
- Use AppModel change-icon route views and command snapshots so choose/preview/apply commands reflect selected icon workflow state.
- Use AppModel shell-bridge route views so native Explorer bridge status, resolved command counts, and safety rules can be reviewed inside WinUI.
- Use AppModel apply orchestration so WinUI does not duplicate folder/shortcut mutation and restore-state persistence.
- Use AppModel restore orchestration so WinUI updates disk state and restore history through one product operation.
- Use AppModel menu snapshots to preview the same categories that shell integration will expose.

Done when manual first-run path is clear and recoverable.

### Slice 8: Packaging and Uninstall

- Package the accepted Modern integration.
- Verify native build tools before building `IconReplacer.ShellExtension.dll`.
- Preserve Icon Library on uninstall.
- Remove shell integration on uninstall.
- Use AppModel package-plan snapshots to keep install/uninstall proof gates explicit before packaging.

Done when fresh install, upgrade/reinstall, and uninstall proof are captured.

### Slice 9: Final Quality Pass

- Run QA matrix.
- Use AppModel release-readiness snapshots to keep automated, manual, package, accessibility, and release-evidence gates explicit.
- Capture Explorer screenshots.
- Fix high-impact edge cases.
- Reconcile docs and ADRs.

Done when main path plus meaningful recovery path are green.

## Stop Conditions

- Missing WinUI or packaging prerequisites.
- Need for admin/elevation in normal path.
- Shell extension cannot be verified without destabilizing Explorer.
- Restore proof fails for folder or `.lnk`.
- User requires unsupported V1 features such as PNG conversion, `.url`, or network folders.
