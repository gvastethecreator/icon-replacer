# No Admin Required For Normal Use

Status: accepted

Normal icon changes, library management, and per-user shell integration must not require elevation. Protected locations may fail with clear recovery guidance instead of triggering automatic admin behavior.

## Consequences

- V1 is scoped to per-user behavior.
- Enterprise/all-users install is out of scope until explicitly requested.
- Permission failures must leave Targets unchanged.

