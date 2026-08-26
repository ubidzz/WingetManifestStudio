using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace ManifestUpdater;

internal static class InstallerInspector
{
	public static async Task<InstallerInspection> InspectAsync(
		string localFile,
		string installerUrl,
		IProgress<string>? progress = null,
		CancellationToken cancellationToken = default)
	{
		string inspectionPath = localFile;
		string? temporaryPath = null;
		try
		{
			if (!File.Exists(inspectionPath))
			{
				if (!Uri.TryCreate(installerUrl, UriKind.Absolute, out Uri? uri))
					throw new FileNotFoundException("Choose a local installer file or enter a downloadable installer URL.");
				temporaryPath = Path.Combine(Path.GetTempPath(), $"ManifestUpdater-{Guid.NewGuid():N}{Path.GetExtension(uri.AbsolutePath)}");
				progress?.Report($"Downloading {Path.GetFileName(uri.AbsolutePath)} for inspection...");
				using HttpClient client = new() { Timeout = TimeSpan.FromMinutes(20) };
				using HttpResponseMessage response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
				response.EnsureSuccessStatusCode();
				await using Stream source = await response.Content.ReadAsStreamAsync(cancellationToken);
				await using FileStream destination = new(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 131072, true);
				await source.CopyToAsync(destination, cancellationToken);
				inspectionPath = temporaryPath;
			}

			progress?.Report($"Calculating SHA-256 for {Path.GetFileName(inspectionPath)}...");
			string sha256 = await CalculateSha256Async(inspectionPath, cancellationToken);
			string extension = Path.GetExtension(inspectionPath).ToLowerInvariant();
			string architecture = InferArchitecture(inspectionPath);
			string installerType = InferInstallerType(inspectionPath, extension);
			string productCode = string.Empty;
			string upgradeCode = string.Empty;
			string version = string.Empty;
			string displayName = string.Empty;
			string publisher = string.Empty;

			if (extension == ".msi")
			{
				IReadOnlyDictionary<string, string> properties = ReadMsiProperties(inspectionPath);
				properties.TryGetValue("ProductCode", out productCode!);
				properties.TryGetValue("UpgradeCode", out upgradeCode!);
				properties.TryGetValue("ProductVersion", out version!);
				properties.TryGetValue("ProductName", out displayName!);
				properties.TryGetValue("Manufacturer", out publisher!);
			}
			else if (extension is ".msix" or ".appx" or ".msixbundle" or ".appxbundle")
			{
				ReadAppPackageMetadata(inspectionPath, ref architecture, ref version, ref displayName, ref publisher);
			}
			else if (extension == ".exe")
			{
				FileVersionInfo info = FileVersionInfo.GetVersionInfo(inspectionPath);
				version = NormalizeVersion(info.ProductVersion ?? info.FileVersion ?? string.Empty);
				displayName = info.ProductName ?? info.FileDescription ?? string.Empty;
				publisher = info.CompanyName ?? string.Empty;
			}

			return new InstallerInspection(
				sha256,
				architecture,
				installerType,
				productCode ?? string.Empty,
				upgradeCode ?? string.Empty,
				version ?? string.Empty,
				displayName ?? string.Empty,
				publisher ?? string.Empty,
				new FileInfo(inspectionPath).Length);
		}
		finally
		{
			if (!string.IsNullOrWhiteSpace(temporaryPath) && File.Exists(temporaryPath))
			{
				try { File.Delete(temporaryPath); } catch { }
			}
		}
	}

	private static async Task<string> CalculateSha256Async(string path, CancellationToken cancellationToken)
	{
		await using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 131072, true);
		byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken);
		return Convert.ToHexString(hash);
	}

	private static string InferInstallerType(string path, string extension)
	{
		return extension switch
		{
			".msi" => "msi",
			".msix" => "msix",
			".msixbundle" => "msix",
			".appx" => "appx",
			".appxbundle" => "appx",
			".zip" => "zip",
			".exe" when ContainsMarker(path, "Inno Setup") => "inno",
			".exe" when ContainsMarker(path, "Nullsoft") => "nullsoft",
			".exe" => "exe",
			_ => "portable"
		};
	}

	private static bool ContainsMarker(string path, string marker)
	{
		const int sampleSize = 2 * 1024 * 1024;
		using FileStream stream = File.OpenRead(path);
		int length = (int)Math.Min(sampleSize, stream.Length);
		byte[] bytes = new byte[length];
		stream.ReadExactly(bytes);
		string ascii = Encoding.ASCII.GetString(bytes);
		if (ascii.Contains(marker, StringComparison.OrdinalIgnoreCase))
			return true;
		string unicode = Encoding.Unicode.GetString(bytes);
		return unicode.Contains(marker, StringComparison.OrdinalIgnoreCase);
	}

	private static string InferArchitecture(string path)
	{
		string name = Path.GetFileName(path);
		if (name.Contains("arm64", StringComparison.OrdinalIgnoreCase)) return "arm64";
		if (name.Contains("x86", StringComparison.OrdinalIgnoreCase) || name.Contains("win32", StringComparison.OrdinalIgnoreCase)) return "x86";
		if (name.Contains("x64", StringComparison.OrdinalIgnoreCase) || name.Contains("amd64", StringComparison.OrdinalIgnoreCase)) return "x64";

		try
		{
			using BinaryReader reader = new(File.OpenRead(path));
			if (reader.ReadUInt16() != 0x5A4D)
				return "x64";
			reader.BaseStream.Seek(0x3C, SeekOrigin.Begin);
			int peOffset = reader.ReadInt32();
			reader.BaseStream.Seek(peOffset, SeekOrigin.Begin);
			if (reader.ReadUInt32() != 0x00004550)
				return "x64";
			return reader.ReadUInt16() switch
			{
				0x014c => "x86",
				0x8664 => "x64",
				0xAA64 => "arm64",
				_ => "x64"
			};
		}
		catch
		{
			return "x64";
		}
	}

	private static string NormalizeVersion(string value)
	{
		int separator = value.IndexOfAny([' ', '(', '+']);
		return (separator > 0 ? value[..separator] : value).Trim();
	}

	private static void ReadAppPackageMetadata(
		string path,
		ref string architecture,
		ref string version,
		ref string displayName,
		ref string publisher)
	{
		using ZipArchive archive = ZipFile.OpenRead(path);
		ZipArchiveEntry? entry = archive.Entries.FirstOrDefault(candidate =>
			candidate.FullName.EndsWith("AppxManifest.xml", StringComparison.OrdinalIgnoreCase) ||
			candidate.FullName.EndsWith("AppxBundleManifest.xml", StringComparison.OrdinalIgnoreCase));
		if (entry is null)
			return;
		using Stream stream = entry.Open();
		XDocument document = XDocument.Load(stream);
		XElement? identity = document.Descendants().FirstOrDefault(element => element.Name.LocalName == "Identity");
		XElement? properties = document.Descendants().FirstOrDefault(element => element.Name.LocalName == "Properties");
		version = identity?.Attribute("Version")?.Value ?? version;
		architecture = identity?.Attribute("ProcessorArchitecture")?.Value ?? architecture;
		publisher = identity?.Attribute("Publisher")?.Value ?? publisher;
		displayName = properties?.Elements().FirstOrDefault(element => element.Name.LocalName == "DisplayName")?.Value ?? displayName;
	}

	private static IReadOnlyDictionary<string, string> ReadMsiProperties(string path)
	{
		const uint success = 0;
		const uint noMoreItems = 259;
		uint status = MsiOpenDatabase(path, IntPtr.Zero, out IntPtr database);
		if (status != success)
			throw new InvalidDataException($"Windows Installer could not open the MSI (error {status}).");

		IntPtr view = IntPtr.Zero;
		try
		{
			status = MsiDatabaseOpenView(database, "SELECT `Property`, `Value` FROM `Property`", out view);
			if (status != success)
				throw new InvalidDataException($"The MSI Property table could not be opened (error {status}).");
			status = MsiViewExecute(view, IntPtr.Zero);
			if (status != success)
				throw new InvalidDataException($"The MSI Property table could not be read (error {status}).");

			Dictionary<string, string> properties = new(StringComparer.OrdinalIgnoreCase);
			while (true)
			{
				status = MsiViewFetch(view, out IntPtr record);
				if (status == noMoreItems) break;
				if (status != success)
					throw new InvalidDataException($"The MSI Property table is incomplete (error {status}).");
				try
				{
					string name = ReadMsiRecordString(record, 1);
					if (name.Length > 0)
						properties[name] = ReadMsiRecordString(record, 2);
				}
				finally
				{
					MsiCloseHandle(record);
				}
			}
			return properties;
		}
		finally
		{
			if (view != IntPtr.Zero) MsiCloseHandle(view);
			MsiCloseHandle(database);
		}
	}

	private static string ReadMsiRecordString(IntPtr record, uint field)
	{
		const uint success = 0;
		const uint moreData = 234;
		uint count = 0;
		uint status = MsiRecordGetString(record, field, null, ref count);
		if (status is not (success or moreData))
			throw new InvalidDataException($"An MSI value could not be read (error {status}).");
		StringBuilder value = new(checked((int)count + 1));
		uint capacity = (uint)value.Capacity;
		status = MsiRecordGetString(record, field, value, ref capacity);
		if (status != success)
			throw new InvalidDataException($"An MSI value could not be read (error {status}).");
		return value.ToString();
	}

	[DllImport("msi.dll", EntryPoint = "MsiOpenDatabaseW", CharSet = CharSet.Unicode)]
	private static extern uint MsiOpenDatabase(string databasePath, IntPtr persist, out IntPtr database);

	[DllImport("msi.dll", EntryPoint = "MsiDatabaseOpenViewW", CharSet = CharSet.Unicode)]
	private static extern uint MsiDatabaseOpenView(IntPtr database, string query, out IntPtr view);

	[DllImport("msi.dll")]
	private static extern uint MsiViewExecute(IntPtr view, IntPtr record);

	[DllImport("msi.dll")]
	private static extern uint MsiViewFetch(IntPtr view, out IntPtr record);

	[DllImport("msi.dll", EntryPoint = "MsiRecordGetStringW", CharSet = CharSet.Unicode)]
	private static extern uint MsiRecordGetString(IntPtr record, uint field, StringBuilder? value, ref uint characterCount);

	[DllImport("msi.dll")]
	private static extern uint MsiCloseHandle(IntPtr handle);
}
