using System.Text;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace ManifestUpdater;

/// <summary>
/// Reads and updates Winget manifests as YAML node trees. Unknown keys, nested
/// mappings, sequences, aliases, and additional locale documents remain part of
/// the document instead of being reconstructed from the Studio's known fields.
/// </summary>
internal static class SchemaAwareYaml
{
	private static string[] KnownManifestTypes => ["version", "defaultLocale", "locale", "installer"];
	private static IReadOnlyList<string> ManagedLocaleFields => new string[]
	{
		"PackageIdentifier", "PackageVersion", "PackageLocale", "Publisher", "PublisherUrl",
		"PublisherSupportUrl", "PrivacyUrl", "Author", "PackageName", "PackageUrl", "License",
		"LicenseUrl", "Copyright", "CopyrightUrl", "ShortDescription", "Description", "Moniker",
		"Tags", "ReleaseNotes", "ReleaseNotesUrl", "PurchaseUrl", "InstallationNotes",
		"ManifestType", "ManifestVersion"
	};
	private static IReadOnlyList<string> ManagedInstallerFields => new string[]
	{
		"PackageIdentifier", "PackageVersion", "Channel", "InstallerLocale", "Platform",
		"MinimumOSVersion", "InstallerType", "NestedInstallerType", "Scope", "InstallModes",
		"InstallerSwitches", "InstallerSuccessCodes", "UpgradeBehavior", "Commands", "Protocols",
		"FileExtensions", "PackageFamilyName", "UnsupportedOSArchitectures", "ElevationRequirement",
		"InstallerAbortsTerminal", "ReleaseDate", "InstallLocationRequired", "RequireExplicitUpgrade",
		"DisplayInstallWarnings", "DownloadCommandProhibited", "RepairBehavior",
		"ArchiveBinariesDependOnPath", "Installers", "ManifestType", "ManifestVersion"
	};
	private static IReadOnlyList<string> ManagedInstallerNodeFields => new string[]
	{
		"Architecture", "InstallerUrl", "InstallerSha256", "SignatureSha256", "InstallerType",
		"NestedInstallerType", "Scope", "ProductCode", "AppsAndFeaturesEntries"
	};

	public static ManifestProject LoadProject(string folder, long maximumBytes)
	{
		List<ManifestDocument> documents = LoadSelectedDocuments(folder, maximumBytes, out string manifestFolder);
		if (documents.Count == 0)
			return new ManifestProject { ManifestFolder = folder };

		ManifestDocument? version = documents.FirstOrDefault(document => document.Type.Equals("version", StringComparison.OrdinalIgnoreCase));
		ManifestDocument? locale = documents.FirstOrDefault(document => document.Type.Equals("defaultLocale", StringComparison.OrdinalIgnoreCase));
		ManifestDocument? installer = documents.FirstOrDefault(document => document.Type.Equals("installer", StringComparison.OrdinalIgnoreCase));
		YamlMappingNode versionRoot = version?.Root ?? new YamlMappingNode();
		YamlMappingNode localeRoot = locale?.Root ?? new YamlMappingNode();
		YamlMappingNode installerRoot = installer?.Root ?? new YamlMappingNode();

		ManifestProject project = new()
		{
			ManifestFolder = manifestFolder,
			PackageIdentifier = FirstValue("PackageIdentifier", versionRoot, localeRoot, installerRoot),
			PackageVersion = FirstValue("PackageVersion", versionRoot, localeRoot, installerRoot),
			DefaultLocale = Value(versionRoot, "DefaultLocale").IfEmpty(Value(localeRoot, "PackageLocale")).IfEmpty("en-US"),
			ManifestVersion = FirstValue("ManifestVersion", versionRoot, localeRoot, installerRoot).IfEmpty("1.12.0"),
			Publisher = Value(localeRoot, "Publisher"),
			PublisherUrl = Value(localeRoot, "PublisherUrl"),
			PublisherSupportUrl = Value(localeRoot, "PublisherSupportUrl"),
			PrivacyUrl = Value(localeRoot, "PrivacyUrl"),
			Author = Value(localeRoot, "Author"),
			PackageName = Value(localeRoot, "PackageName"),
			PackageUrl = Value(localeRoot, "PackageUrl"),
			License = Value(localeRoot, "License"),
			LicenseUrl = Value(localeRoot, "LicenseUrl"),
			Copyright = Value(localeRoot, "Copyright"),
			CopyrightUrl = Value(localeRoot, "CopyrightUrl"),
			PurchaseUrl = Value(localeRoot, "PurchaseUrl"),
			ShortDescription = Value(localeRoot, "ShortDescription"),
			Description = Value(localeRoot, "Description"),
			Moniker = Value(localeRoot, "Moniker"),
			Tags = JoinList(localeRoot, "Tags"),
			ReleaseNotes = Value(localeRoot, "ReleaseNotes"),
			ReleaseNotesUrl = Value(localeRoot, "ReleaseNotesUrl"),
			InstallationNotes = Value(localeRoot, "InstallationNotes"),
			Channel = Value(installerRoot, "Channel"),
			InstallerLocale = Value(installerRoot, "InstallerLocale"),
			Platform = JoinList(installerRoot, "Platform").IfEmpty("Windows.Desktop"),
			MinimumOSVersion = Value(installerRoot, "MinimumOSVersion"),
			InstallerType = Value(installerRoot, "InstallerType").IfEmpty("exe"),
			NestedInstallerType = Value(installerRoot, "NestedInstallerType"),
			Scope = Value(installerRoot, "Scope").IfEmpty("user"),
			InstallModes = JoinList(installerRoot, "InstallModes").IfEmpty("interactive, silent, silentWithProgress"),
			UpgradeBehavior = Value(installerRoot, "UpgradeBehavior").IfEmpty("install"),
			ElevationRequirement = Value(installerRoot, "ElevationRequirement"),
			Protocols = JoinList(installerRoot, "Protocols"),
			FileExtensions = JoinList(installerRoot, "FileExtensions"),
			UnsupportedOSArchitectures = JoinList(installerRoot, "UnsupportedOSArchitectures"),
			InstallerSuccessCodes = JoinList(installerRoot, "InstallerSuccessCodes"),
			PackageFamilyName = Value(installerRoot, "PackageFamilyName"),
			ReleaseDate = Value(installerRoot, "ReleaseDate"),
			InstallerAbortsTerminal = Value(installerRoot, "InstallerAbortsTerminal"),
			InstallLocationRequired = Value(installerRoot, "InstallLocationRequired"),
			RequireExplicitUpgrade = Value(installerRoot, "RequireExplicitUpgrade"),
			DisplayInstallWarnings = Value(installerRoot, "DisplayInstallWarnings"),
			DownloadCommandProhibited = Value(installerRoot, "DownloadCommandProhibited"),
			RepairBehavior = Value(installerRoot, "RepairBehavior"),
			ArchiveBinariesDependOnPath = Value(installerRoot, "ArchiveBinariesDependOnPath"),
			Commands = JoinList(installerRoot, "Commands"),
			LoadedFromExistingManifests = version is not null || locale is not null || installer is not null
		};
		project.ProfileName = project.PackageIdentifier.IfEmpty(project.PackageName).IfEmpty("Imported package");
		ReadInstallerSwitches(installerRoot, project);
		foreach (InstallerArtifact artifact in ReadInstallers(installerRoot, project))
			project.Installers.Add(artifact);
		return project;
	}

	public static ManifestGenerationResult Generate(ManifestProject project, long maximumBytes)
	{
		return GenerateCore(project, maximumBytes);
	}

	private static ManifestGenerationResult GenerateCore(ManifestProject project, long maximumBytes)
	{
		List<ManifestDocument> documents = LoadGenerationDocuments(project, maximumBytes);
		GenerationWorkspace workspace = CreateGenerationWorkspace(project, documents);
		PatchGenerationDocuments(workspace, project);
		Dictionary<string, string> files = EmitGenerationDocuments(workspace, project);
		List<string> changes = BuildGenerationChanges(workspace, project, files.Count);
		List<string> warnings = BuildGenerationWarnings(workspace, project);
		return new ManifestGenerationResult(files, changes, warnings);
	}

	private static List<ManifestDocument> LoadGenerationDocuments(ManifestProject project, long maximumBytes)
	{
		List<ManifestDocument> documents = LoadDocuments(project.ManifestFolder, maximumBytes);
		EnsureSingleDocument(documents, "version");
		EnsureSingleDocument(documents, "defaultLocale");
		EnsureSingleDocument(documents, "installer");
		return documents;
	}

	private static GenerationWorkspace CreateGenerationWorkspace(ManifestProject project, List<ManifestDocument> documents)
	{
		GenerationWorkspace workspace = new();
		workspace.Documents = documents;
		PopulateExistingDocuments(workspace, documents);
		workspace.Version = GetVersionDocument(workspace.ExistingVersion, project);
		workspace.Locale = GetLocaleDocument(workspace.ExistingLocale, project);
		workspace.Installer = GetInstallerDocument(workspace.ExistingInstaller, project);
		return workspace;
	}

	private static void PopulateExistingDocuments(GenerationWorkspace workspace, List<ManifestDocument> documents)
	{
		ManifestDocument? existingVersion = FirstDocument(documents, "version");
		ManifestDocument? existingLocale = FirstDocument(documents, "defaultLocale");
		ManifestDocument? existingInstaller = FirstDocument(documents, "installer");

		workspace.ExistingVersion = existingVersion;
		workspace.ExistingLocale = existingLocale;
		workspace.ExistingInstaller = existingInstaller;
		workspace.PreviousIdentifier = existingVersion is null ? string.Empty : Value(existingVersion.Root, "PackageIdentifier");
		workspace.PreviousVersion = existingVersion is null ? string.Empty : Value(existingVersion.Root, "PackageVersion");
	}

	private static ManifestDocument GetVersionDocument(ManifestDocument? existing, ManifestProject project)
	{
		if (existing is not null) return existing;
		string path = Path.Combine(project.ManifestFolder, project.PackageIdentifier + ".yaml");
		return NewDocument(path, "version", new YamlMappingNode());
	}

	private static ManifestDocument GetLocaleDocument(ManifestDocument? existing, ManifestProject project)
	{
		if (existing is not null) return existing;
		string path = Path.Combine(project.ManifestFolder, project.PackageIdentifier + ".locale." + project.DefaultLocale + ".yaml");
		return NewDocument(path, "defaultLocale", new YamlMappingNode());
	}

	private static ManifestDocument GetInstallerDocument(ManifestDocument? existing, ManifestProject project)
	{
		if (existing is not null) return existing;
		string path = Path.Combine(project.ManifestFolder, project.PackageIdentifier + ".installer.yaml");
		return NewDocument(path, "installer", new YamlMappingNode());
	}

	private static void PatchGenerationDocuments(GenerationWorkspace workspace, ManifestProject project)
	{
		PatchVersion(workspace.Version.Root, project);
		PatchLocale(workspace.Locale.Root, project);
		PatchInstaller(workspace.Installer.Root, project);
	}

	private static Dictionary<string, string> EmitGenerationDocuments(GenerationWorkspace workspace, ManifestProject project)
	{
		Dictionary<string, string> files = new(StringComparer.OrdinalIgnoreCase);
		files[Path.GetFileName(workspace.Version.Path)] = Emit(workspace.Version.Root, "version", project.ManifestVersion);
		files[Path.GetFileName(workspace.Locale.Path)] = Emit(workspace.Locale.Root, "defaultLocale", project.ManifestVersion);
		files[Path.GetFileName(workspace.Installer.Path)] = Emit(workspace.Installer.Root, "installer", project.ManifestVersion);

		foreach (ManifestDocument additionalLocale in workspace.Documents.Where(document => document.Type.Equals("locale", StringComparison.OrdinalIgnoreCase)))
		{
			SetRequiredScalar(additionalLocale.Root, "PackageIdentifier", project.PackageIdentifier);
			SetRequiredScalar(additionalLocale.Root, "PackageVersion", project.PackageVersion);
			SetRequiredScalar(additionalLocale.Root, "ManifestType", "locale");
			SetRequiredScalar(additionalLocale.Root, "ManifestVersion", project.ManifestVersion);
			files[Path.GetFileName(additionalLocale.Path)] = Emit(additionalLocale.Root, "locale", project.ManifestVersion);
		}
		return files;
	}

	private static List<string> BuildGenerationChanges(GenerationWorkspace workspace, ManifestProject project, int fileCount)
	{
		List<string> changes = new();
		changes.Add(!string.IsNullOrWhiteSpace(workspace.PreviousIdentifier) && !workspace.PreviousIdentifier.Equals(project.PackageIdentifier, StringComparison.Ordinal)
			? $"Package identifier: {workspace.PreviousIdentifier}  →  {project.PackageIdentifier}"
			: $"Package: {project.PackageIdentifier}");
		changes.Add(!string.IsNullOrWhiteSpace(workspace.PreviousVersion) && !workspace.PreviousVersion.Equals(project.PackageVersion, StringComparison.Ordinal)
			? $"Release version: {workspace.PreviousVersion}  →  {project.PackageVersion}"
			: $"Release version: {project.PackageVersion}");
		changes.Add($"Default language: {project.DefaultLocale}");
		changes.Add($"Installers: {project.Installers.Count} ({string.Join(", ", project.Installers.Select(item => item.Architecture.IfEmpty("architecture not set")).Distinct(StringComparer.OrdinalIgnoreCase))})");
		changes.Add(workspace.ExistingVersion is null ? $"Create {Path.GetFileName(workspace.Version.Path)}." : $"Update {Path.GetFileName(workspace.Version.Path)} with structural YAML preservation.");
		changes.Add(workspace.ExistingLocale is null ? $"Create {Path.GetFileName(workspace.Locale.Path)}." : $"Update {Path.GetFileName(workspace.Locale.Path)} while preserving unknown fields.");
		changes.Add(workspace.ExistingInstaller is null ? $"Create {Path.GetFileName(workspace.Installer.Path)}." : $"Update {Path.GetFileName(workspace.Installer.Path)} and match installer fields by identity.");
		if (fileCount > 3) changes.Add($"Update and preserve {fileCount - 3} additional locale manifest(s).");
		return changes;
	}

	private static List<string> BuildGenerationWarnings(GenerationWorkspace workspace, ManifestProject project)
	{
		List<string> warnings = new();
		if (workspace.Documents.Count > 0)
			warnings.Add("YAML comments and visual spacing may be normalized; all parsed keys and nested values are preserved structurally.");
		if (project.Installers.Any(item => string.IsNullOrWhiteSpace(item.LocalFile)))
			warnings.Add("At least one installer is URL-only. Its recorded hash was not compared with a local release file during this run.");
		if (project.AllowInsecureUrls)
			warnings.Add("Unsecured HTTP installer URLs are allowed for this project.");
		return warnings;
	}

	public static IReadOnlyList<string> ValidateAdvancedFields(ManifestProject project)
	{
		List<string> errors = [];
		if (!string.IsNullOrWhiteSpace(project.AdvancedLocaleFieldsYaml))
			ValidateAdvancedMapping(project.AdvancedLocaleFieldsYaml, ManagedLocaleFields, "Additional locale fields", errors);
		if (!string.IsNullOrWhiteSpace(project.AdvancedInstallerFieldsYaml))
			ValidateAdvancedMapping(project.AdvancedInstallerFieldsYaml, ManagedInstallerFields, "Additional installer fields", errors);
		for (int index = 0; index < project.Installers.Count; index++)
			if (!string.IsNullOrWhiteSpace(project.Installers[index].AdvancedFieldsYaml))
				ValidateAdvancedMapping(project.Installers[index].AdvancedFieldsYaml, ManagedInstallerNodeFields, $"Installer {index + 1} additional fields", errors);
		return errors;
	}

	private static void PatchVersion(YamlMappingNode root, ManifestProject project)
	{
		SetRequiredScalar(root, "PackageIdentifier", project.PackageIdentifier);
		SetRequiredScalar(root, "PackageVersion", project.PackageVersion);
		SetRequiredScalar(root, "DefaultLocale", project.DefaultLocale);
		SetRequiredScalar(root, "ManifestType", "version");
		SetRequiredScalar(root, "ManifestVersion", project.ManifestVersion);
	}

	private static void PatchLocale(YamlMappingNode root, ManifestProject project)
	{
		SetRequiredScalar(root, "PackageIdentifier", project.PackageIdentifier);
		SetRequiredScalar(root, "PackageVersion", project.PackageVersion);
		SetRequiredScalar(root, "PackageLocale", project.DefaultLocale);
		SetRequiredScalar(root, "Publisher", project.Publisher);
		SetOptionalScalar(root, "PublisherUrl", project.PublisherUrl);
		SetOptionalScalar(root, "PublisherSupportUrl", project.PublisherSupportUrl);
		SetOptionalScalar(root, "PrivacyUrl", project.PrivacyUrl);
		SetOptionalScalar(root, "Author", project.Author);
		SetRequiredScalar(root, "PackageName", project.PackageName);
		SetOptionalScalar(root, "PackageUrl", project.PackageUrl);
		SetRequiredScalar(root, "License", project.License);
		SetOptionalScalar(root, "LicenseUrl", project.LicenseUrl);
		SetOptionalScalar(root, "Copyright", project.Copyright);
		SetOptionalScalar(root, "CopyrightUrl", project.CopyrightUrl);
		SetRequiredScalar(root, "ShortDescription", project.ShortDescription);
		SetOptionalScalar(root, "Description", project.Description, ScalarStyle.Literal);
		SetOptionalScalar(root, "Moniker", project.Moniker);
		SetList(root, "Tags", Split(project.Tags));
		SetOptionalScalar(root, "ReleaseNotes", project.ReleaseNotes, ScalarStyle.Literal);
		SetOptionalScalar(root, "ReleaseNotesUrl", project.ReleaseNotesUrl);
		SetOptionalScalar(root, "PurchaseUrl", project.PurchaseUrl);
		SetOptionalScalar(root, "InstallationNotes", project.InstallationNotes, ScalarStyle.Literal);
		if (!string.IsNullOrWhiteSpace(project.AdvancedLocaleFieldsYaml))
			MergeAdvancedFields(root, project.AdvancedLocaleFieldsYaml, ManagedLocaleFields, "additional locale fields");
		SetRequiredScalar(root, "ManifestType", "defaultLocale");
		SetRequiredScalar(root, "ManifestVersion", project.ManifestVersion);
	}

	private static void PatchInstaller(YamlMappingNode root, ManifestProject project)
	{
		SetRequiredScalar(root, "PackageIdentifier", project.PackageIdentifier);
		SetRequiredScalar(root, "PackageVersion", project.PackageVersion);
		SetOptionalScalar(root, "Channel", project.Channel);
		SetOptionalScalar(root, "InstallerLocale", project.InstallerLocale);
		SetList(root, "Platform", Split(project.Platform));
		SetOptionalScalar(root, "MinimumOSVersion", project.MinimumOSVersion);
		SetRequiredScalar(root, "InstallerType", project.InstallerType);
		SetOptionalScalar(root, "NestedInstallerType", project.NestedInstallerType);
		SetOptionalScalar(root, "Scope", project.Scope);
		SetList(root, "InstallModes", Split(project.InstallModes));
		SetOptionalScalar(root, "UpgradeBehavior", project.UpgradeBehavior);
		SetList(root, "Commands", Split(project.Commands));
		SetList(root, "Protocols", Split(project.Protocols));
		SetList(root, "FileExtensions", Split(project.FileExtensions));
		SetList(root, "UnsupportedOSArchitectures", Split(project.UnsupportedOSArchitectures));
		SetIntegerList(root, "InstallerSuccessCodes", Split(project.InstallerSuccessCodes));
		SetOptionalScalar(root, "PackageFamilyName", project.PackageFamilyName);
		SetOptionalScalar(root, "ElevationRequirement", project.ElevationRequirement);
		SetOptionalScalar(root, "InstallerAbortsTerminal", project.InstallerAbortsTerminal);
		SetOptionalScalar(root, "ReleaseDate", project.ReleaseDate);
		SetOptionalScalar(root, "InstallLocationRequired", project.InstallLocationRequired);
		SetOptionalScalar(root, "RequireExplicitUpgrade", project.RequireExplicitUpgrade);
		SetOptionalScalar(root, "DisplayInstallWarnings", project.DisplayInstallWarnings);
		SetOptionalScalar(root, "DownloadCommandProhibited", project.DownloadCommandProhibited);
		SetOptionalScalar(root, "RepairBehavior", project.RepairBehavior);
		SetOptionalScalar(root, "ArchiveBinariesDependOnPath", project.ArchiveBinariesDependOnPath);
		PatchInstallerSwitches(root, project);
		PatchInstallerNodes(root, project);
		if (!string.IsNullOrWhiteSpace(project.AdvancedInstallerFieldsYaml))
			MergeAdvancedFields(root, project.AdvancedInstallerFieldsYaml, ManagedInstallerFields, "additional installer fields");
		SetRequiredScalar(root, "ManifestType", "installer");
		SetRequiredScalar(root, "ManifestVersion", project.ManifestVersion);
	}

	private static void PatchInstallerSwitches(YamlMappingNode root, ManifestProject project)
	{
		YamlMappingNode switches = Mapping(root, "InstallerSwitches") ?? new YamlMappingNode();
		SetOptionalScalar(switches, "Silent", project.SwitchSilent);
		SetOptionalScalar(switches, "SilentWithProgress", project.SwitchSilentWithProgress);
		SetOptionalScalar(switches, "Interactive", project.SwitchInteractive);
		SetOptionalScalar(switches, "InstallLocation", project.SwitchInstallLocation);
		SetOptionalScalar(switches, "Log", project.SwitchLog);
		SetOptionalScalar(switches, "Upgrade", project.SwitchUpgrade);
		SetOptionalScalar(switches, "Custom", project.CustomInstallerSwitch);
		SetOptionalScalar(switches, "Repair", project.SwitchRepair);
		if (switches.Children.Count == 0) Remove(root, "InstallerSwitches");
		else SetNode(root, "InstallerSwitches", switches);
	}

	private static void PatchInstallerNodes(YamlMappingNode root, ManifestProject project)
	{
		List<YamlMappingNode> existing = Sequence(root, "Installers")?.Children.OfType<YamlMappingNode>().ToList() ?? [];
		HashSet<YamlMappingNode> used = new(ReferenceEqualityComparer.Instance);
		YamlSequenceNode result = new();
		foreach (InstallerArtifact artifact in project.Installers)
		{
			YamlMappingNode node = FindBestInstallerNode(existing, used, artifact, project) ?? new YamlMappingNode();
			used.Add(node);
			SetRequiredScalar(node, "Architecture", artifact.Architecture);
			SetRequiredScalar(node, "InstallerUrl", artifact.InstallerUrl);
			SetRequiredScalar(node, "InstallerSha256", artifact.Sha256.ToUpperInvariant());
			SetOptionalOverride(node, "InstallerType", artifact.InstallerType, project.InstallerType);
			SetOptionalOverride(node, "Scope", artifact.Scope, project.Scope);
			SetOptionalScalar(node, "ProductCode", artifact.ProductCode.ToUpperInvariant());
			SetOptionalScalar(node, "SignatureSha256", artifact.SignatureSha256.ToUpperInvariant());
			PatchAppsAndFeatures(node, artifact, project);
			if (!string.IsNullOrWhiteSpace(artifact.AdvancedFieldsYaml))
				MergeAdvancedFields(node, artifact.AdvancedFieldsYaml, ManagedInstallerNodeFields, "installer row fields");
			result.Add(node);
		}
		SetNode(root, "Installers", result);
	}

	private static void PatchAppsAndFeatures(YamlMappingNode installer, InstallerArtifact artifact, ManifestProject project)
	{
		YamlSequenceNode? entries = Sequence(installer, "AppsAndFeaturesEntries");
		YamlMappingNode? entry = entries?.Children.OfType<YamlMappingNode>().FirstOrDefault();
		bool hasData = !string.IsNullOrWhiteSpace(artifact.DisplayName) || !string.IsNullOrWhiteSpace(artifact.Publisher)
			|| !string.IsNullOrWhiteSpace(artifact.ProductVersion) || !string.IsNullOrWhiteSpace(artifact.ProductCode)
			|| !string.IsNullOrWhiteSpace(artifact.UpgradeCode);
		if (!hasData && entry is null) return;
		entries ??= new YamlSequenceNode();
		entry ??= new YamlMappingNode();
		if (entries.Children.Count == 0) entries.Add(entry);
		SetOptionalScalar(entry, "DisplayName", artifact.DisplayName.IfEmpty(project.PackageName));
		SetOptionalScalar(entry, "Publisher", artifact.Publisher);
		SetOptionalScalar(entry, "DisplayVersion", artifact.ProductVersion);
		SetOptionalScalar(entry, "ProductCode", artifact.ProductCode.ToUpperInvariant());
		SetOptionalScalar(entry, "UpgradeCode", artifact.UpgradeCode.ToUpperInvariant());
		SetNode(installer, "AppsAndFeaturesEntries", entries);
	}

	private static YamlMappingNode? FindBestInstallerNode(
		IEnumerable<YamlMappingNode> existing,
		ISet<YamlMappingNode> used,
		InstallerArtifact artifact,
		ManifestProject project)
	{
		YamlMappingNode? best = null;
		int bestScore = 0;
		foreach (YamlMappingNode candidate in existing.Where(item => !used.Contains(item)))
		{
			int score = 0;
			string candidateProduct = Value(candidate, "ProductCode");
			string candidateUrl = Value(candidate, "InstallerUrl");
			string candidateArchitecture = Value(candidate, "Architecture");
			string candidateType = Value(candidate, "InstallerType").IfEmpty(project.InstallerType);
			string candidateScope = Value(candidate, "Scope").IfEmpty(project.Scope);
			if (SameNonEmpty(candidateProduct, artifact.ProductCode)) score += 1000;
			if (SameNonEmpty(candidateUrl, artifact.InstallerUrl)) score += 500;
			if (SameNonEmpty(candidateArchitecture, artifact.Architecture)) score += 100;
			if (SameNonEmpty(candidateType, artifact.InstallerType.IfEmpty(project.InstallerType))) score += 30;
			if (SameNonEmpty(candidateScope, artifact.Scope.IfEmpty(project.Scope))) score += 20;
			if (score > bestScore) { best = candidate; bestScore = score; }
		}
		return bestScore >= 100 ? best : null;
	}

	private static IEnumerable<InstallerArtifact> ReadInstallers(YamlMappingNode root, ManifestProject project)
	{
		YamlSequenceNode? sequence = Sequence(root, "Installers");
		if (sequence is null) yield break;
		foreach (YamlMappingNode node in sequence.Children.OfType<YamlMappingNode>())
		{
			YamlMappingNode? app = Sequence(node, "AppsAndFeaturesEntries")?.Children.OfType<YamlMappingNode>().FirstOrDefault();
			yield return new InstallerArtifact
			{
				Architecture = Value(node, "Architecture").IfEmpty("x64"),
				InstallerType = Value(node, "InstallerType").IfEmpty(project.InstallerType),
				Scope = Value(node, "Scope").IfEmpty(project.Scope),
				InstallerUrl = Value(node, "InstallerUrl"),
				Sha256 = Value(node, "InstallerSha256"),
				SignatureSha256 = Value(node, "SignatureSha256"),
				ProductCode = Value(node, "ProductCode").IfEmpty(app is null ? string.Empty : Value(app, "ProductCode")),
				UpgradeCode = app is null ? string.Empty : Value(app, "UpgradeCode"),
				ProductVersion = app is null ? string.Empty : Value(app, "DisplayVersion"),
				DisplayName = app is null ? string.Empty : Value(app, "DisplayName"),
				Publisher = app is null ? string.Empty : Value(app, "Publisher"),
				VerificationStatus = "Loaded from manifest • hash not rechecked",
				SignatureStatus = "Loaded from manifest • signature not rechecked"
			};
		}
	}

	private static void ReadInstallerSwitches(YamlMappingNode root, ManifestProject project)
	{
		YamlMappingNode? switches = Mapping(root, "InstallerSwitches");
		if (switches is null) return;
		project.SwitchSilent = Value(switches, "Silent");
		project.SwitchSilentWithProgress = Value(switches, "SilentWithProgress");
		project.SwitchInteractive = Value(switches, "Interactive");
		project.SwitchInstallLocation = Value(switches, "InstallLocation");
		project.SwitchLog = Value(switches, "Log");
		project.SwitchUpgrade = Value(switches, "Upgrade");
		project.CustomInstallerSwitch = Value(switches, "Custom");
		project.SwitchRepair = Value(switches, "Repair");
	}

	private static List<ManifestDocument> LoadSelectedDocuments(string folder, long maximumBytes, out string manifestFolder)
	{
		manifestFolder = folder;
		List<ManifestDocument> directDocuments = LoadDocuments(folder, maximumBytes);
		if (directDocuments.Count > 0 || string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
			return directDocuments;

		Dictionary<string, List<ManifestDocument>> documentsByFolder = new(StringComparer.OrdinalIgnoreCase);
		EnumerationOptions options = new()
		{
			RecurseSubdirectories = true,
			IgnoreInaccessible = true,
			AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.System
		};
		int inspectedYamlFiles = 0;
		foreach (string path in Directory.EnumerateFiles(folder, "*.*", options))
		{
			string extension = Path.GetExtension(path);
			if (!extension.Equals(".yaml", StringComparison.OrdinalIgnoreCase) &&
				!extension.Equals(".yml", StringComparison.OrdinalIgnoreCase))
				continue;
			if (IsIgnoredDiscoveryPath(folder, path)) continue;
			if (++inspectedYamlFiles > 5000)
				throw new InvalidDataException("This folder contains too many YAML files to search safely. Choose the package or version folder that contains the Winget manifests.");

			ManifestDocument? document;
			try { document = ParseManifestDocument(path, maximumBytes); }
			catch (InvalidDataException) { continue; }
			if (document is null) continue;
			string documentFolder = Path.GetDirectoryName(path) ?? folder;
			if (!documentsByFolder.TryGetValue(documentFolder, out List<ManifestDocument>? group))
			{
				group = [];
				documentsByFolder[documentFolder] = group;
			}
			group.Add(document);
		}

		List<KeyValuePair<string, List<ManifestDocument>>> candidates = documentsByFolder.ToList();
		if (candidates.Count == 0) return directDocuments;
		if (candidates.Count == 1)
		{
			manifestFolder = candidates[0].Key;
			return candidates[0].Value;
		}

		List<KeyValuePair<string, List<ManifestDocument>>> completeCandidates = candidates
			.Where(candidate => candidate.Value.Any(document => document.Type.Equals("version", StringComparison.OrdinalIgnoreCase))
				&& candidate.Value.Any(document => document.Type.Equals("defaultLocale", StringComparison.OrdinalIgnoreCase))
				&& candidate.Value.Any(document => document.Type.Equals("installer", StringComparison.OrdinalIgnoreCase)))
			.ToList();
		if (completeCandidates.Count == 1)
		{
			manifestFolder = completeCandidates[0].Key;
			return completeCandidates[0].Value;
		}

		IEnumerable<KeyValuePair<string, List<ManifestDocument>>> shownCandidates = completeCandidates.Count > 0 ? completeCandidates : candidates;
		string choices = string.Join(Environment.NewLine, shownCandidates.Take(6)
			.Select(candidate => "• " + Path.GetRelativePath(folder, candidate.Key)));
		if (shownCandidates.Count() > 6) choices += Environment.NewLine + "• ...";
		throw new InvalidDataException("More than one Winget manifest set was found. Choose the specific package or version folder you want to edit:" + Environment.NewLine + Environment.NewLine + choices);
	}

	private static bool IsIgnoredDiscoveryPath(string root, string path)
	{
		string relative = Path.GetRelativePath(root, path);
		return relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
			.Any(part => part.Equals(".manifest-backups", StringComparison.OrdinalIgnoreCase)
				|| part.Equals(".git", StringComparison.OrdinalIgnoreCase));
	}

	private static List<ManifestDocument> LoadDocuments(string folder, long maximumBytes)
	{
		List<ManifestDocument> result = new();
		if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder)) return result;

		List<string> paths = GetManifestPaths(folder);
		foreach (string path in paths)
		{
			ManifestDocument? document = ParseManifestDocument(path, maximumBytes);
			if (document is not null) result.Add(document);
		}
		return result;
	}

	private static List<string> GetManifestPaths(string folder)
	{
		List<string> paths = new();
		paths.AddRange(Directory.GetFiles(folder, "*.yaml", SearchOption.TopDirectoryOnly));
		paths.AddRange(Directory.GetFiles(folder, "*.yml", SearchOption.TopDirectoryOnly));
		paths.Sort(StringComparer.OrdinalIgnoreCase);
		return paths;
	}

	private static ManifestDocument? ParseManifestDocument(string path, long maximumBytes)
	{
		FileInfo info = new(path);
		if (info.Length > maximumBytes)
			throw new InvalidDataException($"{info.Name} is too large to be a Winget manifest ({info.Length / (1024d * 1024):0.0} MB).");

		YamlStream stream = new();
		try
		{
			using (StreamReader reader = File.OpenText(path))
			{
				stream.Load(reader);
			}
		}
		catch (YamlException ex)
		{
			throw new InvalidDataException($"{info.Name} is not valid YAML near line {ex.Start.Line}, column {ex.Start.Column}: {ex.Message}", ex);
		}

		if (stream.Documents.Count != 1 || stream.Documents[0].RootNode is not YamlMappingNode root) return null;
		string type = Value(root, "ManifestType");
		return IsKnownManifestType(type) ? new ManifestDocument(path, type, root) : null;
	}

	private static bool IsKnownManifestType(string type)
	{
		string[] knownTypes = KnownManifestTypes;
		for (int index = 0; index < knownTypes.Length; index++)
			if (knownTypes[index].Equals(type, StringComparison.OrdinalIgnoreCase)) return true;
		return false;
	}

	private static ManifestDocument NewDocument(string path, string type, YamlMappingNode root)
	{
		return new ManifestDocument(path, type, root);
	}

	private static ManifestDocument? FirstDocument(IEnumerable<ManifestDocument> documents, string type)
	{
		foreach (ManifestDocument document in documents)
			if (document.Type.Equals(type, StringComparison.OrdinalIgnoreCase)) return document;
		return null;
	}

	private static void EnsureSingleDocument(IEnumerable<ManifestDocument> documents, string type)
	{
		List<string> paths = new();
		foreach (ManifestDocument document in documents)
			if (document.Type.Equals(type, StringComparison.OrdinalIgnoreCase)) paths.Add(Path.GetFileName(document.Path));
		if (paths.Count > 1)
			throw new InvalidDataException($"The folder contains more than one {type} manifest: {string.Join(", ", paths)}. Keep one manifest of this type before continuing.");
	}

	private static string Emit(YamlMappingNode root, string type, string schemaVersion)
	{
		YamlStream stream = new(new YamlDocument(root));
		StringBuilder buffer = new();
		using (StringWriter writer = new(buffer)) stream.Save(writer, assignAnchors: false);
		string emitted = buffer.ToString().Replace("\r\n", "\n");
		if (emitted.StartsWith("---\n", StringComparison.Ordinal)) emitted = emitted[4..];
		if (emitted.EndsWith("...\n", StringComparison.Ordinal)) emitted = emitted[..^4];
		string schemaType = type.Equals("locale", StringComparison.OrdinalIgnoreCase) ? "locale" : type;
		return $"# Created or updated with Winget Manifest Studio\n# yaml-language-server: $schema=https://aka.ms/winget-manifest.{schemaType}.{schemaVersion}.schema.json\n\n{emitted.TrimEnd()}\n"
			.Replace("\n", Environment.NewLine);
	}

	private static void MergeAdvancedFields(YamlMappingNode target, string yaml, IReadOnlyList<string> managed, string description)
	{
		if (string.IsNullOrWhiteSpace(yaml)) return;
		YamlStream stream = new();
		try
		{
			using StringReader reader = new(yaml);
			stream.Load(reader);
		}
		catch (YamlException ex)
		{
			throw new InvalidDataException($"The {description} are not valid YAML near line {ex.Start.Line}, column {ex.Start.Column}: {ex.Message}", ex);
		}
		if (stream.Documents.Count != 1 || stream.Documents[0].RootNode is not YamlMappingNode mapping)
			throw new InvalidDataException($"The {description} must be a YAML mapping made of Field: value entries.");
		foreach ((YamlNode key, YamlNode value) in mapping.Children)
		{
			string keyText = (key as YamlScalarNode)?.Value ?? string.Empty;
			if (keyText.Length == 0 || IsManagedField(managed, keyText)) continue;
			SetNode(target, keyText, value);
		}
	}

	private static void ValidateAdvancedMapping(string yaml, IReadOnlyList<string> managed, string description, ICollection<string> errors)
	{
		if (string.IsNullOrWhiteSpace(yaml)) return;
		try
		{
			YamlMappingNode target = new();
			MergeAdvancedFields(target, yaml, managed, description);
		}
		catch (Exception ex) when (ex is InvalidDataException or YamlException)
		{
			errors.Add(description + ": " + ex.Message);
		}
	}

	private static bool IsManagedField(IReadOnlyList<string> managed, string field)
	{
		for (int index = 0; index < managed.Count; index++)
			if (managed[index].Equals(field, StringComparison.OrdinalIgnoreCase)) return true;
		return false;
	}

	private static string FirstValue(string key, params YamlMappingNode[] mappings) =>
		mappings.Select(mapping => Value(mapping, key)).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

	private static string Value(YamlMappingNode mapping, string key)
	{
		return Find(mapping, key)?.Value is YamlScalarNode scalar ? scalar.Value ?? string.Empty : string.Empty;
	}

	private static string JoinList(YamlMappingNode mapping, string key)
	{
		YamlNode? node = Find(mapping, key)?.Value;
		if (node is YamlSequenceNode sequence)
			return string.Join(", ", sequence.Children.OfType<YamlScalarNode>().Select(item => item.Value).Where(value => !string.IsNullOrWhiteSpace(value))!);
		return node is YamlScalarNode scalar ? scalar.Value ?? string.Empty : string.Empty;
	}

	private static YamlMappingNode? Mapping(YamlMappingNode mapping, string key) => Find(mapping, key)?.Value as YamlMappingNode;
	private static YamlSequenceNode? Sequence(YamlMappingNode mapping, string key) => Find(mapping, key)?.Value as YamlSequenceNode;

	private static KeyValuePair<YamlNode, YamlNode>? Find(YamlMappingNode mapping, string key)
	{
		foreach (KeyValuePair<YamlNode, YamlNode> pair in mapping.Children)
			if (pair.Key is YamlScalarNode scalar && string.Equals(scalar.Value, key, StringComparison.OrdinalIgnoreCase))
				return pair;
		return null;
	}

	private static void SetRequiredScalar(YamlMappingNode mapping, string key, string value, ScalarStyle style = ScalarStyle.Any) =>
		SetNode(mapping, key, Scalar(value, style));

	private static void SetOptionalScalar(YamlMappingNode mapping, string key, string value, ScalarStyle style = ScalarStyle.Any)
	{
		if (string.IsNullOrWhiteSpace(value)) Remove(mapping, key);
		else SetNode(mapping, key, Scalar(value, style));
	}

	private static void SetOptionalOverride(YamlMappingNode mapping, string key, string value, string inherited)
	{
		if (string.IsNullOrWhiteSpace(value) || (Find(mapping, key) is null && value.Equals(inherited, StringComparison.OrdinalIgnoreCase)))
			Remove(mapping, key);
		else SetNode(mapping, key, Scalar(value));
	}

	private static YamlScalarNode Scalar(string value, ScalarStyle style = ScalarStyle.Any)
	{
		YamlScalarNode scalar = new(value ?? string.Empty);
		if (style != ScalarStyle.Any) scalar.Style = style;
		return scalar;
	}

	private static void SetList(YamlMappingNode mapping, string key, IReadOnlyList<string> values)
	{
		if (values.Count == 0) { Remove(mapping, key); return; }
		YamlSequenceNode sequence = new(values.Select(value => Scalar(value)));
		SetNode(mapping, key, sequence);
	}

	private static void SetIntegerList(YamlMappingNode mapping, string key, IReadOnlyList<string> values)
	{
		if (values.Count == 0) { Remove(mapping, key); return; }
		foreach (string value in values)
			if (!long.TryParse(value, out _)) throw new InvalidDataException($"{key} must contain comma-separated whole numbers. '{value}' is not a number.");
		SetList(mapping, key, values);
	}

	private static void SetNode(YamlMappingNode mapping, string key, YamlNode value)
	{
		KeyValuePair<YamlNode, YamlNode>? existing = Find(mapping, key);
		if (existing is not null) mapping.Children[existing.Value.Key] = value;
		else mapping.Add(new YamlScalarNode(key), value);
	}

	private static void Remove(YamlMappingNode mapping, string key)
	{
		KeyValuePair<YamlNode, YamlNode>? existing = Find(mapping, key);
		if (existing is not null) mapping.Children.Remove(existing.Value.Key);
	}

	private static string[] Split(string value) => (value ?? string.Empty)
		.Split([',', '\r', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
		.Distinct(StringComparer.OrdinalIgnoreCase)
		.ToArray();

	private static bool SameNonEmpty(string left, string right) =>
		!string.IsNullOrWhiteSpace(left) && !string.IsNullOrWhiteSpace(right) && left.Equals(right, StringComparison.OrdinalIgnoreCase);

	private sealed class GenerationWorkspace
	{
		public List<ManifestDocument> Documents = new();
		public ManifestDocument? ExistingVersion;
		public ManifestDocument? ExistingLocale;
		public ManifestDocument? ExistingInstaller;
		public string PreviousIdentifier = string.Empty;
		public string PreviousVersion = string.Empty;
		public ManifestDocument Version = null!;
		public ManifestDocument Locale = null!;
		public ManifestDocument Installer = null!;
	}

	private sealed record ManifestDocument(string Path, string Type, YamlMappingNode Root);
}
