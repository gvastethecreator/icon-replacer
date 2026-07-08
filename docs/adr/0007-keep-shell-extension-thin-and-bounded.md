# Keep Shell Extension Thin And Bounded

Status: accepted

Explorer-facing code must stay thin, bounded, and safe. Heavy catalog scans, mutation logic, and complex UI must not live in the shell extension path.

## Consequences

- Core Engine owns mutation logic.
- Shell integration can degrade to `Change icon...` or the WinUI app when catalog enumeration is unsafe.
- Context-menu performance and Explorer stability are quality gates.
