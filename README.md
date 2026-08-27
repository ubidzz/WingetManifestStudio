# Winget Manifest Studio

[![Build and test](https://github.com/ubidzz/WingetManifestStudio/actions/workflows/quality.yml/badge.svg)](https://github.com/ubidzz/WingetManifestStudio/actions/workflows/quality.yml)
[![CodeQL](https://github.com/ubidzz/WingetManifestStudio/actions/workflows/codeql.yml/badge.svg)](https://github.com/ubidzz/WingetManifestStudio/actions/workflows/codeql.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-20d4cb.svg)](LICENSE)

Winget Manifest Studio is a Windows desktop application for creating, updating, validating, testing, and submitting Windows Package Manager manifests without editing YAML by hand.

It is designed for first-time package publishers while retaining the controls experienced maintainers need. Microsoft WingetCreate remains responsible for official authentication and submission.

## Highlights

- Create a new three-file Winget manifest project.
- Load and safely update an existing manifest folder.
- Preserve parsed custom and unsupported YAML fields.
- Calculate SHA-256 hashes from local release files.
- Read supported MSI ProductCode and UpgradeCode values.
- Inspect Authenticode and MSIX/APPX signature information.
- Verify that public download URLs match attached local files.
- Preview changes without writing files.
- Create timestamped backups before replacing manifests.
- Validate with the official Winget command.
- Test a manifest installation locally or in Windows Sandbox.
- Confirm the installed package and version.
- Submit through Microsoft's official WingetCreate workflow.
- Keep GitHub authentication in WingetCreate and Windows Credential Manager.

## Requirements

For normal use:

- Windows 10 or Windows 11, x64.
- Microsoft .NET 10 Desktop Runtime x64 when using the framework-dependent EXE or MSI.
- Windows App Installer, which provides the `winget` command.
- A public HTTPS address for every installer that Winget will download.

WingetCreate is only required for its official commands and submission workflow. Windows Sandbox is optional and must already be enabled in Windows before using the Sandbox test.

## Install

Download `StudioSetup.msi` from the repository's [Releases](https://github.com/ubidzz/WingetManifestStudio/releases) page and run it normally. Administrator permission is not required to edit manifests. Windows may request approval only for operations that inherently require elevation, such as enabling Winget local-manifest testing or running an installer that requires it.

The published setup is intentionally framework-dependent and does **not** bundle the .NET 10 framework. Install the Microsoft .NET 10 Desktop Runtime x64 if Windows reports that the runtime is missing.

## Guided Workflow

The main navigation follows the required order:

1. **Start**
2. **Package**
3. **Installers**
4. **Review**
5. **Test Center**

Help and official WingetCreate commands remain available separately and are not extra required steps.

### 1. Start

Choose one of these paths:

- **Create a new project** selects an output folder for a new manifest set.
- **Load existing manifests** reads the YAML files in an existing package folder.

Loading and previewing do not modify the selected manifests.

### 2. Package

Complete the required package identity and public information. A package identifier normally uses `Publisher.ApplicationName`, contains a dot, and stays unchanged between releases. Enter versions without a leading `v`.

Every field includes beginner guidance. Uncommon schema fields are available under the optional sections and may be left blank when they do not apply.

### 3. Installers

Follow the four numbered actions shown across the top:

1. **Add Release Files** — select the exact local MSI, EXE, MSIX, APPX, bundle, ZIP, portable package, or other supported release file.
2. **Enter Public URL** — paste the direct public HTTPS download link into the selected installer row.
3. **Inspect & Fill Selected** — calculate the hash and read supported installer metadata.
4. **Verify Public URLs** — download each published file temporarily and prove that it matches the attached local file and SHA-256.

Use one installer row for each architecture or installer variation. A release webpage is not an installer URL; use the direct release-asset URL.

### 4. Review

Review uses the same guided design as Test Center. A four-step progress tracker and one highlighted action lead through:

1. **Preview** — builds the proposed YAML in memory.
2. **Save safely** — writes the reviewed manifests after backing up existing files.
3. **Validate** — runs Microsoft's Winget validator against a clean temporary copy.
4. **Test & submit** — continues to Test Center for installation testing and submission.

The plain-language review is shown by default. Technical YAML remains behind **Show technical YAML** and is available whenever exact output or a complete validator error is needed.

### 5. Test Center

Test Center presents one required action at a time:

1. **Safe preflight** checks generated YAML, hashes, signatures, official validation, and whether the package identifier already exists. It does not install anything.
2. **Allow local testing** enables Winget's `LocalManifestFiles` setting after one Windows administrator approval.
3. **Test install** runs the exact generated manifest through:

   ```text
   winget install --manifest <folder>
   ```

4. **Verify result** checks the Winget package ID, then the MSI identity or installed application name when necessary.

After all four checks pass, the highlighted action becomes **Submit to Winget**. Submission stays in Test Center; there is no need to return to Review.

Optional diagnostics—including setup checks, signature inspection, existing-package search, Sandbox testing, and report export—remain collapsed until requested.

## Updating an Existing Package

1. Load the folder containing the existing Winget YAML files.
2. Confirm the package identifier and enter the new version.
3. Attach the new local release files.
4. Replace each public URL with the matching new release-asset URL.
5. Inspect the files and verify the public URLs.
6. Preview the proposed changes.
7. Save, validate, and complete Test Center.
8. Submit through WingetCreate.

Unsupported parsed fields are preserved structurally. The original files are also copied to a timestamped `.manifest-backups` folder before any replacement.

## YAML Preservation

Existing manifests are parsed as YAML document trees. The update process preserves parsed root fields, nested mappings, sequences, additional locale documents, uncommon installer values, aliases, anchors, and custom schema fields that the guided interface does not directly expose.

Installer rows are matched using stable values such as ProductCode, URL, architecture, installer type, and scope instead of relying only on row position.

There are two intentional limitations:

- Comments and hand-formatted spacing are not schema data and may be normalized when edited YAML is emitted.
- Invalid YAML that cannot be parsed cannot receive structural preservation. The original source remains untouched unless a validated save succeeds, and backups preserve its exact text.

## Safety and Privacy

- Loading and previewing never change manifest files.
- Existing manifests are backed up before replacement.
- Installer inspection and hashing happen locally.
- Public URL verification downloads to a temporary location and does not replace the attached file.
- Installation and Sandbox tests require an explicit user action.
- Interactive commands open a persistent console so questions and errors remain visible.
- GitHub tokens are owned by WingetCreate and Windows Credential Manager.
- Tokens are never stored in manifests, project profiles, logs, or repository files.
- The application does not require administrator permission for ordinary editing.

## Supported Installer Formats

Guided support includes MSI, WiX, EXE, Burn, Inno Setup, Nullsoft, MSIX, APPX, bundles, ZIP, portable packages, and fonts. Advanced mappings provide access to uncommon Winget installer fields when a package needs them.

## Interface Languages

The Help & Guide page includes English and Spanish interface resources. The language choice is stored in the current Windows user's local application settings and is not written into package profiles.

## Build from Source

Requirements:

- Windows
- .NET 10 SDK
- Visual Studio with Windows Forms support, or the `dotnet` command line
- WiX Toolset SDK dependencies restored by the installer project

Build the application:

```powershell
dotnet restore WingetManifestStudio.slnx
dotnet build ManifestUpdater/WingetManifestStudio.csproj -c Release
```

Run the automated functional and off-screen interface tests without opening visible test windows:

```powershell
./ManifestUpdater/bin/Release/net10.0-windows/WingetManifestStudio.exe --self-test
./ManifestUpdater/bin/Release/net10.0-windows/WingetManifestStudio.exe --ui-self-test
./ManifestUpdater/bin/Release/net10.0-windows/WingetManifestStudio.exe --startup-probe
```

## Automated Repository Checks

Every push and pull request to `master` runs a clean Windows build followed by the functional self-test, off-screen interface test, and startup probe. The startup check rejects a first-window time above 15 seconds so major launch-time regressions cannot pass unnoticed. Test reports and off-screen screenshots are retained with each workflow run.

Repository security automation also includes:

- CodeQL C# analysis on pushes, pull requests, a weekly schedule, and manual runs.
- NuGet auditing during every build, with known vulnerability warnings treated as errors.
- Dependency Review on pull requests, rejecting newly introduced moderate-or-higher vulnerabilities.
- Weekly Dependabot updates for NuGet packages and GitHub Actions.

These checks build the application only. They do not change or invoke the Visual Studio publish profiles and do not create an MSI during CI.

## Publish

Visual Studio Publish and normal `dotnet publish` runs automatically build:

- `WingetManifestStudio.exe`
- `StudioSetup.msi`

The MSI is always named `StudioSetup.msi`. It contains the framework-dependent application payload and requires the Microsoft .NET 10 Desktop Runtime x64; it does not package the framework itself.

Example framework-dependent single-file publish:

```powershell
dotnet publish ManifestUpdater/WingetManifestStudio.csproj `
  -c Release `
  -r win-x64 `
  --self-contained false `
  -p:PublishSingleFile=true `
  -p:PublishReadyToRun=true
```

### Digital Signing

Publishing signs the EXE and MSI when a code-signing certificate with a private key is available in the current user's Windows certificate store. Supply only its thumbprint to the publish process:

```powershell
$env:WMS_SIGNING_CERTIFICATE_THUMBPRINT = '<code-signing-certificate-thumbprint>'
dotnet publish ManifestUpdater/WingetManifestStudio.csproj -p:PublishProfile=FolderProfile
```

The certificate and private key are never stored in this repository. Without a certificate, publishing succeeds but reports that the artifacts are unsigned.

## Project Structure

| Folder | Purpose |
| --- | --- |
| `Application` | Startup and crash reporting |
| `Assets` | Application icons and images |
| `Models` | Manifest and installer data |
| `Services` | YAML, Winget, hashing, inspection, profiles, testing, and repository services |
| `UI` | WinForms form, Designer resources, custom controls, and localization |
| `Testing` | Functional and off-screen UI test runners |
| `Packaging/MSI` | WiX installer project |
| `Properties/PublishProfiles` | Visual Studio publish profiles |

## Community

- Read [CONTRIBUTING.md](CONTRIBUTING.md) before proposing a change.
- Participation is governed by the [Community Code of Conduct](CODE_OF_CONDUCT.md).
- Report vulnerabilities through the process in [SECURITY.md](SECURITY.md), not through a public issue.

## License

Winget Manifest Studio is available under the [MIT License](LICENSE).

## Microsoft Trademark and Affiliation Notice

Winget Manifest Studio is an independent community project. It is not affiliated with, endorsed by, or supported by Microsoft. Windows, Winget, Windows Package Manager, GitHub, and WingetCreate may be trademarks of their respective owners.
