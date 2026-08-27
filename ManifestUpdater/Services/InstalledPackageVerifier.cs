using System.Runtime.InteropServices;
using System.Text;

namespace ManifestUpdater;

internal static class InstalledPackageVerifier
{
	private const int InstallStateDefault = 5;
	private const uint ErrorSuccess = 0;
	private const uint ErrorMoreData = 234;

	public static async Task<InstalledPackageVerification> VerifyAsync(
		ManifestProject project,
		CancellationToken cancellationToken = default)
	{
		CommandResult identifierResult = await WingetCommandService.ListInstalledPackageAsync(project.PackageIdentifier, cancellationToken);
		string identifierOutput = identifierResult.CombinedOutput;
		if (ContainsValue(identifierOutput, project.PackageIdentifier))
		{
			bool versionMatches = VersionsMatchOutput(identifierOutput, project.PackageVersion);
			return new InstalledPackageVerification(
				true,
				versionMatches,
				"Winget package identifier",
				project.PackageIdentifier,
				versionMatches ? project.PackageVersion : string.Empty,
				identifierOutput);
		}

		foreach (InstallerArtifact installer in project.Installers.Where(item => !string.IsNullOrWhiteSpace(item.ProductCode)))
		{
			MsiProduct? installedMsi = TryGetInstalledMsi(installer.ProductCode);
			if (installedMsi is null) continue;
			bool versionMatches = VersionsMatch(installedMsi.Version, project.PackageVersion)
				|| VersionsMatch(installedMsi.Version, installer.ProductVersion);
			return new InstalledPackageVerification(
				true,
				versionMatches,
				"MSI ProductCode",
				installedMsi.Name.IfEmpty(project.PackageName),
				installedMsi.Version,
				$"Winget did not retain the manifest ID in its installed-package list, so the Studio verified the exact MSI ProductCode {installer.ProductCode} with Windows Installer.");
		}

		string[] names = project.Installers
			.Select(installer => installer.DisplayName)
			.Append(project.PackageName)
			.Where(name => !string.IsNullOrWhiteSpace(name))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();
		foreach (string name in names)
		{
			CommandResult nameResult = await WingetCommandService.ListInstalledPackageByNameAsync(name, cancellationToken);
			string nameOutput = nameResult.CombinedOutput;
			if (!ContainsValue(nameOutput, name)) continue;
			bool versionMatches = VersionsMatchOutput(nameOutput, project.PackageVersion)
				|| project.Installers.Any(installer => VersionsMatchOutput(nameOutput, installer.ProductVersion));
			return new InstalledPackageVerification(
				true,
				versionMatches,
				"installed application name",
				name,
				versionMatches ? project.PackageVersion : string.Empty,
				nameOutput);
		}

		return new InstalledPackageVerification(
			false,
			false,
			"Winget ID, MSI ProductCode, and installed application name",
			string.Empty,
			string.Empty,
			identifierOutput.IfEmpty("Winget returned no installed-package details."));
	}

	internal static bool VersionsMatch(string installed, string expected)
	{
		if (string.IsNullOrWhiteSpace(installed) || string.IsNullOrWhiteSpace(expected)) return false;
		if (string.Equals(installed.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase)) return true;

		string[] left = NumericVersionParts(installed);
		string[] right = NumericVersionParts(expected);
		if (left.Length == 0 || right.Length == 0) return false;
		int length = Math.Max(left.Length, right.Length);
		for (int index = 0; index < length; index++)
		{
			string leftPart = index < left.Length ? left[index] : "0";
			string rightPart = index < right.Length ? right[index] : "0";
			if (!int.TryParse(leftPart, out int leftNumber) || !int.TryParse(rightPart, out int rightNumber) || leftNumber != rightNumber)
				return false;
		}
		return true;
	}

	internal static bool VersionsMatchOutput(string output, string expected) =>
		!string.IsNullOrWhiteSpace(expected) && output.Contains(expected.Trim(), StringComparison.OrdinalIgnoreCase);

	private static string[] NumericVersionParts(string version)
	{
		string normalized = version.Trim().TrimStart('v', 'V');
		int suffix = normalized.IndexOfAny(['-', '+', ' ']);
		if (suffix >= 0) normalized = normalized[..suffix];
		return normalized.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
	}

	private static bool ContainsValue(string output, string value) =>
		!string.IsNullOrWhiteSpace(value) && output.Contains(value.Trim(), StringComparison.OrdinalIgnoreCase);

	private static MsiProduct? TryGetInstalledMsi(string productCode)
	{
		try
		{
			if (MsiQueryProductState(productCode) != InstallStateDefault) return null;
			return new MsiProduct(
				ReadMsiProductInfo(productCode, "InstalledProductName"),
				ReadMsiProductInfo(productCode, "VersionString"));
		}
		catch
		{
			return null;
		}
	}

	private static string ReadMsiProductInfo(string productCode, string property)
	{
		uint length = 0;
		uint result = MsiGetProductInfo(productCode, property, null, ref length);
		if (result is not (ErrorSuccess or ErrorMoreData)) return string.Empty;
		StringBuilder value = new(checked((int)length + 1));
		uint capacity = (uint)value.Capacity;
		return MsiGetProductInfo(productCode, property, value, ref capacity) == ErrorSuccess ? value.ToString() : string.Empty;
	}

	[DllImport("msi.dll", EntryPoint = "MsiQueryProductStateW", CharSet = CharSet.Unicode)]
	private static extern int MsiQueryProductState(string productCode);

	[DllImport("msi.dll", EntryPoint = "MsiGetProductInfoW", CharSet = CharSet.Unicode)]
	private static extern uint MsiGetProductInfo(string productCode, string property, StringBuilder? value, ref uint valueLength);

	private sealed record MsiProduct(string Name, string Version);
}

internal sealed record InstalledPackageVerification(
	bool Found,
	bool VersionMatches,
	string Method,
	string InstalledName,
	string InstalledVersion,
	string Diagnostic);
