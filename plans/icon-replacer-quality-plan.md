# Icon Replacer Quality Plan

Date: 2026-07-07
Status: proposed
Owner: current Codex thread

## Goal

Build a simple Windows app that lets a user right-click a folder or shortcut, choose `Change icon`, select an `.ico` file, and have the icon applied automatically. The app also owns a user icon library at `%USERPROFILE%\.icons`; subfolders inside that library become icon categories in the context menu.

The product should feel like an Explorer feature first, and like a WinUI utility app second.

## Current State

- Workspace: `D:\DEV\icon-replacer`
- Repo state: no Git repository detected.
- Files before this plan: empty workspace.
- This plan is the first durable task record.

## Quality Stack

- Mission/domain: `winui-dev-workflow` for WinUI 3 build/run and Windows app workflow.
- Craft/critique: `quality-obsessed` council with product, UX, architect, QA, skeptic, and reality judge lanes.
- Verification/proof: focused unit/integration tests, package/install proof, Explorer manual proof, screenshots, `.reg` exports, and negative-path logs.
- Reference/horizon: Microsoft Learn/Support docs for Windows 11 context menus, packaged app extensions, `desktop.ini`, `IShellLinkW::SetIconLocation`, and June 2026 `desktop.ini` hardening.
- Adjacent impact audit: context menu behavior, folder icon persistence, shortcut mutation, install/uninstall, icon library, permission failures, Explorer cache refresh.

## Baseline To Beat

A competent quick pass would register a static `HKCU` context-menu verb that launches an app, writes `desktop.ini`, and handles only the happy path.

This plan must beat that by providing:

- reliable restore/undo,
- safe `.ico` validation,
- clear permission/error feedback,
- `.icons` library behavior,
- no Explorer-crashing shell work,
- proof on folders and `.lnk` shortcuts,
- a realistic path for modern Windows 11 context menus.

## Recommended Decision

Build in two layers:

1. Prove the core icon engine first with a CLI/test harness.
2. Ship the product path as a packaged WinUI 3 app plus a native Explorer command extension if the first release must appear in the modern Windows 11 context menu with dynamic category submenus.

The simpler `HKCU\Software\Classes` registry approach is useful as a prototype/fallback, but it should not be treated as the final architecture if `Change icon` must appear cleanly in the Windows 11 context menu and support dynamic `.icons` submenus.

## One Material Decision

Recommended answer: target the modern Windows 11 context menu for the real V1, but start implementation with the CLI/core proof slice.

Question for Cristian before implementation: should V1 require the modern Windows 11 right-click menu with MSIX plus `IExplorerCommand`, or is a faster V1 acceptable if it appears under `Show more options` and delays dynamic submenus?

Impact:

- Modern menu: better product, matches the request more closely, but requires MSIX/package identity, a native shell extension, signing/dev setup, and more QA.
- Fast classic menu: much simpler and likely quicker, but weaker UX on Windows 11 and cannot honestly satisfy automatic dynamic submenus without fragile registry regeneration.

Execution gate: do not scaffold the final app architecture until this decision is confirmed or the recommended default is accepted.

## Architecture

### Product V1 Architecture

- `IconReplacer.App`: WinUI 3 utility app for setup, status, import, icon library browsing, recent changes, restore, and diagnostics.
- `IconReplacer.ShellExtension`: native C++/ATL or C++/WinRT COM DLL implementing `IExplorerCommand`; optionally `IObjectWithSelection` for selected target handling and `EnumSubCommands` for catalog items.
- `IconReplacer.Core`: icon catalog, `.ico` validation, file copy/dedupe, folder icon application, shortcut icon application, restore state, Explorer refresh.
- `IconReplacer.Cli`: testable fallback surface for `apply-folder`, `apply-shortcut`, `pick`, `restore`, and diagnostics.
- State location: `%AppData%\Icon Replacer\state.json`.
- Icon library: `%USERPROFILE%\.icons`.

### Shell Integration

Preferred product path:

- packaged app with package identity,
- manifest registration with `windows.comServer`,
- manifest registration with `windows.fileExplorerContextMenus`,
- item types for folders and `.lnk`,
- `IExplorerCommand` for Windows 11 modern menu compatibility,
- one category level from `.icons` subfolders,
- fallback flattening for catalog names if deep submenus render badly.

Prototype/fallback path:

- per-user `HKCU\Software\Classes\Directory\shell\IconReplacer\command`,
- per-user `HKCU\Software\Classes\lnkfile\shell\IconReplacer\command`,
- command launches `IconReplacer.Cli --pick "%1"`,
- no admin required,
- acceptable only if classic-menu behavior is accepted.

## Core Behavior

### `.icons` Library

- Create `%USERPROFILE%\.icons` on first run.
- Copy chosen external icons into `%USERPROFILE%\.icons\Imported`.
- Dedupe by content hash and sanitized display name.
- Read only local `.ico` files by default.
- Treat subfolders as one-level categories.
- Do not follow reparse points/symlinks during catalog scan.
- Limit catalog count shown in Explorer; use `More...` or the app if the menu would become too large.

### Folder Icon Application

- Validate selected `.ico`.
- Copy icon to stable storage before applying.
- Merge or create target `desktop.ini`; never overwrite unrelated keys.
- Prefer `IconResource=<path>,0` or compatible `IconFile`/`IconIndex=0` based on implementation proof.
- Mark `desktop.ini` hidden/system.
- Mark folder with the attribute required for Explorer to process `desktop.ini`.
- Store previous `desktop.ini` values and folder/file attributes before mutation.
- Call `SHChangeNotify` after changes.
- If Explorer cache delays the visual update, report success with a refresh caveat instead of pretending instant update is guaranteed.

### Shortcut Icon Application

- Only `.lnk` in V1.
- Load via `IShellLinkW` and `IPersistFile`.
- Store previous icon path/index.
- Set icon with `IShellLinkW::SetIconLocation(iconPath, 0)`.
- Save without modifying target path, arguments, working directory, hotkey, or description.
- Refresh the changed item.

### Restore

- Restore must be part of V1, not a later nice-to-have.
- Restore folder icons by reverting only keys/attributes touched by Icon Replacer.
- Restore `.lnk` icon path/index.
- Provide restore from toast, WinUI `Recent changes`, and CLI.
- If restore cannot complete because the target moved/deleted, show a recoverable status with details.

## WinUI App Surface

The app is a compact utility, not a landing page.

Recommended first window:

- `InfoBar`: integration status, errors, setup status.
- `CommandBar`: `Import`, `Open .icons`, `Refresh`, `Settings`.
- `GridView` or `ListView`: categories and icons from `.icons`.
- `Recent changes`: last modified folders/shortcuts with `Restore`.
- Diagnostics panel: hidden behind `Show details` for registry/package/shell status.

First run:

- create `.icons`,
- verify integration status,
- offer `Import icons`, `Open .icons`, and `Test with a folder`.

Accessibility basics:

- full keyboard navigation,
- accessible names for icon-only controls,
- high contrast support,
- visible names for icons,
- persistent error messages, not toast-only failures,
- 200% DPI without clipped paths/buttons.

## Implementation Slices

1. Core model and `.ico` validation
   - Create catalog scanner, path sanitizer, hash dedupe, and `.ico` parser.
   - Unit tests for valid, corrupt, truncated, huge, renamed, and duplicate icons.

2. Folder and shortcut engine
   - Apply and restore folder icons in a temp folder.
   - Apply and restore `.lnk` icons without touching target metadata.
   - Add `SHChangeNotify` refresh hook.

3. CLI proof harness
   - `apply-folder`, `apply-shortcut`, `restore`, `catalog`, and `doctor`.
   - Logs with stable success/error codes.

4. Shell integration spike
   - Confirm chosen V1 path:
     - modern: MSIX + `IExplorerCommand`;
     - classic: per-user HKCU verb.
   - Prove right-click on folder and `.lnk` launches picker/apply path.

5. Catalog context menu
   - `Choose icon...`.
   - `.icons` root icons.
   - one-level subfolder categories.
   - empty state and large-catalog fallback.

6. WinUI management app
   - status, import, open library, refresh, recent changes, restore, settings.
   - setup/diagnostic flow for missing integration.

7. Packaging/install/uninstall
   - per-user install.
   - signed package or documented dev signing.
   - uninstall removes integration keys/registrations.
   - `.icons` is preserved unless user explicitly chooses deletion.

8. Quality pass
   - error copy, permission failures, access denied, Explorer refresh delays, high contrast, keyboard, long paths, Unicode paths.

## Acceptance Criteria

- Right-click on a normal folder exposes `Change icon`.
- `Change icon` opens directly to a file picker for `.ico`.
- Choosing a valid icon copies it into `%USERPROFILE%\.icons\Imported` and applies it.
- A normal `.lnk` shortcut can receive and restore a custom icon.
- `%USERPROFILE%\.icons\Work\blue.ico` appears as category `Work` with item `blue` in the next context menu/catalog refresh.
- Empty `.icons` state is useful: `Choose icon...`, `Open .icons`, and `No icons yet`; no dead submenu.
- Invalid `.ico` leaves the target untouched and shows a clear error.
- Permission failure leaves the target untouched and explains recovery.
- Restore returns folder/shortcut icon state to the previous state.
- Existing `desktop.ini` keys unrelated to Icon Replacer survive.
- Uninstall removes integration but does not delete user icons without explicit consent.

## Adversarial Matrix

- Corrupt `.ico`: rejected before apply; target unchanged.
- `.png` renamed to `.ico`: rejected by header validation.
- Large icon file: rejected or capped; app remains responsive.
- Existing `desktop.ini`: merged with backup; unrelated values preserved.
- Protected folder: access denied with recovery guidance; no partial mutation.
- OneDrive/local sync folder: apply attempted only if local write succeeds; clear cache-delay messaging.
- UNC/network folder: blocked or warned in V1 due June 2026 `desktop.ini` trust hardening.
- `.lnk` with broken target: icon change can still work; target metadata unchanged.
- `.url` shortcut: out of scope V1 unless explicitly added.
- Catalog deleted while menu opens: menu degrades gracefully and offers `Open .icons`.
- Many icons/categories: cap menu items and route to WinUI app.
- Multiselect: disabled in V1 or requires explicit future design.

## Verification

Commands/proof expected after implementation:

- `dotnet build` or `BuildAndRun.ps1 -SkipRun` for WinUI project.
- Unit tests for catalog, `.ico` validation, `desktop.ini` merge, restore state.
- CLI integration test creates temp folder and temp `.lnk`, applies icon, restores icon.
- Package/install proof:
  - modern path: manifest registration visible and Explorer command appears;
  - classic path: exported HKCU `.reg` shows only Icon Replacer keys.
- Manual Explorer proof screenshots:
  - context menu,
  - picker,
  - folder before/after/restore,
  - `.lnk` before/after/restore,
  - invalid icon error,
  - protected path error.
- Uninstall proof that integration is removed and `.icons` remains.

Expected result: main path and at least one recovery path are green before claiming `implemented`.

## Sources Checked

- Microsoft Windows app best practices: Windows 11 context menu extensions require `IExplorerCommand` and package identity for the modern context menu.
- Microsoft packaged desktop app extensions: `windows.comServer` and `windows.fileExplorerContextMenus` manifest registration.
- Microsoft `desktop4:FileExplorerContextMenus` schema.
- Microsoft `desktop.ini` folder customization.
- Microsoft `IShellLinkW::SetIconLocation`.
- Microsoft shortcut menu handler guidance, including cascading menus and `EnumSubCommands`.
- Microsoft Support June 2026 `desktop.ini` hardening for untrusted/remote sources.

## STOP Conditions

- Stop and ask if modern Windows 11 context menu is mandatory and packaging/signing setup is missing.
- Stop if a proposed path requires admin/elevation for normal use.
- Stop before supporting EXE/DLL/ICL extraction or PNG/SVG conversion in V1.
- Stop before modifying protected or remote folders automatically.
- Stop if dynamic submenu behavior requires deep nesting or fragile registry regeneration.
- Stop if verification cannot prove folder and `.lnk` restore.

## Council Disagreement Resolved

- Architect/Implementer favored MSIX + `IExplorerCommand` as the real product architecture.
- QA/Skeptic favored a faster HKCU classic-menu V1 for low risk.
- Product/UX favored Explorer-native feel and dynamic `.icons` library.

Resolution: build the core engine first, then choose the shell integration level before scaffolding the final app. The recommended product direction is modern shell integration; the recommended risk-control tactic is to prove the icon mutation engine independently first.

