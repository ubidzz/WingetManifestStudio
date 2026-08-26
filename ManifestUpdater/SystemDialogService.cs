namespace ManifestUpdater;

internal static class SystemDialogService
{
	public static Task<string?> PickFolderAsync(string title, string description, string? initialPath)
	{
		return RunOnStaThreadAsync(() =>
		{
			using FolderBrowserDialog dialog = new()
			{
				AutoUpgradeEnabled = true,
				Description = $"{title}{Environment.NewLine}{description}",
				UseDescriptionForTitle = true,
				ShowNewFolderButton = true,
				SelectedPath = GetSafeInitialDirectory(initialPath)
			};

			return dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath)
				? Path.GetFullPath(dialog.SelectedPath)
				: null;
		});
	}

	public static Task<string[]> OpenFilesAsync(string title, string? initialPath, string filter, bool multiSelect)
	{
		return RunOnStaThreadAsync(() =>
		{
			using OpenFileDialog dialog = new()
			{
				AutoUpgradeEnabled = true,
				Title = title,
				InitialDirectory = GetSafeInitialDirectory(initialPath),
				Filter = filter,
				Multiselect = multiSelect,
				CheckFileExists = true,
				CheckPathExists = true,
				DereferenceLinks = true,
				RestoreDirectory = true
			};

			return dialog.ShowDialog() == DialogResult.OK
				? dialog.FileNames.Select(Path.GetFullPath).ToArray()
				: [];
		});
	}

	public static Task<string?> SaveFileAsync(
		string title,
		string? initialPath,
		string filter,
		string defaultExtension,
		string initialFileName)
	{
		return RunOnStaThreadAsync(() =>
		{
			using SaveFileDialog dialog = new()
			{
				AutoUpgradeEnabled = true,
				Title = title,
				InitialDirectory = GetSafeInitialDirectory(initialPath),
				FileName = initialFileName,
				Filter = filter,
				DefaultExt = defaultExtension,
				AddExtension = true,
				CheckPathExists = true,
				OverwritePrompt = true,
				RestoreDirectory = true
			};

			return dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.FileName)
				? Path.GetFullPath(dialog.FileName)
				: null;
		});
	}

	internal static Task<T> RunOnStaThreadAsync<T>(Func<T> operation)
	{
		TaskCompletionSource<T> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
		Thread thread = new(() =>
		{
			try
			{
				completion.TrySetResult(operation());
			}
			catch (Exception ex)
			{
				completion.TrySetException(ex);
			}
		})
		{
			IsBackground = true,
			Name = "WingetManifestStudio.SystemDialog"
		};
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		return completion.Task;
	}

	private static string GetSafeInitialDirectory(string? path)
	{
		string candidate = string.Empty;
		if (!string.IsNullOrWhiteSpace(path))
		{
			if (Directory.Exists(path)) candidate = Path.GetFullPath(path);
			else
			{
				string? parent = Path.GetDirectoryName(path);
				if (!string.IsNullOrWhiteSpace(parent) && Directory.Exists(parent)) candidate = Path.GetFullPath(parent);
			}
		}

		if (candidate.Length == 0) return string.Empty;
		string? oneDrive = Environment.GetEnvironmentVariable("OneDrive");
		if (!string.IsNullOrWhiteSpace(oneDrive) && candidate.StartsWith(Path.GetFullPath(oneDrive), StringComparison.OrdinalIgnoreCase))
			return string.Empty;
		return candidate;
	}
}
