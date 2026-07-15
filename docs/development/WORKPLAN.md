# Development Workplan

Status: in progress
Date: 2026-07-14

## Mission Control

Objective: develop Icon Replacer into a complete Windows app that can change and restore local folder, directory-link, and `.lnk` shortcut icons from Explorer.

Current loop: core mutation, Gallery First management UI, packaged modern/classic
Explorer integration, zero-window command execution, signed packaging, bounded
preview-capable native menus, guarded install/uninstall, and an About/update
surface are implemented. Release acceptance remains open for the explicitly
approved manual Explorer matrix and current-candidate runtime accessibility
evidence at 100%, 200%, and High Contrast.

Highest-leverage next item: after approval, run the accessibility UIA matrix and
the guarded lifecycle against the exact current package, perform the visual
Explorer matrix, verify coexistence with StartAllBack and the existing handlers,
and return to the agreed final package state. The previous candidate is currently
installed; `package-plan` keeps proof warnings open until the exact current
candidate completes the transactional lifecycle and visual checks.

Expected proof: package identity, signing, install/uninstall proof, dynamic submenu proof, and manual Explorer context-menu proof before Explorer registration is considered complete.

WinUI/native preflight: .NET 10, WinUI templates, Developer Mode, `winapp` 0.4.0, CMake, and Visual Studio C++ Build Tools are available. Use `scripts\Initialize-NativeToolchain.ps1 -PassThru` when a native build shell needs `cl.exe` and MSBuild on PATH.

Accepted decision: packaged dual integration for V1. Native `IExplorerCommand` serves the Windows 11 menu, while `windows.fileExplorerClassicContextMenuHandler` serves `Show more options`; the raw Classic HKCU verb prototype is retired.

## Execution Gate

The shell decision is resolved by ADR-0011. Do not leave Explorer integration
registered after automated proof, and do not begin a visual registration run
without explicit approval. The lifecycle runner enforces that boundary with
`-ApproveExplorerRegistration`; the manual matrix uses `-InteractiveProof` so
registration, evidence capture, uninstall, and baseline verification remain one
transactional session.

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

Done. Temp-folder integration tests apply and restore normal folders, local junctions, and local directory symbolic links while preserving unrelated `desktop.ini` keys and link identity. Restore history is serialized across the app and CommandHost and persisted with flushed atomic replacement; complete apply/restore transactions are serialized per canonical Target path, and persistence failures compensate both apply and restore mutations for folders and shortcuts.

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

- Create one packaged WinUI app with modern `IExplorerCommand` and packaged classic context-menu handlers.
- Do not create Classic HKCU registry verbs; package lifecycle owns both Explorer paths.
- Use AppModel shell-manifest contract for the MSIX COM/context-menu entries before packaging.
- Use AppModel shell-extension bridge snapshots so the future native `IExplorerCommand` can follow one tested manifest/menu/selection/argument contract.
- Use AppModel shell-selection evaluation so `Change icon...` is enabled only for exactly one local folder, directory link, or `.lnk`.
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

Implementation complete in source. The direct command launches only the native picker through `IconReplacer.CommandHost`; the collection submenu enumerates `%USERPROFILE%\.icons` dynamically and applies an icon without opening the management window. The classic handler groups both commands with a separator and application icons, eagerly attaches renderable 32-bit ARGB previews, and is capped at 8 collections/30 icons/242 command ids. Modern commands now hide during classic dispatch to prevent duplicate groups. Native COM smoke proves normal-modern/classic-hidden coexistence, every collection and icon preview, a 250 ms full-catalog query budget, and coexistence with an 8-id Explorer range. Replacing the installed previous candidate remains gated on the registry-preservation lifecycle check.

### Slice 7: WinUI App

- Build the selected Gallery First management UI from `docs/design/GALLERY_FIRST.md`.
- Show real virtualized `.ico` previews in the gallery, collection mosaics, and history.
- Cache one catalog snapshot per refresh and keep tooling probes out of navigation.
- Support System, Light, Dark, High Contrast, Mica, and restrained Acrylic.
- Add import, open library, refresh, recent changes, restore, and diagnostics.
- Add About with official identity, version, project credits, GitHub links, and asynchronous latest-release detection.
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

Done in source. Library, Recent, Settings, and About are distinct; all visible previews are real `.ico` images; catalog/search/navigation/theme behavior is non-blocking; Light/Dark and Mica/Acrylic are implemented; About checks GitHub Releases only after navigation and reports update, current, no-release, timeout, and offline states. Gallery First UI Automation has a repeatable About pass, 287 automated tests cover the current source, and `design-qa.md` records a passing Gallery comparison. A 125% run against the previous installed candidate exposed one missing Settings accessible name plus a brittle UIA property read; both are corrected in source. Runtime proof of the current candidate at 100%, 200%, and High Contrast remains an explicit release gate.

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
- Restore proof fails for a folder, local directory link, or `.lnk`.
- User requires unsupported V1 features such as PNG conversion, `.url`, or network folders.
