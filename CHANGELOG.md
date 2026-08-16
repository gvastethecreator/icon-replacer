# Changelog

All notable user-visible changes to Icon Replacer are recorded here. The project uses Semantic Versioning for GitHub tags and four-part numeric versions for MSIX packages.

## [Unreleased]

### Planned

- Complete clean-install/uninstall lifecycle proof.
- Complete the Windows Explorer target matrix and 200%/High Contrast accessibility proof.
- Replace development signing with a trusted distribution identity before a stable release.

## [1.0.0-rc.1] - 2026-07-15

MSIX package version: `1.0.0.6`.

### Added

- Gallery-first WinUI 3 app with Library, Recent, Settings, and About surfaces.
- Windows 11 modern and classic File Explorer commands for folders and `.lnk` shortcuts.
- Native icon picker, collection submenus, previews, and a zero-window command host.
- Reusable `%USERPROFILE%\.icons` library with imports, collections, search, and thumbnail caching.
- Restore history with compensated folder/shortcut mutations and per-target concurrency control.
- GitHub release update checks from the About page.
- Reproducible release bundle generation with MSIX, public certificate, SHA-256 checksums, release notes, and build evidence.

### Changed

- Replaced the previous split light/dark identity with one canonical app icon across the app, package, Explorer commands, and generated assets.
- Unified modern and classic Explorer integration inside the same MSIX lifecycle.
- Moved shell execution out of the management UI and into the command host.

### Security and reliability

- Hardened local path, reparse-point, icon parsing, `desktop.ini`, shortcut, and restore-state handling.
- Added guarded package lifecycle tooling that snapshots unrelated Explorer handlers and protects user icon/state data.
- Added managed, source-contract, native smoke, UI automation, package-signature, and artifact-integrity checks.

### Known limitations

- This is a release candidate signed with the public development certificate included in the release. Windows will not trust it until that certificate is explicitly imported.
- Clean uninstall, the remaining Explorer target matrix, and 200%/High Contrast accessibility proof are not yet complete.
- The repository has no source-code license yet; downloading the application does not grant permission to reuse or redistribute its source.

[Unreleased]: https://github.com/gvastethecreator/icon-replacer/compare/v1.0.0-rc.1...HEAD
[1.0.0-rc.1]: https://github.com/gvastethecreator/icon-replacer/releases/tag/v1.0.0-rc.1
