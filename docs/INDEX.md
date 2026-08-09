# Documentation Index

Date: 2026-08-09

## Start Here

- [README](../README.md): status, product direction, and current shell integration path.
- [Context](../CONTEXT.md): canonical vocabulary.
- [Quality Plan](../plans/icon-replacer-quality-plan.md): original council synthesis and acceptance gates.

## Product

- [Product Requirements](product/PRD.md): user outcomes, scope, acceptance criteria, and non-goals.
- [Gallery First](design/GALLERY_FIRST.md): selected light/dark visual truth and interaction rules.
- [Official App Icon Assets](design/app-icon/OFFICIAL-ASSETS.md): canonical source, generated derivatives, and runtime routing.

## Architecture

- [Architecture](architecture/ARCHITECTURE.md): components, data flow, shell integration options, and implementation boundaries.
- [Architecture Review](architecture/architecture-review-2026-07-15.md): evidence-backed deepening, optimization, and performance recommendations.
- [Shell Integration](architecture/SHELL_INTEGRATION.md): modern and classic context-menu contracts.
- [ADRs](adr/): durable decisions, including [packaged dual Explorer integration](adr/0011-package-modern-and-classic-explorer-integrations.md).

## Specs

- [V1 Scope](product/V1_SCOPE.md): included and deferred behavior.
- [Icon Engine Contract](spec/ICON_ENGINE_CONTRACT.md): `.ico`, `desktop.ini`, `.lnk`, restore, and atomicity contract.
- [Restore State](spec/RESTORE_STATE.md): state shape and restore guarantees.
- [Icon Library Catalog](spec/ICON_LIBRARY_CATALOG.md): `%USERPROFILE%\.icons` discovery and import rules.

## Execution

- [Development Workplan](development/WORKPLAN.md): sequencing, mission control, and execution gates.
- [Progress](development/PROGRESS.md): implemented slices, proof, and next action.
- [Backlog](tasks/BACKLOG.md): epics and implementation tasks.
- [Verification](development/VERIFICATION.md): required command/manual proof before done.

## Quality and Operations

- [QA Test Plan](qa/TEST-PLAN.md): functional, adversarial, accessibility, install, and Explorer proof matrix.
- [Accessibility Acceptance](qa/ACCESSIBILITY_ACCEPTANCE.md): keyboard, high contrast, DPI, and UIA expectations.
- [Manual Explorer Proof](verification/MANUAL_EXPLORER_PROOF.md): screenshot and Explorer cache proof checklist.
- [Release Evidence Template](verification/RELEASE_EVIDENCE_TEMPLATE.md): release proof packet shape.
- [Shell Extension Safety](security/SHELL_EXTENSION_SAFETY.md): Explorer safety rules.
- [Desktop.ini Hardening](security/DESKTOP_INI_HARDENING.md): local, remote, MOTW, and June 2026 policy.
- [Installation and Packaging](operations/INSTALLATION.md): packaging paths, install/uninstall expectations, and dev prerequisites.
- [Release Downloads](operations/RELEASE.md): candidate download, integrity verification, certificate trust, install, and uninstall steps.
- [Maintenance 2026-08-09](MAINTENANCE_2026-08-09.md): dependency migrations, repository cleanup, quality review, and current gates.
- [Changelog](../CHANGELOG.md): user-visible changes by release.
- [Risks](RISKS.md): risk register and mitigations.
- [Contributing](../CONTRIBUTING.md): development and pull-request expectations.
- [Security Policy](../SECURITY.md): private vulnerability reporting and safe testing.

## Current Product State

V1 uses one signed per-user MSIX for both Explorer surfaces: native
`IExplorerCommand` entries in the Windows 11 menu and a packaged classic handler
under `Show more options`. Both call the zero-window command host; the Gallery
First WinUI app opens only from its normal shortcut. Raw HKCU verbs are retired.
The lifecycle guard itself is green. The previous development candidate is
currently installed; candidate updates and install/uninstall proof remain explicitly
approval-gated.
