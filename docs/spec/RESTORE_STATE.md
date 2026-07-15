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
- Restore preview views classify one Restore Record before mutation and expose the blocking reason or warning shown to the user.
- Product restore rejects records that are not currently `Applied`; restored history remains visible but is not treated as actionable.
- App and CommandHost reads and read-modify-write operations share one per-state-file interprocess lock.
- Saves flush a unique same-directory temporary file before atomically replacing `state.json`, so readers never observe a partial JSON document.
- Lock or replacement failure preserves the previous valid state file, removes temporary output when possible, and returns an actionable error.
- If an apply reaches Target mutation before restore-state persistence fails, the product operation automatically restores the folder or shortcut while its Target lock is still held.
- If that compensation also fails, the result is `PartialFailure` and preserves both the persistence and rollback causes for recovery.
- If disk restore completes but the `Restored` status cannot be saved, the product operation reapplies the recorded icon so the Target remains consistent with the stored `Applied` record.
- Failed reapply after a restore-state update failure reports both causes as `PartialFailure`.

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

## Restore Preview Contract

`IconRestorePreviewService` reads one Restore Record by id and does not mutate target files or restore state.

It returns:

- the same `RestoreRecordSummary` health fields used by history,
- `PreviousStateDetail` for a human-readable confirmation line,
- `RestoreActionDetail` for the action the app will perform,
- `CanRestore`,
- `WarningText` when the applied icon file is missing but restore can continue,
- an `Error` when the restore action must be disabled.

Blocking rules:

- non-`Applied` records return `InvalidArgument`,
- missing targets return `PathNotFound`,
- missing applied icon files are warnings, not blockers.
