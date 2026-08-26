using System.ComponentModel;
using System.Text.Json.Serialization;

namespace ManifestUpdater;

public sealed class ManifestProject
{
	public string ProfileName { get; set; } = "New package";
	public string PackageIdentifier { get; set; } = string.Empty;
	public string PackageVersion { get; set; } = string.Empty;
	public string DefaultLocale { get; set; } = "en-US";
	public string ManifestVersion { get; set; } = "1.12.0";
	public string ManifestFolder { get; set; } = string.Empty;
	public string Publisher { get; set; } = string.Empty;
	public string PublisherUrl { get; set; } = string.Empty;
	public string PublisherSupportUrl { get; set; } = string.Empty;
	public string PrivacyUrl { get; set; } = string.Empty;
	public string Author { get; set; } = string.Empty;
	public string PackageName { get; set; } = string.Empty;
	public string PackageUrl { get; set; } = string.Empty;
	public string License { get; set; } = string.Empty;
	public string LicenseUrl { get; set; } = string.Empty;
	public string Copyright { get; set; } = string.Empty;
	public string CopyrightUrl { get; set; } = string.Empty;
	public string PurchaseUrl { get; set; } = string.Empty;
	public string ShortDescription { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string Moniker { get; set; } = string.Empty;
	public string Tags { get; set; } = string.Empty;
	public string Commands { get; set; } = string.Empty;
	public string ReleaseNotes { get; set; } = string.Empty;
	public string ReleaseNotesUrl { get; set; } = string.Empty;
	public string InstallationNotes { get; set; } = string.Empty;
	public string Channel { get; set; } = string.Empty;
	public string InstallerLocale { get; set; } = string.Empty;
	public string Platform { get; set; } = "Windows.Desktop";
	public string MinimumOSVersion { get; set; } = string.Empty;
	public string InstallerType { get; set; } = "exe";
	public string NestedInstallerType { get; set; } = string.Empty;
	public string Scope { get; set; } = "user";
	public string InstallModes { get; set; } = "interactive, silent, silentWithProgress";
	public string UpgradeBehavior { get; set; } = "install";
	public string ElevationRequirement { get; set; } = string.Empty;
	public string SwitchSilent { get; set; } = string.Empty;
	public string SwitchSilentWithProgress { get; set; } = string.Empty;
	public string SwitchInteractive { get; set; } = string.Empty;
	public string SwitchInstallLocation { get; set; } = string.Empty;
	public string SwitchLog { get; set; } = string.Empty;
	public string SwitchUpgrade { get; set; } = string.Empty;
	public string CustomInstallerSwitch { get; set; } = string.Empty;
	public string SwitchRepair { get; set; } = string.Empty;
	public string Protocols { get; set; } = string.Empty;
	public string FileExtensions { get; set; } = string.Empty;
	public string UnsupportedOSArchitectures { get; set; } = string.Empty;
	public string InstallerSuccessCodes { get; set; } = string.Empty;
	public string PackageFamilyName { get; set; } = string.Empty;
	public string ReleaseDate { get; set; } = string.Empty;
	public string InstallerAbortsTerminal { get; set; } = string.Empty;
	public string InstallLocationRequired { get; set; } = string.Empty;
	public string RequireExplicitUpgrade { get; set; } = string.Empty;
	public string DisplayInstallWarnings { get; set; } = string.Empty;
	public string DownloadCommandProhibited { get; set; } = string.Empty;
	public string RepairBehavior { get; set; } = string.Empty;
	public string ArchiveBinariesDependOnPath { get; set; } = string.Empty;
	public string AdvancedLocaleFieldsYaml { get; set; } = string.Empty;
	public string AdvancedInstallerFieldsYaml { get; set; } = string.Empty;
	public bool AllowInsecureUrls { get; set; }
	public BindingList<InstallerArtifact> Installers { get; set; } = [];

	[JsonIgnore]
	public bool LoadedFromExistingManifests { get; set; }

	public void EnsureInstallerCollection()
	{
		Installers ??= [];
	}
}

public sealed class InstallerArtifact : INotifyPropertyChanged
{
	private string localFile = string.Empty;
	private string installerUrl = string.Empty;
	private string architecture = "x64";
	private string installerType = string.Empty;
	private string scope = string.Empty;
	private string sha256 = string.Empty;
	private string productCode = string.Empty;
	private string upgradeCode = string.Empty;
	private string displayName = string.Empty;
	private string publisher = string.Empty;
	private string productVersion = string.Empty;
	private string verificationStatus = "Not inspected";
	private string signatureSha256 = string.Empty;
	private string signatureStatus = "Not inspected";
	private string signerName = string.Empty;
	private string signerThumbprint = string.Empty;
	private string signatureExpiration = string.Empty;
	private string advancedFieldsYaml = string.Empty;

	public string LocalFile { get => localFile; set => Set(ref localFile, value); }
	public string InstallerUrl { get => installerUrl; set => Set(ref installerUrl, value); }
	public string Architecture { get => architecture; set => Set(ref architecture, value); }
	public string InstallerType { get => installerType; set => Set(ref installerType, value); }
	public string Scope { get => scope; set => Set(ref scope, value); }
	public string Sha256 { get => sha256; set => Set(ref sha256, value); }
	public string ProductCode { get => productCode; set => Set(ref productCode, value); }
	public string UpgradeCode { get => upgradeCode; set => Set(ref upgradeCode, value); }
	public string DisplayName { get => displayName; set => Set(ref displayName, value); }
	public string Publisher { get => publisher; set => Set(ref publisher, value); }
	public string ProductVersion { get => productVersion; set => Set(ref productVersion, value); }
	public string VerificationStatus { get => verificationStatus; set => Set(ref verificationStatus, value); }
	public string SignatureSha256 { get => signatureSha256; set => Set(ref signatureSha256, value); }
	public string SignatureStatus { get => signatureStatus; set => Set(ref signatureStatus, value); }
	public string SignerName { get => signerName; set => Set(ref signerName, value); }
	public string SignerThumbprint { get => signerThumbprint; set => Set(ref signerThumbprint, value); }
	public string SignatureExpiration { get => signatureExpiration; set => Set(ref signatureExpiration, value); }
	public string AdvancedFieldsYaml { get => advancedFieldsYaml; set => Set(ref advancedFieldsYaml, value); }

	public event PropertyChangedEventHandler? PropertyChanged;

	private void Set(ref string field, string value, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
	{
		if (string.Equals(field, value, StringComparison.Ordinal))
			return;
		field = value ?? string.Empty;
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}

public sealed record ManifestGenerationResult(
	IReadOnlyDictionary<string, string> Files,
	IReadOnlyList<string> Changes,
	IReadOnlyList<string> Warnings);

public sealed record InstallerInspection(
	string Sha256,
	string Architecture,
	string InstallerType,
	string ProductCode,
	string UpgradeCode,
	string ProductVersion,
	string DisplayName,
	string Publisher,
	long FileSize,
	AuthenticodeInspection Signature,
	string SignatureSha256);

public sealed record AuthenticodeInspection(
	string Status,
	bool IsSigned,
	bool IsTrusted,
	string SignerName,
	string Thumbprint,
	DateTimeOffset? NotBefore,
	DateTimeOffset? NotAfter,
	string StatusMessage);

public sealed record RepositoryCheckResult(
	bool WingetFound,
	bool GitHubFound,
	string WingetOutput,
	string GitHubUrl,
	string LatestVersion,
	string Summary);

public sealed record CommandResult(int ExitCode, string Output, string Error)
{
	public string CombinedOutput => string.Join(
		Environment.NewLine,
		new[] { Output.Trim(), Error.Trim() }.Where(value => value.Length > 0));
}
