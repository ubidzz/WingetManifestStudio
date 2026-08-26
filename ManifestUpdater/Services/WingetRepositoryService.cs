using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ManifestUpdater;

internal static class WingetRepositoryService
{
	private const string RepositoryBase = "https://github.com/microsoft/winget-pkgs/tree/master/";
	private static readonly HttpClient Client = CreateClient();

	public static async Task<RepositoryCheckResult> CheckAsync(string packageIdentifier, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(packageIdentifier) || !packageIdentifier.Contains('.'))
			throw new ArgumentException("Enter a complete Publisher.Application package identifier first.", nameof(packageIdentifier));

		Task<CommandResult> wingetTask = WingetCommandService.SearchExactPackageAsync(packageIdentifier, cancellationToken);
		Task<(bool found, string url, string latest)> githubTask = CheckGitHubAsync(packageIdentifier, cancellationToken);
		await Task.WhenAll(wingetTask, githubTask);
		CommandResult winget = await wingetTask;
		(bool githubFound, string githubUrl, string latestVersion) = await githubTask;
		bool wingetFound = winget.ExitCode == 0 && winget.CombinedOutput.Contains(packageIdentifier, StringComparison.OrdinalIgnoreCase);
		string summary = githubFound || wingetFound
			? $"An existing Winget package was found{(latestVersion.Length > 0 ? $". Latest repository folder: {latestVersion}" : string.Empty)}. Treat this project as an update and keep the exact package identifier."
			: "No exact package identifier was found in the Winget source or the microsoft/winget-pkgs repository. This appears to be a new package.";
		return new RepositoryCheckResult(wingetFound, githubFound, winget.CombinedOutput, githubUrl, latestVersion, summary);
	}

	private static async Task<(bool found, string url, string latest)> CheckGitHubAsync(string identifier, CancellationToken cancellationToken)
	{
		string path = BuildRepositoryPath(identifier);
		string apiUrl = "https://api.github.com/repos/microsoft/winget-pkgs/contents/" + path;
		using HttpResponseMessage response = await Client.GetAsync(apiUrl, cancellationToken);
		if (response.StatusCode == HttpStatusCode.NotFound) return (false, string.Empty, string.Empty);
		response.EnsureSuccessStatusCode();
		JsonElement entries = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
		string[] versions = entries.ValueKind == JsonValueKind.Array
			? entries.EnumerateArray()
				.Where(item => item.TryGetProperty("type", out JsonElement type) && type.GetString() == "dir")
				.Select(item => item.TryGetProperty("name", out JsonElement name) ? name.GetString() ?? string.Empty : string.Empty)
				.Where(name => name.Length > 0)
				.ToArray()
			: [];
		string latest = versions.OrderByDescending(VersionRank).ThenByDescending(value => value, StringComparer.OrdinalIgnoreCase).FirstOrDefault() ?? string.Empty;
		return (true, RepositoryBase + path, latest);
	}

	internal static string BuildRepositoryPath(string identifier)
	{
		string[] parts = identifier.Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length < 2) throw new ArgumentException("The package identifier needs at least two dot-separated parts.", nameof(identifier));
		string partition = char.ToLowerInvariant(parts[0][0]).ToString();
		return "manifests/" + string.Join('/', new[] { partition }.Concat(parts).Select(Uri.EscapeDataString));
	}

	private static Version VersionRank(string value)
	{
		string normalized = value.Trim().TrimStart('v', 'V');
		string numeric = new(normalized.TakeWhile(character => char.IsDigit(character) || character == '.').ToArray());
		return Version.TryParse(numeric.TrimEnd('.'), out Version? version) ? version : new Version(0, 0);
	}

	private static HttpClient CreateClient()
	{
		HttpClient client = new() { Timeout = TimeSpan.FromSeconds(20) };
		client.DefaultRequestHeaders.UserAgent.ParseAdd("WingetManifestStudio/1.0");
		client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
		return client;
	}
}
