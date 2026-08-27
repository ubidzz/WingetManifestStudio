# Contributing to Winget Manifest Studio

Thank you for helping make Winget manifest publishing easier and safer. Contributions are welcome from beginners, package maintainers, testers, designers, and developers.

By participating, you agree to follow the [Community Code of Conduct](CODE_OF_CONDUCT.md).

## Before You Start

- Search existing issues and pull requests to avoid duplicate work.
- For a substantial feature or interface redesign, open an issue before implementation so its behavior and scope can be agreed upon.
- Report security vulnerabilities privately according to [SECURITY.md](SECURITY.md).
- Never include GitHub tokens, certificates, private keys, personal paths, or release credentials in a contribution.

## Good Contributions

- Clearer beginner guidance or accessibility improvements.
- Winget schema-field coverage.
- Safer YAML preservation and backup behavior.
- Installer inspection, hashing, signature, and validation improvements.
- Reliable MSI, EXE, MSIX, APPX, ZIP, portable, or bundle handling.
- Tests for manifest loading, generation, validation, or interface workflows.
- Documentation and translation corrections.
- Focused bug fixes with a reproducible example.

## Development Setup

You need:

- Windows
- .NET 10 SDK
- Visual Studio with Windows Forms support, or an equivalent .NET development environment
- Git

Clone and build:

```powershell
git clone https://github.com/ubidzz/WingetManifestStudio.git
cd WingetManifestStudio
dotnet restore WingetManifestStudio.slnx
dotnet build ManifestUpdater/WingetManifestStudio.csproj -c Release
```

## Repository Rules

- Keep Winget Manifest Studio separate from unrelated applications and repositories.
- Preserve Visual Studio WinForms Designer compatibility.
- Do not place manually constructed runtime controls inside `InitializeComponent`.
- Keep long-running file, hash, network, Winget, and WingetCreate work off the UI thread.
- Do not delay the first visible window for background tool detection.
- Use normal Windows file and folder dialogs.
- Do not require administrator permission for ordinary use.
- Preserve unsupported parsed YAML fields when updating existing manifests.
- Create recoverable backups before replacing manifests.
- Interactive WingetCreate commands must use a persistent console.
- Keep authentication in WingetCreate and Windows Credential Manager.
- Do not hardcode personal folder paths.
- Do not change publish profiles unless the contribution specifically addresses publishing and explains the reason.
- The setup file must remain named `SynixStudioSetup.msi`.

## Coding Style

- Follow the existing C# style and nullable annotations.
- Prefer small, focused services and reusable custom controls.
- Keep third-party dependencies minimal and justify new packages.
- Use plain language for text shown to nontechnical users.
- Keep buttons, spacing, grids, labels, fields, and status states consistent with the existing dark navy and teal interface.
- Preserve unrelated working behavior and avoid broad rewrites when a focused change is sufficient.

## Tests

Every behavioral or interface change should include an appropriate regression check. Build with warnings treated as errors:

```powershell
dotnet build ManifestUpdater/WingetManifestStudio.csproj -c Release -p:TreatWarningsAsErrors=true
```

Run the functional and hidden UI tests:

```powershell
./ManifestUpdater/bin/Release/net10.0-windows/WingetManifestStudio.exe --self-test
./ManifestUpdater/bin/Release/net10.0-windows/WingetManifestStudio.exe --ui-self-test
./ManifestUpdater/bin/Release/net10.0-windows/WingetManifestStudio.exe --startup-probe
```

Tests must not open visible windows, take control of the desktop, submit packages, change Winget settings, install software, or expose credentials.

When installer or manifest behavior changes, add a focused self-test that proves the failure before the fix and passes afterward.

## Pull Requests

Keep each pull request focused. Include:

- What changed.
- Why the change is needed.
- How a user experiences the improvement.
- Tests that were run and their results.
- Before-and-after screenshots for visible interface changes.
- Any known limitations or follow-up work.

Do not commit generated `bin`, `obj`, publish, test-report, or local Visual Studio files.

## Documentation

Update the README, Help & Guide text, field guidance, and tests whenever a workflow or user-visible label changes. Documentation should describe the current application, not a planned feature.

## License

By submitting a contribution, you agree that it may be distributed under the repository's [MIT License](LICENSE). You confirm that you have the right to submit the contribution.
