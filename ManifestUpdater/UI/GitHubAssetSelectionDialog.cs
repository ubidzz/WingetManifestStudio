namespace ManifestUpdater;

internal sealed class GitHubAssetSelectionDialog : Form
{
	private readonly CheckedListBox assetList;
	private readonly IReadOnlyList<GitHubReleaseAsset> assets;

	private GitHubAssetSelectionDialog(GitHubReleaseImport release)
	{
		assets = release.Assets;
		Text = "Choose GitHub release installers";
		FormBorderStyle = FormBorderStyle.FixedDialog;
		StartPosition = FormStartPosition.CenterParent;
		ShowInTaskbar = false;
		MaximizeBox = false;
		MinimizeBox = false;
		ClientSize = new Size(720, 520);
		BackColor = StudioPalette.Window;
		ForeColor = Color.White;
		Font = new Font("Segoe UI", 9.5F);
		AutoScaleMode = AutoScaleMode.Dpi;

		TableLayoutPanel layout = new()
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 4,
			Padding = new Padding(24),
			BackColor = StudioPalette.Window
		};
		layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
		layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
		layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
		layout.Controls.Add(new Label
		{
			Text = $"{release.Owner}/{release.Repository}  •  {release.Tag}\r\n\r\nChoose only the files users should install. Selected files are downloaded temporarily for SHA-256 and installer inspection.",
			Dock = DockStyle.Fill,
			ForeColor = StudioPalette.SecondaryText,
			AutoSize = false
		}, 0, 0);

		assetList = new CheckedListBox
		{
			Dock = DockStyle.Fill,
			BackColor = StudioPalette.Input,
			ForeColor = StudioPalette.PrimaryText,
			BorderStyle = BorderStyle.FixedSingle,
			CheckOnClick = true,
			IntegralHeight = false,
			Font = new Font("Segoe UI", 9.5F)
		};
		for (int index = 0; index < assets.Count; index++)
			assetList.Items.Add(new AssetItem(index, assets[index]), true);
		layout.Controls.Add(assetList, 0, 1);

		FlowLayoutPanel selectionActions = new()
		{
			Dock = DockStyle.Fill,
			FlowDirection = FlowDirection.LeftToRight,
			WrapContents = false,
			BackColor = StudioPalette.Window
		};
		selectionActions.Controls.Add(NewButton("Select all", (_, _) => SetAll(true), 120));
		selectionActions.Controls.Add(NewButton("Clear all", (_, _) => SetAll(false), 120));
		layout.Controls.Add(selectionActions, 0, 2);

		FlowLayoutPanel finalActions = new()
		{
			Dock = DockStyle.Fill,
			FlowDirection = FlowDirection.RightToLeft,
			WrapContents = false,
			BackColor = StudioPalette.Window
		};
		StudioButton importButton = NewButton("Import selected", (_, _) => { DialogResult = DialogResult.OK; Close(); }, 170, true);
		StudioButton cancelButton = NewButton("Cancel", (_, _) => { DialogResult = DialogResult.Cancel; Close(); }, 120);
		finalActions.Controls.Add(importButton);
		finalActions.Controls.Add(cancelButton);
		layout.Controls.Add(finalActions, 0, 3);
		Controls.Add(layout);
		AcceptButton = importButton;
		CancelButton = cancelButton;
	}

	public static IReadOnlyList<GitHubReleaseAsset>? SelectAssets(IWin32Window owner, GitHubReleaseImport release)
	{
		using GitHubAssetSelectionDialog dialog = new(release);
		return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.GetSelectedAssets() : null;
	}

	private IReadOnlyList<GitHubReleaseAsset> GetSelectedAssets() => assetList.CheckedItems
		.Cast<AssetItem>()
		.Select(item => assets[item.Index])
		.ToArray();

	private void SetAll(bool value)
	{
		for (int index = 0; index < assetList.Items.Count; index++) assetList.SetItemChecked(index, value);
	}

	private static StudioButton NewButton(string text, EventHandler click, int width, bool primary = false)
	{
		StudioButton button = new()
		{
			Text = text,
			Width = width,
			Height = 42,
			ButtonKind = primary ? StudioButtonKind.Primary : StudioButtonKind.Secondary,
			AccessibleName = text,
			Margin = new Padding(8, 0, 0, 0)
		};
		button.Click += click;
		return button;
	}

	private sealed record AssetItem(int Index, GitHubReleaseAsset Asset)
	{
		public override string ToString() => $"{Asset.Name}    ({FormatSize(Asset.Size)})";

		private static string FormatSize(long bytes) => bytes switch
		{
			>= 1024L * 1024 * 1024 => $"{bytes / (1024d * 1024 * 1024):0.##} GB",
			>= 1024L * 1024 => $"{bytes / (1024d * 1024):0.##} MB",
			>= 1024 => $"{bytes / 1024d:0.##} KB",
			_ => $"{bytes} bytes"
		};
	}
}
