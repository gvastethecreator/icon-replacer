# Icon Replacer Context

Project glossary for Icon Replacer. This file defines product-domain terms only; implementation details live in architecture docs and ADRs.

## Language

**Icon Replacer**:
The Windows utility that changes folder and shortcut icons and manages the user's reusable icon library.
_Avoid_: icon changer, icon patcher

**Target**:
The folder or shortcut selected by the user in File Explorer.
_Avoid_: file, item, object when the type matters

**Folder Target**:
A filesystem directory whose display icon can be customized through `desktop.ini`.
_Avoid_: directory target unless referring to Win32 APIs

**Shortcut Target**:
A `.lnk` Shell link whose icon location can be changed without changing its destination.
_Avoid_: link target, URL shortcut

**Icon Library**:
The user-owned `%USERPROFILE%\.icons` folder where Icon Replacer stores and discovers reusable `.ico` files.
_Avoid_: asset folder, cache

**Imported Icons**:
Icons copied by Icon Replacer into the Icon Library after a user selects an external `.ico`.
_Avoid_: downloads, temp icons

**Icon Category**:
A one-level subfolder inside the Icon Library that groups icons in the app and, when supported, in the context menu.
_Avoid_: nested category, pack

**Icon Mutation**:
Any operation that changes the icon metadata of a Folder Target or Shortcut Target.
_Avoid_: edit when restore semantics matter

**Restore Record**:
The saved state needed to undo one Icon Mutation.
_Avoid_: log entry, history item

**Modern Shell Integration**:
The Windows 11 context-menu path implemented with package identity and an `IExplorerCommand` shell extension.
_Avoid_: native menu when the packaging mechanism matters

**Classic Shell Integration**:
The per-user registry verb path under `HKCU\Software\Classes` that may appear under `Show more options` on Windows 11.
_Avoid_: fallback menu unless specifically describing a fallback build

**Core Engine**:
The implementation layer that validates icons, updates folder or shortcut metadata, records restore state, and refreshes Explorer.
_Avoid_: backend, service

