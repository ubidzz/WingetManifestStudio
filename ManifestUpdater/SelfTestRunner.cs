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
			TestCleanValidationFolder(root, results);
			TestReleaseUrlSynchronization(results);
			TestProfileRoundTrip(root, results);
			await TestInstallerInspectionAsync(results);
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
    DisplayName: Sample
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
		string locale = generated.Files.Single(pair => pair.Key.Contains(".locale.", StringComparison.OrdinalIgnoreCase)).Value;
		string installer = generated.Files.Single(pair => pair.Key.Contains(".installer.", StringComparison.OrdinalIgnoreCase)).Value;
		Assert(locale.Contains("CustomLocaleField: KeepMe", StringComparison.Ordinal), "Unknown locale fields must be preserved.");
		Assert(installer.Contains("Commands:", StringComparison.Ordinal) && installer.Contains("- sample", StringComparison.Ordinal), "Commands must survive updates.");
		Assert(installer.Contains("UnsupportedOSArchitectures:", StringComparison.Ordinal), "Unknown root installer fields must be preserved.");
		Assert(installer.Contains("CustomInstallerField: KeepMe", StringComparison.Ordinal), "Unknown per-installer fields must be preserved.");
		Assert(installer.Contains("    - DisplayName: Sample", StringComparison.Ordinal), "AppsAndFeaturesEntries must remain a YAML sequence when an existing manifest is updated.");
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

	private static async Task TestInstallerInspectionAsync(List<string> results)
	{
		string executable = Environment.ProcessPath ?? throw new InvalidOperationException("The self-test executable path is unavailable.");
		InstallerInspection inspection = await InstallerInspector.InspectAsync(executable, string.Empty);
		Assert(inspection.Sha256.Length == 64, "Installer inspection must calculate SHA-256.");
		Assert(inspection.InstallerType == "exe", "An executable must be identified as an EXE installer.");
		results.Add("PASS: local installer inspection and hashing.");
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
