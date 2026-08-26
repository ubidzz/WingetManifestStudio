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
	{
		ProcessStartInfo startInfo = CreateInteractiveProcessStartInfo(command, arguments, workingDirectory);
		using Process process = Process.Start(startInfo)
			?? throw new InvalidOperationException("Windows could not start WingetCreate in an interactive console.");
		return process.Id;
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
			& wingetcreate @wingetArguments
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
