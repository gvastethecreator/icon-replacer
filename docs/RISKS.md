# Risk Register

Status: proposed
Date: 2026-07-07

## High Risks

### RISK-001: Shell Integration Complexity

Modern Shell Integration requires package identity, manifest registration, and a native command extension.

Mitigation: build Core Engine and CLI first; keep Classic Shell Integration as a fallback only after explicit decision.

### RISK-002: Explorer Stability

Shell extension code can hurt Explorer if it blocks, crashes, or scans too much.

Mitigation: keep Explorer path small, bounded, local-only, and delegate heavy work outside the menu path.

### RISK-003: Restore Failure

Icon mutations are trust-sensitive; failed restore would make the app feel unsafe.

Mitigation: create Restore Records before every mutation and test restore on folder and `.lnk`.

### RISK-004: `desktop.ini` Trust Hardening

Windows security updates from June 2026 can ignore `desktop.ini` from untrusted sources.

Mitigation: V1 supports local trusted folders only and does not auto-unblock or bypass Windows policy.

### RISK-005: Explorer Icon Cache Delay

The icon may not visually update immediately even after metadata changes.

Mitigation: call Explorer refresh APIs and show honest success/caveat feedback.

## Medium Risks

### RISK-006: Protected Paths

Program Files, Public Desktop, or admin-owned folders may reject changes.

Mitigation: no auto-elevation in V1; show clear permission error and leave target unchanged.

### RISK-007: Catalog Size

Large `.icons` libraries can make context menus unusable.

Mitigation: cap menu entries and route overflow to the WinUI app.

### RISK-008: Format Creep

PNG/SVG/EXE/DLL support would expand validation and security risk.

Mitigation: V1 accepts only local `.ico`.

## Low Risks

### RISK-009: User Deletes Icon Library

Applied icon references can break if stored icons disappear.

Mitigation: app can recreate `.icons`; restore records should surface missing-icon recovery.

