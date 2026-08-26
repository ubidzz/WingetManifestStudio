using System.Diagnostics;
using System.Text;

namespace ManifestUpdater;

internal static class WingetCommandService
{
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
