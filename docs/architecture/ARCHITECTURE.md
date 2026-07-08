# Architecture

Status: proposed
Date: 2026-07-07

## Architecture Thesis

Icon Replacer should isolate risky Windows Shell integration from the core icon mutation logic. The Core Engine must be independently testable before Explorer integration is treated as done.

## Components

### IconReplacer.Core

Responsibilities:

- validate `.ico` files,
- scan the Icon Library,
- sanitize names and dedupe imports,
- apply Folder Target icon mutations,
- apply Shortcut Target icon mutations,
- create and consume Restore Records,
- call Explorer refresh hooks.

This layer must not depend on WinUI controls or context-menu registration.

### IconReplacer.AppModel

Responsibilities:

- expose first-run setup/readiness state,
- expose app-location targets for Open Library and diagnostics actions,
- expose dashboard snapshots for app diagnostics,
- coordinate Icon Library import and status operations,
- expose filtered restore-history snapshots for recent changes,
- coordinate apply operations that mutate a target and persist restore history,
- coordinate restore operations that revert a target and update restore history,
- build bounded dynamic menu snapshots from the current Icon Library,
- keep WinUI, CLI, and shell surfaces from duplicating product workflow logic.

This layer may depend on `IconReplacer.Core`, but not on WinUI controls or shell registration.

### IconReplacer.Cli

Responsibilities:

- expose `catalog`, `apply`, `apply-folder`, `apply-shortcut`, `restore`, `history`, and `doctor`,
- provide automation-friendly output,
- support QA proof without Explorer UI,
- provide fallback launch target for Classic Shell Integration.

### IconReplacer.App

Responsibilities:

- first-run setup,
- show integration status,
- import icons,
- open `%USERPROFILE%\.icons`,
- browse categories and icons,
- list recent Icon Mutations,
- restore changes,
- show diagnostics and actionable errors.

### IconReplacer.ShellExtension

Responsibilities:

- implement Modern Shell Integration through `IExplorerCommand`,
- discover the selected Target,
- show `Change icon...`,
- expose Icon Library entries when feasible,
- use bounded AppModel menu snapshots rather than ad hoc catalog traversal,
- avoid long work on Explorer UI paths,
- delegate heavy behavior to AppModel, CLI, or app process.

## Data Locations

- Icon Library: `%USERPROFILE%\.icons`
- Imported Icons: `%USERPROFILE%\.icons\Imported`
- Restore state: `%AppData%\Icon Replacer\state.json`
- Diagnostic logs: `%LocalAppData%\Icon Replacer\Logs`

## Folder Target Mutation

The Core Engine should:

1. validate the selected `.ico`,
2. copy it to stable library storage,
3. save a Restore Record,
4. merge or create `desktop.ini`,
5. set required file/folder attributes,
6. notify Explorer.

The implementation must preserve unrelated `desktop.ini` keys.

## Shortcut Target Mutation

The Core Engine should:

1. validate the selected `.ico`,
2. copy it to stable library storage,
3. load `.lnk` with Shell link APIs,
4. save the previous icon path/index,
5. set the new icon path/index,
6. save the Shell link,
7. notify Explorer.

The implementation must not modify the shortcut target path, arguments, working directory, hotkey, or description.

## Shell Integration Options

### Preferred Product Path: Modern Shell Integration

Use package identity, MSIX registration, `windows.comServer`, `windows.fileExplorerContextMenus`, and `IExplorerCommand`.

Why:

- aligns with Windows 11 modern context menu requirements,
- supports richer command behavior,
- avoids pretending classic registry verbs fully satisfy the product.

Cost:

- native shell extension,
- packaging/signing complexity,
- higher QA burden.

### Prototype/Fallback Path: Classic Shell Integration

Use per-user `HKCU\Software\Classes` verbs for folders and `.lnk`.

Why:

- fast to prove end-to-end behavior,
- no admin required,
- useful if Modern Shell Integration is blocked.

Cost:

- likely appears under `Show more options` on Windows 11,
- dynamic submenus are fragile or require registry regeneration,
- weaker first-use product experience.

## Performance and Safety Boundaries

- No deep recursive scans in Explorer context-menu code.
- No network paths in V1 catalog scans.
- No arbitrary code execution or EXE/DLL icon extraction in V1.
- No shell extension work that can hang Explorer.
- No admin elevation as part of the normal user path.
