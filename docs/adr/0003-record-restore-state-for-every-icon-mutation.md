# Record Restore State For Every Icon Mutation

Status: accepted

Every folder or shortcut icon mutation must create a Restore Record before changing the target. Restore is a trust feature for this app, not optional polish.

## Consequences

- Folder mutations must preserve previous `desktop.ini` values and relevant attributes.
- Shortcut mutations must preserve previous icon path and icon index.
- UI, CLI, and shell flows must surface restore capability or at least record enough state for it.

