namespace ManifestUpdater;

internal static class OfficialTestAssets
{
	public const string SandboxTestSource = "https://raw.githubusercontent.com/microsoft/winget-pkgs/master/Tools/SandboxTest.ps1";
	private static readonly HttpClient Client = CreateClient();

	public static async Task<string> GetSandboxTestScriptAsync(CancellationToken cancellationToken = default)
	{
		string folder = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"WingetManifestStudio", "OfficialTools");
		Directory.CreateDirectory(folder);
		string path = Path.Combine(folder, "SandboxTest.ps1");
		string script = await Client.GetStringAsync(SandboxTestSource, cancellationToken);
		if (!script.Contains("Microsoft", StringComparison.OrdinalIgnoreCase)
			|| !script.Contains("WindowsSandbox", StringComparison.Ordinal)
			|| !script.Contains("winget install", StringComparison.OrdinalIgnoreCase))
			throw new InvalidDataException("Microsoft's downloaded SandboxTest script did not contain the expected Winget Sandbox workflow.");
		string temporary = path + ".tmp";
		await File.WriteAllTextAsync(temporary, script, cancellationToken);
		File.Move(temporary, path, true);
		return path;
	}

	private static HttpClient CreateClient()
	{
		HttpClient client = new() { Timeout = TimeSpan.FromMinutes(2) };
		client.DefaultRequestHeaders.UserAgent.ParseAdd("WingetManifestStudio/1.0");
		return client;
	}
}
