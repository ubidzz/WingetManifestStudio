using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ManifestUpdater;

internal static class GitHubReleaseService
{
	private static readonly string[] SupportedExtensions =
	[
		".msi", ".exe", ".msix", ".msixbundle", ".appx", ".appxbundle", ".zip",
		".otf", ".otc", ".ttf", ".ttc", ".fnt"
	];
	private static readonly HttpClient Client = CreateClient();

	public static async Task<GitHubReleaseImport> ReadAsync(string releaseUrl, CancellationToken cancellationToken = default)
	{
		(string owner, string repository, string tag, bool latest) = ParseReleaseUrl(releaseUrl);
		string releaseApi = $"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repository)}/releases/"
			+ (latest ? "latest" : "tags/" + Uri.EscapeDataString(tag));
		JsonElement release = await GetJsonAsync(releaseApi, "The GitHub release could not be found.", cancellationToken);
		JsonElement repo = await GetJsonAsync(
			$"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repository)}",
			"The GitHub repository could not be read.", cancellationToken);

		string actualTag = String(release, "tag_name");
		List<GitHubReleaseAsset> assets = [];
		if (release.TryGetProperty("assets", out JsonElement assetArray) && assetArray.ValueKind == JsonValueKind.Array)
		{
			foreach (JsonElement asset in assetArray.EnumerateArray())
			{
				string name = String(asset, "name");
				string downloadUrl = String(asset, "browser_download_url");
				if (!IsSupportedInstallerAsset(name) || downloadUrl.Length == 0) continue;
				assets.Add(new GitHubReleaseAsset(name, downloadUrl, Int64(asset, "size"), String(asset, "content_type")));
			}
		}

		string license = string.Empty;
		string licenseUrl = string.Empty;
		if (repo.TryGetProperty("license", out JsonElement licenseNode) && licenseNode.ValueKind == JsonValueKind.Object)
		{
			license = String(licenseNode, "spdx_id");
			if (license.Equals("NOASSERTION", StringComparison.OrdinalIgnoreCase)) license = string.Empty;
			licenseUrl = String(licenseNode, "html_url");
		}
		string topics = repo.TryGetProperty("topics", out JsonElement topicArray) && topicArray.ValueKind == JsonValueKind.Array
			? string.Join(", ", topicArray.EnumerateArray().Select(item => item.GetString()).Where(value => !string.IsNullOrWhiteSpace(value)))
			: string.Empty;
		string repositoryUrl = String(repo, "html_url");
		string ownerUrl = repo.TryGetProperty("owner", out JsonElement ownerNode) ? String(ownerNode, "html_url") : string.Empty;
		string issuesUrl = repo.TryGetProperty("has_issues", out JsonElement hasIssues) && hasIssues.ValueKind == JsonValueKind.True
			? repositoryUrl.TrimEnd('/') + "/issues"
			: repositoryUrl;
		string published = String(release, "published_at");
		string releaseDate = DateTimeOffset.TryParse(published, out DateTimeOffset date) ? date.ToString("yyyy-MM-dd") : string.Empty;

		return new GitHubReleaseImport(
			owner,
			repository,
			actualTag,
			NormalizeVersion(actualTag),
			String(release, "name"),
			String(release, "body"),
			String(release, "html_url"),
			repositoryUrl,
			ownerUrl,
			issuesUrl,
			license,
			licenseUrl,
			String(repo, "description"),
			topics,
			releaseDate,
			assets);
	}

	internal static bool IsSupportedInstallerAsset(string name)
	{
		string extension = Path.GetExtension(name);
		if (!SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)) return false;
		string normalized = name.Replace('_', '-').ToLowerInvariant();
		string[] nonInstallerMarkers = ["checksum", "sha256", "hashes", "symbols", "-debug", "source-code", ".pdb"];
		return !nonInstallerMarkers.Any(normalized.Contains);
	}

	internal static (string Owner, string Repository, string Tag, bool Latest) ParseReleaseUrl(string releaseUrl)
	{
		if (!Uri.TryCreate(releaseUrl?.Trim(), UriKind.Absolute, out Uri? uri)
			|| !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
			throw new ArgumentException("Paste a GitHub release URL, for example https://github.com/owner/project/releases/tag/v1.2.3.", nameof(releaseUrl));
		string[] parts = uri.AbsolutePath.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length < 4 || !parts[2].Equals("releases", StringComparison.OrdinalIgnoreCase))
			throw new ArgumentException("The URL must point to a GitHub release or the repository's latest release page.", nameof(releaseUrl));
		if (parts[3].Equals("latest", StringComparison.OrdinalIgnoreCase))
			return (parts[0], parts[1], string.Empty, true);
		if (parts.Length >= 5 && parts[3].Equals("tag", StringComparison.OrdinalIgnoreCase))
			return (parts[0], parts[1], Uri.UnescapeDataString(string.Join('/', parts.Skip(4))), false);
		throw new ArgumentException("The URL must contain /releases/tag/version or /releases/latest.", nameof(releaseUrl));
	}

	private static async Task<JsonElement> GetJsonAsync(string url, string notFoundMessage, CancellationToken cancellationToken)
	{
		using HttpResponseMessage response = await Client.GetAsync(url, cancellationToken);
		if (response.StatusCode == HttpStatusCode.NotFound) throw new InvalidDataException(notFoundMessage);
		if ((int)response.StatusCode == 403)
			throw new InvalidOperationException("GitHub temporarily refused the public release request. Wait a few minutes, then try again.");
		response.EnsureSuccessStatusCode();
		return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
	}

	private static string NormalizeVersion(string tag)
	{
		string value = (tag ?? string.Empty).Trim();
		if (value.StartsWith('v') && value.Length > 1 && char.IsDigit(value[1])) value = value[1..];
		return value;
	}

	private static string String(JsonElement node, string name) =>
		node.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;

	private static long Int64(JsonElement node, string name) =>
		node.TryGetProperty(name, out JsonElement value) && value.TryGetInt64(out long result) ? result : 0;

	private static HttpClient CreateClient()
	{
		HttpClient client = new() { Timeout = TimeSpan.FromSeconds(30) };
		client.DefaultRequestHeaders.UserAgent.ParseAdd("WingetManifestStudio/1.0");
		client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
		return client;
	}
}
