# Backlog

Status: proposed
Date: 2026-07-07

## Epics

### EPIC-CORE: Core Engine

Goal: make icon mutation and restore reliable without Explorer UI.

- IR-001: Bootstrap repo + core/CLI/test harness.
- IR-002: Implement `.ico` validation and Icon Library catalog.
- IR-003A: Define domain types and result/error model.
- IR-003B: Implement Restore Record persistence.

### EPIC-FOLDER: Folder Target Support

Goal: apply and restore folder icons safely.

- IR-003: Folder apply/restore engine.
- IR-003.1: Implement `desktop.ini` parser/merger.
- IR-003.2: Apply folder icon with attributes and Explorer refresh.
- IR-003.3: Restore previous folder icon state.
- IR-003.4: Test existing `desktop.ini` preservation.
- IR-003.5: Test protected and unsupported folder failures.

### EPIC-SHORTCUT: Shortcut Target Support

Goal: apply and restore `.lnk` icons without changing shortcut metadata.

- IR-004: Shortcut `.lnk` apply/restore engine.
- IR-004.1: Load and save `.lnk` through Shell link APIs.
- IR-004.2: Read previous icon path/index.
- IR-004.3: Set new icon path/index.
- IR-004.4: Restore previous icon path/index.
- IR-004.5: Test broken-target shortcut behavior.

### EPIC-CLI: CLI Proof Harness

Goal: make behavior testable before shell UI exists.

- IR-005: CLI proof path.
- IR-005.1: Add `catalog`.
- IR-005.2: Add `apply-folder`.
- IR-005.3: Add `apply-shortcut`.
- IR-005.4: Add `restore`.
- IR-005.5: Add `doctor`.

### EPIC-SHELL: Shell Integration

Goal: expose `Change icon` from File Explorer.

- IR-000: Confirm Modern vs Classic V1 integration decision.
- IR-006: Shell integration spike according to IR-000.
- IR-006.1: Add `Change icon...` path.
- Pre-IR-006.1: Shared post-picker change workflow before Explorer registration.
- Pre-IR-006.2: Shared change preview before mutation.
- Pre-IR-006.3: Shared `.ico` picker request before Explorer registration.
- Pre-IR-006.4: Shared Explorer-to-app launch request contract.
- IR-007: Catalog in context menu.
- IR-007.1: Add empty, large-catalog, and failure states.
- Pre-IR-006: Shell selection evaluation before Explorer registration.
- Pre-IR-007: Dynamic Icon Library menu snapshot before Explorer registration.

### EPIC-APP: WinUI Management App

Goal: provide setup, library management, recent changes, restore, and diagnostics.

- IR-008: WinUI utility app.
- IR-008.1: First-run setup and integration status.
- IR-008.2: Import icons and open Icon Library.
- IR-008.3: Catalog browser and refresh.
- IR-008.4: Recent changes and restore.
- IR-008.5: Diagnostics and error details.
- IR-008.6: Accessibility and high contrast pass.
- Pre-IR-008.1: Restore-history health model for future recent-changes UI.
- Pre-IR-008.2: Shared apply orchestration for CLI, WinUI, and shell entry points.
- Pre-IR-008.3: Shared restore orchestration for recent-changes UI and CLI.
- Pre-IR-008.4: Shared Icon Library import/status operation for WinUI and CLI.
- Pre-IR-008.5: First-run setup/readiness snapshot for WinUI and CLI.
- Pre-IR-008.6: Shared app-location targets for Open Library and diagnostics UI.
- Pre-IR-008.7: Filtered restore-history view model for recent-changes UI.
- Pre-IR-008.8: Shared WinUI home snapshot.
- Pre-IR-008.9: Shared WinUI icon-browser snapshot.
- Pre-IR-008.10: Shared WinUI recent-change action snapshot.
- Pre-IR-008.11: Shared WinUI diagnostics snapshot.
- Pre-IR-008.12: Shared WinUI batch-import result snapshot.
- Pre-IR-008.13: Shared WinUI icon-details snapshot.
- Pre-IR-008.14: Shared packaged-app activation snapshot.
- Pre-IR-008.15: Shared activated post-picker change flow.

### EPIC-PACKAGE: Packaging and Operations

Goal: install, update, and uninstall safely.

- IR-009: Packaging/install/uninstall.
- IR-009.1: Dev signing or local install path.
- IR-009.2: Register shell integration.
- IR-009.3: Unregister shell integration.
- IR-009.4: Preserve Icon Library on uninstall.
- IR-009.5: Capture install/uninstall proof.

### EPIC-QA: Quality and Proof

Goal: prevent happy-path-only completion.

- IR-010: Quality/adversarial proof.
- IR-010.1: Add core unit tests.
- IR-010.2: Add folder integration tests.
- IR-010.3: Add shortcut integration tests.
- IR-010.4: Run Explorer manual proof.
- IR-010.5: Capture negative-path evidence.
- IR-010.6: Reconcile docs and final status.

## First Task

IR-001 through IR-005 are complete. IR-000 is now accepted as Modern Shell Integration. Continue with Modern-path implementation prerequisites before IR-006 registration.

## Order

IR-001 -> IR-002 -> IR-003 and IR-004 -> IR-005 -> IR-000 decision gate -> IR-006 -> IR-007 -> IR-008 -> IR-009 -> IR-010.

IR-000 is complete. Final shell architecture is Modern MSIX plus native `IExplorerCommand`; Explorer registration remains blocked until the selected path is built and verified.

## Status

| ID | Status | Evidence |
| --- | --- | --- |
| IR-001 | done | Solution contains `IconReplacer.Core`, `IconReplacer.Cli`, and `IconReplacer.Core.Tests`; build/test run green. |
| IR-002 | done | `.ico` validation, catalog scan, import/dedupe, and 182-icon real catalog proof are green. |
| IR-003 | done | Folder apply/restore engine has tests for new and existing `desktop.ini`, attributes, restore, and missing-folder failure. |
| IR-004 | done | `.lnk` apply/restore tests prove icon path/index mutation and metadata preservation. |
| IR-005 | done | CLI `catalog`, `doctor`, `history`, `apply-folder`, `apply-shortcut`, and `restore` are implemented; temporary folder and `.lnk` proof passed. |
| Pre-IR-006 | done | `ShellSelectionService` validates single local folder/`.lnk` selections, blocks unsupported selections, and powers CLI `target`/`selection`. |
| Pre-IR-006.1 | done | `IconChangeService` combines shell-selection validation with apply orchestration; CLI `change` exercises the post-picker workflow. |
| Pre-IR-006.2 | done | `IconChangePreviewService` validates target and icon details before mutation; CLI `preview-change` previews the same UI-ready state. |
| Pre-IR-006.3 | done | `IconPickerRequestService` builds the future `.ico` file-picker request for supported shell targets; CLI `picker-request` previews it. |
| Pre-IR-006.4 | done | `AppLaunchRequestService` builds and parses stable `change-icon --target ... --target-kind ...` launch arguments for the future Explorer-to-app handoff; CLI `launch-request` previews it. |
| Pre-IR-008 | done | `IconReplacer.AppModel` dashboard snapshot exists for future WinUI app. |
| Pre-IR-008.1 | done | Restore summaries classify target/icon availability and can-restore status; `doctor` and `history` expose the health state. |
| Pre-IR-008.2 | done | `IconApplyService` applies folder or shortcut icons and persists restore records through one product operation; CLI `apply` uses it. |
| Pre-IR-008.3 | done | `IconRestoreService` restores by record id, updates restore state, rejects non-applied records, and powers CLI `restore`. |
| Pre-IR-008.4 | done | `IconLibraryService` imports icons into `.icons\Imported`, returns library status, and powers CLI `import`. |
| Pre-IR-008.5 | done | `SetupReadinessService` reports first-run readiness, setup actions, and shell integration readiness; CLI `status`/`setup` use it. |
| Pre-IR-008.6 | done | `AppLocationService` reports Icon Library, Imported, AppData, and restore-state locations; CLI `paths`/`locations` use it. |
| Pre-IR-008.7 | done | `RestoreHistoryService` provides filtered all/restorable/applied/restored/stale history snapshots; CLI `history` uses it. |
| Pre-IR-008.8 | done | `AppHomeService` composes setup, dashboard, menu, history, and locations for the future WinUI home screen; CLI `home`/`app` preview it. |
| Pre-IR-008.9 | done | `IconBrowserService` provides search, category filters, caps, omitted counts, and warnings for the future WinUI catalog browser; CLI `browse`/`icons` preview it. |
| Pre-IR-008.10 | done | `RecentChangesService` maps restore history into UI action rows with enabled/disabled/warning states; CLI `recent`/`changes` preview it. |
| Pre-IR-008.11 | done | `AppDiagnosticsService` maps readiness, locations, shell status, and WinUI tooling into diagnostics checks; CLI `diagnostics`/`diag` preview it. |
| Pre-IR-008.12 | done | `IconLibraryService.ImportIcons` maps multi-file picker selections into imported/reused/failed item results; CLI `batch-import`/`import-many` preview it. |
| Pre-IR-008.13 | done | `IconDetailsService` maps selected icons into detail/preview metadata including ICO image entries; CLI `details`/`icon-details` preview it. |
| Pre-IR-008.14 | done | `AppActivationService` routes normal startup to Home and `change-icon` activation arguments to the validated launch/picker flow; CLI `activate` previews it. |
| Pre-IR-008.15 | done | `ActivatedIconChangeService` combines activation arguments and the selected picker icon into preview/apply operations; CLI `activate-preview` previews it. |
| Pre-IR-007 | done | `IconMenuService` builds bounded `Change icon...` menu snapshots from `.icons`; CLI `menu` previews the real catalog as 7 categories and 182 icons. |
| IR-000 | done | ADR-0002 is accepted: V1 uses Modern MSIX plus native `IExplorerCommand`; Classic HKCU remains fallback/prototype only. CLI `shell-plan` reports the selected path and current prerequisites. |
