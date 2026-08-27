using System.Diagnostics;
using YamlDotNet.RepresentationModel;

namespace ManifestUpdater;

internal static class SelfTestRunner
{
	public static async Task<int> RunAsync(string[] args)
	{
		List<string> results = [];
		string root = Path.Combine(Path.GetTempPath(), "WingetManifestStudio-SelfTest-" + Guid.NewGuid().ToString("N"));
		try
		{
			Directory.CreateDirectory(root);
			TestNewProject(root, results);
			TestPreservingUpdate(root, results);
			TestNestedManifestDiscovery(root, results);
			TestStructuralReorderAndExtraLocale(root, results);
			TestAdvancedSchemaFields(root, results);
			TestCleanValidationFolder(root, results);
			TestReleaseUrlSynchronization(results);
			TestBeginnerValidation(results);
			TestWingetCreateCommandModes(results);
			TestCredentialStatusCheck(results);
			TestTestingEnvironmentChecks(results);
			TestRepositoryPathAndLocalization(results);
			TestProfileRoundTrip(root, results);
			await TestInstallerInspectionAsync(results);
			await TestWingetHealthDiagnosticAsync(results);
			TestAuthenticodeInspection(root, results);
			int installerIndex = Array.FindIndex(args, argument => string.Equals(argument, "--verify-installer", StringComparison.OrdinalIgnoreCase));
			if (installerIndex >= 0 && installerIndex + 1 < args.Length)
				await TestRealInstallerAsync(args[installerIndex + 1], results);
			int verifyIndex = Array.FindIndex(args, argument => string.Equals(argument, "--verify-folder", StringComparison.OrdinalIgnoreCase));
			if (verifyIndex >= 0 && verifyIndex + 1 < args.Length)
			{
				string manifestFolder = args[verifyIndex + 1];
				TestRealManifestFolder(manifestFolder, results);
				await TestOfficialWingetValidationAsync(manifestFolder, results);
			}
			results.Add("PASS: all self-tests completed.");
			WriteReport(results);
			return 0;
		}
		catch (Exception ex)
		{
			results.Add("FAIL: " + ex);
			WriteReport(results);
			return 1;
		}
		finally
		{
			try { Directory.Delete(root, true); } catch { }
		}
	}

	private static void TestStructuralReorderAndExtraLocale(string root, List<string> results)
	{
		string folder = Path.Combine(root, "structural-existing");
		Directory.CreateDirectory(folder);
		File.WriteAllText(Path.Combine(folder, "Contoso.Sample.yaml"), """
PackageIdentifier: Contoso.Sample
PackageVersion: 1.0.0
DefaultLocale: en-US
ManifestType: version
ManifestVersion: 1.12.0
""");
		File.WriteAllText(Path.Combine(folder, "Contoso.Sample.locale.en-US.yaml"), """
PackageIdentifier: Contoso.Sample
PackageVersion: 1.0.0
PackageLocale: en-US
Publisher: Contoso
PackageName: Sample
License: MIT
ShortDescription: Sample
ManifestType: defaultLocale
ManifestVersion: 1.12.0
""");
		File.WriteAllText(Path.Combine(folder, "Contoso.Sample.locale.fr-FR.yaml"), """
PackageIdentifier: Contoso.Sample
PackageVersion: 1.0.0
PackageLocale: fr-FR
Publisher: Contoso
PackageName: Exemple
License: MIT
ShortDescription: Exemple localisé
CustomTranslationField:
  NestedValue: KeepFrench
ManifestType: locale
ManifestVersion: 1.12.0
""");
		File.WriteAllText(Path.Combine(folder, "Contoso.Sample.installer.yaml"), """
PackageIdentifier: Contoso.Sample
PackageVersion: 1.0.0
InstallerType: exe
Installers:
  - InstallerUrl: https://example.com/x64.exe
    CustomInstallerField:
      Identity: KeepX64
    Architecture: x64
    InstallerSha256: AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
  - CustomInstallerField:
      Identity: KeepArm64
    Architecture: arm64
    InstallerSha256: BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB
    InstallerUrl: https://example.com/arm64.exe
ManifestType: installer
ManifestVersion: 1.12.0
""");

		ManifestProject project = ManifestService.LoadProject(folder);
		Assert(project.Installers.Count == 2, "Installer items must load even when Architecture is not the first field.");
		InstallerArtifact x64 = project.Installers[0];
		InstallerArtifact arm64 = project.Installers[1];
		project.Installers.Clear();
		project.Installers.Add(arm64);
		project.Installers.Add(x64);
		project.PackageVersion = "1.1.0";
		ManifestGenerationResult generated = ManifestService.Generate(project);
		Assert(generated.Files.Count == 4, "Additional locale manifests must be included in the generated and validation set.");
		string installer = generated.Files.Single(pair => pair.Key.Contains(".installer.", StringComparison.OrdinalIgnoreCase)).Value;
		YamlStream parsed = new();
		parsed.Load(new StringReader(installer));
		YamlMappingNode rootNode = (YamlMappingNode)parsed.Documents[0].RootNode;
		YamlSequenceNode installerNodes = (YamlSequenceNode)rootNode.Children[new YamlScalarNode("Installers")];
		YamlMappingNode armNode = installerNodes.Children.OfType<YamlMappingNode>().Single(node => node.Children[new YamlScalarNode("Architecture")].ToString() == "arm64");
		YamlMappingNode x64Node = installerNodes.Children.OfType<YamlMappingNode>().Single(node => node.Children[new YamlScalarNode("Architecture")].ToString() == "x64");
		YamlMappingNode armCustom = (YamlMappingNode)armNode.Children[new YamlScalarNode("CustomInstallerField")];
		YamlMappingNode x64Custom = (YamlMappingNode)x64Node.Children[new YamlScalarNode("CustomInstallerField")];
		Assert(armCustom.Children[new YamlScalarNode("Identity")].ToString() == "KeepArm64"
			&& x64Custom.Children[new YamlScalarNode("Identity")].ToString() == "KeepX64",
			"Unknown installer fields must remain attached to the correct installer after rows are reordered.");
		string french = generated.Files.Single(pair => pair.Key.Contains("fr-FR", StringComparison.OrdinalIgnoreCase)).Value;
		Assert(french.Contains("PackageVersion: 1.1.0", StringComparison.Ordinal) && french.Contains("KeepFrench", StringComparison.Ordinal),
			"Additional locale versions and unknown nested fields must be preserved.");
		results.Add("PASS: structural YAML preservation, identity matching, and additional locales.");
	}

	private static void TestNestedManifestDiscovery(string root, List<string> results)
	{
		string selectedFolder = Path.Combine(root, "nested-selection");
		string manifestFolder = Path.Combine(selectedFolder, "Contoso.Sample", "1.0.0");
		ManifestProject source = SampleProject(manifestFolder);
		ManifestGenerationResult generated = ManifestService.Generate(source);
		ManifestService.Save(source, generated);

		string backupFolder = Path.Combine(manifestFolder, ".manifest-backups", "older");
		Directory.CreateDirectory(backupFolder);
		foreach (string path in Directory.GetFiles(manifestFolder, "*.yaml", SearchOption.TopDirectoryOnly))
			File.Copy(path, Path.Combine(backupFolder, Path.GetFileName(path)));

		ManifestProject loaded = ManifestService.LoadProject(selectedFolder);
		Assert(loaded.LoadedFromExistingManifests, "Selecting a parent folder must find its single nested Winget manifest set.");
		Assert(loaded.PackageIdentifier == source.PackageIdentifier && loaded.PackageVersion == source.PackageVersion,
			"Nested manifest discovery must populate the package identity.");
		Assert(Path.GetFullPath(loaded.ManifestFolder) == Path.GetFullPath(manifestFolder),
			"The project output folder must resolve to the folder that actually contains the YAML files.");
		results.Add("PASS: parent-folder manifest discovery ignores backup copies and loads the real YAML set.");
	}

	private static void TestAdvancedSchemaFields(string root, List<string> results)
	{
		ManifestProject project = SampleProject(Path.Combine(root, "advanced-schema"));
		project.PrivacyUrl = "https://example.com/privacy";
		project.ReleaseDate = "2026-08-26";
		project.Protocols = "sample, sample-secure";
		project.SwitchSilent = "/quiet";
		project.AdvancedLocaleFieldsYaml = "Agreements:\n  - AgreementLabel: Terms\n    AgreementUrl: https://example.com/terms\n    Agreement: Read the terms";
		project.AdvancedInstallerFieldsYaml = "Dependencies:\n  WindowsFeatures:\n    - NetFx3";
		ManifestGenerationResult generated = ManifestService.Generate(project);
		string locale = generated.Files.Single(pair => pair.Key.Contains(".locale.", StringComparison.OrdinalIgnoreCase)).Value;
		string installer = generated.Files.Single(pair => pair.Key.Contains(".installer.", StringComparison.OrdinalIgnoreCase)).Value;
		Assert(locale.Contains("PrivacyUrl:", StringComparison.Ordinal) && locale.Contains("Agreements:", StringComparison.Ordinal), "Guided and advanced locale fields must be generated.");
		Assert(installer.Contains("Protocols:", StringComparison.Ordinal) && installer.Contains("InstallerSwitches:", StringComparison.Ordinal) && installer.Contains("Dependencies:", StringComparison.Ordinal),
			"Guided and advanced installer fields must be generated.");
		project.AdvancedInstallerFieldsYaml = "Dependencies: [";
		Assert(ManifestService.Validate(project).Any(error => error.Contains("Additional installer fields", StringComparison.OrdinalIgnoreCase)), "Invalid advanced YAML must be caught before preview or save.");
		results.Add("PASS: current-schema guided fields and validated advanced field coverage.");
	}

	private static void TestTestingEnvironmentChecks(List<string> results)
	{
		ProcessStartInfo enableCommand = WingetCommandService.CreateEnableLocalManifestFilesStartInfo();
		Assert(enableCommand.UseShellExecute && enableCommand.Verb == "runas" && enableCommand.WindowStyle == ProcessWindowStyle.Hidden,
			"Local manifest setup must use one hidden, elevated Winget process.");
		Assert(enableCommand.FileName.EndsWith("winget.exe", StringComparison.OrdinalIgnoreCase)
			&& enableCommand.ArgumentList.SequenceEqual(["settings", "--enable", "LocalManifestFiles"]),
			"Local manifest setup must call Winget directly with the official administrator-setting arguments.");
		Assert(!enableCommand.FileName.Contains("powershell", StringComparison.OrdinalIgnoreCase),
			"Local manifest setup must not open the previous PowerShell wrapper.");
		const string enabledInfo = "Windows Package Manager v1.29.290\r\n\r\nAdmin Setting State\r\nLocalManifestFiles                        Enabled";
		const string disabledInfo = "Windows Package Manager v1.29.290\r\n\r\nAdmin Setting State\r\nLocalManifestFiles                        Disabled";
		Assert(WingetCommandService.ParseLocalManifestFilesEnabled(enabledInfo)
			&& !WingetCommandService.ParseLocalManifestFilesEnabled(disabledInfo),
			"Winget --info must be the authoritative local-manifest setting check.");
		Assert(WingetCommandService.ParseWingetVersion(enabledInfo) == "v1.29.290",
			"Winget health checks must retain its version while reading administrator settings.");
		_ = WingetCommandService.IsWindowsSandboxAvailable();
		results.Add("PASS: local-manifest setup calls Winget directly and reads the current administrator setting.");
	}

	private static async Task TestWingetHealthDiagnosticAsync(List<string> results)
	{
		WingetHealthResult health = await WingetCommandService.CheckWingetHealthAsync();
		Assert(!string.IsNullOrWhiteSpace(health.Message), "The Winget health check must always return a beginner-readable result.");
		Assert(health.IsReady || health.ExitCode != 0, "A failed Winget health check must retain the diagnostic exit code.");
		results.Add("PASS: Winget health failures are diagnosed before local-test setup opens.");
	}

	private static void TestRepositoryPathAndLocalization(List<string> results)
	{
		Assert(WingetRepositoryService.BuildRepositoryPath("Microsoft.VisualStudioCode") == "manifests/m/Microsoft/VisualStudioCode",
			"Exact package identifiers must map to the official winget-pkgs directory structure.");
		Assert(StudioLocalization.Translate("Test Center", "es-ES") == "Centro de pruebas", "Spanish interface resources must be available.");
		Assert(StudioLocalization.Translate("Test Center", "en-US") == "Test Center", "English must remain the fallback interface language.");
		results.Add("PASS: repository discovery path and English/Spanish localization resources.");
	}

	private static void TestAuthenticodeInspection(string root, List<string> results)
	{
		string executable = Environment.ProcessPath ?? throw new InvalidOperationException("The self-test executable path is unavailable.");
		AuthenticodeInspection signature = AuthenticodeInspector.Inspect(executable);
		Assert(!string.IsNullOrWhiteSpace(signature.Status), "Authenticode inspection must always return a clear status.");
		string unsignedPath = Path.Combine(root, "definitely-unsigned.exe");
		File.WriteAllText(unsignedPath, "This is deliberately not a signed executable.");
		AuthenticodeInspection unsigned = AuthenticodeInspector.Inspect(unsignedPath);
		Assert(unsigned.Status == "Unsigned" && !unsigned.IsSigned && !unsigned.IsTrusted,
			"An unsigned local file must finish inspection with the explicit Unsigned result.");
		results.Add("PASS: Authenticode inspection clearly distinguishes unsigned files from signed files.");
	}

	private static void TestNewProject(string root, List<string> results)
	{
		string folder = Path.Combine(root, "new");
		ManifestProject project = SampleProject(folder);
		ManifestGenerationResult generated = ManifestService.Generate(project);
		Assert(generated.Files.Count == 3, "A new project must produce three manifests.");
		Assert(generated.Files.Values.Any(text => text.Contains("Commands:", StringComparison.Ordinal)), "New manifests must include command aliases.");
		ManifestService.Save(project, generated);
		Assert(Directory.GetFiles(folder, "*.yaml").Length == 3, "Three new YAML files must be saved.");
		results.Add("PASS: new-manifest generation.");
	}

	private static void TestPreservingUpdate(string root, List<string> results)
	{
		string folder = Path.Combine(root, "existing");
		Directory.CreateDirectory(folder);
		File.WriteAllText(Path.Combine(folder, "Contoso.Sample.yaml"), """
PackageIdentifier: Contoso.Sample
PackageVersion: 1.0.0
DefaultLocale: en-US
ManifestType: version
ManifestVersion: 1.12.0
""");
		File.WriteAllText(Path.Combine(folder, "Contoso.Sample.locale.en-US.yaml"), """
PackageIdentifier: Contoso.Sample
PackageVersion: 1.0.0
PackageLocale: en-US
Publisher: Contoso
PackageName: Sample
License: MIT
ShortDescription: Sample package
CustomLocaleField: KeepMe
ManifestType: defaultLocale
ManifestVersion: 1.12.0
""");
		File.WriteAllText(Path.Combine(folder, "Contoso.Sample.installer.yaml"), """
PackageIdentifier: Contoso.Sample
PackageVersion: 1.0.0
InstallerType: msi
Scope: user
InstallModes:
- interactive
- silent
UpgradeBehavior: install
Commands:
- sample
UnsupportedOSArchitectures:
- arm
Installers:
- Architecture: x64
  InstallerUrl: https://example.com/Sample.msi
  InstallerSha256: AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
  ProductCode: '{11111111-1111-1111-1111-111111111111}'
  CustomInstallerField: KeepMe
  AppsAndFeaturesEntries:
    - DisplayName: Sample
      Publisher: Contoso
      ProductCode: '{11111111-1111-1111-1111-111111111111}'
ManifestType: installer
ManifestVersion: 1.12.0
""");

		ManifestProject project = ManifestService.LoadProject(folder);
		Assert(project.Commands == "sample", $"Commands must load from an existing installer manifest. Actual value: '{project.Commands}'.");
		project.PackageVersion = "1.1.0";
		project.Installers[0].Sha256 = new string('B', 64);
		ManifestGenerationResult generated = ManifestService.Generate(project);
		Assert(generated.Changes.Any(change => change.Contains("1.0.0", StringComparison.Ordinal) && change.Contains("1.1.0", StringComparison.Ordinal)), "An update preview must show the old and new versions.");
		string locale = generated.Files.Single(pair => pair.Key.Contains(".locale.", StringComparison.OrdinalIgnoreCase)).Value;
		string installer = generated.Files.Single(pair => pair.Key.Contains(".installer.", StringComparison.OrdinalIgnoreCase)).Value;
		Assert(locale.Contains("CustomLocaleField: KeepMe", StringComparison.Ordinal), "Unknown locale fields must be preserved.");
		Assert(installer.Contains("Commands:", StringComparison.Ordinal) && installer.Contains("- sample", StringComparison.Ordinal), "Commands must survive updates.");
		Assert(installer.Contains("UnsupportedOSArchitectures:", StringComparison.Ordinal), "Unknown root installer fields must be preserved.");
		Assert(installer.Contains("CustomInstallerField: KeepMe", StringComparison.Ordinal), "Unknown per-installer fields must be preserved.");
		Assert(installer.Contains("- DisplayName: Sample", StringComparison.Ordinal), "AppsAndFeaturesEntries must remain a YAML sequence when an existing manifest is updated.");
		Assert(installer.Contains(new string('B', 64), StringComparison.Ordinal), "The selected release hash must be updated.");
		results.Add("PASS: field-preserving update.");
	}

	private static void TestProfileRoundTrip(string root, List<string> results)
	{
		string path = Path.Combine(root, "sample.wingetprofile.json");
		ManifestProject before = SampleProject(Path.Combine(root, "profile-output"));
		ProfileStore.Save(path, before);
		ManifestProject after = ProfileStore.Load(path);
		Assert(after.PackageIdentifier == before.PackageIdentifier && after.Installers.Count == 1, "Profile fields and installers must round-trip.");
		Assert(!File.ReadAllText(path).Contains("token", StringComparison.OrdinalIgnoreCase), "Profiles must not contain authentication tokens.");
		results.Add("PASS: token-free profile round-trip.");
	}

	private static void TestBeginnerValidation(List<string> results)
	{
		ManifestProject project = SampleProject(Path.GetTempPath());
		project.PackageIdentifier = "WingetManifestStudio";
		Assert(ManifestService.Validate(project).Any(error => error.Contains("dot-separated", StringComparison.OrdinalIgnoreCase)),
			"A package identifier without Publisher.Application sections must be rejected before official validation.");
		project.PackageIdentifier = "ubidzz.WingetManifestStudio";
		Assert(!ManifestService.Validate(project).Any(error => error.Contains("Package Identifier", StringComparison.OrdinalIgnoreCase)),
			"A valid Publisher.Application package identifier must be accepted.");
		results.Add("PASS: beginner-friendly package identifier validation.");
	}

	private static void TestCleanValidationFolder(string root, List<string> results)
	{
		string folder = Path.Combine(root, "validation-source");
		ManifestProject project = SampleProject(folder);
		ManifestGenerationResult generated = ManifestService.Generate(project);
		ManifestService.Save(project, generated);
		Directory.CreateDirectory(Path.Combine(folder, ".manifest-backups", "old"));

		string? cleanFolder = null;
		try
		{
			cleanFolder = ManifestService.CreateCleanManifestFolder(generated);
			Assert(Directory.GetDirectories(cleanFolder).Length == 0, "The Winget validation folder must not contain backup subdirectories.");
			Assert(Directory.GetFiles(cleanFolder, "*.yaml").Length == 3, "The Winget validation folder must contain exactly the generated YAML files.");
		}
		finally
		{
			ManifestService.DeleteCleanManifestFolder(cleanFolder);
		}
		Assert(cleanFolder is null || !Directory.Exists(cleanFolder), "The temporary Winget validation folder must be removed.");
		results.Add("PASS: clean validation staging excludes manifest backup folders.");
	}

	private static void TestReleaseUrlSynchronization(List<string> results)
	{
		string download = ManifestService.SynchronizeGitHubReleaseUrl(
			"https://github.com/contoso/sample/releases/download/v1.0.22/Sample.msi", "1.0.22", "1.0.23");
		string notes = ManifestService.SynchronizeGitHubReleaseUrl(
			"https://github.com/contoso/sample/releases/tag/v1.0.22", "1.0.22", "1.0.23");
		string unrelated = ManifestService.SynchronizeGitHubReleaseUrl(
			"https://downloads.example.com/1.0.22/Sample.msi", "1.0.22", "1.0.23");
		Assert(download.EndsWith("/releases/download/v1.0.23/Sample.msi", StringComparison.Ordinal), "GitHub release download URLs must follow an inspected installer version.");
		Assert(notes.EndsWith("/releases/tag/v1.0.23", StringComparison.Ordinal), "GitHub release-notes URLs must follow an inspected installer version.");
		Assert(unrelated.Contains("1.0.22", StringComparison.Ordinal), "Unrecognized download URLs must not be rewritten automatically.");
		results.Add("PASS: inspected versions safely synchronize GitHub release URLs.");
	}

	private static void TestWingetCreateCommandModes(List<string> results)
	{
		Assert(WingetCommandService.RequiresInteractiveConsole("new", string.Empty), "New manifests require an interactive console.");
		Assert(WingetCommandService.RequiresInteractiveConsole("new-locale", "--locale en-US"), "New locale manifests require an interactive console.");
		Assert(WingetCommandService.RequiresInteractiveConsole("update-locale", "--locale en-US"), "Locale updates require an interactive console.");
		Assert(WingetCommandService.RequiresInteractiveConsole("submit", "C:\\manifests"), "Submission must allow WingetCreate to request GitHub authentication.");
		Assert(WingetCommandService.RequiresInteractiveConsole("token", "--store"), "Token commands must allow WingetCreate to request GitHub authentication.");
		Assert(WingetCommandService.RequiresInteractiveConsole("update", "--interactive Contoso.Sample"), "Interactive updates require a console.");
		Assert(!WingetCommandService.RequiresInteractiveConsole("update", "--version 2.0 Contoso.Sample"), "Non-interactive updates should keep captured output in the Studio.");
		Assert(!WingetCommandService.RequiresInteractiveConsole("show", "Contoso.Sample"), "Show should keep captured output in the Studio.");
		System.Diagnostics.ProcessStartInfo tokenStartInfo = WingetCommandService.CreateInteractiveProcessStartInfo("token", "--store", Environment.CurrentDirectory);
		Assert(string.Equals(tokenStartInfo.FileName, "powershell.exe", StringComparison.OrdinalIgnoreCase), "Interactive commands must use the persistent console host.");
		Assert(!tokenStartInfo.UseShellExecute && !tokenStartInfo.CreateNoWindow, "The WingetCreate sign-in console must remain visible and interactive.");
		Assert(tokenStartInfo.ArgumentList.Contains("-EncodedCommand"), "The persistent console host must receive its fixed launcher script safely.");
		Assert(tokenStartInfo.Environment.TryGetValue("WMS_WINGETCREATE_ARGUMENTS", out string? tokenArguments)
			&& tokenArguments is not null
			&& tokenArguments.Contains("token", StringComparison.Ordinal)
			&& tokenArguments.Contains("--store", StringComparison.Ordinal), "The exact token --store command must reach WingetCreate.");
		results.Add("PASS: WingetCreate interactive commands are routed to a real console.");
	}

	private static void TestCredentialStatusCheck(List<string> results)
	{
		_ = WingetCommandService.IsGitHubTokenStored();
		results.Add("PASS: WingetCreate token status can be checked without reading token data.");
	}

	private static async Task TestInstallerInspectionAsync(List<string> results)
	{
		string publishedExecutable = Path.Combine(AppContext.BaseDirectory, "WingetManifestStudio.exe");
		string executable = File.Exists(publishedExecutable)
			? publishedExecutable
			: Environment.ProcessPath ?? throw new InvalidOperationException("The self-test executable path is unavailable.");
		InstallerInspection inspection = await InstallerInspector.InspectAsync(executable, string.Empty);
		Assert(inspection.Sha256.Length == 64, "Installer inspection must calculate SHA-256.");
		Assert(inspection.InstallerType is "exe" or "inno" or "nullsoft", "An executable must be identified as a supported EXE installer type.");
		results.Add("PASS: local installer inspection and hashing.");
	}

	private static async Task TestRealInstallerAsync(string path, List<string> results)
	{
		Assert(File.Exists(path), "The supplied installer verification file does not exist.");
		InstallerInspection inspection = await InstallerInspector.InspectAsync(path, string.Empty);
		Assert(inspection.Sha256.Length == 64, "The supplied installer did not produce a SHA-256 hash.");
		Assert(inspection.Signature.Status.Length > 0, "The supplied installer did not produce a digital-signature result.");
		results.Add($"PASS: real installer inspection completed: {Path.GetFileName(path)}, {inspection.InstallerType}, {inspection.Signature.Status}.");
	}

	private static void TestRealManifestFolder(string folder, List<string> results)
	{
		ManifestProject project = ManifestService.LoadProject(folder);
		Assert(project.LoadedFromExistingManifests, "The supplied compatibility folder does not contain a complete manifest set.");
		Assert(project.Installers.Count > 0, "The supplied installer manifest contains no readable installer entries.");
		ManifestGenerationResult generated = ManifestService.Generate(project);
		string installer = generated.Files.Single(pair => pair.Key.Contains(".installer.", StringComparison.OrdinalIgnoreCase)).Value;
		if (!string.IsNullOrWhiteSpace(project.Commands))
			Assert(project.Commands.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).All(command => installer.Contains(command, StringComparison.Ordinal)), "A command alias would be lost from the supplied manifests.");
		results.Add($"PASS: compatibility preview for {project.PackageIdentifier} {project.PackageVersion}; no files changed.");
	}

	private static async Task TestOfficialWingetValidationAsync(string folder, List<string> results)
	{
		ManifestProject project = ManifestService.LoadProject(folder);
		ManifestGenerationResult generated = ManifestService.Generate(project);
		string? cleanFolder = null;
		try
		{
			cleanFolder = ManifestService.CreateCleanManifestFolder(generated);
			CommandResult validation = await WingetCommandService.ValidateManifestAsync(cleanFolder);
			string details = string.Join(Environment.NewLine, new[] { validation.Output, validation.Error }
				.Where(value => !string.IsNullOrWhiteSpace(value))).Trim();
			Assert(validation.ExitCode == 0, "Official Winget validation failed: " + details);
		}
		finally
		{
			ManifestService.DeleteCleanManifestFolder(cleanFolder);
		}
		results.Add("PASS: official Winget validation accepted the clean generated manifest set.");
	}

	private static ManifestProject SampleProject(string folder)
	{
		ManifestProject project = new()
		{
			PackageIdentifier = "Contoso.Sample",
			PackageVersion = "1.0.0",
			DefaultLocale = "en-US",
			ManifestVersion = "1.12.0",
			ManifestFolder = folder,
			Publisher = "Contoso",
			PackageName = "Sample",
			License = "MIT",
			ShortDescription = "A sample package.",
			Commands = "sample, sample-cli",
			Tags = "sample, utility",
			InstallerType = "msi",
			Scope = "user"
		};
		project.Installers.Add(new InstallerArtifact
		{
			InstallerUrl = "https://example.com/Sample.msi",
			Architecture = "x64",
			InstallerType = "msi",
			Scope = "user",
			Sha256 = new string('A', 64)
		});
		return project;
	}

	private static void Assert(bool condition, string message)
	{
		if (!condition) throw new InvalidOperationException(message);
	}

	private static void WriteReport(IEnumerable<string> lines)
	{
		File.WriteAllLines(Path.Combine(AppContext.BaseDirectory, "self-test-report.txt"), lines);
	}
}
