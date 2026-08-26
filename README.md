# Winget Manifest Studio

Winget Manifest Studio is a free Windows desktop tool for creating, updating, checking, and submitting Microsoft Winget package manifests without editing YAML by hand.

The program is designed for people who may be publishing their first Windows application. It keeps installer files local, calculates SHA-256 hashes automatically, reads MSI identity values when available, previews every change, and backs up existing manifests before saving.

## What You Need

- A Windows installer or portable release file that you plan to publish.
- A public HTTPS download link for that exact file, normally from a GitHub release.
- Basic package information such as the application name, publisher, version, license, description, and support link.
- WingetCreate only when you are ready to use Microsoft's official validation or submission workflow.

## Update an Existing Winget Package

1. Select **Load existing manifests**.
2. Choose the folder containing the package YAML files.
3. Review **Package Details** and enter the new version without a leading `v`.
4. Open **Installers & Hashes** and attach the exact local release file.
5. Enter the public HTTPS URL where Winget will download that file.
6. Select **Inspect & Fill Details**. The Studio calculates the SHA-256 hash and reads supported installer information.
7. After uploading the release, select **Verify Public URLs** to prove the public downloads match those hashes.
8. Follow **Project Readiness**, then open **Preview & Submit** and select **Preview Changes**.
9. Select **Save Manifests** only after the preview is correct.
10. Open **Test Center** and run **Safe Preflight**.
11. Run **Test Install Here** or **Test in Windows Sandbox**, then verify the installed result.
12. Submit with Microsoft's official WingetCreate tool when ready. The submission opens the pull request in `microsoft/winget-pkgs`.

## Create a New Winget Package

1. Select **Create a new project** and choose an empty output folder.
2. Complete the required fields under **Package Details**.
3. Add the release installer under **Installers & Hashes**.
4. Enter the public installer URL and inspect the file.
5. Preview the generated version, locale, and installer manifests.
6. Use **Find Existing Package** to confirm that the package identifier is new.
7. Save, validate, and test-install the files.
8. Submit them with WingetCreate.

## Test Center

The Test Center separates checks that do not install anything from tests that can change a computer.

**Run Safe Preflight** performs these checks without launching an installer:

- Generates every managed manifest, including additional locale manifests.
- Recalculates attached local-file hashes and compares them with the YAML.
- Checks Authenticode signer, certificate dates, and Windows trust.
- Runs the official `winget validate --manifest <folder>` command against a clean temporary folder.
- Searches the configured Winget source and `microsoft/winget-pkgs` for the exact package identifier.
- Produces an exportable test report.

**Test Install Here** runs the official local-manifest installation command in a persistent console:

```text
winget install --manifest <folder>
```

Windows requires a one-time administrator action to enable local manifests. The **Enable Local Testing** button runs only:

```text
winget settings --enable LocalManifestFiles
```

The application itself remains non-administrator for normal work. **Verify Installed Result** checks the exact identifier and expected version after installation.

**Test in Windows Sandbox** downloads Microsoft's current official `Tools/SandboxTest.ps1` from `microsoft/winget-pkgs`, validates the generated manifests, and launches the test in a disposable Windows Sandbox. Windows Sandbox must already be enabled as an optional Windows feature.

## YAML and Current Schema Coverage

Existing manifests are parsed as YAML document trees instead of being split with regular expressions. This provides structural preservation for unknown root fields, unknown nested installer fields, YAML lists and mappings, installer rows in any key order, and additional locale manifests. Installer rows are matched by ProductCode, URL, architecture, type, and scope rather than only by row number.

The guided interface includes the common locale, installer behavior, installer switch, platform, protocol, file-extension, release-date, repair, and boolean fields in the Winget 1.12 schema. **Additional locale fields**, **Additional installer fields**, and **Additional row YAML** accept validated mappings for every uncommon or nested schema field, including agreements, documentation, icons, dependencies, markets, expected return codes, nested installer files, authentication, and installation metadata.

YAML comments and hand-formatted spacing are not schema data and may be normalized when an edited document is emitted. Parsed keys, sequences, mappings, anchors, aliases, and scalar values remain in the document. Timestamped source backups preserve the exact original text before every save.

## Existing Package and Pull Request Workflow

**Find Existing Package** checks both Winget and the official GitHub manifest path. Submission repeats this lookup automatically, requires a successful Safe Preflight for the exact current project, saves recoverable backups, and then launches WingetCreate's official `submit` workflow. GitHub authentication remains owned by WingetCreate and Windows Credential Manager.

## Interface Languages

The interface has English and Spanish resources. Change the language at the top of **Help & Guide**. The choice is stored in the current user's local application settings and is not written into project profiles.

## Important Field Meanings

| Field | What to enter |
| --- | --- |
| Package identifier | A stable ID such as `Publisher.ApplicationName`. Do not change it between releases. |
| Package version | The release version without a leading `v`, such as `1.2.3`. |
| Default locale | Usually `en-US`. |
| Installer URL | The public HTTPS address for the exact release file. |
| SHA-256 | Filled automatically from the selected local release file. |
| ProductCode | Read automatically from MSI packages. Usually blank for ordinary EXE installers. |
| UpgradeCode | Read automatically from MSI packages. Usually blank for ordinary EXE installers. |
| Commands | Optional command aliases installed by the package. Existing values are preserved during updates. |

## Safety and Privacy

- Loading manifests does not change them.
- Previewing does not write files.
- Existing manifests receive timestamped backups before saving.
- Installer files are inspected locally and are not uploaded by the Studio.
- Authenticode inspection reads public certificate information only; it never accesses a signing private key.
- Installation and Sandbox tests never run automatically.
- The WingetCreate GitHub token remains in Windows Credential Manager and is not stored in a Studio profile.
- Unsaved editing sessions are recovered from the current user's local application data and never contain authentication tokens.

## Build and Publish

The project targets .NET 10 for Windows. Publishing version 1.1.0 creates:

- A ReadyToRun, self-contained portable folder containing `WingetManifestStudio.exe` and its runtime files.
- `SynixStudioSetup.msi` for a normal Windows installation.

The MSI is created automatically during Visual Studio Publish and contains the complete runtime payload. Keep all files in the portable publish folder together; use the MSI when distributing a single setup file.

### Digital signing

Publishing signs `WingetManifestStudio.exe` before MSI packaging and signs `SynixStudioSetup.msi` afterward when a code-signing certificate with a private key is installed in the current user's Windows certificate store. Set its thumbprint only for the publish process; no certificate or secret is stored in this repository:

```powershell
$env:WMS_SIGNING_CERTIFICATE_THUMBPRINT = '<code-signing-certificate-thumbprint>'
dotnet publish ManifestUpdater/WingetManifestStudio.csproj -p:PublishProfile=FolderProfile
```

The publisher uses SHA-256 and an RFC 3161 timestamp, then verifies both signatures with the Windows SDK signing tool. If no certificate is supplied, publishing still succeeds but reports that the artifacts are unsigned. A trusted public code-signing certificate must be obtained from a certificate authority; the application cannot create that trust identity itself.

Signed artifacts materially improve publisher identity and Windows reputation, but reputation is also affected by certificate history and download prevalence. ReadyToRun publishing, deferred WingetCreate detection, and background inspection keep application startup work out of the first visible window.

## Source Organization

- `Application` contains startup and crash-reporting code.
- `Assets` contains the application icon and image resources.
- `Models` contains manifest and installer data objects.
- `Services` contains YAML, installer inspection, Winget, repository, profile, and state logic.
- `UI` contains the main WinForms form, its Designer resource files, custom controls, and interface text.
- `Testing` contains the automated functional and off-screen interface test runners.
- `Packaging/MSI` contains the WiX installer project.
- `Properties/PublishProfiles` contains the Visual Studio publishing profiles.

The C# namespace remains `ManifestUpdater` so this folder-only organization does not change saved data, published assembly identity, or existing application behavior.
