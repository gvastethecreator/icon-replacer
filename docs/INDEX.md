# Documentation index

Start with the [README](../README.md) for product status, download, and build from source. Contributor rules are in [CONTRIBUTING.md](../CONTRIBUTING.md). Report vulnerabilities through [SECURITY.md](../SECURITY.md).

## Product

- [Gallery First](design/GALLERY_FIRST.md): visual hierarchy and interaction rules
- [Official app icon assets](design/app-icon/OFFICIAL-ASSETS.md): canonical source and generated derivatives

## Specs

- [Icon engine contract](spec/ICON_ENGINE_CONTRACT.md): `.ico`, `desktop.ini`, `.lnk`, restore, and atomicity
- [Restore state](spec/RESTORE_STATE.md): record shape and restore guarantees
- [Icon library catalog](spec/ICON_LIBRARY_CATALOG.md): `%USERPROFILE%\.icons` discovery and import

## Setup and release

- [Installation and packaging](operations/INSTALLATION.md): packaging paths, uninstall policy, and developer setup
- [Release downloads](operations/RELEASE.md): candidate download, hash check, certificate trust, install, and uninstall
- [Changelog](../CHANGELOG.md): user-visible changes by release
- [Privacy policy](../PRIVACY.md): local data, network use, and Store vs direct updates

## Tests and verification

- [Verification](development/VERIFICATION.md): commands and manual proof before calling work done
- [QA test plan](qa/TEST-PLAN.md): functional, adversarial, accessibility, install, and Explorer matrix
- [Accessibility acceptance](qa/ACCESSIBILITY_ACCEPTANCE.md): keyboard, names, high contrast, and scaling
- [Manual Explorer proof](verification/MANUAL_EXPLORER_PROOF.md): screenshot and Explorer-cache checklist
- [Release evidence template](verification/RELEASE_EVIDENCE_TEMPLATE.md): release proof packet

## Security

- [Shell extension safety](security/SHELL_EXTENSION_SAFETY.md): Explorer-handler bounds
- [Desktop.ini hardening](security/DESKTOP_INI_HARDENING.md): local, remote, MOTW, and June 2026 policy

## Microsoft Store

- [Store submission runbook](store/README.md)
- [Store listing source](store/LISTING.md)
- [Certification notes](store/CERTIFICATION-NOTES.md)
- [Release evidence template](store/RELEASE-EVIDENCE-TEMPLATE.md)
- [Source-license decision](store/LICENSE-DECISION.md)

## Current product state

V1 ships one signed per-user MSIX for both Explorer surfaces: native `IExplorerCommand` entries in the Windows 11 menu and a packaged classic handler under **Show more options**. Both call the zero-window command host. The Gallery First WinUI app opens only from its normal shortcut. Raw HKCU verbs are retired.

Stable release remains blocked until clean install, update, uninstall, Explorer modern/classic scenarios, accessibility, Store listing/privacy, and source-license decisions have complete evidence.