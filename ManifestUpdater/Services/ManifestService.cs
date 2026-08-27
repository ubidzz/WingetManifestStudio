using System.Text;
using System.Text.RegularExpressions;

namespace ManifestUpdater;

internal static partial class ManifestService
{
	private const long MaximumManifestBytes = 16L * 1024 * 1024;
	private static readonly HashSet<string> InstallerTypes = new(StringComparer.OrdinalIgnoreCase)
	{
		"msix", "msi", "appx", "exe", "zip", "inno", "nullsoft", "wix", "burn", "pwa", "portable", "font"
	};
	private static readonly HashSet<string> NestedInstallerTypes = new(StringComparer.OrdinalIgnoreCase)
	{
		"msix", "msi", "appx", "exe", "inno", "nullsoft", "wix", "burn", "portable", "font"
	};
	private static readonly HashSet<string> Architectures = new(StringComparer.OrdinalIgnoreCase) { "x86", "x64", "arm", "arm64", "neutral" };
	private static readonly HashSet<string> Scopes = new(StringComparer.OrdinalIgnoreCase) { "user", "machine" };
	private static readonly HashSet<string> InstallModes = new(StringComparer.OrdinalIgnoreCase) { "interactive", "silent", "silentWithProgress" };
	private static readonly HashSet<string> UpgradeBehaviors = new(StringComparer.OrdinalIgnoreCase) { "install", "uninstallPrevious", "deny" };
	private static readonly HashSet<string> ElevationRequirements = new(StringComparer.OrdinalIgnoreCase) { "elevationRequired", "elevationProhibited", "elevatesSelf" };
	private static readonly HashSet<string> Platforms = new(StringComparer.OrdinalIgnoreCase) { "Windows.Desktop", "Windows.Universal" };
	private static readonly HashSet<string> RepairBehaviors = new(StringComparer.OrdinalIgnoreCase) { "modify", "uninstaller", "installer" };
	private static readonly HashSet<string> UnsupportedArchitectures = new(StringComparer.OrdinalIgnoreCase) { "x86", "x64", "arm", "arm64" };

	public static ManifestProject LoadProject(string folder)
	{
		return SchemaAwareYaml.LoadProject(folder, MaximumManifestBytes);
		/* Legacy targeted reader retained below as a compatibility reference.
		ManifestFiles files = FindManifestFiles(folder);
		if (files.Version is null && files.Locale is null && files.Installer is null)
			return new ManifestProject { ManifestFolder = folder };

		string versionText = ReadOptional(files.Version);
		string localeText = ReadOptional(files.Locale);
		string installerText = ReadOptional(files.Installer);
		ManifestProject project = new()
		{
			ManifestFolder = folder,
			PackageIdentifier = FirstScalar("PackageIdentifier", versionText, localeText, installerText),
			PackageVersion = FirstScalar("PackageVersion", versionText, localeText, installerText),
			DefaultLocale = FirstScalar("DefaultLocale", versionText).IfEmpty(FirstScalar("PackageLocale", localeText)).IfEmpty("en-US"),
			ManifestVersion = FirstScalar("ManifestVersion", versionText, localeText, installerText).IfEmpty("1.12.0"),
			Publisher = FirstScalar("Publisher", localeText),
			PublisherUrl = FirstScalar("PublisherUrl", localeText),
			PublisherSupportUrl = FirstScalar("PublisherSupportUrl", localeText),
			Author = FirstScalar("Author", localeText),
			PackageName = FirstScalar("PackageName", localeText),
			PackageUrl = FirstScalar("PackageUrl", localeText),
			License = FirstScalar("License", localeText),
			LicenseUrl = FirstScalar("LicenseUrl", localeText),
			Copyright = FirstScalar("Copyright", localeText),
			ShortDescription = FirstScalar("ShortDescription", localeText),
			Description = FirstScalar("Description", localeText),
			Moniker = FirstScalar("Moniker", localeText),
			Tags = string.Join(", ", ReadRootList(localeText, "Tags")),
			Commands = string.Join(", ", ReadRootList(installerText, "Commands")),
			ReleaseNotes = ReadBlock(localeText, "ReleaseNotes"),
			ReleaseNotesUrl = FirstScalar("ReleaseNotesUrl", localeText),
			InstallerType = FirstScalar("InstallerType", installerText).IfEmpty("exe"),
			Scope = FirstScalar("Scope", installerText).IfEmpty("user"),
			InstallModes = string.Join(", ", ReadRootList(installerText, "InstallModes")),
			UpgradeBehavior = FirstScalar("UpgradeBehavior", installerText).IfEmpty("install"),
			ElevationRequirement = FirstScalar("ElevationRequirement", installerText),
			LoadedFromExistingManifests = true
		};
		project.ProfileName = project.PackageIdentifier.IfEmpty(project.PackageName).IfEmpty("Imported package");
		project.InstallModes = project.InstallModes.IfEmpty("interactive, silent, silentWithProgress");
		foreach (InstallerArtifact artifact in ReadInstallers(installerText, project.InstallerType, project.Scope))
			project.Installers.Add(artifact);
		return project; */
	}

	public static ManifestGenerationResult Generate(ManifestProject project)
	{
		List<string> errors = Validate(project);
		if (errors.Count > 0)
			throw new InvalidDataException(string.Join(Environment.NewLine, errors.Select(error => "• " + error)));

		return SchemaAwareYaml.Generate(project, MaximumManifestBytes);
		/* Legacy targeted writer retained below as a compatibility reference.
		ManifestFiles existing = FindManifestFiles(project.ManifestFolder);
		Dictionary<string, string> files = new(StringComparer.OrdinalIgnoreCase);
		List<string> changes = [];
		List<string> warnings = [];

		string versionName = existing.Version is null
			? $"{project.PackageIdentifier}.yaml"
			: Path.GetFileName(existing.Version);
		string localeName = existing.Locale is null
			? $"{project.PackageIdentifier}.locale.{project.DefaultLocale}.yaml"
			: Path.GetFileName(existing.Locale);
		string installerName = existing.Installer is null
			? $"{project.PackageIdentifier}.installer.yaml"
			: Path.GetFileName(existing.Installer);

		files[versionName] = existing.Version is null
			? BuildVersionManifest(project)
			: PatchVersionManifest(File.ReadAllText(existing.Version), project);
		files[localeName] = existing.Locale is null
			? BuildLocaleManifest(project)
			: PatchLocaleManifest(File.ReadAllText(existing.Locale), project);
		files[installerName] = existing.Installer is null
			? BuildInstallerManifest(project)
			: PatchInstallerManifest(File.ReadAllText(existing.Installer), project);

		string previousIdentifier = existing.Version is null ? string.Empty : FirstScalar("PackageIdentifier", File.ReadAllText(existing.Version));
		string previousVersion = existing.Version is null ? string.Empty : FirstScalar("PackageVersion", File.ReadAllText(existing.Version));
		changes.Add(!string.IsNullOrWhiteSpace(previousIdentifier) && !string.Equals(previousIdentifier, project.PackageIdentifier, StringComparison.Ordinal)
			? $"Package identifier: {previousIdentifier}  →  {project.PackageIdentifier}"
			: $"Package: {project.PackageIdentifier}");
		changes.Add(!string.IsNullOrWhiteSpace(previousVersion) && !string.Equals(previousVersion, project.PackageVersion, StringComparison.Ordinal)
			? $"Release version: {previousVersion}  →  {project.PackageVersion}"
			: $"Release version: {project.PackageVersion}");
		changes.Add($"Default language: {project.DefaultLocale}");
		changes.Add($"Installers: {project.Installers.Count} ({string.Join(", ", project.Installers.Select(installer => installer.Architecture.IfEmpty("architecture not set")).Distinct(StringComparer.OrdinalIgnoreCase))})");
		changes.Add(existing.Version is null ? $"Create {versionName}." : $"Update {versionName} and preserve fields the Studio does not edit.");
		changes.Add(existing.Locale is null ? $"Create {localeName}." : $"Update {localeName} and preserve fields the Studio does not edit.");
		changes.Add(existing.Installer is null ? $"Create {installerName}." : $"Update {installerName} and preserve custom installer fields.");
		if (project.Installers.Any(installer => string.IsNullOrWhiteSpace(installer.LocalFile)))
			warnings.Add("At least one installer is URL-only. Its recorded hash could not be compared with a local release file during this run.");
		if (project.AllowInsecureUrls)
			warnings.Add("Unsecured HTTP installer URLs are allowed for this project.");
		return new ManifestGenerationResult(files, changes, warnings); */
	}

	public static void Save(ManifestProject project, ManifestGenerationResult result)
	{
		Directory.CreateDirectory(project.ManifestFolder);
		string backupFolder = Path.Combine(project.ManifestFolder, ".manifest-backups", DateTime.Now.ToString("yyyyMMdd-HHmmss"));
		foreach ((string fileName, string contents) in result.Files)
		{
			string path = Path.Combine(project.ManifestFolder, fileName);
			if (File.Exists(path))
			{
				Directory.CreateDirectory(backupFolder);
				File.Copy(path, Path.Combine(backupFolder, fileName), true);
			}
			WriteAtomically(path, contents);
		}
	}

	public static string CreateCleanManifestFolder(ManifestGenerationResult result)
	{
		string stagingRoot = Path.Combine(Path.GetTempPath(), "WingetManifestStudio");
		Directory.CreateDirectory(stagingRoot);
		string stagingFolder = Path.Combine(stagingRoot, "manifest-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(stagingFolder);
		foreach ((string fileName, string contents) in result.Files)
		{
			if (!string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal) ||
				!string.Equals(Path.GetExtension(fileName), ".yaml", StringComparison.OrdinalIgnoreCase))
				throw new InvalidDataException($"The generated manifest name '{fileName}' is not a safe YAML file name.");
			File.WriteAllText(Path.Combine(stagingFolder, fileName), contents, new UTF8Encoding(false));
		}
		return stagingFolder;
	}

	public static void DeleteCleanManifestFolder(string? folder)
	{
		if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder)) return;
		string stagingRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "WingetManifestStudio"))
			.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
		string fullFolder = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
		if (!fullFolder.StartsWith(stagingRoot, StringComparison.OrdinalIgnoreCase))
			throw new InvalidOperationException("The manifest staging folder is outside the expected temporary location.");
		Directory.Delete(folder, true);
	}

	public static List<string> Validate(ManifestProject project)
	{
		List<string> errors = [];
		if (string.IsNullOrWhiteSpace(project.PackageIdentifier))
			errors.Add("Package Identifier is required. Use Publisher.Application, for example Contoso.Sample.");
		else if (project.PackageIdentifier.Length > 128)
			errors.Add("Package Identifier is too long. It must be 128 characters or fewer.");
		else if (!PackageIdRegex().IsMatch(project.PackageIdentifier))
			errors.Add("Package Identifier must contain 2 to 8 dot-separated parts, such as Contoso.Sample. Each part can be up to 32 characters and cannot contain spaces or Windows filename symbols.");
		if (string.IsNullOrWhiteSpace(project.PackageVersion) || project.PackageVersion.StartsWith('v'))
			errors.Add("Package Version is required and must not begin with v.");
		else if (project.PackageVersion.Length > 128 || InvalidVersionCharactersRegex().IsMatch(project.PackageVersion))
			errors.Add("Package Version cannot exceed 128 characters or contain Windows path symbols.");
		if (string.IsNullOrWhiteSpace(project.DefaultLocale)) errors.Add("Default Locale is required, usually en-US.");
		else if (project.DefaultLocale.Length > 20 || !LocaleRegex().IsMatch(project.DefaultLocale)) errors.Add("Default Locale must be a language tag of 20 characters or fewer, such as en-US, es-ES, fr-FR, or ja-JP.");
		if (string.IsNullOrWhiteSpace(project.ManifestVersion)) errors.Add("Manifest Version is required, usually 1.12.0.");
		else if (!ManifestVersionRegex().IsMatch(project.ManifestVersion)
			|| project.ManifestVersion.Split('.').Any(part => !ushort.TryParse(part, out _)))
			errors.Add("Manifest Version must use three numeric parts from 0 through 65535, such as 1.12.0.");
		if (string.IsNullOrWhiteSpace(project.PackageName)) errors.Add("Package Name is required.");
		if (string.IsNullOrWhiteSpace(project.Publisher)) errors.Add("Publisher is required.");
		if (string.IsNullOrWhiteSpace(project.ShortDescription)) errors.Add("Short Description is required.");
		if (string.IsNullOrWhiteSpace(project.License)) errors.Add("License is required.");
		if (string.IsNullOrWhiteSpace(project.ManifestFolder)) errors.Add("Choose a manifest output folder.");
		ValidateChoice("Installer Type", project.InstallerType, InstallerTypes, errors, allowBlank: true);
		ValidateChoice("Nested Installer Type", project.NestedInstallerType, NestedInstallerTypes, errors, allowBlank: true);
		ValidateChoice("Scope", project.Scope, Scopes, errors, allowBlank: true);
		ValidateChoice("Upgrade Behavior", project.UpgradeBehavior, UpgradeBehaviors, errors, allowBlank: true);
		ValidateChoice("Elevation Requirement", project.ElevationRequirement, ElevationRequirements, errors, allowBlank: true);
		ValidateChoice("Repair Behavior", project.RepairBehavior, RepairBehaviors, errors, allowBlank: true);
		ValidateList("Install Modes", project.InstallModes, InstallModes, errors);
		ValidateList("Platforms", project.Platform, Platforms, errors);
		ValidateList("Unsupported OS Architectures", project.UnsupportedOSArchitectures, UnsupportedArchitectures, errors);
		ValidateSuccessCodes(project.InstallerSuccessCodes, errors);
		ValidateOptionalUrl("Publisher URL", project.PublisherUrl, errors);
		ValidateOptionalUrl("Publisher Support URL", project.PublisherSupportUrl, errors);
		ValidateOptionalUrl("Privacy URL", project.PrivacyUrl, errors);
		ValidateOptionalUrl("Package URL", project.PackageUrl, errors);
		ValidateOptionalUrl("License URL", project.LicenseUrl, errors);
		ValidateOptionalUrl("Copyright URL", project.CopyrightUrl, errors);
		ValidateOptionalUrl("Purchase URL", project.PurchaseUrl, errors);
		ValidateOptionalUrl("Release Notes URL", project.ReleaseNotesUrl, errors);
		if (project.Installers.Count == 0) errors.Add("Add at least one installer.");
		for (int index = 0; index < project.Installers.Count; index++)
		{
			InstallerArtifact installer = project.Installers[index];
			string label = $"Installer {index + 1}";
			string installerType = installer.InstallerType.IfEmpty(project.InstallerType);
			string nestedInstallerType = installer.NestedInstallerType.IfEmpty(project.NestedInstallerType);
			string nestedInstallerFiles = installer.NestedInstallerFiles.IfEmpty(project.NestedInstallerFiles);
			string scope = installer.Scope.IfEmpty(project.Scope);
			if (!Uri.TryCreate(installer.InstallerUrl, UriKind.Absolute, out Uri? uri)
				|| (!uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
					&& !uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)))
				errors.Add($"{label} needs a valid public Installer URL.");
			else if (installer.InstallerUrl.Length > 2048)
				errors.Add($"{label} Installer URL is longer than Winget's 2048-character limit.");
			else if (!project.AllowInsecureUrls && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
				errors.Add($"{label} must use HTTPS unless unsecured URLs are explicitly allowed.");
			if (!ShaRegex().IsMatch(installer.Sha256)) errors.Add($"{label} needs a calculated 64-character SHA-256 hash.");
			if (string.IsNullOrWhiteSpace(installer.Architecture)) errors.Add($"{label} needs an architecture. Inspect its local file or choose x86, x64, arm, arm64, or neutral.");
			else if (!Architectures.Contains(installer.Architecture)) errors.Add($"{label} Architecture must be x86, x64, arm, arm64, or neutral.");
			if (string.IsNullOrWhiteSpace(installerType)) errors.Add($"{label} needs an Installer Type. Inspect its local file or choose the correct type.");
			else if (!InstallerTypes.Contains(installerType)) errors.Add($"{label} Installer Type '{installerType}' is not supported by Winget schema {project.ManifestVersion}.");
			if (!string.IsNullOrWhiteSpace(scope) && !Scopes.Contains(scope)) errors.Add($"{label} Scope must be user, machine, or blank.");
			if (installerType.Equals("zip", StringComparison.OrdinalIgnoreCase))
			{
				if (string.IsNullOrWhiteSpace(nestedInstallerType))
					errors.Add($"{label} is a ZIP and needs its Nested Installer Type.");
				else if (!NestedInstallerTypes.Contains(nestedInstallerType))
					errors.Add($"{label} Nested Installer Type '{nestedInstallerType}' is not supported.");
				try
				{
					IReadOnlyList<NestedInstallerFileEntry> nestedFiles = ParseNestedInstallerFiles(nestedInstallerFiles);
					if (nestedFiles.Count == 0) errors.Add($"{label} is a ZIP and needs at least one file path from inside the archive.");
					if (nestedFiles.Count > 1 && !nestedInstallerType.Equals("portable", StringComparison.OrdinalIgnoreCase))
						errors.Add($"{label} can contain more than one nested file only when Nested Installer Type is portable.");
					if (!nestedInstallerType.Equals("portable", StringComparison.OrdinalIgnoreCase) && nestedFiles.Any(file => file.PortableCommandAlias.Length > 0))
						errors.Add($"{label} command aliases are only valid for nested portable files.");
				}
				catch (InvalidDataException ex) { errors.Add($"{label} ZIP contents: {ex.Message}"); }
			}
			else if (!string.IsNullOrWhiteSpace(nestedInstallerType) || !string.IsNullOrWhiteSpace(nestedInstallerFiles))
				errors.Add($"{label} has nested installer values, but its Installer Type is not ZIP. Clear those ZIP-only fields or choose ZIP.");
			if (!string.IsNullOrWhiteSpace(installer.SignatureSha256) && !ShaRegex().IsMatch(installer.SignatureSha256))
				errors.Add($"{label} Signature SHA-256 must be blank or contain exactly 64 hexadecimal characters.");
			if (installer.VerificationStatus.StartsWith("FAILED", StringComparison.OrdinalIgnoreCase))
				errors.Add($"{label} failed public URL verification. The public download must match the attached local file.");
		}
		if (!string.IsNullOrWhiteSpace(project.ReleaseDate)
			&& !DateOnly.TryParseExact(project.ReleaseDate, "yyyy-MM-dd", out _))
			errors.Add("Release Date must use YYYY-MM-DD.");
		foreach ((string name, string value) in new Dictionary<string, string>
		{
			["Installer Aborts Terminal"] = project.InstallerAbortsTerminal,
			["Install Location Required"] = project.InstallLocationRequired,
			["Require Explicit Upgrade"] = project.RequireExplicitUpgrade,
			["Display Install Warnings"] = project.DisplayInstallWarnings,
			["Download Command Prohibited"] = project.DownloadCommandProhibited,
			["Archive Binaries Depend On Path"] = project.ArchiveBinariesDependOnPath
		})
			if (!string.IsNullOrWhiteSpace(value) && !bool.TryParse(value, out _))
				errors.Add($"{name} must be true, false, or blank.");
		errors.AddRange(SchemaAwareYaml.ValidateAdvancedFields(project));
		return errors;
	}

	internal static IReadOnlyList<NestedInstallerFileEntry> ParseNestedInstallerFiles(string value)
	{
		List<NestedInstallerFileEntry> result = [];
		foreach (string item in (value ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n')
			.Split(['\n', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
		{
			string[] parts = item.Split('|', 2, StringSplitOptions.TrimEntries);
			string relativePath = parts[0];
			string alias = parts.Length > 1 ? parts[1] : string.Empty;
			if (relativePath.Length == 0) throw new InvalidDataException("Each entry needs a relative file path.");
			if (relativePath.Length > 512) throw new InvalidDataException($"'{relativePath}' is longer than 512 characters.");
			if (Path.IsPathRooted(relativePath) || relativePath.Split(['\\', '/']).Any(segment => segment == ".."))
				throw new InvalidDataException($"'{relativePath}' must be a safe path relative to the ZIP file.");
			if (alias.Length > 40) throw new InvalidDataException($"The command alias '{alias}' is longer than 40 characters.");
			result.Add(new NestedInstallerFileEntry(relativePath, alias));
		}
		return result;
	}

	private static void ValidateChoice(string name, string value, ISet<string> choices, ICollection<string> errors, bool allowBlank)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			if (!allowBlank) errors.Add(name + " is required.");
			return;
		}
		if (!choices.Contains(value)) errors.Add($"{name} must be one of: {string.Join(", ", choices)}.");
	}

	private static void ValidateList(string name, string value, ISet<string> choices, ICollection<string> errors)
	{
		string[] items = SplitValues(value);
		foreach (string item in items)
			if (!choices.Contains(item)) errors.Add($"{name} contains '{item}'. Choose only: {string.Join(", ", choices)}.");
		if (items.Length != items.Distinct(StringComparer.OrdinalIgnoreCase).Count()) errors.Add(name + " contains a duplicate value.");
	}

	private static void ValidateSuccessCodes(string value, ICollection<string> errors)
	{
		foreach (string code in SplitValues(value))
			if (!long.TryParse(code, out long number) || number is < -2147483648L or > 4294967295L || number == 0)
				errors.Add($"Extra Success Code '{code}' must be a non-zero whole number from -2147483648 through 4294967295.");
	}

	private static void ValidateOptionalUrl(string name, string value, ICollection<string> errors)
	{
		if (string.IsNullOrWhiteSpace(value)) return;
		if (value.Length > 2048)
			errors.Add(name + " is longer than Winget's 2048-character limit.");
		else if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
			errors.Add(name + " must be a complete public HTTP or HTTPS address, or be left blank.");
	}

	private static string PatchVersionManifest(string text, ManifestProject project)
	{
		text = SetRootScalar(text, "PackageIdentifier", project.PackageIdentifier);
		text = SetRootScalar(text, "PackageVersion", project.PackageVersion);
		text = SetRootScalar(text, "DefaultLocale", project.DefaultLocale);
		text = SetRootScalar(text, "ManifestVersion", project.ManifestVersion);
		return EnsureFinalNewLine(text);
	}

	private static string PatchLocaleManifest(string text, ManifestProject project)
	{
		Dictionary<string, string> fields = new()
		{
			["PackageIdentifier"] = project.PackageIdentifier,
			["PackageVersion"] = project.PackageVersion,
			["PackageLocale"] = project.DefaultLocale,
			["Publisher"] = project.Publisher,
			["PublisherUrl"] = project.PublisherUrl,
			["PublisherSupportUrl"] = project.PublisherSupportUrl,
			["Author"] = project.Author,
			["PackageName"] = project.PackageName,
			["PackageUrl"] = project.PackageUrl,
			["License"] = project.License,
			["LicenseUrl"] = project.LicenseUrl,
			["Copyright"] = project.Copyright,
			["ShortDescription"] = project.ShortDescription,
			["Description"] = project.Description,
			["Moniker"] = project.Moniker,
			["ReleaseNotesUrl"] = project.ReleaseNotesUrl,
			["ManifestVersion"] = project.ManifestVersion
		};
		foreach ((string key, string value) in fields.Where(pair => !string.IsNullOrWhiteSpace(pair.Value)))
			text = SetRootScalar(text, key, value);
		if (!string.IsNullOrWhiteSpace(project.Tags))
			text = SetRootList(text, "Tags", SplitValues(project.Tags));
		if (!string.IsNullOrWhiteSpace(project.ReleaseNotes))
			text = SetRootBlock(text, "ReleaseNotes", project.ReleaseNotes);
		return EnsureFinalNewLine(text);
	}

	private static string PatchInstallerManifest(string text, ManifestProject project)
	{
		text = SetRootScalar(text, "PackageIdentifier", project.PackageIdentifier);
		text = SetRootScalar(text, "PackageVersion", project.PackageVersion);
		text = SetRootScalar(text, "InstallerType", project.InstallerType);
		if (!string.IsNullOrWhiteSpace(project.Scope)) text = SetRootScalar(text, "Scope", project.Scope);
		if (!string.IsNullOrWhiteSpace(project.UpgradeBehavior)) text = SetRootScalar(text, "UpgradeBehavior", project.UpgradeBehavior);
		if (!string.IsNullOrWhiteSpace(project.ElevationRequirement)) text = SetRootScalar(text, "ElevationRequirement", project.ElevationRequirement);
		if (!string.IsNullOrWhiteSpace(project.Commands)) text = SetRootList(text, "Commands", SplitValues(project.Commands));
		if (!string.IsNullOrWhiteSpace(project.InstallModes)) text = SetRootList(text, "InstallModes", SplitValues(project.InstallModes));
		text = SetRootScalar(text, "ManifestVersion", project.ManifestVersion);
		return EnsureFinalNewLine(PatchInstallerNodes(text, project));
	}

	private static string PatchInstallerNodes(string text, ManifestProject project)
	{
		List<string> lines = NormalizeLines(text).Split('\n').ToList();
		int start = lines.FindIndex(line => line.TrimEnd() == "Installers:");
		if (start < 0)
		{
			int manifestType = lines.FindIndex(line => line.StartsWith("ManifestType:", StringComparison.Ordinal));
			if (manifestType < 0) manifestType = lines.Count;
			lines.InsertRange(manifestType, BuildInstallerSection(project).Split('\n'));
			return string.Join('\n', lines);
		}
		int end = FindInstallerSectionEnd(lines, start + 1);
		List<string> section = lines.GetRange(start + 1, end - start - 1);
		List<List<string>> existingNodes = SplitInstallerNodes(section);
		List<string> replacement = [];
		for (int index = 0; index < project.Installers.Count; index++)
		{
			InstallerArtifact artifact = project.Installers[index];
			List<string> node = index < existingNodes.Count
				? existingNodes[index]
				: BuildInstallerNode(project, artifact).Split('\n').ToList();
			PatchNode(node, project, artifact);
			replacement.AddRange(node);
		}
		lines.RemoveRange(start + 1, end - start - 1);
		lines.InsertRange(start + 1, replacement);
		return string.Join('\n', lines);
	}

	private static List<List<string>> SplitInstallerNodes(List<string> section)
	{
		List<List<string>> nodes = [];
		foreach (string line in section)
		{
			if (Regex.IsMatch(line, @"^\s*-\s+Architecture\s*:", RegexOptions.IgnoreCase)) nodes.Add([]);
			if (nodes.Count > 0) nodes[^1].Add(line);
		}
		return nodes;
	}

	private static int FindInstallerSectionEnd(List<string> lines, int start)
	{
		int end = start;
		while (end < lines.Count)
		{
			if (Regex.IsMatch(lines[end], @"^[A-Za-z][A-Za-z0-9]*\s*:")) break;
			end++;
		}
		return end;
	}

	private static void PatchNode(List<string> node, ManifestProject project, InstallerArtifact artifact)
	{
		int nodeIndent = node.Count == 0 ? 0 : node[0].TakeWhile(char.IsWhiteSpace).Count();
		int childIndent = nodeIndent + 2;
		EnsureNodeFirstScalar(node, "Architecture", artifact.Architecture);
		SetNodeScalar(node, "InstallerUrl", artifact.InstallerUrl, childIndent);
		SetNodeScalar(node, "InstallerSha256", artifact.Sha256.ToUpperInvariant(), childIndent);
		if (!string.IsNullOrWhiteSpace(artifact.InstallerType) && !string.Equals(artifact.InstallerType, project.InstallerType, StringComparison.OrdinalIgnoreCase))
			SetNodeScalar(node, "InstallerType", artifact.InstallerType, childIndent);
		if (!string.IsNullOrWhiteSpace(artifact.Scope) && !string.Equals(artifact.Scope, project.Scope, StringComparison.OrdinalIgnoreCase))
			SetNodeScalar(node, "Scope", artifact.Scope, childIndent);
		if (!string.IsNullOrWhiteSpace(artifact.ProductCode)) SetNodeScalar(node, "ProductCode", artifact.ProductCode.ToUpperInvariant(), childIndent);
		if (!string.IsNullOrWhiteSpace(project.CustomInstallerSwitch)) SetExistingNodeScalar(node, "Custom", project.CustomInstallerSwitch);
		if (!string.IsNullOrWhiteSpace(artifact.UpgradeCode)) SetExistingNodeScalar(node, "UpgradeCode", artifact.UpgradeCode.ToUpperInvariant());
		if (!string.IsNullOrWhiteSpace(artifact.DisplayName)) SetExistingNodeScalar(node, "DisplayName", artifact.DisplayName, sequenceItem: true);
		if (!string.IsNullOrWhiteSpace(artifact.Publisher)) SetExistingNodeScalar(node, "Publisher", artifact.Publisher);
	}

	private static void EnsureNodeFirstScalar(List<string> node, string key, string value)
	{
		int existing = node.FindIndex(line => Regex.IsMatch(line, $@"^\s*-\s*{Regex.Escape(key)}\s*:", RegexOptions.IgnoreCase));
		if (existing >= 0)
		{
			int indentation = node[existing].TakeWhile(char.IsWhiteSpace).Count();
			node[existing] = new string(' ', indentation) + $"- {key}: {Yaml(value)}";
			return;
		}
		if (node.Count == 0)
		{
			node.Add($"  - {key}: {Yaml(value)}");
			return;
		}
		int currentIndent = node[0].TakeWhile(char.IsWhiteSpace).Count();
		node[0] = Regex.Replace(node[0], @"^\s*-\s*", new string(' ', currentIndent + 2));
		node.Insert(0, new string(' ', currentIndent) + $"- {key}: {Yaml(value)}");
	}

	private static void SetNodeScalar(List<string> node, string key, string value, int indentation)
	{
		Regex matcher = new($@"^ {{{indentation}}}{Regex.Escape(key)}\s*:", RegexOptions.IgnoreCase);
		int index = node.FindIndex(line => matcher.IsMatch(line));
		string replacement = new string(' ', indentation) + key + ": " + Yaml(value);
		if (index >= 0)
		{
			node[index] = replacement;
			return;
		}
		int insertAt = node.FindIndex(line => line.TrimStart().StartsWith("AppsAndFeaturesEntries:", StringComparison.Ordinal));
		if (insertAt < 0) insertAt = node.Count;
		node.Insert(insertAt, replacement);
	}

	private static void SetExistingNodeScalar(List<string> node, string key, string value, bool sequenceItem = false)
	{
		Regex matcher = new($@"^(?<indent>\s+)(?<sequence>-\s*)?{Regex.Escape(key)}\s*:", RegexOptions.IgnoreCase);
		int index = node.FindIndex(line => matcher.IsMatch(line));
		if (index < 0) return;
		Match match = matcher.Match(node[index]);
		string prefix = match.Groups["indent"].Value;
		if (sequenceItem || match.Groups["sequence"].Success) prefix += "- ";
		node[index] = prefix + key + ": " + Yaml(value);
	}

	private static string BuildVersionManifest(ManifestProject project) => $"""
		# Created with Winget Manifest Studio
		# yaml-language-server: $schema=https://aka.ms/winget-manifest.version.{project.ManifestVersion}.schema.json

		PackageIdentifier: {Yaml(project.PackageIdentifier)}
		PackageVersion: {Yaml(project.PackageVersion)}
		DefaultLocale: {Yaml(project.DefaultLocale)}
		ManifestType: version
		ManifestVersion: {Yaml(project.ManifestVersion)}
		""" + Environment.NewLine;

	private static string BuildLocaleManifest(ManifestProject project)
	{
		StringBuilder text = new();
		text.AppendLine("# Created with Winget Manifest Studio");
		text.AppendLine($"# yaml-language-server: $schema=https://aka.ms/winget-manifest.defaultLocale.{project.ManifestVersion}.schema.json");
		text.AppendLine();
		AppendScalar(text, "PackageIdentifier", project.PackageIdentifier);
		AppendScalar(text, "PackageVersion", project.PackageVersion);
		AppendScalar(text, "PackageLocale", project.DefaultLocale);
		AppendScalar(text, "Publisher", project.Publisher);
		AppendOptionalScalar(text, "PublisherUrl", project.PublisherUrl);
		AppendOptionalScalar(text, "PublisherSupportUrl", project.PublisherSupportUrl);
		AppendOptionalScalar(text, "Author", project.Author);
		AppendScalar(text, "PackageName", project.PackageName);
		AppendOptionalScalar(text, "PackageUrl", project.PackageUrl);
		AppendScalar(text, "License", project.License);
		AppendOptionalScalar(text, "LicenseUrl", project.LicenseUrl);
		AppendOptionalScalar(text, "Copyright", project.Copyright);
		AppendScalar(text, "ShortDescription", project.ShortDescription);
		AppendOptionalScalar(text, "Description", project.Description);
		AppendOptionalScalar(text, "Moniker", project.Moniker);
		AppendList(text, "Tags", SplitValues(project.Tags));
		if (!string.IsNullOrWhiteSpace(project.ReleaseNotes))
		{
			text.AppendLine("ReleaseNotes: |-" );
			foreach (string line in NormalizeLines(project.ReleaseNotes).Split('\n')) text.AppendLine("  " + line);
		}
		AppendOptionalScalar(text, "ReleaseNotesUrl", project.ReleaseNotesUrl);
		text.AppendLine("ManifestType: defaultLocale");
		AppendScalar(text, "ManifestVersion", project.ManifestVersion);
		return text.ToString();
	}

	private static string BuildInstallerManifest(ManifestProject project)
	{
		StringBuilder text = new();
		text.AppendLine("# Created with Winget Manifest Studio");
		text.AppendLine($"# yaml-language-server: $schema=https://aka.ms/winget-manifest.installer.{project.ManifestVersion}.schema.json");
		text.AppendLine();
		AppendScalar(text, "PackageIdentifier", project.PackageIdentifier);
		AppendScalar(text, "PackageVersion", project.PackageVersion);
		AppendScalar(text, "InstallerType", project.InstallerType);
		AppendOptionalScalar(text, "Scope", project.Scope);
		AppendList(text, "Commands", SplitValues(project.Commands));
		AppendList(text, "InstallModes", SplitValues(project.InstallModes));
		AppendOptionalScalar(text, "UpgradeBehavior", project.UpgradeBehavior);
		AppendOptionalScalar(text, "ElevationRequirement", project.ElevationRequirement);
		text.Append(BuildInstallerSection(project));
		text.AppendLine("ManifestType: installer");
		AppendScalar(text, "ManifestVersion", project.ManifestVersion);
		return text.ToString();
	}

	private static string BuildInstallerSection(ManifestProject project)
	{
		StringBuilder text = new();
		text.AppendLine("Installers:");
		foreach (InstallerArtifact installer in project.Installers)
			text.AppendLine(BuildInstallerNode(project, installer));
		return text.ToString().TrimEnd() + Environment.NewLine;
	}

	private static string BuildInstallerNode(ManifestProject project, InstallerArtifact installer)
	{
		StringBuilder text = new();
		text.AppendLine($"  - Architecture: {Yaml(installer.Architecture)}");
		if (!string.IsNullOrWhiteSpace(installer.InstallerType) && !string.Equals(installer.InstallerType, project.InstallerType, StringComparison.OrdinalIgnoreCase))
			text.AppendLine($"    InstallerType: {Yaml(installer.InstallerType)}");
		if (!string.IsNullOrWhiteSpace(installer.Scope) && !string.Equals(installer.Scope, project.Scope, StringComparison.OrdinalIgnoreCase))
			text.AppendLine($"    Scope: {Yaml(installer.Scope)}");
		if (!string.IsNullOrWhiteSpace(project.CustomInstallerSwitch))
		{
			text.AppendLine("    InstallerSwitches:");
			text.AppendLine($"      Custom: {Yaml(project.CustomInstallerSwitch)}");
		}
		text.AppendLine($"    InstallerUrl: {Yaml(installer.InstallerUrl)}");
		text.AppendLine($"    InstallerSha256: {installer.Sha256.ToUpperInvariant()}");
		if (!string.IsNullOrWhiteSpace(installer.ProductCode)) text.AppendLine($"    ProductCode: {Yaml(installer.ProductCode.ToUpperInvariant())}");
		if (!string.IsNullOrWhiteSpace(installer.DisplayName) || !string.IsNullOrWhiteSpace(installer.UpgradeCode))
		{
			text.AppendLine("    AppsAndFeaturesEntries:");
			text.AppendLine($"      - DisplayName: {Yaml(installer.DisplayName.IfEmpty(project.PackageName))}");
			if (!string.IsNullOrWhiteSpace(installer.Publisher)) text.AppendLine($"        Publisher: {Yaml(installer.Publisher)}");
			if (!string.IsNullOrWhiteSpace(installer.ProductCode)) text.AppendLine($"        ProductCode: {Yaml(installer.ProductCode.ToUpperInvariant())}");
			if (!string.IsNullOrWhiteSpace(installer.UpgradeCode)) text.AppendLine($"        UpgradeCode: {Yaml(installer.UpgradeCode.ToUpperInvariant())}");
		}
		return text.ToString().TrimEnd();
	}

	private static ManifestFiles FindManifestFiles(string folder)
	{
		if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder)) return new(null, null, null);
		string[] candidates = Directory.GetFiles(folder, "*.yaml", SearchOption.TopDirectoryOnly)
			.Concat(Directory.GetFiles(folder, "*.yml", SearchOption.TopDirectoryOnly)).ToArray();
		string? version = null;
		string? locale = null;
		string? installer = null;
		foreach (string path in candidates)
		{
			string text;
			try { text = ReadManifestText(path); } catch { continue; }
			switch (FirstScalar("ManifestType", text).ToLowerInvariant())
			{
				case "version": version ??= path; break;
				case "defaultlocale": locale ??= path; break;
				case "installer": installer ??= path; break;
			}
		}
		return new(version, locale, installer);
	}

	private static List<InstallerArtifact> ReadInstallers(string text, string rootType, string rootScope)
	{
		List<InstallerArtifact> result = [];
		List<string> lines = NormalizeLines(text).Split('\n').ToList();
		int start = lines.FindIndex(line => line.TrimEnd() == "Installers:");
		if (start < 0) return result;
		int end = FindInstallerSectionEnd(lines, start + 1);
		foreach (List<string> node in SplitInstallerNodes(lines.GetRange(start + 1, end - start - 1)))
		{
			string block = string.Join('\n', node);
			result.Add(new InstallerArtifact
			{
				Architecture = FirstIndentedScalar("Architecture", block).IfEmpty("x64"),
				InstallerType = FirstIndentedScalar("InstallerType", block).IfEmpty(rootType),
				Scope = FirstIndentedScalar("Scope", block).IfEmpty(rootScope),
				InstallerUrl = FirstIndentedScalar("InstallerUrl", block),
				Sha256 = FirstIndentedScalar("InstallerSha256", block),
				ProductCode = FirstIndentedScalar("ProductCode", block),
				UpgradeCode = FirstIndentedScalar("UpgradeCode", block),
				DisplayName = FirstIndentedScalar("DisplayName", block),
				Publisher = FirstIndentedScalar("Publisher", block),
				VerificationStatus = "Loaded from manifest • hash not rechecked"
			});
		}
		return result;
	}

	private static string SetRootScalar(string text, string key, string value)
	{
		string normalized = NormalizeLines(text);
		Regex regex = new($@"(?m)^{Regex.Escape(key)}\s*:\s*.*$");
		string line = $"{key}: {Yaml(value)}";
		if (regex.IsMatch(normalized)) return regex.Replace(normalized, line, 1);
		return InsertBeforeManifestType(normalized, line + "\n");
	}

	private static string SetRootList(string text, string key, IReadOnlyList<string> values)
	{
		if (values.Count == 0) return text;
		string normalized = NormalizeLines(text);
		string replacement = key + ":\n" + string.Join('\n', values.Select(value => "  - " + Yaml(value))) + "\n";
		Regex regex = new($@"(?m)^{Regex.Escape(key)}[ \t]*:[ \t]*\n(?:^[ \t]*-[ \t]+.*\n?)*");
		return regex.IsMatch(normalized) ? regex.Replace(normalized, replacement, 1) : InsertBeforeManifestType(normalized, replacement);
	}

	private static string SetRootBlock(string text, string key, string value)
	{
		string normalized = NormalizeLines(text);
		string replacement = key + ": |-\n" + string.Join('\n', NormalizeLines(value).Split('\n').Select(line => "  " + line)) + "\n";
		Regex regex = new($@"(?m)^{Regex.Escape(key)}[ \t]*:[ \t]*(?:\|-?|>-?)?[ \t]*\n(?:^[ \t]+.*\n?)*");
		return regex.IsMatch(normalized) ? regex.Replace(normalized, replacement, 1) : InsertBeforeManifestType(normalized, replacement);
	}

	private static string InsertBeforeManifestType(string text, string insertion)
	{
		Match match = Regex.Match(text, @"(?m)^ManifestType\s*:");
		if (!match.Success) return text.TrimEnd() + "\n" + insertion;
		return text.Insert(match.Index, insertion);
	}

	private static string FirstScalar(string key, params string[] texts)
	{
		foreach (string text in texts)
		{
			Match match = Regex.Match(text ?? string.Empty, $@"(?m)^{Regex.Escape(key)}\s*:\s*(.*?)\s*$", RegexOptions.IgnoreCase);
			if (match.Success) return Unquote(match.Groups[1].Value);
		}
		return string.Empty;
	}

	private static string FirstIndentedScalar(string key, string text)
	{
		Match match = Regex.Match(text, $@"(?m)^\s+(?:-\s*)?{Regex.Escape(key)}\s*:\s*(.*?)\s*$", RegexOptions.IgnoreCase);
		return match.Success ? Unquote(match.Groups[1].Value) : string.Empty;
	}

	private static IReadOnlyList<string> ReadRootList(string text, string key)
	{
		Match match = Regex.Match(text ?? string.Empty, $@"(?m)^{Regex.Escape(key)}[ \t]*:[ \t]*\n((?:^[ \t]*-[ \t]+.*\n?)*)", RegexOptions.IgnoreCase);
		if (!match.Success) return [];
		return match.Groups[1].Value.Split('\n')
			.Select(line => Regex.Match(line, @"^\s*-\s+(.*)$"))
			.Where(item => item.Success)
			.Select(item => Unquote(item.Groups[1].Value.Trim()))
			.ToArray();
	}

	private static string ReadBlock(string text, string key)
	{
		Match match = Regex.Match(text ?? string.Empty, $@"(?m)^{Regex.Escape(key)}[ \t]*:[ \t]*(?:\|-?|>-?)?[ \t]*\n((?:^[ \t]+.*\n?)*)", RegexOptions.IgnoreCase);
		if (!match.Success) return FirstScalar(key, text ?? string.Empty);
		return string.Join('\n', match.Groups[1].Value.Split('\n').Select(line => line.StartsWith("  ") ? line[2..] : line.TrimStart())).Trim();
	}

	private static string ReadOptional(string? path) => path is null ? string.Empty : ReadManifestText(path);

	private static string ReadManifestText(string path)
	{
		FileInfo information = new(path);
		if (information.Length > MaximumManifestBytes)
			throw new InvalidDataException($"{information.Name} is too large to be a Winget manifest ({information.Length / (1024d * 1024):0.0} MB).");
		return File.ReadAllText(path);
	}

	private static string Yaml(string value)
	{
		value ??= string.Empty;
		if (value.Length > 0 && Regex.IsMatch(value, @"^[A-Za-z0-9][A-Za-z0-9._/+-]*$") &&
			!value.Equals("true", StringComparison.OrdinalIgnoreCase) && !value.Equals("false", StringComparison.OrdinalIgnoreCase) && !value.Equals("null", StringComparison.OrdinalIgnoreCase))
			return value;
		return "'" + value.Replace("'", "''") + "'";
	}

	private static string Unquote(string value)
	{
		value = value.Trim();
		if (value.Length >= 2 && value[0] == '\'' && value[^1] == '\'') return value[1..^1].Replace("''", "'");
		if (value.Length >= 2 && value[0] == '"' && value[^1] == '"') return value[1..^1].Replace("\\\"", "\"");
		return value;
	}

	private static string[] SplitValues(string value) => value.Split([',', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

	private static void AppendScalar(StringBuilder text, string key, string value) => text.AppendLine($"{key}: {Yaml(value)}");
	private static void AppendOptionalScalar(StringBuilder text, string key, string value) { if (!string.IsNullOrWhiteSpace(value)) AppendScalar(text, key, value); }
	private static void AppendList(StringBuilder text, string key, IReadOnlyList<string> values)
	{
		if (values.Count == 0) return;
		text.AppendLine(key + ":");
		foreach (string value in values) text.AppendLine("  - " + Yaml(value));
	}

	private static string NormalizeLines(string value) => (value ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
	private static string EnsureFinalNewLine(string value) => NormalizeLines(value).TrimEnd() + Environment.NewLine;

	private static void WriteAtomically(string path, string contents)
	{
		string temporary = path + ".tmp";
		try
		{
			File.WriteAllText(temporary, contents, new UTF8Encoding(false));
			File.Move(temporary, path, true);
		}
		finally
		{
			if (File.Exists(temporary)) File.Delete(temporary);
		}
	}

	[GeneratedRegex(@"^[^.\s\\/:*?""<>|\x01-\x1f]{1,32}(\.[^.\s\\/:*?""<>|\x01-\x1f]{1,32}){1,7}$", RegexOptions.CultureInvariant)]
	private static partial Regex PackageIdRegex();

	[GeneratedRegex(@"[\\/:*?""<>|\x01-\x1f]", RegexOptions.CultureInvariant)]
	private static partial Regex InvalidVersionCharactersRegex();

	[GeneratedRegex(@"^([a-zA-Z]{2,3}|[iI]-[a-zA-Z]+|[xX]-[a-zA-Z]{1,8})(-[a-zA-Z]{1,8})*$", RegexOptions.CultureInvariant)]
	private static partial Regex LocaleRegex();

	[GeneratedRegex(@"^\d+\.\d+\.\d+$", RegexOptions.CultureInvariant)]
	private static partial Regex ManifestVersionRegex();

	[GeneratedRegex(@"^[A-Fa-f0-9]{64}$", RegexOptions.CultureInvariant)]
	private static partial Regex ShaRegex();

	private sealed record ManifestFiles(string? Version, string? Locale, string? Installer);
}

internal static class StringExtensions
{
	public static string IfEmpty(this string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;
}
