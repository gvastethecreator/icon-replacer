# Icon Replacer Microsoft Store release evidence

Copy this file for each submission. Keep credentials, Partner Center session data, private certificate material, proprietary icons, and sensitive file-system screenshots outside the repository.

## Identity

| Field | Value |
| --- | --- |
| Product | Icon Replacer |
| Store ID | `[value]` |
| Package identity name | `[value]` |
| Publisher | `[value]` |
| Publisher display name | `[value]` |
| PFN | `[verification value]` |
| Package SID | `[verification value]` |
| Package version | `[0.0.0.0]` |
| Architecture | `x64` |
| Source commit | `[SHA]` |
| Source tag | `[tag/n-a]` |
| Build date UTC | `[timestamp]` |

## Artifact

| Field | Value |
| --- | --- |
| Store package | `[name.msix]` |
| Size | `[bytes]` |
| SHA-256 | `[hash]` |
| Target device family | `Windows.Desktop` |
| Minimum OS | `10.0.17763.0` |
| Application ID | `IconReplacer.App` |
| Restricted capability | `runFullTrust` |
| Distribution channel | `store` |
| Native shell DLL | `IconReplacer.ShellExtension.dll` |
| Command host | `IconReplacer.CommandHost.exe` |
| Build command/workflow | `[reference]` |

## Automated gates

| Check | Command/run | Result | Evidence |
| --- | --- | --- | --- |
| Store structure | `.\scripts\Test-StoreReadiness.ps1 -RequireReservedIdentity` | `[pass/fail]` | |
| Managed restore | `dotnet restore ... --locked-mode` | `[pass/fail]` | |
| Managed tests | `dotnet test tests\IconReplacer.Core.Tests\IconReplacer.Core.Tests.csproj --configuration Release --no-restore` | `[pass/fail]` | |
| App/command host/native build | `.\scripts\Build-StoreMsix.ps1` | `[pass/fail]` | |
| Package contents | Store builder checks | `[pass/fail]` | |
| Package signature | `Get-AuthenticodeSignature` / builder | `[pass/fail]` | |
| Store update policy | network/test evidence | `[pass/fail]` | |

## Test environments

Record both Windows versions when available.

| Field | Windows 10 | Windows 11 |
| --- | --- | --- |
| Edition/version/build | `[value]` | `[value]` |
| Architecture | `x64` | `x64` |
| Clean VM/profile | `[yes/no]` | `[yes/no]` |
| Developer tools installed | `[yes/no]` | `[yes/no]` |
| Account type | `[standard/admin]` | `[standard/admin]` |
| Explorer version | `[value]` | `[value]` |

## Test fixture

| Item | Value |
| --- | --- |
| Test root | `[synthetic user-owned path]` |
| Folder targets | `[names]` |
| Shortcut target | `[synthetic target]` |
| `.ico` source | `[owned/licensed test icon]` |
| Original folder/shortcut state hashes or metadata | `[evidence]` |
| Icon library pre-state | `[summary]` |
| Restore-history pre-state | `[summary]` |

## Application and library qualification

| Scenario | Result | Evidence/notes |
| --- | --- | --- |
| Clean install | `[pass/fail]` | |
| Start-menu launch | `[pass/fail]` | |
| Empty library state | `[pass/fail]` | |
| Import valid `.ico` | `[pass/fail]` | |
| Reject malformed `.ico` | `[pass/fail]` | |
| Search/filter | `[pass/fail]` | |
| Create/use collection | `[pass/fail]` | |
| Preview icon | `[pass/fail]` | |
| Recent/restore UI | `[pass/fail]` | |
| Settings/appearance | `[pass/fail]` | |
| Standard-user operation | `[pass/fail]` | |
| Offline operation | `[pass/fail]` | |

## Explorer integration matrix

| Scenario | Windows 10 | Windows 11 | Evidence/notes |
| --- | --- | --- | --- |
| Classic menu on folder | `[pass/fail]` | `[pass/fail]` | |
| Classic menu on `.lnk` | `[pass/fail]` | `[pass/fail]` | |
| Modern menu on folder | `n/a` | `[pass/fail]` | |
| Modern menu on `.lnk` | `n/a` | `[pass/fail]` | |
| Change-icon command activation | `[pass/fail]` | `[pass/fail]` | |
| Collections command activation | `[pass/fail]` | `[pass/fail]` | |
| Classic handler activation | `[pass/fail]` | `[pass/fail]` | |
| Explorer remains responsive | `[pass/fail]` | `[pass/fail]` | |
| Large library menu remains bounded | `[pass/fail]` | `[pass/fail]` | |
| Explorer restart recovery | `[pass/fail]` | `[pass/fail]` | |

## Mutation and restore matrix

| Scenario | Result | Evidence/notes |
| --- | --- | --- |
| Apply folder icon | `[pass/fail]` | |
| Apply shortcut icon | `[pass/fail]` | |
| Restore record created before mutation | `[pass/fail]` | |
| Preserve unrelated `desktop.ini` data | `[pass/fail]` | |
| Required file attributes set correctly | `[pass/fail]` | |
| Explorer refresh notification | `[pass/fail]` | |
| Restore folder icon | `[pass/fail]` | |
| Restore shortcut icon | `[pass/fail]` | |
| Original target/source files unchanged | `[pass/fail]` | |
| Missing icon recovery feedback | `[pass/fail]` | |
| Protected path safe failure | `[pass/fail]` | |
| Reparse/untrusted target safe failure | `[pass/fail]` | |
| No automatic elevation | `[pass/fail]` | |

## Update qualification

Run once with Explorer already having invoked the extension and once after restarting Explorer.

| Scenario | Result | Evidence/notes |
| --- | --- | --- |
| Install previous Store version | `[pass/fail/n-a]` | |
| Invoke modern/classic extension | `[pass/fail/n-a]` | |
| Update with Explorer running | `[pass/fail/n-a]` | |
| Update after Explorer restart | `[pass/fail/n-a]` | |
| New package COM registration active | `[pass/fail/n-a]` | |
| Old registration replaced cleanly | `[pass/fail/n-a]` | |
| Library preserved | `[pass/fail/n-a]` | |
| Restore history preserved | `[pass/fail/n-a]` | |
| Existing applied icon references remain valid | `[pass/fail/n-a]` | |
| Explorer remains stable | `[pass/fail/n-a]` | |

## Uninstall and reinstall qualification

| Scenario | Result | Evidence/notes |
| --- | --- | --- |
| Uninstall with Explorer running | `[pass/fail]` | |
| Uninstall after Explorer restart | `[pass/fail]` | |
| Modern menu removed | `[pass/fail]` | |
| Classic menu removed | `[pass/fail]` | |
| Packaged COM activation removed | `[pass/fail]` | |
| Explorer remains stable | `[pass/fail]` | |
| Original folders/shortcuts remain | `[pass/fail]` | |
| Applied metadata not corrupted | `[pass/fail]` | |
| `%USERPROFILE%\.icons` follows preservation policy | `[pass/fail]` | |
| Restore history follows preservation policy | `[pass/fail]` | |
| Reinstall succeeds | `[pass/fail]` | |
| Reinstalled menus activate correct package | `[pass/fail]` | |

## Store update-channel proof

- Build property/environment: `ICON_REPLACER_DISTRIBUTION_CHANNEL=store`
- Compiled `DistributionChannelPolicy.Current`: `[store required]`
- Manual update action message: `[Store-managed message]`
- GitHub Releases HTTP request during check: `[no required]`
- Network-monitor or mock-handler evidence: `[reference]`

Do not infer network behavior only from visible UI text.

## Privacy/security review

- [ ] Public privacy-policy URL loads without authentication.
- [ ] Policy matches `.ico`, folder, `.lnk`, `desktop.ini`, library, restore, Explorer, and update behavior.
- [ ] Test paths and icons are synthetic and non-sensitive.
- [ ] Screenshots contain no username, personal path, customer data, cloud-drive names, proprietary icons, or recent files.
- [ ] Logs and restore evidence are redacted.
- [ ] No `.pfx`, private key, certificate password, development certificate, or Partner Center secret is present in the final Store bundle.
- [ ] Untrusted files are not automatically unblocked.
- [ ] Normal use does not auto-elevate.
- [ ] Explorer work remains bounded and non-blocking.

## Package contract evidence

- COM AppId: `B8F1A86D-4C52-4C53-BF72-30B59F7F0F7D`
- Change command CLSID: `B8F1A86D-4C52-4C53-BF72-30B59F7F0F7D`
- Collections CLSID: `B8F1A86D-4C52-4C53-BF72-30B59F7F0F7E`
- Classic handler CLSID: `B8F1A86D-4C52-4C53-BF72-30B59F7F0F7F`
- COM threading model: `STA`
- Modern targets: `Directory`, `.lnk`
- Classic targets: `Directory`, `.lnk`
- Native DLL hash: `[SHA-256]`
- Command host hash: `[SHA-256]`

## Partner Center

### Pricing and availability

- Markets: `[selection]`
- Audience: `[selection]`
- Discoverability: `[selection]`
- Schedule: `[selection]`
- Base price: `[selection]`
- Publishing hold: `[selection]`

### Properties

- Category/subcategory: `[selection]`
- Privacy URL: `[url]`
- Website: `[url]`
- Support: `[url/email]`
- Contact information complete: `[yes/no]`
- x64/Windows requirements complete: `[yes/no]`

### Age ratings

- Questionnaire complete: `[yes/no]`
- User-imported artwork answer reviewed: `[yes/no]`
- Assigned rating: `[value]`

### Packages

- Upload validation: `[result]`
- Packages section complete: `[yes/no]`
- Device family: `Windows Desktop only`
- Architecture: `x64`
- Warnings: `[none/list]`

### Store listings

- English listing reviewed: `[yes/no]`
- Spanish listing reviewed: `[yes/no]`
- What's new updated: `[yes/no]`
- Screenshot count: `[number]`
- Icon/artwork rights verified: `[yes/no]`

### Submission options

- Notes date: `[date]`
- Explorer/COM test instructions entered: `[yes/no]`
- `runFullTrust` explanation entered: `[yes/no]`
- Uninstall/update considerations entered: `[yes/no]`
- Notification audience reviewed: `[yes/no]`
- Publishing hold confirmed: `[yes/no]`

## Certification outcome

| Field | Value |
| --- | --- |
| Submitted | `[timestamp]` |
| Result | `[passed/failed/cancelled]` |
| Findings | `[summary]` |
| Remediation | `[commit/submission]` |
| Approved | `[timestamp]` |
| Published/held | `[value]` |
| Live Store URL | `[url]` |
| Deep link | `[value]` |

## Approval

- Engineering: `[name/date]`
- Explorer/shell qualification: `[name/date]`
- Product/listing: `[name/date]`
- Privacy/security: `[name/date]`
- Publisher owner: `[name/date]`
- Source-license posture reviewed: `[name/date]`
- Decision: `[publish/hold/reject]`
