using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

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
	private enum ReviewProgress { Editing, Previewed, Saved, ValidationFailed, Validated }
	private readonly Dictionary<string, Control> fields = new(StringComparer.OrdinalIgnoreCase);
	private DataGridView installerGrid = null!;
	private RichTextBox previewBox = null!;
	private RichTextBox toolOutputBox = null!;
	private RichTextBox testOutputBox = null!;
	private StudioComboBox toolCommandBox = null!;
	private StudioComboBox languageBox = null!;
	private StudioTextBox toolArgumentsBox = null!;
	private StudioCheckBox insecureUrlCheck = null!;
	private Label readinessLabel = null!;
	private Button previewButton = null!;
	private Button saveButton = null!;
	private Button validateButton = null!;
	private Button testCenterButton = null!;
	private Button submitButton = null!;
	private Button previewModeButton = null!;
	private Button toolRunButton = null!;
	private readonly ErrorProvider fieldErrors = new() { BlinkStyle = ErrorBlinkStyle.NeverBlink };
	private System.ComponentModel.BindingList<InstallerArtifact>? trackedInstallers;
	private bool refreshingReadiness;
	private CancellationTokenSource? operationCancellation;
	private bool isBusy;
	private readonly Dictionary<string, StudioNavButton> navigationButtons = new(StringComparer.Ordinal);
	private bool draggingWindow;
	private Point dragCursorOrigin;
	private Point dragWindowOrigin;
	private readonly bool uiTestMode;
	private bool systemDialogOpen;
	private bool toolAvailabilityCheckStarted;
	private bool wingetCreateReady;
	private System.Windows.Forms.Timer? wingetCreateStartupTimer;
	private System.Windows.Forms.Timer? tokenStatusTimer;
	private System.Windows.Forms.Timer? recoveryTimer;
	private string latestTestReport = "No test report has been generated yet.";
	private readonly Dictionary<Control, string> originalInterfaceText = new(ReferenceEqualityComparer.Instance);
	private bool applyingLanguage;
	private string successfulPreflightFingerprint = string.Empty;
	private ReviewProgress reviewProgress;
	private string reviewFingerprint = string.Empty;
	private string simplePreviewText = string.Empty;
	private string technicalPreviewText = string.Empty;
	private bool showingTechnicalPreview;

	public MainForm() : this(false)
	{
	}

	internal MainForm(bool uiTestMode)
	{
		this.uiTestMode = uiTestMode;
		InitializeComponent();
		fieldErrors.ContainerControl = this;
		if (uiTestMode)
		{
			ShowInTaskbar = false;
			Opacity = 0.01;
			StartPosition = FormStartPosition.Manual;
			Location = new Point(-32000, -32000);
		}
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
			WireReadinessTracking();
			workspaceTabs.ResumeLayout(true);
			ResumeLayout(true);
			Shown += MainForm_Shown;
		}
	}

	private void MainForm_Shown(object? sender, EventArgs e)
	{
		try
		{
			ApplyProjectToControls();
			if (!uiTestMode) ApplyInterfaceLanguage(StudioStateStore.GetLanguage());
			SetStatus("Ready. Start a new package or explicitly load a manifest folder.");
			if (uiTestMode)
			{
				modeLabel.Text = "SAFE UI TEST MODE";
				return;
			}

			modeLabel.Text = "LOCAL AUTHORING READY • WINGETCREATE STARTING SHORTLY";
			toolLoadingProgress.Visible = false;
			SetStatus("Manifest Studio is ready. WingetCreate official tools will load shortly in the background.");
			ScheduleToolAvailabilityCheck();
		}
		catch (Exception ex)
		{
			ShowError("Startup could not finish", ex);
		}
	}

	private void ScheduleToolAvailabilityCheck()
	{
		wingetCreateStartupTimer?.Stop();
		wingetCreateStartupTimer?.Dispose();
		wingetCreateStartupTimer = new System.Windows.Forms.Timer { Interval = 3000 };
		wingetCreateStartupTimer.Tick += (_, _) =>
		{
			wingetCreateStartupTimer?.Stop();
			wingetCreateStartupTimer?.Dispose();
			wingetCreateStartupTimer = null;
			if (IsDisposed || Disposing) return;
			modeLabel.Text = "LOCAL AUTHORING READY • LOADING WINGETCREATE";
			toolLoadingProgress.Visible = true;
			SetStatus("Local manifest tools are ready. Preparing WingetCreate in the background...");
			StartToolAvailabilityCheck();
		};
		wingetCreateStartupTimer.Start();
	}

	protected override void OnFormClosed(FormClosedEventArgs eventArgs)
	{
		if (!uiTestMode)
		{
			try { ReadProjectFromControls(); StudioStateStore.SaveRecovery(project); } catch { }
		}
		wingetCreateStartupTimer?.Stop();
		wingetCreateStartupTimer?.Dispose();
		wingetCreateStartupTimer = null;
		tokenStatusTimer?.Stop();
		tokenStatusTimer?.Dispose();
		tokenStatusTimer = null;
		recoveryTimer?.Stop();
		recoveryTimer?.Dispose();
		recoveryTimer = null;
		fieldErrors.Dispose();
		base.OnFormClosed(eventArgs);
	}

	private void StartTokenStatusMonitor()
	{
		if (uiTestMode || tokenStatusTimer != null || IsDisposed || Disposing) return;

		tokenStatusTimer = new System.Windows.Forms.Timer { Interval = 1000 };
		tokenStatusTimer.Tick += (_, _) => RefreshTokenStatus();
		tokenStatusTimer.Start();
	}

	private void RefreshTokenStatus()
	{
		if (!wingetCreateReady || IsDisposed || Disposing) return;

		bool tokenStored = WingetCommandService.IsGitHubTokenStored();
		modeLabel.Text = tokenStored
			? "WINGETCREATE READY • TOKEN STORED"
			: "WINGETCREATE READY • NO TOKEN STORED";
		securityBadge.Text = tokenStored
			? "LOCAL-FIRST • TOKEN STORED"
			: "LOCAL-FIRST • NO TOKEN STORED";
	}

	private void StartToolAvailabilityCheck()
	{
		if (toolAvailabilityCheckStarted || IsDisposed || Disposing) return;
		toolAvailabilityCheckStarted = true;
		_ = UpdateToolAvailabilityAsync();
	}

	private async Task UpdateToolAvailabilityAsync()
	{
		try
		{
			bool available = await Task.Run(
				() => WingetCommandService.IsAvailableAsync("wingetcreate.exe", TimeSpan.FromSeconds(3)));
			if (IsDisposed || Disposing) return;
			if (!available)
			{
				wingetCreateReady = false;
				modeLabel.Text = "LOCAL AUTHORING READY • WINGETCREATE OPTIONAL";
				SetStatus("Local manifest tools are ready. Install WingetCreate only when you need the official command tools.");
				return;
			}

			modeLabel.Text = "LOCAL AUTHORING READY • PREPARING WINGETCREATE";
			SetStatus("Local manifest tools are ready. Preparing WingetCreate in the background...");
			bool warmed = await Task.Run(
				() => WingetCommandService.WarmUpAsync(TimeSpan.FromSeconds(20)));
			if (IsDisposed || Disposing) return;
			wingetCreateReady = true;
			RefreshTokenStatus();
			StartTokenStatusMonitor();
			SetStatus(warmed
				? "WingetCreate is ready. All manifest and official command tools are available."
				: "WingetCreate is installed and available. Its first official command may need a little extra time.");
		}
		catch
		{
			if (IsDisposed || Disposing) return;
			wingetCreateReady = false;
			modeLabel.Text = "LOCAL AUTHORING READY • WINGETCREATE OPTIONAL";
			SetStatus("Local manifest tools are ready. WingetCreate could not be prepared in the background.");
		}
		finally
		{
			if (!IsDisposed && !Disposing)
			{
				toolLoadingProgress.Visible = false;
				toolRunButton.Enabled = wingetCreateReady;
			}
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
		TabPage[] pages = [BuildStartTab(), BuildProjectTab(), BuildInstallersTab(), BuildPreviewTab(), BuildTestTab(), BuildHelpTab(), BuildToolsTab()];
		Dictionary<string, string> navigationLabels = new(StringComparer.Ordinal)
		{
			["Start Here"] = "1  Start",
			["Package Details"] = "2  Package",
			["Installers & Hashes"] = "3  Installers",
			["Preview & Submit"] = "4  Review",
			["Test Center"] = "5  Test Center",
			["Help & Guide"] = "Help",
			["Official Tool Commands"] = "Official Tools"
		};
		workspaceTabs.TabPages.AddRange(pages);
		navigationPanel.Controls.Clear();
		navigationPanel.ColumnStyles.Clear();
		navigationPanel.ColumnCount = pages.Length;
		foreach (TabPage page in pages)
		{
			navigationPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / pages.Length));
			StudioNavButton button = new() { Text = navigationLabels.GetValueOrDefault(page.Text, page.Text), Tag = page, AccessibleName = page.Text + " page" };
			button.Click += (_, _) => workspaceTabs.SelectedTab = page;
			navigationButtons[page.Text] = button;
			navigationPanel.Controls.Add(button, navigationPanel.Controls.Count, 0);
		}
		workspaceTabs.SelectedIndexChanged += (_, _) => UpdateNavigationState();
		UpdateNavigationState();
	}

	private void UpdateNavigationState()
	{
		foreach (StudioNavButton button in navigationButtons.Values)
			button.Selected = ReferenceEquals(workspaceTabs.SelectedTab, button.Tag as TabPage);
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
		content.Controls.Add(CreateWorkflowCard("↻", "Continue where you left off", "Restore the last project you edited, including unsaved field values and attached local installer paths. Recovery information stays on this computer and never includes a GitHub token.",
			("Restore Last Session", (_, _) => RestoreLastSession()),
			("Open Recent Project", (sender, _) => ShowRecentProjects(sender as Control))));
		content.Controls.Add(CreateWorkflowCard("1", "Choose what you are doing", "Load an existing manifest folder to update a package, or start a new project and choose an empty output folder.",
			("Load existing manifests", async (_, _) => await LoadManifestsAsync()),
			("Create a new project", (_, _) => { NewProject(); SelectTab("Package Details"); })));
		content.Controls.Add(CreateWorkflowCard("2", "Add the release installers", "Choose the local MSI, EXE, MSIX, Appx, or ZIP files that you will upload. The Studio reads those exact files and calculates their SHA-256 hashes. Then enter the public download URL for each file.",
			("Open Installers & Hashes", (_, _) => SelectTab("Installers & Hashes"))));
		content.Controls.Add(CreateWorkflowCard("3", "Review before anything is changed", "Preview builds all three manifests in memory. Save writes them only after validation and keeps timestamped backups of files that already exist.",
			("Open Preview & Submit", (_, _) => SelectTab("Preview & Submit"))));
		content.Controls.Add(CreateWorkflowCard("4", "Validate, test-install, and submit", "Run the safe preflight checks, test the generated manifest locally or in Windows Sandbox, verify the installed result, and only then use WingetCreate to open the pull request.",
			("Open Test Center", (_, _) => SelectTab("Test Center")),
			("Open Official Tools", (_, _) => SelectTab("Official Tool Commands"))));
		content.Controls.Add(CreateWorkflowCard("?", "Need help?", "Open the built-in beginner guide for field meanings, installer IDs, hashes, validation, and submission.",
			("Open Help & Guide", (_, _) => SelectTab("Help & Guide"))));
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
			("Choose Output", async (_, _) => await ChooseOutputFolderAsync()),
			("Suggest Package ID", (_, _) => SuggestPackageIdentifier())), 0, 1);

		FlowLayoutPanel content = NewScrollFlow();
		content.Controls.Add(CreateSection("PACKAGE IDENTITY", "The values shared by every manifest file.",
			Field("PackageIdentifier", "Package identifier", "Required format: Publisher.Application (example: ubidzz.WingetManifestStudio)"),
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
			Field("PrivacyUrl", "Privacy URL"),
			Field("PackageUrl", "Package URL"),
			Field("LicenseUrl", "License URL"),
			Field("Copyright", "Copyright"),
			Field("CopyrightUrl", "Copyright URL"),
			Field("PurchaseUrl", "Purchase URL"),
			Field("ReleaseNotesUrl", "Release notes URL"),
			Field("ReleaseNotes", "Release notes", multiline: true),
			Field("InstallationNotes", "Installation notes", "Shown to the user after installation", multiline: true)));
		content.Controls.Add(CreateSection("INSTALLER BEHAVIOR", "Optional current Winget schema fields. Leave a field blank when it does not apply.",
			Field("Channel", "Channel", "Example: stable or beta"),
			Field("InstallerLocale", "Installer locale", "Example: en-US"),
			Field("Platform", "Platforms", "Comma-separated; usually Windows.Desktop"),
			Field("MinimumOSVersion", "Minimum Windows version", "Example: 10.0.19041.0"),
			Field("NestedInstallerType", "Nested installer type", "Required for ZIP installers"),
			Field("Protocols", "Protocols", "Comma-separated URL protocols"),
			Field("FileExtensions", "File extensions", "Comma-separated, without dots"),
			Field("UnsupportedOSArchitectures", "Unsupported architectures", "Comma-separated"),
			Field("InstallerSuccessCodes", "Extra success codes", "Comma-separated whole numbers"),
			Field("PackageFamilyName", "Package family name"),
			Field("ReleaseDate", "Release date", "YYYY-MM-DD"),
			Field("RepairBehavior", "Repair behavior", "modify, uninstaller, or installer"),
			Field("InstallerAbortsTerminal", "Installer aborts terminal", "true, false, or blank"),
			Field("InstallLocationRequired", "Install location required", "true, false, or blank"),
			Field("RequireExplicitUpgrade", "Require explicit upgrade", "true, false, or blank"),
			Field("DisplayInstallWarnings", "Display install warnings", "true, false, or blank"),
			Field("DownloadCommandProhibited", "Prohibit download command", "true, false, or blank"),
			Field("ArchiveBinariesDependOnPath", "Archive binaries depend on PATH", "true, false, or blank")));
		content.Controls.Add(CreateSection("INSTALLER SWITCHES", "Winget uses these command-line switches for installer actions. Known Inno, Nullsoft, MSI, and MSIX types often need no custom values.",
			Field("SwitchSilent", "Silent switch"),
			Field("SwitchSilentWithProgress", "Silent with progress"),
			Field("SwitchInteractive", "Interactive switch"),
			Field("SwitchInstallLocation", "Install-location switch"),
			Field("SwitchLog", "Log switch"),
			Field("SwitchUpgrade", "Upgrade switch"),
			Field("CustomInstallerSwitch", "Custom switch"),
			Field("SwitchRepair", "Repair switch")));
		content.Controls.Add(CreateSection("ALL OTHER SCHEMA FIELDS", "For uncommon nested fields such as dependencies, agreements, documentation, icons, markets, expected return codes, nested files, and installation metadata. These optional boxes accept a YAML Field: value mapping and are checked before previewing.",
			Field("AdvancedLocaleFieldsYaml", "Additional locale fields", "Optional advanced YAML mapping", multiline: true, width: 520),
			Field("AdvancedInstallerFieldsYaml", "Additional installer fields", "Optional advanced YAML mapping", multiline: true, width: 520)));
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
		root.Controls.Add(CreateInfoStrip("INSTALLER DETAILS ARE AUTOMATIC", "Add or attach a local installer and the Studio calculates its SHA-256, detects its type and architecture, and reads ProductCode and UpgradeCode directly from MSI files. For EXE, Inno, and Nullsoft installers these fields are optional and are left blank when the installer does not provide them."), 0, 0);
		root.Controls.Add(CreateToolbar(
			("Add Release Files", async (_, _) => await AddInstallerFilesAsync()),
			("Add URL-Only Row", (_, _) => AddUrlInstaller()),
			("Attach File to Selected", async (_, _) => await AttachFileToSelectedAsync()),
			("Inspect & Fill Details", async (_, _) => await InspectSelectedAsync()),
			("Inspect Local Files", async (_, _) => await InspectAllLocalAsync()),
			("Verify Public URLs", async (_, _) => await VerifyPublicUrlsAsync()),
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
		root.RowCount = 5;
		root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		root.Controls.Add(CreateInfoStrip("WHAT DO I DO NEXT?", "Follow the highlighted next step. The Studio will say NOTHING NEEDS FIXING when the required information is ready, or list exactly what must be corrected and where to find it."), 0, 0);
		root.Controls.Add(CreateReadinessPanel(), 0, 1);
		TableLayoutPanel actions = (TableLayoutPanel)CreateToolbar(
			("Preview Changes", (_, _) => GeneratePreview()),
			("Save Manifests", (_, _) => SaveManifests()),
			("Validate Locally", async (_, _) => await ValidateWithWingetAsync()),
			("Open Test Center", (_, _) => SelectTab("Test Center")),
			("Submit to Winget", async (_, _) => await SubmitAsync()));
		previewButton = actions.Controls.OfType<Button>().First(button => button.Text == "Preview Changes");
		saveButton = actions.Controls.OfType<Button>().First(button => button.Text == "Save Manifests");
		validateButton = actions.Controls.OfType<Button>().First(button => button.Text == "Validate Locally");
		testCenterButton = actions.Controls.OfType<Button>().First(button => button.Text == "Open Test Center");
		submitButton = actions.Controls.OfType<Button>().First(button => button.Text == "Submit to Winget");
		root.Controls.Add(actions, 0, 2);
		TableLayoutPanel supportingActions = (TableLayoutPanel)CreateToolbar(
			("Open Output Folder", (_, _) => OpenOutputFolder()),
			("Technical Details", (_, _) => TogglePreviewMode()));
		previewModeButton = supportingActions.Controls.OfType<Button>().First(button => button.Text == "Technical Details");
		previewModeButton.Enabled = false;
		root.Controls.Add(supportingActions, 0, 3);
		previewBox = NewRichTextBox();
		previewBox.ReadOnly = true;
		previewBox.Font = new Font("Cascadia Mono", 9.5F);
		previewBox.Text = "WHAT NEEDS ATTENTION\r\n\r\nComplete the Package and Installers pages. This page will then tell you the next step.";
		root.Controls.Add(previewBox, 0, 4);
		page.Controls.Add(root);
		return page;
	}

	private TabPage BuildHelpTab()
	{
		TabPage page = NewPage("Help & Guide");
		FlowLayoutPanel content = NewScrollFlow();
		content.Padding = new Padding(18, 20, 18, 30);
		FlowLayoutPanel languageRow = CreateInlinePanel();
		languageRow.Controls.Add(NewInlineLabel("Language"));
		languageBox = NewComboBox(180);
		languageBox.SetItems(["English", "Español"]);
		languageBox.SelectedIndex = 0;
		languageBox.SelectedIndexChanged += (_, _) => ChangeLanguage();
		languageRow.Controls.Add(languageBox);
		content.Controls.Add(languageRow);
		content.Controls.Add(CreateInfoStrip("HOW TO USE THIS SOFTWARE", "This guide explains every screen and the information Winget needs. You can read it at any time; the buttons only take you to the screen being described."));
		content.Controls.Add(CreateWorkflowCard("1", "Start or open a manifest project", "For a first release, choose New Project and select the folder where the three YAML files will be saved. For an update, choose Load Manifests and select the folder containing the existing version, installer, and locale YAML files. Loading never changes them.",
			("Go to Package Details", (_, _) => SelectTab("Package Details"))));
		content.Controls.Add(CreateWorkflowCard("2", "Enter the package identity", "Package Identifier is the permanent Winget name, normally Publisher.Application. Enter Publisher and Package Name first, then use Suggest Package ID if you want help. Package Version has no leading v. Keep the identifier unchanged for updates.",
			("Edit Package Identity", (_, _) => SelectTab("Package Details"))));
		content.Controls.Add(CreateWorkflowCard("3", "Complete the public package information", "Package Name, Publisher, License, and Short Description are required. Add the official website, support and license links when available. Description, tags, release notes, and moniker help people understand and find the application.",
			("Edit Package Information", (_, _) => SelectTab("Package Details"))));
		content.Controls.Add(CreateInfoStrip("INSTALLER FILES AND DOWNLOAD LINKS", "Winget downloads from a public URL, but the Studio uses your matching local release file to calculate the trusted SHA-256 value."));
		content.Controls.Add(CreateWorkflowCard("4", "Add the exact release file", "Choose Add Release Files for every architecture you publish. Select the same MSI, EXE, MSIX, APPX, bundle, or ZIP file that will be uploaded to your release. Use one row for each architecture or installer variation.",
			("Open Installers & Hashes", (_, _) => SelectTab("Installers & Hashes"))));
		content.Controls.Add(CreateWorkflowCard("5", "Enter its public HTTPS URL", "Paste the direct download URL for each installer—not a web page containing a download button. The URL must remain public and must download the exact local file in that row. GitHub release asset URLs are suitable.",
			("Enter Download URLs", (_, _) => SelectTab("Installers & Hashes"))));
		content.Controls.Add(CreateWorkflowCard("6", "Inspect and verify the published installer", "Inspect & Fill Details calculates the local SHA-256, type, architecture, and MSI IDs. After uploading the release, Verify Public URLs downloads it temporarily and proves the public file matches that SHA-256.",
			("Inspect Installer Files", (_, _) => SelectTab("Installers & Hashes"))));
		content.Controls.Add(CreateInfoStrip("REVIEW, SAVE, AND PUBLISH", "The preview is your safety check. It creates the proposed YAML in memory without writing to the selected folder."));
		content.Controls.Add(CreateWorkflowCard("7", "Follow Project Readiness, then preview", "The readiness panel counts anything still required and marks problem fields. When it says READY, choose Preview Changes and review the identifier, old and new versions, URLs, architectures, installer types, hashes, and filenames.",
			("Review the Preview", (_, _) => SelectTab("Preview & Submit"))));
		content.Controls.Add(CreateWorkflowCard("8", "Save with recoverable backups", "Choose Save Manifests only after the preview is correct. New files are created in the output folder. Existing files are copied into a timestamped .manifest-backups folder before they are replaced.",
			("Save or Validate", (_, _) => SelectTab("Preview & Submit"))));
		content.Controls.Add(CreateWorkflowCard("9", "Validate before submission", "Validate Locally runs the official Winget validator against a clean temporary copy. If it reports an error, fix the related field and validate again. Validation does not modify the saved manifests.",
			("Open Validation", (_, _) => SelectTab("Preview & Submit"))));
		content.Controls.Add(CreateWorkflowCard("10", "Run the complete safe preflight", "The Test Center rechecks attached file hashes and Authenticode signatures, runs official validation, and searches Winget plus microsoft/winget-pkgs for the exact package identifier. Export the report if you need to keep evidence of the checks.",
			("Open Test Center", (_, _) => SelectTab("Test Center"))));
		content.Controls.Add(CreateWorkflowCard("11", "Test the real installation", "Enable Local Testing once, then Test Install Here runs winget install --manifest in a persistent console. It can change this computer. Verify Installed Result checks that Windows reports the expected package identifier and version.",
			("Open Installation Tests", (_, _) => SelectTab("Test Center"))));
		content.Controls.Add(CreateWorkflowCard("12", "Use Windows Sandbox when available", "Test in Windows Sandbox downloads Microsoft's official SandboxTest.ps1 and installs the generated manifests inside a disposable environment. The first run can take several minutes while Microsoft dependencies are prepared.",
			("Open Sandbox Test", (_, _) => SelectTab("Test Center"))));
		content.Controls.Add(CreateWorkflowCard("13", "Submit with Microsoft's WingetCreate", "Install WingetCreate from Official Tool Commands if needed. Submit opens a separate console for sign-in and creates the pull request. The GitHub token is handled by WingetCreate and Windows Credential Manager; it is never saved in a Studio profile.",
			("Open Official Tools", (_, _) => SelectTab("Official Tool Commands"))));
		content.Controls.Add(CreateInfoStrip("COMMON PROBLEMS", "Do not use a leading v in the version, a release web-page URL instead of the direct asset URL, a hash from a different file, or the wrong architecture. Reattach and inspect the exact published file whenever it changes."));
		page.Controls.Add(content);
		return page;
	}

	private TabPage BuildTestTab()
	{
		TabPage page = NewPage("Test Center");
		TableLayoutPanel root = NewRoot();
		root.RowCount = 5;
		root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		root.Controls.Add(CreateInfoStrip("TEST BEFORE SUBMITTING", "Safe Preflight checks YAML, hashes, signatures, official Winget validation, and whether the package already exists. Install tests are separate because they can change the computer."), 0, 0);
		root.Controls.Add(CreateToolbar(
			("Run Safe Preflight", async (_, _) => await RunSafePreflightAsync()),
			("Inspect Signatures", async (_, _) => await InspectSignaturesAsync()),
			("Find Existing Package", async (_, _) => await FindExistingPackageAsync()),
			("Export Test Report", async (_, _) => await ExportTestReportAsync())), 0, 1);
		root.Controls.Add(CreateInfoStrip("INSTALLATION TESTS REQUIRE YOUR CONFIRMATION", "Test Install Here runs winget install --manifest and may install or elevate software on this PC. Test in Sandbox downloads and runs Microsoft's official SandboxTest.ps1 in a disposable Windows Sandbox."), 0, 2);
		root.Controls.Add(CreateToolbar(
			("Enable Local Testing", (_, _) => EnableLocalManifestTesting()),
			("Test Install Here", async (_, _) => await TestInstallHereAsync()),
			("Verify Installed Result", async (_, _) => await VerifyInstalledResultAsync()),
			("Test in Windows Sandbox", async (_, _) => await TestInSandboxAsync())), 0, 3);
		testOutputBox = NewRichTextBox();
		testOutputBox.ReadOnly = true;
		testOutputBox.DetectUrls = true;
		testOutputBox.Font = new Font("Cascadia Mono", 9F);
		testOutputBox.Text = "Run Safe Preflight when the package details and installer rows are ready. No installer is launched by the safe checks.";
		testOutputBox.LinkClicked += (_, eventArgs) =>
		{
			if (!uiTestMode && Uri.TryCreate(eventArgs.LinkText, UriKind.Absolute, out Uri? uri))
				Process.Start(new ProcessStartInfo { FileName = uri.AbsoluteUri, UseShellExecute = true });
		};
		root.Controls.Add(testOutputBox, 0, 4);
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
			"Full WingetCreate access for New, Update, New-Locale, Update-Locale, Submit, Show, Token, Settings, Cache, Info, and DSC. Commands run directly without cmd.exe. Commands that ask questions open a real WingetCreate console so you can answer them.", 950), 0, 0);

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
		string suggestedArguments = string.Empty;
		toolCommandBox.SelectedIndexChanged += (_, _) =>
		{
			if (!string.IsNullOrWhiteSpace(toolArgumentsBox.Text)
				&& !string.Equals(toolArgumentsBox.Text, suggestedArguments, StringComparison.Ordinal)) return;
			suggestedArguments = string.Equals(toolCommandBox.Text, "token", StringComparison.OrdinalIgnoreCase)
				? "--store"
				: string.Empty;
			toolArgumentsBox.Text = suggestedArguments;
		};
		toolRunButton = CreateButton("Run", async (_, _) => await RunOfficialCommandAsync(), true);
		toolRunButton.Enabled = uiTestMode;
		argsRow.Controls.Add(toolRunButton);
		root.Controls.Add(argsRow, 0, 2);

		toolOutputBox = NewRichTextBox();
		toolOutputBox.ReadOnly = true;
		toolOutputBox.DetectUrls = true;
		toolOutputBox.LinkClicked += (_, eventArgs) =>
		{
			if (!uiTestMode && Uri.TryCreate(eventArgs.LinkText, UriKind.Absolute, out Uri? uri))
				Process.Start(new ProcessStartInfo { FileName = uri.AbsoluteUri, UseShellExecute = true });
		};
		toolOutputBox.Font = new Font("Cascadia Mono", 9F);
		toolOutputBox.Text = "Official command output appears here. Question-based commands open a separate WingetCreate console. GitHub tokens are managed by WingetCreate, not saved in this application.";
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

	private void RestoreLastSession()
	{
		if (uiTestMode) { SetStatus("TEST: Last-session recovery opened safely."); return; }
		ManifestProject? recovered = StudioStateStore.LoadRecovery();
		if (recovered is null)
		{
			MessageBox.Show(this, "There is no recoverable session on this computer yet.", "Restore last session", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}
		project = recovered;
		ApplyProjectToControls();
		SelectTab("Package Details");
		SetStatus("The last editing session was restored. Review installer paths before saving.");
	}

	private void ShowRecentProjects(Control? anchor)
	{
		if (uiTestMode) { SetStatus("TEST: Recent projects opened safely."); return; }
		IReadOnlyList<string> folders = StudioStateStore.GetRecentFolders();
		if (folders.Count == 0 || anchor is null)
		{
			MessageBox.Show(this, "No recent manifest folders are available yet.", "Recent projects", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}
		ContextMenuStrip menu = new() { ShowImageMargin = false, BackColor = CardColor, ForeColor = Color.White, Renderer = new StudioMenuRenderer() };
		foreach (string folder in folders)
		{
			ToolStripMenuItem item = new(folder) { ToolTipText = folder };
			item.Click += async (_, _) => await LoadManifestFolderAsync(folder);
			menu.Items.Add(item);
		}
		menu.Closed += (_, _) => menu.Dispose();
		menu.Show(anchor, new Point(0, anchor.Height + 2));
	}

	private void SuggestPackageIdentifier()
	{
		string publisher = CleanIdentifierPart(Read("Publisher").IfEmpty(Read("Author")));
		string packageName = CleanIdentifierPart(Read("PackageName"));
		if (uiTestMode && (publisher.Length == 0 || packageName.Length == 0)) { publisher = "Contoso"; packageName = "Sample"; }
		if (publisher.Length == 0 || packageName.Length == 0)
		{
			SetStatus("Enter Publisher and Package Name first, then choose Suggest Package ID.");
			return;
		}
		string suggestion = $"{publisher}.{packageName}";
		if (!uiTestMode && !string.IsNullOrWhiteSpace(Read("PackageIdentifier")) &&
			MessageBox.Show(this, $"Replace the current package identifier with {suggestion}?", "Suggest package identifier", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
			return;
		Write("PackageIdentifier", suggestion);
		SetStatus($"Suggested package identifier: {suggestion}. Keep it unchanged for future releases.");
	}

	private static string CleanIdentifierPart(string value)
	{
		string cleaned = new(value.Where(character => char.IsLetterOrDigit(character) || character == '-').ToArray());
		return cleaned.Length > 32 ? cleaned[..32] : cleaned;
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
		await LoadManifestFolderAsync(selectedPath);
	}

	private async Task LoadManifestFolderAsync(string selectedPath)
	{
		try
		{
			SetBusy(true, "Reading manifest files...");
			ManifestProject loadedProject = await Task.Run(() => ManifestService.LoadProject(selectedPath), operationCancellation!.Token);
			project = loadedProject;
			ApplyProjectToControls();
			StudioStateStore.AddRecentFolder(selectedPath);
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

	private async Task VerifyPublicUrlsAsync()
	{
		if (isBusy) return;
		if (uiTestMode)
		{
			foreach (InstallerArtifact item in project.Installers) item.VerificationStatus = "Public URL verified • safe UI test";
			SetStatus("TEST: Public URL verification completed without network access.");
			return;
		}
		InstallerArtifact[] items = project.Installers.Where(item => Uri.TryCreate(item.InstallerUrl, UriKind.Absolute, out _)).ToArray();
		if (items.Length == 0)
		{
			MessageBox.Show(this, "Enter at least one public installer URL first.", "Verify public URLs", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}
		DialogResult answer = MessageBox.Show(this,
			$"The Studio will download {items.Length} public installer file(s) temporarily and compare their SHA-256 values. Continue?",
			"Verify published installer files", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
		if (answer != DialogResult.Yes) return;

		int matched = 0;
		try
		{
			SetBusy(true, "Verifying public installer URLs...");
			foreach (InstallerArtifact item in items)
			{
				Progress<string> progress = new(SetStatus);
				InstallerInspection remote = await InstallerInspector.InspectAsync(string.Empty, item.InstallerUrl, progress, operationCancellation!.Token);
				if (string.IsNullOrWhiteSpace(item.Sha256))
				{
					item.Sha256 = remote.Sha256;
					item.VerificationStatus = "Public URL verified • SHA-256 calculated from download";
					matched++;
				}
				else if (string.Equals(item.Sha256, remote.Sha256, StringComparison.OrdinalIgnoreCase))
				{
					item.VerificationStatus = "Public URL verified • remote file matches SHA-256";
					matched++;
				}
				else item.VerificationStatus = "FAILED • public download does not match the attached local file";
			}
			SetStatus(matched == items.Length
				? $"Verified {matched} public installer URL(s). Every downloaded file matches its SHA-256."
				: $"Verified {matched} of {items.Length} public installer URLs. Fix rows marked FAILED before submission.");
		}
		catch (OperationCanceledException) { SetStatus("Public URL verification was cancelled."); }
		catch (Exception ex) { ShowError("A public installer could not be verified", ex); }
		finally { SetBusy(false); installerGrid.Refresh(); }
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
			item.SignatureSha256 = result.SignatureSha256;
			ApplySignature(item, result.Signature);
			if (!string.IsNullOrWhiteSpace(result.SignatureSha256) && !result.Signature.IsSigned)
				item.SignatureStatus = "MSIX/APPX package signature present";
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
			simplePreviewText = "PREVIEW READY — NOTHING HAS BEEN SAVED\r\n\r\nWHAT NEEDS ATTENTION\r\n[OK] No required information is missing.\r\n\r\nNEXT: Click Save Manifests.";
			technicalPreviewText = "SAFE UI TEST TECHNICAL YAML\r\nNo manifests were generated or changed.";
			SetReviewProgress(ReviewProgress.Previewed);
			ShowSimplePreview();
			SelectTab("Preview & Submit");
			SetStatus("TEST: Preview generated safely in memory.");
			return;
		}
		try
		{
			ReadProjectFromControls();
			ManifestGenerationResult result = ManifestService.Generate(project);
			simplePreviewText = BuildSimpleReview(result, ReviewProgress.Previewed);
			StringBuilder preview = new("TECHNICAL YAML PREVIEW — ADVANCED\r\nThe simple review is available with the Show Simple Review button.\r\n");
			foreach ((string name, string content) in result.Files)
			{
				preview.AppendLine().AppendLine(new string('═', 90)).AppendLine(name).AppendLine(new string('─', 90)).AppendLine(content);
			}
			technicalPreviewText = preview.ToString();
			SetReviewProgress(ReviewProgress.Previewed);
			ShowSimplePreview();
			SelectTab("Preview & Submit");
			SetStatus($"Preview ready for {result.Files.Count} manifest files. Nothing was saved. Review the summary, then choose Save Manifests.");
		}
		catch (Exception ex) { ShowError("The project is not ready to preview", ex); }
	}

	private void SetReviewProgress(ReviewProgress progress)
	{
		if (fields.Count > 0) ReadProjectFromControls();
		reviewProgress = progress;
		reviewFingerprint = progress == ReviewProgress.Editing ? string.Empty : ProjectFingerprint();
		RefreshReadiness();
	}

	private string BuildSimpleReview(ManifestGenerationResult result, ReviewProgress progress)
	{
		StringBuilder summary = new();
		summary.AppendLine(progress == ReviewProgress.Saved
			? "SAVED SAFELY — READY FOR VALIDATION"
			: "PREVIEW READY — NOTHING HAS BEEN SAVED");
		summary.AppendLine();
		summary.AppendLine("WHAT NEEDS ATTENTION");
		summary.AppendLine("[OK] Nothing is missing from the required fields.");
		if (result.Warnings.Count == 0) summary.AppendLine("[OK] The Studio found no preview warnings.");
		else foreach (string warning in result.Warnings) summary.AppendLine("[CHECK] " + warning);

		summary.AppendLine().AppendLine("CONFIRM THESE IMPORTANT VALUES");
		summary.AppendLine($"Package ID:       {project.PackageIdentifier}");
		summary.AppendLine($"Release version:  {project.PackageVersion}");
		summary.AppendLine($"Language:         {project.DefaultLocale}");
		summary.AppendLine($"YAML files:       {result.Files.Count}");

		summary.AppendLine().AppendLine("WHAT WILL CHANGE");
		foreach (string change in result.Changes) summary.AppendLine("• " + SimplifyPlannedChange(change));

		summary.AppendLine().AppendLine("INSTALLERS TO CHECK");
		for (int index = 0; index < project.Installers.Count; index++)
		{
			InstallerArtifact installer = project.Installers[index];
			summary.AppendLine($"Installer {index + 1}: {installer.Architecture.IfEmpty("architecture missing")} {installer.InstallerType.IfEmpty(project.InstallerType).ToUpperInvariant()}");
			summary.AppendLine($"  Release file: {Path.GetFileName(installer.LocalFile).IfEmpty("URL-only installer — no local hash comparison")}");
			summary.AppendLine($"  Public URL:   {installer.InstallerUrl}");
			summary.AppendLine($"  SHA-256:      {(installer.Sha256.Length == 64 ? "Present (64 characters)" : "MISSING OR INVALID")}");
		}

		summary.AppendLine().AppendLine("NEXT STEP");
		if (progress == ReviewProgress.Saved)
			summary.AppendLine("Click Validate Locally. Winget will check the manifests without installing the package.");
		else
			summary.AppendLine("If the package ID, version, release file, and public URL above are correct, click Save Manifests. Any file being replaced will be backed up first.");
		summary.AppendLine().Append("Technical YAML is hidden. Use Technical Details only when you want to inspect it.");
		return summary.ToString();
	}

	private static string SimplifyPlannedChange(string change) => change
		.Replace("with structural YAML preservation", "while keeping existing custom fields", StringComparison.OrdinalIgnoreCase)
		.Replace("while preserving unknown fields", "while keeping existing custom fields", StringComparison.OrdinalIgnoreCase)
		.Replace("and match installer fields by identity", "while keeping existing installer details", StringComparison.OrdinalIgnoreCase);

	private static string BuildFixList(IReadOnlyList<string> errors)
	{
		if (errors.Count == 0) return string.Empty;
		StringBuilder message = new("WHAT NEEDS ATTENTION\r\n\r\n");
		for (int index = 0; index < errors.Count; index++) message.AppendLine($"{index + 1}. {errors[index]}");
		bool hasInstallerProblem = errors.Any(error => error.StartsWith("Installer", StringComparison.OrdinalIgnoreCase) || error.Contains("Installer Type", StringComparison.OrdinalIgnoreCase));
		bool hasPackageProblem = errors.Any(error => !error.StartsWith("Installer", StringComparison.OrdinalIgnoreCase));
		message.AppendLine().AppendLine("WHERE TO FIX IT");
		if (hasPackageProblem) message.AppendLine("• Open 2 Package for package ID, version, name, publisher, license, descriptions, or output-folder problems.");
		if (hasInstallerProblem) message.AppendLine("• Open 3 Installers for release file, public URL, architecture, type, or SHA-256 problems.");
		message.AppendLine().Append("After fixing the listed items, return here and click Preview Changes.");
		return message.ToString();
	}

	private static string BuildValidationFailureSummary(string output)
	{
		HashSet<string> fields = new(StringComparer.OrdinalIgnoreCase);
		foreach (string line in (output ?? string.Empty).Replace("\r\n", "\n").Split('\n'))
		{
			const string propertyMarker = "property name '";
			int marker = line.IndexOf(propertyMarker, StringComparison.OrdinalIgnoreCase);
			if (marker >= 0)
			{
				int start = marker + propertyMarker.Length;
				int end = line.IndexOf('\'', start);
				if (end > start) AddValidationField(fields, line.Substring(start, end - start));
			}

			int searchFrom = 0;
			while (searchFrom < line.Length)
			{
				int open = line.IndexOf('[', searchFrom);
				if (open < 0) break;
				int close = line.IndexOf(']', open + 1);
				if (close < 0) break;
				AddValidationField(fields, line.Substring(open + 1, close - open - 1));
				searchFrom = close + 1;
			}
		}

		StringBuilder message = new("WINGET FOUND A PROBLEM — DO NOT SUBMIT YET\r\n\r\nWHAT NEEDS ATTENTION\r\n");
		if (fields.Count == 0)
			message.AppendLine("Winget did not identify one simple field name. Open Technical Details and read the first Manifest Error.");
		else
		{
			int number = 1;
			foreach (string field in fields) message.AppendLine($"{number++}. {FriendlyValidationField(field)}");
		}
		message.AppendLine().AppendLine("NEXT STEP");
		message.AppendLine("Correct the listed field on 2 Package or 3 Installers, then repeat Preview Changes → Save Manifests → Validate Locally.");
		message.AppendLine().Append("The complete Winget error is available under Technical Details.");
		return message.ToString();
	}

	private static void AddValidationField(ISet<string> fields, string candidate)
	{
		string name = candidate.Trim();
		if (name.Length > 0 && name.All(char.IsLetterOrDigit) && !name.Equals("root", StringComparison.OrdinalIgnoreCase)) fields.Add(name);
	}

	private static string FriendlyValidationField(string field)
	{
		string normalized = field.ToLowerInvariant();
		if (normalized == "packageidentifier") return "Package Identifier — open 2 Package. Use Publisher.ApplicationName with a dot and no spaces.";
		if (normalized == "packageversion") return "Package Version — open 2 Package. Do not include a leading v.";
		if (normalized == "packagelocale" || normalized == "defaultlocale") return "Package language — open 2 Package and use a locale such as en-US.";
		if (normalized == "installerurl") return "Installer URL — open 3 Installers and enter the direct public HTTPS download link.";
		if (normalized == "installersha256" || normalized == "sha256") return "SHA-256 — open 3 Installers and attach/inspect the exact release file again.";
		if (normalized == "architecture") return "Architecture — open 3 Installers and choose x64, x86, arm64, arm, or neutral.";
		if (normalized == "installertype") return "Installer Type — open 3 Installers and choose the format that matches the release file.";
		if (field.StartsWith("Installer", StringComparison.OrdinalIgnoreCase)) return $"{field} — open 3 Installers and review that installer value.";
		return $"{field} — open 2 Package and review the matching field. Uncommon fields are under All Other Schema Fields.";
	}

	private void TogglePreviewMode()
	{
		if (string.IsNullOrWhiteSpace(technicalPreviewText))
		{
			SetStatus("Generate a preview before opening technical details.");
			return;
		}
		if (showingTechnicalPreview) ShowSimplePreview();
		else
		{
			showingTechnicalPreview = true;
			previewBox.Text = technicalPreviewText;
			previewModeButton.Text = "Simple Review";
			previewModeButton.AccessibleName = previewModeButton.Text;
		}
	}

	private void ShowSimplePreview()
	{
		showingTechnicalPreview = false;
		previewBox.Text = simplePreviewText;
		previewModeButton.Text = "Technical Details";
		previewModeButton.AccessibleName = previewModeButton.Text;
	}

	private bool SaveManifests()
	{
		if (uiTestMode)
		{
			SetReviewProgress(ReviewProgress.Saved);
			simplePreviewText = "SAVED SAFELY\r\n\r\n[OK] The manifests were saved.\r\n[OK] Any replaced files were backed up first.\r\n\r\nNEXT: Click Validate Locally.";
			ShowSimplePreview();
			SetStatus("TEST: Save Manifests completed safely without writing files.");
			return true;
		}
		try
		{
			ReadProjectFromControls();
			ManifestGenerationResult result = ManifestService.Generate(project);
			ManifestService.Save(project, result);
			project.LoadedFromExistingManifests = true;
			simplePreviewText = BuildSimpleReview(result, ReviewProgress.Saved);
			SetReviewProgress(ReviewProgress.Saved);
			ShowSimplePreview();
			SetStatus($"Saved {result.Files.Count} manifests safely. Next, choose Validate Locally.");
			return true;
		}
		catch (Exception ex) { ShowError("The manifests could not be saved", ex); return false; }
	}

	private async Task ValidateWithWingetAsync()
	{
		if (uiTestMode)
		{
			SetReviewProgress(ReviewProgress.Validated);
			simplePreviewText = "VALIDATION PASSED\r\n\r\n[OK] Winget found no manifest problems.\r\n\r\nNEXT: Open 5 Test Center and run Safe Preflight.";
			technicalPreviewText = "SAFE UI TEST VALIDATION\r\nOfficial validation was intentionally not launched.";
			ShowSimplePreview();
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
			technicalPreviewText = result.CombinedOutput;
			if (result.ExitCode == 0)
			{
				simplePreviewText = "VALIDATION PASSED — NOTHING NEEDS FIXING\r\n\r\n[OK] Microsoft's Winget validator accepted the generated manifests.\r\n[OK] No files were changed during validation.\r\n\r\nNEXT: Open 5 Test Center and run Safe Preflight, then test the installation.";
				SetReviewProgress(ReviewProgress.Validated);
			}
			else
			{
				simplePreviewText = BuildValidationFailureSummary(result.CombinedOutput);
				SetReviewProgress(ReviewProgress.ValidationFailed);
			}
			ShowSimplePreview();
			SetStatus(result.ExitCode == 0
				? "Official Winget validation passed. Next, open Test Center."
				: "Winget found manifest problems. The Review page now explains what to fix.");
		}
		catch (InvalidDataException ex)
		{
			reviewProgress = ReviewProgress.Editing;
			reviewFingerprint = string.Empty;
			simplePreviewText = BuildFixList(ManifestService.Validate(project));
			if (string.IsNullOrWhiteSpace(simplePreviewText)) simplePreviewText = "WHAT NEEDS ATTENTION\r\n\r\n" + ex.Message;
			technicalPreviewText = ex.ToString();
			ShowSimplePreview();
			SelectTab("Preview & Submit");
			SetStatus("The project needs a few corrections before Winget validation can run.");
		}
		catch (Exception ex) { ShowError("Winget validation could not run", ex); }
		finally
		{
			try { ManifestService.DeleteCleanManifestFolder(cleanFolder); } catch { }
			SetBusy(false);
		}
	}

	private async Task<bool> RunSafePreflightAsync()
	{
		if (uiTestMode)
		{
			testOutputBox.Text = "SAFE UI TEST PREFLIGHT\r\nPASS: No files, network services, installers, or external tools were used.";
			latestTestReport = testOutputBox.Text;
			successfulPreflightFingerprint = ProjectFingerprint();
			RefreshReadiness();
			SetStatus("TEST: Safe Preflight completed without changing the computer.");
			return true;
		}

		string? cleanFolder = null;
		StringBuilder report = new();
		bool criticalFailure = false;
		try
		{
			SetBusy(true, "Running safe preflight checks...");
			ReadProjectFromControls();
			ManifestGenerationResult generated = ManifestService.Generate(project);
			report.AppendLine("WINGET MANIFEST STUDIO — SAFE PREFLIGHT")
				.AppendLine($"Package: {project.PackageIdentifier} {project.PackageVersion}")
				.AppendLine($"Generated: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}")
				.AppendLine();
			report.AppendLine($"PASS  YAML generation: {generated.Files.Count} manifest file(s), including additional locales.");
			foreach (string warning in generated.Warnings) report.AppendLine("NOTE  " + warning);

			for (int index = 0; index < project.Installers.Count; index++)
			{
				InstallerArtifact item = project.Installers[index];
				if (!File.Exists(item.LocalFile))
				{
					report.AppendLine($"WARN  Installer {index + 1}: no attached local file; hash and signature could not be rechecked.");
					continue;
				}
				InstallerInspection inspection = await InstallerInspector.InspectAsync(item.LocalFile, string.Empty, null, operationCancellation!.Token);
				bool hashMatches = string.Equals(item.Sha256, inspection.Sha256, StringComparison.OrdinalIgnoreCase);
				if (!hashMatches) criticalFailure = true;
				report.AppendLine($"{(hashMatches ? "PASS" : "FAIL")}  Installer {index + 1} SHA-256: {(hashMatches ? "matches the manifest" : "does not match the manifest")}.");
				item.SignatureSha256 = inspection.SignatureSha256;
				ApplySignature(item, inspection.Signature);
				bool appPackageSignature = item.InstallerType is "msix" or "appx" && !string.IsNullOrWhiteSpace(inspection.SignatureSha256);
				string signatureLevel = inspection.Signature.IsTrusted || appPackageSignature ? "PASS" : "WARN";
				string signatureStatus = appPackageSignature ? "MSIX/APPX package signature present" : inspection.Signature.Status + FormatSigner(inspection.Signature);
				report.AppendLine($"{signatureLevel}  Installer {index + 1} signature: {signatureStatus}.");
				if (item.InstallerType is "msix" or "appx" && !appPackageSignature)
				{
					criticalFailure = true;
					report.AppendLine($"FAIL  Installer {index + 1}: MSIX/APPX packages must be signed for the community repository.");
				}
				if (!item.VerificationStatus.Contains("Public URL verified", StringComparison.OrdinalIgnoreCase))
					report.AppendLine($"WARN  Installer {index + 1}: use Verify Public URLs to prove the published download matches this file.");
			}

			cleanFolder = ManifestService.CreateCleanManifestFolder(generated);
			SetStatus("Running official Winget schema validation...");
			CommandResult validation = await WingetCommandService.ValidateManifestAsync(cleanFolder, operationCancellation!.Token);
			if (validation.ExitCode != 0) criticalFailure = true;
			report.AppendLine($"{(validation.ExitCode == 0 ? "PASS" : "FAIL")}  Official winget validate: exit code {validation.ExitCode}.");
			if (!string.IsNullOrWhiteSpace(validation.CombinedOutput))
				report.AppendLine(IndentReport(validation.CombinedOutput));

			try
			{
				SetStatus("Checking Winget and microsoft/winget-pkgs for the package identifier...");
				RepositoryCheckResult repository = await WingetRepositoryService.CheckAsync(project.PackageIdentifier, operationCancellation!.Token);
				report.AppendLine($"INFO  Existing package check: {repository.Summary}");
				if (!string.IsNullOrWhiteSpace(repository.GitHubUrl)) report.AppendLine(repository.GitHubUrl);
			}
			catch (Exception ex)
			{
				report.AppendLine("WARN  Existing package lookup could not finish: " + ex.Message);
			}

			AuthenticodeInspection applicationSignature = AuthenticodeInspector.Inspect(Environment.ProcessPath ?? Application.ExecutablePath);
			report.AppendLine($"{(applicationSignature.IsTrusted ? "PASS" : "WARN")}  Studio trust: {applicationSignature.Status}{FormatSigner(applicationSignature)}.");
			report.AppendLine().AppendLine(validation.ExitCode == 0
				? "NEXT  Run Test Install Here or Test in Windows Sandbox before submitting."
				: "STOP  Fix validation failures before any installation test or submission.");
			latestTestReport = report.ToString();
			successfulPreflightFingerprint = criticalFailure ? string.Empty : ProjectFingerprint();
			RefreshReadiness();
			testOutputBox.Text = latestTestReport;
			installerGrid.Refresh();
			SelectTab("Test Center");
			SetStatus(!criticalFailure ? "Safe preflight passed. Continue with an installation test." : "Safe preflight found failures. Review the Test Center report.");
			return !criticalFailure;
		}
		catch (OperationCanceledException) { SetStatus("Safe preflight was cancelled."); return false; }
		catch (Exception ex)
		{
			report.AppendLine("FAIL  Preflight stopped: " + ex.Message);
			latestTestReport = report.ToString();
			testOutputBox.Text = latestTestReport;
			ShowError("Safe preflight could not finish", ex);
			return false;
		}
		finally
		{
			try { ManifestService.DeleteCleanManifestFolder(cleanFolder); } catch { }
			SetBusy(false);
		}
	}

	private async Task InspectSignaturesAsync()
	{
		if (uiTestMode)
		{
			testOutputBox.Text = "SAFE UI TEST: Signature inspection completed without opening files.";
			SetStatus("TEST: Signature inspection completed safely.");
			return;
		}
		try
		{
			SetBusy(true, "Inspecting digital signatures...");
			ReadProjectFromControls();
			StringBuilder report = new("AUTHENTICODE SIGNATURE INSPECTION\r\n\r\n");
			for (int index = 0; index < project.Installers.Count; index++)
			{
				InstallerArtifact item = project.Installers[index];
				if (!File.Exists(item.LocalFile))
				{
					report.AppendLine($"Installer {index + 1}: no local file is attached.");
					continue;
				}
				InstallerInspection inspection = await InstallerInspector.InspectAsync(item.LocalFile, string.Empty, null, operationCancellation!.Token);
				AuthenticodeInspection signature = inspection.Signature;
				item.SignatureSha256 = inspection.SignatureSha256;
				ApplySignature(item, signature);
				if (!string.IsNullOrWhiteSpace(inspection.SignatureSha256) && !signature.IsSigned)
					item.SignatureStatus = "MSIX/APPX package signature present";
				report.AppendLine($"Installer {index + 1}: {Path.GetFileName(item.LocalFile)}")
					.AppendLine($"  Status: {item.SignatureStatus}")
					.AppendLine($"  Signer: {signature.SignerName.IfEmpty("Not available")}")
					.AppendLine($"  Thumbprint: {signature.Thumbprint.IfEmpty("Not available")}")
					.AppendLine($"  Expires: {(signature.NotAfter?.ToString("yyyy-MM-dd") ?? "Not available")}")
					.AppendLine($"  Details: {signature.StatusMessage}").AppendLine();
			}
			AuthenticodeInspection studio = await Task.Run(() => AuthenticodeInspector.Inspect(Environment.ProcessPath ?? Application.ExecutablePath), operationCancellation!.Token);
			report.AppendLine("Winget Manifest Studio executable:").AppendLine($"  {studio.Status}{FormatSigner(studio)}");
			latestTestReport = report.ToString();
			testOutputBox.Text = latestTestReport;
			installerGrid.Refresh();
			SelectTab("Test Center");
			SetStatus("Digital signature inspection finished.");
		}
		catch (Exception ex) { ShowError("Signature inspection failed", ex); }
		finally { SetBusy(false); }
	}

	private async Task FindExistingPackageAsync()
	{
		if (uiTestMode)
		{
			testOutputBox.Text = "SAFE UI TEST: Existing-package discovery completed without network access.";
			SetStatus("TEST: Existing package discovery completed safely.");
			return;
		}
		try
		{
			ReadProjectFromControls();
			SetBusy(true, "Searching Winget and Microsoft's manifest repository...");
			RepositoryCheckResult result = await WingetRepositoryService.CheckAsync(project.PackageIdentifier, operationCancellation!.Token);
			StringBuilder report = new("EXISTING PACKAGE DISCOVERY\r\n\r\n");
			report.AppendLine(result.Summary);
			if (result.LatestVersion.Length > 0) report.AppendLine("Latest repository version folder: " + result.LatestVersion);
			if (result.GitHubUrl.Length > 0) report.AppendLine("Microsoft repository: " + result.GitHubUrl);
			if (!string.IsNullOrWhiteSpace(result.WingetOutput)) report.AppendLine().AppendLine("winget search result:").AppendLine(result.WingetOutput);
			latestTestReport = report.ToString();
			testOutputBox.Text = latestTestReport;
			SelectTab("Test Center");
			SetStatus(result.GitHubFound || result.WingetFound ? "Existing Winget package found. Keep its exact identifier for the update." : "No exact existing package was found.");
		}
		catch (Exception ex) { ShowError("The existing package search could not finish", ex); }
		finally { SetBusy(false); }
	}

	private async Task ExportTestReportAsync()
	{
		if (uiTestMode) { SetStatus("TEST: Test report export completed without writing a file."); return; }
		ReadProjectFromControls();
		string name = SafeFileName(project.PackageIdentifier.IfEmpty("winget-package")) + "-test-report.txt";
		string? path = await SaveFileAsync("Export Winget Test Report", project.ManifestFolder, [".txt"], name);
		if (string.IsNullOrWhiteSpace(path)) return;
		await File.WriteAllTextAsync(path, latestTestReport, Encoding.UTF8);
		SetStatus("Test report exported to " + path);
	}

	private void EnableLocalManifestTesting()
	{
		if (uiTestMode) { SetStatus("TEST: Administrator settings were not opened."); return; }
		if (WingetCommandService.IsLocalManifestFilesEnabled())
		{
			MessageBox.Show(this, "Winget local manifest testing is already enabled for this Windows account.", "Local testing ready", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}
		DialogResult answer = MessageBox.Show(this,
			"Windows requires a one-time administrator confirmation before Winget can install from local manifest files. This changes only Winget's LocalManifestFiles setting. Continue?",
			"Enable local manifest testing", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
		if (answer != DialogResult.Yes) return;
		try
		{
			int processId = WingetCommandService.StartEnableLocalManifestFilesElevated();
			testOutputBox.Text = $"An administrator console opened to enable Winget LocalManifestFiles.\r\nProcess ID: {processId}\r\n\r\nComplete that window, then return here and choose Test Install Here.";
			SelectTab("Test Center");
			SetStatus("Complete the administrator console to enable local manifest testing.");
		}
		catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
		{
			SetStatus("Administrator confirmation was cancelled. No setting was changed.");
		}
		catch (Exception ex) { ShowError("Local manifest testing could not be enabled", ex); }
	}

	private async Task TestInstallHereAsync()
	{
		if (uiTestMode)
		{
			testOutputBox.Text = "SAFE UI TEST: winget install --manifest was intentionally not launched.";
			SetStatus("TEST: Local installation test completed safely without installing software.");
			return;
		}
		ReadProjectFromControls();
		if (!WingetCommandService.IsLocalManifestFilesEnabled())
		{
			MessageBox.Show(this, "Choose Enable Local Testing first and complete the one-time administrator console.", "Local manifest testing is disabled", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}
		DialogResult answer = MessageBox.Show(this,
			$"This will run winget install --manifest for:\r\n\r\n{project.PackageIdentifier}  {project.PackageVersion}\r\nScope: {project.Scope}\r\n\r\nThe installer may change this computer and may request elevation. Continue?",
			"Test install on this computer", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
		if (answer != DialogResult.Yes) return;
		string? cleanFolder = null;
		try
		{
			SetBusy(true, "Validating before the local installation test...");
			cleanFolder = await CreateValidatedTestFolderAsync();
			InteractiveCommandSession session = WingetCommandService.StartManifestInstallSession(cleanFolder);
			testOutputBox.Text = $"LOCAL INSTALL TEST STARTED\r\n\r\nA persistent console is running winget install --manifest. Answer any installer or elevation prompts there.\r\nProcess ID: {session.ProcessId}";
			SelectTab("Test Center");
			SetStatus("Local install test opened in a persistent console.");
			_ = MonitorTestSessionAsync(session, cleanFolder, "Local install test");
			cleanFolder = null;
		}
		catch (Exception ex) { ShowError("The local installation test could not start", ex); }
		finally
		{
			try { ManifestService.DeleteCleanManifestFolder(cleanFolder); } catch { }
			SetBusy(false);
		}
	}

	private async Task VerifyInstalledResultAsync()
	{
		if (uiTestMode)
		{
			testOutputBox.Text = "SAFE UI TEST: Installed package verification did not query the computer.";
			SetStatus("TEST: Installed result verification completed safely.");
			return;
		}
		try
		{
			ReadProjectFromControls();
			SetBusy(true, "Checking the installed package identity and version...");
			CommandResult result = await WingetCommandService.ListInstalledPackageAsync(project.PackageIdentifier, operationCancellation!.Token);
			string output = result.CombinedOutput;
			bool identifierFound = output.Contains(project.PackageIdentifier, StringComparison.OrdinalIgnoreCase);
			bool versionFound = output.Contains(project.PackageVersion, StringComparison.OrdinalIgnoreCase);
			latestTestReport = $"INSTALLED RESULT VERIFICATION\r\n\r\n{(identifierFound ? "PASS" : "FAIL")}: Package identifier {(identifierFound ? "was found" : "was not found")}.\r\n{(versionFound ? "PASS" : "WARN")}: Expected version {project.PackageVersion} {(versionFound ? "was reported" : "was not visible in the result")}.\r\n\r\n{output}";
			testOutputBox.Text = latestTestReport;
			SelectTab("Test Center");
			SetStatus(identifierFound && versionFound ? "Installed package and version match the project." : "The installed result needs review.");
		}
		catch (Exception ex) { ShowError("The installed package could not be verified", ex); }
		finally { SetBusy(false); }
	}

	private async Task TestInSandboxAsync()
	{
		if (uiTestMode)
		{
			testOutputBox.Text = "SAFE UI TEST: Windows Sandbox and Microsoft's SandboxTest script were intentionally not launched.";
			SetStatus("TEST: Sandbox test completed safely without opening a window.");
			return;
		}
		if (!WingetCommandService.IsWindowsSandboxAvailable())
		{
			MessageBox.Show(this, "Windows Sandbox is not available. Enable the Windows Sandbox optional feature in Windows Features, restart if requested, and try again.", "Windows Sandbox unavailable", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}
		ReadProjectFromControls();
		DialogResult answer = MessageBox.Show(this,
			$"This will download Microsoft's official SandboxTest.ps1 from microsoft/winget-pkgs, open Windows Sandbox, and install {project.PackageIdentifier} {project.PackageVersion} inside that disposable environment. Continue?",
			"Test in Windows Sandbox", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
		if (answer != DialogResult.Yes) return;
		string? cleanFolder = null;
		try
		{
			SetBusy(true, "Validating and preparing Microsoft's Windows Sandbox test...");
			cleanFolder = await CreateValidatedTestFolderAsync();
			string script = await OfficialTestAssets.GetSandboxTestScriptAsync(operationCancellation!.Token);
			InteractiveCommandSession session = WingetCommandService.StartSandboxTestSession(script, cleanFolder);
			testOutputBox.Text = $"WINDOWS SANDBOX TEST STARTED\r\n\r\nMicrosoft's official SandboxTest.ps1 is preparing a disposable environment. The first run can take several minutes while it downloads the current Winget package and dependencies.\r\n\r\nOfficial source: {OfficialTestAssets.SandboxTestSource}\r\nProcess ID: {session.ProcessId}";
			SelectTab("Test Center");
			SetStatus("Microsoft's Sandbox test is running in a persistent console.");
			_ = MonitorTestSessionAsync(session, cleanFolder, "Windows Sandbox test");
			cleanFolder = null;
		}
		catch (Exception ex) { ShowError("The Windows Sandbox test could not start", ex); }
		finally
		{
			try { ManifestService.DeleteCleanManifestFolder(cleanFolder); } catch { }
			SetBusy(false);
		}
	}

	private async Task<string> CreateValidatedTestFolderAsync()
	{
		ManifestGenerationResult generated = ManifestService.Generate(project);
		string cleanFolder = ManifestService.CreateCleanManifestFolder(generated);
		try
		{
			CommandResult validation = await WingetCommandService.ValidateManifestAsync(cleanFolder, operationCancellation!.Token);
			if (validation.ExitCode != 0)
				throw new InvalidDataException("Official Winget validation failed. Run Safe Preflight and correct the reported fields before testing installation.\r\n\r\n" + validation.CombinedOutput);
			return cleanFolder;
		}
		catch
		{
			ManifestService.DeleteCleanManifestFolder(cleanFolder);
			throw;
		}
	}

	private async Task MonitorTestSessionAsync(InteractiveCommandSession session, string cleanFolder, string title)
	{
		try
		{
			using Process process = Process.GetProcessById(session.ProcessId);
			await process.WaitForExitAsync();
			string output = File.Exists(session.LogPath) ? await File.ReadAllTextAsync(session.LogPath) : "The console did not produce a captured log.";
			if (IsDisposed || Disposing) return;
			latestTestReport = $"{title.ToUpperInvariant()} RESULT\r\n\r\nExit code: {process.ExitCode}\r\n\r\n{output}";
			testOutputBox.Text = latestTestReport;
			SelectTab("Test Center");
			SetStatus(process.ExitCode == 0 ? $"{title} completed successfully. Review the captured result." : $"{title} exited with code {process.ExitCode}. Review the captured result.");
		}
		catch (Exception ex)
		{
			if (!IsDisposed && !Disposing) SetStatus($"{title} closed, but its final result could not be captured: {ex.Message}");
		}
		finally
		{
			try { if (File.Exists(session.LogPath)) File.Delete(session.LogPath); } catch { }
			try { ManifestService.DeleteCleanManifestFolder(cleanFolder); } catch { }
		}
	}

	private static void ApplySignature(InstallerArtifact item, AuthenticodeInspection signature)
	{
		item.SignatureStatus = signature.Status;
		item.SignerName = signature.SignerName;
		item.SignerThumbprint = signature.Thumbprint;
		item.SignatureExpiration = signature.NotAfter?.ToString("yyyy-MM-dd") ?? string.Empty;
	}

	private static string FormatSigner(AuthenticodeInspection signature) =>
		string.IsNullOrWhiteSpace(signature.SignerName) ? string.Empty : $" — {signature.SignerName}";

	private static string IndentReport(string value) => string.Join(Environment.NewLine,
		value.Replace("\r\n", "\n").Split('\n').Select(line => "      " + line));

	private string ProjectFingerprint()
	{
		string json = JsonSerializer.Serialize(project);
		return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
	}

	private async Task SubmitAsync()
	{
		if (uiTestMode)
		{
			SetStatus("TEST: Submit to Winget completed safely without authentication or submission.");
			await Task.CompletedTask;
			return;
		}
		ReadProjectFromControls();
		if (!string.Equals(successfulPreflightFingerprint, ProjectFingerprint(), StringComparison.Ordinal))
		{
			DialogResult preflightAnswer = MessageBox.Show(this,
				"This exact project has not passed Safe Preflight yet, or it changed after the last test. Run the complete non-installing preflight now before submission?",
				"Preflight required", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (preflightAnswer != DialogResult.Yes || !await RunSafePreflightAsync()) return;
		}
		try
		{
			SetBusy(true, "Confirming whether this is a new package or an update...");
			RepositoryCheckResult repository = await WingetRepositoryService.CheckAsync(project.PackageIdentifier, operationCancellation!.Token);
			if (repository.GitHubFound || repository.WingetFound)
			{
				DialogResult repositoryAnswer = MessageBox.Show(this,
					$"{repository.Summary}\r\n\r\nContinue with an update pull request for {project.PackageIdentifier} {project.PackageVersion}?",
					"Existing Winget package found", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
				if (repositoryAnswer != DialogResult.Yes) return;
			}
		}
		catch (Exception ex)
		{
			DialogResult lookupAnswer = MessageBox.Show(this,
				"The automatic Winget/GitHub lookup could not finish:\r\n\r\n" + ex.Message + "\r\n\r\nContinue to Microsoft's WingetCreate submission checks?",
				"Package lookup unavailable", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
			if (lookupAnswer != DialogResult.Yes) return;
		}
		finally { SetBusy(false); }
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
			"token" => "--store",
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
		if (!wingetCreateReady)
		{
			SetStatus("WingetCreate is still preparing. Local manifest tools remain available while it finishes.");
			return;
		}
		try
		{
			SetBusy(true);
			toolOutputBox.Text = $"> wingetcreate {command} {arguments}{Environment.NewLine}{Environment.NewLine}";
			SelectTab("Official Tool Commands");
			string commandFolder = string.IsNullOrWhiteSpace(workingDirectory) ? project.ManifestFolder : workingDirectory;
			if (WingetCommandService.RequiresInteractiveConsole(command, arguments))
			{
				InteractiveCommandSession session = WingetCommandService.StartWingetCreateInteractiveSession(command, arguments, commandFolder);
				toolOutputBox.AppendText(
					"WingetCreate opened in a persistent console because this command asks interactive questions."
					+ Environment.NewLine
					+ "Complete the questions in that console window. It will stay open so you can read any error. Manifest Studio remains available here."
					+ Environment.NewLine
					+ $"Process ID: {session.ProcessId}");
				SetStatus("WingetCreate opened an interactive console. Complete the questions there.");
				_ = MonitorInteractiveCommandAsync(session, command);
				return;
			}
			CommandResult result = await WingetCommandService.RunWingetCreateAsync(
				command,
				arguments,
				commandFolder,
				operationCancellation!.Token);
			toolOutputBox.AppendText(result.CombinedOutput);
			SetStatus(result.ExitCode == 0 ? "WingetCreate completed successfully." : $"WingetCreate exited with code {result.ExitCode}.");
		}
		catch (Exception ex) { ShowError("WingetCreate could not run", ex); }
		finally { SetBusy(false); }
	}

	private async Task MonitorInteractiveCommandAsync(InteractiveCommandSession session, string command)
	{
		try
		{
			using Process process = Process.GetProcessById(session.ProcessId);
			await process.WaitForExitAsync();
			string output = File.Exists(session.LogPath) ? await File.ReadAllTextAsync(session.LogPath) : string.Empty;
			if (IsDisposed || Disposing) return;
			toolOutputBox.AppendText(Environment.NewLine + Environment.NewLine + "INTERACTIVE COMMAND RESULT" + Environment.NewLine + output);
			System.Text.RegularExpressions.Match pullRequest = System.Text.RegularExpressions.Regex.Match(output,
				@"https://github\.com/microsoft/winget-pkgs/pull/\d+", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
			if (pullRequest.Success)
			{
				toolOutputBox.AppendText(Environment.NewLine + Environment.NewLine +
					"✓ SUBMISSION COMPLETE" + Environment.NewLine +
					"Microsoft received the manifests. Open the pull request to follow automated checks and reviewer feedback:" + Environment.NewLine +
					pullRequest.Value);
				SelectTab("Official Tool Commands");
				SetStatus("Submission completed. Select the pull-request link in Official Tools to review it.");
			}
			else SetStatus($"WingetCreate {command} finished. Review the captured result in Official Tools.");
		}
		catch (Exception ex)
		{
			if (!IsDisposed && !Disposing) SetStatus("The interactive command closed; its final result could not be captured: " + ex.Message);
		}
		finally
		{
			try { if (File.Exists(session.LogPath)) File.Delete(session.LogPath); } catch { }
		}
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
			if (result.ExitCode == 0)
			{
				toolAvailabilityCheckStarted = false;
				wingetCreateReady = false;
				toolRunButton.Enabled = false;
				toolLoadingProgress.Visible = true;
				modeLabel.Text = "LOCAL AUTHORING READY • LOADING WINGETCREATE";
				BeginInvoke(new Action(StartToolAvailabilityCheck));
			}
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
		project.PrivacyUrl = Read("PrivacyUrl");
		project.Author = Read("Author");
		project.PackageName = Read("PackageName");
		project.PackageUrl = Read("PackageUrl");
		project.License = Read("License");
		project.LicenseUrl = Read("LicenseUrl");
		project.Copyright = Read("Copyright");
		project.CopyrightUrl = Read("CopyrightUrl");
		project.PurchaseUrl = Read("PurchaseUrl");
		project.ShortDescription = Read("ShortDescription");
		project.Description = Read("Description");
		project.Moniker = Read("Moniker");
		project.Tags = Read("Tags");
		project.Commands = Read("Commands");
		project.ReleaseNotes = Read("ReleaseNotes");
		project.ReleaseNotesUrl = Read("ReleaseNotesUrl");
		project.InstallationNotes = Read("InstallationNotes");
		project.Channel = Read("Channel");
		project.InstallerLocale = Read("InstallerLocale");
		project.Platform = Read("Platform");
		project.MinimumOSVersion = Read("MinimumOSVersion");
		project.InstallerType = Read("InstallerType");
		project.NestedInstallerType = Read("NestedInstallerType");
		project.Scope = Read("Scope");
		project.InstallModes = Read("InstallModes");
		project.UpgradeBehavior = Read("UpgradeBehavior");
		project.ElevationRequirement = Read("ElevationRequirement");
		project.SwitchSilent = Read("SwitchSilent");
		project.SwitchSilentWithProgress = Read("SwitchSilentWithProgress");
		project.SwitchInteractive = Read("SwitchInteractive");
		project.SwitchInstallLocation = Read("SwitchInstallLocation");
		project.SwitchLog = Read("SwitchLog");
		project.SwitchUpgrade = Read("SwitchUpgrade");
		project.CustomInstallerSwitch = Read("CustomInstallerSwitch");
		project.SwitchRepair = Read("SwitchRepair");
		project.Protocols = Read("Protocols");
		project.FileExtensions = Read("FileExtensions");
		project.UnsupportedOSArchitectures = Read("UnsupportedOSArchitectures");
		project.InstallerSuccessCodes = Read("InstallerSuccessCodes");
		project.PackageFamilyName = Read("PackageFamilyName");
		project.ReleaseDate = Read("ReleaseDate");
		project.RepairBehavior = Read("RepairBehavior");
		project.InstallerAbortsTerminal = Read("InstallerAbortsTerminal");
		project.InstallLocationRequired = Read("InstallLocationRequired");
		project.RequireExplicitUpgrade = Read("RequireExplicitUpgrade");
		project.DisplayInstallWarnings = Read("DisplayInstallWarnings");
		project.DownloadCommandProhibited = Read("DownloadCommandProhibited");
		project.ArchiveBinariesDependOnPath = Read("ArchiveBinariesDependOnPath");
		project.AdvancedLocaleFieldsYaml = Read("AdvancedLocaleFieldsYaml");
		project.AdvancedInstallerFieldsYaml = Read("AdvancedInstallerFieldsYaml");
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
		Write("PrivacyUrl", project.PrivacyUrl);
		Write("Author", project.Author);
		Write("PackageName", project.PackageName);
		Write("PackageUrl", project.PackageUrl);
		Write("License", project.License);
		Write("LicenseUrl", project.LicenseUrl);
		Write("Copyright", project.Copyright);
		Write("CopyrightUrl", project.CopyrightUrl);
		Write("PurchaseUrl", project.PurchaseUrl);
		Write("ShortDescription", project.ShortDescription);
		Write("Description", project.Description);
		Write("Moniker", project.Moniker);
		Write("Tags", project.Tags);
		Write("Commands", project.Commands);
		Write("ReleaseNotes", project.ReleaseNotes);
		Write("ReleaseNotesUrl", project.ReleaseNotesUrl);
		Write("InstallationNotes", project.InstallationNotes);
		Write("Channel", project.Channel);
		Write("InstallerLocale", project.InstallerLocale);
		Write("Platform", project.Platform);
		Write("MinimumOSVersion", project.MinimumOSVersion);
		Write("InstallerType", project.InstallerType);
		Write("NestedInstallerType", project.NestedInstallerType);
		Write("Scope", project.Scope);
		Write("InstallModes", project.InstallModes);
		Write("UpgradeBehavior", project.UpgradeBehavior);
		Write("ElevationRequirement", project.ElevationRequirement);
		Write("SwitchSilent", project.SwitchSilent);
		Write("SwitchSilentWithProgress", project.SwitchSilentWithProgress);
		Write("SwitchInteractive", project.SwitchInteractive);
		Write("SwitchInstallLocation", project.SwitchInstallLocation);
		Write("SwitchLog", project.SwitchLog);
		Write("SwitchUpgrade", project.SwitchUpgrade);
		Write("CustomInstallerSwitch", project.CustomInstallerSwitch);
		Write("SwitchRepair", project.SwitchRepair);
		Write("Protocols", project.Protocols);
		Write("FileExtensions", project.FileExtensions);
		Write("UnsupportedOSArchitectures", project.UnsupportedOSArchitectures);
		Write("InstallerSuccessCodes", project.InstallerSuccessCodes);
		Write("PackageFamilyName", project.PackageFamilyName);
		Write("ReleaseDate", project.ReleaseDate);
		Write("RepairBehavior", project.RepairBehavior);
		Write("InstallerAbortsTerminal", project.InstallerAbortsTerminal);
		Write("InstallLocationRequired", project.InstallLocationRequired);
		Write("RequireExplicitUpgrade", project.RequireExplicitUpgrade);
		Write("DisplayInstallWarnings", project.DisplayInstallWarnings);
		Write("DownloadCommandProhibited", project.DownloadCommandProhibited);
		Write("ArchiveBinariesDependOnPath", project.ArchiveBinariesDependOnPath);
		Write("AdvancedLocaleFieldsYaml", project.AdvancedLocaleFieldsYaml);
		Write("AdvancedInstallerFieldsYaml", project.AdvancedInstallerFieldsYaml);
		insecureUrlCheck.Checked = project.AllowInsecureUrls;
		installerGrid.DataSource = project.Installers;
		TrackInstallerChanges();
		RefreshReadiness();
	}

	private Control CreateReadinessPanel()
	{
		StudioCard panel = new()
		{
			Dock = DockStyle.Top,
			Height = 118,
			BackColor = CardColor,
			Padding = new Padding(16, 12, 16, 12),
			Margin = new Padding(0, 0, 0, 8),
			CornerRadius = 10
		};
		readinessLabel = new Label
		{
			Dock = DockStyle.Fill,
			Text = "WHAT NEEDS ATTENTION\r\nComplete Package Details and add an installer to continue.",
			ForeColor = StudioPalette.Warning,
			Font = new Font("Segoe UI Semibold", 9.5F),
			TextAlign = ContentAlignment.MiddleLeft
		};
		panel.Controls.Add(readinessLabel);
		return panel;
	}

	private void WireReadinessTracking()
	{
		foreach (Control control in fields.Values)
			control.TextChanged += (_, _) => RefreshReadiness();
		insecureUrlCheck.CheckedChanged += (_, _) => RefreshReadiness();
		installerGrid.CellValueChanged += (_, _) => RefreshReadiness();
		installerGrid.RowsAdded += (_, _) => BeginInvoke(new Action(RefreshReadiness));
		installerGrid.RowsRemoved += (_, _) => BeginInvoke(new Action(RefreshReadiness));
		TrackInstallerChanges();
	}

	private void TrackInstallerChanges()
	{
		if (ReferenceEquals(trackedInstallers, project.Installers)) return;
		if (trackedInstallers is not null) trackedInstallers.ListChanged -= Installers_ListChanged;
		trackedInstallers = project.Installers;
		trackedInstallers.ListChanged += Installers_ListChanged;
	}

	private void Installers_ListChanged(object? sender, System.ComponentModel.ListChangedEventArgs eventArgs) => RefreshReadiness();

	private void RefreshReadiness()
	{
		if (refreshingReadiness || readinessLabel is null || readinessLabel.IsDisposed || fields.Count == 0) return;
		refreshingReadiness = true;
		try
		{
			ReadProjectFromControls();
			List<string> errors = ManifestService.Validate(project);
			fieldErrors.Clear();
			SetFieldError("PackageIdentifier", errors, "Package Identifier");
			SetFieldError("PackageVersion", errors, "Package Version");
			SetFieldError("PackageName", errors, "Package Name");
			SetFieldError("Publisher", errors, "Publisher");
			SetFieldError("ShortDescription", errors, "Short Description");
			SetFieldError("License", errors, "License");
			SetFieldError("ManifestFolder", errors, "Choose a manifest output folder");

			bool ready = errors.Count == 0;
			string currentFingerprint = ProjectFingerprint();
			if (!ready || !string.Equals(reviewFingerprint, currentFingerprint, StringComparison.Ordinal))
			{
				reviewProgress = ReviewProgress.Editing;
				reviewFingerprint = string.Empty;
				technicalPreviewText = string.Empty;
				previewModeButton.Enabled = false;
				if (ready)
				{
					simplePreviewText = "NOTHING NEEDS FIXING\r\n\r\n[OK] All required package information is present.\r\n[OK] Every installer has a public URL, architecture, and SHA-256 hash.\r\n\r\nNEXT: Click Preview Changes. Nothing will be saved yet.";
				}
				else
				{
					StringBuilder fixes = new("WHAT NEEDS ATTENTION\r\n\r\n");
					for (int index = 0; index < errors.Count; index++) fixes.AppendLine($"{index + 1}. {errors[index]}");
					fixes.Append("\r\nOpen 2 Package for package information or 3 Installers for release-file, URL, architecture, and hash problems.");
					simplePreviewText = fixes.ToString();
				}
				showingTechnicalPreview = false;
				previewBox.Text = simplePreviewText;
				previewModeButton.Text = "Technical Details";
				previewModeButton.AccessibleName = previewModeButton.Text;
			}

			readinessLabel.ForeColor = ready ? StudioPalette.Success : StudioPalette.Warning;
			if (errors.Count > 0)
			{
				string first = SimplifyReadinessError(errors[0]);
				string second = errors.Count > 1 ? "\r\n2. " + SimplifyReadinessError(errors[1]) : string.Empty;
				string more = errors.Count > 2 ? $"  (+{errors.Count - 2} more shown below)" : string.Empty;
				readinessLabel.Text = $"FIX {errors.Count} ITEM(S) BEFORE CONTINUING\r\n1. {first}{second}{more}\r\nNEXT: Open 2 Package or 3 Installers and correct the listed fields.";
			}
			else if (reviewProgress == ReviewProgress.Previewed)
				readinessLabel.Text = "PREVIEW READY — NOTHING HAS BEEN SAVED\r\nNothing needs fixing in the required fields. Confirm the summary below.\r\nNEXT: Click Save Manifests.";
			else if (reviewProgress == ReviewProgress.Saved)
				readinessLabel.Text = "SAVED SAFELY\r\nAny replaced manifest was backed up first.\r\nNEXT: Click Validate Locally.";
			else if (reviewProgress == ReviewProgress.ValidationFailed)
				readinessLabel.Text = "WINGET FOUND A PROBLEM\r\nThe simple explanation is shown below; submission remains locked.\r\nNEXT: Fix the named field, preview, save, and validate again.";
			else if (reviewProgress == ReviewProgress.Validated && string.Equals(successfulPreflightFingerprint, reviewFingerprint, StringComparison.Ordinal))
				readinessLabel.Text = "SAFE PREFLIGHT PASSED — READY TO SUBMIT\r\nThe current project passed validation, hashes, signatures, and repository checks.\r\nNEXT: Click Submit to Winget.";
			else if (reviewProgress == ReviewProgress.Validated)
				readinessLabel.Text = "VALIDATION PASSED — NOTHING NEEDS FIXING\r\nMicrosoft's Winget validator accepted these manifests.\r\nNEXT: Click Open Test Center and run Safe Preflight.";
			else
				readinessLabel.Text = $"READY — NOTHING NEEDS FIXING\r\n{project.PackageIdentifier}  •  version {project.PackageVersion}  •  {project.Installers.Count} installer(s)\r\nNEXT: Click Preview Changes. This does not save files.";
			bool currentReview = ready && string.Equals(reviewFingerprint, currentFingerprint, StringComparison.Ordinal);
			bool preflightCurrent = currentReview && string.Equals(successfulPreflightFingerprint, currentFingerprint, StringComparison.Ordinal);
			previewButton.Enabled = ready && reviewProgress == ReviewProgress.Editing && !isBusy;
			saveButton.Enabled = currentReview && reviewProgress == ReviewProgress.Previewed && !isBusy;
			validateButton.Enabled = currentReview && (reviewProgress == ReviewProgress.Saved || reviewProgress == ReviewProgress.ValidationFailed) && !isBusy;
			testCenterButton.Enabled = currentReview && reviewProgress == ReviewProgress.Validated && !preflightCurrent && !isBusy;
			submitButton.Enabled = preflightCurrent && reviewProgress == ReviewProgress.Validated && !isBusy;
			previewModeButton.Enabled = technicalPreviewText.Length > 0 && !isBusy;
			ScheduleRecoverySave();
		}
		finally
		{
			refreshingReadiness = false;
		}
	}

	private void SetFieldError(string field, IEnumerable<string> errors, string prefix)
	{
		if (!fields.TryGetValue(field, out Control? control)) return;
		string? error = errors.FirstOrDefault(value => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
		fieldErrors.SetError(control, error ?? string.Empty);
		fieldErrors.SetIconAlignment(control, ErrorIconAlignment.MiddleRight);
	}

	private static string SimplifyReadinessError(string error) => error
		.Replace("Package Identifier", "Package ID", StringComparison.OrdinalIgnoreCase)
		.Replace("Installer URL", "download URL", StringComparison.OrdinalIgnoreCase)
		.TrimEnd('.');


	private void ScheduleRecoverySave()
	{
		if (uiTestMode || IsDisposed || Disposing) return;
		recoveryTimer ??= new System.Windows.Forms.Timer { Interval = 1200 };
		recoveryTimer.Stop();
		recoveryTimer.Tick -= RecoveryTimer_Tick;
		recoveryTimer.Tick += RecoveryTimer_Tick;
		recoveryTimer.Start();
	}

	private void RecoveryTimer_Tick(object? sender, EventArgs eventArgs)
	{
		recoveryTimer?.Stop();
		try { ReadProjectFromControls(); StudioStateStore.SaveRecovery(project); } catch { }
	}

	private Control CreateInstallerDefaults()
	{
		FlowLayoutPanel row = CreateInlinePanel();
		row.Padding = new Padding(14, 9, 14, 9);
		row.Controls.Add(NewInlineLabel("Defaults"));
		row.Controls.Add(ChoiceField("InstallerType", "Installer type", ["exe", "msi", "wix", "burn", "inno", "nullsoft", "msix", "appx", "zip", "portable", "font"], 150));
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
		Add(nameof(InstallerArtifact.SignatureStatus), "DIGITAL SIGNATURE", 230);
		Add(nameof(InstallerArtifact.SignerName), "SIGNER", 180);
		Add(nameof(InstallerArtifact.Architecture), "ARCH", 65);
		Add(nameof(InstallerArtifact.InstallerType), "TYPE", 75);
		Add(nameof(InstallerArtifact.Scope), "SCOPE", 70);
		Add(nameof(InstallerArtifact.Sha256), "SHA-256", 220);
		Add(nameof(InstallerArtifact.ProductCode), "PRODUCT CODE", 190);
		Add(nameof(InstallerArtifact.UpgradeCode), "UPGRADE CODE", 190);
		Add(nameof(InstallerArtifact.SignatureSha256), "MSIX SIGNATURE SHA-256", 220);
		Add(nameof(InstallerArtifact.AdvancedFieldsYaml), "ADDITIONAL ROW YAML", 280);
		grid.DataError += (_, eventArgs) =>
		{
			eventArgs.ThrowException = false;
			SetStatus("That installer value could not be applied. Review the selected cell and try again.");
		};
		grid.CellFormatting += (_, eventArgs) =>
		{
			if (eventArgs.RowIndex < 0 || eventArgs.ColumnIndex < 0) return;
			string columnName = grid.Columns[eventArgs.ColumnIndex].Name;
			if (columnName is nameof(InstallerArtifact.ProductCode) or nameof(InstallerArtifact.UpgradeCode) &&
				grid.Rows[eventArgs.RowIndex].DataBoundItem is InstallerArtifact installer &&
				string.IsNullOrWhiteSpace(Convert.ToString(eventArgs.Value)))
			{
				bool usesMsiCodes = UsesMsiIdentityCodes(installer.InstallerType);
				eventArgs.Value = usesMsiCodes ? "Not found in MSI" : "Not provided (optional)";
				eventArgs.CellStyle!.ForeColor = usesMsiCodes ? StudioPalette.Warning : StudioPalette.MutedText;
				eventArgs.FormattingApplied = true;
				return;
			}
			if (columnName == nameof(InstallerArtifact.SignatureStatus))
			{
				string signature = Convert.ToString(eventArgs.Value) ?? string.Empty;
				eventArgs.CellStyle!.ForeColor = signature.Contains("trusted", StringComparison.OrdinalIgnoreCase)
					? StudioPalette.Success
					: signature.Contains("unsigned", StringComparison.OrdinalIgnoreCase) || signature.Contains("failed", StringComparison.OrdinalIgnoreCase)
						? Color.FromArgb(255, 105, 125)
						: StudioPalette.Warning;
				return;
			}
			if (columnName != nameof(InstallerArtifact.VerificationStatus)) return;
			string value = Convert.ToString(eventArgs.Value) ?? string.Empty;
			eventArgs.CellStyle!.ForeColor = value.Contains("verified", StringComparison.OrdinalIgnoreCase) || value.Contains("calculated", StringComparison.OrdinalIgnoreCase)
				? StudioPalette.Success
				: value.Contains("failed", StringComparison.OrdinalIgnoreCase) || value.Contains("missing", StringComparison.OrdinalIgnoreCase)
					? Color.FromArgb(255, 105, 125)
					: StudioPalette.Warning;
		};
		grid.CellToolTipTextNeeded += (_, eventArgs) =>
		{
			if (eventArgs.RowIndex < 0 || eventArgs.ColumnIndex < 0 ||
				grid.Rows[eventArgs.RowIndex].DataBoundItem is not InstallerArtifact installer)
				return;
			string columnName = grid.Columns[eventArgs.ColumnIndex].Name;
			if (columnName is not (nameof(InstallerArtifact.ProductCode) or nameof(InstallerArtifact.UpgradeCode)))
				return;
			eventArgs.ToolTipText = UsesMsiIdentityCodes(installer.InstallerType)
				? "This value is read automatically from the selected MSI file. 'Not found in MSI' means the package author did not include it."
				: "This installer does not provide standardized MSI identity codes. Winget treats these fields as optional, so leave them blank unless you know the installed Apps & Features correlation value.";
		};
		return grid;
	}

	private static bool UsesMsiIdentityCodes(string installerType) =>
		installerType.Equals("msi", StringComparison.OrdinalIgnoreCase) ||
		installerType.Equals("wix", StringComparison.OrdinalIgnoreCase);

	private Control Field(string key, string label, string hint = "", bool multiline = false, int width = 520)
	{
		Panel wrapper = new() { Width = width, Height = multiline ? 115 : 70, Margin = new Padding(8) };
		Label caption = new() { Text = label, AutoSize = true, ForeColor = Color.FromArgb(189, 213, 244), Font = new Font("Segoe UI Semibold", 9F), Location = new Point(0, 0) };
		StudioTextBox box = NewTextBox(width);
		box.AccessibleName = label;
		box.AccessibleDescription = hint;
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
		box.AccessibleName = label;
		box.AccessibleDescription = $"Choose {label.ToLowerInvariant()}";
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
			Height = 190,
			ColumnCount = 2,
			BackColor = CardColor,
			Padding = new Padding(24),
			Margin = new Padding(0, 0, 0, 18)
		};
		hero.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72));
		hero.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
		TableLayoutPanel copy = new() { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = CardColor };
		copy.RowStyles.Add(new RowStyle(SizeType.Absolute, 74F));
		copy.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		Label title = new() { Text = "Build a Winget submission without editing YAML by hand.", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Margin = Padding.Empty, Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold), ForeColor = Color.White };
		Label description = new() { Text = "Create a new three-file manifest set or safely update an existing one. Local release files provide the real SHA-256 hash; public URLs tell Winget where users will download them.", Dock = DockStyle.Fill, AutoSize = false, MaximumSize = new Size(780, 0), Margin = Padding.Empty, Padding = new Padding(0, 6, 12, 0), ForeColor = MutedColor, Font = new Font("Segoe UI", 10.5F) };
		copy.Controls.Add(title, 0, 0);
		copy.Controls.Add(description, 0, 1);
		Label safety = new() { Text = "LOCAL-FIRST\n\nGitHub token stays in Windows Credential Manager\nNo manifest overwritten without backup\nNo installer downloaded automatically", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = SuccessColor, Font = new Font("Segoe UI Semibold", 9.5F), BackColor = InputColor, Padding = new Padding(18) };
		hero.Controls.Add(copy, 0, 0);
		hero.Controls.Add(safety, 1, 0);
		return hero;
	}

	private Control CreateWorkflowCard(string number, string title, string description, params (string text, EventHandler handler)[] actions)
	{
		StudioCard card = new()
		{
			Width = 1160,
			Height = Math.Max(136, 58 + actions.Length * 54),
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
		copy.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
		copy.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		copy.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Margin = Padding.Empty, Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold), ForeColor = Color.White }, 0, 0);
		copy.Controls.Add(new Label { Text = description, Dock = DockStyle.Fill, AutoSize = false, Margin = Padding.Empty, Padding = new Padding(0, 5, 14, 0), ForeColor = MutedColor, MaximumSize = new Size(700, 0) }, 0, 1);
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

	private static TabPage NewPage(string title) => new(title) { AccessibleName = title, BackColor = PageColor, ForeColor = Color.White, Padding = new Padding(18) };
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
			AccessibleName = text,
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
			Size originalSize = Size;
			Size = MinimumSize;
			PerformLayout();
			LayoutHeaderControls();
			Record(securityBadge.Right + 12 <= minimizeButton.Left && navigationPanel.ClientSize.Width > 0, "Minimum-size layout remains usable");
			Size = new Size(1600, 1000);
			PerformLayout();
			LayoutHeaderControls();
			Record(closeButton.Right <= headerPanel.ClientSize.Width && workspaceTabs.ClientSize.Width > 0, "Large high-DPI-style layout remains usable");
			Size = originalSize;
			PerformLayout();
			LayoutHeaderControls();

			foreach ((string title, StudioNavButton button) in navigationButtons)
			{
				button.PerformClick();
				Application.DoEvents();
				Record(string.Equals(workspaceTabs.SelectedTab?.Text, title, StringComparison.Ordinal) && button.Selected, $"Navigation tab: {title}");
			}
			Record(
				navigationButtons.TryGetValue("Test Center", out StudioNavButton? testCenterButton)
					&& testCenterButton.Text.Contains("Test Center", StringComparison.Ordinal),
				"Test Center is clearly named in the main navigation");

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

			Write("PackageIdentifier", "InvalidIdentifier");
			Write("PackageVersion", "1.0.0");
			Write("DefaultLocale", "en-US");
			Write("ManifestVersion", "1.12.0");
			Write("PackageName", "Sample");
			Write("Publisher", "Contoso");
			Write("ShortDescription", "Sample package");
			Write("License", "MIT");
			Write("ManifestFolder", Path.Combine(Path.GetTempPath(), "WingetManifestStudioUiTest"));
			gridItem.Sha256 = new string('A', 64);
			RefreshReadiness();
			Record(!previewButton.Enabled && !saveButton.Enabled && fieldErrors.GetError(fields["PackageIdentifier"]).Length > 0,
				"Invalid fields are explained and the guided review is blocked");
			Write("PackageIdentifier", "Contoso.Sample");
			RefreshReadiness();
			Record(previewButton.Enabled && !saveButton.Enabled && !validateButton.Enabled && readinessLabel.Text.StartsWith("READY", StringComparison.Ordinal),
				"Ready projects clearly unlock Preview as the only next step", string.Join(" | ", ManifestService.Validate(project)));
			GeneratePreview();
			bool simpleReviewVisible = simplePreviewText?.Contains("WHAT NEEDS ATTENTION", StringComparison.Ordinal) == true;
			Record(saveButton is { Enabled: true } && validateButton is not { Enabled: true } && simpleReviewVisible,
				"A simple preview unlocks Save and keeps technical YAML hidden");
			SaveManifests();
			string savedReadiness = readinessLabel.Text ?? string.Empty;
			Record(saveButton is not { Enabled: true } && validateButton is { Enabled: true } && savedReadiness.StartsWith("SAVED", StringComparison.Ordinal),
				"Saving unlocks Validate as the next step");
			SetReviewProgress(ReviewProgress.Validated);
			string validationReadiness = readinessLabel.Text ?? string.Empty;
			Record(testCenterButton is { Enabled: true } && submitButton is not { Enabled: true } && validationReadiness.StartsWith("VALIDATION PASSED", StringComparison.Ordinal),
				"Successful validation unlocks Test Center as the next step");
			successfulPreflightFingerprint = ProjectFingerprint();
			RefreshReadiness();
			string preflightReadiness = readinessLabel.Text ?? string.Empty;
			Record(submitButton is { Enabled: true } && preflightReadiness.StartsWith("SAFE PREFLIGHT PASSED", StringComparison.Ordinal),
				"Safe Preflight unlocks submission as the final step");

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
			Record(fields.Values.All(control => !string.IsNullOrWhiteSpace(control.AccessibleName)), "Every package field has an accessible name");
			Record(actionButtons.All(button => !string.IsNullOrWhiteSpace(button.AccessibleName)), "Every action button has an accessible name");
			Record(readinessLabel.Width > 0 && readinessLabel.Height > 0, "Project readiness guidance is visible");
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
		RefreshReadiness();
	}

	private void SelectTab(string title)
	{
		TabPage? page = workspaceTabs.TabPages.Cast<TabPage>().FirstOrDefault(candidate =>
			string.Equals(candidate.AccessibleName, title, StringComparison.Ordinal) || string.Equals(candidate.Text, title, StringComparison.Ordinal));
		if (page is not null)
		{
			workspaceTabs.SelectedTab = page;
			UpdateNavigationState();
		}
	}

	private void ChangeLanguage()
	{
		if (applyingLanguage || languageBox is null) return;
		if (uiTestMode) { SetStatus("TEST: Language selection changed without writing application settings."); return; }
		string language = languageBox.SelectedIndex == 1 ? "es-ES" : "en-US";
		StudioStateStore.SetLanguage(language);
		ApplyInterfaceLanguage(language);
		SetStatus(language == "es-ES" ? "Idioma cambiado a Español." : "Language changed to English.");
	}

	private void ApplyInterfaceLanguage(string language)
	{
		if (!StudioLocalization.IsSupported(language)) language = "en-US";
		applyingLanguage = true;
		try
		{
			foreach (Control control in DescendantsAndSelf(this))
			{
				bool localizable = control is Button or TabPage or CheckBox or Label || ReferenceEquals(control, this);
				if (!localizable || ReferenceEquals(control, statusLabel) || ReferenceEquals(control, modeLabel)
					|| ReferenceEquals(control, securityBadge) || ReferenceEquals(control, readinessLabel))
					continue;
				if (!originalInterfaceText.TryGetValue(control, out string? english))
				{
					english = control.Text;
					originalInterfaceText[control] = english;
				}
				control.Text = StudioLocalization.Translate(english, language);
			}
			if (languageBox is not null) languageBox.SelectedIndex = language == "es-ES" ? 1 : 0;
			UpdateNavigationState();
		}
		finally { applyingLanguage = false; }
	}

	private static IEnumerable<Control> DescendantsAndSelf(Control root)
	{
		yield return root;
		foreach (Control child in root.Controls)
			foreach (Control descendant in DescendantsAndSelf(child))
				yield return descendant;
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
