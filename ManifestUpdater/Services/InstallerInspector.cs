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
				progress?.Report($"Downloading {Path.GetFileName(uri.AbsolutePath)} for inspection...");
				using HttpClient client = new() { Timeout = TimeSpan.FromMinutes(20) };
				using HttpResponseMessage response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
				response.EnsureSuccessStatusCode();
				string responseName = response.Content.Headers.ContentDisposition?.FileNameStar
					?? response.Content.Headers.ContentDisposition?.FileName
					?? uri.AbsolutePath;
				string responseExtension = Path.GetExtension(responseName.Trim().Trim('"'));
				if (responseExtension.Length > 16 || responseExtension.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
					responseExtension = string.Empty;
				temporaryPath = Path.Combine(Path.GetTempPath(), $"ManifestUpdater-{Guid.NewGuid():N}{responseExtension}");
				await using Stream source = await response.Content.ReadAsStreamAsync(cancellationToken);
				await using FileStream destination = new(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 131072, true);
				await source.CopyToAsync(destination, cancellationToken);
				inspectionPath = temporaryPath;
			}

			progress?.Report($"Calculating SHA-256 for {Path.GetFileName(inspectionPath)}...");
			string sha256 = await CalculateSha256Async(inspectionPath, cancellationToken);
			string extension = Path.GetExtension(inspectionPath).ToLowerInvariant();
			string architecture = InferArchitecture(inspectionPath, extension);
			string installerType = InferInstallerType(inspectionPath, extension);
			string scope = string.Empty;
			string nestedInstallerType = string.Empty;
			string nestedInstallerFiles = string.Empty;
			string productCode = string.Empty;
			string upgradeCode = string.Empty;
			string version = string.Empty;
			string displayName = string.Empty;
			string publisher = string.Empty;
			string signatureSha256 = string.Empty;

			if (installerType == "msi")
			{
				IReadOnlyDictionary<string, string> properties = ReadMsiProperties(inspectionPath);
				architecture = InferMsiArchitecture(ReadMsiTemplate(inspectionPath), architecture);
				scope = InferMsiScope(properties);
				properties.TryGetValue("ProductCode", out productCode!);
				properties.TryGetValue("UpgradeCode", out upgradeCode!);
				properties.TryGetValue("ProductVersion", out version!);
				properties.TryGetValue("ProductName", out displayName!);
				properties.TryGetValue("Manufacturer", out publisher!);
			}
			else if (extension is ".msix" or ".appx" or ".msixbundle" or ".appxbundle")
			{
				ReadAppPackageMetadata(inspectionPath, ref architecture, ref version, ref displayName, ref publisher);
				signatureSha256 = await CalculateAppPackageSignatureSha256Async(inspectionPath, cancellationToken);
			}
			else if (installerType is "exe" or "inno" or "nullsoft" or "burn")
			{
				FileVersionInfo info = FileVersionInfo.GetVersionInfo(inspectionPath);
				version = NormalizeVersion(info.ProductVersion ?? info.FileVersion ?? string.Empty);
				displayName = info.ProductName ?? info.FileDescription ?? string.Empty;
				publisher = info.CompanyName ?? string.Empty;
			}
			else if (extension == ".zip")
			{
				(nestedInstallerType, nestedInstallerFiles) = InspectArchive(inspectionPath);
			}

			progress?.Report($"Checking the digital signature on {Path.GetFileName(inspectionPath)}...");
			AuthenticodeInspection signature = AuthenticodeInspector.Inspect(inspectionPath);
			return new InstallerInspection(
				sha256,
				architecture,
				installerType,
				scope,
				nestedInstallerType,
				nestedInstallerFiles,
				productCode ?? string.Empty,
				upgradeCode ?? string.Empty,
				version ?? string.Empty,
				displayName ?? string.Empty,
				publisher ?? string.Empty,
				new FileInfo(inspectionPath).Length,
				signature,
				signatureSha256);
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

	private static async Task<string> CalculateAppPackageSignatureSha256Async(string path, CancellationToken cancellationToken)
	{
		using ZipArchive archive = ZipFile.OpenRead(path);
		ZipArchiveEntry? signature = archive.Entries.FirstOrDefault(entry =>
			entry.FullName.EndsWith("AppxSignature.p7x", StringComparison.OrdinalIgnoreCase));
		if (signature is null) return string.Empty;
		await using Stream stream = signature.Open();
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
			".otf" or ".otc" or ".ttf" or ".ttc" or ".fnt" => "font",
			".exe" => InferExecutableInstallerType(path),
			_ when LooksLikeMsi(path) => "msi",
			_ when IsPortableExecutable(path) => InferExecutableInstallerType(path),
			_ => "portable"
		};
	}

	private static string InferExecutableInstallerType(string path)
	{
		if (ContainsMarker(path, "Inno Setup")) return "inno";
		if (ContainsMarker(path, "Nullsoft")) return "nullsoft";
		if (ContainsMarker(path, "WixBundle") || ContainsMarker(path, "WiX Toolset Burn")) return "burn";
		return "exe";
	}

	private static bool LooksLikeMsi(string path)
	{
		ReadOnlySpan<byte> compoundFileHeader = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];
		try
		{
			Span<byte> header = stackalloc byte[8];
			using FileStream stream = File.OpenRead(path);
			return stream.Read(header) == header.Length && header.SequenceEqual(compoundFileHeader);
		}
		catch { return false; }
	}

	private static bool IsPortableExecutable(string path)
	{
		try
		{
			using BinaryReader reader = new(File.OpenRead(path));
			return reader.BaseStream.Length >= 2 && reader.ReadUInt16() == 0x5A4D;
		}
		catch { return false; }
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

	private static string InferArchitecture(string path, string extension)
	{
		string name = Path.GetFileName(path);
		if (name.Contains("arm64", StringComparison.OrdinalIgnoreCase)) return "arm64";
		if (name.Contains("x86", StringComparison.OrdinalIgnoreCase) || name.Contains("win32", StringComparison.OrdinalIgnoreCase)) return "x86";
		if (name.Contains("x64", StringComparison.OrdinalIgnoreCase) || name.Contains("amd64", StringComparison.OrdinalIgnoreCase)) return "x64";

		if (extension is ".zip" or ".otf" or ".otc" or ".ttf" or ".ttc" or ".fnt") return "neutral";

		try
		{
			using BinaryReader reader = new(File.OpenRead(path));
			if (reader.ReadUInt16() != 0x5A4D)
				return "neutral";
			reader.BaseStream.Seek(0x3C, SeekOrigin.Begin);
			int peOffset = reader.ReadInt32();
			reader.BaseStream.Seek(peOffset, SeekOrigin.Begin);
			if (reader.ReadUInt32() != 0x00004550)
				return "neutral";
			return reader.ReadUInt16() switch
			{
				0x014c => "x86",
				0x8664 => "x64",
				0x01c0 or 0x01c2 or 0x01c4 => "arm",
				0xAA64 => "arm64",
				_ => "neutral"
			};
		}
		catch
		{
			return "neutral";
		}
	}

	private static string InferMsiArchitecture(string template, string fallback)
	{
		string platform = template.Split(';', StringSplitOptions.TrimEntries)[0];
		if (platform.Equals("x64", StringComparison.OrdinalIgnoreCase) || platform.Equals("Intel64", StringComparison.OrdinalIgnoreCase)) return "x64";
		if (platform.Equals("Arm64", StringComparison.OrdinalIgnoreCase)) return "arm64";
		if (platform.Equals("Arm", StringComparison.OrdinalIgnoreCase)) return "arm";
		if (platform.Equals("Intel", StringComparison.OrdinalIgnoreCase)) return "x86";
		return fallback;
	}

	private static string InferMsiScope(IReadOnlyDictionary<string, string> properties)
	{
		properties.TryGetValue("ALLUSERS", out string? allUsers);
		properties.TryGetValue("MSIINSTALLPERUSER", out string? perUser);
		if (string.Equals(perUser, "1", StringComparison.OrdinalIgnoreCase)) return "user";
		if (string.Equals(allUsers, "1", StringComparison.OrdinalIgnoreCase)) return "machine";
		if (string.IsNullOrWhiteSpace(allUsers)) return "user";
		return string.Empty;
	}

	private static (string NestedInstallerType, string NestedInstallerFiles) InspectArchive(string path)
	{
		using ZipArchive archive = ZipFile.OpenRead(path);
		string[] supported = [".msi", ".exe", ".msix", ".msixbundle", ".appx", ".appxbundle", ".otf", ".otc", ".ttf", ".ttc", ".fnt"];
		List<ZipArchiveEntry> candidates = archive.Entries
			.Where(entry => entry.Length > 0 && supported.Contains(Path.GetExtension(entry.FullName), StringComparer.OrdinalIgnoreCase))
			.ToList();
		if (candidates.Count == 0) return (string.Empty, string.Empty);
		string files = string.Join("; ", candidates.Select(entry => entry.FullName.Replace('/', '\\')));
		if (candidates.Count != 1) return (string.Empty, files);
		string extension = Path.GetExtension(candidates[0].FullName).ToLowerInvariant();
		string nestedType = extension switch
		{
			".msi" => "msi",
			".msix" or ".msixbundle" => "msix",
			".appx" or ".appxbundle" => "appx",
			".otf" or ".otc" or ".ttf" or ".ttc" or ".fnt" => "font",
			_ => "exe"
		};
		return (nestedType, files);
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

	private static string ReadMsiTemplate(string path)
	{
		const uint success = 0;
		const uint moreData = 234;
		const uint templateProperty = 7;
		uint status = MsiOpenDatabase(path, IntPtr.Zero, out IntPtr database);
		if (status != success) return string.Empty;
		IntPtr summary = IntPtr.Zero;
		try
		{
			status = MsiGetSummaryInformation(database, null, 0, out summary);
			if (status != success) return string.Empty;
			uint dataType;
			int integerValue;
			System.Runtime.InteropServices.ComTypes.FILETIME fileTime;
			uint length = 0;
			status = MsiSummaryInfoGetProperty(summary, templateProperty, out dataType, out integerValue, out fileTime, null, ref length);
			if (status is not (success or moreData)) return string.Empty;
			StringBuilder value = new(checked((int)length + 1));
			uint capacity = (uint)value.Capacity;
			status = MsiSummaryInfoGetProperty(summary, templateProperty, out dataType, out integerValue, out fileTime, value, ref capacity);
			return status == success ? value.ToString() : string.Empty;
		}
		finally
		{
			if (summary != IntPtr.Zero) MsiCloseHandle(summary);
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

	[DllImport("msi.dll", EntryPoint = "MsiGetSummaryInformationW", CharSet = CharSet.Unicode)]
	private static extern uint MsiGetSummaryInformation(IntPtr database, string? databasePath, uint updateCount, out IntPtr summaryInfo);

	[DllImport("msi.dll", EntryPoint = "MsiSummaryInfoGetPropertyW", CharSet = CharSet.Unicode)]
	private static extern uint MsiSummaryInfoGetProperty(
		IntPtr summaryInfo,
		uint property,
		out uint dataType,
		out int integerValue,
		out System.Runtime.InteropServices.ComTypes.FILETIME fileTimeValue,
		StringBuilder? value,
		ref uint valueLength);
}
