using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ManifestUpdater;

internal static class AuthenticodeInspector
{
	private static readonly Guid GenericVerifyV2 = new("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");
	private const uint UiNone = 2;
	private const uint UnionFile = 1;
	private const uint RevocationNone = 0;
	private const uint StateActionIgnore = 0;
	private const uint CacheOnlyUrlRetrieval = 0x00001000;
	private const uint RevocationCheckNone = 0x00000010;

	public static AuthenticodeInspection Inspect(string path)
	{
		if (!OperatingSystem.IsWindows())
			return new AuthenticodeInspection("Unavailable", false, false, string.Empty, string.Empty, null, null, "Authenticode inspection is available on Windows.");
		if (!File.Exists(path))
			return new AuthenticodeInspection("Missing", false, false, string.Empty, string.Empty, null, null, "The local installer file does not exist.");

		X509Certificate2? certificate = null;
		try
		{
			#pragma warning disable SYSLIB0057 // Required Windows API for extracting an Authenticode signer from a PE/MSI file.
			using X509Certificate signedFileCertificate = X509Certificate.CreateFromSignedFile(path);
			#pragma warning restore SYSLIB0057
			certificate = new X509Certificate2(signedFileCertificate);
		}
		catch (CryptographicException)
		{
			return new AuthenticodeInspection("Unsigned", false, false, string.Empty, string.Empty, null, null, "No Authenticode signature was found.");
		}

		uint trustResult = VerifyTrust(path);
		bool trusted = trustResult == 0;
		DateTimeOffset notBefore = certificate.NotBefore;
		DateTimeOffset notAfter = certificate.NotAfter;
		bool expired = DateTimeOffset.Now < notBefore || DateTimeOffset.Now > notAfter;
		string status = trusted && !expired ? "Signed and trusted" : expired ? "Signed • certificate outside its validity period" : "Signed • trust check failed";
		string message = trusted
			? "Windows verified the file's Authenticode signature and certificate chain."
			: $"Windows trust verification returned 0x{trustResult:X8} ({new Win32Exception(unchecked((int)trustResult)).Message}).";
		return new AuthenticodeInspection(
			status,
			true,
			trusted && !expired,
			certificate.GetNameInfo(X509NameType.SimpleName, false).IfEmpty(certificate.Subject),
			certificate.Thumbprint ?? string.Empty,
			notBefore,
			notAfter,
			message);
	}

	private static uint VerifyTrust(string path)
	{
		WinTrustFileInfo fileInfo = new()
		{
			StructSize = (uint)Marshal.SizeOf<WinTrustFileInfo>(),
			FilePath = path,
			FileHandle = IntPtr.Zero,
			KnownSubject = IntPtr.Zero
		};
		IntPtr fileInfoPointer = Marshal.AllocHGlobal(Marshal.SizeOf<WinTrustFileInfo>());
		try
		{
			Marshal.StructureToPtr(fileInfo, fileInfoPointer, false);
			WinTrustData data = new()
			{
				StructSize = (uint)Marshal.SizeOf<WinTrustData>(),
				PolicyCallbackData = IntPtr.Zero,
				SipClientData = IntPtr.Zero,
				UiChoice = UiNone,
				RevocationChecks = RevocationNone,
				UnionChoice = UnionFile,
				FileInformation = fileInfoPointer,
				StateAction = StateActionIgnore,
				StateData = IntPtr.Zero,
				UrlReference = IntPtr.Zero,
				ProviderFlags = CacheOnlyUrlRetrieval | RevocationCheckNone,
				UiContext = 0
			};
			return WinVerifyTrust(new IntPtr(-1), GenericVerifyV2, ref data);
		}
		finally
		{
			Marshal.DestroyStructure<WinTrustFileInfo>(fileInfoPointer);
			Marshal.FreeHGlobal(fileInfoPointer);
		}
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct WinTrustFileInfo
	{
		public uint StructSize;
		[MarshalAs(UnmanagedType.LPWStr)] public string FilePath;
		public IntPtr FileHandle;
		public IntPtr KnownSubject;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct WinTrustData
	{
		public uint StructSize;
		public IntPtr PolicyCallbackData;
		public IntPtr SipClientData;
		public uint UiChoice;
		public uint RevocationChecks;
		public uint UnionChoice;
		public IntPtr FileInformation;
		public uint StateAction;
		public IntPtr StateData;
		public IntPtr UrlReference;
		public uint ProviderFlags;
		public uint UiContext;
	}

	[DllImport("wintrust.dll", ExactSpelling = true, PreserveSig = true)]
	private static extern uint WinVerifyTrust(
		IntPtr windowHandle,
		[MarshalAs(UnmanagedType.LPStruct)] Guid actionId,
		ref WinTrustData trustData);
}
