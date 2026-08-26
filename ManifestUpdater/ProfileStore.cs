using System.Text.Json;

namespace ManifestUpdater;

internal static class ProfileStore
{
	private static readonly JsonSerializerOptions Options = new()
	{
		WriteIndented = true,
		PropertyNameCaseInsensitive = true
	};

	public static void Save(string path, ManifestProject project)
	{
		string? folder = Path.GetDirectoryName(path);
		if (!string.IsNullOrWhiteSpace(folder))
			Directory.CreateDirectory(folder);
		string temporaryPath = path + ".tmp";
		File.WriteAllText(temporaryPath, JsonSerializer.Serialize(project, Options));
		File.Move(temporaryPath, path, true);
	}

	public static ManifestProject Load(string path)
	{
		ManifestProject project = JsonSerializer.Deserialize<ManifestProject>(File.ReadAllText(path), Options)
			?? throw new InvalidDataException("The selected project profile is empty or invalid.");
		project.EnsureInstallerCollection();
		return project;
	}
}
