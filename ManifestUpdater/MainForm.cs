using System.Diagnostics;
using System.Text;

namespace ManifestUpdater;

public partial class MainForm : Form
{
	private static readonly string[] InstallerExtensions = [".msi", ".exe", ".msix", ".msixbundle", ".appx", ".appxbundle", ".zip"];
	private static readonly Color PageColor = StudioPalette.Window;
	private static readonly Color CardColor = StudioPalette.Card;
	private static readonly Color InputColor = StudioPalette.Input;
	private static readonly Color BorderColor = StudioPalette.Border;
	private static readonly Color MutedColor = StudioPalette.SecondaryText;
	private static readonly Color AccentColor = StudioPalette.Accent;
	private static readonly Color SuccessColor = StudioPalette.Success;

	private ManifestProject project = new();
	private readonly Dictionary<string, Control> fields = new(StringComparer.OrdinalIgnoreCase);
	private DataGridView installerGrid = null!;
	private RichTextBox previewBox = null!;
	private RichTextBox toolOutputBox = null!;
	private StudioComboBox toolCommandBox = null!;
	private StudioTextBox toolArgumentsBox = null!;
	private StudioCheckBox insecureUrlCheck = null!;
	private Button validateButton = null!;
	private CancellationTokenSource? operationCancellation;
	private bool isBusy;
	private readonly Dictionary<string, StudioNavButton> navigationButtons = new(StringComparer.Ordinal);
	private bool draggingWindow;
	private Point dragCursorOrigin;
	private Point dragWindowOrigin;
	private readonly bool uiTestMode;
	private bool systemDialogOpen;

	public MainForm() : this(false)
	{
	}

	internal MainForm(bool uiTestMode)
	{
		this.uiTestMode = uiTestMode;
		InitializeComponent();
		Icon = new System.ComponentModel.ComponentResourceManager(typeof(MainForm)).GetObject("$this.Icon") as Icon;
		brandIcon.Image = Icon?.ToBitmap();
		minimizeButton.Click += (_, _) => WindowState = FormWindowState.Minimized;
		closeButton.Click += (_, _) => Close();
		headerPanel.Resize += (_, _) => LayoutHeaderControls();
		LayoutHeaderControls();
		AttachWindowDrag(headerPanel);
		AttachWindowDrag(brandIcon);
		AttachWindowDrag(titleLabel);
		AttachWindowDrag(subtitleLabel);
		if (System.ComponentModel.LicenseManager.UsageMode != System.ComponentModel.LicenseUsageMode.Designtime)
		{
			SuspendLayout();
			workspaceTabs.SuspendLayout();
			BuildWorkspace();
			workspaceTabs.ResumeLayout(true);
			ResumeLayout(true);
			Shown += MainForm_Shown;
		}
	}

	private async void MainForm_Shown(object? sender, EventArgs e)
	{
		try
		{
			ApplyProjectToControls();
			SetStatus("Ready. Start a new package or explicitly load a manifest folder.");
			if (uiTestMode)
			{
				modeLabel.Text = "SAFE UI TEST MODE";
				return;
			}
			bool available = await WingetCommandService.IsAvailableAsync("wingetcreate.exe", TimeSpan.FromSeconds(3));
			modeLabel.Text = available
				? "WINGETCREATE READY • NO TOKEN STORED"
				: "LOCAL AUTHORING READY • WINGETCREATE OPTIONAL";
		}
		catch (Exception ex)
		{
			ShowError("Startup could not finish", ex);
		}
	}

	private void LayoutHeaderControls()
	{
		const int margin = 22;
		closeButton.Location = new Point(Math.Max(margin, headerPanel.ClientSize.Width - closeButton.Width - margin), 22);
		minimizeButton.Location = new Point(Math.Max(margin, closeButton.Left - minimizeButton.Width - 10), 22);
		int badgeWidth = Math.Min(230, Math.Max(170, headerPanel.ClientSize.Width / 5));
		securityBadge.AutoSize = false;
		securityBadge.Size = new Size(badgeWidth, 34);
		securityBadge.Location = new Point(Math.Max(590, minimizeButton.Left - badgeWidth - 22), 25);
		securityBadge.TextAlign = ContentAlignment.MiddleCenter;
	}

	private void BuildWorkspace()
	{
		TabPage[] pages = [BuildStartTab(), BuildProjectTab(), BuildInstallersTab(), BuildPreviewTab(), BuildToolsTab()];
		workspaceTabs.TabPages.AddRange(pages);
		navigationPanel.Controls.Clear();
		navigationPanel.ColumnStyles.Clear();
		navigationPanel.ColumnCount = pages.Length;
		foreach (TabPage page in pages)
		{
			navigationPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / pages.Length));
			StudioNavButton button = new() { Text = page.Text, Tag = page };
			button.Click += (_, _) => workspaceTabs.SelectedTab = page;
			navigationButtons[page.Text] = button;
			navigationPanel.Controls.Add(button, navigationPanel.Controls.Count, 0);
		}
		workspaceTabs.SelectedIndexChanged += (_, _) => UpdateNavigationState();
		UpdateNavigationState();
	}

	private void UpdateNavigationState()
	{
		foreach ((string title, StudioNavButton button) in navigationButtons)
			button.Selected = string.Equals(workspaceTabs.SelectedTab?.Text, title, StringComparison.Ordinal);
	}

	private void AttachWindowDrag(Control control)
	{
		control.MouseDown += (_, eventArgs) =>
		{
			if (eventArgs.Button != MouseButtons.Left) return;
			draggingWindow = true;
			dragCursorOrigin = Cursor.Position;
			dragWindowOrigin = Location;
		};
		control.MouseMove += (_, _) =>
		{
			if (!draggingWindow) return;
			Point offset = new(Cursor.Position.X - dragCursorOrigin.X, Cursor.Position.Y - dragCursorOrigin.Y);
			Location = new Point(dragWindowOrigin.X + offset.X, dragWindowOrigin.Y + offset.Y);
		};
		control.MouseUp += (_, _) => draggingWindow = false;
	}

	protected override void WndProc(ref Message message)
	{
		const int WmNcHitTest = 0x0084;
		const int HtClient = 1;
		base.WndProc(ref message);
		if (message.Msg != WmNcHitTest || (int)message.Result != HtClient || WindowState == FormWindowState.Maximized) return;

		const int grip = 8;
		Point point = PointToClient(Cursor.Position);
		bool left = point.X <= grip;
		bool right = point.X >= ClientSize.Width - grip;
		bool top = point.Y <= grip;
		bool bottom = point.Y >= ClientSize.Height - grip;
		message.Result = (IntPtr)(top && left ? 13 : top && right ? 14 : bottom && left ? 16 : bottom && right ? 17 : left ? 10 : right ? 11 : top ? 12 : bottom ? 15 : HtClient);
	}

	private TabPage BuildStartTab()
	{
		TabPage page = NewPage("Start Here");
		FlowLayoutPanel content = NewScrollFlow();
		content.Padding = new Padding(18, 20, 18, 30);
		content.Controls.Add(CreateHeroCard());
		content.Controls.Add(CreateWorkflowCard("1", "Choose what you are doing", "Load an existing manifest folder to update a package, or start a new project and choose an empty output folder.",
			("Load existing manifests", async (_, _) => await LoadManifestsAsync()),
			("Create a new project", (_, _) => { NewProject(); SelectTab("Package Details"); })));
		content.Controls.Add(CreateWorkflowCard("2", "Add the release installers", "Choose the local MSI, EXE, MSIX, Appx, or ZIP files that you will upload. The Studio reads those exact files and calculates their SHA-256 hashes. Then enter the public download URL for each file.",
			("Open Installers & Hashes", (_, _) => SelectTab("Installers & Hashes"))));
		content.Controls.Add(CreateWorkflowCard("3", "Review before anything is changed", "Preview builds all three manifests in memory. Save writes them only after validation and keeps timestamped backups of files that already exist.",
			("Open Preview & Submit", (_, _) => SelectTab("Preview & Submit"))));
		content.Controls.Add(CreateWorkflowCard("4", "Validate and submit", "Use the official validation and WingetCreate submission tools when the files are ready. GitHub tokens remain managed by Microsoft's tool and are never stored in a Studio profile.",
			("Open Official Tools", (_, _) => SelectTab("Official Tool Commands"))));
		page.Controls.Add(content);
		return page;
	}

	private TabPage BuildProjectTab()
	{
		TabPage page = NewPage("Package Details");
		TableLayoutPanel root = NewRoot();
		root.RowCount = 3;
		root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		root.Controls.Add(CreateInfoStrip("PACKAGE WORKSPACE", "Every box below is editable. Loading a folder reads its YAML files only; it never downloads installers or changes the manifests."), 0, 0);
		root.Controls.Add(CreateToolbar(
			("New Project", (_, _) => NewProject()),
			("Load Manifests", async (_, _) => await LoadManifestsAsync()),
			("Open Profile", async (_, _) => await OpenProfileAsync()),
			("Save Profile", async (_, _) => await SaveProfileAsync()),
			("Choose Output", async (_, _) => await ChooseOutputFolderAsync())), 0, 1);

		FlowLayoutPanel content = NewScrollFlow();
		content.Controls.Add(CreateSection("PACKAGE IDENTITY", "The values shared by every manifest file.",
			Field("PackageIdentifier", "Package identifier", "Example: Contoso.UsefulApp"),
			Field("PackageVersion", "Package version", "Do not include a leading v."),
			Field("DefaultLocale", "Default locale", "Usually en-US"),
			Field("ManifestVersion", "Winget schema", "Current schema version, for example 1.12.0"),
			Field("ManifestFolder", "Manifest output folder", "Choose any empty folder or an existing manifest folder.")));
		content.Controls.Add(CreateSection("PUBLIC PACKAGE INFORMATION", "Shown to users by Windows Package Manager.",
			Field("PackageName", "Package name"),
			Field("Publisher", "Publisher"),
			Field("Author", "Author"),
			Field("License", "License", "Example: MIT, Proprietary, Freeware"),
			Field("ShortDescription", "Short description"),
			Field("Description", "Full description", multiline: true),
			Field("Moniker", "Moniker"),
			Field("Tags", "Tags", "Comma-separated"),
			Field("Commands", "Commands", "Comma-separated command aliases. Preserved during updates.")));
		content.Controls.Add(CreateSection("PROJECT LINKS & RELEASE", "Use public HTTPS links when available.",
			Field("PublisherUrl", "Publisher URL"),
			Field("PublisherSupportUrl", "Support URL"),
			Field("PackageUrl", "Package URL"),
			Field("LicenseUrl", "License URL"),
			Field("Copyright", "Copyright"),
			Field("ReleaseNotesUrl", "Release notes URL"),
			Field("ReleaseNotes", "Release notes", multiline: true)));
		root.Controls.Add(content, 0, 2);
		page.Controls.Add(root);
		return page;
	}

	private TabPage BuildInstallersTab()
	{
		TabPage page = NewPage("Installers & Hashes");
		TableLayoutPanel root = NewRoot();
		root.RowCount = 4;
		root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		root.Controls.Add(CreateInfoStrip("HOW HASHING WORKS", "Loaded manifests show the URLs and hashes already written in their YAML. To verify or replace a hash, select that row, attach the matching local release file, and inspect it. The Studio never guesses which file belongs to a URL."), 0, 0);
		root.Controls.Add(CreateToolbar(
			("Add Release Files", async (_, _) => await AddInstallerFilesAsync()),
			("Add URL-Only Row", (_, _) => AddUrlInstaller()),
			("Attach File to Selected", async (_, _) => await AttachFileToSelectedAsync()),
			("Inspect Selected", async (_, _) => await InspectSelectedAsync()),
			("Inspect Local Files", async (_, _) => await InspectAllLocalAsync()),
			("Remove", (_, _) => RemoveSelectedInstaller())), 0, 1);

		installerGrid = CreateInstallerGrid();
		root.Controls.Add(installerGrid, 0, 2);
		root.Controls.Add(CreateInstallerDefaults(), 0, 3);
		page.Controls.Add(root);
		return page;
	}

	private TabPage BuildPreviewTab()
	{
		TabPage page = NewPage("Preview & Submit");
		TableLayoutPanel root = NewRoot();
		root.RowCount = 3;
		root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		root.Controls.Add(CreateInfoStrip("REVIEW BEFORE SAVING", "Preview shows the exact YAML that will be written. Saving keeps timestamped backups, and submission uses Microsoft's official WingetCreate sign-in flow."), 0, 0);
		TableLayoutPanel actions = (TableLayoutPanel)CreateToolbar(
			("Preview Changes", (_, _) => GeneratePreview()),
			("Save Manifests", (_, _) => SaveManifests()),
			("Validate Locally", async (_, _) => await ValidateWithWingetAsync()),
			("Submit to Winget", async (_, _) => await SubmitAsync()),
			("Open Output Folder", (_, _) => OpenOutputFolder()));
		validateButton = actions.Controls.OfType<Button>().First(button => button.Text == "Validate Locally");
		root.Controls.Add(actions, 0, 1);
		previewBox = NewRichTextBox();
		previewBox.ReadOnly = true;
		previewBox.Font = new Font("Cascadia Mono", 9.5F);
		previewBox.Text = "Choose Preview to generate the manifests without changing any files.";
		root.Controls.Add(previewBox, 0, 2);
		page.Controls.Add(root);
		return page;
	}

	private TabPage BuildToolsTab()
	{
		TabPage page = NewPage("Official Tool Commands");
		TableLayoutPanel root = NewRoot();
		root.RowCount = 4;
		root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		root.Controls.Add(NewHelpLabel(
			"Full WingetCreate access for New, Update, New-Locale, Update-Locale, Submit, Show, Token, Settings, Cache, Info, and DSC. Commands run directly without cmd.exe.", 950), 0, 0);

		FlowLayoutPanel commandRow = CreateInlinePanel();
		toolCommandBox = NewComboBox(190);
		toolCommandBox.SetItems(["new", "update", "new-locale", "update-locale", "submit", "show", "token", "settings", "cache", "info", "dsc", "help", "version"]);
		toolCommandBox.SelectedIndex = 0;
		commandRow.Controls.Add(NewInlineLabel("Command"));
		commandRow.Controls.Add(toolCommandBox);
		commandRow.Controls.Add(CreateButton("Use Current Project", (_, _) => BuildOfficialCommandFromProject()));
		commandRow.Controls.Add(CreateButton("Install WingetCreate", async (_, _) => await InstallWingetCreateAsync()));
		root.Controls.Add(commandRow, 0, 1);

		FlowLayoutPanel argsRow = CreateInlinePanel();
		argsRow.Controls.Add(NewInlineLabel("Arguments"));
		toolArgumentsBox = NewTextBox(780);
		argsRow.Controls.Add(toolArgumentsBox);
		argsRow.Controls.Add(CreateButton("Run", async (_, _) => await RunOfficialCommandAsync(), true));
		root.Controls.Add(argsRow, 0, 2);

		toolOutputBox = NewRichTextBox();
		toolOutputBox.ReadOnly = true;
		toolOutputBox.Font = new Font("Cascadia Mono", 9F);
		toolOutputBox.Text = "Official command output appears here. GitHub tokens are managed by WingetCreate, not saved in this application.";
		root.Controls.Add(toolOutputBox, 0, 3);
		page.Controls.Add(root);
		return page;
	}

	private void NewProject()
	{
		project = new ManifestProject();
		ApplyProjectToControls();
		previewBox.Clear();
		SetStatus("New project created. Choose an output folder and enter package details.");
	}

	private async Task LoadManifestsAsync()
	{
		if (uiTestMode)
		{
			SetStatus("TEST: Load Manifests opened safely without showing a system dialog.");
			await Task.CompletedTask;
			return;
		}
		if (isBusy)
		{
			SetStatus("Please wait for the current operation to finish.");
			return;
		}
		string? selectedPath = await PickFolderAsync(
			"Load Winget Manifests",
			"Choose the folder that contains the package YAML files. Nothing is changed while loading.",
			fields.GetValueOrDefault("ManifestFolder")?.Text);
		if (string.IsNullOrWhiteSpace(selectedPath)) return;
		try
		{
			SetBusy(true, "Reading manifest files...");
			ManifestProject loadedProject = await Task.Run(() => ManifestService.LoadProject(selectedPath), operationCancellation!.Token);
			project = loadedProject;
			ApplyProjectToControls();
			SelectTab("Package Details");
			SetStatus(project.LoadedFromExistingManifests
				? $"Loaded {project.PackageIdentifier} {project.PackageVersion}. Installer hashes came from the selected YAML and have not been rechecked yet."
				: "The selected folder has no manifests. Enter the package details to create a new set.");
		}
		catch (OperationCanceledException) { SetStatus("Manifest loading cancelled."); }
		catch (Exception ex) { ShowError("Could not load the manifest folder", ex); }
		finally { SetBusy(false); }
	}

	private async Task ChooseOutputFolderAsync()
	{
		if (uiTestMode)
		{
			fields["ManifestFolder"].Text = Path.Combine(Path.GetTempPath(), "WingetManifestStudioUiTest");
			SetStatus("TEST: Output-folder selection completed safely.");
			return;
		}
		string? selectedPath = await PickFolderAsync(
			"Choose Manifest Output Folder",
			"Choose where the three Winget manifest files and their safety backups will be stored.",
			fields.GetValueOrDefault("ManifestFolder")?.Text);
		if (string.IsNullOrWhiteSpace(selectedPath)) return;
		fields["ManifestFolder"].Text = selectedPath;
	}

	private async Task OpenProfileAsync()
	{
		if (uiTestMode) { SetStatus("TEST: Open Profile opened safely without showing a system dialog."); return; }
		string[] selectedPaths = await OpenFilesAsync(
			"Open Winget Studio Profile",
			fields.GetValueOrDefault("ManifestFolder")?.Text,
			[".json"],
			false);
		if (selectedPaths.Length == 0) return;
		try { project = ProfileStore.Load(selectedPaths[0]); ApplyProjectToControls(); SetStatus("Profile loaded."); }
		catch (Exception ex) { ShowError("Could not open the profile", ex); }
	}

	private async Task SaveProfileAsync()
	{
		if (uiTestMode) { SetStatus("TEST: Save Profile completed safely without writing a file."); return; }
		ReadProjectFromControls();
		string profileName = SafeFileName(project.PackageIdentifier.IfEmpty("new-package")) + ".wingetprofile.json";
		string? selectedPath = await SaveFileAsync(
			"Save Winget Studio Profile",
			fields.GetValueOrDefault("ManifestFolder")?.Text,
			[".json"],
			profileName);
		if (string.IsNullOrWhiteSpace(selectedPath)) return;
		try { ProfileStore.Save(selectedPath, project); SetStatus("Profile saved. No GitHub token was included."); }
		catch (Exception ex) { ShowError("Could not save the profile", ex); }
	}

	private async Task AddInstallerFilesAsync()
	{
		if (isBusy) return;
		if (uiTestMode)
		{
			project.Installers.Add(new InstallerArtifact
			{
				LocalFile = @"C:\Test\Package.msi",
				InstallerUrl = "https://example.invalid/Package.msi",
				Architecture = "x64",
				InstallerType = "msi",
				VerificationStatus = "Safe UI test row"
			});
			SetStatus("TEST: Add Release Files created a safe in-memory row.");
			await Task.CompletedTask;
			return;
		}
		string[] selectedPaths = await OpenFilesAsync(
			"Add Release Installers",
			fields.GetValueOrDefault("ManifestFolder")?.Text,
			InstallerExtensions,
			true);
		if (selectedPaths.Length == 0) return;
		List<InstallerArtifact> added = [];
		foreach (string file in selectedPaths)
		{
			InstallerArtifact item = new() { LocalFile = file, Architecture = "x64", VerificationStatus = "Waiting for inspection" };
			project.Installers.Add(item);
			added.Add(item);
		}
		foreach (InstallerArtifact item in added)
			await InspectInstallerAsync(item, allowRemoteDownload: false);
	}

	private void AddUrlInstaller()
	{
		project.Installers.Add(new InstallerArtifact { Architecture = "x64", VerificationStatus = "URL entered manually • not inspected" });
		installerGrid.CurrentCell = installerGrid.Rows[^1].Cells[nameof(InstallerArtifact.InstallerUrl)];
		installerGrid.BeginEdit(true);
	}

	private async Task AttachFileToSelectedAsync()
	{
		if (isBusy) return;
		if (uiTestMode)
		{
			if (installerGrid.CurrentRow?.DataBoundItem is InstallerArtifact selected)
			{
				selected.LocalFile = @"C:\Test\AttachedPackage.msi";
				selected.VerificationStatus = "Safe UI test attachment";
			}
			SetStatus("TEST: Attach File completed safely without showing a system dialog.");
			await Task.CompletedTask;
			return;
		}
		if (installerGrid.CurrentRow?.DataBoundItem is not InstallerArtifact item)
		{
			MessageBox.Show(this, "Select the manifest installer row that belongs to the local release file.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}
		string[] selectedPaths = await OpenFilesAsync(
			"Attach Local Installer",
			fields.GetValueOrDefault("ManifestFolder")?.Text,
			InstallerExtensions,
			false);
		if (selectedPaths.Length == 0) return;
		item.LocalFile = selectedPaths[0];
		item.VerificationStatus = "Local file attached • waiting for inspection";
		await InspectInstallerAsync(item, allowRemoteDownload: false);
	}

	private void RemoveSelectedInstaller()
	{
		if (installerGrid.CurrentRow?.DataBoundItem is InstallerArtifact item)
			project.Installers.Remove(item);
	}

	private async Task InspectSelectedAsync()
	{
		if (isBusy) return;
		if (uiTestMode)
		{
			if (installerGrid.CurrentRow?.DataBoundItem is InstallerArtifact selected)
				selected.VerificationStatus = "Safe UI inspection passed";
			SetStatus("TEST: Inspect Selected completed without file or network access.");
			await Task.CompletedTask;
			return;
		}
		if (installerGrid.CurrentRow?.DataBoundItem is not InstallerArtifact item)
		{
			MessageBox.Show(this, "Select an installer row first.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}
		bool willDownload = !File.Exists(item.LocalFile) && Uri.TryCreate(item.InstallerUrl, UriKind.Absolute, out _);
		if (willDownload)
		{
			DialogResult answer = MessageBox.Show(this,
				"No local release file is attached. Inspecting this row will download the public installer temporarily to calculate its hash. Continue?",
				"Download installer for inspection", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (answer != DialogResult.Yes) return;
		}
		await InspectInstallerAsync(item, allowRemoteDownload: true);
	}

	private async Task InspectAllLocalAsync()
	{
		if (isBusy) return;
		if (uiTestMode)
		{
			foreach (InstallerArtifact item in project.Installers)
				item.VerificationStatus = "Safe UI inspection passed";
			SetStatus("TEST: Inspect Local Files completed without file or network access.");
			await Task.CompletedTask;
			return;
		}
		InstallerArtifact[] localItems = project.Installers.Where(item => File.Exists(item.LocalFile)).ToArray();
		if (localItems.Length == 0)
		{
			MessageBox.Show(this, "There are no attached local installer files. Use Add Release Files or Attach File to Selected first.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}
		foreach (InstallerArtifact item in localItems)
			await InspectInstallerAsync(item, allowRemoteDownload: false);
		SetStatus($"Inspected {localItems.Length} attached local release file(s). URL-only rows were not downloaded.");
	}

	private async Task InspectInstallerAsync(InstallerArtifact item, bool allowRemoteDownload)
	{
		try
		{
			if (!allowRemoteDownload && !File.Exists(item.LocalFile))
				throw new FileNotFoundException("Attach the matching local release file before inspecting this row.");
			SetBusy(true, "Inspecting installer and calculating SHA-256...");
			Progress<string> progress = new(SetStatus);
			InstallerInspection result = await InstallerInspector.InspectAsync(item.LocalFile, item.InstallerUrl, progress, operationCancellation!.Token);
			item.Sha256 = result.Sha256;
			item.Architecture = result.Architecture.IfEmpty(item.Architecture).IfEmpty("x64");
			item.InstallerType = result.InstallerType;
			item.ProductCode = result.ProductCode;
			item.UpgradeCode = result.UpgradeCode;
			item.ProductVersion = result.ProductVersion;
			item.DisplayName = result.DisplayName;
			item.Publisher = result.Publisher;
			item.VerificationStatus = File.Exists(item.LocalFile) ? "Verified from local release file" : "Calculated from temporary URL download";
			SynchronizePackageVersionFromInstaller(item, result.ProductVersion);
			if (string.IsNullOrWhiteSpace(project.PackageName) && !string.IsNullOrWhiteSpace(result.DisplayName))
				fields["PackageName"].Text = result.DisplayName;
			if (string.IsNullOrWhiteSpace(project.Publisher) && !string.IsNullOrWhiteSpace(result.Publisher))
				fields["Publisher"].Text = result.Publisher;
			SetStatus($"Inspected {Path.GetFileName(item.LocalFile.IfEmpty(item.InstallerUrl))}: {FormatSize(result.FileSize)}, {result.Architecture}, {result.InstallerType}.");
		}
		catch (OperationCanceledException) { SetStatus("Installer inspection cancelled."); }
		catch (Exception ex) { ShowError("Installer inspection failed", ex); }
		finally { SetBusy(false); installerGrid.Refresh(); }
	}

	private void SynchronizePackageVersionFromInstaller(InstallerArtifact inspectedInstaller, string inspectedVersion)
	{
		string newVersion = inspectedVersion.Trim().TrimStart('v', 'V');
		if (string.IsNullOrWhiteSpace(newVersion)) return;
		string oldVersion = fields["PackageVersion"].Text.Trim().TrimStart('v', 'V');
		if (string.Equals(oldVersion, newVersion, StringComparison.OrdinalIgnoreCase)) return;
		bool conflictingInstaller = project.Installers
			.Where(installer => !ReferenceEquals(installer, inspectedInstaller))
			.Select(installer => installer.ProductVersion.Trim().TrimStart('v', 'V'))
			.Any(version => !string.IsNullOrWhiteSpace(version) && !string.Equals(version, newVersion, StringComparison.OrdinalIgnoreCase));
		if (conflictingInstaller)
		{
			SetStatus($"The inspected file is version {newVersion}, but another installer reports a different version. The package version was not changed.");
			return;
		}

		fields["PackageVersion"].Text = newVersion;
		project.PackageVersion = newVersion;
		if (!string.IsNullOrWhiteSpace(oldVersion))
		{
			fields["ReleaseNotesUrl"].Text = ManifestService.SynchronizeGitHubReleaseUrl(fields["ReleaseNotesUrl"].Text, oldVersion, newVersion);
			project.ReleaseNotesUrl = fields["ReleaseNotesUrl"].Text;
			foreach (InstallerArtifact installer in project.Installers)
				installer.InstallerUrl = ManifestService.SynchronizeGitHubReleaseUrl(installer.InstallerUrl, oldVersion, newVersion);
		}
	}

	private void GeneratePreview()
	{
		if (uiTestMode)
		{
			previewBox.Text = "SAFE UI TEST PREVIEW\r\nNo manifests were generated or changed.";
			SelectTab("Preview & Submit");
			SetStatus("TEST: Preview generated safely in memory.");
			return;
		}
		try
		{
			ReadProjectFromControls();
			ManifestGenerationResult result = ManifestService.Generate(project);
			StringBuilder preview = new();
			preview.AppendLine("PLANNED CHANGES");
			foreach (string change in result.Changes) preview.AppendLine("• " + change);
			foreach (string warning in result.Warnings) preview.AppendLine("WARNING: " + warning);
			foreach ((string name, string content) in result.Files)
			{
				preview.AppendLine().AppendLine(new string('═', 90)).AppendLine(name).AppendLine(new string('─', 90)).AppendLine(content);
			}
			previewBox.Text = preview.ToString();
			SelectTab("Preview & Submit");
			SetStatus($"Preview generated for {result.Files.Count} manifest files. No files were changed.");
		}
		catch (Exception ex) { ShowError("The project is not ready to preview", ex); }
	}

	private bool SaveManifests()
	{
		if (uiTestMode)
		{
			SetStatus("TEST: Save Manifests completed safely without writing files.");
			return true;
		}
		try
		{
			ReadProjectFromControls();
			ManifestGenerationResult result = ManifestService.Generate(project);
			ManifestService.Save(project, result);
			project.LoadedFromExistingManifests = true;
			SetStatus($"Saved {result.Files.Count} manifests. Existing files were backed up first.");
			return true;
		}
		catch (Exception ex) { ShowError("The manifests could not be saved", ex); return false; }
	}

	private async Task ValidateWithWingetAsync()
	{
		if (uiTestMode)
		{
			previewBox.Text = "SAFE UI TEST VALIDATION\r\nOfficial validation was intentionally not launched.";
			SetStatus("TEST: Validate Locally completed safely without launching a process.");
			await Task.CompletedTask;
			return;
		}
		string? cleanFolder = null;
		try
		{
			ReadProjectFromControls();
			ManifestGenerationResult generated = ManifestService.Generate(project);
			cleanFolder = ManifestService.CreateCleanManifestFolder(generated);
			SetBusy(true);
			SetStatus("Running official local Winget validation in a clean temporary folder...");
			CommandResult result = await WingetCommandService.ValidateManifestAsync(cleanFolder, operationCancellation!.Token);
			previewBox.Text = result.CombinedOutput;
			SetStatus(result.ExitCode == 0
				? "Official Winget validation passed. No manifest files were changed."
				: "Winget validation reported problems. Review the output.");
		}
		catch (Exception ex) { ShowError("Winget validation could not run", ex); }
		finally
		{
			try { ManifestService.DeleteCleanManifestFolder(cleanFolder); } catch { }
			SetBusy(false);
		}
	}

	private async Task SubmitAsync()
	{
		if (uiTestMode)
		{
			SetStatus("TEST: Submit to Winget completed safely without authentication or submission.");
			await Task.CompletedTask;
			return;
		}
		if (!SaveManifests()) return;
		DialogResult answer = MessageBox.Show(this,
			"This opens Microsoft's official WingetCreate submission workflow and may create a pull request in microsoft/winget-pkgs. Continue?",
			"Submit manifests", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
		if (answer != DialogResult.Yes) return;
		string? cleanFolder = null;
		try
		{
			ReadProjectFromControls();
			cleanFolder = ManifestService.CreateCleanManifestFolder(ManifestService.Generate(project));
			await RunToolAsync("submit", QuoteArgument(cleanFolder), cleanFolder);
		}
		finally
		{
			try { ManifestService.DeleteCleanManifestFolder(cleanFolder); } catch { }
		}
	}

	private void BuildOfficialCommandFromProject()
	{
		ReadProjectFromControls();
		string command = toolCommandBox.Text;
		string folder = project.ManifestFolder;
		toolArgumentsBox.Text = command switch
		{
			"new" => string.Join(" ", project.Installers.Select(item => QuoteArgument(item.InstallerUrl)).Where(value => value != "\"\"")),
			"update" => $"--urls {string.Join(" ", project.Installers.Select(item => QuoteArgument(item.InstallerUrl)))} --version {QuoteArgument(project.PackageVersion)} --out {QuoteArgument(folder)} {QuoteArgument(project.PackageIdentifier)}",
			"submit" => $"--prtitle {QuoteArgument($"Add or update {project.PackageIdentifier} {project.PackageVersion}")} {QuoteArgument(folder)}",
			"show" => $"--version {QuoteArgument(project.PackageVersion)} {QuoteArgument(project.PackageIdentifier)}",
			"new-locale" or "update-locale" => $"--locale {QuoteArgument(project.DefaultLocale)} --out {QuoteArgument(folder)} --version {QuoteArgument(project.PackageVersion)} {QuoteArgument(project.PackageIdentifier)}",
			_ => string.Empty
		};
	}

	private async Task RunOfficialCommandAsync() => await RunToolAsync(toolCommandBox.Text, toolArgumentsBox.Text);

	private async Task RunToolAsync(string command, string arguments, string? workingDirectory = null)
	{
		if (uiTestMode)
		{
			toolOutputBox.Text = $"> wingetcreate {command} {arguments}\r\n\r\nSAFE UI TEST: No external process was launched.";
			SelectTab("Official Tool Commands");
			SetStatus("TEST: Official command completed safely without launching a process.");
			await Task.CompletedTask;
			return;
		}
		try
		{
			SetBusy(true);
			toolOutputBox.Text = $"> wingetcreate {command} {arguments}{Environment.NewLine}{Environment.NewLine}";
			SelectTab("Official Tool Commands");
			CommandResult result = await WingetCommandService.RunWingetCreateAsync(
				command,
				arguments,
				string.IsNullOrWhiteSpace(workingDirectory) ? project.ManifestFolder : workingDirectory,
				operationCancellation!.Token);
			toolOutputBox.AppendText(result.CombinedOutput);
			SetStatus(result.ExitCode == 0 ? "WingetCreate completed successfully." : $"WingetCreate exited with code {result.ExitCode}.");
		}
		catch (Exception ex) { ShowError("WingetCreate could not run", ex); }
		finally { SetBusy(false); }
	}

	private async Task InstallWingetCreateAsync()
	{
		if (uiTestMode)
		{
			toolOutputBox.Text = "SAFE UI TEST: WingetCreate installation was intentionally not launched.";
			SetStatus("TEST: Install WingetCreate completed safely without changing the computer.");
			await Task.CompletedTask;
			return;
		}
		try
		{
			SetBusy(true);
			CommandResult result = await WingetCommandService.InstallWingetCreateAsync(operationCancellation!.Token);
			toolOutputBox.Text = result.CombinedOutput;
			SetStatus(result.ExitCode == 0 ? "WingetCreate is installed." : "WingetCreate installation reported a problem.");
		}
		catch (Exception ex) { ShowError("WingetCreate could not be installed", ex); }
		finally { SetBusy(false); }
	}

	private void OpenOutputFolder()
	{
		if (uiTestMode) { SetStatus("TEST: Open Output Folder completed safely without opening Explorer."); return; }
		ReadProjectFromControls();
		if (!Directory.Exists(project.ManifestFolder)) { MessageBox.Show(this, "Save the manifests first.", Text); return; }
		Process.Start(new ProcessStartInfo { FileName = project.ManifestFolder, UseShellExecute = true });
	}

	private void ReadProjectFromControls()
	{
		project.PackageIdentifier = Read("PackageIdentifier");
		project.PackageVersion = Read("PackageVersion").TrimStart('v', 'V');
		project.DefaultLocale = Read("DefaultLocale");
		project.ManifestVersion = Read("ManifestVersion");
		project.ManifestFolder = Read("ManifestFolder");
		project.Publisher = Read("Publisher");
		project.PublisherUrl = Read("PublisherUrl");
		project.PublisherSupportUrl = Read("PublisherSupportUrl");
		project.Author = Read("Author");
		project.PackageName = Read("PackageName");
		project.PackageUrl = Read("PackageUrl");
		project.License = Read("License");
		project.LicenseUrl = Read("LicenseUrl");
		project.Copyright = Read("Copyright");
		project.ShortDescription = Read("ShortDescription");
		project.Description = Read("Description");
		project.Moniker = Read("Moniker");
		project.Tags = Read("Tags");
		project.Commands = Read("Commands");
		project.ReleaseNotes = Read("ReleaseNotes");
		project.ReleaseNotesUrl = Read("ReleaseNotesUrl");
		project.InstallerType = Read("InstallerType");
		project.Scope = Read("Scope");
		project.InstallModes = Read("InstallModes");
		project.UpgradeBehavior = Read("UpgradeBehavior");
		project.ElevationRequirement = Read("ElevationRequirement");
		project.AllowInsecureUrls = insecureUrlCheck.Checked;
	}

	private void ApplyProjectToControls()
	{
		project.EnsureInstallerCollection();
		Write("PackageIdentifier", project.PackageIdentifier);
		Write("PackageVersion", project.PackageVersion);
		Write("DefaultLocale", project.DefaultLocale);
		Write("ManifestVersion", project.ManifestVersion);
		Write("ManifestFolder", project.ManifestFolder);
		Write("Publisher", project.Publisher);
		Write("PublisherUrl", project.PublisherUrl);
		Write("PublisherSupportUrl", project.PublisherSupportUrl);
		Write("Author", project.Author);
		Write("PackageName", project.PackageName);
		Write("PackageUrl", project.PackageUrl);
		Write("License", project.License);
		Write("LicenseUrl", project.LicenseUrl);
		Write("Copyright", project.Copyright);
		Write("ShortDescription", project.ShortDescription);
		Write("Description", project.Description);
		Write("Moniker", project.Moniker);
		Write("Tags", project.Tags);
		Write("Commands", project.Commands);
		Write("ReleaseNotes", project.ReleaseNotes);
		Write("ReleaseNotesUrl", project.ReleaseNotesUrl);
		Write("InstallerType", project.InstallerType);
		Write("Scope", project.Scope);
		Write("InstallModes", project.InstallModes);
		Write("UpgradeBehavior", project.UpgradeBehavior);
		Write("ElevationRequirement", project.ElevationRequirement);
		insecureUrlCheck.Checked = project.AllowInsecureUrls;
		installerGrid.DataSource = project.Installers;
	}

	private Control CreateInstallerDefaults()
	{
		FlowLayoutPanel row = CreateInlinePanel();
		row.Padding = new Padding(14, 9, 14, 9);
		row.Controls.Add(NewInlineLabel("Defaults"));
		row.Controls.Add(ChoiceField("InstallerType", "Installer type", ["exe", "msi", "wix", "inno", "nullsoft", "msix", "portable"], 150));
		row.Controls.Add(ChoiceField("Scope", "Scope", ["user", "machine"], 125));
		row.Controls.Add(Field("InstallModes", "Install modes", "Comma-separated", width: 270));
		row.Controls.Add(ChoiceField("UpgradeBehavior", "Upgrade behavior", ["install", "uninstallPrevious", "deny"], 180));
		row.Controls.Add(Field("ElevationRequirement", "Elevation", "optional", width: 125));
		insecureUrlCheck = NewCheckBox("Allow HTTP URLs");
		row.Controls.Add(insecureUrlCheck);
		return row;
	}

	private DataGridView CreateInstallerGrid()
	{
		StudioDataGridView grid = new()
		{
			Dock = DockStyle.Fill,
			ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText,
			ScrollBars = ScrollBars.Both
		};
		void Add(string property, string title, int width, bool fill = false)
		{
			grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = property, Name = property, HeaderText = title, Width = width, AutoSizeMode = fill ? DataGridViewAutoSizeColumnMode.Fill : DataGridViewAutoSizeColumnMode.None });
		}
		Add(nameof(InstallerArtifact.LocalFile), "LOCAL RELEASE FILE", 245);
		Add(nameof(InstallerArtifact.InstallerUrl), "PUBLIC INSTALLER URL", 340);
		Add(nameof(InstallerArtifact.VerificationStatus), "HASH SOURCE / STATUS", 255);
		Add(nameof(InstallerArtifact.Architecture), "ARCH", 65);
		Add(nameof(InstallerArtifact.InstallerType), "TYPE", 75);
		Add(nameof(InstallerArtifact.Scope), "SCOPE", 70);
		Add(nameof(InstallerArtifact.Sha256), "SHA-256", 220);
		Add(nameof(InstallerArtifact.ProductCode), "PRODUCT CODE", 190);
		Add(nameof(InstallerArtifact.UpgradeCode), "UPGRADE CODE", 190);
		grid.DataError += (_, eventArgs) =>
		{
			eventArgs.ThrowException = false;
			SetStatus("That installer value could not be applied. Review the selected cell and try again.");
		};
		grid.CellFormatting += (_, eventArgs) =>
		{
			if (eventArgs.RowIndex < 0 || eventArgs.ColumnIndex < 0 || grid.Columns[eventArgs.ColumnIndex].Name != nameof(InstallerArtifact.VerificationStatus)) return;
			string value = Convert.ToString(eventArgs.Value) ?? string.Empty;
			eventArgs.CellStyle!.ForeColor = value.Contains("verified", StringComparison.OrdinalIgnoreCase) || value.Contains("calculated", StringComparison.OrdinalIgnoreCase)
				? StudioPalette.Success
				: value.Contains("failed", StringComparison.OrdinalIgnoreCase) || value.Contains("missing", StringComparison.OrdinalIgnoreCase)
					? Color.FromArgb(255, 105, 125)
					: StudioPalette.Warning;
		};
		return grid;
	}

	private Control Field(string key, string label, string hint = "", bool multiline = false, int width = 520)
	{
		Panel wrapper = new() { Width = width, Height = multiline ? 115 : 70, Margin = new Padding(8) };
		Label caption = new() { Text = label, AutoSize = true, ForeColor = Color.FromArgb(189, 213, 244), Font = new Font("Segoe UI Semibold", 9F), Location = new Point(0, 0) };
		StudioTextBox box = NewTextBox(width);
		box.Multiline = multiline;
		box.Height = multiline ? 78 : 38;
		box.Location = new Point(0, 24);
		box.PlaceholderText = hint;
		wrapper.Controls.Add(caption);
		wrapper.Controls.Add(box);
		fields[key] = box;
		return wrapper;
	}

	private Control ChoiceField(string key, string label, IEnumerable<string> choices, int width)
	{
		Panel wrapper = new() { Width = width, Height = 70, Margin = new Padding(8) };
		Label caption = new() { Text = label, AutoSize = true, ForeColor = Color.FromArgb(189, 213, 244), Font = new Font("Segoe UI Semibold", 9F), Location = new Point(0, 0) };
		StudioComboBox box = NewComboBox(width);
		box.Location = new Point(0, 24);
		box.SetItems(choices);
		wrapper.Controls.Add(caption);
		wrapper.Controls.Add(box);
		fields[key] = box;
		return wrapper;
	}

	private Control CreateSection(string title, string subtitle, params Control[] controls)
	{
		StudioCard card = new()
		{
			Width = 1160,
			Height = 320,
			AutoSize = false,
			ColumnCount = 1,
			RowCount = 3,
			BackColor = CardColor,
			Margin = new Padding(0, 0, 0, 16),
			Padding = new Padding(20)
		};
		card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		Label heading = new() { Text = title, Dock = DockStyle.Top, Height = 28, ForeColor = AccentColor, Font = new Font("Segoe UI Semibold", 11F) };
		Label help = new() { Text = subtitle, Dock = DockStyle.Top, AutoSize = true, MaximumSize = new Size(1090, 0), Padding = new Padding(0, 0, 0, 10), ForeColor = MutedColor };
		FlowLayoutPanel fieldLayout = new()
		{
			Width = 1090,
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowOnly,
			FlowDirection = FlowDirection.LeftToRight,
			WrapContents = true,
			BackColor = CardColor,
			Margin = Padding.Empty
		};
		fieldLayout.Controls.AddRange(controls);
		int lastLayoutWidth = -1;
		card.SizeChanged += (_, _) =>
		{
			int availableWidth = Math.Max(760, card.ClientSize.Width - card.Padding.Horizontal);
			if (availableWidth == lastLayoutWidth) return;
			lastLayoutWidth = availableWidth;
			fieldLayout.Width = availableWidth;
			fieldLayout.MaximumSize = new Size(availableWidth, 0);
			help.MaximumSize = new Size(availableWidth, 0);
			fieldLayout.PerformLayout();
			int helpHeight = Math.Max(24, help.GetPreferredSize(new Size(availableWidth, 0)).Height);
			int fieldsHeight = Math.Max(70, fieldLayout.GetPreferredSize(new Size(availableWidth, 0)).Height);
			card.Height = card.Padding.Vertical + heading.Height + helpHeight + fieldsHeight + 8;
		};
		card.Controls.Add(heading, 0, 0);
		card.Controls.Add(help, 0, 1);
		card.Controls.Add(fieldLayout, 0, 2);
		return card;
	}

	private Control CreateHeroCard()
	{
		StudioCard hero = new()
		{
			Width = 1160,
			Height = 150,
			ColumnCount = 2,
			BackColor = CardColor,
			Padding = new Padding(24),
			Margin = new Padding(0, 0, 0, 18)
		};
		hero.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72));
		hero.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
		Label title = new() { Text = "Build a Winget submission without editing YAML by hand.", Dock = DockStyle.Top, Height = 44, Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold), ForeColor = Color.White };
		Label description = new() { Text = "Create a new three-file manifest set or safely update an existing one. Local release files provide the real SHA-256 hash; public URLs tell Winget where users will download them.", Dock = DockStyle.Fill, MaximumSize = new Size(780, 0), ForeColor = MutedColor, Font = new Font("Segoe UI", 10.5F) };
		Panel copy = new() { Dock = DockStyle.Fill };
		copy.Controls.Add(description);
		copy.Controls.Add(title);
		Label safety = new() { Text = "LOCAL-FIRST\n\nNo GitHub token stored\nNo manifest overwritten without backup\nNo installer downloaded automatically", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = SuccessColor, Font = new Font("Segoe UI Semibold", 9.5F), BackColor = InputColor, Padding = new Padding(18) };
		hero.Controls.Add(copy, 0, 0);
		hero.Controls.Add(safety, 1, 0);
		return hero;
	}

	private Control CreateWorkflowCard(string number, string title, string description, params (string text, EventHandler handler)[] actions)
	{
		StudioCard card = new()
		{
			Width = 1160,
			Height = Math.Max(112, 36 + actions.Length * 52),
			ColumnCount = 3,
			BackColor = CardColor,
			Padding = new Padding(18),
			Margin = new Padding(0, 0, 0, 12)
		};
		card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58));
		card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		card.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		Label step = new() { Text = number, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold), ForeColor = AccentColor, BackColor = InputColor, Margin = new Padding(0, 0, 14, 0) };
		TableLayoutPanel copy = new() { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = CardColor };
		copy.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold), ForeColor = Color.White }, 0, 0);
		copy.Controls.Add(new Label { Text = description, Dock = DockStyle.Fill, ForeColor = MutedColor, MaximumSize = new Size(700, 0), AutoSize = true }, 0, 1);
		FlowLayoutPanel buttons = new() { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = CardColor };
		foreach ((string text, EventHandler handler) in actions) buttons.Controls.Add(CreateButton(text, handler, true));
		card.Controls.Add(step, 0, 0);
		card.Controls.Add(copy, 1, 0);
		card.Controls.Add(buttons, 2, 0);
		return card;
	}

	private Control CreateInfoStrip(string heading, string message)
	{
		StudioCard panel = new() { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, BackColor = Color.FromArgb(8, 42, 54), Padding = new Padding(14), Margin = new Padding(4, 4, 4, 8), CornerRadius = 10 };
		panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
		panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		panel.Controls.Add(new Label { Text = heading, Dock = DockStyle.Fill, AutoSize = true, Font = new Font("Segoe UI Semibold", 9F), ForeColor = AccentColor }, 0, 0);
		panel.Controls.Add(new Label { Text = message, Dock = DockStyle.Fill, AutoSize = true, MaximumSize = new Size(900, 0), ForeColor = Color.FromArgb(195, 218, 236) }, 1, 0);
		return panel;
	}

	private static TabPage NewPage(string title) => new(title) { BackColor = PageColor, ForeColor = Color.White, Padding = new Padding(18) };
	private static TableLayoutPanel NewRoot()
	{
		TableLayoutPanel root = new() { Dock = DockStyle.Fill, BackColor = PageColor, ColumnCount = 1, RowCount = 1, Padding = new Padding(4) };
		root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		return root;
	}
	private FlowLayoutPanel NewScrollFlow()
	{
		FlowLayoutPanel flow = new() { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = false, FlowDirection = FlowDirection.TopDown, BackColor = PageColor, Padding = new Padding(0, 14, 8, 18) };
		bool resizingChildren = false;
		flow.ClientSizeChanged += (_, _) =>
		{
			if (resizingChildren || flow.IsDisposed) return;
			resizingChildren = true;
			try
			{
				int width = Math.Max(820, flow.ClientSize.Width - flow.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 10);
				foreach (Control control in flow.Controls)
				{
					if (control.Width != width) control.Width = width;
				}
			}
			finally
			{
				resizingChildren = false;
			}
		};
		return flow;
	}
	private static FlowLayoutPanel CreateInlinePanel() => new() { Dock = DockStyle.Top, AutoSize = true, BackColor = CardColor, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(10) };

	private Control CreateToolbar(params (string text, EventHandler handler)[] actions)
	{
		TableLayoutPanel panel = new()
		{
			Dock = DockStyle.Top,
			Height = 66,
			BackColor = CardColor,
			ColumnCount = actions.Length,
			RowCount = 1,
			Padding = new Padding(12, 10, 12, 10),
			Margin = new Padding(0, 0, 0, 10)
		};
		panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		for (int index = 0; index < actions.Length; index++)
		{
			panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / Math.Max(1, actions.Length)));
			StudioButton button = (StudioButton)CreateButton(actions[index].text, actions[index].handler);
			if (index == 0) button.ButtonKind = StudioButtonKind.Primary;
			button.AutoSize = false;
			button.Dock = DockStyle.Fill;
			button.Margin = new Padding(5, 2, 5, 2);
			panel.Controls.Add(button, index, 0);
		}
		return panel;
	}

	private Button CreateButton(string text, EventHandler handler, bool primary = false)
	{
		StudioButton button = new()
		{
			Text = text,
			AutoSize = true,
			Height = 42,
			MinimumSize = new Size(118, 42),
			ButtonKind = primary ? StudioButtonKind.Primary : StudioButtonKind.Secondary,
			Margin = new Padding(5)
		};
		button.Click += handler;
		return button;
	}

	private Button ActionButton(string text, EventHandler handler, bool primary = false)
	{
		Button button = CreateButton(text, handler, primary);
		button.Width = 185;
		button.Height = 42;
		return button;
	}

	private static StudioTextBox NewTextBox(int width) => new() { Width = width, Height = 38, BackColor = InputColor, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5F) };
	private static StudioComboBox NewComboBox(int width) => new() { Width = width, Height = 38, BackColor = InputColor, ForeColor = Color.White };
	private static RichTextBox NewRichTextBox() => new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(4, 12, 22), ForeColor = Color.FromArgb(221, 233, 249), BorderStyle = BorderStyle.None, Padding = new Padding(12) };
	private static Label NewInlineLabel(string text) => new() { Text = text, AutoSize = true, ForeColor = MutedColor, Margin = new Padding(6, 11, 8, 0) };
	private static Label NewHelpLabel(string text, int width) => new() { Text = text, Width = width, AutoSize = true, MaximumSize = new Size(width, 0), ForeColor = MutedColor, Margin = new Padding(8, 12, 8, 8) };
	private static StudioCheckBox NewCheckBox(string text) => new() { Text = text, ForeColor = MutedColor, Margin = new Padding(14, 27, 8, 0), Width = 170 };

	internal async Task<IReadOnlyList<string>> RunUiVerificationAsync()
	{
		List<string> report = [];
		int passed = 0;
		int failed = 0;

		void Record(bool success, string name, string? details = null)
		{
			if (success) passed++; else failed++;
			report.Add($"{(success ? "PASS" : "FAIL")}: {name}{(string.IsNullOrWhiteSpace(details) ? string.Empty : " — " + details)}");
		}

		IEnumerable<Control> Descendants(Control parent)
		{
			foreach (Control child in parent.Controls)
			{
				yield return child;
				foreach (Control nested in Descendants(child))
					yield return nested;
			}
		}

		try
		{
			PerformLayout();
			LayoutHeaderControls();
			Record(securityBadge.Right + 12 <= minimizeButton.Left, "Header badge and window buttons do not overlap",
				$"badge right {securityBadge.Right}, minimize left {minimizeButton.Left}");
			Record(closeButton.Left > minimizeButton.Right, "Minimize and Close buttons are aligned with a visible gap");

			foreach ((string title, StudioNavButton button) in navigationButtons)
			{
				button.PerformClick();
				Application.DoEvents();
				Record(string.Equals(workspaceTabs.SelectedTab?.Text, title, StringComparison.Ordinal) && button.Selected, $"Navigation tab: {title}");
			}

			StudioTextBox[] textBoxes = Descendants(this).OfType<StudioTextBox>().Where(control => control.Enabled && !control.ReadOnly).ToArray();
			foreach (StudioTextBox textBox in textBoxes)
			{
				string original = textBox.Text;
				string testValue = original + " UI-TEST";
				textBox.Text = testValue;
				Record(textBox.Text == testValue, "Custom text box accepts and returns text");
				textBox.Text = original;
			}

			StudioComboBox[] comboBoxes = Descendants(this).OfType<StudioComboBox>().Where(control => control.Enabled).ToArray();
			foreach (StudioComboBox comboBox in comboBoxes)
			{
				int original = comboBox.SelectedIndex;
				if (comboBox.Items.Count == 0)
				{
					Record(false, "Custom dropdown has choices", "No choices were configured.");
					continue;
				}
				int candidate = comboBox.Items.Count > 1 ? (original == 0 ? 1 : 0) : 0;
				comboBox.SelectedIndex = candidate;
				Record(comboBox.SelectedIndex == candidate && comboBox.Text == comboBox.Items[candidate], "Custom dropdown selection");
				comboBox.SelectedIndex = original;
			}
			if (comboBoxes.Length > 0)
			{
				comboBoxes[0].ExerciseDropDownLifecycle();
				Record(true, "Custom dropdown can open, close, and reopen without reusing disposed UI");
			}

			StudioCheckBox[] checkBoxes = Descendants(this).OfType<StudioCheckBox>().Where(control => control.Enabled).ToArray();
			foreach (StudioCheckBox checkBox in checkBoxes)
			{
				bool original = checkBox.Checked;
				checkBox.Checked = !original;
				Record(checkBox.Checked != original, $"Custom checkbox: {checkBox.Text}");
				checkBox.Checked = original;
			}

			project.Installers.Clear();
			InstallerArtifact gridItem = new()
			{
				LocalFile = @"C:\Test\GridPackage.msi",
				InstallerUrl = "https://example.invalid/GridPackage.msi",
				Architecture = "x64",
				InstallerType = "msi",
				VerificationStatus = "Safe UI grid test"
			};
			project.Installers.Add(gridItem);
			installerGrid.Refresh();
			Record(installerGrid.Rows.Count == 1, "Installer grid displays bound rows");
			installerGrid.Rows[0].Cells[nameof(InstallerArtifact.InstallerUrl)].Value = "https://example.invalid/Edited.msi";
			installerGrid.EndEdit();
			Record(string.Equals(gridItem.InstallerUrl, "https://example.invalid/Edited.msi", StringComparison.Ordinal), "Installer grid edits update the project model");

			int responsiveTicks = 0;
			using (System.Windows.Forms.Timer responsivenessTimer = new() { Interval = 20 })
			{
				responsivenessTimer.Tick += (_, _) => responsiveTicks++;
				responsivenessTimer.Start();
				await SystemDialogService.RunOnStaThreadAsync(() =>
				{
					Thread.Sleep(300);
					return true;
				});
				responsivenessTimer.Stop();
			}
			Record(responsiveTicks >= 5, "Windows picker work does not block the main interface", $"UI timer advanced {responsiveTicks} times");

			StudioButton[] actionButtons = Descendants(this)
				.OfType<StudioButton>()
				.Where(button => button != closeButton && button != minimizeButton && button is not StudioNavButton)
				.ToArray();
			foreach (StudioButton button in actionButtons)
			{
				Control? ancestor = button.Parent;
				while (ancestor is not null && ancestor is not TabPage) ancestor = ancestor.Parent;
				if (ancestor is TabPage tabPage) workspaceTabs.SelectedTab = tabPage;
				Application.DoEvents();
				try
				{
					button.PerformClick();
					Application.DoEvents();
					await Task.Delay(35);
					Application.DoEvents();
					Record(true, $"Button: {button.Text}");
				}
				catch (Exception ex)
				{
					Record(false, $"Button: {button.Text}", ex.Message);
				}
			}

			TableLayoutPanel[] toolbars = Descendants(this).OfType<TableLayoutPanel>()
				.Where(panel => panel.Controls.OfType<StudioButton>().Count() >= 2)
				.ToArray();
			foreach (TableLayoutPanel toolbar in toolbars)
			{
				int[] heights = toolbar.Controls.OfType<StudioButton>().Select(button => button.Height).Distinct().ToArray();
				Record(heights.Length <= 1, "Button row uses consistent control heights");
			}

			SelectTab("Start Here");
			Record(fields.Values.All(control => control.Width > 0 && control.Height > 0), "All package fields have usable dimensions");
			Record(installerGrid.Columns.Count >= 9, "Installer grid contains the complete editing columns");
			Record(BuildFileDialogFilter([".msi", ".exe"]).Contains("*.msi;*.exe", StringComparison.Ordinal), "Windows file picker filter includes every supported installer type");
		}
		catch (Exception ex)
		{
			Record(false, "Full interface verification", ex.ToString());
		}

		report.Insert(0, $"Winget Manifest Studio UI verification: {passed} passed, {failed} failed");
		return report;
	}

	private async Task<string?> PickFolderAsync(string title, string description, string? initialPath)
	{
		if (!BeginSystemDialog()) return null;
		try
		{
			return await SystemDialogService.PickFolderAsync(title, description, initialPath);
		}
		catch (Exception ex)
		{
			ShowError("The Windows folder picker could not open", ex);
			return null;
		}
		finally
		{
			EndSystemDialog();
		}
	}

	private async Task<string[]> OpenFilesAsync(string title, string? initialPath, IEnumerable<string> extensions, bool multiSelect)
	{
		if (!BeginSystemDialog()) return [];
		try
		{
			return await SystemDialogService.OpenFilesAsync(title, initialPath, BuildFileDialogFilter(extensions), multiSelect);
		}
		catch (Exception ex)
		{
			ShowError("The Windows file picker could not open", ex);
			return [];
		}
		finally
		{
			EndSystemDialog();
		}
	}

	private async Task<string?> SaveFileAsync(string title, string? initialPath, IEnumerable<string> extensions, string initialFileName)
	{
		if (!BeginSystemDialog()) return null;
		string[] normalizedExtensions = extensions.Select(NormalizeExtension).Where(extension => extension.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
		try
		{
			return await SystemDialogService.SaveFileAsync(
				title,
				initialPath,
				BuildFileDialogFilter(normalizedExtensions),
				normalizedExtensions.FirstOrDefault()?.TrimStart('.') ?? string.Empty,
				initialFileName);
		}
		catch (Exception ex)
		{
			ShowError("The Windows save picker could not open", ex);
			return null;
		}
		finally
		{
			EndSystemDialog();
		}
	}

	private bool BeginSystemDialog()
	{
		if (systemDialogOpen)
		{
			SetStatus("A Windows file or folder picker is already open.");
			return false;
		}

		systemDialogOpen = true;
		SetStatus("Windows picker open. Choose a location or select Cancel to return.");
		return true;
	}

	private void EndSystemDialog()
	{
		systemDialogOpen = false;
		if (!IsDisposed)
		{
			Activate();
			SetStatus("Ready.");
		}
	}

	private static string BuildFileDialogFilter(IEnumerable<string> extensions)
	{
		string[] normalized = extensions.Select(NormalizeExtension).Where(extension => extension.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
		if (normalized.Length == 0) return "All files (*.*)|*.*";
		string patterns = string.Join(';', normalized.Select(extension => "*" + extension));
		return $"Supported files ({patterns})|{patterns}|All files (*.*)|*.*";
	}

	private static string NormalizeExtension(string extension)
	{
		string value = extension.Trim();
		if (value.Length == 0 || value == ".*") return string.Empty;
		return value.StartsWith('.') ? value : "." + value;
	}

	private void SetBusy(bool busy, string? message = null)
	{
		isBusy = busy;
		operationCancellation?.Dispose();
		operationCancellation = busy ? new CancellationTokenSource() : null;
		UseWaitCursor = busy;
		busyProgress.Visible = busy;
		if (!string.IsNullOrWhiteSpace(message)) SetStatus(message);
	}

	private void SelectTab(string title)
	{
		TabPage? page = workspaceTabs.TabPages.Cast<TabPage>().FirstOrDefault(candidate => string.Equals(candidate.Text, title, StringComparison.Ordinal));
		if (page is not null)
		{
			workspaceTabs.SelectedTab = page;
			UpdateNavigationState();
		}
	}

	private void SetStatus(string message) => statusLabel.Text = message;
	private string Read(string key) => fields.TryGetValue(key, out Control? control) ? control.Text.Trim() : string.Empty;
	private void Write(string key, string value) { if (fields.TryGetValue(key, out Control? control)) control.Text = value ?? string.Empty; }
	private void ShowError(string heading, Exception ex)
	{
		SetStatus(heading + ".");
		if (uiTestMode)
		{
			CrashReporter.Report(ex, "Safe UI test: " + heading);
			return;
		}
		MessageBox.Show(this, ex.Message, heading, MessageBoxButtons.OK, MessageBoxIcon.Error);
	}
	private static string SafeFileName(string value) => string.Concat(value.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '-' : ch));
	private static string QuoteArgument(string value) => "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\"";
	private static string FormatSize(long bytes) => bytes >= 1024L * 1024 * 1024 ? $"{bytes / (1024d * 1024 * 1024):0.00} GB" : bytes >= 1024L * 1024 ? $"{bytes / (1024d * 1024):0.00} MB" : $"{bytes / 1024d:0.0} KB";
}
