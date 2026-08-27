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

	public static async Task<RepositoryImportResult> ImportLatestAsync(
		string packageIdentifier,
		string destinationParent,
		IProgress<string>? progress = null,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(packageIdentifier) || !packageIdentifier.Contains('.'))
			throw new ArgumentException("Enter the exact Winget package ID in Publisher.Application format.", nameof(packageIdentifier));
		if (string.IsNullOrWhiteSpace(destinationParent))
			throw new ArgumentException("Choose a parent folder for the imported working copy.", nameof(destinationParent));

		string root = string.Empty;
		string packagePath = string.Empty;
		JsonElement versionEntries = default;
		foreach (string candidateRoot in new[] { "manifests", "fonts" })
		{
			string candidatePath = BuildRepositoryPath(packageIdentifier.Trim(), candidateRoot);
			(HttpStatusCode status, JsonElement contents) = await GetContentsAsync(candidatePath, cancellationToken);
			if (status == HttpStatusCode.NotFound) continue;
			root = candidateRoot;
			packagePath = candidatePath;
			versionEntries = contents;
			break;
		}
		if (root.Length == 0)
			throw new InvalidDataException($"No exact package named '{packageIdentifier}' was found in microsoft/winget-pkgs. Check the ID and try again.");

		string[] versions = DirectoryNames(versionEntries);
		if (versions.Length == 0)
			throw new InvalidDataException("The package exists, but no version folders were found in the Winget repository.");
		string version = versions.OrderByDescending(value => value, PackageVersionComparer.Instance).First();
		string versionPath = packagePath + "/" + Uri.EscapeDataString(version);
		progress?.Report($"Downloading the current {packageIdentifier} {version} manifests...");
		(HttpStatusCode fileStatus, JsonElement fileEntries) = await GetContentsAsync(versionPath, cancellationToken);
		if (fileStatus == HttpStatusCode.NotFound || fileEntries.ValueKind != JsonValueKind.Array)
			throw new InvalidDataException("The current Winget version folder could not be read.");

		List<(string Name, string Url)> manifestFiles = fileEntries.EnumerateArray()
			.Where(item => item.TryGetProperty("type", out JsonElement type) && type.GetString() == "file")
			.Select(item => (
				Name: item.TryGetProperty("name", out JsonElement name) ? name.GetString() ?? string.Empty : string.Empty,
				Url: item.TryGetProperty("download_url", out JsonElement url) ? url.GetString() ?? string.Empty : string.Empty))
			.Where(item => (item.Name.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || item.Name.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)) && item.Url.Length > 0)
			.ToList();
		if (manifestFiles.Count == 0)
			throw new InvalidDataException("The current Winget version folder does not contain manifest YAML files.");

		string packageFolder = SafeFolderName(packageIdentifier.Trim());
		string versionFolder = SafeFolderName(version);
		string finalFolder = Path.Combine(Path.GetFullPath(destinationParent), packageFolder, versionFolder);
		if (Directory.Exists(finalFolder) && Directory.EnumerateFileSystemEntries(finalFolder).Any())
			finalFolder += "-import-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
		string stagingFolder = finalFolder + ".downloading-" + Guid.NewGuid().ToString("N");
		Directory.CreateDirectory(stagingFolder);
		try
		{
			foreach ((string name, string url) in manifestFiles)
			{
				progress?.Report("Downloading " + name + "...");
				string contents = await Client.GetStringAsync(url, cancellationToken);
				await File.WriteAllTextAsync(Path.Combine(stagingFolder, SafeFolderName(name)), contents, cancellationToken);
			}
			Directory.CreateDirectory(Path.GetDirectoryName(finalFolder)!);
			Directory.Move(stagingFolder, finalFolder);
		}
		catch
		{
			try { if (Directory.Exists(stagingFolder)) Directory.Delete(stagingFolder, true); } catch { }
			throw;
		}

		return new RepositoryImportResult(
			packageIdentifier.Trim(),
			version,
			finalFolder,
			manifestFiles.Select(item => item.Name).ToArray(),
			RepositoryBase + packagePath + "/" + Uri.EscapeDataString(version));
	}

	private static async Task<(bool found, string url, string latest)> CheckGitHubAsync(string identifier, CancellationToken cancellationToken)
	{
		foreach (string root in new[] { "manifests", "fonts" })
		{
			string path = BuildRepositoryPath(identifier, root);
			string apiUrl = "https://api.github.com/repos/microsoft/winget-pkgs/contents/" + path;
			using HttpResponseMessage response = await Client.GetAsync(apiUrl, cancellationToken);
			if (response.StatusCode == HttpStatusCode.NotFound) continue;
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
		return (false, string.Empty, string.Empty);
	}

	private static async Task<(HttpStatusCode Status, JsonElement Contents)> GetContentsAsync(string path, CancellationToken cancellationToken)
	{
		string apiUrl = "https://api.github.com/repos/microsoft/winget-pkgs/contents/" + path;
		using HttpResponseMessage response = await Client.GetAsync(apiUrl, cancellationToken);
		if (response.StatusCode == HttpStatusCode.NotFound) return (response.StatusCode, default);
		if ((int)response.StatusCode == 403)
			throw new InvalidOperationException("GitHub temporarily refused the public repository request. Wait a few minutes, then try again.");
		response.EnsureSuccessStatusCode();
		return (response.StatusCode, await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken));
	}

	private static string[] DirectoryNames(JsonElement entries) => entries.ValueKind == JsonValueKind.Array
		? entries.EnumerateArray()
			.Where(item => item.TryGetProperty("type", out JsonElement type) && type.GetString() == "dir")
			.Select(item => item.TryGetProperty("name", out JsonElement name) ? name.GetString() ?? string.Empty : string.Empty)
			.Where(name => name.Length > 0)
			.ToArray()
		: [];

	internal static string BuildRepositoryPath(string identifier, string root = "manifests")
	{
		string[] parts = identifier.Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length < 2) throw new ArgumentException("The package identifier needs at least two dot-separated parts.", nameof(identifier));
		if (root is not ("manifests" or "fonts")) throw new ArgumentException("The repository root must be manifests or fonts.", nameof(root));
		string partition = char.ToLowerInvariant(parts[0][0]).ToString();
		return root + "/" + string.Join('/', new[] { partition }.Concat(parts).Select(Uri.EscapeDataString));
	}

	private static Version VersionRank(string value)
	{
		string normalized = value.Trim().TrimStart('v', 'V');
		string numeric = new(normalized.TakeWhile(character => char.IsDigit(character) || character == '.').ToArray());
		return Version.TryParse(numeric.TrimEnd('.'), out Version? version) ? version : new Version(0, 0);
	}

	private static string SafeFolderName(string value)
	{
		string result = string.Concat(value.Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '-' : character));
		return string.IsNullOrWhiteSpace(result) ? "package" : result;
	}

	private sealed class PackageVersionComparer : IComparer<string>
	{
		public static readonly PackageVersionComparer Instance = new();

		public int Compare(string? left, string? right)
		{
			string[] leftParts = Tokenize(left);
			string[] rightParts = Tokenize(right);
			for (int index = 0; index < Math.Max(leftParts.Length, rightParts.Length); index++)
			{
				string leftPart = index < leftParts.Length ? leftParts[index] : "0";
				string rightPart = index < rightParts.Length ? rightParts[index] : "0";
				bool leftNumber = long.TryParse(leftPart, out long leftValue);
				bool rightNumber = long.TryParse(rightPart, out long rightValue);
				int comparison = leftNumber && rightNumber
					? leftValue.CompareTo(rightValue)
					: StringComparer.OrdinalIgnoreCase.Compare(leftPart, rightPart);
				if (comparison != 0) return comparison;
			}
			return StringComparer.OrdinalIgnoreCase.Compare(left, right);
		}

		private static string[] Tokenize(string? value) => (value ?? string.Empty).Trim().TrimStart('v', 'V')
			.Split(['.', '-', '_', '+'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
	}

	private static HttpClient CreateClient()
	{
		HttpClient client = new() { Timeout = TimeSpan.FromSeconds(20) };
		client.DefaultRequestHeaders.UserAgent.ParseAdd("WingetManifestStudio/1.0");
		client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
		return client;
	}
}
