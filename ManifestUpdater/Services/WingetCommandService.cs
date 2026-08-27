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
		return RunWithTimeoutAsync("winget", ["validate", "--manifest", manifestFolder], manifestFolder, TimeSpan.FromSeconds(45), cancellationToken);
	}

	public static Task<CommandResult> SearchExactPackageAsync(string packageIdentifier, CancellationToken cancellationToken = default)
	{
		return RunWithTimeoutAsync("winget.exe", ["search", "--id", packageIdentifier, "--exact", "--source", "winget", "--accept-source-agreements"], Environment.CurrentDirectory, TimeSpan.FromSeconds(30), cancellationToken);
	}

	public static Task<CommandResult> ListInstalledPackageAsync(string packageIdentifier, CancellationToken cancellationToken = default)
	{
		return RunWithTimeoutAsync("winget.exe", ["list", "--id", packageIdentifier, "--exact", "--accept-source-agreements"], Environment.CurrentDirectory, TimeSpan.FromSeconds(30), cancellationToken);
	}

	public static Task<CommandResult> ListInstalledPackageByNameAsync(string packageName, CancellationToken cancellationToken = default)
	{
		return RunWithTimeoutAsync("winget.exe", ["list", "--name", packageName, "--exact", "--accept-source-agreements"], Environment.CurrentDirectory, TimeSpan.FromSeconds(30), cancellationToken);
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

	internal static ProcessStartInfo CreateEnableLocalManifestFilesStartInfo()
	{
		string wingetPath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"Microsoft", "WindowsApps", "winget.exe");
		if (!File.Exists(wingetPath)) wingetPath = "winget.exe";

		ProcessStartInfo startInfo = new()
		{
			FileName = wingetPath,
			WorkingDirectory = Environment.CurrentDirectory,
			UseShellExecute = true,
			Verb = "runas",
			WindowStyle = ProcessWindowStyle.Hidden
		};
		startInfo.ArgumentList.Add("settings");
		startInfo.ArgumentList.Add("--enable");
		startInfo.ArgumentList.Add("LocalManifestFiles");
		return startInfo;
	}

	public static async Task<CommandResult> EnableLocalManifestFilesElevatedAsync(
		CancellationToken cancellationToken = default)
	{
		using Process process = Process.Start(CreateEnableLocalManifestFilesStartInfo())
			?? throw new InvalidOperationException("Windows could not start Winget with administrator approval.");
		try
		{
			await process.WaitForExitAsync(cancellationToken);
			return process.ExitCode == 0
				? new CommandResult(0, "Winget enabled the LocalManifestFiles administrator setting.", string.Empty)
				: new CommandResult(process.ExitCode, string.Empty,
					$"Winget could not enable LocalManifestFiles (exit code 0x{unchecked((uint)process.ExitCode):X8}).");
		}
		catch (OperationCanceledException)
		{
			try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
			throw;
		}
	}

	public static async Task<WingetHealthResult> CheckWingetHealthAsync(CancellationToken cancellationToken = default)
	{
		using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		timeout.CancelAfter(TimeSpan.FromSeconds(8));
		try
		{
			CommandResult result = await RunAsync("winget.exe", ["--info"], Environment.CurrentDirectory, timeout.Token);
			string output = result.CombinedOutput;
			string version = ParseWingetVersion(output);
			bool localManifestFilesEnabled = ParseLocalManifestFilesEnabled(output);
			if (result.ExitCode == 0)
				return new WingetHealthResult(true, version.IfEmpty("Installed"), 0, "Windows Package Manager is ready.", localManifestFilesEnabled);
			string code = "0x" + unchecked((uint)result.ExitCode).ToString("X8");
			string details = result.CombinedOutput.IfEmpty("Winget returned no diagnostic text.");
			string message = result.ExitCode == unchecked((int)0x8A150001)
				? $"Windows Package Manager returned an internal error ({code}). Repair or update Microsoft App Installer, then run Check Test Setup again."
				: $"Windows Package Manager is not ready ({code}). {details}";
			return new WingetHealthResult(false, string.Empty, result.ExitCode, message);
		}
		catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
		{
			return new WingetHealthResult(false, string.Empty, 1460, "Windows Package Manager did not respond within 8 seconds. Repair or update Microsoft App Installer, then try again.");
		}
		catch (Exception ex)
		{
			return new WingetHealthResult(false, string.Empty, 1, "Windows Package Manager could not start: " + ex.Message);
		}
	}

	internal static string ParseWingetVersion(string output)
	{
		foreach (string line in output.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
		{
			int versionIndex = line.LastIndexOf(" v", StringComparison.OrdinalIgnoreCase);
			if (line.Contains("Windows Package Manager", StringComparison.OrdinalIgnoreCase) && versionIndex >= 0)
				return line[(versionIndex + 1)..].Trim();
		}
		return string.Empty;
	}

	internal static bool ParseLocalManifestFilesEnabled(string output)
	{
		foreach (string line in output.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
		{
			int settingIndex = line.IndexOf("LocalManifestFiles", StringComparison.OrdinalIgnoreCase);
			if (settingIndex < 0) continue;
			string state = line[(settingIndex + "LocalManifestFiles".Length)..].Trim();
			return state.Equals("Enabled", StringComparison.OrdinalIgnoreCase)
				|| state.Equals("Habilitado", StringComparison.OrdinalIgnoreCase)
				|| state.Equals("Activado", StringComparison.OrdinalIgnoreCase);
		}
		return false;
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
		try
		{
			Task<string> output = process.StandardOutput.ReadToEndAsync(cancellationToken);
			Task<string> error = process.StandardError.ReadToEndAsync(cancellationToken);
			await process.WaitForExitAsync(cancellationToken);
			return new CommandResult(process.ExitCode, await output, await error);
		}
		catch (OperationCanceledException)
		{
			try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
			throw;
		}
	}

	private static async Task<CommandResult> RunWithTimeoutAsync(
		string executable,
		IEnumerable<string> arguments,
		string workingDirectory,
		TimeSpan timeout,
		CancellationToken cancellationToken)
	{
		using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		deadline.CancelAfter(timeout);
		try
		{
			return await RunAsync(executable, arguments, workingDirectory, deadline.Token);
		}
		catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
		{
			return new CommandResult(1460, string.Empty, $"{executable} did not respond within {timeout.TotalSeconds:0} seconds and was stopped.");
		}
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
