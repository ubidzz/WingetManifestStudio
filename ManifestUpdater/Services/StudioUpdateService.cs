using Microsoft.Win32;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ManifestUpdater;

internal enum StudioDistributionKind
{
	Portable,
	MsiInstalled
}

internal sealed record StudioUpdateAsset(string Name, string DownloadUrl, long Size, string Sha256Digest);

internal sealed record StudioUpdateRelease(
	Version Version,
	string VersionText,
	string Tag,
	string Title,
	string Notes,
	string ReleaseUrl,
	DateTimeOffset PublishedAt,
	StudioUpdateAsset Asset);

internal sealed record StudioUpdateCheck(
	Version CurrentVersion,
	StudioDistributionKind Distribution,
	StudioUpdateRelease? LatestRelease,
	bool UpdateAvailable);

internal sealed record DownloadedStudioUpdate(string FilePath, string Sha256, long Size);

internal static class StudioUpdateService
{
	internal const string PortableAssetName = "WingetManifestStudio.exe";
	internal const string MsiAssetName = "StudioSetup.msi";
	private const string ReleasesApi = "https://api.github.com/repos/ubidzz/WingetManifestStudio/releases/latest";
	private static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(4);
	private static readonly HttpClient Client = CreateClient();
	private static readonly JsonSerializerOptions CacheOptions = new() { PropertyNameCaseInsensitive = true };

	public static Version CurrentVersion => NormalizeVersion(Assembly.GetEntryAssembly()?.GetName().Version) ?? new Version(0, 0, 0);
	public static string CurrentVersionText => DisplayVersion(CurrentVersion);

	public static StudioDistributionKind DetectDistribution(string? executablePath = null)
	{
		string path = executablePath ?? Environment.ProcessPath ?? string.Empty;
		if (path.Length == 0) return StudioDistributionKind.Portable;

		foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
		{
			try
			{
				using RegistryKey hive = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view);
				using RegistryKey? studio = hive.OpenSubKey(@"Software\ubidzz\Winget Manifest Studio", false);
				string installLocation = Convert.ToString(studio?.GetValue("InstallLocation")) ?? string.Empty;
				if (IsExecutableInInstallLocation(path, installLocation)) return StudioDistributionKind.MsiInstalled;
			}
			catch (Exception exception) when (exception is UnauthorizedAccessException or IOException or System.Security.SecurityException)
			{
				// Registry detection is only a hint. A blocked or missing key means the
				// current copy behaves as a portable application.
			}
		}

		return StudioDistributionKind.Portable;
	}

	internal static bool IsExecutableInInstallLocation(string executablePath, string installLocation)
	{
		if (string.IsNullOrWhiteSpace(executablePath) || string.IsNullOrWhiteSpace(installLocation)) return false;
		try
		{
			string executable = Path.GetFullPath(executablePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			string expected = Path.Combine(Path.GetFullPath(installLocation), PortableAssetName)
				.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			return executable.Equals(expected, StringComparison.OrdinalIgnoreCase);
		}
		catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
		{
			return false;
		}
	}

	public static async Task<StudioUpdateCheck> CheckAsync(bool forceRefresh, CancellationToken cancellationToken = default)
	{
		StudioDistributionKind distribution = DetectDistribution();
		string json = forceRefresh ? string.Empty : ReadCachedReleaseJson();
		if (json.Length == 0)
		{
			json = await DownloadReleaseJsonAsync(cancellationToken);
			WriteCachedReleaseJson(json);
		}

		StudioUpdateRelease? release = ParseReleaseJson(json, distribution);
		Version current = CurrentVersion;
		return new StudioUpdateCheck(current, distribution, release, release is not null && release.Version > current);
	}

	internal static StudioUpdateRelease? ParseReleaseJson(string json, StudioDistributionKind distribution)
	{
		using JsonDocument document = JsonDocument.Parse(json);
		JsonElement root = document.RootElement;
		if (Boolean(root, "draft") || Boolean(root, "prerelease")) return null;

		string tag = String(root, "tag_name");
		string versionTag = tag.Trim().TrimStart('v', 'V');
		if (versionTag.IndexOfAny(['-', '+']) >= 0) return null;
		if (!TryParseVersion(tag, out Version? version))
			throw new InvalidDataException($"The latest GitHub release tag '{tag}' is not a valid stable version.");

		string requiredAsset = distribution == StudioDistributionKind.MsiInstalled ? MsiAssetName : PortableAssetName;
		StudioUpdateAsset? selected = null;
		if (root.TryGetProperty("assets", out JsonElement assets) && assets.ValueKind == JsonValueKind.Array)
		{
			foreach (JsonElement asset in assets.EnumerateArray())
			{
				string name = String(asset, "name");
				if (!name.Equals(requiredAsset, StringComparison.OrdinalIgnoreCase)) continue;
				string downloadUrl = String(asset, "browser_download_url");
				EnsureTrustedAssetUrl(downloadUrl, requiredAsset);
				selected = new StudioUpdateAsset(name, downloadUrl, Int64(asset, "size"), NormalizeDigest(String(asset, "digest")));
				break;
			}
		}

		if (selected is null)
			throw new InvalidDataException($"Release {tag} does not contain the required {requiredAsset} update file.");

		string published = String(root, "published_at");
		DateTimeOffset.TryParse(published, out DateTimeOffset publishedAt);
		Version stableVersion = version!;
		return new StudioUpdateRelease(
			stableVersion,
			DisplayVersion(stableVersion),
			tag,
			String(root, "name").IfEmpty(tag),
			String(root, "body"),
			String(root, "html_url"),
			publishedAt,
			selected);
	}

	internal static bool TryParseVersion(string text, out Version? version)
	{
		string normalized = (text ?? string.Empty).Trim();
		if (normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase)) normalized = normalized[1..];
		int suffix = normalized.IndexOfAny(['-', '+']);
		if (suffix >= 0) normalized = normalized[..suffix];
		if (!Version.TryParse(normalized, out Version? parsed))
		{
			version = null;
			return false;
		}
		version = NormalizeVersion(parsed);
		return version is not null;
	}

	public static async Task<DownloadedStudioUpdate> DownloadAsync(
		StudioUpdateRelease release,
		IProgress<int>? progress = null,
		CancellationToken cancellationToken = default)
	{
		EnsureTrustedAssetUrl(release.Asset.DownloadUrl, release.Asset.Name);
		string versionFolder = SafePathPart(release.Tag.IfEmpty(release.VersionText));
		string updateFolder = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"WingetManifestStudio", "Updates", versionFolder);
		Directory.CreateDirectory(updateFolder);
		string destination = Path.Combine(updateFolder, release.Asset.Name);
		string temporary = destination + ".download";
		if (File.Exists(temporary)) File.Delete(temporary);

		try
		{
			using HttpRequestMessage request = new(HttpMethod.Get, release.Asset.DownloadUrl);
			using HttpResponseMessage response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
			if (response.StatusCode == HttpStatusCode.NotFound)
				throw new InvalidDataException("The update file is missing from the GitHub release.");
			response.EnsureSuccessStatusCode();
			long total = response.Content.Headers.ContentLength ?? release.Asset.Size;
			await using Stream input = await response.Content.ReadAsStreamAsync(cancellationToken);
			await using (FileStream output = new(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024 * 128, FileOptions.Asynchronous | FileOptions.SequentialScan))
			{
				byte[] buffer = new byte[1024 * 128];
				long copied = 0;
				while (true)
				{
					int read = await input.ReadAsync(buffer, cancellationToken);
					if (read == 0) break;
					await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
					copied += read;
					if (total > 0) progress?.Report(Math.Clamp((int)(copied * 100L / total), 0, 100));
				}
			}

			FileInfo file = new(temporary);
			if (file.Length < 64 * 1024)
				throw new InvalidDataException("The downloaded update is too small to be a valid Studio application file.");
			if (release.Asset.Size > 0 && file.Length != release.Asset.Size)
				throw new InvalidDataException($"The downloaded update size is {file.Length:N0} bytes, but GitHub reported {release.Asset.Size:N0} bytes.");
			await ValidateFileHeaderAsync(temporary, release.Asset.Name, cancellationToken);
			string sha256 = await ComputeSha256Async(temporary, cancellationToken);
			if (release.Asset.Sha256Digest.Length > 0 && !sha256.Equals(release.Asset.Sha256Digest, StringComparison.OrdinalIgnoreCase))
				throw new InvalidDataException("The downloaded update failed its GitHub SHA-256 verification. The file was not opened.");
			File.Move(temporary, destination, true);
			progress?.Report(100);
			return new DownloadedStudioUpdate(destination, sha256, file.Length);
		}
		catch
		{
			try { if (File.Exists(temporary)) File.Delete(temporary); } catch { }
			throw;
		}
	}

	public static ProcessStartInfo CreateMsiUpdateLauncher(string downloadedMsi) => new()
	{
		FileName = "msiexec.exe",
		Arguments = $"/i \"{downloadedMsi}\"",
		UseShellExecute = true,
		WorkingDirectory = Path.GetDirectoryName(downloadedMsi) ?? AppContext.BaseDirectory
	};

	public static ProcessStartInfo CreatePortableUpdateLauncher(string downloadedExecutable, string currentExecutable, int processId)
	{
		string current = PowerShellLiteral(Path.GetFullPath(currentExecutable));
		string downloaded = PowerShellLiteral(Path.GetFullPath(downloadedExecutable));
		string script = $$"""
			$ErrorActionPreference = 'Stop'
			$current = '{{current}}'
			$downloaded = '{{downloaded}}'
			$backup = $current + '.previous'
			try {
			    Wait-Process -Id {{processId}} -ErrorAction SilentlyContinue
			    if (Test-Path -LiteralPath $backup) { Remove-Item -LiteralPath $backup -Force }
			    $replaced = $false
			    Move-Item -LiteralPath $current -Destination $backup -Force
			    try { Move-Item -LiteralPath $downloaded -Destination $current -Force }
			    catch { Move-Item -LiteralPath $backup -Destination $current -Force; throw }
			    $replaced = $true
			    $newProcess = Start-Process -FilePath $current -PassThru
			    Start-Sleep -Milliseconds 1500
			    if ($newProcess.HasExited -and $newProcess.ExitCode -ne 0) {
			        throw "The updated application exited with code $($newProcess.ExitCode)."
			    }
			    if (Test-Path -LiteralPath $backup) { Remove-Item -LiteralPath $backup -Force }
			}
			catch {
			    try {
			        if (Test-Path -LiteralPath $backup) {
			            if (Test-Path -LiteralPath $current) { Remove-Item -LiteralPath $current -Force }
			            Move-Item -LiteralPath $backup -Destination $current -Force
			        }
			    } catch { }
			    Add-Type -AssemblyName System.Windows.Forms
			    [System.Windows.Forms.MessageBox]::Show(
			        'Winget Manifest Studio could not replace the portable application file. ' + $_.Exception.Message,
			        'Update could not finish', 'OK', 'Error') | Out-Null
			    if (Test-Path -LiteralPath $current) { Start-Process -FilePath $current }
			    exit 1
			}
			""";
		string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
		ProcessStartInfo start = new()
		{
			FileName = "powershell.exe",
			UseShellExecute = false,
			CreateNoWindow = true,
			WindowStyle = ProcessWindowStyle.Hidden,
			WorkingDirectory = Path.GetDirectoryName(currentExecutable) ?? AppContext.BaseDirectory
		};
		start.ArgumentList.Add("-NoLogo");
		start.ArgumentList.Add("-NoProfile");
		start.ArgumentList.Add("-NonInteractive");
		start.ArgumentList.Add("-WindowStyle");
		start.ArgumentList.Add("Hidden");
		start.ArgumentList.Add("-EncodedCommand");
		start.ArgumentList.Add(encoded);
		return start;
	}

	public static bool CanReplacePortableExecutable(string executablePath, out string reason)
	{
		reason = string.Empty;
		try
		{
			string directory = Path.GetDirectoryName(Path.GetFullPath(executablePath))
				?? throw new InvalidOperationException("The portable application folder could not be determined.");
			string probe = Path.Combine(directory, ".wms-update-write-test-" + Guid.NewGuid().ToString("N"));
			using (File.Create(probe, 1, FileOptions.DeleteOnClose)) { }
			return true;
		}
		catch (Exception exception) when (exception is UnauthorizedAccessException or IOException or System.Security.SecurityException or ArgumentException)
		{
			reason = "The folder containing this portable copy cannot be changed. Move the EXE to a writable folder or install StudioSetup.msi, then try again. " + exception.Message;
			return false;
		}
	}

	private static async Task<string> DownloadReleaseJsonAsync(CancellationToken cancellationToken)
	{
		using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		timeout.CancelAfter(TimeSpan.FromSeconds(20));
		HttpResponseMessage response;
		try { response = await Client.GetAsync(ReleasesApi, timeout.Token); }
		catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
		{
			throw new InvalidOperationException("The Studio could not reach GitHub within 20 seconds. Check your connection and try again.");
		}
		using (response)
		{
		if (response.StatusCode == HttpStatusCode.NotFound)
			throw new InvalidDataException("No published Winget Manifest Studio release was found on GitHub.");
		if ((int)response.StatusCode == 403)
			throw new InvalidOperationException("GitHub temporarily limited public update checks. The Studio will try again later; no token is required.");
		response.EnsureSuccessStatusCode();
		return await response.Content.ReadAsStringAsync(timeout.Token);
		}
	}

	private static string ReadCachedReleaseJson()
	{
		try
		{
			if (!File.Exists(CachePath)) return string.Empty;
			StudioUpdateCache? cache = JsonSerializer.Deserialize<StudioUpdateCache>(File.ReadAllText(CachePath), CacheOptions);
			return cache is not null && DateTimeOffset.UtcNow - cache.CheckedAt <= CacheLifetime ? cache.ReleaseJson : string.Empty;
		}
		catch { return string.Empty; }
	}

	private static void WriteCachedReleaseJson(string json)
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(CachePath)!);
			string temporary = CachePath + ".tmp";
			File.WriteAllText(temporary, JsonSerializer.Serialize(new StudioUpdateCache(DateTimeOffset.UtcNow, json), CacheOptions));
			File.Move(temporary, CachePath, true);
		}
		catch { }
	}

	private static async Task ValidateFileHeaderAsync(string path, string assetName, CancellationToken cancellationToken)
	{
		byte[] header = new byte[8];
		await using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
		int read = await stream.ReadAsync(header, cancellationToken);
		bool valid = assetName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
			? read >= 2 && header[0] == (byte)'M' && header[1] == (byte)'Z'
			: read == 8 && header.SequenceEqual(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 });
		if (!valid) throw new InvalidDataException($"The downloaded {assetName} file does not have a valid Windows application header.");
	}

	private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
	{
		await using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 128, FileOptions.Asynchronous | FileOptions.SequentialScan);
		byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken);
		return Convert.ToHexString(hash);
	}

	private static void EnsureTrustedAssetUrl(string url, string expectedName)
	{
		if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
			|| uri.Scheme != Uri.UriSchemeHttps
			|| !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
			|| !uri.AbsolutePath.StartsWith("/ubidzz/WingetManifestStudio/releases/download/", StringComparison.OrdinalIgnoreCase)
			|| !Uri.UnescapeDataString(uri.AbsolutePath).EndsWith('/' + expectedName, StringComparison.OrdinalIgnoreCase))
			throw new InvalidDataException($"The {expectedName} update link is not an official Winget Manifest Studio GitHub release asset.");
	}

	private static string NormalizeDigest(string digest)
	{
		string value = (digest ?? string.Empty).Trim();
		if (value.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)) value = value[7..];
		return value.Length == 64 && value.All(Uri.IsHexDigit) ? value.ToUpperInvariant() : string.Empty;
	}

	private static Version? NormalizeVersion(Version? version)
	{
		if (version is null || version.Major < 0 || version.Minor < 0) return null;
		return new Version(version.Major, version.Minor, Math.Max(0, version.Build), Math.Max(0, version.Revision));
	}

	private static string DisplayVersion(Version version)
	{
		List<int> parts = [version.Major, version.Minor, Math.Max(0, version.Build)];
		if (version.Revision > 0) parts.Add(version.Revision);
		return string.Join('.', parts);
	}

	private static string PowerShellLiteral(string value) => value.Replace("'", "''", StringComparison.Ordinal);
	private static string SafePathPart(string value) => string.Concat(value.Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '-' : character));
	private static string CachePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WingetManifestStudio", "update-check.json");
	private static string String(JsonElement node, string name) => node.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;
	private static long Int64(JsonElement node, string name) => node.TryGetProperty(name, out JsonElement value) && value.TryGetInt64(out long result) ? result : 0;
	private static bool Boolean(JsonElement node, string name) => node.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.True;

	private static HttpClient CreateClient()
	{
		HttpClient client = new() { Timeout = TimeSpan.FromMinutes(10) };
		client.DefaultRequestHeaders.UserAgent.ParseAdd("WingetManifestStudio-Updater/1.0");
		client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
		client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
		return client;
	}

	private sealed record StudioUpdateCache(DateTimeOffset CheckedAt, string ReleaseJson);
}
