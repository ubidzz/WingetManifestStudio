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

		// Keep Microsoft's download intact for inspection, and launch a narrowly
		// adapted copy for Windows PowerShell 5.1. The upstream script currently uses
		// Out-File -Path (the parameter is named -FilePath), and piping a ScriptBlock
		// through Out-File also formats and wraps long generated lines. Its legacy web
		// parser can display an Internet Explorer security prompt. Its token notice is
		// optional and unrelated to the WingetCreate credential stored by the Studio.
		string compatibilityPath = Path.Combine(folder, "SandboxTest.WingetManifestStudio.ps1");
		string compatibilityScript = CreateSandboxCompatibilityScript(script);
		string compatibilityTemporary = compatibilityPath + ".tmp";
		await File.WriteAllTextAsync(compatibilityTemporary, compatibilityScript, cancellationToken);
		File.Move(compatibilityTemporary, compatibilityPath, true);
		return compatibilityPath;
	}

	internal static string CreateSandboxCompatibilityScript(string officialScript)
	{
		string compatible = officialScript.Replace(
			"Invoke-WebRequest -Uri $URL -Method Head",
			"Invoke-WebRequest -UseBasicParsing -Uri $URL -Method Head",
			StringComparison.Ordinal);
		compatible = compatible.Replace(
			"$Script | Out-File -Path (Join-Path $script:TestDataFolder -ChildPath 'BoundParameterScript.ps1')",
			"[System.IO.File]::WriteAllText((Join-Path $script:TestDataFolder -ChildPath 'BoundParameterScript.ps1'), $Script.ToString(), [System.Text.UTF8Encoding]::new($false))",
			StringComparison.Ordinal);

		const string tokenWarningStart = "Write-Warning @\"\r\nA valid GitHub token was not provided.";
		const string tokenWarningStartLf = "Write-Warning @\"\nA valid GitHub token was not provided.";
		compatible = compatible.Replace(tokenWarningStart, "Write-Verbose @\"\r\nA valid GitHub token was not provided.", StringComparison.Ordinal);
		compatible = compatible.Replace(tokenWarningStartLf, "Write-Verbose @\"\nA valid GitHub token was not provided.", StringComparison.Ordinal);
		return compatible;
	}

	private static HttpClient CreateClient()
	{
		HttpClient client = new() { Timeout = TimeSpan.FromMinutes(2) };
		client.DefaultRequestHeaders.UserAgent.ParseAdd("WingetManifestStudio/1.0");
		return client;
	}
}
