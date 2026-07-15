# Architecture review — Icon Replacer

Date: 2026-07-15

## Summary

- The highest-cost friction is repeated filesystem work: one Home route composition can trigger five full Icon Library scans and five restore-state reads.
- The production WinUI path and the AppModel route-planning path model the same experience separately. The deletion test shows the route-planning path is not yet part of the product seam.
- Large multi-purpose files concentrate unrelated change risk: the CLI is 3,060 lines, the native shell implementation is 1,854 lines, and the main WinUI page spans 1,057 lines of XAML plus 1,039 lines of code and view-model implementation.
- Optimization should begin by making one request produce one read snapshot. UI throttling or micro-optimizations cannot recover repeated catalog validation upstream.

## Recommendations

### 1. Deepen the application read-model module

**Recommendation strength**: Strong

**Files**

- `src/IconReplacer.Core/IconCatalogService.cs`
- `src/IconReplacer.Core/IconValidator.cs`
- `src/IconReplacer.Core/RestoreRecordStore.cs`
- `src/IconReplacer.AppModel/DashboardService.cs`
- `src/IconReplacer.AppModel/SetupReadinessService.cs`
- `src/IconReplacer.AppModel/AppDiagnosticsService.cs`
- `src/IconReplacer.AppModel/AppHomeService.cs`
- `src/IconReplacer.AppModel/AppWindowService.cs`
- `src/IconReplacer.AppModel/AppRouteViewService.cs`

**Problem**

Read composition is shallow — each caller asks another module to rescan the same files. `AppRouteViewService.GetView` builds an `AppWindowSnapshot`; that calls diagnostics, which calls setup and dashboard. Home composition then calls setup, dashboard, menu, and history again. The resulting Home path can execute five `IconCatalogService.Scan` calls and five `RestoreRecordStore.List` calls. Each scan reopens and validates every `.ico`; with the documented 185-icon catalog, that is up to 925 ICO validations for one composed Home view.

**Solution**

Deepen one request-scoped application read-model module. Capture paths, one validated `IconCatalog`, one restore-record set, and request time once; derive setup, diagnostics, dashboard, menu, history, and Home projections from that captured state. Invalidation belongs at the mutation/import/refresh seam, not inside each projection.

**Benefits**

- locality: filesystem reads, validation, invalidation, and failure handling concentrate in one module
- leverage: one captured read supplies every route projection
- interface shrinks: projection modules consume known state instead of reopening storage
- performance: catalog validation and restore JSON parsing become O(N) per refresh rather than O(routes × N)
- test seam: one deterministic captured state can drive setup, diagnostics, Home, and route tests

**Before / After**

Before: route → window → diagnostics → setup/dashboard → repeated filesystem reads; route → Home → setup/dashboard/menu/history → more reads.

After: route → one captured application read → pure projections for window, diagnostics, setup, Home, menu, and history.

**Dependencies / sequencing**

- Do this first; it gives every later refactor a stable state seam.
- Add scan-count and restore-read-count tests before moving callers.
- Preserve explicit refresh after import, restore, or external library changes.

**Documentation follow-ups**

- Add the accepted read-state term to `CONTEXT.md`.
- Record invalidation ownership in `docs/architecture/ARCHITECTURE.md`.
- Add performance budgets and scan-count proof to `docs/development/VERIFICATION.md`.

### 2. Resolve the duplicate application-model paths

**Recommendation strength**: Strong

**Files**

- `src/IconReplacer.App/ViewModels/MainPageViewModel.cs`
- `src/IconReplacer.App/MainPage.xaml.cs`
- `src/IconReplacer.AppModel/AppRouteViewService.cs`
- `src/IconReplacer.AppModel/AppCommandService.cs`
- `src/IconReplacer.AppModel/AppHomeService.cs`
- `src/IconReplacer.Cli/Program.cs`
- `tests/IconReplacer.Core.Tests/AppRouteViewServiceTests.cs`
- `tests/IconReplacer.Core.Tests/AppCommandServiceTests.cs`

**Problem**

Two implementations model the management experience. Production WinUI directly constructs nine concrete dependencies in `MainPageViewModel`; it does not consume `AppRouteViewService` or `AppCommandService`. Those AppModel modules are exercised by the CLI, tests, and planning docs. Under the deletion test, removing the route-planning modules does not move their complexity into the real UI path; it removes a parallel model. `AppRouteViewService` is also shallow at its seam: a 15-parameter constructor exposes most of its implementation graph to callers.

**Solution**

Choose the production management workflow as the source of truth. Preserve only AppModel behavior used by WinUI, command host, or an explicit automation contract; migrate valuable route behavior into feature-owned modules and delete planning-only projections. Do not keep a second render model only because CLI snapshots can print it.

**Benefits**

- locality: each management feature owns its state, commands, and verification
- leverage: WinUI, CLI, and tests share real workflow behavior instead of parallel projections
- depth: callers ask for a feature outcome, not a graph of nullable concrete dependencies
- deletion: obsolete snapshots and pass-through modules disappear instead of being renamed

**Before / After**

Before: WinUI → direct feature dependencies, while CLI → route snapshots → separate command and content projections.

After: WinUI and supported automation adapters call the same feature modules; planning-only types are removed.

**Dependencies / sequencing**

- Start after recommendation 1 supplies a stable captured read.
- Inventory every AppModel type by production caller before deleting anything.
- Keep command-host mutation flows isolated from management UI startup.

**Documentation follow-ups**

- Update `docs/architecture/ARCHITECTURE.md` with the selected source of truth.
- Retire stale AppModel contracts from `docs/spec/ICON_ENGINE_CONTRACT.md`.
- Add an ADR only if CLI compatibility requires keeping a public snapshot format.

### 3. Deepen the gallery-loading module and bound asynchronous work

**Recommendation strength**: Strong

**Files**

- `src/IconReplacer.App/MainPage.xaml`
- `src/IconReplacer.App/MainPage.xaml.cs`
- `src/IconReplacer.App/ViewModels/MainPageViewModel.cs`
- `src/IconReplacer.App/IconThumbnailCache.cs`
- `src/IconReplacer.App/ViewModels/MainPageModels.cs`
- `tests/ui/gallery-first-ui.ps1`
- `tests/ui/responsive-grid-ui.ps1`

**Problem**

Gallery behavior leaks across XAML, page events, the view model, and the thumbnail cache. Four template areas attach `Loaded`, `DataContextChanged`, and `SizeChanged` to the same async loader. The cache bounds completed images at 256 entries but does not bound concurrent decodes or cancel obsolete requests. Search rebuilds `VisibleIcons` on every text change. Fast resize, scroll, theme, and search activity can therefore create work that is already stale when it finishes.

**Solution**

Deepen a gallery-loading module that owns request identity, cancellation, decode concurrency, cache eviction, and stale-result rejection. Move search/category projection behind a coalesced query update. Keep layout calculation and thumbnail lifecycle behind one interface used by the page.

**Benefits**

- locality: image lifetime, cache policy, and cancellation live together
- leverage: one request policy serves gallery, collection, recent, and history previews
- performance: bounded decodes reduce file-handle and dispatcher pressure during scroll and resize
- interface shrinks: XAML stops coordinating three event paths per image
- test seam: deterministic scheduler and cache counters make stale-result behavior provable

**Before / After**

Before: each `Image` event starts or joins work; the page checks tags after completion.

After: visible-item intent enters one bounded loader; obsolete generations cancel before decode or assignment.

**Dependencies / sequencing**

- Implement after recommendation 1 so refresh invalidation is explicit.
- Add counters for queued, active, cached, cancelled, and failed decodes.
- Preserve current virtualization and accessibility behavior.

**Documentation follow-ups**

- Add decode concurrency and cache budgets to `docs/development/VERIFICATION.md`.
- Record gallery performance evidence in `docs/design/GALLERY_FIRST.md`.

### 4. Split the CLI by command domain

**Recommendation strength**: Medium

**Files**

- `src/IconReplacer.Cli/Program.cs`
- `src/IconReplacer.Cli/IconReplacer.Cli.csproj`

**Problem**

`Program.cs` is a 3,060-line module with 90 static methods and more than 50 primary command/alias cases. Dispatch, argument parsing, environment probes, evidence parsing, output formatting, and workflow execution share one implementation. A command change commonly touches the top-level switch, a parser, output code, and the 100-line usage block.

**Solution**

Deepen command-domain modules for library, mutation/restore, app inspection, packaging/readiness, and tooling diagnostics. Keep one small dispatch interface and one shared result presenter. Retain aliases as data rather than switch branches.

**Benefits**

- locality: parsing, execution, usage, and tests for one command domain stay together
- leverage: one presenter maps `OperationResult` and errors consistently
- depth: the entry module exposes command discovery and execution without owning every implementation
- build/test focus: command-domain tests can run without compiling unrelated parsing code

**Before / After**

Before: one file is the dispatcher, parser library, presenter, probe runner, and evidence reader.

After: the entry module selects a command-domain adapter; each adapter owns its implementation and help.

**Dependencies / sequencing**

- Do after recommendation 2 decides which inspection commands remain supported.
- Preserve current exit codes and command aliases with contract tests.

**Documentation follow-ups**

- Move the supported CLI contract to a focused `docs/cli.md` only if the CLI remains user-facing.
- Trim obsolete command lists from `docs/spec/ICON_ENGINE_CONTRACT.md`.

### 5. Split native Explorer roles at crash-containment seams

**Recommendation strength**: Strong

**Files**

- `src/IconReplacer.ShellExtension/IconReplacer.ShellExtension.cpp`
- `src/IconReplacer.ShellExtension/CMakeLists.txt`
- `tests/native/ShellExtensionSmoke.cpp`
- `docs/security/SHELL_EXTENSION_SAFETY.md`

**Problem**

The 1,854-line native module contains target parsing, filesystem enumeration, WIC bitmap creation, command-host launch, modern `IExplorerCommand` implementations, classic `IContextMenu3`, COM factories, and exports. The modern and classic adapters share useful behavior, but their lifecycle and failure modes are interleaved inside one implementation file loaded by Explorer. During this review, the first cold smoke attempt missed the combined classic-menu gate; the immediate retry and five repetitions passed with 187 commands and 64-73 ms query time. Cold-start latency therefore remains a separate unproven case even though the warm budget is stable.

**Solution**

Deepen native modules around existing seams: catalog enumeration, preview bitmap ownership, command execution, modern Explorer adapter, classic Explorer adapter, and COM activation. Keep Windows interfaces at the outer adapters and move shared policy into narrow internal modules.

**Benefits**

- locality: bitmap ownership, menu-id allocation, and COM lifetime bugs concentrate in their modules
- leverage: both Explorer adapters reuse one catalog and command policy
- test seam: native smoke can exercise enumeration and bitmap modules without constructing every COM adapter
- safety: smaller adapter implementations reduce the area reviewed for Explorer crash and leak risk

**Before / After**

Before: every native concern compiles and changes inside one Explorer-loaded implementation.

After: two Explorer adapters depend on shared internal modules with focused lifetime and performance tests.

**Dependencies / sequencing**

- Preserve behavior first; do not combine this with menu UX changes.
- Extract pure/path logic before COM ownership code.
- Keep the 250 ms full-catalog smoke budget; record cold and warm runs separately and add leak/lifetime repetitions.

**Documentation follow-ups**

- Update `docs/architecture/SHELL_INTEGRATION.md` with module ownership.
- Update `docs/security/SHELL_EXTENSION_SAFETY.md` with lifetime and failure isolation proof.

## Suggested execution order

1. Deepen the application read-model module — removes repeated I/O and creates the state seam needed by later work.
2. Resolve duplicate application-model paths — removes speculative complexity before further interface design.
3. Deepen gallery loading — uses the stable read/invalidation seam to bound real UI work.
4. Split native Explorer roles — high safety value, behavior-preserving, independent of management UI after shared policy is clear.
5. Split the CLI — wait until supported automation contracts are known so obsolete commands are deleted rather than reorganized.

## Documentation fan-out

- `CONTEXT.md`: add accepted terms for captured application state and management feature ownership.
- `docs/adr/`: add only decisions that preserve a CLI snapshot contract or choose a surprising invalidation strategy.
- `docs/architecture/ARCHITECTURE.md`: record accepted module ownership and seams.
- `docs/development/WORKPLAN.md`: track accepted recommendations in the execution order above.
- `docs/development/VERIFICATION.md`: add scan, restore-read, decode-concurrency, cache, and native lifetime budgets.

Recommendations are pending acceptance. Final interfaces, ADRs, context updates, and workplan fan-out intentionally remain deferred.
