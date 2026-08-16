# Maintenance 2026-08-14

## Scope

This pass refreshed supported dependencies, public documentation, CI supply-chain pins, GitHub metadata, real product screenshots, and the static GitHub Pages surface. It did not publish commits, replace the release candidate, select a source license, or close the remaining manual release gates.

## Dependencies

Updated direct packages and lock files:

| Package | Before | After |
| --- | ---: | ---: |
| `Microsoft.WindowsAppSDK` | 2.3.1 | 2.4.0 |
| `Microsoft.Windows.SDK.BuildTools.WinApp` | 0.5.0 | 0.6.0 |
| `Microsoft.NET.Test.Sdk` | 18.8.1 | 18.9.0 |

NuGet reported no known vulnerable packages after the refresh. The Windows App SDK 2.4 x64 runtime was installed from the signed package contained in the restored NuGet dependency so the updated Release build could be exercised locally.

## Public surfaces

- Reworked the README with an adaptive Shieldcn header, focused badges, current release language, sponsor links, and a 2×2 real product tour.
- Captured the WinUI Release build at 1346×853 and published optimized WebP assets.
- Used four temporary documentation icons generated in a restrained matte print style; the Icon Library was returned to its original empty state after capture.
- Added an English-only GitHub Pages landing page with a responsive editorial layout, no gradients or glow, and direct release/install paths.
- Added GitHub Sponsors and Ko-fi funding metadata.
- Updated the remote repository description, homepage, and topics.
- Configured GitHub Pages for Actions deployment. The first deployment remains pending until these local files are pushed.

## CI

- Pinned `actions/checkout`, `actions/setup-dotnet`, and all Pages actions to immutable commit SHAs.
- Added manual dispatch to CI.
- Changed managed restore to `--locked-mode` so dependency drift fails before tests.
- Kept the existing focused managed-test scope; the native/package Release build remains a separate local and release gate.

## Verification

- `dotnet test tests\IconReplacer.Core.Tests\IconReplacer.Core.Tests.csproj --configuration Release --no-restore`: 290 passed, 0 failed, 0 skipped.
- `BuildAndRun.ps1 ... -SkipRun /p:Configuration=Release /p:Platform=x64`: WinUI, command host, and native shell extension built with 0 warnings and 0 errors.
- Ruthless Designer runtime review: desktop 1440×900 and mobile 390×844 captured with no P0/P1/P2 findings, no overflow, no broken images, no contrast failures, and no runtime blockers. Two P3 comfort-target advisories remain for short navigation links.
- Code map: refreshed against the current branch and validated after the maintenance diff.

## Open gates

- No source license has been selected; the README keeps the existing no-license warning.
- Stable release status still requires clean-uninstall evidence, the remaining Explorer matrix, and outstanding accessibility proof.
- GitHub Pages will return 404 until the Pages workflow is pushed and completes successfully.
