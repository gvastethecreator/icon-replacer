# Restore State

Status: proposed
Date: 2026-07-07

## Location

Restore state lives in `%AppData%\Icon Replacer\state.json`.

## Record Requirements

Each Restore Record should include:

- record id,
- target path,
- target type,
- mutation timestamp,
- icon path applied,
- previous folder icon keys or previous shortcut icon location,
- previous attributes touched by Icon Replacer,
- operation status,
- error/recovery details when partial failure happens.

## Guarantees

- A Restore Record is created before mutation.
- Restore changes only Icon Replacer-owned edits.
- Restore failure is reported with a recovery reason.
- Missing or moved Targets do not cause silent deletion of history.
- Recent-change views classify each Restore Record with target availability, applied-icon availability, and whether restore is currently possible.
- Product restore rejects records that are not currently `Applied`; restored history remains visible but is not treated as actionable.

## Non-Guarantees

- V1 does not promise perfect restore after target move.
- V1 does not track file IDs or USN journal state.
- V1 does not restore user edits made after Icon Replacer changed the same keys unless conflict handling is explicitly added later.

## Current Health Classification

The AppModel layer exposes:

- `TargetExists`: folder target still exists or `.lnk` file still exists.
- `AppliedIconExists`: copied/imported icon path still exists.
- `CanRestore`: record status is `Applied` and the target exists.

`CanRestore` does not require `AppliedIconExists` because restoring uses the previous state snapshot, not the applied icon file. Missing icon paths are still surfaced as history warnings.
