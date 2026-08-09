# Release Downloads

## Current Candidate

GitHub prerelease: `v1.0.0-rc.1`  
MSIX package version: `1.0.0.6`  
Architecture: Windows x64

This candidate is intended for evaluation. It is signed with `CN=IconReplacerDev`, not with a production-trusted publisher certificate. Installing it requires explicitly trusting the public certificate shipped beside the MSIX.

## Download And Verify

Download these three assets from the GitHub release:

- `IconReplacer_1.0.0.6_x64.msix`
- `IconReplacer.Dev.cer`
- `SHA256SUMS.txt`

From PowerShell in the download directory, verify both binary hashes:

```powershell
$expected = @{}
Get-Content .\SHA256SUMS.txt | ForEach-Object {
    if ($_ -match '^([A-Fa-f0-9]{64})  (.+)$') { $expected[$Matches[2]] = $Matches[1] }
}

'IconReplacer_1.0.0.6_x64.msix', 'IconReplacer.Dev.cer' | ForEach-Object {
    $actual = (Get-FileHash -Algorithm SHA256 -LiteralPath $_).Hash
    if ($actual -ne $expected[$_]) { throw "SHA-256 mismatch: $_" }
    "Verified: $_"
}
```

Inspect the package signature before trusting the certificate:

```powershell
Get-AuthenticodeSignature .\IconReplacer_1.0.0.6_x64.msix |
    Select-Object Status, @{n='Signer';e={$_.SignerCertificate.Subject}},
        @{n='Thumbprint';e={$_.SignerCertificate.Thumbprint}}
```

The status must be `Valid`; the signer must be `CN=IconReplacerDev`; and the certificate thumbprint must be `F235B1142A11E383C0673599772334C0F0797F4A`.

## Trust And Install

Open an elevated PowerShell session in the download directory:

```powershell
Import-Certificate `
    -FilePath .\IconReplacer.Dev.cer `
    -CertStoreLocation Cert:\LocalMachine\TrustedPeople

Add-AppxPackage .\IconReplacer_1.0.0.6_x64.msix
```

Importing a certificate into `TrustedPeople` is a security-sensitive action. Only do this after verifying the SHA-256 hashes, signature subject, thumbprint, repository owner, and release URL.

The package registers Icon Replacer commands in File Explorer. Explorer may need to be restarted before the commands appear.

## Uninstall

Remove Icon Replacer from **Settings > Apps > Installed apps**, or run:

```powershell
Get-AppxPackage -Name IconReplacer | Remove-AppxPackage
```

After uninstalling the candidate, remove the development certificate if no other installed Icon Replacer candidate uses it:

```powershell
Get-ChildItem Cert:\LocalMachine\TrustedPeople |
    Where-Object Subject -eq 'CN=IconReplacerDev' |
    Remove-Item
```

The product policy preserves the user's `%USERPROFILE%\.icons` library and restore history by default. The stable release remains blocked until clean-uninstall preservation and Explorer cleanup are captured end to end.

## Maintainer Build

Create a clean release bundle from the manifest version:

```powershell
$env:ICON_REPLACER_CERT_PASSWORD = '<local-secret>'
.\scripts\New-ReleaseBundle.ps1 -ReleaseTag v1.0.0-rc.1
```

Generated output is written below `artifacts\release\v1.0.0-rc.1`. The bundle intentionally excludes the PFX/private key.
