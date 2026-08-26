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
7. Open **Preview & Submit** and select **Preview Changes**.
8. Select **Save Manifests** only after the preview is correct.
9. Validate and submit with Microsoft's official WingetCreate tool when ready.

## Create a New Winget Package

1. Select **Create a new project** and choose an empty output folder.
2. Complete the required fields under **Package Details**.
3. Add the release installer under **Installers & Hashes**.
4. Enter the public installer URL and inspect the file.
5. Preview the generated version, locale, and installer manifests.
6. Save, validate, and submit the files.

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
- The WingetCreate GitHub token remains in Windows Credential Manager and is not stored in a Studio profile.

## Build and Publish

The project targets .NET 10 for Windows. Publishing creates:

- `WingetManifestStudio.exe` for portable use.
- `SynixStudioSetup.msi` for a normal Windows installation.

The MSI is created automatically during Visual Studio Publish.
