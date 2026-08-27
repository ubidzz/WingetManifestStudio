namespace ManifestUpdater;

internal static class ManifestSchemaSupport
{
	public const string CurrentVersion = "1.28.0";

	public static readonly IReadOnlyList<string> SupportedVersions =
	[
		CurrentVersion,
		"1.12.0",
		"1.10.0",
		"1.9.0",
		"1.7.0"
	];

	public static string RecommendedForWinget(string wingetVersion)
	{
		string normalized = (wingetVersion ?? string.Empty).Trim().TrimStart('v', 'V');
		int suffix = normalized.IndexOfAny(['-', '+', ' ']);
		if (suffix >= 0) normalized = normalized[..suffix];
		if (!Version.TryParse(normalized, out Version? version)) return CurrentVersion;
		if (version.Major > 1 || version.Major == 1 && version.Minor >= 28) return CurrentVersion;
		if (version.Major == 1 && version.Minor >= 12) return "1.12.0";
		if (version.Major == 1 && version.Minor >= 10) return "1.10.0";
		if (version.Major == 1 && version.Minor >= 9) return "1.9.0";
		return "1.7.0";
	}
}
