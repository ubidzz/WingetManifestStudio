using System.Text.Json;

namespace ManifestUpdater;

internal static class StudioStateStore
{
	private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
	private static string StateFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WingetManifestStudio");
	private static string StatePath => Path.Combine(StateFolder, "studio-state.json");

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
		File.WriteAllText(temporary, JsonSerializer.Serialize(new StudioState(normalized), Options));
		File.Move(temporary, StatePath, true);
	}

	private sealed record StudioState(string Language = "en-US");
}
