using System.Text.Json;

namespace ManifestUpdater;

internal static class StudioStateStore
{
	private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
	private static string StateFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WingetManifestStudio");
	private static string StatePath => Path.Combine(StateFolder, "studio-state.json");
	private static string RecoveryPath => Path.Combine(StateFolder, "recovery.wingetprofile.json");

	public static void SaveRecovery(ManifestProject project)
	{
		Directory.CreateDirectory(StateFolder);
		ProfileStore.Save(RecoveryPath, project);
		if (!string.IsNullOrWhiteSpace(project.ManifestFolder)) AddRecentFolder(project.ManifestFolder);
	}

	public static ManifestProject? LoadRecovery()
	{
		try { return File.Exists(RecoveryPath) ? ProfileStore.Load(RecoveryPath) : null; }
		catch { return null; }
	}

	public static IReadOnlyList<string> GetRecentFolders()
	{
		try
		{
			if (!File.Exists(StatePath)) return [];
			StudioState? state = JsonSerializer.Deserialize<StudioState>(File.ReadAllText(StatePath), Options);
			return state?.RecentFolders.Where(Directory.Exists).Take(8).ToArray() ?? [];
		}
		catch { return []; }
	}

	public static string GetLanguage()
	{
		try
		{
			if (!File.Exists(StatePath)) return "en-US";
			StudioState? state = JsonSerializer.Deserialize<StudioState>(File.ReadAllText(StatePath), Options);
			return string.IsNullOrWhiteSpace(state?.Language) ? "en-US" : state.Language;
		}
		catch { return "en-US"; }
	}

	public static void SetLanguage(string language)
	{
		string normalized = StudioLocalization.IsSupported(language) ? language : "en-US";
		Directory.CreateDirectory(StateFolder);
		string temporary = StatePath + ".tmp";
		File.WriteAllText(temporary, JsonSerializer.Serialize(new StudioState(GetRecentFolders().ToArray(), normalized), Options));
		File.Move(temporary, StatePath, true);
	}

	public static void AddRecentFolder(string folder)
	{
		if (string.IsNullOrWhiteSpace(folder)) return;
		string fullPath;
		try { fullPath = Path.GetFullPath(folder); }
		catch { return; }
		List<string> folders = GetRecentFolders().Where(path => !string.Equals(path, fullPath, StringComparison.OrdinalIgnoreCase)).ToList();
		folders.Insert(0, fullPath);
		Directory.CreateDirectory(StateFolder);
		string temporary = StatePath + ".tmp";
		File.WriteAllText(temporary, JsonSerializer.Serialize(new StudioState(folders.Take(8).ToArray(), GetLanguage()), Options));
		File.Move(temporary, StatePath, true);
	}

	private sealed record StudioState(string[] RecentFolders, string Language = "en-US");
}
