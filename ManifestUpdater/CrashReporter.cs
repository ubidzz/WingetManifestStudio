namespace ManifestUpdater;

internal static class CrashReporter
{
	private static readonly object Sync = new();

	public static string LogFolder => Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		"WingetManifestStudio",
		"Logs");

	public static string Report(Exception exception, string context)
	{
		try
		{
			Directory.CreateDirectory(LogFolder);
			string path = Path.Combine(LogFolder, $"error_{DateTime.Now:yyyyMMdd}.log");
			string entry = $"[{DateTime.Now:O}] {context}{Environment.NewLine}{exception}{Environment.NewLine}{new string('-', 90)}{Environment.NewLine}";
			lock (Sync) File.AppendAllText(path, entry);
			return path;
		}
		catch
		{
			return string.Empty;
		}
	}
}
