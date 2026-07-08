# Limit V1 To Local Ico Files

Status: accepted

V1 accepts local `.ico` files only. PNG/SVG conversion and EXE/DLL/ICL extraction are intentionally deferred because they expand format, validation, and security scope.

## Consequences

- The file picker should filter to `.ico`.
- The Core Engine still validates file headers and bounds.
- Unsupported formats must fail clearly and leave the Target unchanged.

