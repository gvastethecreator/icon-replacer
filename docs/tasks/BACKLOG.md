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
- IR-003.6: Support local directory junctions/symbolic links and reject remote link targets.

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

- IR-000: Confirm packaged modern/classic V1 integration decision.
- IR-006: Shell integration spike according to IR-000.
- IR-006.1: Add `Change icon...` path.
- Pre-IR-006.1: Shared post-picker change workflow before Explorer registration.
- Pre-IR-006.2: Shared change preview before mutation.
- Pre-IR-006.3: Shared `.ico` picker request before Explorer registration.
- Pre-IR-006.4: Shared Explorer-to-app launch request contract.
- Pre-IR-006.5: Shared Modern MSIX shell manifest contract.
- Pre-IR-006.6: Shared native shell extension bridge contract.
- IR-007: Catalog in context menu.
- IR-007.1: Add empty, large-catalog, and failure states.
- Pre-IR-006: Shell selection evaluation before Explorer registration.
- Pre-IR-007: Dynamic Icon Library menu snapshot before Explorer registration.
- Pre-IR-007.2: Shell-facing menu command descriptors before Explorer registration.
- Pre-IR-007.3: Shell-facing menu command invocation preview before Explorer registration.
- Pre-IR-007.1: Direct dynamic-menu icon apply before Explorer registration.
- Pre-IR-007.4: Packaged direct menu-apply activation before Explorer registration.

### EPIC-APP: WinUI Management App

Goal: provide setup, library management, recent changes, restore, and diagnostics.

- IR-008: WinUI utility app.
- IR-008.0: Packaged WinUI app shell.
- IR-008.1: First-run setup and integration status.
- IR-008.2: Import icons and open Icon Library.
- IR-008.3: Catalog browser and refresh.
- IR-008.4: Recent changes and restore.
- IR-008.5: Diagnostics and error details.
- IR-008.6: Accessibility and high contrast pass.
- IR-008.7: About, project credits, GitHub links, and update detection.
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
- Pre-IR-008.16: Shared Icon Library collection management.
- Pre-IR-008.17: Shared app-location open requests.
- Pre-IR-008.18: Shared Icon Library collection import.
- Pre-IR-008.19: Shared restore preview before mutation.
- Pre-IR-008.20: Shared setup/home app-action routing.
- Pre-IR-008.21: Shared operation feedback formatting.
- Pre-IR-008.22: Shared app import-picker request.
- Pre-IR-008.23: Shared catalog-warning review snapshot.
- Pre-IR-008.24: Shared accessibility acceptance plan.
- Pre-IR-008.25: Shared WinUI navigation plan.
- Pre-IR-008.26: Shared WinUI window startup state.
- Pre-IR-008.27: Shared WinUI command state.
- Pre-IR-008.28: Shared WinUI route view composition.
- Pre-IR-008.29: Shared Change Icon workflow state.
- Pre-IR-008.30: Shared Restore workflow state.
- Pre-IR-008.31: Shared Restore route view and command composition.
- Pre-IR-008.32: Shared Collections route view composition.
- Pre-IR-008.33: Shared Change Icon route view and command composition.
- Pre-IR-008.34: Shared Import Icons route view composition.
- Pre-IR-008.35: Shared Icon Details route view and command composition.
- Pre-IR-008.36: Shared filtered Icon Browser route view composition.
- Pre-IR-008.37: Shared package-plan setup action composition.
- Pre-IR-008.38: Shared shell-bridge route view composition.
- Pre-IR-008.39: Shared package-plan route native-tooling composition.
- Pre-IR-008.40: Shared native build-tooling diagnostics.
- Pre-IR-008.41: Shared WinUI command request routing.

### EPIC-PACKAGE: Packaging and Operations

Goal: install, update, and uninstall safely.

- IR-009: Packaging/install/uninstall.
- Pre-IR-009: Shared packaging/install readiness plan.
- Pre-IR-009.1: Shared native build-tooling packaging gate.
- IR-009.1: Dev signing or local install path.
- IR-009.2: Register shell integration.
- IR-009.3: Unregister shell integration.
- IR-009.4: Preserve Icon Library on uninstall.
- IR-009.5: Capture install/uninstall proof.

### EPIC-QA: Quality and Proof

Goal: prevent happy-path-only completion.

- IR-010: Quality/adversarial proof.
- Pre-IR-010: Shared release readiness/evidence gate.
- IR-010.1: Add core unit tests.
- IR-010.2: Add folder integration tests.
- IR-010.3: Add shortcut integration tests.
- IR-010.4: Run Explorer manual proof.
- IR-010.5: Capture negative-path evidence.
- IR-010.6: Reconcile docs and final status.

## First Task

IR-001 through IR-005 are complete. IR-000 is accepted as packaged dual
Explorer integration. Continue the explicitly approved manual Explorer matrix
before closing IR-006.1 and IR-010.4.

## Order

IR-001 -> IR-002 -> IR-003 and IR-004 -> IR-005 -> IR-000 decision gate -> IR-006 -> IR-007 -> IR-008 -> IR-009 -> IR-010.

IR-000 is complete. Final shell architecture is one MSIX with native modern and
classic handlers. Automated lifecycle proof leaves Explorer registration
removed; visual registration remains approval-gated.

## Status

| ID | Status | Evidence |
| --- | --- | --- |
| IR-001 | done | Solution contains `IconReplacer.Core`, `IconReplacer.Cli`, and `IconReplacer.Core.Tests`; build/test run green. |
| IR-002 | done | `.ico` validation, catalog scan, import/dedupe, and the current 185-icon real catalog proof are green. |
| IR-003B | done | Restore history uses per-file interprocess serialization plus flushed atomic replacement; app/CommandHost mutations serialize per Target, apply/restore persistence failures compensate folder/shortcut state, and concurrency/failure tests preserve a fully restorable chain. |
| IR-003 | done | Folder apply/restore covers new/existing `desktop.ini`, attributes, missing targets, local junctions/symbolic links, link-preserving restore, and remote-link rejection. |
| IR-004 | done | `.lnk` apply/restore tests prove icon path/index mutation and metadata preservation. |
| IR-005 | done | CLI `catalog`, `doctor`, `history`, `apply-folder`, `apply-shortcut`, and `restore` are implemented; temporary folder and `.lnk` proof passed. |
| Pre-IR-006 | done | `ShellSelectionService` validates single local folder/`.lnk` selections, blocks unsupported selections, and powers CLI `target`/`selection`. |
| Pre-IR-006.1 | done | `IconChangeService` combines shell-selection validation with apply orchestration; CLI `change` exercises the post-picker workflow. |
| Pre-IR-006.2 | done | `IconChangePreviewService` validates target and icon details before mutation; CLI `preview-change` previews the same UI-ready state. |
| Pre-IR-006.3 | done | `IconPickerRequestService` builds the future `.ico` file-picker request for supported shell targets; CLI `picker-request` previews it. |
| Pre-IR-006.4 | done | `AppLaunchRequestService` builds and parses stable `change-icon --target ... --target-kind ...` launch arguments for the future Explorer-to-app handoff; CLI `launch-request` previews it. |
| Pre-IR-006.5 | done | `ShellManifestContractService` defines the future MSIX COM/context-menu manifest contract; CLI `shell-manifest` previews the XML fragment without registering Explorer. |
| Pre-IR-006.6 | done | `ShellExtensionBridgeService` composes manifest identity, menu command descriptors, shell target evaluation, resolved arguments, and Explorer safety rules for the future native `IExplorerCommand`; CLI `shell-bridge [target]` previews it without mutation or registration. |
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
| Pre-IR-008.15 | done | `ActivatedIconChangeService` combines activation arguments and the selected picker icon into preview/apply operations; CLI `activate-preview` and `activate-apply` preview/prove it. |
| Pre-IR-008.16 | done | `IconCollectionService` lists current one-level collections and creates sanitized collections for future WinUI library management; CLI `collections` proves the real 19-collection library. |
| Pre-IR-008.17 | done | `AppLocationService` creates safe open requests for known app locations; CLI `open-request` previews Icon Library, Imported, AppData, and Restore State navigation. |
| Pre-IR-008.18 | done | `IconCollectionImportService` imports valid icons into one-level collections with per-file results and collection-local dedupe; unit proof covers import, duplicate reuse, invalid files, and null inputs. |
| Pre-IR-008.19 | done | `IconRestorePreviewService` previews one restore record before mutation, including previous-state detail, warnings, and disabled reasons; CLI `restore-preview` exposes the same contract. |
| Pre-IR-008.20 | done | `AppActionRequestService` resolves setup/home action ids into stable WinUI workflow/navigation intents; CLI `action-request` previews enabled and disabled action requests. |
| Pre-IR-008.21 | done | `AppOperationFeedbackService` turns apply, restore, import, batch import, and errors into shared WinUI/CLI feedback messages. |
| Pre-IR-008.22 | done | `IconImportPickerRequestService` builds multi-select `.ico` import picker requests for Imported or selected collections; CLI `import-picker-request` previews the contract. |
| Pre-IR-008.23 | done | `CatalogWarningsService` exposes invalid/skipped icon rows for future WinUI diagnostics; CLI `catalog-warnings` previews the warning list. |
| Pre-IR-008.24 | done | `AccessibilityPlanService` exposes keyboard, names/semantics, visual adaptation, manual proof items, and WinUI surfaces; CLI `accessibility-plan` previews the acceptance contract. |
| Pre-IR-008.25 | done | `AppNavigationService` exposes top-level routes, workflow routes, primary commands, and setup-action targets; CLI `navigation-plan` previews the route contract. |
| Pre-IR-008.26 | done | `AppWindowService` composes navigation, activation, and diagnostics into startup state; CLI `app-window` previews the future main-window route and badges. |
| Pre-IR-008.27 | done | `AppCommandService` exposes per-route command ids, targets, enabled states, and disabled reasons; CLI `app-commands` previews future WinUI command bars. |
| Pre-IR-008.28 | done | `AppRouteViewService` composes window state, route metadata, commands, and route content; CLI `app-view` previews future renderable route snapshots. |
| Pre-IR-008.29 | done | `AppChangeIconWorkflowService` exposes picker, preview, apply, and blocked states for Explorer-launched Change Icon; CLI `change-icon-workflow` previews the non-mutating workflow. |
| Pre-IR-008.30 | done | `AppRestoreWorkflowService` exposes history, selected-record preview, ready-to-restore, need-selection, and blocked states; CLI `restore-workflow` previews the non-mutating workflow. |
| Pre-IR-008.31 | done | `AppRouteViewService` and `AppCommandService` compose the restore workflow into `restore-preview` route content and commands; CLI `app-view`/`app-commands` preview no-selection and selected-record states. |
| Pre-IR-008.32 | done | `AppRouteViewService` composes one-level Icon Library collections into the `collections` route; the current curated proof catalog has 7 folders and 185 icons. |
| Pre-IR-008.33 | done | `AppRouteViewService` and `AppCommandService` compose Explorer-launched `change-icon` workflow content and choose/preview/apply commands; CLI `app-view --icon ... change-icon ...` previews `NeedIcon` and `ReadyToApply` without mutation. |
| Pre-IR-008.34 | done | `AppRouteViewService` composes `import-icons` picker destination content; CLI `app-view --route import-icons [--collection <name>]` previews Imported or collection targets without copying icons. |
| Pre-IR-008.35 | done | `AppRouteViewService` and `AppCommandService` compose `icon-details` metadata and selected-icon commands; CLI `app-view --route icon-details --icon <icon.ico>` previews details without mutation. |
| Pre-IR-008.36 | done | `AppRouteViewService` passes search/category/max browser state into the `icon-browser` route; CLI `app-view --route icon-browser --search <text> --category <name> --max <count>` previews filtered route content. |
| Pre-IR-008.37 | done | `SetupReadinessService`, `AppHomeService`, and `AppActionRequestService` surface package-plan blockers as a setup action; CLI `status`, `home`, and `action-request review-package-plan` preview the path without installing anything. |
| Pre-IR-008.38 | done | `AppNavigationService`, `AppCommandService`, and `AppRouteViewService` expose a `shell-bridge` route so future WinUI can review the native Explorer bridge contract, resolved commands, target status, and safety rules; CLI `app-view --route shell-bridge --shell-target <target>` previews it without mutation or registration. |
| Pre-IR-008.39 | done | `AppRouteViewService` accepts full `PackagingPlanInputs` for the `package-plan` route so future WinUI can surface package and native build-tooling gates. |
| Pre-IR-008.40 | done | `AppDiagnosticsService` and `AppDiagnosticsSnapshot` expose native build-tooling checks alongside WinUI tooling; CLI `diagnostics` and `app-view --route diagnostics` report available or missing native tools without attempting installation. |
| Pre-IR-008.41 | done | `AppCommandRequestService` resolves route command ids into non-mutating WinUI intents for navigation, location opening, refresh, workflow, and disabled reasons; CLI `app-command-request <command-id>` previews button behavior without launching or mutating. |
| IR-008.0 | done | `IconReplacer.App` is a packaged WinUI app shell connected to AppModel snapshots; `BuildAndRun.ps1 src\IconReplacer.App\IconReplacer.App.csproj -Detach` launched it through `winapp` with AUMID `IconReplacer_2wx6x5nenbha0!IconReplacer.App`. |
| IR-008.1 | done | First-run setup and integration status are visible through Home, Diagnostics, Package, and inline `InfoBar` state; WinUI setup was revalidated with `.NET 10.0.300`, refreshed templates, Developer Mode enabled, `winapp 0.4.0`, and native toolchain available. |
| IR-008.2 | done | WinUI `Import` opens a native Windows `.ico` file dialog through the app HWND and imports multi-select results into `.icons\Imported`; UIA proof finds `ImportIconsButton`, and final manual selection proof remains part of Windows interaction evidence. |
| IR-008.3 | done | WinUI catalog browser and Refresh are backed by `AppRouteViewService` snapshots; the current curated library has 185 icons. |
| IR-008.4 | done | WinUI Home/History rows expose `Restore` actions; non-restorable records are disabled with status text, and restorable records call `IconRestoreService.Restore` then refresh History. |
| IR-008.7 | done | About is implemented with rounded high-resolution theme identity, installed version, project credits, GitHub/release links, and an 8-second GitHub Releases check. Unit/source/build and installed runtime UIA proof pass. |
| IR-006.1 | in-progress | Native modern/classic Explorer integration and zero-window command host are implemented. Installed `1.0.0.5` proves one grouped command pair, direct picker launch, preserved unrelated entries, and callback-rendered collection/icon previews in native smoke; the final Explorer submenu screenshot and remaining target matrix stay open. |
| Pre-IR-007 | done | `IconMenuService` builds bounded `Change icon...` menu snapshots from `.icons`; CLI `menu` previews the current real catalog as 7 categories and 185 icons. |
| IR-007.1 | done | `IconMenuState` and command fallbacks cover empty, truncated, warning, and unavailable catalog states without dropping `Change icon...`. |
| Pre-IR-007.1 | done | `IconMenuApplyService` applies only current Icon Library catalog icons from the dynamic menu path; CLI `menu-apply` changed and restored a temporary folder target. |
| Pre-IR-007.2 | done | `IconMenuCommandService` turns menu snapshots into stable shell-facing command descriptors; CLI `menu-commands` previews command ids and argument templates. |
| Pre-IR-007.3 | done | `IconMenuCommandInvocationService` resolves menu command ids against selected targets without mutation; CLI `menu-invoke-preview` previews final arguments and disabled reasons. |
| Pre-IR-007.4 | done | `AppMenuApplyActivationService` and `ActivatedMenuApplyService` let the packaged app preview and apply `menu-apply <target> <icon>` activations through the same validated submenu path; CLI `activate menu-apply ...` and `activate-menu-apply` prove it before Explorer registration. |
| IR-000 | done | ADR-0011 supersedes ADR-0002: V1 packages native modern and classic Explorer handlers together, and raw HKCU verbs are retired. CLI `shell-plan` reports the selected path and current prerequisites. |
| Pre-IR-009 | done | `PackagingPlanService` exposes install/uninstall gates and user-data preservation policy; CLI `package-plan` previews current blockers without installing anything. |
| Pre-IR-009.1 | done | `NativeToolingSnapshot` and `PackagingPlanService` surface the native shell-extension build-tooling gate; CLI `package-plan` reports native tooling and now passes the native shell-extension-built gate when `artifacts\native\x64\Debug\IconReplacer.ShellExtension.dll` exists. |
| IR-009 | in-progress | Package filenames derive from the manifest identity; signed candidate `1.0.0.5` was installed through the explicitly approved `UpgradeInstalledPackage + KeepInstalled` path. Exact-package install, Explorer reload, Icon Library preservation, restore-state preservation, and unrelated-handler preservation pass; clean uninstall proof remains open. |
| Pre-IR-010 | done | `ReleaseReadinessService` composes build/test/CLI proof, diagnostics, package, accessibility, manual Explorer, and release-evidence gates; CLI `release-readiness` previews why release is currently blocked. |
