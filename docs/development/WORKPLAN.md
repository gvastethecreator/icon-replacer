# Development Workplan

Status: proposed
Date: 2026-07-07

## Mission Control

Objective: develop Icon Replacer into a complete Windows app that can change and restore folder and `.lnk` shortcut icons from Explorer.

Current loop: IR-001 through IR-005 complete, IR-000 accepted as Modern Shell Integration, plus AppModel setup readiness, app locations, dashboard, WinUI-ready home, app-activation routing, activated post-picker change flow, icon-browser, icon-details, picker-request, launch-request, change-preview, shell-integration plan, recent-change action, diagnostics, and batch-import snapshots, shell-selection evaluation, post-picker change orchestration, Icon Library import/status, filtered restore history, shared apply/restore operations, and dynamic menu-snapshot foundations.

Highest-leverage next item: resolve the missing `winapp` prerequisite, then scaffold the packaged WinUI app and native `IExplorerCommand` path.

Expected proof: `shell-plan` reports Modern MSIX plus `IExplorerCommand`, then packaged WinUI/native extension build and launch proof once tooling is ready.

WinUI preflight: templates are installed, but `winapp` is missing. Run `/winui-setup` before scaffolding/running the WinUI app with `winui-dev-workflow`.

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
- Use AppModel shell-selection evaluation so `Change icon...` is enabled only for exactly one local folder or `.lnk`.
- Use AppModel launch requests for the Explorer-to-app handoff instead of embedding argument construction in the native extension.
- Use AppModel picker requests so `Change icon...` opens a single-select `.ico` picker rooted at the Icon Library.
- Use AppModel change preview after the user picks an icon and before applying it, so target/icon readiness can be shown without mutation.
- Use AppModel post-picker change orchestration after the user chooses an `.ico`.
- Use AppModel menu snapshots for bounded Icon Library categories and entries.

Done when right-click launches `Change icon...` for folder and `.lnk`.

### Slice 7: WinUI App

- Build compact management UI.
- Add import, open library, refresh, recent changes, restore, and diagnostics.
- Add accessibility basics.
- Use AppModel home snapshots as the first screen's state source.
- Use AppModel activation snapshots so normal startup and Explorer `change-icon` activation route through one tested entry point.
- Use AppModel activated change flow so the icon returned by the picker can be previewed and then applied without duplicating workflow logic.
- Use AppModel icon-browser snapshots for catalog search and category filters.
- Use AppModel icon-details snapshots for selected-icon preview/details.
- Use AppModel picker-request snapshots for the direct `.ico` picker launched from shell/app flows.
- Use AppModel launch-request parsing so packaged app activation arguments land in the same validated flow.
- Use AppModel change-preview snapshots for target/icon readiness before calling the mutation flow.
- Use AppModel setup readiness to drive first-run state and integration-status messaging.
- Use AppModel app-location targets for Open Library, Imported, AppData, and restore-state diagnostics.
- Use AppModel diagnostics snapshots for readiness, blockers, shell status, and WinUI tooling state.
- Use AppModel Icon Library import/status operations for first-run and import flows.
- Use AppModel batch import results for multi-file picker feedback.
- Use AppModel filtered restore-history snapshots so stale history cannot be presented as safely restorable.
- Use AppModel recent-change action snapshots to enable/disable restore buttons with reasons.
- Use AppModel apply orchestration so WinUI does not duplicate folder/shortcut mutation and restore-state persistence.
- Use AppModel restore orchestration so WinUI updates disk state and restore history through one product operation.
- Use AppModel menu snapshots to preview the same categories that shell integration will expose.

Done when manual first-run path is clear and recoverable.

### Slice 8: Packaging and Uninstall

- Package the accepted Modern integration.
- Preserve Icon Library on uninstall.
- Remove shell integration on uninstall.

Done when fresh install, upgrade/reinstall, and uninstall proof are captured.

### Slice 9: Final Quality Pass

- Run QA matrix.
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
