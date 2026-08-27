using System.Diagnostics;
using System.IO.Compression;
using System.Text;
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
			TestDisplayVersionNormalization(root, results);
			TestNestedManifestDiscovery(root, results);
			TestStructuralReorderAndExtraLocale(root, results);
			TestOptionalRootPreservation(root, results);
			TestZipNestedInstallerFiles(root, results);
			TestAdvancedSchemaFields(root, results);
			TestGuidedSchemaRoundTrip(root, results);
			TestSpecialPackageGuidance(root, results);
			TestCleanValidationFolder(root, results);
			TestBeginnerValidation(results);
			TestDynamicPackageValidation(results);
			TestWingetCreateCommandModes(root, results);
			TestCredentialStatusCheck(results);
			TestTestingEnvironmentChecks(results);
			await TestSchemaRecommendationAndSandboxUninstallAsync(results);
			TestInstalledVerificationMatching(results);
			TestRepositoryPathAndLocalization(results);
			TestGitHubReleaseParsing(results);
			TestStudioUpdater(results);
			TestProfileRoundTrip(root, results);
			await TestInstallerInspectionAsync(results);
			await TestInstallerTechnologyDetectionAsync(root, results);
			await TestRealInstallerCorpusAsync(root, results);
			await TestFontInspectionAsync(root, results);
			await TestZipInspectionAsync(root, results);
			await TestWingetHealthDiagnosticAsync(results);
			TestAuthenticodeInspection(root, results);
			if (args.Any(argument => string.Equals(argument, "--network-tests", StringComparison.OrdinalIgnoreCase)))
			{
				await TestPublicImportServicesAsync(root, results);
				await TestStudioUpdateFeedAsync(results);
			}
			if (args.Any(argument => string.Equals(argument, "--official-schema-tests", StringComparison.OrdinalIgnoreCase)))
				await TestOfficialGuidedSchemaAsync(root, results);
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
		project.Agreements = "Terms | https://example.com/terms | Read the terms";
		project.WindowsFeatures = "NetFx3";
		project.AdvancedLocaleFieldsYaml = "CustomLocaleField: KeepLocale";
		project.AdvancedInstallerFieldsYaml = "CustomInstallerField: KeepInstaller";
		ManifestGenerationResult generated = ManifestService.Generate(project);
		string locale = generated.Files.Single(pair => pair.Key.Contains(".locale.", StringComparison.OrdinalIgnoreCase)).Value;
		string installer = generated.Files.Single(pair => pair.Key.Contains(".installer.", StringComparison.OrdinalIgnoreCase)).Value;
		Assert(locale.Contains("PrivacyUrl:", StringComparison.Ordinal) && locale.Contains("Agreements:", StringComparison.Ordinal) && locale.Contains("CustomLocaleField:", StringComparison.Ordinal), "Guided and advanced locale fields must be generated.");
		Assert(installer.Contains("Protocols:", StringComparison.Ordinal) && installer.Contains("InstallerSwitches:", StringComparison.Ordinal) && installer.Contains("Dependencies:", StringComparison.Ordinal) && installer.Contains("CustomInstallerField:", StringComparison.Ordinal),
			"Guided and advanced installer fields must be generated.");
		project.AdvancedInstallerFieldsYaml = "Dependencies: [";
		Assert(ManifestService.Validate(project).Any(error => error.Contains("Additional installer fields", StringComparison.OrdinalIgnoreCase)), "Invalid advanced YAML must be caught before preview or save.");
		results.Add("PASS: current-schema guided fields and validated advanced field coverage.");
	}

	private static void TestGuidedSchemaRoundTrip(string root, List<string> results)
	{
		string folder = Path.Combine(root, "guided-schema");
		ManifestProject project = SampleProject(folder);
		project.Agreements = "Terms | https://example.com/terms | Read before installing";
		project.Documentations = "User guide | https://example.com/docs";
		project.PackageDependencies = "Contoso.Runtime | 2.0.0";
		project.WindowsFeatures = "NetFx3, Containers";
		project.Capabilities = "internetClient";
		project.RestrictedCapabilities = "runFullTrust";
		project.Markets = "US, CA";
		project.ExcludedMarkets = "AQ";
		project.ExpectedReturnCodes = "1603 | contactSupport | https://example.com/support";
		project.UnsupportedArguments = "log, location";
		project.DefaultInstallLocation = "%ProgramFiles%\\Contoso\\Sample";
		project.InstalledFiles = "Sample.exe | launch | " + new string('A', 64) + " | --safe | Sample";
		ManifestGenerationResult generated = ManifestService.Generate(project);
		ManifestService.Save(project, generated);
		ManifestProject loaded = ManifestService.LoadProject(folder);
		Assert(loaded.Agreements.Contains("https://example.com/terms", StringComparison.Ordinal)
			&& loaded.Documentations.Contains("User guide", StringComparison.Ordinal)
			&& loaded.PackageDependencies.Contains("Contoso.Runtime", StringComparison.Ordinal)
			&& loaded.WindowsFeatures.Contains("Containers", StringComparison.Ordinal)
			&& loaded.ExpectedReturnCodes.Contains("1603", StringComparison.Ordinal)
			&& loaded.InstalledFiles.Contains("Sample.exe", StringComparison.Ordinal),
			"Guided uncommon schema fields must survive save and reload without raw YAML editing.");
		Assert(ManifestService.Validate(loaded).Count == 0, "Guided uncommon schema fields must pass Studio validation.");
		results.Add("PASS: guided uncommon Winget schema fields save and reload structurally.");
	}

	private static void TestOptionalRootPreservation(string root, List<string> results)
	{
		string folder = Path.Combine(root, "optional-root-preservation");
		Directory.CreateDirectory(folder);
		File.WriteAllText(Path.Combine(folder, "Fabrikam.Utility.yaml"), """
PackageIdentifier: Fabrikam.Utility
PackageVersion: 2.0.0
DefaultLocale: en-US
ManifestType: version
ManifestVersion: 1.12.0
""");
		File.WriteAllText(Path.Combine(folder, "Fabrikam.Utility.locale.en-US.yaml"), """
PackageIdentifier: Fabrikam.Utility
PackageVersion: 2.0.0
PackageLocale: en-US
Publisher: Fabrikam
PackageName: Utility
License: MIT
ShortDescription: A utility.
ManifestType: defaultLocale
ManifestVersion: 1.12.0
""");
		File.WriteAllText(Path.Combine(folder, "Fabrikam.Utility.installer.yaml"), """
PackageIdentifier: Fabrikam.Utility
PackageVersion: 2.0.0
Installers:
  - Architecture: arm64
    InstallerType: exe
    InstallerUrl: https://example.com/Utility-arm64.exe
    InstallerSha256: AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
ManifestType: installer
ManifestVersion: 1.12.0
""");

		ManifestProject project = ManifestService.LoadProject(folder);
		Assert(project.Platform.Length == 0 && project.InstallerType.Length == 0 && project.Scope.Length == 0
			&& project.InstallModes.Length == 0 && project.UpgradeBehavior.Length == 0,
			"Loading an existing manifest must not invent optional root installer defaults.");
		ManifestGenerationResult generated = ManifestService.Generate(project);
		string installer = generated.Files.Single(pair => pair.Key.Contains(".installer.", StringComparison.OrdinalIgnoreCase)).Value;
		YamlStream parsed = new();
		parsed.Load(new StringReader(installer));
		YamlMappingNode manifest = (YamlMappingNode)parsed.Documents[0].RootNode;
		Assert(!HasRootKey(manifest, "Platform") && !HasRootKey(manifest, "InstallerType") && !HasRootKey(manifest, "Scope")
			&& !HasRootKey(manifest, "InstallModes") && !HasRootKey(manifest, "UpgradeBehavior"),
			"Previewing an existing manifest must keep absent optional root fields absent.");
		YamlMappingNode row = ((YamlSequenceNode)manifest.Children[new YamlScalarNode("Installers")]).Children.OfType<YamlMappingNode>().Single();
		Assert(row.Children[new YamlScalarNode("InstallerType")].ToString() == "exe", "A row-level installer type must remain on that row.");
		results.Add("PASS: optional root fields are preserved without package-specific defaults.");
	}

	private static void TestZipNestedInstallerFiles(string root, List<string> results)
	{
		string folder = Path.Combine(root, "zip-nested-files");
		ManifestProject project = SampleProject(folder);
		project.InstallerType = string.Empty;
		InstallerArtifact installer = project.Installers.Single();
		installer.InstallerType = "zip";
		installer.NestedInstallerType = "portable";
		installer.NestedInstallerFiles = "tools\\sample.exe | sample; tools\\sample-cli.exe | sample-cli";
		installer.InstallerUrl = "https://example.com/Sample.zip";
		Assert(ManifestService.Validate(project).Count == 0, "A complete portable ZIP package must pass local validation.");

		ManifestGenerationResult generated = ManifestService.Generate(project);
		string yaml = generated.Files.Single(pair => pair.Key.Contains(".installer.", StringComparison.OrdinalIgnoreCase)).Value;
		Assert(yaml.Contains("NestedInstallerFiles:", StringComparison.Ordinal)
			&& yaml.Contains("RelativeFilePath: tools\\sample.exe", StringComparison.Ordinal)
			&& yaml.Contains("PortableCommandAlias: sample-cli", StringComparison.Ordinal),
			"ZIP contents and portable command aliases must be emitted as schema-aware YAML.");
		ManifestService.Save(project, generated);
		ManifestProject loaded = ManifestService.LoadProject(folder);
		Assert(loaded.Installers.Single().NestedInstallerFiles.Contains("sample-cli.exe | sample-cli", StringComparison.Ordinal),
			"ZIP nested file paths and aliases must load back into the installer row.");

		installer.NestedInstallerFiles = string.Empty;
		Assert(ManifestService.Validate(project).Any(error => error.Contains("file path from inside", StringComparison.OrdinalIgnoreCase)),
			"A ZIP without NestedInstallerFiles must be stopped with a clear error.");

		ManifestProject sharedProject = SampleProject(Path.Combine(root, "zip-shared-nested-files"));
		sharedProject.InstallerType = "zip";
		sharedProject.NestedInstallerType = "portable";
		sharedProject.NestedInstallerFiles = "tools\\shared.exe | shared";
		InstallerArtifact sharedInstaller = sharedProject.Installers.Single();
		sharedInstaller.InstallerType = string.Empty;
		sharedInstaller.NestedInstallerType = string.Empty;
		sharedInstaller.NestedInstallerFiles = string.Empty;
		sharedInstaller.InstallerUrl = "https://example.com/Shared.zip";
		Assert(ManifestService.Validate(sharedProject).Count == 0, "A shared root-level ZIP definition must satisfy its installer rows.");
		ManifestGenerationResult sharedGenerated = ManifestService.Generate(sharedProject);
		string sharedYaml = sharedGenerated.Files.Single(pair => pair.Key.Contains(".installer.", StringComparison.OrdinalIgnoreCase)).Value;
		YamlStream sharedParsed = new();
		sharedParsed.Load(new StringReader(sharedYaml));
		YamlMappingNode sharedRoot = (YamlMappingNode)sharedParsed.Documents[0].RootNode;
		YamlMappingNode sharedRow = ((YamlSequenceNode)sharedRoot.Children[new YamlScalarNode("Installers")]).Children.OfType<YamlMappingNode>().Single();
		Assert(HasRootKey(sharedRoot, "NestedInstallerFiles") && !HasRootKey(sharedRow, "NestedInstallerFiles"),
			"A shared root-level ZIP definition must stay at the root instead of being duplicated into every row.");
		ManifestService.Save(sharedProject, sharedGenerated);
		ManifestProject sharedLoaded = ManifestService.LoadProject(sharedProject.ManifestFolder);
		Assert(sharedLoaded.NestedInstallerFiles.Contains("shared.exe | shared", StringComparison.Ordinal)
			&& sharedLoaded.Installers.Single().NestedInstallerFiles.Contains("shared.exe | shared", StringComparison.Ordinal),
			"Root-level ZIP contents must load as the shared value and the effective row value.");
		results.Add("PASS: ZIP nested installers round-trip and validate for any package.");
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

	private static void TestSpecialPackageGuidance(string root, List<string> results)
	{
		ManifestProject fontProject = SampleProject(Path.Combine(root, "font-guidance"));
		fontProject.InstallerType = string.Empty;
		fontProject.Installers[0].InstallerType = "font";
		fontProject.Installers[0].Architecture = "neutral";
		fontProject.Installers[0].InstallerUrl = "https://example.com/Fabrikam.ttf";
		ManifestGenerationResult fontResult = ManifestService.Generate(fontProject);
		Assert(fontResult.Warnings.Any(warning => warning.Contains("fonts manifest root", StringComparison.OrdinalIgnoreCase)),
			"Font projects must explain the separate Microsoft repository root before submission.");

		ManifestProject pwaProject = SampleProject(Path.Combine(root, "pwa-guidance"));
		pwaProject.InstallerType = string.Empty;
		pwaProject.Installers[0].InstallerType = "pwa";
		pwaProject.Installers[0].Architecture = "neutral";
		ManifestGenerationResult pwaResult = ManifestService.Generate(pwaProject);
		Assert(pwaResult.Warnings.Any(warning => warning.Contains("client and community-repository support", StringComparison.OrdinalIgnoreCase)),
			"PWA projects must explain that official client and repository support can differ from schema authoring support.");
		results.Add("PASS: font and PWA projects receive current submission-context guidance.");
	}

	private static async Task TestWingetHealthDiagnosticAsync(List<string> results)
	{
		WingetHealthResult health = await WingetCommandService.CheckWingetHealthAsync();
		Assert(!string.IsNullOrWhiteSpace(health.Message), "The Winget health check must always return a beginner-readable result.");
		Assert(health.IsReady || health.ExitCode != 0, "A failed Winget health check must retain the diagnostic exit code.");
		results.Add("PASS: Winget health failures are diagnosed before local-test setup opens.");
	}

	private static void TestInstalledVerificationMatching(List<string> results)
	{
		Assert(InstalledPackageVerifier.VersionsMatch("1.1.0", "1.1.0")
			&& InstalledPackageVerifier.VersionsMatch("1.1.0.0", "1.1")
			&& !InstalledPackageVerifier.VersionsMatch("1.0.0", "1.1.0"),
			"Installed-version verification must accept equivalent numeric versions and reject a different release.");
		Assert(InstalledPackageVerifier.VersionsMatchOutput("Winget Manifest Studio  1.1.0", "1.1.0")
			&& !InstalledPackageVerifier.VersionsMatchOutput("Winget Manifest Studio  1.0.0", "1.1.0"),
			"Winget name fallback must still require the expected installed version.");
		results.Add("PASS: installed-result verification matches Winget, MSI, and display-version formats safely.");
	}

	private static void TestRepositoryPathAndLocalization(List<string> results)
	{
		Assert(WingetRepositoryService.BuildRepositoryPath("Microsoft.VisualStudioCode") == "manifests/m/Microsoft/VisualStudioCode",
			"Exact package identifiers must map to the official winget-pkgs directory structure.");
		Assert(WingetRepositoryService.BuildRepositoryPath("Microsoft.FluentFonts", "fonts") == "fonts/m/Microsoft/FluentFonts",
			"Font package identifiers must map to the separate official fonts repository root.");
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

	private static void TestDisplayVersionNormalization(string root, List<string> results)
	{
		ManifestProject project = SampleProject(Path.Combine(root, "display-version"));
		InstallerArtifact installer = project.Installers[0];
		installer.DisplayName = "Sample";
		installer.Publisher = "Contoso";
		installer.ProductCode = "{11111111-1111-1111-1111-111111111111}";
		installer.UpgradeCode = "{22222222-2222-2222-2222-222222222222}";
		installer.ProductVersion = project.PackageVersion;
		string equalVersionManifest = ManifestService.Generate(project).Files
			.Single(pair => pair.Key.Contains(".installer.", StringComparison.OrdinalIgnoreCase)).Value;
		Assert(equalVersionManifest.Contains("AppsAndFeaturesEntries:", StringComparison.Ordinal)
			&& !equalVersionManifest.Contains("DisplayVersion:", StringComparison.Ordinal),
			"DisplayVersion must be omitted when it duplicates PackageVersion while other AppsAndFeatures fields remain.");

		installer.ProductVersion = project.PackageVersion + ".0";
		string distinctVersionManifest = ManifestService.Generate(project).Files
			.Single(pair => pair.Key.Contains(".installer.", StringComparison.OrdinalIgnoreCase)).Value;
		Assert(distinctVersionManifest.Contains("DisplayVersion: 1.0.0.0", StringComparison.Ordinal),
			"DisplayVersion must remain when the installed version genuinely differs from PackageVersion.");
		results.Add("PASS: redundant AppsAndFeatures DisplayVersion values are removed without losing distinct installed versions.");
	}

	private static void TestBeginnerValidation(List<string> results)
	{
		ManifestProject project = SampleProject(Path.GetTempPath());
		project.PackageIdentifier = "WingetManifestStudio";
		Assert(ManifestService.Validate(project).Any(error => error.Contains("dot-separated", StringComparison.OrdinalIgnoreCase)),
			"A package identifier without Publisher.Application sections must be rejected before official validation.");
		project.PackageIdentifier = "Fabrikam.Utility";
		Assert(!ManifestService.Validate(project).Any(error => error.Contains("Package Identifier", StringComparison.OrdinalIgnoreCase)),
			"A valid Publisher.Application package identifier must be accepted.");
		results.Add("PASS: beginner-friendly package identifier validation.");
	}

	private static void TestDynamicPackageValidation(List<string> results)
	{
		ManifestProject project = SampleProject(Path.GetTempPath());
		project.DefaultLocale = "not a locale";
		project.Platform = "Windows.Desktop, Windows.Desktop";
		project.RepairBehavior = "restart";
		project.Installers[0].Architecture = "mips";
		project.Installers[0].InstallerType = "custom-setup";
		project.Installers[0].NestedInstallerType = "exe";
		IReadOnlyList<string> errors = ManifestService.Validate(project);
		Assert(errors.Any(error => error.Contains("Default Locale", StringComparison.OrdinalIgnoreCase))
			&& errors.Any(error => error.Contains("Architecture", StringComparison.OrdinalIgnoreCase))
			&& errors.Any(error => error.Contains("Installer Type", StringComparison.OrdinalIgnoreCase))
			&& errors.Any(error => error.Contains("Repair Behavior", StringComparison.OrdinalIgnoreCase))
			&& errors.Any(error => error.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
			&& errors.Any(error => error.Contains("nested installer", StringComparison.OrdinalIgnoreCase)),
			"Package-independent validation must clearly identify invalid locale, architecture, installer behavior, duplicate, and ZIP-only values.");
		results.Add("PASS: dynamic package values are checked against beginner-readable Winget choices.");
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

	private static void TestWingetCreateCommandModes(string root, List<string> results)
	{
		Assert(WingetCommandService.RequiresInteractiveConsole("new", string.Empty), "New manifests require an interactive console.");
		Assert(WingetCommandService.RequiresInteractiveConsole("new-locale", "--locale en-US"), "New locale manifests require an interactive console.");
		Assert(WingetCommandService.RequiresInteractiveConsole("update-locale", "--locale en-US"), "Locale updates require an interactive console.");
		Assert(WingetCommandService.RequiresInteractiveConsole("submit", "C:\\manifests"), "Submission must allow WingetCreate to request GitHub authentication.");
		Assert(WingetCommandService.RequiresInteractiveConsole("token", "--store"), "Token commands must allow WingetCreate to request GitHub authentication.");
		Assert(WingetCommandService.RequiresInteractiveConsole("update", "--interactive Contoso.Sample"), "Interactive updates require a console.");
		Assert(!WingetCommandService.RequiresInteractiveConsole("update", "--version 2.0 Contoso.Sample"), "Non-interactive updates should keep captured output in the Studio.");
		Assert(!WingetCommandService.RequiresInteractiveConsole("show", "Contoso.Sample"), "Show should keep captured output in the Studio.");
		Assert(WingetCommandService.ManifestValidationSucceeded(new CommandResult(1, "Manifest validation succeeded with warnings.", "Manifest Warning: restricted field"))
			&& !WingetCommandService.ManifestValidationSucceeded(new CommandResult(1, "Manifest validation failed.", "Schema error")),
			"Official validation warnings must remain reviewable without being mistaken for schema failures.");
		System.Diagnostics.ProcessStartInfo tokenStartInfo = WingetCommandService.CreateInteractiveProcessStartInfo("token", "--store", Environment.CurrentDirectory);
		Assert(string.Equals(tokenStartInfo.FileName, "powershell.exe", StringComparison.OrdinalIgnoreCase), "Interactive commands must use the persistent console host.");
		Assert(!tokenStartInfo.UseShellExecute && !tokenStartInfo.CreateNoWindow, "The WingetCreate sign-in console must remain visible and interactive.");
		Assert(tokenStartInfo.ArgumentList.Contains("-EncodedCommand"), "The persistent console host must receive its fixed launcher script safely.");
		Assert(tokenStartInfo.Environment.TryGetValue("WMS_WINGETCREATE_ARGUMENTS", out string? tokenArguments)
			&& tokenArguments is not null
			&& tokenArguments.Contains("token", StringComparison.Ordinal)
			&& tokenArguments.Contains("--store", StringComparison.Ordinal), "The exact token --store command must reach WingetCreate.");
		string submissionFolder = ManifestService.CreateCleanManifestFolder(
			ManifestService.Generate(SampleProject(Path.Combine(root, "submission-source"))));
		string submissionLog = Path.Combine(submissionFolder, "interactive-command.log");
		File.WriteAllText(submissionLog, "submission test");
		InteractiveCommandSession submissionSession = new(
			0,
			submissionLog,
			CleanupFolder: submissionFolder);
		Assert(Directory.Exists(submissionFolder),
			"The clean submission folder must remain available while the interactive command owns it.");
		WingetCommandService.CleanupInteractiveCommandSessionArtifacts(submissionSession);
		Assert(!Directory.Exists(submissionFolder),
			"The clean submission folder must be removed after the interactive WingetCreate session finishes.");
		results.Add("PASS: WingetCreate interactive commands keep submission manifests alive until the console finishes.");
	}

	private static async Task TestSchemaRecommendationAndSandboxUninstallAsync(List<string> results)
	{
		Assert(ManifestSchemaSupport.RecommendedForWinget("v1.29.290") == "1.12.0"
			&& ManifestSchemaSupport.RecommendedForWinget("v1.28.10") == "1.12.0"
			&& ManifestSchemaSupport.RecommendedForWinget("v1.12.350") == "1.12.0"
			&& ManifestSchemaSupport.RecommendedForWinget("v1.10.100") == "1.10.0",
			"The manifest schema recommendation must stay within versions accepted by the Winget community repository.");
		Assert(ManifestSchemaSupport.NormalizeKnownStudioVersion("1.28.0") == "1.12.0"
			&& !ManifestSchemaSupport.SupportedVersions.Contains("1.28.0", StringComparer.OrdinalIgnoreCase),
			"Projects created with the Studio's invalid preview schema must migrate to the recommended community schema.");
		ManifestProject project = new()
		{
			PackageIdentifier = "AnyPublisher.AnyApplication",
			PackageVersion = "2.4.0",
			Publisher = "Any Publisher",
			PackageName = "Any Application",
			License = "MIT",
			ShortDescription = "A package-independent schema test.",
			ManifestFolder = Path.Combine(Path.GetTempPath(), "WingetManifestStudio-SchemaTest"),
			PackageFamilyName = "AnyPublisher.AnyApplication_1234567890abc"
		};
		project.Installers.Add(new InstallerArtifact
		{
			ProductCode = "{11111111-2222-3333-4444-555555555555}",
			DisplayName = "Any Application",
			Architecture = "x64",
			InstallerType = "msi",
			InstallerUrl = "https://example.com/AnyApplication.msi",
			Sha256 = new string('A', 64)
		});
		ManifestGenerationResult currentSchema = ManifestService.Generate(project);
		Assert(project.ManifestVersion == ManifestSchemaSupport.CurrentVersion
			&& currentSchema.Files.Values.All(yaml => yaml.Contains("ManifestVersion: 1.12.0", StringComparison.Ordinal)
				&& yaml.Contains(".1.12.0.schema.json", StringComparison.Ordinal)),
			"A new package must generate every manifest against the recommended community schema.");
		project.ManifestVersion = "1.28.0";
		Assert(ManifestService.Validate(project).Any(error => error.Contains("not accepted", StringComparison.OrdinalIgnoreCase)),
			"A schema version that WingetCreate cannot submit must be blocked with beginner-readable guidance.");
		project.ManifestVersion = ManifestSchemaSupport.CurrentVersion;
		project.ElevationRequirement = "elevationProhibited";
		Assert(WingetCommandService.HasSandboxElevationConflict(project),
			"Sandbox tests must stop before launch when Winget would block an elevationProhibited installer from Microsoft's Administrator session.");
		project.ElevationRequirement = "elevatesSelf";
		Assert(!WingetCommandService.HasSandboxElevationConflict(project),
			"Sandbox tests must remain available for installers that elevate themselves.");
		project.ElevationRequirement = string.Empty;
		string script = WingetCommandService.BuildSandboxInstallUninstallScript(project, "safe-result.txt");
		Assert(script.Contains("winget.exe uninstall", StringComparison.OrdinalIgnoreCase)
			&& script.Contains("STATUS=", StringComparison.Ordinal)
			&& script.Contains("Get-WmsArpMatches", StringComparison.Ordinal)
			&& script.Contains("Get-WmsAppxMatches", StringComparison.Ordinal)
			&& !script.Contains(project.PackageIdentifier, StringComparison.Ordinal)
			&& !script.Contains(project.PackageName, StringComparison.Ordinal),
			"The disposable uninstall script must verify removal and encode package-specific values instead of injecting them into PowerShell.");
		const string officialSandboxSample = """
			$response = Invoke-WebRequest -Uri $URL -Method Head -ErrorAction SilentlyContinue
			Write-Warning @"
			A valid GitHub token was not provided. You may encounter API rate limits.
			Please consider adding your token using the WINGET_PKGS_GITHUB_TOKEN environment variable.
			"@
			Write-Warning 'A different useful warning'
			$Script | Out-File -Path (Join-Path $script:TestDataFolder -ChildPath 'BoundParameterScript.ps1')
			""";
		string compatibleSandboxSample = OfficialTestAssets.CreateSandboxCompatibilityScript(officialSandboxSample);
		Assert(compatibleSandboxSample.Contains("Invoke-WebRequest -UseBasicParsing -Uri", StringComparison.Ordinal)
			&& compatibleSandboxSample.Contains("[System.IO.File]::WriteAllText", StringComparison.Ordinal)
			&& compatibleSandboxSample.Contains("$Script.ToString()", StringComparison.Ordinal)
			&& !compatibleSandboxSample.Contains("$Script | Out-File", StringComparison.Ordinal)
			&& compatibleSandboxSample.Contains("Write-Verbose @\"", StringComparison.Ordinal)
			&& compatibleSandboxSample.Contains("A valid GitHub token was not provided.", StringComparison.Ordinal)
			&& compatibleSandboxSample.Contains("Write-Warning 'A different useful warning'", StringComparison.Ordinal),
			"The official Sandbox compatibility copy must avoid legacy web prompts, repair the invalid Out-File parameter, and suppress only the optional token warning.");
		ProcessStartInfo parserStartInfo = new()
		{
			FileName = "powershell.exe",
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		};
		parserStartInfo.ArgumentList.Add("-NoLogo");
		parserStartInfo.ArgumentList.Add("-NoProfile");
		parserStartInfo.ArgumentList.Add("-NonInteractive");
		parserStartInfo.ArgumentList.Add("-Command");
		parserStartInfo.ArgumentList.Add("$source=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($env:WMS_SANDBOX_SCRIPT)); $tokens=$null; $errors=$null; [System.Management.Automation.Language.Parser]::ParseInput($source,[ref]$tokens,[ref]$errors)|Out-Null; if($errors.Count -gt 0){$errors|ForEach-Object{[Console]::Error.WriteLine($_.Message)}; exit 1}");
		parserStartInfo.Environment["WMS_SANDBOX_SCRIPT"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(script));
		using Process parser = Process.Start(parserStartInfo) ?? throw new InvalidOperationException("PowerShell syntax verification could not start.");
		string parserError = await parser.StandardError.ReadToEndAsync();
		await parser.WaitForExitAsync();
		Assert(parser.ExitCode == 0, "The generated Sandbox install-and-uninstall PowerShell did not parse: " + parserError);

		string binderRoot = Path.Combine(Path.GetTempPath(), "WingetManifestStudio-SandboxBinding-" + Guid.NewGuid().ToString("N"));
		try
		{
			string manifestFolder = Path.Combine(binderRoot, "manifest");
			string fakeSandboxTool = Path.Combine(binderRoot, "SandboxTest.ps1");
			string receivedScriptPath = Path.Combine(binderRoot, "BoundParameterScript.ps1");
			string receivedTypePath = Path.Combine(binderRoot, "received-type.txt");
			string basicParsingPath = Path.Combine(binderRoot, "basic-parsing.txt");
			Directory.CreateDirectory(manifestFolder);
			string fakeOfficialSandboxTool = """
				[CmdletBinding()]
				param(
				    [string] $Manifest,
				    [ScriptBlock] $Script,
				    [string] $MapFolder
				)
				function Invoke-WebRequest {
				    [CmdletBinding()]
				    param([switch] $UseBasicParsing)
				    return $UseBasicParsing.IsPresent
				}
				$script:TestDataFolder = $MapFolder
				$Script | Out-File -Path (Join-Path $script:TestDataFolder -ChildPath 'BoundParameterScript.ps1')
				[IO.File]::WriteAllText((Join-Path $MapFolder 'received-type.txt'), $Script.GetType().FullName, [Text.UTF8Encoding]::new($false))
				[IO.File]::WriteAllText((Join-Path $MapFolder 'basic-parsing.txt'), (Invoke-WebRequest).ToString(), [Text.UTF8Encoding]::new($false))
				exit 0
				""";
			File.WriteAllText(
				fakeSandboxTool,
				OfficialTestAssets.CreateSandboxCompatibilityScript(fakeOfficialSandboxTool),
				new UTF8Encoding(false));
			SandboxPowerShellInvocation invocation = WingetCommandService.CreateSandboxInstallUninstallInvocation(
				fakeSandboxTool,
				manifestFolder,
				binderRoot,
				script);
			Assert(!invocation.Arguments.Contains(script, StringComparer.Ordinal)
				&& invocation.Environment.TryGetValue("WMS_SANDBOX_VERIFICATION_SCRIPT", out string? encodedSandboxScript)
				&& encodedSandboxScript is not null
				&& Encoding.UTF8.GetString(Convert.FromBase64String(encodedSandboxScript)) == script,
				"The Sandbox verification script must cross the process boundary without becoming a plain -File argument.");

			ProcessStartInfo binderStartInfo = new()
			{
				FileName = "powershell.exe",
				WorkingDirectory = binderRoot,
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};
			foreach (string argument in invocation.Arguments) binderStartInfo.ArgumentList.Add(argument);
			foreach ((string name, string value) in invocation.Environment) binderStartInfo.Environment[name] = value;
			using Process binder = Process.Start(binderStartInfo)
				?? throw new InvalidOperationException("The Sandbox ScriptBlock binding verification could not start.");
			Task<string> binderOutputTask = binder.StandardOutput.ReadToEndAsync();
			Task<string> binderErrorTask = binder.StandardError.ReadToEndAsync();
			using CancellationTokenSource binderTimeout = new(TimeSpan.FromSeconds(15));
			await binder.WaitForExitAsync(binderTimeout.Token);
			string binderOutput = await binderOutputTask;
			string binderError = await binderErrorTask;
			Assert(binder.ExitCode == 0
				&& File.Exists(receivedScriptPath)
				&& File.ReadAllText(receivedScriptPath) == script
				&& File.ReadAllText(receivedTypePath) == "System.Management.Automation.ScriptBlock"
				&& File.ReadAllText(basicParsingPath).Equals("True", StringComparison.OrdinalIgnoreCase),
				"Microsoft's SandboxTest.ps1 must receive a real ScriptBlock and non-interactive web parsing. " + binderOutput + binderError);
		}
		finally
		{
			try { if (Directory.Exists(binderRoot)) Directory.Delete(binderRoot, true); } catch { }
		}
		results.Add("PASS: current-schema recommendation and package-independent Sandbox uninstall verification.");
	}

	private static void TestCredentialStatusCheck(List<string> results)
	{
		_ = WingetCommandService.IsGitHubTokenStored();
		results.Add("PASS: WingetCreate token status can be checked without reading token data.");
	}

	private static void TestGitHubReleaseParsing(List<string> results)
	{
		(string owner, string repository, string tag, bool latest) = GitHubReleaseService.ParseReleaseUrl(
			"https://github.com/AnyPublisher/AnyApplication/releases/tag/v2.4.1");
		Assert(owner == "AnyPublisher" && repository == "AnyApplication" && tag == "v2.4.1" && !latest,
			"GitHub release import must parse arbitrary owner, repository, and tag values.");
		(_, _, _, bool latestPage) = GitHubReleaseService.ParseReleaseUrl(
			"https://github.com/AnotherPublisher/DifferentTool/releases/latest");
		Assert(latestPage && GitHubReleaseService.IsSupportedInstallerAsset("DifferentTool-arm64.msixbundle")
			&& !GitHubReleaseService.IsSupportedInstallerAsset("checksums.txt"),
			"GitHub release import must support latest-release URLs and ignore non-installer assets.");
		results.Add("PASS: dynamic GitHub release URL and asset parsing.");
	}

	private static void TestStudioUpdater(List<string> results)
	{
		const string releaseJson = """
		{
		  "tag_name": "v9.8.7",
		  "name": "Winget Manifest Studio 9.8.7",
		  "body": "Updater verification release",
		  "html_url": "https://github.com/ubidzz/WingetManifestStudio/releases/tag/v9.8.7",
		  "published_at": "2026-08-27T10:00:00Z",
		  "draft": false,
		  "prerelease": false,
		  "assets": [
		    {
		      "name": "WingetManifestStudio.exe",
		      "browser_download_url": "https://github.com/ubidzz/WingetManifestStudio/releases/download/v9.8.7/WingetManifestStudio.exe",
		      "size": 120000000,
		      "digest": "sha256:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
		    },
		    {
		      "name": "StudioSetup.msi",
		      "browser_download_url": "https://github.com/ubidzz/WingetManifestStudio/releases/download/v9.8.7/StudioSetup.msi",
		      "size": 121000000,
		      "digest": "sha256:BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB"
		    }
		  ]
		}
		""";
		StudioUpdateRelease portable = StudioUpdateService.ParseReleaseJson(releaseJson, StudioDistributionKind.Portable)
			?? throw new InvalidOperationException("The stable updater test release was ignored.");
		StudioUpdateRelease installed = StudioUpdateService.ParseReleaseJson(releaseJson, StudioDistributionKind.MsiInstalled)
			?? throw new InvalidOperationException("The stable updater test release was ignored.");
		Assert(portable.Asset.Name == StudioUpdateService.PortableAssetName
			&& installed.Asset.Name == StudioUpdateService.MsiAssetName
			&& portable.Version == new Version(9, 8, 7, 0),
			"The updater must select the EXE for portable copies and StudioSetup.msi for installed copies.");
		Assert(StudioUpdateService.IsExecutableInInstallLocation(
			@"C:\Users\Sample\AppData\Local\Programs\Winget Manifest Studio\WingetManifestStudio.exe",
			@"C:\Users\Sample\AppData\Local\Programs\Winget Manifest Studio")
			&& !StudioUpdateService.IsExecutableInInstallLocation(@"D:\Portable\WingetManifestStudio.exe", @"C:\Programs\Winget Manifest Studio"),
			"MSI and portable distribution detection must be based on the running application path.");
		Assert(StudioUpdateService.TryParseVersion("v1.2.3", out Version? stable) && stable == new Version(1, 2, 3, 0),
			"Updater version tags must support the repository's v-prefixed release format.");
		ProcessStartInfo msi = StudioUpdateService.CreateMsiUpdateLauncher(@"C:\Updates\StudioSetup.msi");
		ProcessStartInfo portableLauncher = StudioUpdateService.CreatePortableUpdateLauncher(
			@"C:\Updates\WingetManifestStudio.exe", @"C:\Portable\WingetManifestStudio.exe", 1234);
		Assert(msi.FileName.Equals("msiexec.exe", StringComparison.OrdinalIgnoreCase)
			&& msi.Arguments.Contains("StudioSetup.msi", StringComparison.OrdinalIgnoreCase)
			&& portableLauncher.FileName.Equals("powershell.exe", StringComparison.OrdinalIgnoreCase)
			&& portableLauncher.ArgumentList.Contains("-EncodedCommand"),
			"Both distribution modes must create a non-interactive update launcher.");
		string prereleaseJson = releaseJson.Replace("\"prerelease\": false", "\"prerelease\": true", StringComparison.Ordinal);
		Assert(StudioUpdateService.ParseReleaseJson(prereleaseJson, StudioDistributionKind.Portable) is null,
			"Automatic updates must ignore prerelease and draft releases.");
		string untrustedJson = releaseJson.Replace(
			"https://github.com/ubidzz/WingetManifestStudio/releases/download/v9.8.7/WingetManifestStudio.exe",
			"https://example.invalid/WingetManifestStudio.exe", StringComparison.Ordinal);
		bool rejectedUntrustedAsset = false;
		try { _ = StudioUpdateService.ParseReleaseJson(untrustedJson, StudioDistributionKind.Portable); }
		catch (InvalidDataException) { rejectedUntrustedAsset = true; }
		Assert(rejectedUntrustedAsset, "The updater must reject files outside this repository's official HTTPS release path.");
		results.Add("PASS: stable GitHub updates select, verify, and launch the correct MSI or portable asset.");
	}

	private static async Task TestStudioUpdateFeedAsync(List<string> results)
	{
		StudioUpdateCheck check = await StudioUpdateService.CheckAsync(true);
		StudioUpdateRelease release = check.LatestRelease
			?? throw new InvalidOperationException("The live Studio update feed did not return a stable release.");
		Assert(release.Asset.Name is StudioUpdateService.PortableAssetName or StudioUpdateService.MsiAssetName,
			"The live Studio release feed must publish the exact update asset required by this distribution mode.");
		results.Add($"PASS: live Studio update feed returned stable release {release.Tag} and {release.Asset.Name}.");
	}

	private static async Task TestInstallerInspectionAsync(List<string> results)
	{
		string publishedExecutable = Path.Combine(AppContext.BaseDirectory, "WingetManifestStudio.exe");
		string executable = File.Exists(publishedExecutable)
			? publishedExecutable
			: Environment.ProcessPath ?? throw new InvalidOperationException("The self-test executable path is unavailable.");
		InstallerInspection inspection = await InstallerInspector.InspectAsync(executable, string.Empty);
		Assert(inspection.Sha256.Length == 64, "Installer inspection must calculate SHA-256.");
		Assert(inspection.InstallerType is "exe" or "inno" or "nullsoft" or "burn", "An executable must be identified as a supported EXE installer type.");
		Assert(inspection.Technology.Length > 0 && inspection.AnalysisNotes.Length > 0, "Executable inspection must explain the detected technology and safe next step.");
		results.Add("PASS: deep local executable inspection and hashing.");
	}

	private static async Task TestRealInstallerCorpusAsync(string root, List<string> results)
	{
		string executable = Environment.ProcessPath ?? throw new InvalidOperationException("The running executable path is unavailable.");
		InstallerInspection application = await InstallerInspector.InspectAsync(executable, string.Empty);
		Assert(application.Sha256.Length == 64 && application.Technology.Length > 0,
			"The real application executable must pass hashing and installer-technology analysis.");

		string archivePath = Path.Combine(root, "real-installer-corpus.zip");
		using (ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
			archive.CreateEntryFromFile(executable, "bin/" + Path.GetFileName(executable), CompressionLevel.NoCompression);
		InstallerInspection archiveInspection = await InstallerInspector.InspectAsync(archivePath, string.Empty);
		Assert(archiveInspection.InstallerType == "zip" && archiveInspection.NestedInstallerFiles.Contains(Path.GetFileName(executable), StringComparison.OrdinalIgnoreCase),
			"A ZIP containing a real executable must be analyzed as a nested installer package.");

		string signedSystemExecutable = Path.Combine(Environment.SystemDirectory, "notepad.exe");
		if (File.Exists(signedSystemExecutable))
		{
			InstallerInspection signed = await InstallerInspector.InspectAsync(signedSystemExecutable, string.Empty);
			Assert(signed.Signature.Status.Length > 0, "A real Windows executable must produce a completed signed-or-unsigned result.");
		}
		results.Add("PASS: real executable, signed-or-unsigned, and ZIP installer regression corpus.");
	}

	private static async Task TestInstallerTechnologyDetectionAsync(string root, List<string> results)
	{
		string source = Environment.ProcessPath ?? throw new InvalidOperationException("The running executable path is unavailable.");
		(string Marker, string Technology, string InstallerType)[] cases =
		[
			("Inno Setup Setup Data", "Inno Setup", "inno"),
			("NullsoftInst", "NSIS / Nullsoft", "nullsoft"),
			("WixBundle", "WiX Burn bundle", "burn"),
			("SquirrelSetup.log", "Squirrel.Windows", "exe"),
			("VelopackAsset", "Velopack", "exe")
		];
		foreach ((string marker, string technology, string installerType) in cases)
		{
			string target = Path.Combine(root, technology.Replace('/', '-') + ".exe");
			File.Copy(source, target);
			await File.AppendAllTextAsync(target, marker, Encoding.ASCII);
			InstallerInspection inspection = await InstallerInspector.InspectAsync(target, string.Empty);
			Assert(inspection.Technology == technology && inspection.InstallerType == installerType,
				$"EXE analysis must identify {technology} without changing its Winget-compatible installer type.");
		}
		results.Add("PASS: Inno, NSIS, Burn, Squirrel, and Velopack EXE technology detection.");
	}

	private static async Task TestPublicImportServicesAsync(string root, List<string> results)
	{
		RepositoryImportResult repository = await WingetRepositoryService.ImportLatestAsync(
			"Microsoft.PowerToys", Path.Combine(root, "public-repository-import"));
		ManifestProject imported = ManifestService.LoadProject(repository.ManifestFolder);
		Assert(imported.LoadedFromExistingManifests
			&& imported.PackageIdentifier == "Microsoft.PowerToys"
			&& imported.Installers.Count > 0,
			"Public package-ID import must download and populate a real current Winget manifest set.");
		GitHubReleaseImport release = await GitHubReleaseService.ReadAsync(
			"https://github.com/microsoft/PowerToys/releases/latest");
		Assert(release.Owner == "microsoft" && release.Repository == "PowerToys"
			&& release.Tag.Length > 0 && release.RepositoryUrl.Length > 0,
			"Public GitHub release import must read real repository and release metadata.");
		results.Add("PASS: live Winget package-ID and GitHub release import services.");
	}

	private static async Task TestOfficialGuidedSchemaAsync(string root, List<string> results)
	{
		WingetHealthResult health = await WingetCommandService.CheckWingetHealthAsync();
		if (!health.IsReady)
		{
			results.Add($"SKIP: official Winget schema validation is unavailable in this test session: {health.Message}");
			return;
		}
		ManifestProject project = SampleProject(Path.Combine(root, "official-guided-schema"));
		project.ManifestVersion = ManifestSchemaSupport.CurrentVersion;
		project.Agreements = "Terms | https://example.com/terms | Read before installing";
		project.Documentations = "User guide | https://example.com/docs";
		project.PackageDependencies = "Microsoft.VCRedist.2015+.x64 | 14.0.0";
		project.WindowsFeatures = "NetFx3";
		project.ExpectedReturnCodes = "1603 | contactSupport | https://example.com/support";
		project.UnsupportedArguments = "log, location";
		project.DefaultInstallLocation = "%ProgramFiles%\\Contoso\\Sample";
		project.InstalledFiles = "Sample.exe | launch | " + new string('A', 64) + " | --safe | Sample";
		ManifestGenerationResult generated = ManifestService.Generate(project);
		string? cleanFolder = null;
		try
		{
			cleanFolder = ManifestService.CreateCleanManifestFolder(generated);
			CommandResult validation = await WingetCommandService.ValidateManifestAsync(cleanFolder);
			Assert(WingetCommandService.ManifestValidationSucceeded(validation), "Official Winget validation rejected guided uncommon schema fields: " + validation.CombinedOutput);
		}
		finally
		{
			ManifestService.DeleteCleanManifestFolder(cleanFolder);
		}
		results.Add($"PASS: official Winget validation accepted guided uncommon schema fields with manifest schema {ManifestSchemaSupport.CurrentVersion}.");
	}

	private static async Task TestFontInspectionAsync(string root, List<string> results)
	{
		string font = Path.Combine(root, "AnyPublisher-AnyFont.ttf");
		File.WriteAllBytes(font, [0, 1, 0, 0, 0, 0, 0, 0]);
		InstallerInspection inspection = await InstallerInspector.InspectAsync(font, string.Empty);
		Assert(inspection.InstallerType == "font" && inspection.Architecture == "neutral" && inspection.Sha256.Length == 64,
			"Font release files must be accepted without assuming an x64 application installer.");
		results.Add("PASS: font packages inspect as neutral, package-independent installers.");
	}

	private static async Task TestZipInspectionAsync(string root, List<string> results)
	{
		string archivePath = Path.Combine(root, "AnyPublisher-AnyUtility.zip");
		using (ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
		{
			ZipArchiveEntry entry = archive.CreateEntry("release/AnyUtility.exe");
			using Stream content = entry.Open();
			content.Write([0x4d, 0x5a, 0, 0]);
		}
		InstallerInspection inspection = await InstallerInspector.InspectAsync(archivePath, string.Empty);
		Assert(inspection.InstallerType == "zip" && inspection.Architecture == "neutral"
			&& inspection.NestedInstallerType == "exe" && inspection.NestedInstallerFiles == "release\\AnyUtility.exe",
			"ZIP inspection must discover its nested installer without assuming a publisher, product, or architecture.");
		results.Add("PASS: ZIP inspection discovers package-independent nested installer paths.");
	}

	private static async Task TestRealInstallerAsync(string path, List<string> results)
	{
		Assert(File.Exists(path), "The supplied installer verification file does not exist.");
		InstallerInspection inspection = await InstallerInspector.InspectAsync(path, string.Empty);
		Assert(inspection.Sha256.Length == 64, "The supplied installer did not produce a SHA-256 hash.");
		Assert(inspection.Signature.Status.Length > 0, "The supplied installer did not produce a digital-signature result.");
		results.Add($"PASS: real installer inspection completed: {Path.GetFileName(path)}, {inspection.Architecture}, {inspection.InstallerType}, scope {inspection.Scope.IfEmpty("not declared")}, identity {(inspection.ProductCode.Length > 0 ? "found" : "not declared")}, {inspection.Signature.Status}.");
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
			Assert(WingetCommandService.ManifestValidationSucceeded(validation), "Official Winget validation failed: " + details);
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

	private static bool HasRootKey(YamlMappingNode mapping, string key) =>
		mapping.Children.Keys.OfType<YamlScalarNode>().Any(item => item.Value?.Equals(key, StringComparison.OrdinalIgnoreCase) == true);

	private static void WriteReport(IEnumerable<string> lines)
	{
		File.WriteAllLines(Path.Combine(AppContext.BaseDirectory, "self-test-report.txt"), lines);
	}
}
