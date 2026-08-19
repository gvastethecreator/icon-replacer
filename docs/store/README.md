# Icon Replacer Microsoft Store submission runbook

This directory defines the release contract for publishing Icon Replacer as a packaged x64 desktop application with File Explorer integration.

Icon Replacer is not a normal window-only MSIX. The package includes:

```text
WinUI management application
IconReplacer.CommandHost.exe
IconReplacer.ShellExtension.dll
packaged COM server
Windows 11 File Explorer commands
classic File Explorer context-menu handler
local icon engine and restore history
```

Certification must therefore cover both the app and Explorer lifecycle.

## Distribution channels

```text
Microsoft Store
  -> exact Partner Center identity
  -> x64 raw MSIX
  -> ICON_REPLACER_DISTRIBUTION_CHANNEL=store
  -> Store-managed package updates
  -> no GitHub Releases request from the update service

Direct/GitHub release candidate
  -> development or publisher-owned identity
  -> development/public Authenticode policy
  -> explicit certificate/install procedure where applicable
  -> GitHub Releases update check
```

The current direct release candidate uses a development certificate and requires explicit trust. It must not be presented as the stable Store package.

Partner Center accepts raw `.msix` packages. Icon Replacer already has a deterministic WinApp/MSIX builder, so the Store path reuses the real package rather than manufacturing an upload wrapper.

## Current identity status

The checked-in package still uses a development identity:

```text
Name:      IconReplacer
Publisher: CN=IconReplacerDev
Display:   Icon Replacer
Version:   1.0.0.6
```

Machine-readable Store state lives in:

```text
docs/store/store-identity.json
```

Reserve the product in Partner Center, then apply the exact values:

```powershell
.\scripts\Set-StoreIdentity.ps1 `
  -Name '<Package/Identity/Name>' `
  -Publisher '<Package/Identity/Publisher>' `
  -PublisherDisplayName '<Package/Properties/PublisherDisplayName>' `
  -StoreId '<Store ID>' `
  -PackageFamilyName '<PFN>' `
  -PackageSid '<Package SID>'
```

The script rejects surrounding and non-breaking whitespace. PFN and Package SID are verification metadata only; they are not manifest fields.

## First Store architecture

The first Store release is intentionally **x64 only**.

Although some managed project files list additional platforms, the native Explorer extension, command host, package layout, lifecycle tests, and current release tooling are qualified for x64. Do not advertise or upload ARM64 until all of these exist for ARM64:

- native shell-extension DLL;
- matching command host and app payload;
- architecture-correct COM registration;
- package and lifecycle tests;
- Explorer modern/classic menu verification;
- upgrade and uninstall evidence.

## Desktop-only target

The package manifest targets only:

```xml
<TargetDeviceFamily Name="Windows.Desktop" ... />
```

Do not restore `Windows.Universal`. Icon Replacer depends on desktop shell, COM, file-system, shortcut, and Explorer behavior that is unavailable on non-desktop device families.

## Store update policy

`Directory.Build.props` defines `ICON_REPLACER_STORE` when:

```text
ICON_REPLACER_DISTRIBUTION_CHANNEL=store
```

`DistributionChannelPolicy` exposes the compiled channel. In Store builds, `GitHubReleaseUpdateService.CheckAsync` returns immediately with a Microsoft Store-managed message and does not create or send the GitHub HTTP request.

Direct builds keep the existing bounded GitHub Releases request.

Release evidence must prove the Store package was compiled with the Store channel and made no GitHub update request during manual update checking.

## Commands

Structural validation, allowed while reservation is pending:

```powershell
.\scripts\Test-StoreReadiness.ps1
```

Strict validation:

```powershell
.\scripts\Test-StoreReadiness.ps1 -RequireReservedIdentity
```

Build the x64 Store candidate after reservation:

```powershell
.\scripts\Build-StoreMsix.ps1
```

The final review bundle is written below:

```text
artifacts\store\x64
```

The builder runs the managed tests first under the direct/default compile policy, then compiles the package with the Store symbol, creates a short-lived matching certificate for package construction, verifies package contents/signature, copies only submission-safe files, and deletes temporary private signing material.

Microsoft Store replaces the MSIX signature after certification. The temporary package certificate is not a public code-signing credential.

## Partner Center submission

### 1. Pricing and availability

Complete every field intentionally:

- Markets: select only markets where listing/support obligations can be maintained.
- Audience: use Public only after the shell lifecycle matrix passes.
- Discoverability: searchable or direct-link-only by explicit decision.
- Schedule: use a manual publishing hold for the first release.
- Base price: Free unless a separate commercial plan is approved.

The publishing hold is especially useful for an Explorer extension because the final Store-delivered package should be installed and reviewed before broad exposure.

### 2. Properties

Recommended first-release values:

- Primary category: Personalization or Utilities & tools, depending on the current taxonomy.
- Privacy policy URL: stable public rendering of [`../../PRIVACY.md`](../../PRIVACY.md).
- Website: project site.
- Support: issue tracker, support page, or monitored email.
- Minimum OS: Windows 10 version 1809.
- Architecture: x64.
- Windows 11 recommended for the modern Explorer menu; Windows 10 uses classic integration.
- XR/immersive options: disabled.

Icon Replacer reads local paths, `.ico`, `.lnk`, `desktop.ini`, restore records, and Explorer selection data. Publish a privacy policy even though data stays local.

### 3. Age ratings

Complete every questionnaire item. Icon Replacer does not supply violence, sexual content, gambling, public user-generated content, or unrestricted web browsing.

Users can import arbitrary local icon artwork. That content is selected by the user, remains local, and is not uploaded or distributed by Icon Replacer. Answer any user-selected-content question accurately rather than claiming the product can never display third-party artwork.

### 4. Packages

Upload only the reviewed x64 Store `.msix`.

Before upload, verify:

- exact `Name`, `Publisher`, and `PublisherDisplayName` from Partner Center;
- `Windows.Desktop` is the only target family;
- version is greater than every previous applicable Store package;
- architecture is x64;
- `runFullTrust` is declared and explained;
- package contains the WinUI app, command host, native shell DLL, assets, manifest, block map, and signature;
- packaged COM classes match the context-menu CLSIDs;
- both modern and classic context-menu declarations reference the expected targets;
- no `.pfx`, private key, password, dev certificate, build staging tree, test fixture, or direct release bundle is uploaded;
- the build evidence identifies the exact source commit and compiled distribution channel.

Partner Center can show a file as Validated while the Packages section remains Incomplete. Finish every device-family and package control.

### 5. Store listings

Use [`LISTING.md`](LISTING.md) for English and Spanish copy.

Prepare at least five current screenshots:

1. icon library;
2. filtered library/search;
3. context menu on a test folder;
4. change-icon preview/confirmation;
5. Recent/restore workflow;
6. optional Settings/About view.

Use only synthetic test folders, shortcuts, paths, and icon artwork licensed for promotion. Do not expose a normal user profile or customer file system.

### 6. Submission options

Use [`CERTIFICATION-NOTES.md`](CERTIFICATION-NOTES.md).

The `runFullTrust` explanation must describe:

- local folder and `.lnk` access selected by the user;
- `.ico` validation/import;
- `desktop.ini` and file-attribute operations;
- Windows shortcut COM APIs;
- packaged COM server and Explorer context menus;
- local restore records;
- Explorer refresh notifications;
- command-host delegation;
- no silent elevation or security-policy bypass.

Certification notes must provide a disposable test folder, shortcut, and `.ico` workflow; explain modern versus classic menus; explain restore behavior; and warn that Explorer may need a short refresh/restart after package changes.

## Explorer integration contract

The manifest registers three COM classes:

| Purpose | CLSID |
| --- | --- |
| Change icon command | `B8F1A86D-4C52-4C53-BF72-30B59F7F0F7D` |
| Collections command | `B8F1A86D-4C52-4C53-BF72-30B59F7F0F7E` |
| Classic context menu | `B8F1A86D-4C52-4C53-BF72-30B59F7F0F7F` |

Targets:

- `Directory`
- `.lnk`

These identifiers are persistent package contracts. Do not change them after public release without a migration and compatibility plan.

## Required lifecycle matrix

Test on Windows 10 and Windows 11 where available.

| Scenario | Windows 10 | Windows 11 |
| --- | --- | --- |
| Clean package install | Required | Required |
| Start-menu launch | Required | Required |
| Classic context menu on folder | Required | Required |
| Classic context menu on `.lnk` | Required | Required |
| Modern context menu on folder | N/A | Required |
| Modern context menu on `.lnk` | N/A | Required |
| Import valid `.ico` | Required | Required |
| Reject malformed/unsupported icon | Required | Required |
| Apply folder icon | Required | Required |
| Apply shortcut icon | Required | Required |
| Preserve unrelated `desktop.ini` data | Required | Required |
| Create restore record before change | Required | Required |
| Restore folder icon | Required | Required |
| Restore shortcut icon | Required | Required |
| Protected path fails without partial mutation | Required | Required |
| Reparse/untrusted target policy | Required | Required |
| Large icon library/menu cap | Required | Required |
| Explorer restart recovery | Required | Required |
| Package update with Explorer open | Required | Required |
| Package update after Explorer restart | Required | Required |
| Uninstall with Explorer open | Required | Required |
| COM/menu cleanup after uninstall | Required | Required |
| `%USERPROFILE%\.icons` preservation | Required | Required |
| Reinstall after uninstall | Required | Required |
| Standard-user operation | Required | Required |
| Store update check makes no GitHub request | Required | Required |

## Update qualification

An update must prove:

- package identity is unchanged;
- version increases;
- Explorer unload/reload behavior is safe;
- old COM registration is replaced cleanly;
- context menus resolve to the new package;
- library and restore history survive;
- no stale DLL remains loaded after the expected Explorer lifecycle;
- rollback/failure does not leave Explorer unusable.

## Uninstall qualification

Uninstall must prove:

- package and COM registration are removed;
- modern/classic menu entries disappear;
- Explorer remains stable;
- command host is no longer invokable through package activation;
- original folders and shortcuts remain present;
- applied icon metadata is not corrupted;
- `%USERPROFILE%\.icons` and restore history follow the documented preservation policy;
- reinstall succeeds without stale registration.

## Privacy and security review

Confirm every release matches [`../../PRIVACY.md`](../../PRIVACY.md):

- selections are processed only for an explicit Icon Replacer command;
- imported icons and paths stay local;
- restore history stays local;
- Store builds do not call GitHub for updates;
- direct update checks retrieve release metadata only;
- no automatic elevation;
- untrusted `desktop.ini` is not silently unblocked;
- logs/screenshots are reviewed for paths and proprietary artwork.

## Source-code license decision

The repository currently states that no source-code license has been selected. Microsoft Store publication does not itself require an open-source license, but the public repository must not imply permissions that have not been granted.

Review [`LICENSE-DECISION.md`](LICENSE-DECISION.md) before the stable public release and choose deliberately between an open-source license, source-available terms, or an explicit all-rights-reserved notice.

## Direct-channel certificate policy

The current RC uses a development certificate and requires manual trust. That may remain an explicit evaluator channel, but it is not equivalent to trusted production distribution.

For stable direct distribution:

1. obtain a publisher-owned Authenticode signing solution;
2. sign all executable PE files, including the app, command host, and native shell DLL as appropriate;
3. timestamp with RFC 3161;
4. sign the final package/installer;
5. verify signatures and hashes;
6. stop instructing general users to trust a development root/certificate.

Store signing and direct signing are independent.

## Release gates

- [ ] Product reserved in Partner Center.
- [ ] Exact identity applied and strict validator passes.
- [ ] First Store architecture remains x64.
- [ ] Full managed tests pass under direct/default compile policy.
- [ ] Store package compiles with `ICON_REPLACER_DISTRIBUTION_CHANNEL=store`.
- [ ] Native shell extension and command host are present.
- [ ] Package/signature/content checks pass.
- [ ] Store update service makes no GitHub request.
- [ ] Windows 10/11 Explorer lifecycle matrix is evidenced.
- [ ] Update and uninstall with Explorer open/restarted are evidenced.
- [ ] Privacy URL is public and stable.
- [ ] English/Spanish listing copy is reviewed.
- [ ] Screenshots use synthetic paths/icons only.
- [ ] Age rating is complete.
- [ ] `runFullTrust` explanation is entered.
- [ ] Certification notes match the exact package.
- [ ] Source-code license posture is deliberate and documented.
- [ ] First submission uses an intentional publishing hold.

## First-submission sequence

1. Reserve Icon Replacer in Partner Center.
2. Run `Set-StoreIdentity.ps1`.
3. Set the final four-part package version.
4. Run `Test-StoreReadiness.ps1 -RequireReservedIdentity`.
5. Run all managed, native, UI, Explorer, and package lifecycle checks.
6. Run `Build-StoreMsix.ps1`.
7. Copy and complete [`RELEASE-EVIDENCE-TEMPLATE.md`](RELEASE-EVIDENCE-TEMPLATE.md).
8. Test on clean Windows 10 and Windows 11 profiles/VMs.
9. Complete all six Partner Center sections.
10. Upload only the reviewed x64 `.msix`.
11. Submit with a manual publishing hold.
12. Review certification findings and the Store-delivered package before publishing.

## Non-automatable account steps

The repository cannot safely choose or perform:

- Store reservation/identity;
- pricing, markets, audience, discoverability, or schedule;
- company contact details;
- age-rating answers;
- privacy/support URL ownership;
- screenshots and listing uploads;
- restricted-capability form submission;
- final certification or publishing decision;
- source-code licensing choice.

## Official references

- [Create an app submission for an MSIX app](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/create-app-submission)
- [Upload MSIX app packages](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/upload-app-packages)
- [Manage submission options](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/manage-submission-options)
- [App capability declarations](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/app-capability-declarations)
- [Package desktop shell extensions](https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-extensions)
- [Classic Explorer context-menu handler schema](https://learn.microsoft.com/en-us/uwp/schemas/appxpackage/uapmanifestschema/element-desktop9-fileexplorerclassiccontextmenuhandler)
- [Microsoft Store policies](https://learn.microsoft.com/en-us/windows/apps/publish/store-policies)
