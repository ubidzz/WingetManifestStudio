using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace ManifestUpdater;

internal static class WingetCommandService
{
	private const string GitHubCredentialTarget = "winget-create:GitHub [repo]";
	private const uint GenericCredentialType = 1;

	private static readonly HashSet<string> InteractiveCommands = new(StringComparer.OrdinalIgnoreCase)
	{
		"new",
		"new-locale",
		"update-locale",
		"submit",
		"token"
	};

	[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CredReadW")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool CredRead(
		string target,
		uint type,
		int flags,
		out IntPtr credential);

	[DllImport("advapi32.dll", SetLastError = true)]
	private static extern void CredFree(IntPtr credential);

	public static bool IsGitHubTokenStored()
	{
		if (!OperatingSystem.IsWindows()) return false;

		IntPtr credential = IntPtr.Zero;
		try
		{
			return CredRead(GitHubCredentialTarget, GenericCredentialType, 0, out credential);
		}
		finally
		{
			if (credential != IntPtr.Zero) CredFree(credential);
		}
	}

	public static bool RequiresInteractiveConsole(string command, string arguments)
	{
		if (InteractiveCommands.Contains(command)) return true;
		return string.Equals(command, "update", StringComparison.OrdinalIgnoreCase)
			&& Tokenize(arguments).Any(argument => string.Equals(argument, "--interactive", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(argument, "-i", StringComparison.OrdinalIgnoreCase));
	}

	public static int StartWingetCreateInteractive(string command, string arguments, string workingDirectory)
		=> StartWingetCreateInteractiveSession(command, arguments, workingDirectory).ProcessId;

	public static InteractiveCommandSession StartWingetCreateInteractiveSession(string command, string arguments, string workingDirectory)
	{
		string logFolder = Path.Combine(Path.GetTempPath(), "WingetManifestStudio", "command-logs");
		Directory.CreateDirectory(logFolder);
		string logPath = Path.Combine(logFolder, $"wingetcreate-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.log");
		ProcessStartInfo startInfo = CreateInteractiveProcessStartInfo(command, arguments, workingDirectory);
		startInfo.Environment["WMS_WINGETCREATE_LOG"] = logPath;
		using Process process = Process.Start(startInfo)
			?? throw new InvalidOperationException("Windows could not start WingetCreate in an interactive console.");
		return new InteractiveCommandSession(process.Id, logPath);
	}

	internal static ProcessStartInfo CreateInteractiveProcessStartInfo(string command, string arguments, string workingDirectory)
	{
		List<string> wingetCreateArguments = [command];
		wingetCreateArguments.AddRange(Tokenize(arguments));

		const string consoleScript = """
			$ErrorActionPreference = 'Continue'
			$wingetArguments = @((ConvertFrom-Json -InputObject $env:WMS_WINGETCREATE_ARGUMENTS))
			$host.UI.RawUI.WindowTitle = 'Winget Manifest Studio - WingetCreate'
			Write-Host ('> wingetcreate ' + ($wingetArguments -join ' ')) -ForegroundColor Cyan
			Write-Host ''
			& wingetcreate @wingetArguments 2>&1 | Tee-Object -FilePath $env:WMS_WINGETCREATE_LOG
			$wingetExitCode = $LASTEXITCODE
			Write-Host ''
			if ($wingetExitCode -eq 0) {
				Write-Host 'WingetCreate completed successfully.' -ForegroundColor Green
			} else {
				Write-Host ('WingetCreate exited with code ' + $wingetExitCode + '.') -ForegroundColor Red
			}
			Write-Host 'This window is staying open so you can read the result.' -ForegroundColor Yellow
			[void](Read-Host 'Press Enter to close')
			exit $wingetExitCode
			""";
		string encodedScript = Convert.ToBase64String(Encoding.Unicode.GetBytes(consoleScript));
		ProcessStartInfo startInfo = new()
		{
			FileName = "powershell.exe",
			WorkingDirectory = Directory.Exists(workingDirectory) ? workingDirectory : Environment.CurrentDirectory,
			UseShellExecute = false,
			CreateNoWindow = false,
			WindowStyle = ProcessWindowStyle.Normal
		};
		startInfo.ArgumentList.Add("-NoLogo");
		startInfo.ArgumentList.Add("-NoProfile");
		startInfo.ArgumentList.Add("-ExecutionPolicy");
		startInfo.ArgumentList.Add("Bypass");
		startInfo.ArgumentList.Add("-EncodedCommand");
		startInfo.ArgumentList.Add(encodedScript);
		startInfo.Environment["WMS_WINGETCREATE_ARGUMENTS"] = JsonSerializer.Serialize(wingetCreateArguments);
		return startInfo;
	}

	public static async Task<CommandResult> RunWingetCreateAsync(
		string command,
		string arguments,
		string workingDirectory,
		CancellationToken cancellationToken = default)
	{
		List<string> tokens = [command];
		tokens.AddRange(Tokenize(arguments));
		return await RunAsync("wingetcreate", tokens, workingDirectory, cancellationToken);
	}

	public static Task<CommandResult> ValidateManifestAsync(string manifestFolder, CancellationToken cancellationToken = default)
	{
		return RunAsync("winget", ["validate", "--manifest", manifestFolder], manifestFolder, cancellationToken);
	}

	public static Task<CommandResult> SearchExactPackageAsync(string packageIdentifier, CancellationToken cancellationToken = default)
	{
		return RunAsync("winget.exe", ["search", "--id", packageIdentifier, "--exact", "--source", "winget", "--accept-source-agreements"], Environment.CurrentDirectory, cancellationToken);
	}

	public static Task<CommandResult> ListInstalledPackageAsync(string packageIdentifier, CancellationToken cancellationToken = default)
	{
		return RunAsync("winget.exe", ["list", "--id", packageIdentifier, "--exact", "--accept-source-agreements"], Environment.CurrentDirectory, cancellationToken);
	}

	public static InteractiveCommandSession StartManifestInstallSession(string manifestFolder)
	{
		return StartPersistentConsoleSession(
			"winget.exe",
			["install", "--manifest", manifestFolder, "--accept-package-agreements", "--accept-source-agreements", "--verbose-logs"],
			manifestFolder,
			"Winget Manifest Studio - Local Install Test",
			"local-install");
	}

	public static InteractiveCommandSession StartSandboxTestSession(string scriptPath, string manifestFolder)
	{
		string mapFolder = Directory.GetParent(manifestFolder)?.FullName ?? manifestFolder;
		return StartPersistentConsoleSession(
			"powershell.exe",
			["-NoLogo", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", scriptPath, "-Manifest", manifestFolder, "-MapFolder", mapFolder, "-WarningAction", "Continue"],
			mapFolder,
			"Winget Manifest Studio - Official Windows Sandbox Test",
			"sandbox-test");
	}

	public static int StartEnableLocalManifestFilesElevated()
	{
		const string script = """
			$host.UI.RawUI.WindowTitle = 'Winget Manifest Studio - Enable Local Manifest Testing'
			Write-Host 'Enabling Winget local manifest testing for this computer...' -ForegroundColor Cyan
			& winget.exe settings --enable LocalManifestFiles
			$code = $LASTEXITCODE
			Write-Host ''
			if ($code -eq 0) { Write-Host 'Local manifest testing is enabled.' -ForegroundColor Green }
			else { Write-Host ('Winget returned exit code ' + $code + '.') -ForegroundColor Red }
			[void](Read-Host 'Press Enter to close')
			exit $code
			""";
		string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
		ProcessStartInfo startInfo = new()
		{
			FileName = "powershell.exe",
			UseShellExecute = true,
			Verb = "runas",
			WindowStyle = ProcessWindowStyle.Normal
		};
		startInfo.ArgumentList.Add("-NoLogo");
		startInfo.ArgumentList.Add("-NoProfile");
		startInfo.ArgumentList.Add("-ExecutionPolicy");
		startInfo.ArgumentList.Add("Bypass");
		startInfo.ArgumentList.Add("-EncodedCommand");
		startInfo.ArgumentList.Add(encoded);
		using Process process = Process.Start(startInfo)
			?? throw new InvalidOperationException("Windows could not start the administrator confirmation window.");
		return process.Id;
	}

	public static bool IsLocalManifestFilesEnabled()
	{
		try
		{
			string path = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"Packages", "Microsoft.DesktopAppInstaller_8wekyb3d8bbwe", "LocalState", "settings.json");
			if (!File.Exists(path)) return false;
			using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
			return document.RootElement.TryGetProperty("experimentalFeatures", out JsonElement features)
				&& features.TryGetProperty("localManifestFiles", out JsonElement enabled)
				&& enabled.ValueKind == JsonValueKind.True;
		}
		catch { return false; }
	}

	public static bool IsWindowsSandboxAvailable()
	{
		string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
		return File.Exists(Path.Combine(windows, "System32", "WindowsSandbox.exe"));
	}

	public static Task<CommandResult> InstallWingetCreateAsync(CancellationToken cancellationToken = default)
	{
		return RunAsync("winget", ["install", "wingetcreate", "--accept-source-agreements", "--accept-package-agreements"], Environment.CurrentDirectory, cancellationToken);
	}

	public static async Task<bool> IsAvailableAsync(string executable, TimeSpan? timeout = null)
	{
		using CancellationTokenSource cancellation = new(timeout ?? TimeSpan.FromSeconds(5));
		try
		{
			CommandResult result = await RunAsync("where.exe", [executable], Environment.CurrentDirectory, cancellation.Token);
			return result.ExitCode == 0;
		}
		catch (OperationCanceledException)
		{
			return false;
		}
		catch
		{
			return false;
		}
	}

	public static async Task<bool> WarmUpAsync(TimeSpan? timeout = null)
	{
		using CancellationTokenSource cancellation = new(timeout ?? TimeSpan.FromSeconds(20));
		try
		{
			CommandResult result = await RunWingetCreateAsync(
				"info",
				string.Empty,
				Environment.CurrentDirectory,
				cancellation.Token);
			return result.ExitCode == 0;
		}
		catch (OperationCanceledException)
		{
			return false;
		}
		catch
		{
			return false;
		}
	}

	private static async Task<CommandResult> RunAsync(
		string executable,
		IEnumerable<string> arguments,
		string workingDirectory,
		CancellationToken cancellationToken)
	{
		ProcessStartInfo startInfo = new()
		{
			FileName = executable,
			WorkingDirectory = Directory.Exists(workingDirectory) ? workingDirectory : Environment.CurrentDirectory,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true,
			StandardOutputEncoding = Encoding.UTF8,
			StandardErrorEncoding = Encoding.UTF8
		};
		foreach (string argument in arguments) startInfo.ArgumentList.Add(argument);
		using Process process = new() { StartInfo = startInfo };
		if (!process.Start()) throw new InvalidOperationException($"Windows could not start {executable}.");
		Task<string> output = process.StandardOutput.ReadToEndAsync(cancellationToken);
		Task<string> error = process.StandardError.ReadToEndAsync(cancellationToken);
		await process.WaitForExitAsync(cancellationToken);
		return new CommandResult(process.ExitCode, await output, await error);
	}

	private static InteractiveCommandSession StartPersistentConsoleSession(
		string executable,
		IReadOnlyList<string> arguments,
		string workingDirectory,
		string title,
		string logPrefix)
	{
		string logFolder = Path.Combine(Path.GetTempPath(), "WingetManifestStudio", "command-logs");
		Directory.CreateDirectory(logFolder);
		string logPath = Path.Combine(logFolder, $"{logPrefix}-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.log");
		const string consoleScript = """
			$ErrorActionPreference = 'Continue'
			$command = $env:WMS_CONSOLE_EXECUTABLE
			$arguments = @((ConvertFrom-Json -InputObject $env:WMS_CONSOLE_ARGUMENTS))
			$host.UI.RawUI.WindowTitle = $env:WMS_CONSOLE_TITLE
			Write-Host ('> ' + $command + ' ' + ($arguments -join ' ')) -ForegroundColor Cyan
			Write-Host ''
			& $command @arguments 2>&1 | Tee-Object -FilePath $env:WMS_CONSOLE_LOG
			$code = $LASTEXITCODE
			Write-Host ''
			if ($code -eq 0) { Write-Host 'The test command completed successfully.' -ForegroundColor Green }
			else { Write-Host ('The test command exited with code ' + $code + '.') -ForegroundColor Red }
			Write-Host 'This window is staying open so you can review the complete result.' -ForegroundColor Yellow
			[void](Read-Host 'Press Enter to close')
			exit $code
			""";
		string encodedScript = Convert.ToBase64String(Encoding.Unicode.GetBytes(consoleScript));
		ProcessStartInfo startInfo = new()
		{
			FileName = "powershell.exe",
			WorkingDirectory = Directory.Exists(workingDirectory) ? workingDirectory : Environment.CurrentDirectory,
			UseShellExecute = false,
			CreateNoWindow = false,
			WindowStyle = ProcessWindowStyle.Normal
		};
		startInfo.ArgumentList.Add("-NoLogo");
		startInfo.ArgumentList.Add("-NoProfile");
		startInfo.ArgumentList.Add("-ExecutionPolicy");
		startInfo.ArgumentList.Add("Bypass");
		startInfo.ArgumentList.Add("-EncodedCommand");
		startInfo.ArgumentList.Add(encodedScript);
		startInfo.Environment["WMS_CONSOLE_EXECUTABLE"] = executable;
		startInfo.Environment["WMS_CONSOLE_ARGUMENTS"] = JsonSerializer.Serialize(arguments);
		startInfo.Environment["WMS_CONSOLE_TITLE"] = title;
		startInfo.Environment["WMS_CONSOLE_LOG"] = logPath;
		using Process process = Process.Start(startInfo)
			?? throw new InvalidOperationException("Windows could not start the persistent test console.");
		return new InteractiveCommandSession(process.Id, logPath);
	}

	public static IReadOnlyList<string> Tokenize(string commandLine)
	{
		List<string> arguments = [];
		StringBuilder current = new();
		bool quoted = false;
		char quote = '\0';
		for (int index = 0; index < commandLine.Length; index++)
		{
			char character = commandLine[index];
			if ((character == '"' || character == '\'') && (!quoted || quote == character))
			{
				quoted = !quoted;
				quote = quoted ? character : '\0';
				continue;
			}
			if (character == '\\' && index + 1 < commandLine.Length && commandLine[index + 1] == quote)
			{
				current.Append(commandLine[++index]);
				continue;
			}
			if (char.IsWhiteSpace(character) && !quoted)
			{
				if (current.Length > 0)
				{
					arguments.Add(current.ToString());
					current.Clear();
				}
				continue;
			}
			current.Append(character);
		}
		if (quoted) throw new FormatException("An argument quote was opened but not closed.");
		if (current.Length > 0) arguments.Add(current.ToString());
		return arguments;
	}
}

internal sealed record InteractiveCommandSession(int ProcessId, string LogPath);
