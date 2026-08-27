# Winget Manifest Studio

[![Build and test](https://github.com/ubidzz/WingetManifestStudio/actions/workflows/quality.yml/badge.svg)](https://github.com/ubidzz/WingetManifestStudio/actions/workflows/quality.yml)
[![Repository checks](https://github.com/ubidzz/WingetManifestStudio/actions/workflows/repository-checks.yml/badge.svg)](https://github.com/ubidzz/WingetManifestStudio/actions/workflows/repository-checks.yml)
[![CodeQL](https://github.com/ubidzz/WingetManifestStudio/actions/workflows/codeql.yml/badge.svg)](https://github.com/ubidzz/WingetManifestStudio/actions/workflows/codeql.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-20d4cb.svg)](LICENSE)

Winget Manifest Studio is a Windows desktop application for creating, updating, validating, testing, and submitting Windows Package Manager manifests without editing YAML by hand.

It is designed for first-time package publishers while retaining the controls experienced maintainers need. Microsoft WingetCreate remains responsible for official authentication and submission.

## Highlights

- Create a new three-file Winget manifest project.
- Load and safely update an existing manifest folder.
- Import the newest manifest set for any exact Winget package ID into a separate working copy.
- Import public GitHub release metadata and supported release assets without hardcoded publishers or repositories.
- Preserve parsed custom and unsupported YAML fields.
- Calculate SHA-256 hashes from local release files.
- Read supported MSI ProductCode, UpgradeCode, architecture, and install-scope values.
- Discover supported installer files inside ZIP packages and generate `NestedInstallerFiles` entries.
- Treat each installer row independently, including mixed architectures, installer technologies, and scopes.
- Inspect Authenticode and MSIX/APPX signature information while supporting both signed and unsigned EXE/MSI installers.
- Identify common EXE technologies including Inno Setup, NSIS, WiX Burn, Squirrel, Velopack, InstallShield, Advanced Installer, and self-extracting archives.
- Verify that public download URLs match attached local files.
- Preview changes without writing files.
- Create timestamped backups before replacing manifests.
- Validate with the official Winget command.
- Test a manifest installation locally or in Windows Sandbox.
- Run an optional Windows Sandbox install-and-uninstall cycle that verifies the installed identity is removed.
- Confirm the installed package and version.
- Submit through Microsoft's official WingetCreate workflow.
- Keep GitHub authentication in WingetCreate and Windows Credential Manager.

## Requirements

For normal use:

- Windows 10 or Windows 11, x64.
- No separate .NET installation is required. Both release files include the Microsoft .NET 10 Windows Desktop runtime.
- Windows App Installer, which provides the `winget` command.
- A public HTTPS address for every installer that Winget will download.

WingetCreate is only required for its official commands and submission workflow. Windows Sandbox is optional and must already be enabled in Windows before using the Sandbox test.

## Install

Download `StudioSetup.msi` from the repository's [Releases](https://github.com/ubidzz/WingetManifestStudio/releases) page and run it normally. Administrator permission is not required to edit manifests. Windows may request approval only for operations that inherently require elevation, such as enabling Winget local-manifest testing or running an installer that requires it.

Both `WingetManifestStudio.exe` and `StudioSetup.msi` are self-contained x64 releases. They include the .NET 10 Windows Desktop runtime so a new user can open the Studio immediately without waiting for Windows to locate or install a shared runtime.

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
- **Import existing Winget package** accepts any exact `Publisher.Application` ID, downloads the newest manifest set from `microsoft/winget-pkgs`, and creates a new `PackageID\Version` working folder without overwriting existing files.
- **Import a GitHub release** accepts any public `github.com/owner/repository/releases/tag/...` or `/releases/latest` URL. It fills only blank metadata fields, lets the user choose the actual installer assets, and asks before downloading those selections temporarily for hashing and inspection.

Loading and previewing do not modify the selected manifests.

### 2. Package

Complete the required package identity and public information. A package identifier normally uses `Publisher.ApplicationName`, contains a dot, and stays unchanged between releases. Enter versions without a leading `v`.

Every field includes beginner guidance. Optional guided controls cover agreements, documentation links, package and Windows-feature dependencies, MSIX capabilities, market rules, expected return codes, unsupported Winget arguments, installed-file detection, and private-source authentication. The raw YAML boxes remain an escape hatch for fields that still have no guided control.

The schema control offers versions accepted by the Microsoft Winget community repository. New projects default to the currently recommended schema `1.12.0`; older compatible versions remain available when maintaining existing manifests. Projects created by an earlier Studio build with the invalid preview value `1.28.0` are corrected to `1.12.0` in memory before preview or saving.

### 3. Installers

Follow the four numbered actions shown across the top:

1. **Add Release Files** — select the exact local MSI, EXE, MSIX, APPX, bundle, ZIP, portable application, or font release file.
2. **Enter Public URL** — paste the direct public HTTPS download link into the selected installer row.
3. **Inspect & Fill Selected** — calculate the hash and read supported installer metadata.
4. **Verify Public URLs** — download each published file temporarily and prove that it matches the attached local file and SHA-256.

Use one installer row for each architecture or installer variation. The Studio does not assume that every package is x64 or that every row uses the same installer technology. A release webpage is not an installer URL; use the direct release-asset URL.

MSI files can provide the product name, publisher, version, ProductCode, UpgradeCode, architecture, and install scope. EXE analysis reports the detected installer technology and explains whether Winget has known switches or the publisher's documentation must be checked. EXE metadata is used only to fill empty package fields; inspection never replaces a package name, publisher, or release version you already entered. ZIP rows show the detected files inside the archive. Review the nested type and paths before saving, especially when a ZIP contains more than one executable.

Signed and unsigned are both completed inspection results. An unsigned EXE or MSI is allowed and shown as a warning so the maintainer can make an informed decision. MSIX and APPX packages are different: Microsoft's community repository requires their package signature, so a missing package signature remains a blocking preflight result.

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

The optional Sandbox tools provide two clearly separate choices:

- **Sandbox install only** runs Microsoft's official `SandboxTest.ps1` workflow.
- **Sandbox install + uninstall** installs the manifest, locates its Winget, Apps & Features, or MSIX identity, uninstalls it through Winget, and verifies that identity was removed. The cycle happens only inside the disposable Sandbox.

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

Review includes **Open backup folder** so the timestamped copies can be reached without finding the hidden folder manually. Starting another project, loading another project, or closing the Studio with unsaved edits displays a Save, Discard, or Cancel decision. This protection does not restore the removed recent-project or previous-session features.

## YAML Preservation

Existing manifests are parsed as YAML document trees. The update process preserves parsed root fields, nested mappings, sequences, additional locale documents, uncommon installer values, aliases, anchors, and custom schema fields that the guided interface does not directly expose.

Installer rows are matched using stable values such as ProductCode, URL, architecture, installer type, and scope instead of relying only on row position.

Optional root-level installer defaults remain optional. If an existing manifest stores type or scope on individual installer rows, loading and previewing do not invent a new root default. This allows mixed MSI/EXE/MSIX packages and different per-user or per-machine installers to remain structurally accurate.

There are two intentional limitations:

- Comments and hand-formatted spacing are not schema data and may be normalized when edited YAML is emitted.
- Invalid YAML that cannot be parsed cannot receive structural preservation. The original source remains untouched unless a validated save succeeds, and backups preserve its exact text.

## Safety and Privacy

- Loading and previewing never change manifest files.
- Existing manifests are backed up before replacement.
- Installer inspection and hashing happen locally.
- Public URL verification downloads to a temporary location and does not replace the attached file.
- Repository and GitHub imports run only after the user chooses them. Installer assets are never downloaded merely because the application opened.
- Installation and Sandbox tests require an explicit user action.
- Interactive commands open a persistent console so questions and errors remain visible.
- GitHub tokens are owned by WingetCreate and Windows Credential Manager.
- Tokens are never stored in manifests, project profiles, logs, or repository files.
- The application does not require administrator permission for ordinary editing.

## Supported Installer Formats

Guided authoring support includes MSI, WiX, EXE, Burn, Inno Setup, Nullsoft, MSIX, APPX, bundles, ZIP, PWA, portable packages, and fonts. Supported architectures are x86, x64, ARM, ARM64, and neutral. EXE inspection also recognizes Squirrel, Velopack, InstallShield, Advanced Installer, and 7-Zip self-extracting clues while keeping the Winget installer type editable. Guided uncommon fields cover most current nested schema structures; advanced mappings remain available for the rest.

Portable EXEs cannot always be distinguished safely from normal EXE installers, so the detected type remains editable. Font packages use the separate `fonts` root in microsoft/winget-pkgs and are subject to stricter submission availability. PWA support can vary by Winget client and repository policy. The Studio warns about these cases and leaves final validation and submission decisions to Microsoft's current Winget and WingetCreate tools.

## Package and Computer Independence

- No publisher, package name, version, manifest folder, release URL, or installer architecture is fixed in the authoring workflow.
- New projects start with package-specific installer defaults blank. Inspection fills each installer row from the selected file, and the user can correct metadata that cannot be determined safely.
- Profiles store project data but no credentials. When a profile is moved to another Windows account or computer, missing local files are reported and can be reattached without losing public URLs or saved metadata.
- Runtime files use Windows-provided application-data and temporary folders instead of a personal user path.
- Existing YAML controls the loaded project. Optional values that were absent are not silently added during preview.

## Interface Languages

The Start and Help pages both provide a visible interface-language setting for English and Spanish. Navigation, guided actions, Review, Test Center, primary field labels, and beginner instructions are translated; manifest data, package metadata, YAML, installer output, and official Winget output are never translated or modified. The choice is stored in the current Windows user's local application settings and is not written into package profiles.

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

An optional live importer test verifies the public Winget repository and GitHub release APIs. It requires internet access and is intentionally separate from deterministic CI tests:

```powershell
./ManifestUpdater/bin/Release/net10.0-windows/WingetManifestStudio.exe --self-test --network-tests
```

## Automated Repository Checks

Every push and pull request to `master` runs a clean Windows build followed by the functional self-test, off-screen interface test, and startup probe. The interface suite checks the minimum supported window size through 1920×1080 under the active Windows DPI setting. The functional corpus inspects real PE files, signed-or-unsigned results, MSI input when supplied, and ZIP packages containing real executables. The startup check rejects a first-window time above 15 seconds so major launch-time regressions cannot pass unnoticed. Test reports and off-screen screenshots are retained with each workflow run.

Repository security automation also includes:

- CodeQL C# analysis on pushes, pull requests, a weekly schedule, and manual runs.
- NuGet auditing during every build, with known vulnerability warnings treated as errors.
- Dependency Review on pull requests, rejecting newly introduced moderate-or-higher vulnerabilities.
- Weekly Dependabot updates for NuGet packages and GitHub Actions.
- Project-policy checks on pushes and pull requests, including required community files, tracked build-output detection, and enforcement of the `StudioSetup.msi` release name.
- A self-contained publish smoke test on pushes and pull requests. It creates the EXE and MSI in an isolated CI folder, confirms that both contain the .NET runtime payload, verifies their names and size limits, then runs the published functional and startup tests.

The publish smoke test does not read or change any Visual Studio publish profile. Its isolated files are temporary GitHub Actions output and do not alter a developer's local publish settings.

## Publish

Visual Studio Publish and normal `dotnet publish` runs automatically build:

- `WingetManifestStudio.exe`
- `StudioSetup.msi`

The MSI is always named `StudioSetup.msi`. Both the MSI and standalone EXE contain the .NET 10 Windows Desktop runtime, so they are larger than framework-dependent builds but avoid the slow or blocked first launch caused by a missing shared runtime.

Example self-contained single-file publish:

```powershell
dotnet publish ManifestUpdater/WingetManifestStudio.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
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
