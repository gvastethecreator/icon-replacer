# Documentation Index

Date: 2026-07-07

## Start Here

- [README](../README.md): status, product direction, and current shell integration path.
- [Context](../CONTEXT.md): canonical vocabulary.
- [Quality Plan](../plans/icon-replacer-quality-plan.md): original council synthesis and acceptance gates.

## Product

- [Product Requirements](product/PRD.md): user outcomes, scope, acceptance criteria, and non-goals.

## Architecture

- [Architecture](architecture/ARCHITECTURE.md): components, data flow, shell integration options, and implementation boundaries.
- [Shell Integration](architecture/SHELL_INTEGRATION.md): modern and classic context-menu contracts.
- [ADRs](adr/): durable decisions and proposed decisions.

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
- [Risks](RISKS.md): risk register and mitigations.

## Current Grilling State

The V1 shell integration decision is accepted as Modern Shell Integration: MSIX package identity plus a native `IExplorerCommand` extension. Classic HKCU verbs remain a fallback/prototype path. WinUI tooling and native build tools are available; the selected path is not configured yet because the package identity, native extension, signing, installer, Explorer registration, and install/uninstall proof still need implementation.
