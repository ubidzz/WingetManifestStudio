using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ManifestUpdater;

public partial class MainForm : Form
{
	private static readonly string[] InstallerExtensions = [".msi", ".exe", ".msix", ".msixbundle", ".appx", ".appxbundle", ".zip", ".otf", ".otc", ".ttf", ".ttc", ".fnt"];
	private static readonly Color PageColor = StudioPalette.Window;
	private static readonly Color CardColor = StudioPalette.Card;
	private static readonly Color InputColor = StudioPalette.Input;
	private static readonly Color BorderColor = StudioPalette.Border;
	private static readonly Color MutedColor = StudioPalette.SecondaryText;
	private static readonly Color AccentColor = StudioPalette.Accent;
	private static readonly Color SuccessColor = StudioPalette.Success;
	private const string DefaultOfficialToolOutput = "Official command output appears here. Question-based commands open a separate WingetCreate console. GitHub tokens are managed by WingetCreate, not saved in this application.";
	private const string DefaultTestOutput = "Your latest test result appears here. Start with the highlighted action above; the Studio will tell you exactly what to do next.";
	private static readonly HashSet<string> RequiredProjectFields = new(StringComparer.OrdinalIgnoreCase)
	{
		"PackageIdentifier", "PackageVersion", "DefaultLocale", "ManifestVersion", "ManifestFolder",
		"PackageName", "Publisher", "License", "ShortDescription"
	};

	private ManifestProject project = new();
	private enum ReviewProgress { Editing, Previewed, Saved, ValidationFailed, Validated }
	private enum StudioUpdateUiState { Idle, Checking, Current, Available, Error, Downloading }
	private readonly Dictionary<string, Control> fields = new(StringComparer.OrdinalIgnoreCase);
	private DataGridView installerGrid = null!;
	private RichTextBox previewBox = null!;
	private RichTextBox toolOutputBox = null!;
	private RichTextBox testOutputBox = null!;
	private Label testPlanLabel = null!;
	private FlowLayoutPanel optionalProjectFieldsPanel = null!;
	private StudioComboBox toolCommandBox = null!;
	private readonly List<StudioComboBox> languageBoxes = [];
	private StudioTextBox toolArgumentsBox = null!;
	private StudioToggleSwitch insecureUrlCheck = null!;
	private Label readinessLabel = null!;
	private Button reviewNextActionButton = null!;
	private Label reviewActionTitleLabel = null!;
	private Label reviewActionDescriptionLabel = null!;
	private Label reviewActionSafetyLabel = null!;
	private StudioTestProgressStep[] reviewProgressSteps = [];
	private StudioStatusPill[] reviewStatusPills = [];
	private Button nextTestActionButton = null!;
	private Label nextTestActionTitleLabel = null!;
	private Label nextTestActionDescriptionLabel = null!;
	private Label nextTestActionSafetyLabel = null!;
	private StudioTestProgressStep[] testProgressSteps = [];
	private StudioStatusPill[] testStatusPills = [];
	private Control testOptionalToolsCard = null!;
	private Button optionalToolsToggleButton = null!;
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
	private System.Windows.Forms.Timer? studioUpdateStartupTimer;
	private Label studioUpdateTitleLabel = null!;
	private Label studioUpdateDescriptionLabel = null!;
	private Label studioUpdateStatusLabel = null!;
	private Button studioUpdateButton = null!;
	private StudioUpdateRelease? availableStudioUpdate;
	private StudioDistributionKind studioDistribution = StudioDistributionKind.Portable;
	private StudioUpdateUiState studioUpdateUiState;
	private string studioUpdateError = string.Empty;
	private bool studioUpdateCheckRunning;
	private string latestTestReport = "No test report has been generated yet.";
	private readonly Dictionary<Control, string> originalInterfaceText = new(ReferenceEqualityComparer.Instance);
	private readonly Dictionary<StudioTextBox, string> originalPlaceholderText = new(ReferenceEqualityComparer.Instance);
	private string currentStatusEnglish = "Ready";
	private bool applyingLanguage;
	private string successfulPreflightFingerprint = string.Empty;
	private ReviewProgress reviewProgress;
	private string reviewFingerprint = string.Empty;
	private string simplePreviewText = string.Empty;
	private string technicalPreviewText = string.Empty;
	private bool showingTechnicalPreview;
	private bool workspaceInitialized;
	private bool applyingProjectToControls;
	private WingetHealthResult? wingetHealth;
	private bool localManifestFilesEnabled;
	private bool testEnvironmentCheckRunning;
	private DateTimeOffset wingetHealthCheckedAt;
	private string successfulLocalInstallFingerprint = string.Empty;
	private string verifiedInstalledFingerprint = string.Empty;
	private string cleanProjectFingerprint = string.Empty;
	private string currentInterfaceLanguage = "en-US";
	private bool closingAfterConfirmation;
	private bool schemaRecommendationStarted;

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
			// The complete visual tree must exist before Windows shows the form. Building
			// pages after Shown exposes half-laid-out controls and produces the broken
			// first-paint state reported on clean launches.
			InitializeWorkspaceControls();
			Shown += MainForm_Shown;
		}
	}

	private void MainForm_Shown(object? sender, EventArgs e)
	{
		CompleteStartup();
	}

	private void InitializeWorkspaceControls()
	{
		if (workspaceInitialized) return;
		SuspendLayout();
		workspaceTabs.SuspendLayout();
		try
		{
			BuildWorkspace();
			WireReadinessTracking();
			workspaceInitialized = true;
		}
		finally
		{
			workspaceTabs.ResumeLayout(true);
			ResumeLayout(true);
		}
	}

	private void CompleteStartup()
	{
		try
		{
			ApplyProjectToControls();
			MarkProjectClean();
			if (!uiTestMode) ApplyInterfaceLanguage(StudioStateStore.GetLanguage());
			SetStatus("Ready. Start a new package or explicitly load a manifest folder.");
			if (uiTestMode)
			{
				SetModeText("SAFE UI TEST MODE");
				return;
			}

			SetModeText("LOCAL AUTHORING READY • WINGETCREATE STARTING SHORTLY");
			busyProgress.Visible = false;
			toolLoadingProgress.Visible = false;
			SetStatus("Manifest Studio is ready. WingetCreate official tools will load shortly in the background.");
			ScheduleToolAvailabilityCheck();
			StartManifestSchemaRecommendation();
			ScheduleStudioUpdateCheck();
		}
		catch (Exception ex)
		{
			ShowError("Startup could not finish", ex);
		}
	}

	protected override void OnFormClosing(FormClosingEventArgs eventArgs)
	{
		if (!uiTestMode && !closingAfterConfirmation && eventArgs.CloseReason != CloseReason.WindowsShutDown)
		{
			if (isBusy)
			{
				DialogResult running = MessageBox.Show(this,
					"A Studio operation is still running. Choose Yes to cancel that operation and stay in the Studio. After it stops, close again to Save, Discard, or Cancel your edits. Choose No to let the operation continue.",
					"Operation still running", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
				if (running == DialogResult.Yes) operationCancellation?.Cancel();
				eventArgs.Cancel = true;
				return;
			}

			if (!ConfirmSaveOrDiscardChanges("close Winget Manifest Studio"))
			{
				eventArgs.Cancel = true;
				return;
			}
			closingAfterConfirmation = true;
		}
		base.OnFormClosing(eventArgs);
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
			SetModeText("LOCAL AUTHORING READY • LOADING WINGETCREATE");
			toolLoadingProgress.Visible = true;
			SetStatus("Local manifest tools are ready. Preparing WingetCreate in the background...");
			StartToolAvailabilityCheck();
		};
		wingetCreateStartupTimer.Start();
	}

	private void ScheduleStudioUpdateCheck()
	{
		if (uiTestMode || studioUpdateStartupTimer is not null || IsDisposed || Disposing) return;
		studioUpdateStartupTimer = new System.Windows.Forms.Timer { Interval = 6000 };
		studioUpdateStartupTimer.Tick += async (_, _) =>
		{
			studioUpdateStartupTimer?.Stop();
			studioUpdateStartupTimer?.Dispose();
			studioUpdateStartupTimer = null;
			if (IsDisposed || Disposing) return;
			await CheckForStudioUpdateAsync(false);
		};
		studioUpdateStartupTimer.Start();
	}

	private async Task CheckForStudioUpdateAsync(bool forceRefresh)
	{
		if (studioUpdateCheckRunning || IsDisposed || Disposing) return;
		studioUpdateCheckRunning = true;
		studioUpdateUiState = StudioUpdateUiState.Checking;
		studioUpdateError = string.Empty;
		RefreshStudioUpdateCard();
		try
		{
			StudioUpdateCheck check = await StudioUpdateService.CheckAsync(forceRefresh);
			if (IsDisposed || Disposing) return;
			studioDistribution = check.Distribution;
			availableStudioUpdate = check.UpdateAvailable ? check.LatestRelease : null;
			studioUpdateUiState = check.UpdateAvailable ? StudioUpdateUiState.Available : StudioUpdateUiState.Current;
		}
		catch (Exception ex)
		{
			if (IsDisposed || Disposing) return;
			availableStudioUpdate = null;
			studioUpdateUiState = StudioUpdateUiState.Error;
			studioUpdateError = ex.Message;
		}
		finally
		{
			studioUpdateCheckRunning = false;
			if (!IsDisposed && !Disposing) RefreshStudioUpdateCard();
		}
	}

	private async void StudioUpdateButton_Click(object? sender, EventArgs eventArgs)
	{
		if (uiTestMode)
		{
			SetStatus("TEST: The update button is connected without contacting GitHub or changing application files.");
			return;
		}
		if (studioUpdateUiState != StudioUpdateUiState.Available || availableStudioUpdate is null)
		{
			await CheckForStudioUpdateAsync(true);
			return;
		}
		await InstallStudioUpdateAsync(availableStudioUpdate);
	}

	private async Task InstallStudioUpdateAsync(StudioUpdateRelease release)
	{
		string currentExecutable = Environment.ProcessPath ?? string.Empty;
		if (currentExecutable.Length == 0)
		{
			ShowError("The application update could not start", new InvalidOperationException("The current Winget Manifest Studio application path is unavailable."));
			return;
		}
		if (studioDistribution == StudioDistributionKind.Portable
			&& !StudioUpdateService.CanReplacePortableExecutable(currentExecutable, out string writeError))
		{
			ShowError("The portable application cannot update itself", new InvalidOperationException(writeError));
			return;
		}
		if (!ConfirmSaveOrDiscardChanges("install the Studio update")) return;

		string distributionText = studioDistribution == StudioDistributionKind.MsiInstalled
			? T("StudioSetup.msi will update the installed copy.")
			: T("The new portable EXE will replace this file after the Studio closes. A backup is restored automatically if replacement fails.");
		string confirmation = string.Format(T("Winget Manifest Studio {0} is available."), release.VersionText)
			+ "\r\n\r\n" + string.Format(T("File: {0} ({1})"), release.Asset.Name, FormatSize(release.Asset.Size))
			+ "\r\n" + distributionText + "\r\n\r\n" + T("Download and install it now?");
		if (MessageBox.Show(this, confirmation, T("Install Studio update?"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

		SetBusy(true, T("Downloading the verified Studio update from GitHub..."));
		studioUpdateUiState = StudioUpdateUiState.Downloading;
		RefreshStudioUpdateCard();
		try
		{
			CancellationToken cancellationToken = operationCancellation?.Token ?? CancellationToken.None;
			Progress<int> progress = new(percent =>
			{
				if (IsDisposed || Disposing) return;
				studioUpdateButton.Text = string.Format(T("Downloading... {0}%"), percent);
				studioUpdateStatusLabel.Text = string.Format(T("Downloading and checking {0}: {1}%"), release.Asset.Name, percent);
			});
			DownloadedStudioUpdate downloaded = await StudioUpdateService.DownloadAsync(release, progress, cancellationToken);
			ProcessStartInfo launcher = studioDistribution == StudioDistributionKind.MsiInstalled
				? StudioUpdateService.CreateMsiUpdateLauncher(downloaded.FilePath)
				: StudioUpdateService.CreatePortableUpdateLauncher(downloaded.FilePath, currentExecutable, Environment.ProcessId);
			if (Process.Start(launcher) is null)
				throw new InvalidOperationException("Windows did not start the downloaded update.");

			SetBusy(false, T("The verified update is ready. Winget Manifest Studio is closing so the update can finish."));
			closingAfterConfirmation = true;
			BeginInvoke(Close);
		}
		catch (OperationCanceledException)
		{
			SetBusy(false, T("The update download was canceled. No application files were changed."));
			studioUpdateUiState = StudioUpdateUiState.Available;
			RefreshStudioUpdateCard();
		}
		catch (Exception ex)
		{
			SetBusy(false);
			studioUpdateUiState = StudioUpdateUiState.Error;
			studioUpdateError = ex.Message;
			RefreshStudioUpdateCard();
			ShowError("The application update could not finish", ex);
		}
	}

	protected override void OnFormClosed(FormClosedEventArgs eventArgs)
	{
		wingetCreateStartupTimer?.Stop();
		wingetCreateStartupTimer?.Dispose();
		wingetCreateStartupTimer = null;
		tokenStatusTimer?.Stop();
		tokenStatusTimer?.Dispose();
		tokenStatusTimer = null;
		studioUpdateStartupTimer?.Stop();
		studioUpdateStartupTimer?.Dispose();
		studioUpdateStartupTimer = null;
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
		SetModeText(tokenStored
			? "WINGETCREATE READY • TOKEN STORED"
			: "WINGETCREATE READY • NO TOKEN STORED");
		SetSecurityText(tokenStored
			? "LOCAL-FIRST • TOKEN STORED"
			: "LOCAL-FIRST • NO TOKEN STORED");
	}

	private void StartToolAvailabilityCheck()
	{
		if (toolAvailabilityCheckStarted || IsDisposed || Disposing) return;
		toolAvailabilityCheckStarted = true;
		_ = UpdateToolAvailabilityAsync();
	}

	private void StartManifestSchemaRecommendation()
	{
		if (uiTestMode || schemaRecommendationStarted || IsDisposed || Disposing) return;
		schemaRecommendationStarted = true;
		_ = DetectRecommendedManifestSchemaAsync();
	}

	private async Task DetectRecommendedManifestSchemaAsync()
	{
		try
		{
			WingetHealthResult result = await WingetCommandService.CheckWingetHealthAsync();
			if (IsDisposed || Disposing) return;
			wingetHealth = result;
			wingetHealthCheckedAt = DateTimeOffset.Now;
			localManifestFilesEnabled = localManifestFilesEnabled || result.LocalManifestFilesEnabled;
			if (!result.IsReady || project.LoadedFromExistingManifests) return;

			string recommended = ManifestSchemaSupport.RecommendedForWinget(result.Version);
			string selected = Read("ManifestVersion");
			if (selected.Length > 0 && !ManifestSchemaSupport.IsCommunitySupported(selected)) return;
			if (HasUnsavedChanges()) return;
			Write("ManifestVersion", recommended);
			project.ManifestVersion = recommended;
			MarkProjectClean();
			UpdateTestPlanStatus();
		}
		catch
		{
			// Schema selection is a convenience. Winget diagnostics remain available in Test Center.
		}
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
				SetModeText("LOCAL AUTHORING READY • WINGETCREATE OPTIONAL");
				SetStatus("Local manifest tools are ready. Install WingetCreate only when you need the official command tools.");
				return;
			}

			SetModeText("LOCAL AUTHORING READY • PREPARING WINGETCREATE");
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
			SetModeText("LOCAL AUTHORING READY • WINGETCREATE OPTIONAL");
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
			button.Click += async (_, _) =>
			{
				workspaceTabs.SelectedTab = page;
				if (page.Text == "Test Center") await RefreshTestEnvironmentAsync(showReport: false);
			};
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
		content.Controls.Add(CreateLanguageSettingsCard());
		content.Controls.Add(CreateStudioUpdateCard());
		content.Controls.Add(CreateWorkflowCard("1", "Choose how to start", "Create a blank package, load YAML files already on this computer, or enter an existing Winget package ID to download its current manifests into a new working copy.",
			("Load existing manifests", async (_, _) => await LoadManifestsAsync()),
			("Import existing Winget package", async (_, _) => await ImportExistingPackageAsync()),
			("Create a new project", (_, _) => { if (NewProject()) SelectTab("Package Details"); })));
		content.Controls.Add(CreateWorkflowCard("2", "Fill release information", "Enter package details yourself, or paste a public GitHub release URL. The importer fills only blank fields and asks before downloading supported release assets for hashes and installer inspection.",
			("Import a GitHub release", async (_, _) => await ImportGitHubReleaseAsync()),
			("Open Package Details", (_, _) => SelectTab("Package Details"))));
		content.Controls.Add(CreateWorkflowCard("3", "Add the release installers", "Choose the local MSI, EXE, MSIX, APPX, ZIP, portable app, or font files that you will upload. The Studio reads those exact files and calculates their SHA-256 hashes. Then enter the public download URL for each file.",
			("Open Installers & Hashes", (_, _) => SelectTab("Installers & Hashes"))));
		content.Controls.Add(CreateWorkflowCard("4", "Review before anything is changed", "Preview builds all three manifests in memory. Save writes them only after validation and keeps timestamped backups of files that already exist.",
			("Open Preview & Submit", (_, _) => SelectTab("Preview & Submit"))));
		content.Controls.Add(CreateWorkflowCard("5", "Test in the numbered order, then submit", "Open Test Center and follow its numbered status panel: 1 Safe Preflight, 2 Enable Local Testing once, 3 Test Install Here, and 4 Verify Installed Result. Each step explains the next action and stops before changing the computer without confirmation.",
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
			Field("PackageIdentifier", "Package identifier", "Required format: Publisher.Application (example: Contoso.Sample)"),
			Field("PackageVersion", "Package version", "Do not include a leading v."),
			Field("DefaultLocale", "Default locale", "Usually en-US"),
			ChoiceField("ManifestVersion", "Winget schema", ManifestSchemaSupport.SupportedVersions, 220),
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
		content.Controls.Add(CreateWorkflowCard("+", "Optional advanced package fields", "Most beginners do not need installer behavior overrides, custom switches, or raw advanced YAML. Open this section only when the installer documentation or an existing manifest requires one of these values.",
			("Show Optional Fields", (sender, _) => ToggleOptionalProjectFields(sender as Button))));
		optionalProjectFieldsPanel = new FlowLayoutPanel
		{
			Width = 1160,
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			FlowDirection = FlowDirection.TopDown,
			WrapContents = false,
			BackColor = PageColor,
			Margin = Padding.Empty,
			Visible = false
		};
		optionalProjectFieldsPanel.Controls.Add(CreateSection("INSTALLER BEHAVIOR", "Optional current Winget schema fields. Leave a field blank when it does not apply.",
			Field("Channel", "Channel", "Example: stable or beta"),
			Field("InstallerLocale", "Installer locale", "Example: en-US"),
			Field("Platform", "Platforms", "Comma-separated; usually Windows.Desktop"),
			Field("MinimumOSVersion", "Minimum Windows version", "Example: 10.0.19041.0"),
			ChoiceField("NestedInstallerType", "Shared nested type", ["exe", "msi", "wix", "burn", "inno", "nullsoft", "msix", "appx", "portable", "font"], 260),
			Field("NestedInstallerFiles", "Shared ZIP contents", "Semicolon-separated paths inside the ZIP; add | command after a portable file when needed", multiline: true),
			Field("Protocols", "Protocols", "Comma-separated URL protocols"),
			Field("FileExtensions", "File extensions", "Comma-separated, without dots"),
			Field("UnsupportedOSArchitectures", "Unsupported architectures", "Comma-separated"),
			Field("InstallerSuccessCodes", "Extra success codes", "Comma-separated whole numbers"),
			Field("PackageFamilyName", "Package family name"),
			Field("ReleaseDate", "Release date", "YYYY-MM-DD"),
			ChoiceField("RepairBehavior", "Repair behavior", ["modify", "uninstaller", "installer"], 260),
			ChoiceField("InstallerAbortsTerminal", "Installer aborts terminal", ["true", "false"], 260),
			ChoiceField("InstallLocationRequired", "Install location required", ["true", "false"], 260),
			ChoiceField("RequireExplicitUpgrade", "Require explicit upgrade", ["true", "false"], 260),
			ChoiceField("DisplayInstallWarnings", "Display install warnings", ["true", "false"], 260),
			ChoiceField("DownloadCommandProhibited", "Prohibit download command", ["true", "false"], 260),
			ChoiceField("ArchiveBinariesDependOnPath", "Archive binaries depend on PATH", ["true", "false"], 300)));
		optionalProjectFieldsPanel.Controls.Add(CreateSection("INSTALLER SWITCHES", "Winget uses these command-line switches for installer actions. Known Inno, Nullsoft, MSI, and MSIX types often need no custom values.",
			Field("SwitchSilent", "Silent switch"),
			Field("SwitchSilentWithProgress", "Silent with progress"),
			Field("SwitchInteractive", "Interactive switch"),
			Field("SwitchInstallLocation", "Install-location switch"),
			Field("SwitchLog", "Log switch"),
			Field("SwitchUpgrade", "Upgrade switch"),
			Field("CustomInstallerSwitch", "Custom switch"),
			Field("SwitchRepair", "Repair switch")));
		optionalProjectFieldsPanel.Controls.Add(CreateSection("AGREEMENTS & DOCUMENTATION", "Friendly one-line formats create the nested YAML for you. Use one entry per line; leave the entire box blank when it does not apply.",
			Field("Agreements", "Agreements", "One per line: label | HTTPS URL | agreement text", multiline: true, width: 520),
			Field("Documentations", "Documentation links", "One per line: label | HTTPS URL", multiline: true, width: 520)));
		optionalProjectFieldsPanel.Controls.Add(CreateSection("DEPENDENCIES & AVAILABILITY", "Optional rules for packages that depend on another Winget package or Windows feature, MSIX capabilities, or market restrictions.",
			Field("PackageDependencies", "Package dependencies", "One per line: Publisher.Application | minimum version", multiline: true, width: 520),
			Field("WindowsFeatures", "Windows features", "Comma-separated Windows feature names"),
			Field("Capabilities", "MSIX capabilities", "Comma-separated"),
			Field("RestrictedCapabilities", "Restricted capabilities", "Comma-separated"),
			Field("Markets", "Allowed markets", "Comma-separated market codes such as US, CA"),
			Field("ExcludedMarkets", "Excluded markets", "Comma-separated market codes")));
		optionalProjectFieldsPanel.Controls.Add(CreateSection("RETURN CODES & INSTALL DETECTION", "Describe uncommon installer results and installed files without writing YAML. These values are optional and official validation checks their schema.",
			Field("ExpectedReturnCodes", "Expected return codes", "One per line: number | response | optional HTTPS help URL", multiline: true, width: 520),
			ChoiceField("UnsupportedArguments", "Unsupported Winget arguments", ["log", "location", "log, location"], 300),
			Field("DefaultInstallLocation", "Default install location", "Example: %ProgramFiles%\\Publisher\\Application", width: 420),
			Field("InstalledFiles", "Installed files", "One per line: relative path | launch/uninstall/other | optional SHA-256 | optional argument | optional display name", multiline: true, width: 620)));
		optionalProjectFieldsPanel.Controls.Add(CreateSection("PRIVATE SOURCE AUTHENTICATION", "Only private Entra ID secured sources use these fields. Community repository packages should leave all three blank.",
			ChoiceField("AuthenticationType", "Authentication type", ["none", "microsoftEntraId", "microsoftEntraIdForAzureBlobStorage"], 340),
			Field("AuthenticationResource", "Entra resource"),
			Field("AuthenticationScope", "Entra scope")));
		optionalProjectFieldsPanel.Controls.Add(CreateSection("YAML ESCAPE HATCH", "Only use these boxes for schema fields that still have no guided control. Existing custom keys remain preserved even when these boxes stay blank.",
			Field("AdvancedLocaleFieldsYaml", "Additional locale fields", "Optional advanced YAML mapping", multiline: true, width: 520),
			Field("AdvancedInstallerFieldsYaml", "Additional installer fields", "Optional advanced YAML mapping", multiline: true, width: 520)));
		optionalProjectFieldsPanel.ClientSizeChanged += (_, _) =>
		{
			int width = Math.Max(820, optionalProjectFieldsPanel.ClientSize.Width - optionalProjectFieldsPanel.Padding.Horizontal);
			foreach (Control section in optionalProjectFieldsPanel.Controls)
				if (section.Width != width) section.Width = width;
		};
		content.Controls.Add(optionalProjectFieldsPanel);
		root.Controls.Add(content, 0, 2);
		page.Controls.Add(root);
		return page;
	}

	private void ToggleOptionalProjectFields(Button? button)
	{
		optionalProjectFieldsPanel.Visible = !optionalProjectFieldsPanel.Visible;
		if (button is not null)
		{
			SetInterfaceText(button, optionalProjectFieldsPanel.Visible ? "Hide Optional Fields" : "Show Optional Fields");
			button.AccessibleName = button.Text;
		}
		optionalProjectFieldsPanel.Parent?.PerformLayout();
		SetStatus(optionalProjectFieldsPanel.Visible
			? "Optional advanced fields are open. Leave anything you do not recognize blank."
			: "Optional advanced fields are hidden. Their saved values are preserved.");
	}

	private TabPage BuildInstallersTab()
	{
		TabPage page = NewPage("Installers & Hashes");
		TableLayoutPanel root = NewRoot();
		root.RowCount = 5;
		root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		root.Controls.Add(CreateInfoStrip("FOLLOW THESE INSTALLER STEPS", "1 Add each exact release file. 2 Paste its direct public HTTPS URL. 3 Inspect it to fill the hash and metadata. 4 Verify URLs after uploading. Architecture, type, and scope stay visible beside the URL and can be corrected from their dropdowns."), 0, 0);
		root.Controls.Add(CreateToolbar(
			("1 Add Release Files", async (_, _) => await AddInstallerFilesAsync()),
			("2 Enter Public URL", (_, _) => FocusInstallerUrlCell()),
			("3 Inspect & Fill Selected", async (_, _) => await InspectSelectedAsync()),
			("4 Verify Public URLs", async (_, _) => await VerifyPublicUrlsAsync())), 0, 1);
		root.Controls.Add(CreateToolbar(
			("Add URL-Only Row", (_, _) => AddUrlInstaller()),
			("Attach File to Selected", async (_, _) => await AttachFileToSelectedAsync()),
			("Inspect All Local Files", async (_, _) => await InspectAllLocalAsync()),
			("Remove Selected", (_, _) => RemoveSelectedInstaller())), 0, 2);

		installerGrid = CreateInstallerGrid();
		root.Controls.Add(installerGrid, 0, 3);
		root.Controls.Add(CreateInstallerDefaults(), 0, 4);
		page.Controls.Add(root);
		return page;
	}

	private TabPage BuildPreviewTab()
	{
		TabPage page = NewPage("Preview & Submit");
		TableLayoutPanel root = NewRoot();
		root.RowCount = 3;
		root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		root.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
		root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		root.Controls.Add(CreateInfoStrip("REVIEW AND SAVE SAFELY", "Use the single highlighted action below. Review never changes files until you choose Save, and existing manifests are backed up before replacement."), 0, 0);
		root.Controls.Add(CreateReviewProgressPanel(), 0, 1);

		TableLayoutPanel body = new()
		{
			AccessibleName = "Review guided workspace",
			Dock = DockStyle.Fill,
			ColumnCount = 2,
			RowCount = 1,
			BackColor = PageColor,
			Padding = new Padding(0, 2, 0, 0),
			Margin = Padding.Empty
		};
		body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64));
		body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));

		TableLayoutPanel work = new()
		{
			AccessibleName = "Current review action and plain-language results",
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 2,
			BackColor = PageColor,
			Padding = new Padding(0, 0, 8, 0),
			Margin = Padding.Empty
		};
		work.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		work.RowStyles.Add(new RowStyle(SizeType.Absolute, 176));
		work.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		work.Controls.Add(CreateCurrentReviewActionPanel(), 0, 0);
		work.Controls.Add(CreateReviewResultsPanel(), 0, 1);

		FlowLayoutPanel sidebar = new()
		{
			AccessibleName = "Review checklist and view options",
			Dock = DockStyle.Fill,
			AutoScroll = true,
			FlowDirection = FlowDirection.TopDown,
			WrapContents = false,
			BackColor = PageColor,
			Padding = new Padding(8, 0, 0, 0),
			Margin = Padding.Empty
		};
		Control checklist = CreateReviewChecklistPanel();
		Control options = CreateReviewViewOptionsPanel();
		sidebar.Controls.Add(checklist);
		sidebar.Controls.Add(options);
		bool sizingSidebar = false;
		sidebar.ClientSizeChanged += (_, _) =>
		{
			if (sizingSidebar || sidebar.IsDisposed) return;
			sizingSidebar = true;
			try
			{
				int width = Math.Max(320, sidebar.ClientSize.Width - sidebar.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 3);
				foreach (Control control in sidebar.Controls) control.Width = width;
			}
			finally { sizingSidebar = false; }
		};

		body.Controls.Add(work, 0, 0);
		body.Controls.Add(sidebar, 1, 0);
		root.Controls.Add(body, 0, 2);
		page.Controls.Add(root);
		return page;
	}

	private TabPage BuildHelpTab()
	{
		TabPage page = NewPage("Help & Guide");
		FlowLayoutPanel content = NewScrollFlow();
		content.Padding = new Padding(18, 20, 18, 30);
		content.Controls.Add(CreateLanguageSettingsCard());
		content.Controls.Add(CreateInfoStrip("HOW TO USE THIS SOFTWARE", "This guide explains every screen and the information Winget needs. You can read it at any time; the buttons only take you to the screen being described."));
		content.Controls.Add(CreateWorkflowCard("↻", "Keep Winget Manifest Studio up to date", "The Start page checks the latest stable GitHub release after the window is already open. An installed copy uses StudioSetup.msi; a portable copy replaces only its WingetManifestStudio.exe. Nothing downloads or installs until you choose the update button and confirm.",
			("Open application updates", (_, _) => SelectTab("Start Here"))));
		content.Controls.Add(CreateWorkflowCard("1", "Start or open a manifest project", "For a first release, choose New Project. For an update, load a local YAML folder or choose Import Existing Winget Package and enter its exact package ID. Repository import downloads the newest manifests into a separate working-copy folder and never overwrites an existing manifest folder.",
			("Go to Package Details", (_, _) => SelectTab("Package Details"))));
		content.Controls.Add(CreateWorkflowCard("2", "Enter the package identity", "Package Identifier is the permanent Winget name, normally Publisher.Application. Enter Publisher and Package Name first, then use Suggest Package ID if you want help. Package Version has no leading v. Keep the identifier unchanged for updates.",
			("Edit Package Identity", (_, _) => SelectTab("Package Details"))));
		content.Controls.Add(CreateWorkflowCard("3", "Complete the public package information", "Package Name, Publisher, License, and Short Description are required. Enter them yourself or use Import a GitHub Release from Start. The importer fills only blank fields and asks before temporarily downloading supported release assets. Optional guided fields create dependencies, agreements, documentation, return codes, market rules, and install-detection YAML without manual YAML editing.",
			("Edit Package Information", (_, _) => SelectTab("Package Details"))));
		content.Controls.Add(CreateInfoStrip("INSTALLER FILES AND DOWNLOAD LINKS", "Winget downloads from a public URL, but the Studio uses your matching local release file to calculate the trusted SHA-256 value."));
		content.Controls.Add(CreateWorkflowCard("4", "Add the exact release file", "Choose Add Release Files for every installer you publish. Select the same MSI, EXE, MSIX, APPX, bundle, ZIP, portable app, or font file that will be uploaded. Use one row for each architecture, scope, or installer variation. Nothing is assumed to be x64.",
			("Open Installers & Hashes", (_, _) => SelectTab("Installers & Hashes"))));
		content.Controls.Add(CreateWorkflowCard("5", "Enter its public HTTPS URL", "Paste the direct download URL for each installer—not a web page containing a download button. The URL must remain public and must download the exact local file in that row. GitHub release asset URLs are suitable.",
			("Enter Download URLs", (_, _) => SelectTab("Installers & Hashes"))));
		content.Controls.Add(CreateWorkflowCard("6", "Inspect and verify the published installer", "Inspect & Fill Details calculates SHA-256, reports signed or unsigned status, and detects MSI, MSIX, Inno, NSIS, WiX Burn, Squirrel, Velopack, InstallShield, Advanced Installer, and self-extracting EXE clues. Unsigned EXE/MSI files are supported and shown as a warning; MSIX/APPX packages still require their package signature. ZIP files show nested paths. Verify Public URLs proves the published file matches the hash.",
			("Inspect Installer Files", (_, _) => SelectTab("Installers & Hashes"))));
		content.Controls.Add(CreateInfoStrip("SPECIAL PACKAGE TYPES", "Portable EXEs may look like normal EXE installers, so choose portable in the row when needed. Font packages use Microsoft's separate fonts manifest root and have stricter submission rules. PWA support can vary by Winget client and repository policy; always keep the official validation and install-test result."));
		content.Controls.Add(CreateInfoStrip("REVIEW, SAVE, AND PUBLISH", "The preview is your safety check. It creates the proposed YAML in memory without writing to the selected folder."));
		content.Controls.Add(CreateWorkflowCard("7", "Follow Project Readiness, then preview", "The readiness panel counts anything still required and marks problem fields. When it says READY, choose Preview Changes and review the identifier, old and new versions, URLs, architectures, installer types, hashes, and filenames.",
			("Review the Preview", (_, _) => SelectTab("Preview & Submit"))));
		content.Controls.Add(CreateWorkflowCard("8", "Save with recoverable backups", "Choose Save Manifests only after the preview is correct. New files are created in the output folder. Existing files are copied into a timestamped .manifest-backups folder before they are replaced.",
			("Save or Validate", (_, _) => SelectTab("Preview & Submit"))));
		content.Controls.Add(CreateWorkflowCard("9", "Validate before submission", "Validate Locally runs the official Winget validator against a clean temporary copy. If it reports an error, fix the related field and validate again. Validation does not modify the saved manifests.",
			("Open Validation", (_, _) => SelectTab("Preview & Submit"))));
		content.Controls.Add(CreateWorkflowCard("10", "Run test step 1 — Safe Preflight", "The Test Center first checks whether Winget itself works, then rechecks attached file hashes and signatures, runs official validation, and searches Winget plus microsoft/winget-pkgs for the exact package identifier. It does not install anything.",
			("Open Test Center", (_, _) => SelectTab("Test Center"))));
		content.Controls.Add(CreateWorkflowCard("11", "Run test steps 2, 3, and 4", "Enable Local Testing requests one Windows administrator approval. Test Install Here validates again before running winget install --manifest. Verify Installation checks the Winget ID, then falls back to the exact MSI ProductCode or installed application name when Winget does not retain the local manifest ID.",
			("Open Installation Tests", (_, _) => SelectTab("Test Center"))));
		content.Controls.Add(CreateWorkflowCard("12", "Use Windows Sandbox when available", "Sandbox install runs Microsoft's official SandboxTest.ps1 in a disposable environment. Sandbox install + uninstall also verifies removal before the Sandbox closes. The first run can take several minutes while Microsoft dependencies are prepared. A manifest using elevationProhibited must use Test Install Here instead because Microsoft's Sandbox runs Winget as Administrator.",
			("Open Sandbox Test", (_, _) => SelectTab("Test Center"))));
		content.Controls.Add(CreateWorkflowCard("13", "Submit directly from Test Center", "After all four required tests pass, choose Submit to Winget at the bottom of the Test Center steps. It opens Microsoft's WingetCreate workflow for sign-in and pull-request creation. The GitHub token stays in Windows Credential Manager.",
			("Open Test Center", (_, _) => SelectTab("Test Center")),
			("Open Official Tools", (_, _) => SelectTab("Official Tool Commands"))));
		content.Controls.Add(CreateInfoStrip("COMMON PROBLEMS", "Do not use a leading v in the version, a release web-page URL instead of the direct asset URL, a hash from a different file, or the wrong architecture. For ZIP packages, review NESTED TYPE and ZIP CONTENTS. Reattach and inspect the exact published file whenever it changes."));
		page.Controls.Add(content);
		return page;
	}

	private TabPage BuildTestTab()
	{
		TabPage page = NewPage("Test Center");
		TableLayoutPanel root = NewRoot();
		root.RowCount = 3;
		root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		root.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
		root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		root.Controls.Add(CreateInfoStrip("TEST AND FINISH", "Follow the progress line, then use the single highlighted action below. The Studio unlocks each test in the correct order and enables submission when all four pass."), 0, 0);
		root.Controls.Add(CreateTestProgressPanel(), 0, 1);

		TableLayoutPanel body = new()
		{
			AccessibleName = "Test Center guided workspace",
			Dock = DockStyle.Fill,
			ColumnCount = 2,
			RowCount = 1,
			BackColor = PageColor,
			Padding = new Padding(0, 2, 0, 0),
			Margin = Padding.Empty
		};
		body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64));
		body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));

		TableLayoutPanel work = new()
		{
			AccessibleName = "Current test action and results",
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 2,
			BackColor = PageColor,
			Padding = new Padding(0, 0, 8, 0),
			Margin = Padding.Empty
		};
		work.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		work.RowStyles.Add(new RowStyle(SizeType.Absolute, 176));
		work.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		work.Controls.Add(CreateCurrentTestActionPanel(), 0, 0);
		work.Controls.Add(CreateTestResultsPanel(), 0, 1);

		FlowLayoutPanel sidebar = new()
		{
			AccessibleName = "Test checklist and optional tools",
			Dock = DockStyle.Fill,
			AutoScroll = true,
			FlowDirection = FlowDirection.TopDown,
			WrapContents = false,
			BackColor = PageColor,
			Padding = new Padding(8, 0, 0, 0),
			Margin = Padding.Empty
		};
		Control checklist = CreateTestChecklistPanel();
		optionalToolsToggleButton = CreateButton("Show optional tools", (_, _) => ToggleOptionalTestTools());
		optionalToolsToggleButton.AccessibleName = "Show optional Test Center tools";
		optionalToolsToggleButton.AutoSize = false;
		optionalToolsToggleButton.Height = 42;
		testOptionalToolsCard = CreateOptionalTestToolsCard();
		testOptionalToolsCard.Visible = false;
		sidebar.Controls.Add(checklist);
		sidebar.Controls.Add(optionalToolsToggleButton);
		sidebar.Controls.Add(testOptionalToolsCard);
		bool sizingSidebar = false;
		sidebar.ClientSizeChanged += (_, _) =>
		{
			if (sizingSidebar || sidebar.IsDisposed) return;
			sizingSidebar = true;
			try
			{
				int width = Math.Max(320, sidebar.ClientSize.Width - sidebar.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 3);
				foreach (Control control in sidebar.Controls) control.Width = width;
			}
			finally { sizingSidebar = false; }
		};

		body.Controls.Add(work, 0, 0);
		body.Controls.Add(sidebar, 1, 0);
		root.Controls.Add(body, 0, 2);
		page.Controls.Add(root);
		UpdateTestPlanStatus();
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
		toolOutputBox.Text = DefaultOfficialToolOutput;
		root.Controls.Add(toolOutputBox, 0, 3);
		page.Controls.Add(root);
		return page;
	}

	private bool NewProject()
	{
		if (!ConfirmSaveOrDiscardChanges("start a new project")) return false;
		string manifestVersion = wingetHealth is { IsReady: true }
			? ManifestSchemaSupport.RecommendedForWinget(wingetHealth.Version)
			: ManifestSchemaSupport.CurrentVersion;
		project = new ManifestProject { ManifestVersion = manifestVersion };
		ApplyProjectToControls();
		previewBox.Clear();
		MarkProjectClean();
		SetStatus("New project created. Choose an output folder and enter package details.");
		return true;
	}

	private void MarkProjectClean()
	{
		if (fields.Count > 0) ReadProjectFromControls();
		cleanProjectFingerprint = ProjectFingerprint();
	}

	private bool HasUnsavedChanges()
	{
		if (!workspaceInitialized || fields.Count == 0 || cleanProjectFingerprint.Length == 0) return false;
		ReadProjectFromControls();
		return !string.Equals(cleanProjectFingerprint, ProjectFingerprint(), StringComparison.Ordinal);
	}

	private bool ConfirmSaveOrDiscardChanges(string nextAction)
	{
		if (uiTestMode || !HasUnsavedChanges()) return true;
		DialogResult answer = MessageBox.Show(this,
			$"This project has changes that have not been saved.\r\n\r\nChoose Yes to save the manifests before you {nextAction}.\r\nChoose No to discard the unsaved changes.\r\nChoose Cancel to stay here.",
			"Save changes?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
		return answer switch
		{
			DialogResult.Yes => SaveManifests(),
			DialogResult.No => true,
			_ => false
		};
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
		if (!ConfirmSaveOrDiscardChanges("load another manifest folder")) return;
		string? selectedPath = await PickFolderAsync(
			"Load Winget Manifests",
			"Choose the folder that contains the package YAML files. Nothing is changed while loading.",
			fields.GetValueOrDefault("ManifestFolder")?.Text);
		if (string.IsNullOrWhiteSpace(selectedPath)) return;
		await LoadManifestFolderAsync(selectedPath);
	}

	private async Task ImportExistingPackageAsync()
	{
		if (uiTestMode)
		{
			SetStatus("TEST: Existing Winget package import opened safely without network or file access.");
			return;
		}
		if (isBusy) return;
		if (!ConfirmSaveOrDiscardChanges("import another Winget package")) return;
		string? identifier = StudioTextPromptDialog.ShowPrompt(
			this,
			"Import an existing Winget package",
			"Enter the exact package ID shown by Winget. The Studio will download the newest manifest set from microsoft/winget-pkgs into a new working copy.",
			"Winget package ID",
			"Example: Microsoft.PowerToys",
			Read("PackageIdentifier"));
		if (string.IsNullOrWhiteSpace(identifier)) return;
		string? destination = await PickFolderAsync(
			"Choose Parent Folder for Imported Manifests",
			"A new PackageID\\Version working folder will be created here. Existing manifest files will not be overwritten.",
			fields.GetValueOrDefault("ManifestFolder")?.Text);
		if (string.IsNullOrWhiteSpace(destination)) return;

		try
		{
			SetBusy(true, "Finding the current Winget manifests...");
			Progress<string> progress = new(SetStatus);
			RepositoryImportResult imported = await WingetRepositoryService.ImportLatestAsync(
				identifier, destination, progress, operationCancellation!.Token);
			ManifestProject loaded = await Task.Run(
				() => ManifestService.LoadProject(imported.ManifestFolder), operationCancellation.Token);
			if (!loaded.LoadedFromExistingManifests)
				throw new InvalidDataException("The downloaded working copy did not contain a complete Winget manifest set.");
			project = loaded;
			ApplyProjectToControls();
			MarkProjectClean();
			SelectTab("Package Details");
			SetStatus($"Imported {imported.PackageIdentifier} {imported.Version} into a separate working copy. Change the release version and installer URLs for the new release.");
		}
		catch (OperationCanceledException) { SetStatus("Winget package import was cancelled."); }
		catch (Exception ex) { ShowError("The Winget package could not be imported", ex); }
		finally { SetBusy(false); }
	}

	private async Task ImportGitHubReleaseAsync()
	{
		if (uiTestMode)
		{
			SetStatus("TEST: GitHub release import opened safely without network or file access.");
			return;
		}
		if (isBusy) return;
		string? releaseUrl = StudioTextPromptDialog.ShowPrompt(
			this,
			"Import a GitHub release",
			"Paste the public URL for the exact release you are packaging. Existing values are kept; the importer fills only blank fields.",
			"GitHub release URL",
			"https://github.com/owner/project/releases/tag/v1.2.3",
			Read("ReleaseNotesUrl"));
		if (string.IsNullOrWhiteSpace(releaseUrl)) return;

		GitHubReleaseImport release;
		try
		{
			SetBusy(true, "Reading GitHub release information...");
			release = await GitHubReleaseService.ReadAsync(releaseUrl, operationCancellation!.Token);
		}
		catch (OperationCanceledException) { SetStatus("GitHub release import was cancelled."); return; }
		catch (Exception ex) { ShowError("The GitHub release could not be read", ex); return; }
		finally { SetBusy(false); }

		IReadOnlyList<GitHubReleaseAsset> selectedAssets;
		if (release.Assets.Count == 0)
		{
			DialogResult answer = MessageBox.Show(this,
				$"Release: {release.Tag}\r\nRepository: {release.Owner}/{release.Repository}\r\n\r\nNo supported installer assets were found. Import the package and release details only?",
				"Import GitHub release", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (answer != DialogResult.Yes) return;
			selectedAssets = [];
		}
		else
		{
			IReadOnlyList<GitHubReleaseAsset>? selection = GitHubAssetSelectionDialog.SelectAssets(this, release);
			if (selection is null) return;
			selectedAssets = selection;
		}

		ReadProjectFromControls();
		ApplyGitHubReleaseMetadata(release);
		List<InstallerArtifact> added = [];
		foreach (GitHubReleaseAsset asset in selectedAssets)
		{
			if (project.Installers.Any(item => item.InstallerUrl.Equals(asset.DownloadUrl, StringComparison.OrdinalIgnoreCase))) continue;
			InstallerArtifact item = new()
			{
				InstallerUrl = asset.DownloadUrl,
				VerificationStatus = "Imported from GitHub release • waiting for inspection"
			};
			project.Installers.Add(item);
			added.Add(item);
		}
		ApplyProjectToControls();
		foreach (InstallerArtifact item in added)
			await InspectInstallerAsync(item, allowRemoteDownload: true);
		SelectTab(added.Count > 0 ? "Installers & Hashes" : "Package Details");
		SetStatus($"Imported GitHub release {release.Tag}. Filled blank package fields and added {added.Count} new installer row(s); review every value before saving.");
	}

	private void ApplyGitHubReleaseMetadata(GitHubReleaseImport release)
	{
		if (string.IsNullOrWhiteSpace(project.PackageIdentifier))
		{
			string owner = CleanIdentifierPart(release.Owner);
			string repository = CleanIdentifierPart(release.Repository);
			if (owner.Length > 0 && repository.Length > 0) project.PackageIdentifier = owner + "." + repository;
		}
		project.PackageVersion = project.PackageVersion.IfEmpty(release.Version);
		project.PackageName = project.PackageName.IfEmpty(release.Repository);
		project.Author = project.Author.IfEmpty(release.Owner);
		project.PackageUrl = project.PackageUrl.IfEmpty(release.RepositoryUrl);
		project.PublisherUrl = project.PublisherUrl.IfEmpty(release.PublisherUrl);
		project.PublisherSupportUrl = project.PublisherSupportUrl.IfEmpty(release.SupportUrl);
		project.License = project.License.IfEmpty(release.License);
		project.LicenseUrl = project.LicenseUrl.IfEmpty(release.LicenseUrl);
		project.ShortDescription = project.ShortDescription.IfEmpty(release.Description);
		project.Description = project.Description.IfEmpty(release.Description);
		project.Tags = project.Tags.IfEmpty(release.Topics);
		project.ReleaseNotes = project.ReleaseNotes.IfEmpty(release.ReleaseNotes);
		project.ReleaseNotesUrl = project.ReleaseNotesUrl.IfEmpty(release.ReleaseUrl);
		project.ReleaseDate = project.ReleaseDate.IfEmpty(release.ReleaseDate);
		project.ProfileName = project.ProfileName == "New package" ? release.Repository + " " + release.Version : project.ProfileName;
	}

	private async Task LoadManifestFolderAsync(string selectedPath)
	{
		try
		{
			SetBusy(true, "Reading manifest files...");
			ManifestProject loadedProject = await Task.Run(() => ManifestService.LoadProject(selectedPath), operationCancellation!.Token);
			if (!loadedProject.LoadedFromExistingManifests)
				throw new InvalidDataException("No Winget manifest YAML files were found. Choose the package or version folder that contains the version, installer, and locale YAML files.");
			project = loadedProject;
			ApplyProjectToControls();
			MarkProjectClean();
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
		if (!ConfirmSaveOrDiscardChanges("open another profile")) return;
		string[] selectedPaths = await OpenFilesAsync(
			"Open Winget Studio Profile",
			fields.GetValueOrDefault("ManifestFolder")?.Text,
			[".json"],
			false);
		if (selectedPaths.Length == 0) return;
		try
		{
			project = ProfileStore.Load(selectedPaths[0]);
			ApplyProjectToControls();
			MarkProjectClean();
			int missingFiles = project.Installers.Count(item => !string.IsNullOrWhiteSpace(item.LocalFile) && !File.Exists(item.LocalFile));
			SetStatus(missingFiles == 0
				? "Profile loaded. Review the package details and installer rows before continuing."
				: $"Profile loaded on this computer. Reattach {missingFiles} missing local installer file(s); public URLs and saved metadata were kept.");
		}
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
		try { ProfileStore.Save(selectedPath, project); MarkProjectClean(); SetStatus("Profile saved. No GitHub token was included."); }
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
			InstallerArtifact item = new() { LocalFile = file, VerificationStatus = "Waiting for inspection" };
			project.Installers.Add(item);
			added.Add(item);
		}
		foreach (InstallerArtifact item in added)
			await InspectInstallerAsync(item, allowRemoteDownload: false);
	}

	private void AddUrlInstaller()
	{
		project.Installers.Add(new InstallerArtifact { VerificationStatus = "URL entered manually • not inspected" });
		installerGrid.CurrentCell = installerGrid.Rows[^1].Cells[nameof(InstallerArtifact.InstallerUrl)];
		installerGrid.BeginEdit(true);
	}

	private void FocusInstallerUrlCell()
	{
		if (installerGrid.Rows.Count == 0)
		{
			SetStatus("Complete step 1 first: add the exact local release file, then enter its public download URL.");
			return;
		}
		int rowIndex = installerGrid.CurrentRow?.Index ?? 0;
		installerGrid.CurrentCell = installerGrid.Rows[rowIndex].Cells[nameof(InstallerArtifact.InstallerUrl)];
		installerGrid.Focus();
		installerGrid.BeginEdit(true);
		SetStatus("Step 2: paste the direct public HTTPS download URL for this exact release file.");
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
			item.Architecture = item.Architecture.IfEmpty(result.Architecture);
			item.InstallerType = item.InstallerType.IfEmpty(result.InstallerType);
			item.Scope = item.Scope.IfEmpty(result.Scope);
			item.NestedInstallerType = item.NestedInstallerType.IfEmpty(result.NestedInstallerType);
			item.NestedInstallerFiles = item.NestedInstallerFiles.IfEmpty(result.NestedInstallerFiles);
			item.ProductCode = result.ProductCode.IfEmpty(item.ProductCode);
			item.UpgradeCode = result.UpgradeCode.IfEmpty(item.UpgradeCode);
			item.ProductVersion = result.ProductVersion.IfEmpty(item.ProductVersion);
			item.DisplayName = result.DisplayName.IfEmpty(item.DisplayName);
			item.Publisher = result.Publisher.IfEmpty(item.Publisher);
			item.AnalysisSummary = $"{result.Technology}: {result.AnalysisNotes}";
			item.SignatureSha256 = result.SignatureSha256;
			ApplySignature(item, result.Signature);
			if (!string.IsNullOrWhiteSpace(result.SignatureSha256) && !result.Signature.IsSigned)
				item.SignatureStatus = "MSIX/APPX package signature present";
			item.VerificationStatus = File.Exists(item.LocalFile) ? "Verified from local release file" : "Calculated from temporary URL download";
			string versionNote = SynchronizePackageVersionFromInstaller(item, result.ProductVersion);
			if (string.IsNullOrWhiteSpace(Read("PackageName")) && !string.IsNullOrWhiteSpace(result.DisplayName))
				fields["PackageName"].Text = result.DisplayName;
			if (string.IsNullOrWhiteSpace(Read("Publisher")) && !string.IsNullOrWhiteSpace(result.Publisher))
				fields["Publisher"].Text = result.Publisher;
			if (result.InstallerType.Equals("exe", StringComparison.OrdinalIgnoreCase))
			{
				if (string.IsNullOrWhiteSpace(Read("SwitchSilent")) && !string.IsNullOrWhiteSpace(result.SuggestedSilentSwitch))
					fields["SwitchSilent"].Text = result.SuggestedSilentSwitch;
				if (string.IsNullOrWhiteSpace(Read("SwitchSilentWithProgress")) && !string.IsNullOrWhiteSpace(result.SuggestedSilentWithProgressSwitch))
					fields["SwitchSilentWithProgress"].Text = result.SuggestedSilentWithProgressSwitch;
				if (string.IsNullOrWhiteSpace(Read("SwitchInstallLocation")) && !string.IsNullOrWhiteSpace(result.SuggestedInstallLocationSwitch))
					fields["SwitchInstallLocation"].Text = result.SuggestedInstallLocationSwitch;
				if (string.IsNullOrWhiteSpace(Read("InstallModes")) && !string.IsNullOrWhiteSpace(result.SuggestedSilentSwitch))
					fields["InstallModes"].Text = "silent, silentWithProgress";
			}
			string zipNote = result.InstallerType.Equals("zip", StringComparison.OrdinalIgnoreCase)
				? string.IsNullOrWhiteSpace(result.NestedInstallerFiles)
					? " No supported installer file was found inside the ZIP; enter its contents in the ZIP CONTENTS column."
					: $" Found {ManifestService.ParseNestedInstallerFiles(result.NestedInstallerFiles).Count} installer file(s) inside the ZIP."
				: string.Empty;
			string choiceNote = (!item.Architecture.Equals(result.Architecture, StringComparison.OrdinalIgnoreCase)
				|| !item.InstallerType.Equals(result.InstallerType, StringComparison.OrdinalIgnoreCase))
				? $" The row kept your choices ({item.Architecture}, {item.InstallerType}); file inspection suggested {result.Architecture}, {result.InstallerType}."
				: string.Empty;
			SetStatus($"Inspected {Path.GetFileName(item.LocalFile.IfEmpty(item.InstallerUrl))}: {FormatSize(result.FileSize)}, {item.Architecture}, {item.InstallerType}, {result.Technology}.{zipNote}{choiceNote}{versionNote}");
		}
		catch (OperationCanceledException) { SetStatus("Installer inspection cancelled."); }
		catch (Exception ex) { ShowError("Installer inspection failed", ex); }
		finally { SetBusy(false); installerGrid.Refresh(); }
	}

	private string SynchronizePackageVersionFromInstaller(InstallerArtifact inspectedInstaller, string inspectedVersion)
	{
		string newVersion = inspectedVersion.Trim().TrimStart('v', 'V');
		if (string.IsNullOrWhiteSpace(newVersion)) return string.Empty;
		string oldVersion = fields["PackageVersion"].Text.Trim().TrimStart('v', 'V');
		if (string.Equals(oldVersion, newVersion, StringComparison.OrdinalIgnoreCase)) return string.Empty;
		bool conflictingInstaller = project.Installers
			.Where(installer => !ReferenceEquals(installer, inspectedInstaller))
			.Select(installer => installer.ProductVersion.Trim().TrimStart('v', 'V'))
			.Any(version => !string.IsNullOrWhiteSpace(version) && !string.Equals(version, newVersion, StringComparison.OrdinalIgnoreCase));
		if (conflictingInstaller)
			return $" This file reports version {newVersion}, but another installer reports a different version; the package version was not changed.";
		if (!string.IsNullOrWhiteSpace(oldVersion))
			return $" This file reports version {newVersion}; your package version {oldVersion} was kept so an inspection never changes release data you entered.";

		fields["PackageVersion"].Text = newVersion;
		project.PackageVersion = newVersion;
		return $" Package version was filled with {newVersion}.";
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
			StringBuilder preview = new("TECHNICAL YAML PREVIEW — ADVANCED\r\nReturn to the beginner-friendly summary with Show Plain-Language Review.\r\n");
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
		summary.AppendLine().Append("Technical YAML is hidden. Use Show Technical YAML only when you want to inspect it.");
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
			message.AppendLine("Winget did not identify one simple field name. Choose Show Technical YAML and read the first Manifest Error.");
		else
		{
			int number = 1;
			foreach (string field in fields) message.AppendLine($"{number++}. {FriendlyValidationField(field)}");
		}
		message.AppendLine().AppendLine("NEXT STEP");
		message.AppendLine("Correct the listed field on 2 Package or 3 Installers, then repeat Preview Changes → Save Manifests → Validate Locally.");
		message.AppendLine().Append("The complete Winget error is available under Show Technical YAML.");
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
			SetInterfaceText(previewModeButton, "Show plain-language review");
			previewModeButton.AccessibleName = previewModeButton.Text;
		}
	}

	private void ShowSimplePreview()
	{
		showingTechnicalPreview = false;
		previewBox.Text = simplePreviewText;
		SetInterfaceText(previewModeButton, "Show technical YAML");
		previewModeButton.AccessibleName = previewModeButton.Text;
	}

	private bool SaveManifests()
	{
		if (uiTestMode)
		{
			SetReviewProgress(ReviewProgress.Saved);
			simplePreviewText = "SAVED SAFELY\r\n\r\n[OK] The manifests were saved.\r\n[OK] Any replaced files were backed up first.\r\n\r\nNEXT: Click Validate Locally.";
			ShowSimplePreview();
			MarkProjectClean();
			SetStatus("TEST: Save Manifests completed safely without writing files.");
			return true;
		}
		try
		{
			ReadProjectFromControls();
			ManifestGenerationResult result = ManifestService.Generate(project);
			ManifestService.Save(project, result);
			project.LoadedFromExistingManifests = true;
			MarkProjectClean();
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
			bool validationSucceeded = WingetCommandService.ManifestValidationSucceeded(result);
			bool hasWarnings = validationSucceeded && result.ExitCode != 0;
			if (validationSucceeded)
			{
				simplePreviewText = hasWarnings
					? "VALIDATION PASSED WITH WARNINGS\r\n\r\n[OK] Microsoft's Winget validator accepted the generated manifests.\r\n[CHECK] Review the warning in Show technical YAML. Restricted fields may require a verified publisher.\r\n[OK] No files were changed during validation.\r\n\r\nNEXT: Open 5 Test Center and run Safe Preflight, then test the installation."
					: "VALIDATION PASSED — NOTHING NEEDS FIXING\r\n\r\n[OK] Microsoft's Winget validator accepted the generated manifests.\r\n[OK] No files were changed during validation.\r\n\r\nNEXT: Open 5 Test Center and run Safe Preflight, then test the installation.";
				SetReviewProgress(ReviewProgress.Validated);
			}
			else
			{
				simplePreviewText = BuildValidationFailureSummary(result.CombinedOutput);
				SetReviewProgress(ReviewProgress.ValidationFailed);
			}
			ShowSimplePreview();
			SetStatus(validationSucceeded
				? (hasWarnings ? "Official Winget validation passed with warnings. Review them, then open Test Center." : "Official Winget validation passed. Next, open Test Center.")
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
		if (!await RefreshTestEnvironmentAsync(showReport: false))
		{
			testOutputBox.Text = "STEP 1 CANNOT RUN\r\n\r\n" + (wingetHealth?.Message ?? "Windows Package Manager is not ready.")
				+ "\r\n\r\nNEXT: Choose Check Test Setup and follow the repair instructions.";
			latestTestReport = testOutputBox.Text;
			SelectTab("Test Center");
			return false;
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
			bool validationSucceeded = WingetCommandService.ManifestValidationSucceeded(validation);
			if (!validationSucceeded) criticalFailure = true;
			report.AppendLine($"{(validationSucceeded ? validation.ExitCode == 0 ? "PASS" : "WARN" : "FAIL")}  Official winget validate: exit code {validation.ExitCode}.");
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
			report.AppendLine().AppendLine(validationSucceeded
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
			StringBuilder report = new("DIGITAL SIGNATURE RESULTS\r\n\r\nSigned software identifies its publisher. Unsigned is a completed result, not a test still running.\r\n\r\n");
			int unsignedCount = 0;
			int signedCount = 0;
			for (int index = 0; index < project.Installers.Count; index++)
			{
				InstallerArtifact item = project.Installers[index];
				if (!File.Exists(item.LocalFile))
				{
					item.SignatureStatus = "Not checked — attach local file";
					item.SignerName = string.Empty;
					item.SignerThumbprint = string.Empty;
					item.SignatureExpiration = string.Empty;
					report.AppendLine($"Installer {index + 1}: no local file is attached.");
					continue;
				}
				AuthenticodeInspection signature;
				if (Path.GetExtension(item.LocalFile).Equals(".msix", StringComparison.OrdinalIgnoreCase)
					|| Path.GetExtension(item.LocalFile).Equals(".appx", StringComparison.OrdinalIgnoreCase)
					|| Path.GetExtension(item.LocalFile).Equals(".msixbundle", StringComparison.OrdinalIgnoreCase)
					|| Path.GetExtension(item.LocalFile).Equals(".appxbundle", StringComparison.OrdinalIgnoreCase))
				{
					InstallerInspection inspection = await InstallerInspector.InspectAsync(item.LocalFile, string.Empty, null, operationCancellation!.Token);
					signature = inspection.Signature;
					item.SignatureSha256 = inspection.SignatureSha256;
				}
				else
				{
					signature = await Task.Run(() => AuthenticodeInspector.Inspect(item.LocalFile), operationCancellation!.Token);
					item.SignatureSha256 = string.Empty;
				}
				ApplySignature(item, signature);
				if (!string.IsNullOrWhiteSpace(item.SignatureSha256) && !signature.IsSigned)
					item.SignatureStatus = "MSIX/APPX package signature present";
				if (signature.IsSigned || !string.IsNullOrWhiteSpace(item.SignatureSha256)) signedCount++; else if (signature.Status == "Unsigned") unsignedCount++;
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
			SetStatus($"Digital signature inspection finished: {signedCount} signed, {unsignedCount} unsigned.");
		}
		catch (OperationCanceledException) { SetStatus("Digital signature inspection was cancelled."); }
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

	private async Task<bool> RefreshTestEnvironmentAsync(bool showReport)
	{
		if (uiTestMode)
		{
			wingetHealth = new WingetHealthResult(true, "Safe UI test", 0, "Windows Package Manager check was simulated safely.", localManifestFilesEnabled);
			UpdateTestPlanStatus();
			if (showReport) SetStatus("TEST: Winget setup check completed without running an external command.");
			return true;
		}
		if (!showReport && wingetHealth is not null && DateTimeOffset.Now - wingetHealthCheckedAt < TimeSpan.FromSeconds(30))
		{
			UpdateTestPlanStatus();
			return wingetHealth.IsReady;
		}
		if (testEnvironmentCheckRunning)
		{
			while (testEnvironmentCheckRunning) await Task.Delay(100);
			return wingetHealth?.IsReady == true;
		}

		testEnvironmentCheckRunning = true;
		try
		{
			SetStatus("Checking Windows Package Manager before testing...");
			wingetHealth = await WingetCommandService.CheckWingetHealthAsync();
			localManifestFilesEnabled = localManifestFilesEnabled || wingetHealth.LocalManifestFilesEnabled;
			wingetHealthCheckedAt = DateTimeOffset.Now;
			UpdateTestPlanStatus();
			if (showReport)
			{
				testOutputBox.Text = "TEST SETUP CHECK\r\n\r\n"
					+ $"Winget: {(wingetHealth.IsReady ? "READY" : "NOT READY")}\r\n"
					+ (wingetHealth.Version.Length > 0 ? "Version: " + wingetHealth.Version + "\r\n" : string.Empty)
					+ "Local manifest setting: " + (localManifestFilesEnabled ? "ENABLED" : "NOT ENABLED") + "\r\n\r\n"
					+ wingetHealth.Message
					+ (wingetHealth.IsReady ? "\r\n\r\nNEXT: Run step 1, then follow the numbered buttons." : "\r\n\r\nNEXT: Open Windows Settings > Apps > Installed apps > App Installer > Advanced options, choose Repair, install Microsoft Store updates, and run Check Test Setup again.");
				latestTestReport = testOutputBox.Text;
				SelectTab("Test Center");
			}
			SetStatus(wingetHealth.IsReady ? "Winget is ready for manifest testing." : "Winget is not ready. The Test Center explains how to repair App Installer.");
			return wingetHealth.IsReady;
		}
		finally
		{
			testEnvironmentCheckRunning = false;
		}
	}

	private void UpdateTestPlanStatus()
	{
		if (testPlanLabel is null || testPlanLabel.IsDisposed || fields.Count == 0) return;
		project.EnsureInstallerCollection();
		List<string> errors = ManifestService.Validate(project);
		string fingerprint = ProjectFingerprint();
		bool projectReady = errors.Count == 0;
		bool preflightReady = string.Equals(successfulPreflightFingerprint, fingerprint, StringComparison.Ordinal);
		bool localTestingEnabled = localManifestFilesEnabled;
		bool installPassed = string.Equals(successfulLocalInstallFingerprint, fingerprint, StringComparison.Ordinal);
		bool installedVerified = string.Equals(verifiedInstalledFingerprint, fingerprint, StringComparison.Ordinal);
		bool wingetReady = wingetHealth is not { IsReady: false };
		string wingetState = wingetHealth is null
			? T("WINGET NOT CHECKED")
			: wingetHealth.IsReady
				? T("WINGET READY") + (wingetHealth.Version.Length > 0 ? " · " + wingetHealth.Version : string.Empty)
				: T("WINGET NEEDS ATTENTION");
		string projectState = projectReady
			? T("PROJECT READY")
			: string.Format(T(errors.Count == 1 ? "PROJECT NEEDS {0} FIX" : "PROJECT NEEDS {0} FIXES"), errors.Count);
		testPlanLabel.Text = $"{projectState}   •   {wingetState}";
		testPlanLabel.ForeColor = projectReady && wingetReady ? AccentColor : StudioPalette.Warning;

		bool[] complete = [preflightReady, localTestingEnabled, installPassed, installedVerified];
		string[] completeText = [T("PASSED"), T("ENABLED"), T("INSTALLED"), T("VERIFIED")];
		int currentStep = !preflightReady ? 0 : !localTestingEnabled ? 1 : !installPassed ? 2 : !installedVerified ? 3 : 4;
		for (int index = 0; index < testProgressSteps.Length; index++)
		{
			StudioStepState state = complete[index]
				? StudioStepState.Complete
				: index == currentStep
					? (!projectReady || !wingetReady ? StudioStepState.Problem : StudioStepState.Current)
					: StudioStepState.Pending;
			testProgressSteps[index].State = state;
			testProgressSteps[index].StatusText = complete[index] ? completeText[index] : index == currentStep ? state == StudioStepState.Problem ? T("NEEDS ATTENTION") : T("NEXT") : T("WAITING");
			testProgressSteps[index].AccessibleDescription = testProgressSteps[index].StatusText;
			if (index < testStatusPills.Length)
			{
				testStatusPills[index].State = state;
				testStatusPills[index].Text = complete[index] ? completeText[index] : index == currentStep ? state == StudioStepState.Problem ? T("FIX FIRST") : T("NEXT") : T("WAITING");
				testStatusPills[index].AccessibleName = $"Step {index + 1} status: {testStatusPills[index].Text}";
			}
		}

		if (!projectReady)
		{
			nextTestActionTitleLabel.Text = "Fix the package information";
			nextTestActionDescriptionLabel.Text = SimplifyReadinessError(errors[0]) + ". The Studio will return you to the correct page.";
			nextTestActionSafetyLabel.Text = "REQUIRED · Testing stays locked until this is corrected";
			nextTestActionSafetyLabel.ForeColor = StudioPalette.Warning;
			nextTestActionButton.Text = "Open the field to fix";
			nextTestActionButton.Tag = "fix-project";
		}
		else if (!wingetReady)
		{
			nextTestActionTitleLabel.Text = "Repair the Winget test setup";
			nextTestActionDescriptionLabel.Text = "Windows Package Manager is not ready. Run the setup check to see the exact repair instructions.";
			nextTestActionSafetyLabel.Text = "SAFE · This only checks Winget and changes nothing";
			nextTestActionSafetyLabel.ForeColor = StudioPalette.Warning;
			nextTestActionButton.Text = "Check Winget setup";
			nextTestActionButton.Tag = "health";
		}
		else if (!preflightReady)
		{
			nextTestActionTitleLabel.Text = "Run safe preflight";
			nextTestActionDescriptionLabel.Text = "Checks YAML, file hashes, signatures, official Winget validation, and whether this package already exists.";
			nextTestActionSafetyLabel.Text = "SAFE · Nothing will be installed or changed";
			nextTestActionSafetyLabel.ForeColor = SuccessColor;
			nextTestActionButton.Text = "Run safe preflight";
			nextTestActionButton.Tag = "preflight";
		}
		else if (!localTestingEnabled)
		{
			nextTestActionTitleLabel.Text = "Allow local manifest testing";
			nextTestActionDescriptionLabel.Text = "Windows requires one administrator approval before Winget can install a manifest from this computer.";
			nextTestActionSafetyLabel.Text = "ONE-TIME SETUP · Approve the Windows prompt";
			nextTestActionSafetyLabel.ForeColor = StudioPalette.Warning;
			nextTestActionButton.Text = "Enable local testing";
			nextTestActionButton.Tag = "local-testing";
		}
		else if (!installPassed)
		{
			nextTestActionTitleLabel.Text = "Test install this release";
			nextTestActionDescriptionLabel.Text = "Runs winget install --manifest with the exact generated files. Review the installer console, then close it.";
			nextTestActionSafetyLabel.Text = "CONFIRMATION REQUIRED · This installs software on this PC";
			nextTestActionSafetyLabel.ForeColor = StudioPalette.Warning;
			nextTestActionButton.Text = "Test install here";
			nextTestActionButton.Tag = "install";
		}
		else if (!installedVerified)
		{
			nextTestActionTitleLabel.Text = "Confirm the installed result";
			nextTestActionDescriptionLabel.Text = "Checks the Winget package ID, then the MSI identity or installed application name when needed.";
			nextTestActionSafetyLabel.Text = "SAFE · Verification does not reinstall the package";
			nextTestActionSafetyLabel.ForeColor = SuccessColor;
			nextTestActionButton.Text = "Verify installation";
			nextTestActionButton.Tag = "verify";
		}
		else
		{
			nextTestActionTitleLabel.Text = "All tests passed — ready to submit";
			nextTestActionDescriptionLabel.Text = "Start Microsoft's official WingetCreate submission without returning to the Review page.";
			nextTestActionSafetyLabel.Text = "READY · WingetCreate handles sign-in and pull-request creation";
			nextTestActionSafetyLabel.ForeColor = SuccessColor;
			nextTestActionButton.Text = "Submit to Winget";
			nextTestActionButton.Tag = "submit";
		}

		LocalizeDynamicControls(nextTestActionTitleLabel, nextTestActionDescriptionLabel, nextTestActionSafetyLabel, nextTestActionButton);
		if (!projectReady) SetLocalizedReadinessError(nextTestActionDescriptionLabel, errors[0]);
		nextTestActionButton.Enabled = !isBusy && (projectReady || string.Equals(nextTestActionButton.Tag as string, "fix-project", StringComparison.Ordinal));
		nextTestActionButton.AccessibleName = nextTestActionButton.Text;
		if (nextTestActionButton is StudioButton studioButton) studioButton.ButtonKind = StudioButtonKind.Primary;
		bool currentReview = projectReady && string.Equals(reviewFingerprint, fingerprint, StringComparison.Ordinal);
		UpdateReviewWorkflowStatus(errors, fingerprint, currentReview);
	}

	private async Task RunNextTestActionAsync()
	{
		switch (nextTestActionButton.Tag as string)
		{
			case "fix-project":
				List<string> errors = ManifestService.Validate(project);
				string first = errors.FirstOrDefault() ?? string.Empty;
				SelectTab(first.Contains("installer", StringComparison.OrdinalIgnoreCase)
					|| first.Contains("hash", StringComparison.OrdinalIgnoreCase)
					|| first.Contains("architecture", StringComparison.OrdinalIgnoreCase)
					? "Installers & Hashes" : "Package Details");
				break;
			case "health": await RefreshTestEnvironmentAsync(showReport: true); break;
			case "preflight": await RunSafePreflightAsync(); break;
			case "local-testing": await EnableLocalManifestTestingAsync(); break;
			case "install": await TestInstallHereAsync(); break;
			case "verify": await VerifyInstalledResultAsync(); break;
			case "submit": await SubmitAsync(); break;
		}
	}

	private async Task<bool> EnableLocalManifestTestingAsync(bool askForConfirmation = true)
	{
		if (uiTestMode)
		{
			localManifestFilesEnabled = true;
			UpdateTestPlanStatus();
			SetStatus("TEST: Administrator settings were not opened.");
			return true;
		}
		if (!await RefreshTestEnvironmentAsync(showReport: false))
		{
			testOutputBox.Text = "LOCAL TESTING CANNOT BE ENABLED YET\r\n\r\n" + (wingetHealth?.Message ?? "Windows Package Manager is not ready.")
				+ "\r\n\r\nChoose Check Test Setup for repair instructions.";
			SelectTab("Test Center");
			return false;
		}
		if (localManifestFilesEnabled)
		{
			UpdateTestPlanStatus();
			SetStatus("Local manifest testing is already enabled. Continue with step 3.");
			return true;
		}
		if (askForConfirmation)
		{
			DialogResult answer = MessageBox.Show(this,
				"Winget requires one Windows administrator approval to enable local manifest testing. No PowerShell window will open. The Studio will run Winget directly in the background and show the result here. Continue?",
				"Step 2 — Enable local manifest testing", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (answer != DialogResult.Yes) return false;
		}

		try
		{
			SetBusy(true, "Waiting for the one-time Winget administrator setting...");
			testOutputBox.Text = "STEP 2 — ENABLING LOCAL TESTING\r\n\r\nApprove the Windows administrator prompt. Winget is running directly in the background; there is no PowerShell window to wait on.";
			SelectTab("Test Center");
			using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(operationCancellation!.Token);
			timeout.CancelAfter(TimeSpan.FromSeconds(60));
			CommandResult result = await WingetCommandService.EnableLocalManifestFilesElevatedAsync(timeout.Token);
			localManifestFilesEnabled = result.ExitCode == 0;
			if (localManifestFilesEnabled)
			{
				wingetHealth = await WingetCommandService.CheckWingetHealthAsync(timeout.Token);
				wingetHealthCheckedAt = DateTimeOffset.Now;
				localManifestFilesEnabled = localManifestFilesEnabled || wingetHealth.LocalManifestFilesEnabled;
			}
			testOutputBox.Text = "STEP 2 — LOCAL TESTING RESULT\r\n\r\n"
				+ (localManifestFilesEnabled ? "PASS: Winget LocalManifestFiles is enabled. Step 2 is complete." : "FAIL: Winget did not enable LocalManifestFiles.")
				+ "\r\n\r\n" + result.CombinedOutput
				+ (localManifestFilesEnabled ? "\r\n\r\nNEXT: Choose 3 Test Install Here." : "\r\n\r\nChoose Check Test Setup. If Windows requested credentials for a different administrator account, Winget cannot apply this setting to the non-administrator account.");
			latestTestReport = testOutputBox.Text;
			UpdateTestPlanStatus();
			SetStatus(localManifestFilesEnabled ? "Local manifest testing is enabled. Continue with step 3." : "Local testing was not enabled. Review the exact result in Test Center.");
			return localManifestFilesEnabled;
		}
		catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
		{
			SetStatus("Administrator confirmation was cancelled. No setting was changed.");
			return false;
		}
		catch (OperationCanceledException)
		{
			testOutputBox.Text = "STEP 2 FAILED\r\n\r\nWinget did not finish the local-testing setting within one minute. Choose Check Test Setup for a specific Winget diagnosis.";
			SetStatus("Winget did not finish enabling local testing.");
			return false;
		}
		catch (Exception ex)
		{
			ShowError("Local manifest testing could not be enabled", ex);
			return false;
		}
		finally
		{
			SetBusy(false);
			UpdateTestPlanStatus();
		}
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
		if (!await RefreshTestEnvironmentAsync(showReport: false))
		{
			testOutputBox.Text = "STEP 3 CANNOT START\r\n\r\n" + (wingetHealth?.Message ?? "Windows Package Manager is not ready.")
				+ "\r\n\r\nNEXT: Choose Check Test Setup and follow the repair instructions.";
			SelectTab("Test Center");
			return;
		}
		string fingerprint = ProjectFingerprint();
		if (!string.Equals(successfulPreflightFingerprint, fingerprint, StringComparison.Ordinal))
		{
			DialogResult runPreflight = MessageBox.Show(this,
				"Step 1 Safe Preflight has not passed for the current project. Run it now before the installation test? Nothing is installed during Safe Preflight.",
				"Complete step 1 first", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
			if (runPreflight != DialogResult.Yes || !await RunSafePreflightAsync()) return;
			ReadProjectFromControls();
			fingerprint = ProjectFingerprint();
		}
		if (!localManifestFilesEnabled)
		{
			DialogResult enable = MessageBox.Show(this,
				"Step 2 Local Testing is not enabled yet. Enable it now, verify the result automatically, and then continue this installation test?",
				"Complete step 2 now", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
			if (enable != DialogResult.Yes || !await EnableLocalManifestTestingAsync(askForConfirmation: false)) return;
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
			string testedFingerprint = ProjectFingerprint();
			testOutputBox.Text = $"LOCAL INSTALL TEST STARTED\r\n\r\nA persistent console is running winget install --manifest. Answer any installer or elevation prompts there.\r\nProcess ID: {session.ProcessId}";
			SelectTab("Test Center");
			SetStatus("Local install test opened in a persistent console.");
			_ = MonitorTestSessionAsync(session, cleanFolder, "Local install test", testedFingerprint);
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
			if (!await RefreshTestEnvironmentAsync(showReport: false))
			{
				testOutputBox.Text = "STEP 4 CANNOT RUN\r\n\r\n" + (wingetHealth?.Message ?? "Windows Package Manager is not ready.");
				SelectTab("Test Center");
				return;
			}
			SetBusy(true, "Checking Winget and the installer's Windows identity...");
			InstalledPackageVerification result = await InstalledPackageVerifier.VerifyAsync(project, operationCancellation!.Token);
			bool verified = result.Found && result.VersionMatches;
			verifiedInstalledFingerprint = verified ? ProjectFingerprint() : string.Empty;
			latestTestReport = "STEP 4 — INSTALLED RESULT\r\n\r\n"
				+ (result.Found ? "PASS: The installed application was found." : "FAIL: The installed application was not found.") + "\r\n"
				+ (result.VersionMatches
					? $"PASS: Installed version matches {project.PackageVersion}."
					: $"FAIL: Installed version {result.InstalledVersion.IfEmpty("was not reported")} does not match {project.PackageVersion}.") + "\r\n"
				+ $"Matched by: {result.Method}\r\n"
				+ (result.InstalledName.Length > 0 ? "Installed name: " + result.InstalledName + "\r\n" : string.Empty)
				+ (result.InstalledVersion.Length > 0 ? "Installed version: " + result.InstalledVersion + "\r\n" : string.Empty)
				+ "\r\n" + result.Diagnostic
				+ (verified ? "\r\n\r\nALL FOUR TESTS PASSED. Choose Submit to Winget on this page." : "\r\n\r\nReview the installed name and version above, then correct the manifest or installer before submitting.");
			testOutputBox.Text = latestTestReport;
			SelectTab("Test Center");
			SetStatus(verified ? "All required tests passed. Submit to Winget directly from Test Center." : "The installed result needs review.");
			UpdateTestPlanStatus();
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
		if (!await RefreshTestEnvironmentAsync(showReport: false))
		{
			testOutputBox.Text = "SANDBOX TEST CANNOT START\r\n\r\n" + (wingetHealth?.Message ?? "Windows Package Manager is not ready.");
			SelectTab("Test Center");
			return;
		}
		ReadProjectFromControls();
		if (ShowSandboxElevationConflict()) return;
		if (!WingetCommandService.IsWindowsSandboxAvailable())
		{
			MessageBox.Show(this, "Windows Sandbox is not available. Enable the Windows Sandbox optional feature in Windows Features, restart if requested, and try again.", "Windows Sandbox unavailable", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}
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
			_ = MonitorTestSessionAsync(session, cleanFolder, "Windows Sandbox test", ProjectFingerprint());
			cleanFolder = null;
		}
		catch (Exception ex) { ShowError("The Windows Sandbox test could not start", ex); }
		finally
		{
			try { ManifestService.DeleteCleanManifestFolder(cleanFolder); } catch { }
			SetBusy(false);
		}
	}

	private async Task TestInstallAndUninstallInSandboxAsync()
	{
		if (uiTestMode)
		{
			testOutputBox.Text = "SAFE UI TEST: The Sandbox install-and-uninstall cycle was intentionally not launched.";
			SetStatus("TEST: Sandbox install-and-uninstall test completed safely without opening a window.");
			return;
		}
		if (!await RefreshTestEnvironmentAsync(showReport: false))
		{
			testOutputBox.Text = "SANDBOX INSTALL + UNINSTALL CANNOT START\r\n\r\n" + (wingetHealth?.Message ?? "Windows Package Manager is not ready.");
			SelectTab("Test Center");
			return;
		}
		ReadProjectFromControls();
		if (ShowSandboxElevationConflict()) return;
		if (!WingetCommandService.IsWindowsSandboxAvailable())
		{
			MessageBox.Show(this, "Windows Sandbox is not available. Enable the Windows Sandbox optional feature in Windows Features, restart if requested, and try again.", "Windows Sandbox unavailable", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}
		DialogResult answer = MessageBox.Show(this,
			$"This disposable Sandbox test will:\r\n\r\n1. Install {project.PackageIdentifier} {project.PackageVersion}.\r\n2. Confirm its Winget, Apps & Features, or MSIX identity.\r\n3. Uninstall it through Winget.\r\n4. Confirm that identity was removed.\r\n\r\nYour real Windows installation is not changed. Continue?",
			"Sandbox install and uninstall test", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
		if (answer != DialogResult.Yes) return;
		string? cleanFolder = null;
		try
		{
			SetBusy(true, "Validating and preparing the Sandbox install-and-uninstall test...");
			cleanFolder = await CreateValidatedTestFolderAsync();
			string script = await OfficialTestAssets.GetSandboxTestScriptAsync(operationCancellation!.Token);
			InteractiveCommandSession session = WingetCommandService.StartSandboxInstallUninstallTestSession(script, cleanFolder, project);
			testOutputBox.Text = $"SANDBOX INSTALL + UNINSTALL STARTED\r\n\r\nMicrosoft's official SandboxTest.ps1 is preparing a disposable Windows environment. It will install the manifest, locate the installed identity, uninstall it through Winget, and confirm removal.\r\n\r\nKeep the Sandbox open until the green PASS or red FAIL result appears, then close it.\r\n\r\nOfficial source: {OfficialTestAssets.SandboxTestSource}\r\nProcess ID: {session.ProcessId}";
			SelectTab("Test Center");
			SetStatus("The Sandbox install-and-uninstall test is running. Keep the Sandbox open until its final result appears.");
			_ = MonitorTestSessionAsync(session, cleanFolder, "Sandbox install and uninstall test", ProjectFingerprint());
			cleanFolder = null;
		}
		catch (Exception ex) { ShowError("The Sandbox install-and-uninstall test could not start", ex); }
		finally
		{
			try { ManifestService.DeleteCleanManifestFolder(cleanFolder); } catch { }
			SetBusy(false);
		}
	}

	private bool ShowSandboxElevationConflict()
	{
		if (!WingetCommandService.HasSandboxElevationConflict(project)) return false;

		bool spanish = currentInterfaceLanguage.Equals("es-ES", StringComparison.OrdinalIgnoreCase);
		string report = spanish
			? "WINDOWS SANDBOX NO PUEDE EJECUTAR ESTE MANIFIESTO\r\n\r\n"
				+ "POR QUÉ\r\nLa elevación está configurada como elevationProhibited. Winget debe bloquear este instalador desde una ventana de Administrador. La prueba Sandbox de Microsoft ejecuta Winget como Administrador, por lo que el instalador nunca comenzará.\r\n\r\n"
				+ "QUÉ HACER\r\n1. Si el instalador puede ejecutarse como administrador, abre 3 Instaladores y cambia Elevación al comportamiento real: vacío, elevatesSelf o elevationRequired.\r\n2. Si el instalador realmente nunca puede ejecutarse como administrador, conserva elevationProhibited y usa Probar instalación aquí con Studio ejecutándose normalmente. La prueba Sandbox actual de Microsoft no puede probar exactamente este tipo de paquete.\r\n\r\nNo cambies el campo solo para hacer pasar una prueba. Debe describir el comportamiento real del instalador."
			: "WINDOWS SANDBOX CANNOT RUN THIS MANIFEST\r\n\r\n"
				+ "WHY\r\nElevation is set to elevationProhibited. Winget must block this installer from an Administrator window. Microsoft's Sandbox test runs Winget as Administrator, so the installer can never start there.\r\n\r\n"
				+ "WHAT TO DO\r\n1. If the installer can run as administrator, open 3 Installers and set Elevation to its real behavior: blank, elevatesSelf, or elevationRequired.\r\n2. If the installer truly must never run as administrator, keep elevationProhibited and use Test Install Here while the Studio is running normally. Microsoft's current Sandbox test cannot accurately test this package type.\r\n\r\nDo not change the field only to make a test pass. It must describe the installer's real behavior.";
		latestTestReport = report;
		testOutputBox.Text = report;
		SelectTab("Test Center");
		SetStatus(spanish
			? "Sandbox ejecuta Winget como Administrador y no admite elevationProhibited."
			: "Sandbox runs Winget as Administrator and cannot test elevationProhibited installers.");

		DialogResult openInstallers = MessageBox.Show(this,
			report + (spanish ? "\r\n\r\n¿Abrir 3 Instaladores ahora?" : "\r\n\r\nOpen 3 Installers now?"),
			spanish ? "Conflicto de elevación en Sandbox" : "Sandbox elevation conflict",
			MessageBoxButtons.YesNo,
			MessageBoxIcon.Warning);
		if (openInstallers == DialogResult.Yes)
		{
			SelectTab("Installers & Hashes");
			if (fields.TryGetValue("ElevationRequirement", out Control? elevationField)) elevationField.Focus();
		}
		return true;
	}

	private async Task<string> CreateValidatedTestFolderAsync()
	{
		ManifestGenerationResult generated = ManifestService.Generate(project);
		string cleanFolder = ManifestService.CreateCleanManifestFolder(generated);
		try
		{
			CommandResult validation = await WingetCommandService.ValidateManifestAsync(cleanFolder, operationCancellation!.Token);
			if (!WingetCommandService.ManifestValidationSucceeded(validation))
				throw new InvalidDataException("Official Winget validation failed. Run Safe Preflight and correct the reported fields before testing installation.\r\n\r\n" + validation.CombinedOutput);
			return cleanFolder;
		}
		catch
		{
			ManifestService.DeleteCleanManifestFolder(cleanFolder);
			throw;
		}
	}

	private async Task MonitorTestSessionAsync(InteractiveCommandSession session, string cleanFolder, string title, string testedFingerprint)
	{
		try
		{
			using Process process = Process.GetProcessById(session.ProcessId);
			await process.WaitForExitAsync();
			string output = File.Exists(session.LogPath) ? await File.ReadAllTextAsync(session.LogPath) : "The console did not produce a captured log.";
			if (IsDisposed || Disposing) return;
			string? sandboxResult = !string.IsNullOrWhiteSpace(session.ResultPath) && File.Exists(session.ResultPath)
				? await File.ReadAllTextAsync(session.ResultPath)
				: null;
			latestTestReport = $"{title.ToUpperInvariant()} RESULT\r\n\r\nExit code: {process.ExitCode}\r\n\r\n"
				+ (sandboxResult is null
					? output
					: sandboxResult + "\r\n\r\nHOST LAUNCHER OUTPUT\r\n" + output);
			if (title.Equals("Local install test", StringComparison.OrdinalIgnoreCase))
				successfulLocalInstallFingerprint = process.ExitCode == 0 ? testedFingerprint : string.Empty;
			testOutputBox.Text = latestTestReport;
			SelectTab("Test Center");
			if (!string.IsNullOrWhiteSpace(session.ResultPath))
			{
				bool passed = sandboxResult?.Contains("STATUS=PASS", StringComparison.OrdinalIgnoreCase) == true;
				SetStatus(passed
					? "The Sandbox install-and-uninstall test passed. The package installed and was removed successfully."
					: sandboxResult is null
						? "The Sandbox closed before a final install-and-uninstall result was saved. Run the test again and wait for PASS or FAIL."
						: "The Sandbox install-and-uninstall test needs attention. Review the captured result.");
			}
			else
				SetStatus(process.ExitCode == 0 ? $"{title} completed successfully. Review the captured result." : $"{title} exited with code {process.ExitCode}. Review the captured result.");
			UpdateTestPlanStatus();
		}
		catch (Exception ex)
		{
			if (!IsDisposed && !Disposing) SetStatus($"{title} closed, but its final result could not be captured: {ex.Message}");
		}
		finally
		{
			try { if (File.Exists(session.LogPath)) File.Delete(session.LogPath); } catch { }
			try { if (!string.IsNullOrWhiteSpace(session.ResultPath) && File.Exists(session.ResultPath)) File.Delete(session.ResultPath); } catch { }
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
			bool cleanupTransferred = await RunToolAsync(
				"submit",
				QuoteArgument(cleanFolder),
				cleanFolder,
				cleanFolder);
			if (cleanupTransferred) cleanFolder = null;
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

	private async Task RunOfficialCommandAsync() => _ = await RunToolAsync(toolCommandBox.Text, toolArgumentsBox.Text);

	private async Task<bool> RunToolAsync(
		string command,
		string arguments,
		string? workingDirectory = null,
		string? cleanupFolderAfterInteractive = null)
	{
		if (uiTestMode)
		{
			toolOutputBox.Text = $"> wingetcreate {command} {arguments}\r\n\r\nSAFE UI TEST: No external process was launched.";
			SelectTab("Official Tool Commands");
			SetStatus("TEST: Official command completed safely without launching a process.");
			await Task.CompletedTask;
			return false;
		}
		if (!wingetCreateReady)
		{
			SetStatus("WingetCreate is still preparing. Local manifest tools remain available while it finishes.");
			return false;
		}
		bool cleanupTransferred = false;
		try
		{
			SetBusy(true);
			toolOutputBox.Text = $"> wingetcreate {command} {arguments}{Environment.NewLine}{Environment.NewLine}";
			SelectTab("Official Tool Commands");
			string commandFolder = string.IsNullOrWhiteSpace(workingDirectory) ? project.ManifestFolder : workingDirectory;
			if (WingetCommandService.RequiresInteractiveConsole(command, arguments))
			{
				InteractiveCommandSession session = WingetCommandService.StartWingetCreateInteractiveSession(
					command,
					arguments,
					commandFolder,
					cleanupFolderAfterInteractive);
				cleanupTransferred = !string.IsNullOrWhiteSpace(session.CleanupFolder);
				_ = MonitorInteractiveCommandAsync(session, command);
				toolOutputBox.AppendText(
					"WingetCreate opened in a persistent console because this command asks interactive questions."
					+ Environment.NewLine
					+ "Complete the questions in that console window. It will stay open so you can read any error. Manifest Studio remains available here."
					+ Environment.NewLine
					+ $"Process ID: {session.ProcessId}");
				SetStatus("WingetCreate opened an interactive console. Complete the questions there.");
				return cleanupTransferred;
			}
			CommandResult result = await WingetCommandService.RunWingetCreateAsync(
				command,
				arguments,
				commandFolder,
				operationCancellation!.Token);
			toolOutputBox.AppendText(result.CombinedOutput);
			SetStatus(result.ExitCode == 0 ? "WingetCreate completed successfully." : $"WingetCreate exited with code {result.ExitCode}.");
			return false;
		}
		catch (Exception ex)
		{
			ShowError("WingetCreate could not run", ex);
			return cleanupTransferred;
		}
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
			WingetCommandService.CleanupInteractiveCommandSessionArtifacts(session);
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
				SetModeText("LOCAL AUTHORING READY • LOADING WINGETCREATE");
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

	private void OpenBackupFolder()
	{
		if (uiTestMode)
		{
			SetStatus("TEST: Open Backup Folder completed safely without opening Explorer.");
			return;
		}
		ReadProjectFromControls();
		if (string.IsNullOrWhiteSpace(project.ManifestFolder))
		{
			MessageBox.Show(this, "Choose the manifest output folder first.", "Backup folder", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}
		string backupFolder = Path.Combine(project.ManifestFolder, ".manifest-backups");
		if (!Directory.Exists(backupFolder))
		{
			MessageBox.Show(this,
				"No backups exist for this project yet. The Studio creates a timestamped backup automatically the first time existing manifests are replaced.",
				"No backups yet", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}
		Process.Start(new ProcessStartInfo { FileName = backupFolder, UseShellExecute = true });
		SetStatus("Opened the recoverable manifest backups in File Explorer.");
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
		project.Agreements = Read("Agreements");
		project.Documentations = Read("Documentations");
		project.Channel = Read("Channel");
		project.InstallerLocale = Read("InstallerLocale");
		project.Platform = Read("Platform");
		project.MinimumOSVersion = Read("MinimumOSVersion");
		project.InstallerType = Read("InstallerType");
		project.NestedInstallerType = Read("NestedInstallerType");
		project.NestedInstallerFiles = Read("NestedInstallerFiles");
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
		project.PackageDependencies = Read("PackageDependencies");
		project.WindowsFeatures = Read("WindowsFeatures");
		project.Capabilities = Read("Capabilities");
		project.RestrictedCapabilities = Read("RestrictedCapabilities");
		project.Markets = Read("Markets");
		project.ExcludedMarkets = Read("ExcludedMarkets");
		project.ExpectedReturnCodes = Read("ExpectedReturnCodes");
		project.UnsupportedArguments = Read("UnsupportedArguments");
		project.DefaultInstallLocation = Read("DefaultInstallLocation");
		project.InstalledFiles = Read("InstalledFiles");
		project.AuthenticationType = Read("AuthenticationType");
		project.AuthenticationResource = Read("AuthenticationResource");
		project.AuthenticationScope = Read("AuthenticationScope");
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
		applyingProjectToControls = true;
		try
		{
			project.ManifestVersion = ManifestSchemaSupport.NormalizeKnownStudioVersion(project.ManifestVersion);
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
			Write("Agreements", project.Agreements);
			Write("Documentations", project.Documentations);
			Write("Channel", project.Channel);
			Write("InstallerLocale", project.InstallerLocale);
			Write("Platform", project.Platform);
			Write("MinimumOSVersion", project.MinimumOSVersion);
			Write("InstallerType", project.InstallerType);
			Write("NestedInstallerType", project.NestedInstallerType);
			Write("NestedInstallerFiles", project.NestedInstallerFiles);
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
			Write("PackageDependencies", project.PackageDependencies);
			Write("WindowsFeatures", project.WindowsFeatures);
			Write("Capabilities", project.Capabilities);
			Write("RestrictedCapabilities", project.RestrictedCapabilities);
			Write("Markets", project.Markets);
			Write("ExcludedMarkets", project.ExcludedMarkets);
			Write("ExpectedReturnCodes", project.ExpectedReturnCodes);
			Write("UnsupportedArguments", project.UnsupportedArguments);
			Write("DefaultInstallLocation", project.DefaultInstallLocation);
			Write("InstalledFiles", project.InstalledFiles);
			Write("AuthenticationType", project.AuthenticationType);
			Write("AuthenticationResource", project.AuthenticationResource);
			Write("AuthenticationScope", project.AuthenticationScope);
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
		}
		finally
		{
			applyingProjectToControls = false;
		}
		RefreshReadiness();
	}

	private Control CreateReviewProgressPanel()
	{
		StudioCard panel = new()
		{
			AccessibleName = "Four-step review progress",
			Dock = DockStyle.Fill,
			ColumnCount = 4,
			RowCount = 1,
			BackColor = CardColor,
			Padding = new Padding(22, 7, 22, 7),
			Margin = new Padding(0, 8, 0, 8),
			CornerRadius = 12
		};
		for (int column = 0; column < 4; column++) panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
		panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

		string[] titles = ["Preview", "Save safely", "Validate", "Test & submit"];
		reviewProgressSteps = new StudioTestProgressStep[4];
		for (int index = 0; index < reviewProgressSteps.Length; index++)
		{
			reviewProgressSteps[index] = new StudioTestProgressStep
			{
				Dock = DockStyle.Fill,
				StepNumber = index + 1,
				Title = titles[index],
				IsFirst = index == 0,
				IsLast = index == reviewProgressSteps.Length - 1,
				Margin = Padding.Empty,
				AccessibleName = $"Review step {index + 1}: {titles[index]}"
			};
			panel.Controls.Add(reviewProgressSteps[index], index, 0);
		}
		return panel;
	}

	private Control CreateCurrentReviewActionPanel()
	{
		StudioCard card = new()
		{
			AccessibleName = "Current required review action",
			Dock = DockStyle.Fill,
			ColumnCount = 2,
			RowCount = 1,
			BackColor = CardColor,
			Padding = new Padding(20, 14, 18, 14),
			Margin = new Padding(0, 0, 0, 10),
			CornerRadius = 12
		};
		card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 222));

		TableLayoutPanel copy = new()
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 4,
			BackColor = CardColor,
			Margin = Padding.Empty,
			Padding = new Padding(0, 0, 18, 0)
		};
		copy.RowStyles.Add(new RowStyle(SizeType.Absolute, 23));
		copy.RowStyles.Add(new RowStyle(SizeType.Absolute, 37));
		copy.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		copy.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
		readinessLabel = new Label
		{
			Dock = DockStyle.Fill,
			Text = "PROJECT STATUS",
			ForeColor = StudioPalette.Warning,
			Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
			TextAlign = ContentAlignment.MiddleLeft,
			Margin = Padding.Empty
		};
		reviewActionTitleLabel = new Label
		{
			Dock = DockStyle.Fill,
			Text = "Complete the package information",
			ForeColor = Color.White,
			Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold),
			TextAlign = ContentAlignment.MiddleLeft,
			AutoEllipsis = true,
			Margin = Padding.Empty
		};
		reviewActionDescriptionLabel = new Label
		{
			Dock = DockStyle.Fill,
			Text = "The Studio will identify the first required item and take you to the correct page.",
			ForeColor = MutedColor,
			Font = new Font("Segoe UI", 9F),
			TextAlign = ContentAlignment.MiddleLeft,
			AutoEllipsis = true,
			Margin = Padding.Empty
		};
		reviewActionSafetyLabel = new Label
		{
			Dock = DockStyle.Fill,
			Text = "REQUIRED · Review is locked until this is corrected",
			ForeColor = StudioPalette.Warning,
			Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
			TextAlign = ContentAlignment.MiddleLeft,
			Margin = Padding.Empty
		};
		copy.Controls.Add(readinessLabel, 0, 0);
		copy.Controls.Add(reviewActionTitleLabel, 0, 1);
		copy.Controls.Add(reviewActionDescriptionLabel, 0, 2);
		copy.Controls.Add(reviewActionSafetyLabel, 0, 3);

		TableLayoutPanel actionHost = new()
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 3,
			BackColor = CardColor,
			Margin = Padding.Empty
		};
		actionHost.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
		actionHost.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
		actionHost.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
		reviewNextActionButton = CreateButton("Open the field to fix", async (_, _) => await RunNextReviewActionAsync(), true);
		reviewNextActionButton.AccessibleName = "Run the next required review action";
		reviewNextActionButton.Dock = DockStyle.Fill;
		reviewNextActionButton.AutoSize = false;
		reviewNextActionButton.Margin = Padding.Empty;
		actionHost.Controls.Add(reviewNextActionButton, 0, 1);

		card.Controls.Add(copy, 0, 0);
		card.Controls.Add(actionHost, 1, 0);
		return card;
	}

	private Control CreateReviewChecklistPanel()
	{
		StudioCard card = new()
		{
			AccessibleName = "Review checklist",
			Width = 420,
			Height = 266,
			ColumnCount = 1,
			RowCount = 5,
			BackColor = CardColor,
			Padding = new Padding(16, 12, 16, 12),
			Margin = new Padding(0, 0, 0, 10),
			CornerRadius = 12
		};
		card.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
		for (int row = 1; row < 5; row++) card.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
		TableLayoutPanel heading = new()
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 2,
			BackColor = CardColor,
			Margin = Padding.Empty
		};
		heading.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
		heading.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
		heading.Controls.Add(new Label
		{
			Text = "REVIEW CHECKLIST",
			Dock = DockStyle.Fill,
			ForeColor = AccentColor,
			Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
			TextAlign = ContentAlignment.MiddleLeft,
			Margin = Padding.Empty
		}, 0, 0);
		heading.Controls.Add(new Label
		{
			Text = "The Studio unlocks these in the correct order.",
			Dock = DockStyle.Fill,
			ForeColor = MutedColor,
			Font = new Font("Segoe UI", 8.2F),
			TextAlign = ContentAlignment.TopLeft,
			Margin = Padding.Empty
		}, 0, 1);
		card.Controls.Add(heading, 0, 0);

		(string title, string description)[] rows =
		[
			("1  Preview", "Builds the proposed YAML in memory"),
			("2  Save safely", "Creates backups before replacing files"),
			("3  Validate", "Runs the official Winget validator"),
			("4  Test & submit", "Continues in the guided Test Center")
		];
		reviewStatusPills = new StudioStatusPill[rows.Length];
		for (int index = 0; index < rows.Length; index++)
		{
			Control row = CreateTestChecklistRow(rows[index].title, rows[index].description, out StudioStatusPill pill);
			reviewStatusPills[index] = pill;
			card.Controls.Add(row, 0, index + 1);
		}
		return card;
	}

	private Control CreateReviewViewOptionsPanel()
	{
		StudioCard card = new()
		{
			AccessibleName = "Review view options",
			Width = 420,
			Height = 210,
			ColumnCount = 1,
			RowCount = 4,
			BackColor = CardColor,
			Padding = new Padding(12),
			Margin = new Padding(0, 0, 0, 8),
			CornerRadius = 12
		};
		card.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
		card.RowStyles.Add(new RowStyle(SizeType.Percent, 33.333F));
		card.RowStyles.Add(new RowStyle(SizeType.Percent, 33.333F));
		card.RowStyles.Add(new RowStyle(SizeType.Percent, 33.334F));
		card.Controls.Add(new Label
		{
			Text = "VIEW OPTIONS\r\nThe plain-language review stays selected by default.",
			Dock = DockStyle.Fill,
			ForeColor = MutedColor,
			Font = new Font("Segoe UI Semibold", 8.5F),
			TextAlign = ContentAlignment.MiddleLeft,
			Margin = new Padding(4, 0, 4, 4)
		}, 0, 0);
		previewModeButton = CreateButton("Show technical YAML", (_, _) => TogglePreviewMode());
		previewModeButton.Enabled = false;
		previewModeButton.Dock = DockStyle.Fill;
		previewModeButton.AutoSize = false;
		previewModeButton.Margin = new Padding(4);
		Button outputButton = CreateButton("Open output folder", (_, _) => OpenOutputFolder());
		outputButton.Dock = DockStyle.Fill;
		outputButton.AutoSize = false;
		outputButton.Margin = new Padding(4);
		Button backupButton = CreateButton("Open backup folder", (_, _) => OpenBackupFolder());
		backupButton.Dock = DockStyle.Fill;
		backupButton.AutoSize = false;
		backupButton.Margin = new Padding(4);
		card.Controls.Add(previewModeButton, 0, 1);
		card.Controls.Add(outputButton, 0, 2);
		card.Controls.Add(backupButton, 0, 3);
		return card;
	}

	private Control CreateReviewResultsPanel()
	{
		StudioCard panel = new()
		{
			AccessibleName = "Plain-language manifest review",
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 2,
			BackColor = CardColor,
			Padding = new Padding(16, 12, 16, 16),
			Margin = Padding.Empty,
			CornerRadius = 12
		};
		panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
		panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		panel.Controls.Add(new Label
		{
			Text = "PLAIN-LANGUAGE REVIEW",
			Dock = DockStyle.Fill,
			TextAlign = ContentAlignment.MiddleLeft,
			Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
			ForeColor = AccentColor,
			Margin = Padding.Empty
		}, 0, 0);
		previewBox = NewRichTextBox();
		previewBox.ReadOnly = true;
		previewBox.Font = new Font("Cascadia Mono", 9.5F);
		previewBox.Text = "WHAT NEEDS ATTENTION\r\n\r\nComplete the Package and Installers pages. The first item to fix and your next action will appear above.";
		panel.Controls.Add(previewBox, 0, 1);
		return panel;
	}

	private Control CreateTestProgressPanel()
	{
		StudioCard panel = new()
		{
			AccessibleName = "Four-step test progress",
			Dock = DockStyle.Fill,
			ColumnCount = 4,
			RowCount = 1,
			BackColor = CardColor,
			Padding = new Padding(22, 7, 22, 7),
			Margin = new Padding(0, 8, 0, 8),
			CornerRadius = 12
		};
		for (int column = 0; column < 4; column++) panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
		panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

		string[] titles = ["Safe preflight", "Allow testing", "Test install", "Verify result"];
		testProgressSteps = new StudioTestProgressStep[4];
		for (int index = 0; index < testProgressSteps.Length; index++)
		{
			testProgressSteps[index] = new StudioTestProgressStep
			{
				Dock = DockStyle.Fill,
				StepNumber = index + 1,
				Title = titles[index],
				IsFirst = index == 0,
				IsLast = index == testProgressSteps.Length - 1,
				Margin = Padding.Empty,
				AccessibleName = $"Test step {index + 1}: {titles[index]}"
			};
			panel.Controls.Add(testProgressSteps[index], index, 0);
		}
		return panel;
	}

	private Control CreateCurrentTestActionPanel()
	{
		StudioCard card = new()
		{
			AccessibleName = "Current required test action",
			Dock = DockStyle.Fill,
			ColumnCount = 2,
			RowCount = 1,
			BackColor = CardColor,
			Padding = new Padding(20, 14, 18, 14),
			Margin = new Padding(0, 0, 0, 10),
			CornerRadius = 12
		};
		card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 222));

		TableLayoutPanel copy = new()
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 4,
			BackColor = CardColor,
			Margin = Padding.Empty,
			Padding = new Padding(0, 0, 18, 0)
		};
		copy.RowStyles.Add(new RowStyle(SizeType.Absolute, 23));
		copy.RowStyles.Add(new RowStyle(SizeType.Absolute, 37));
		copy.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		copy.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
		testPlanLabel = new Label
		{
			Dock = DockStyle.Fill,
			Text = "PROJECT STATUS",
			ForeColor = AccentColor,
			Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
			TextAlign = ContentAlignment.MiddleLeft,
			Margin = Padding.Empty
		};
		nextTestActionTitleLabel = new Label
		{
			Dock = DockStyle.Fill,
			Text = "Run safe preflight",
			ForeColor = Color.White,
			Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold),
			TextAlign = ContentAlignment.MiddleLeft,
			AutoEllipsis = true,
			Margin = Padding.Empty
		};
		nextTestActionDescriptionLabel = new Label
		{
			Dock = DockStyle.Fill,
			Text = "Checks the manifest and installer without installing anything.",
			ForeColor = MutedColor,
			Font = new Font("Segoe UI", 9F),
			TextAlign = ContentAlignment.MiddleLeft,
			AutoEllipsis = true,
			Margin = Padding.Empty
		};
		nextTestActionSafetyLabel = new Label
		{
			Dock = DockStyle.Fill,
			Text = "SAFE · Nothing will be installed",
			ForeColor = SuccessColor,
			Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
			TextAlign = ContentAlignment.MiddleLeft,
			Margin = Padding.Empty
		};
		copy.Controls.Add(testPlanLabel, 0, 0);
		copy.Controls.Add(nextTestActionTitleLabel, 0, 1);
		copy.Controls.Add(nextTestActionDescriptionLabel, 0, 2);
		copy.Controls.Add(nextTestActionSafetyLabel, 0, 3);

		TableLayoutPanel actionHost = new()
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 3,
			BackColor = CardColor,
			Margin = Padding.Empty
		};
		actionHost.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
		actionHost.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
		actionHost.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
		nextTestActionButton = CreateButton("Run safe preflight", async (_, _) => await RunNextTestActionAsync(), true);
		nextTestActionButton.AccessibleName = "Run the next required test action";
		nextTestActionButton.Dock = DockStyle.Fill;
		nextTestActionButton.AutoSize = false;
		nextTestActionButton.Margin = Padding.Empty;
		actionHost.Controls.Add(nextTestActionButton, 0, 1);

		card.Controls.Add(copy, 0, 0);
		card.Controls.Add(actionHost, 1, 0);
		return card;
	}

	private Control CreateTestChecklistPanel()
	{
		StudioCard card = new()
		{
			AccessibleName = "Required test checklist",
			Width = 420,
			Height = 288,
			ColumnCount = 1,
			RowCount = 5,
			BackColor = CardColor,
			Padding = new Padding(16, 12, 16, 12),
			Margin = new Padding(0, 0, 0, 10),
			CornerRadius = 12
		};
		card.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
		for (int row = 1; row < 5; row++) card.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
		TableLayoutPanel heading = new()
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 2,
			BackColor = CardColor,
			Margin = Padding.Empty
		};
		heading.Controls.Add(new Label
		{
			Text = "REQUIRED CHECKLIST",
			Dock = DockStyle.Fill,
			ForeColor = AccentColor,
			Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
			TextAlign = ContentAlignment.MiddleLeft,
			Margin = Padding.Empty
		}, 0, 0);
		heading.Controls.Add(new Label
		{
			Text = "These are completed automatically in order.",
			Dock = DockStyle.Fill,
			ForeColor = MutedColor,
			Font = new Font("Segoe UI", 8.2F),
			TextAlign = ContentAlignment.TopLeft,
			Margin = Padding.Empty
		}, 0, 1);
		card.Controls.Add(heading, 0, 0);

		(string title, string description)[] rows =
		[
			("1  Safe preflight", "Manifest, hash, signature, and repository checks"),
			("2  Local testing", "One-time Windows setting"),
			("3  Test install", "Installs this exact release through Winget"),
			("4  Installed result", "Confirms the installed version")
		];
		testStatusPills = new StudioStatusPill[rows.Length];
		for (int index = 0; index < rows.Length; index++)
		{
			Control row = CreateTestChecklistRow(rows[index].title, rows[index].description, out StudioStatusPill pill);
			testStatusPills[index] = pill;
			card.Controls.Add(row, 0, index + 1);
		}
		return card;
	}

	private static Control CreateTestChecklistRow(string title, string description, out StudioStatusPill pill)
	{
		TableLayoutPanel row = new()
		{
			Dock = DockStyle.Fill,
			ColumnCount = 2,
			RowCount = 1,
			BackColor = StudioPalette.Card,
			Padding = new Padding(0, 4, 0, 4),
			Margin = Padding.Empty
		};
		row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
		row.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		TableLayoutPanel copy = new()
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 2,
			BackColor = StudioPalette.Card,
			Margin = Padding.Empty
		};
		copy.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
		copy.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
		copy.Controls.Add(new Label
		{
			Text = title,
			UseMnemonic = false,
			Dock = DockStyle.Fill,
			ForeColor = StudioPalette.PrimaryText,
			Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
			TextAlign = ContentAlignment.BottomLeft,
			Margin = Padding.Empty
		}, 0, 0);
		copy.Controls.Add(new Label
		{
			Text = description,
			Dock = DockStyle.Fill,
			ForeColor = StudioPalette.MutedText,
			Font = new Font("Segoe UI", 7.8F),
			TextAlign = ContentAlignment.TopLeft,
			AutoEllipsis = true,
			Margin = Padding.Empty
		}, 0, 1);
		pill = new StudioStatusPill
		{
			Text = "WAITING",
			Anchor = AnchorStyles.None,
			Size = new Size(96, 28),
			Margin = Padding.Empty
		};
		row.Controls.Add(copy, 0, 0);
		row.Controls.Add(pill, 1, 0);
		return row;
	}

	private Control CreateOptionalTestToolsCard()
	{
		StudioCard card = new()
		{
			AccessibleName = "Optional diagnostic tools",
			Width = 420,
			Height = 224,
			ColumnCount = 1,
			RowCount = 2,
			BackColor = CardColor,
			Padding = new Padding(12),
			Margin = new Padding(0, 0, 0, 8),
			CornerRadius = 12
		};
		card.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
		card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		card.Controls.Add(new Label
		{
			Text = "OPTIONAL DIAGNOSTICS\r\nExtra detail only — these are not required steps.",
			Dock = DockStyle.Fill,
			ForeColor = MutedColor,
			Font = new Font("Segoe UI Semibold", 8.5F),
			TextAlign = ContentAlignment.MiddleLeft,
			Margin = new Padding(4, 0, 4, 4)
		}, 0, 0);
		card.Controls.Add(CreateTestToolsPanel(), 0, 1);
		return card;
	}

	private void ToggleOptionalTestTools()
	{
		if (testOptionalToolsCard is null || testOptionalToolsCard.IsDisposed) return;
		testOptionalToolsCard.Visible = !testOptionalToolsCard.Visible;
		SetInterfaceText(optionalToolsToggleButton, testOptionalToolsCard.Visible ? "Hide optional tools" : "Show optional tools");
		optionalToolsToggleButton.AccessibleName = optionalToolsToggleButton.Text;
	}

	private Control CreateTestResultsPanel()
	{
		StudioCard panel = new()
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 2,
			BackColor = CardColor,
			Padding = new Padding(16, 12, 16, 16),
			Margin = Padding.Empty,
			CornerRadius = 10
		};
		panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
		panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		panel.Controls.Add(new Label
		{
			Text = "RESULTS AND INSTRUCTIONS",
			Dock = DockStyle.Fill,
			TextAlign = ContentAlignment.MiddleLeft,
			Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
			ForeColor = AccentColor,
			Margin = Padding.Empty
		}, 0, 0);
		testOutputBox = NewRichTextBox();
		testOutputBox.ReadOnly = true;
		testOutputBox.DetectUrls = true;
		testOutputBox.Font = new Font("Cascadia Mono", 9F);
		testOutputBox.Text = DefaultTestOutput;
		testOutputBox.LinkClicked += (_, eventArgs) =>
		{
			if (!uiTestMode && Uri.TryCreate(eventArgs.LinkText, UriKind.Absolute, out Uri? uri))
				Process.Start(new ProcessStartInfo { FileName = uri.AbsoluteUri, UseShellExecute = true });
		};
		panel.Controls.Add(testOutputBox, 0, 1);
		return panel;
	}

	private Control CreateTestToolsPanel()
	{
		(string text, EventHandler handler)[] actions =
		[
			("Check Winget setup", async (_, _) => await RefreshTestEnvironmentAsync(showReport: true)),
			("Inspect signatures", async (_, _) => await InspectSignaturesAsync()),
			("Find existing package", async (_, _) => await FindExistingPackageAsync()),
			("Sandbox install only", async (_, _) => await TestInSandboxAsync()),
			("Sandbox install + uninstall", async (_, _) => await TestInstallAndUninstallInSandboxAsync()),
			("Export test report", async (_, _) => await ExportTestReportAsync())
		];
		TableLayoutPanel panel = new()
		{
			Dock = DockStyle.Fill,
			ColumnCount = 2,
			RowCount = 3,
			BackColor = CardColor,
			Padding = new Padding(10),
			Margin = new Padding(0, 0, 0, 4)
		};
		panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
		panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
		for (int row = 0; row < 3; row++) panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / 3F));
		for (int index = 0; index < actions.Length; index++)
		{
			Button button = CreateButton(actions[index].text, actions[index].handler);
			button.Dock = DockStyle.Fill;
			button.AutoSize = false;
			button.Margin = new Padding(4);
			if (index == actions.Length - 1 && actions.Length % 2 == 1)
			{
				panel.Controls.Add(button, 0, index / 2);
				panel.SetColumnSpan(button, 2);
			}
			else
			{
				panel.Controls.Add(button, index % 2, index / 2);
			}
		}
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
		if (applyingProjectToControls || refreshingReadiness || readinessLabel is null || readinessLabel.IsDisposed || fields.Count == 0) return;
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
					simplePreviewText = T("NOTHING NEEDS FIXING") + "\r\n\r\n"
						+ T("[OK] All required package information is present.") + "\r\n"
						+ T("[OK] Every installer has a public URL, architecture, and SHA-256 hash.") + "\r\n\r\n"
						+ T("NEXT: Click Preview Changes. Nothing will be saved yet.");
				}
				else
				{
					StringBuilder fixes = new(T("WHAT NEEDS ATTENTION") + "\r\n\r\n");
					for (int index = 0; index < errors.Count; index++) fixes.AppendLine($"{index + 1}. {LocalizeReadinessMessage(errors[index])}.");
					fixes.Append("\r\n" + T("Open 2 Package for package information or 3 Installers for release-file, URL, architecture, and hash problems."));
					simplePreviewText = fixes.ToString();
				}
				showingTechnicalPreview = false;
				previewBox.Text = simplePreviewText;
				SetInterfaceText(previewModeButton, "Show technical YAML");
				previewModeButton.AccessibleName = previewModeButton.Text;
			}
			bool currentReview = ready && string.Equals(reviewFingerprint, currentFingerprint, StringComparison.Ordinal);
			UpdateReviewWorkflowStatus(errors, currentFingerprint, currentReview);
			previewModeButton.Enabled = technicalPreviewText.Length > 0 && !isBusy;
		}
		finally
		{
			refreshingReadiness = false;
		}
		UpdateTestPlanStatus();
	}

	private void UpdateReviewWorkflowStatus(IReadOnlyList<string> errors, string currentFingerprint, bool currentReview)
	{
		if (reviewNextActionButton is null || reviewNextActionButton.IsDisposed || reviewProgressSteps.Length == 0) return;
		bool projectReady = errors.Count == 0;
		bool previewComplete = currentReview && reviewProgress != ReviewProgress.Editing;
		bool saveComplete = currentReview && reviewProgress is ReviewProgress.Saved or ReviewProgress.ValidationFailed or ReviewProgress.Validated;
		bool validationComplete = currentReview && reviewProgress == ReviewProgress.Validated;
		bool testingComplete = validationComplete
			&& localManifestFilesEnabled
			&& string.Equals(successfulPreflightFingerprint, currentFingerprint, StringComparison.Ordinal)
			&& string.Equals(successfulLocalInstallFingerprint, currentFingerprint, StringComparison.Ordinal)
			&& string.Equals(verifiedInstalledFingerprint, currentFingerprint, StringComparison.Ordinal);
		bool validationFailed = currentReview && reviewProgress == ReviewProgress.ValidationFailed;
		bool[] complete = [previewComplete, saveComplete, validationComplete, testingComplete];
		string[] completeText = [T("PREVIEWED"), T("SAVED"), T("VALIDATED"), T("COMPLETE")];
		int currentStep = !previewComplete ? 0 : !saveComplete ? 1 : !validationComplete ? 2 : 3;

		for (int index = 0; index < reviewProgressSteps.Length; index++)
		{
			StudioStepState state = complete[index]
				? StudioStepState.Complete
				: index == currentStep
					? (!projectReady || validationFailed ? StudioStepState.Problem : StudioStepState.Current)
					: StudioStepState.Pending;
			reviewProgressSteps[index].State = state;
			reviewProgressSteps[index].StatusText = complete[index] ? completeText[index] : index == currentStep ? state == StudioStepState.Problem ? T("NEEDS ATTENTION") : T("NEXT") : T("WAITING");
			reviewProgressSteps[index].AccessibleDescription = reviewProgressSteps[index].StatusText;
			if (index < reviewStatusPills.Length)
			{
				reviewStatusPills[index].State = state;
				reviewStatusPills[index].Text = complete[index] ? completeText[index] : index == currentStep ? state == StudioStepState.Problem ? T("FIX FIRST") : T("NEXT") : T("WAITING");
				reviewStatusPills[index].AccessibleName = $"Review step {index + 1} status: {reviewStatusPills[index].Text}";
			}
		}

		if (!projectReady)
		{
			readinessLabel.Text = string.Format(T(errors.Count == 1 ? "PROJECT NEEDS {0} FIX" : "PROJECT NEEDS {0} FIXES"), errors.Count)
				+ "   •   " + T("REVIEW LOCKED");
			readinessLabel.ForeColor = StudioPalette.Warning;
			reviewActionTitleLabel.Text = "Fix the package information";
			reviewActionDescriptionLabel.Text = SimplifyReadinessError(errors[0]) + ". The Studio will return you to the correct page.";
			reviewActionSafetyLabel.Text = "REQUIRED · Preview stays locked until this is corrected";
			reviewActionSafetyLabel.ForeColor = StudioPalette.Warning;
			reviewNextActionButton.Text = "Open the field to fix";
			reviewNextActionButton.Tag = "fix-project";
		}
		else if (validationFailed)
		{
			readinessLabel.Text = T("WINGET FOUND A PROBLEM   •   NOTHING WAS SUBMITTED");
			readinessLabel.ForeColor = StudioPalette.Warning;
			reviewActionTitleLabel.Text = "Fix the validation problem";
			reviewActionDescriptionLabel.Text = "The plain-language result below names the problem and where to correct it. Then preview and save again.";
			reviewActionSafetyLabel.Text = "STOP · Submission remains locked until validation passes";
			reviewActionSafetyLabel.ForeColor = StudioPalette.Warning;
			reviewNextActionButton.Text = "Open the fields to fix";
			reviewNextActionButton.Tag = "fix-validation";
		}
		else if (!previewComplete)
		{
			readinessLabel.Text = T("READY TO REVIEW") + $"   •   {project.PackageIdentifier}   •   {project.Installers.Count} "
				+ T(project.Installers.Count == 1 ? "INSTALLER" : "INSTALLERS");
			readinessLabel.ForeColor = AccentColor;
			reviewActionTitleLabel.Text = "Preview the proposed changes";
			reviewActionDescriptionLabel.Text = "Builds the exact manifest changes in memory and explains them below. No files are written.";
			reviewActionSafetyLabel.Text = "SAFE · Preview does not change any files";
			reviewActionSafetyLabel.ForeColor = SuccessColor;
			reviewNextActionButton.Text = "Preview changes";
			reviewNextActionButton.Tag = "preview";
		}
		else if (!saveComplete)
		{
			readinessLabel.Text = T("PREVIEW READY   •   NOTHING HAS BEEN SAVED");
			readinessLabel.ForeColor = AccentColor;
			reviewActionTitleLabel.Text = "Save the reviewed manifests";
			reviewActionDescriptionLabel.Text = "Writes the reviewed YAML to the output folder after creating recoverable backups of existing files.";
			reviewActionSafetyLabel.Text = "PROTECTED · Existing manifests are backed up first";
			reviewActionSafetyLabel.ForeColor = SuccessColor;
			reviewNextActionButton.Text = "Save manifests";
			reviewNextActionButton.Tag = "save";
		}
		else if (!validationComplete)
		{
			readinessLabel.Text = T("SAVED SAFELY   •   READY FOR OFFICIAL VALIDATION");
			readinessLabel.ForeColor = AccentColor;
			reviewActionTitleLabel.Text = "Validate with Winget";
			reviewActionDescriptionLabel.Text = "Runs Microsoft's Winget validator against a clean temporary copy. It does not install the package.";
			reviewActionSafetyLabel.Text = "SAFE · Validation does not change the saved manifests";
			reviewActionSafetyLabel.ForeColor = SuccessColor;
			reviewNextActionButton.Text = "Validate locally";
			reviewNextActionButton.Tag = "validate";
		}
		else if (!testingComplete)
		{
			readinessLabel.Text = T("VALIDATION PASSED   •   READY FOR TEST CENTER");
			readinessLabel.ForeColor = SuccessColor;
			reviewActionTitleLabel.Text = "Continue to Test Center";
			reviewActionDescriptionLabel.Text = "Run safe preflight, test the installation, verify the result, and submit from one guided screen.";
			reviewActionSafetyLabel.Text = "NEXT · Testing and submission continue without returning here";
			reviewActionSafetyLabel.ForeColor = AccentColor;
			reviewNextActionButton.Text = "Open Test Center";
			reviewNextActionButton.Tag = "test-center";
		}
		else
		{
			readinessLabel.Text = T("ALL REVIEW AND INSTALLATION TESTS PASSED");
			readinessLabel.ForeColor = SuccessColor;
			reviewActionTitleLabel.Text = "Ready to submit in Test Center";
			reviewActionDescriptionLabel.Text = "All required review and installation checks passed. The submission action is ready in Test Center.";
			reviewActionSafetyLabel.Text = "READY · Microsoft's WingetCreate handles the submission";
			reviewActionSafetyLabel.ForeColor = SuccessColor;
			reviewNextActionButton.Text = "Open Test Center to submit";
			reviewNextActionButton.Tag = "test-center";
		}

		LocalizeDynamicControls(reviewActionTitleLabel, reviewActionDescriptionLabel, reviewActionSafetyLabel, reviewNextActionButton);
		if (!projectReady) SetLocalizedReadinessError(reviewActionDescriptionLabel, errors[0]);
		reviewNextActionButton.Enabled = !isBusy;
		reviewNextActionButton.AccessibleName = reviewNextActionButton.Text;
		if (reviewNextActionButton is StudioButton studioButton) studioButton.ButtonKind = StudioButtonKind.Primary;
	}

	private async Task RunNextReviewActionAsync()
	{
		switch (reviewNextActionButton.Tag as string)
		{
			case "fix-project":
				List<string> errors = ManifestService.Validate(project);
				string first = errors.FirstOrDefault() ?? string.Empty;
				SelectTab(first.Contains("installer", StringComparison.OrdinalIgnoreCase)
					|| first.Contains("hash", StringComparison.OrdinalIgnoreCase)
					|| first.Contains("architecture", StringComparison.OrdinalIgnoreCase)
					? "Installers & Hashes" : "Package Details");
				break;
			case "fix-validation":
				SelectTab(simplePreviewText.Contains("3 Installers", StringComparison.OrdinalIgnoreCase)
					&& !simplePreviewText.Contains("2 Package", StringComparison.OrdinalIgnoreCase)
					? "Installers & Hashes" : "Package Details");
				break;
			case "preview": GeneratePreview(); break;
			case "save": SaveManifests(); break;
			case "validate": await ValidateWithWingetAsync(); break;
			case "test-center": SelectTab("Test Center"); break;
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

	private Control CreateInstallerDefaults()
	{
		FlowLayoutPanel row = CreateInlinePanel();
		// Shared settings must remain reachable at the minimum supported window
		// width. Let the fields form a second line instead of extending beyond the
		// right edge of the page on smaller displays and CI virtual desktops.
		row.WrapContents = true;
		row.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		row.Padding = new Padding(14, 9, 14, 9);
		row.Controls.Add(NewInlineLabel("Optional shared settings"));
		row.Controls.Add(ChoiceField("InstallerType", "Shared installer type", ["exe", "msi", "wix", "burn", "inno", "nullsoft", "msix", "appx", "zip", "pwa", "portable", "font"], 145));
		row.Controls.Add(ChoiceField("Scope", "Scope", ["user", "machine"], 105));
		row.Controls.Add(Field("InstallModes", "Install modes", "Comma-separated", width: 220));
		row.Controls.Add(ChoiceField("UpgradeBehavior", "Upgrade behavior", ["install", "uninstallPrevious", "deny"], 155));
		row.Controls.Add(ChoiceField("ElevationRequirement", "Elevation", ["elevationRequired", "elevatesSelf", "elevationProhibited"], 170));
		row.Controls.Add(CreateHttpUrlToggleField());
		return row;
	}

	private Control CreateHttpUrlToggleField()
	{
		Panel wrapper = new() { Width = 150, Height = 98, Margin = new Padding(6, 8, 4, 8) };
		Label caption = new()
		{
			Text = "Allow HTTP URLs",
			AutoSize = true,
			ForeColor = Color.FromArgb(189, 213, 244),
			Font = new Font("Segoe UI Semibold", 9F),
			Location = new Point(0, 0)
		};
		insecureUrlCheck = NewToggleSwitch();
		insecureUrlCheck.Location = new Point(0, 24);
		Label help = new()
		{
			Text = "Off is safer. Enable only when HTTPS is unavailable.",
			Location = new Point(1, 62),
			Width = 146,
			Height = 30,
			AutoEllipsis = true,
			ForeColor = MutedColor,
			Font = new Font("Segoe UI", 8.25F)
		};
		wrapper.Controls.Add(caption);
		wrapper.Controls.Add(insecureUrlCheck);
		wrapper.Controls.Add(help);
		return wrapper;
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
		void AddChoice(string property, string title, int width, params string[] choices)
		{
			StudioDataGridViewChoiceColumn column = new(choices)
			{
				DataPropertyName = property,
				Name = property,
				HeaderText = title,
				Width = width,
				AutoSizeMode = DataGridViewAutoSizeColumnMode.None
			};
			grid.Columns.Add(column);
		}
		Add(nameof(InstallerArtifact.LocalFile), "LOCAL RELEASE FILE", 210);
		Add(nameof(InstallerArtifact.InstallerUrl), "PUBLIC INSTALLER URL", 285);
		AddChoice(nameof(InstallerArtifact.Architecture), "ARCH", 88, "x86", "x64", "arm", "arm64", "neutral");
		AddChoice(nameof(InstallerArtifact.InstallerType), "TYPE", 108, "exe", "msi", "wix", "burn", "inno", "nullsoft", "msix", "appx", "zip", "pwa", "portable", "font");
		AddChoice(nameof(InstallerArtifact.Scope), "SCOPE", 96, "user", "machine");
		Add(nameof(InstallerArtifact.VerificationStatus), "HASH SOURCE / STATUS", 220);
		Add(nameof(InstallerArtifact.AnalysisSummary), "INSTALLER ANALYSIS", 300);
		AddChoice(nameof(InstallerArtifact.NestedInstallerType), "NESTED TYPE", 132, "exe", "msi", "wix", "burn", "inno", "nullsoft", "msix", "appx", "portable", "font");
		Add(nameof(InstallerArtifact.NestedInstallerFiles), "ZIP CONTENTS", 245);
		Add(nameof(InstallerArtifact.SignatureStatus), "DIGITAL SIGNATURE", 230);
		Add(nameof(InstallerArtifact.SignerName), "SIGNER", 180);
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
			if (columnName is nameof(InstallerArtifact.ProductCode) or nameof(InstallerArtifact.UpgradeCode))
			{
				eventArgs.ToolTipText = UsesMsiIdentityCodes(installer.InstallerType)
					? T("This value is read automatically from the selected MSI file. 'Not found in MSI' means the package author did not include it.")
					: T("This installer does not provide standardized MSI identity codes. Winget treats these fields as optional, so leave them blank unless you know the installed Apps & Features correlation value.");
			}
			else if (columnName == nameof(InstallerArtifact.NestedInstallerType))
				eventArgs.ToolTipText = T("Required only for ZIP packages. Use the installer technology inside the archive, such as exe, msi, portable, or font.");
			else if (columnName == nameof(InstallerArtifact.NestedInstallerFiles))
				eventArgs.ToolTipText = T("Required only for ZIP packages. Enter paths inside the ZIP separated by semicolons. For portable files, add an optional command after |, for example tools\\sample.exe | sample.");
		};
		return grid;
	}

	private static bool UsesMsiIdentityCodes(string installerType) =>
		installerType.Equals("msi", StringComparison.OrdinalIgnoreCase) ||
		installerType.Equals("wix", StringComparison.OrdinalIgnoreCase);

	private Control Field(string key, string label, string hint = "", bool multiline = false, int width = 520)
	{
		bool required = RequiredProjectFields.Contains(key);
		string guidance = FieldGuidance(key, hint, required);
		Panel wrapper = new() { Width = width, Height = multiline ? 150 : 98, Margin = new Padding(8) };
		Label caption = new() { Text = required ? label + "  * Required" : label, AutoSize = true, ForeColor = required ? Color.White : Color.FromArgb(189, 213, 244), Font = new Font("Segoe UI Semibold", 9F), Location = new Point(0, 0) };
		StudioTextBox box = NewTextBox(width);
		box.AccessibleName = label;
		box.AccessibleDescription = guidance;
		box.Multiline = multiline;
		box.Height = multiline ? 78 : 38;
		box.Location = new Point(0, 24);
		box.PlaceholderText = guidance;
		Label help = new()
		{
			Text = guidance,
			Location = new Point(1, multiline ? 106 : 65),
			Width = width - 8,
			Height = multiline ? 38 : 30,
			AutoEllipsis = true,
			ForeColor = MutedColor,
			Font = new Font("Segoe UI", 8.25F)
		};
		wrapper.Controls.Add(caption);
		wrapper.Controls.Add(box);
		wrapper.Controls.Add(help);
		fields[key] = box;
		return wrapper;
	}

	private Control ChoiceField(string key, string label, IEnumerable<string> choices, int width)
	{
		bool required = RequiredProjectFields.Contains(key);
		string guidance = FieldGuidance(key, string.Empty, required);
		Panel wrapper = new() { Width = width, Height = 98, Margin = new Padding(8) };
		Label caption = new() { Text = required ? label + " *" : label, AutoSize = true, ForeColor = required ? Color.White : Color.FromArgb(189, 213, 244), Font = new Font("Segoe UI Semibold", 9F), Location = new Point(0, 0) };
		StudioComboBox box = NewComboBox(width);
		box.AllowEmptySelection = !required;
		box.AccessibleName = label;
		box.AccessibleDescription = guidance;
		box.Location = new Point(0, 24);
		box.SetItems(choices);
		Label help = new() { Text = guidance, Location = new Point(1, 65), Width = width - 4, Height = 30, AutoEllipsis = true, ForeColor = MutedColor, Font = new Font("Segoe UI", 8.25F) };
		wrapper.Controls.Add(caption);
		wrapper.Controls.Add(box);
		wrapper.Controls.Add(help);
		fields[key] = box;
		return wrapper;
	}

	private static string FieldGuidance(string key, string suppliedHint, bool required)
	{
		string prefix = required ? "Required. " : "Optional. ";
		if (!string.IsNullOrWhiteSpace(suppliedHint) && !suppliedHint.Equals("optional", StringComparison.OrdinalIgnoreCase))
			return prefix + suppliedHint.Trim().TrimEnd('.') + ".";
		string explanation = key switch
		{
			"ManifestVersion" => "Schema version used by the generated YAML; 1.12.0 is recommended for Microsoft Winget community submissions",
			"PackageName" => "The public product name users see in Winget",
			"Publisher" => "The company or person that publishes the application",
			"Author" => "The original application author when different from the publisher",
			"License" => "The license name, such as MIT, GPL-3.0, Proprietary, or Freeware",
			"ShortDescription" => "One clear sentence explaining what the application does",
			"Description" => "A longer public explanation of the application and its purpose",
			"Moniker" => "A short command-friendly nickname used to find the package",
			"Tags" => "Search words separated with commas; do not add # symbols",
			"Commands" => "Command names installed by the package, separated with commas",
			"PublisherUrl" => "Public HTTPS home page for the publisher",
			"PublisherSupportUrl" => "Public HTTPS page where users can get help",
			"PrivacyUrl" => "Public HTTPS privacy-policy page",
			"PackageUrl" => "Public HTTPS home page for this application",
			"LicenseUrl" => "Public HTTPS page containing the license terms",
			"Copyright" => "Copyright notice shown with the package",
			"CopyrightUrl" => "Public HTTPS page containing copyright information",
			"PurchaseUrl" => "Public HTTPS purchase page when the application is paid",
			"ReleaseNotesUrl" => "Public HTTPS page for this exact version's release notes",
			"ReleaseNotes" => "What changed in this exact release",
			"InstallationNotes" => "Instructions Winget shows after installation",
			"Agreements" => "One agreement per line using label | HTTPS URL | agreement text",
			"Documentations" => "One documentation link per line using label | HTTPS URL",
			"Channel" => "Release channel such as stable, beta, or preview",
			"InstallerLocale" => "Language built into the installer, such as en-US",
			"Platform" => "Supported Winget platforms; normally Windows.Desktop",
			"MinimumOSVersion" => "Lowest supported Windows version, such as 10.0.19041.0",
			"InstallerType" => "Optional shared type; inspected rows keep their own type, so leave this blank for mixed installers",
			"NestedInstallerType" => "Real installer type inside a ZIP package",
			"NestedInstallerFiles" => "Shared paths inside a ZIP; separate paths with semicolons and add | command only for portable files",
			"Scope" => "Optional shared scope; choose user for one account, machine for the whole computer, or leave blank when it varies by installer",
			"InstallModes" => "Supported modes separated with commas: interactive, silent, silentWithProgress",
			"UpgradeBehavior" => "Optional instruction for upgrades; leave blank unless the installer requires a specific behavior",
			"ElevationRequirement" => "Choose elevationRequired when admin is always required, elevatesSelf when the installer decides, or elevationProhibited only when admin must be blocked; elevationProhibited cannot run in Microsoft's Administrator-based Sandbox test",
			"Protocols" => "URL protocols registered by the app, separated with commas",
			"FileExtensions" => "File extensions registered by the app, separated with commas and without dots",
			"UnsupportedOSArchitectures" => "Architectures that cannot use this installer, separated with commas",
			"InstallerSuccessCodes" => "Extra successful installer exit codes, separated with commas",
			"PackageDependencies" => "One Winget dependency per line using Publisher.Application | minimum version",
			"WindowsFeatures" => "Windows feature names required by the application, separated with commas",
			"Capabilities" => "MSIX capabilities required by the package, separated with commas",
			"RestrictedCapabilities" => "Restricted MSIX capabilities, separated with commas",
			"Markets" => "Market codes where installation is allowed, separated with commas",
			"ExcludedMarkets" => "Market codes where installation is blocked, separated with commas",
			"ExpectedReturnCodes" => "One installer result per line using code | Winget response | optional HTTPS help URL",
			"UnsupportedArguments" => "Choose log, location, or both only when the installer cannot support those Winget arguments",
			"DefaultInstallLocation" => "The usual installed application folder; environment variables such as %ProgramFiles% are allowed",
			"InstalledFiles" => "One installed file per line using relative path | file type | optional SHA-256 | optional argument | optional display name",
			"AuthenticationType" => "Authentication for a private source; community repository packages leave this blank",
			"AuthenticationResource" => "Microsoft Entra resource used by a private source",
			"AuthenticationScope" => "Microsoft Entra scope used by a private source",
			"PackageFamilyName" => "Microsoft Store or MSIX package family name",
			"ReleaseDate" => "Public release date in YYYY-MM-DD format",
			"RepairBehavior" => "How Winget repairs the app: modify, uninstaller, or installer",
			"InstallerAbortsTerminal" => "Enter true only if installation closes the user's terminal",
			"InstallLocationRequired" => "Enter true only when a custom install location is mandatory",
			"RequireExplicitUpgrade" => "Enter true when Winget must not upgrade automatically",
			"DisplayInstallWarnings" => "Enter true when Winget should show installer warnings",
			"DownloadCommandProhibited" => "Enter true when winget download must be blocked",
			"ArchiveBinariesDependOnPath" => "For archives, enter true when extracted commands depend on PATH",
			"SwitchSilent" => "Installer argument for a completely silent installation",
			"SwitchSilentWithProgress" => "Installer argument for quiet installation with progress",
			"SwitchInteractive" => "Installer argument that forces the interactive interface",
			"SwitchInstallLocation" => "Installer argument template for a custom install folder",
			"SwitchLog" => "Installer argument template for a log-file path",
			"SwitchUpgrade" => "Installer argument used specifically during upgrades",
			"CustomInstallerSwitch" => "Argument Winget must add to every install command",
			"SwitchRepair" => "Installer argument used for repair",
			"AdvancedLocaleFieldsYaml" => "Advanced locale YAML only; most users should leave this blank",
			"AdvancedInstallerFieldsYaml" => "Advanced installer YAML only; most users should leave this blank",
			_ => "Leave blank when this value does not apply or is unknown"
		};
		return prefix + explanation.TrimEnd('.') + ".";
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
		Label safety = new() { Text = "LOCAL-FIRST\n\nGitHub token stays in Windows Credential Manager\nNo manifest overwritten without backup\nNo installer downloaded without confirmation", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = SuccessColor, Font = new Font("Segoe UI Semibold", 9.5F), BackColor = InputColor, Padding = new Padding(18) };
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

	private Control CreateInfoStrip(string heading, string message, int headingWidth = 190)
	{
		StudioCard panel = new() { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, BackColor = Color.FromArgb(8, 42, 54), Padding = new Padding(14), Margin = new Padding(4, 4, 4, 8), CornerRadius = 10 };
		panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, headingWidth));
		panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		panel.Controls.Add(new Label { Text = heading, Dock = DockStyle.Fill, AutoSize = true, Font = new Font("Segoe UI Semibold", 9F), ForeColor = AccentColor }, 0, 0);
		panel.Controls.Add(new Label { Text = message, Dock = DockStyle.Fill, AutoSize = true, MaximumSize = new Size(900, 0), ForeColor = Color.FromArgb(195, 218, 236) }, 1, 0);
		return panel;
	}

	private Control CreateLanguageSettingsCard()
	{
		StudioCard card = new()
		{
			AccessibleName = "Interface language setting",
			Width = 1160,
			Height = 76,
			ColumnCount = 3,
			RowCount = 1,
			BackColor = CardColor,
			Padding = new Padding(18, 12, 18, 12),
			Margin = new Padding(0, 0, 0, 12),
			CornerRadius = 10
		};
		card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
		card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
		card.Controls.Add(new Label
		{
			Text = "INTERFACE LANGUAGE",
			Dock = DockStyle.Fill,
			ForeColor = AccentColor,
			Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
			TextAlign = ContentAlignment.MiddleLeft,
			Margin = Padding.Empty
		}, 0, 0);
		card.Controls.Add(new Label
		{
			Text = "Choose the language used by the Studio. Package data and generated YAML are never translated or changed.",
			Dock = DockStyle.Fill,
			ForeColor = MutedColor,
			Font = new Font("Segoe UI", 8.8F),
			TextAlign = ContentAlignment.MiddleLeft,
			Margin = new Padding(0, 0, 16, 0)
		}, 1, 0);
		StudioComboBox selector = NewComboBox(214);
		selector.AccessibleName = "Interface language";
		selector.AccessibleDescription = "Changes only the Winget Manifest Studio interface language.";
		selector.Anchor = AnchorStyles.Left | AnchorStyles.Right;
		selector.SetItems(StudioLocalization.AvailableLanguages.Select(language => language.DisplayName));
		selector.SelectedIndex = StudioLocalization.IndexOf(currentInterfaceLanguage);
		selector.SelectedIndexChanged += (_, _) => ChangeLanguage(selector);
		languageBoxes.Add(selector);
		card.Controls.Add(selector, 2, 0);
		return card;
	}

	private Control CreateStudioUpdateCard()
	{
		studioDistribution = StudioUpdateService.DetectDistribution();
		StudioCard card = new()
		{
			AccessibleName = "Winget Manifest Studio application updates",
			Width = 1160,
			Height = 126,
			ColumnCount = 3,
			RowCount = 3,
			BackColor = CardColor,
			Padding = new Padding(18, 12, 18, 12),
			Margin = new Padding(0, 0, 0, 12),
			CornerRadius = 10
		};
		card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
		card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
		card.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
		card.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
		card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		card.Controls.Add(new Label
		{
			Text = "APPLICATION UPDATES",
			Dock = DockStyle.Fill,
			ForeColor = AccentColor,
			Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
			TextAlign = ContentAlignment.MiddleLeft,
			Margin = Padding.Empty
		}, 0, 0);
		studioUpdateTitleLabel = new Label
		{
			Dock = DockStyle.Fill,
			ForeColor = Color.White,
			Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
			TextAlign = ContentAlignment.MiddleLeft,
			Margin = Padding.Empty
		};
		studioUpdateDescriptionLabel = new Label
		{
			Dock = DockStyle.Fill,
			ForeColor = MutedColor,
			Font = new Font("Segoe UI", 8.8F),
			TextAlign = ContentAlignment.MiddleLeft,
			Margin = Padding.Empty,
			AutoEllipsis = true
		};
		studioUpdateStatusLabel = new Label
		{
			Dock = DockStyle.Fill,
			ForeColor = MutedColor,
			Font = new Font("Segoe UI", 8.8F),
			TextAlign = ContentAlignment.MiddleLeft,
			Margin = new Padding(0, 0, 16, 0),
			AutoEllipsis = true
		};
		studioUpdateButton = CreateButton("Check for updates", StudioUpdateButton_Click) as Button ?? throw new InvalidOperationException();
		studioUpdateButton.Dock = DockStyle.Fill;
		studioUpdateButton.Margin = new Padding(5, 3, 5, 3);
		card.Controls.Add(studioUpdateTitleLabel, 1, 0);
		card.Controls.Add(studioUpdateDescriptionLabel, 0, 1);
		card.SetColumnSpan(studioUpdateDescriptionLabel, 2);
		card.Controls.Add(studioUpdateButton, 2, 0);
		card.SetRowSpan(studioUpdateButton, 3);
		card.Controls.Add(studioUpdateStatusLabel, 0, 2);
		card.SetColumnSpan(studioUpdateStatusLabel, 2);
		RefreshStudioUpdateCard();
		return card;
	}

	private void RefreshStudioUpdateCard()
	{
		if (studioUpdateTitleLabel is null || studioUpdateTitleLabel.IsDisposed) return;
		studioUpdateTitleLabel.Text = $"Winget Manifest Studio {StudioUpdateService.CurrentVersionText}";
		studioUpdateDescriptionLabel.Text = studioDistribution == StudioDistributionKind.MsiInstalled
			? T("Installed with StudioSetup.msi. Updates use the matching MSI from the official GitHub release.")
			: T("Portable copy. Updates replace this EXE with the matching file from the official GitHub release.");

		string status;
		string action;
		bool enabled = true;
		switch (studioUpdateUiState)
		{
			case StudioUpdateUiState.Checking:
				status = T("Checking the latest stable GitHub release...");
				action = T("Checking...");
				enabled = false;
				break;
			case StudioUpdateUiState.Current:
				status = T("You have the latest stable version.");
				action = T("Check again");
				break;
			case StudioUpdateUiState.Available when availableStudioUpdate is not null:
				status = string.Format(T("Version {0} is available: {1}"), availableStudioUpdate.VersionText, availableStudioUpdate.Title);
				action = string.Format(T("Update to {0}"), availableStudioUpdate.VersionText);
				break;
			case StudioUpdateUiState.Error:
				status = string.Format(T("Update check needs attention: {0}"), studioUpdateError);
				action = T("Try again");
				break;
			case StudioUpdateUiState.Downloading:
				status = T("Downloading and verifying the selected update...");
				action = T("Downloading...");
				enabled = false;
				break;
			default:
				status = T("Updates are checked quietly after the Studio opens. You can also check now.");
				action = T("Check for updates");
				break;
		}
		studioUpdateStatusLabel.Text = status;
		studioUpdateButton.Text = action;
		studioUpdateButton.AccessibleName = action;
		studioUpdateButton.Enabled = enabled;
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
	private static StudioToggleSwitch NewToggleSwitch() => new()
	{
		AccessibleName = "Allow HTTP installer URLs",
		AccessibleDescription = "Off requires secure HTTPS download URLs. Turn this on only when an installer is available over HTTP.",
		ForeColor = MutedColor
	};

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
			Record(workspaceInitialized && workspaceTabs.TabPages.Count == 7 && navigationButtons.Count == 7
				&& workspaceTabs.TabPages.Cast<TabPage>().All(page => page.Controls.Count > 0),
				"The complete workspace is constructed before the first visible paint");
			Record(securityBadge.Right + 12 <= minimizeButton.Left, "Header badge and window buttons do not overlap",
				$"badge right {securityBadge.Right}, minimize left {minimizeButton.Left}");
			Record(closeButton.Left > minimizeButton.Right, "Minimize and Close buttons are aligned with a visible gap");
			SelectTab("Start Here");
			Record(studioUpdateButton.Text == "Check for updates"
				&& studioUpdateTitleLabel.Text.Contains(StudioUpdateService.CurrentVersionText, StringComparison.Ordinal)
				&& studioUpdateDescriptionLabel.Text.Contains(studioDistribution == StudioDistributionKind.MsiInstalled ? "StudioSetup.msi" : "Portable", StringComparison.OrdinalIgnoreCase),
				"Start clearly identifies the current version and the correct installed or portable update method");
			Size originalSize = Size;
			Size[] layoutMatrix =
			[
				MinimumSize,
				new Size(1280, 840),
				new Size(1536, 864),
				new Size(1600, 1000),
				new Size(1920, 1080)
			];
			foreach (Size testSize in layoutMatrix)
			{
				Size = testSize;
				PerformLayout();
				workspaceTabs.PerformLayout();
				LayoutHeaderControls();
				bool headerFits = securityBadge.Right + 12 <= minimizeButton.Left && closeButton.Right <= headerPanel.ClientSize.Width;
				bool navigationFits = navigationButtons.Values.All(button => button.Left >= 0 && button.Right <= navigationPanel.ClientSize.Width);
				Record(headerFits && navigationFits && workspaceTabs.ClientSize.Width > 0 && workspaceTabs.ClientSize.Height > 0,
					$"Responsive layout at {testSize.Width}×{testSize.Height}");
			}
			Record(DeviceDpi >= 96 && AutoScaleMode is AutoScaleMode.Font or AutoScaleMode.Dpi,
				$"Windows DPI scaling is active at {DeviceDpi} DPI", $"AutoScaleMode: {AutoScaleMode}");
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
			SelectTab("Test Center");
			Application.DoEvents();
			Record(testPlanLabel.Text.Contains("PROJECT", StringComparison.Ordinal)
				&& nextTestActionTitleLabel.Text.Length > 0
				&& nextTestActionDescriptionLabel.Text.Length > 0,
				"Test Center explains project readiness and one clear next action");
			TableLayoutPanel? testWorkspace = Descendants(this).OfType<TableLayoutPanel>()
				.FirstOrDefault(panel => panel.AccessibleName == "Test Center guided workspace");
			Record(testWorkspace is { ColumnCount: 2 }
				&& Descendants(testWorkspace).Any(control => control.AccessibleName == "Current test action and results")
				&& Descendants(testWorkspace).Any(control => control.AccessibleName == "Test checklist and optional tools"),
				"Test Center separates the current action, results, and checklist");
			Record(testProgressSteps.Length == 4 && testProgressSteps.All(step => step.Width > 120 && step.Height >= 70),
				"A four-step horizontal progress tracker replaces the repeated action cards");
			Record(nextTestActionButton.Height == 54 && nextTestActionButton.Parent is TableLayoutPanel,
				"The Test Center presents one large primary next-action button");
			Record(testStatusPills.Length == 4 && testStatusPills.All(pill => pill.Width > 80 && pill.Height is >= 26 and <= 32),
				"The compact checklist shows a distinct status for every required test",
				string.Join(", ", testStatusPills.Select(pill => $"{pill.Bounds} in {pill.Parent?.ClientRectangle}")));
			Record(testOptionalToolsCard is { Visible: false } && optionalToolsToggleButton.Visible,
				"Optional diagnostics are hidden by default to keep the required workflow uncluttered");
			Button? exportReportButton = Descendants(this).OfType<Button>().FirstOrDefault(button => button.Text == "Export test report");
			Record(exportReportButton?.Parent is TableLayoutPanel optionalTools
				&& optionalTools.Controls.OfType<Button>().Count() == 6
				&& optionalTools.GetColumnSpan(exportReportButton) == 1,
				"Optional tools grid includes the separate Sandbox uninstall test without an empty cell");
			string[] removedStartActions = ["Continue where you left off", "Restore Last Session", "Open Recent Project"];
			Record(!Descendants(this).Any(control => removedStartActions.Contains(control.Text, StringComparer.OrdinalIgnoreCase)),
				"Removed session recovery and recent-project actions are absent from the interface");
			SelectTab("Installers & Hashes");
			Application.DoEvents();
			Button? installerUrlStep = Descendants(this).OfType<Button>().FirstOrDefault(button => button.Text == "2 Enter Public URL");
			Record(installerUrlStep is { Visible: true }, "Installer workflow visibly includes step 2 for the public URL");

			SelectTab("Preview & Submit");
			Application.DoEvents();
			TableLayoutPanel? reviewWorkspace = Descendants(this).OfType<TableLayoutPanel>()
				.FirstOrDefault(panel => panel.AccessibleName == "Review guided workspace");
			Record(reviewWorkspace is { ColumnCount: 2 }
				&& Descendants(reviewWorkspace).Any(control => control.AccessibleName == "Current review action and plain-language results")
				&& Descendants(reviewWorkspace).Any(control => control.AccessibleName == "Review checklist and view options"),
				"Review uses the same guided action, results, and checklist structure as Test Center");
			Record(reviewProgressSteps.Length == 4 && reviewProgressSteps.All(step => step.Width > 120 && step.Height >= 70),
				"Review has a four-step horizontal progress tracker");
			Record(reviewNextActionButton.Height == 54 && reviewNextActionButton.Parent is TableLayoutPanel,
				"Review presents one large primary next-action button");
			Record(reviewStatusPills.Length == 4 && reviewStatusPills.All(pill => pill.Width > 80 && pill.Height is >= 26 and <= 32),
				"Review checklist has aligned status tags");
			Record(previewModeButton.Text == "Show technical YAML" && !showingTechnicalPreview,
				"Technical YAML is hidden behind a clearly named secondary action");

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
			StudioComboBox? optionalComboBox = comboBoxes.FirstOrDefault(comboBox => comboBox.AllowEmptySelection);
			if (optionalComboBox is not null)
			{
				int original = optionalComboBox.SelectedIndex;
				optionalComboBox.SelectedIndex = 0;
				optionalComboBox.SelectedIndex = -1;
				Record(optionalComboBox.Text.Length == 0, "Optional dropdowns provide a clear leave-blank choice");
				optionalComboBox.SelectedIndex = original;
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
			bool originalHttpSetting = insecureUrlCheck.Checked;
			insecureUrlCheck.Checked = !originalHttpSetting;
			Record(insecureUrlCheck.Checked != originalHttpSetting, "HTTP URL toggle changes between off and on");
			insecureUrlCheck.Checked = originalHttpSetting;
			SelectTab("Installers & Hashes");
			Record(fields.TryGetValue("ElevationRequirement", out Control? elevationControl)
				&& elevationControl.AccessibleDescription?.Contains("Administrator-based Sandbox", StringComparison.OrdinalIgnoreCase) == true,
				"Elevation guidance explains the elevationProhibited Sandbox limitation before testing");
			Size sizeBeforeSharedSettingsCheck = Size;
			Size minimumSizeBeforeSharedSettingsCheck = MinimumSize;
			MinimumSize = Size.Empty;
			Size = new Size(1022, 718);
			PerformLayout();
			workspaceTabs.PerformLayout();
			Application.DoEvents();
			Control? toggleWrapper = insecureUrlCheck.Parent;
			Control? toggleRow = toggleWrapper?.Parent;
			Record(toggleWrapper is not null && toggleRow is not null &&
				toggleWrapper.Right <= toggleRow.ClientSize.Width - toggleRow.Padding.Right,
				"HTTP URL toggle remains fully visible inside the shared-settings row",
				$"Field right: {toggleWrapper?.Right ?? 0}; row width: {toggleRow?.ClientSize.Width ?? 0}");
			Label? httpLabel = toggleWrapper?.Controls.OfType<Label>()
				.FirstOrDefault(label => label.Text == "Allow HTTP URLs");
			Record(httpLabel is { AutoSize: true } && string.IsNullOrEmpty(insecureUrlCheck.Text) && insecureUrlCheck.Width <= 80,
				"HTTP URL text is a separate label and only the small switch is clickable");
			Size = sizeBeforeSharedSettingsCheck;
			MinimumSize = minimumSizeBeforeSharedSettingsCheck;
			PerformLayout();
			workspaceTabs.PerformLayout();
			Application.DoEvents();

			ManifestProject importedProject = new()
			{
				PackageIdentifier = "Contoso.Imported",
				PackageVersion = "9.8.7",
				DefaultLocale = "en-US",
				ManifestVersion = "1.12.0",
				ManifestFolder = Path.Combine(Path.GetTempPath(), "WingetManifestStudioImportedUiTest"),
				Publisher = "Contoso Publisher",
				PackageName = "Imported Package",
				License = "MIT",
				ShortDescription = "Loaded from existing YAML"
			};
			importedProject.Installers.Add(new InstallerArtifact
			{
				InstallerUrl = "https://example.invalid/Imported.msi",
				Architecture = "x64",
				InstallerType = "msi",
				Sha256 = new string('C', 64)
			});
			project = importedProject;
			ApplyProjectToControls();
			Record(
				Read("PackageIdentifier") == "Contoso.Imported"
					&& Read("PackageVersion") == "9.8.7"
					&& Read("Publisher") == "Contoso Publisher"
					&& Read("PackageName") == "Imported Package"
					&& Read("ShortDescription") == "Loaded from existing YAML"
					&& installerGrid.Rows.Count == 1,
				"Loaded YAML values populate every package field and installer row");

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
			Record(reviewNextActionButton.Text == "Open the field to fix" && fieldErrors.GetError(fields["PackageIdentifier"]).Length > 0,
				"Invalid fields are explained and the guided review is blocked");
			Write("PackageIdentifier", "Contoso.Sample");
			RefreshReadiness();
			Record(reviewNextActionButton is { Enabled: true } && reviewNextActionButton.Text == "Preview changes" && readinessLabel.Text.StartsWith("READY", StringComparison.Ordinal),
				"Ready projects clearly unlock Preview as the only next step", string.Join(" | ", ManifestService.Validate(project)));
			GeneratePreview();
			bool simpleReviewVisible = simplePreviewText?.Contains("WHAT NEEDS ATTENTION", StringComparison.Ordinal) == true;
			Record(reviewNextActionButton is { Enabled: true } && reviewNextActionButton.Text == "Save manifests" && simpleReviewVisible && !showingTechnicalPreview,
				"A simple preview unlocks Save and keeps technical YAML hidden");
			SaveManifests();
			string savedReadiness = readinessLabel.Text ?? string.Empty;
			Record(reviewNextActionButton is { Enabled: true } && reviewNextActionButton.Text == "Validate locally" && savedReadiness.StartsWith("SAVED", StringComparison.Ordinal),
				"Saving unlocks Validate as the next step");
			SetReviewProgress(ReviewProgress.Validated);
			string validationReadiness = readinessLabel.Text ?? string.Empty;
			Record(reviewNextActionButton is { Enabled: true } && reviewNextActionButton.Text == "Open Test Center" && validationReadiness.StartsWith("VALIDATION PASSED", StringComparison.Ordinal),
				"Successful validation unlocks Test Center as the next step");
			successfulPreflightFingerprint = ProjectFingerprint();
			RefreshReadiness();
			Record(reviewNextActionButton.Text == "Open Test Center" && reviewProgressSteps[3].State == StudioStepState.Current,
				"Review keeps installation testing and submission in Test Center");
			localManifestFilesEnabled = true;
			successfulLocalInstallFingerprint = ProjectFingerprint();
			verifiedInstalledFingerprint = ProjectFingerprint();
			RefreshReadiness();
			Record(nextTestActionButton is { Enabled: true } && nextTestActionButton.Text == "Submit to Winget"
				&& testProgressSteps.All(step => step.State == StudioStepState.Complete),
				"Completing all four tests unlocks submission directly in Test Center");
			Record(reviewProgressSteps[3].State == StudioStepState.Complete && reviewNextActionButton.Text == "Open Test Center to submit",
				"Review reflects completed Test Center checks without duplicating the submission workflow");

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
			Record(fields.Values.All(control => !string.IsNullOrWhiteSpace(control.AccessibleDescription)), "Every package field explains what to enter or when to leave it blank");
			Record(actionButtons.All(button => !string.IsNullOrWhiteSpace(button.AccessibleName)), "Every action button has an accessible name");
			Record(readinessLabel.Width > 0 && readinessLabel.Height > 0, "Project readiness guidance is visible");
			Record(installerGrid.Columns.Count >= 9, "Installer grid contains the complete editing columns");
			Record(installerGrid.Columns.OfType<StudioDataGridViewChoiceColumn>().Count() == 4,
				"Installer choices use the same Studio-styled dropdown design as shared Scope");
			SelectTab("Installers & Hashes");
			if (installerGrid.Rows.Count > 0 && installerGrid.Columns
				.OfType<StudioDataGridViewChoiceColumn>()
				.FirstOrDefault()?.Index is int choiceColumnIndex &&
				installerGrid.Rows[0].Cells[choiceColumnIndex] is StudioDataGridViewChoiceCell choiceCell)
			{
				choiceCell.ExerciseDropDownLifecycle();
				Record(true, "Installer dropdown can open, close, and reopen without disposing an active popup");
			}
			else
			{
				Record(false, "Installer dropdown can open, close, and reopen without disposing an active popup", "No installer choice cell was available.");
			}
			SelectTab("Start Here");
			Record(BuildFileDialogFilter([".msi", ".exe"]).Contains("*.msi;*.exe", StringComparison.Ordinal), "Windows file picker filter includes every supported installer type");
			Record(fields["ManifestVersion"] is StudioComboBox schemaSelector
				&& !schemaSelector.Items.Contains("1.28.0")
				&& schemaSelector.Items.Contains("1.12.0")
				&& ManifestSchemaSupport.RecommendedForWinget("v1.29.290") == "1.12.0",
				"Schema selector offers only community-supported versions and recommends 1.12 for current Winget");
			Record(Descendants(this).OfType<Button>().Any(button => button.Text == "Open backup folder"),
				"Review exposes the recoverable manifest backup folder");
			Record(Descendants(this).OfType<Button>().Any(button => button.Text == "Sandbox install + uninstall"),
				"Test Center exposes a separate disposable install-and-uninstall test");
			string originalReleaseNotes = Read("ReleaseNotes");
			MarkProjectClean();
			Write("ReleaseNotes", originalReleaseNotes + " unsaved-test");
			Record(HasUnsavedChanges(), "Unsaved project edits are detected before replacement or close");
			Write("ReleaseNotes", originalReleaseNotes);
			MarkProjectClean();
			Record(languageBoxes.Count == 2 && StudioLocalization.AvailableLanguages.Count == 6
				&& languageBoxes.All(selector => selector.Items.Count == StudioLocalization.AvailableLanguages.Count),
				"Language settings on Start and Help offer all six interface languages");
			ApplyInterfaceLanguage("es-ES");
			IReadOnlyList<string> spanishUntranslated = FindUntranslatedInterfaceText("es-ES");
			Record(navigationButtons["Start Here"].Text.Contains("Inicio", StringComparison.Ordinal)
				&& reviewProgressSteps[0].Title == "Vista previa"
				&& testProgressSteps[0].Title == "Comprobación previa"
				&& installerGrid.Columns[nameof(InstallerArtifact.InstallerUrl)] is DataGridViewColumn installerUrlColumn
				&& installerUrlColumn.HeaderText == "URL PÚBLICA DEL INSTALADOR"
				&& studioUpdateButton.Text == "Buscar actualizaciones"
				&& reviewActionDescriptionLabel.Text.StartsWith("La versión del paquete", StringComparison.Ordinal)
				&& previewBox.Text.StartsWith("QUÉ REQUIERE ATENCIÓN", StringComparison.Ordinal)
				&& spanishUntranslated.Count == 0
				&& Descendants(this).OfType<Label>().Any(label => label.Text.StartsWith("Dependencias del paquete", StringComparison.Ordinal)),
				"Spanish translates all normal interface text, including package fields, Review, and Test Center",
				spanishUntranslated.Count == 0 ? null : "Untranslated: " + string.Join(" | ", spanishUntranslated));
			Dictionary<string, string> additionalNavigation = new(StringComparer.OrdinalIgnoreCase)
			{
				["fr-FR"] = "Centre de tests",
				["de-DE"] = "Testcenter",
				["pt-BR"] = "Central de testes",
				["ja-JP"] = "テストセンター"
			};
			foreach ((string language, string expectedTestCenter) in additionalNavigation)
			{
				ApplyInterfaceLanguage(language);
				PerformLayout();
				workspaceTabs.PerformLayout();
				IReadOnlyList<string> untranslated = FindUntranslatedInterfaceText(language);
				Record(navigationButtons["Test Center"].Text.Contains(expectedTestCenter, StringComparison.Ordinal)
					&& studioUpdateButton.Text != "Check for updates"
					&& installerGrid.Columns[nameof(InstallerArtifact.InstallerUrl)] is DataGridViewColumn translatedInstallerUrlColumn
					&& translatedInstallerUrlColumn.HeaderText != "PUBLIC INSTALLER URL"
					&& languageBoxes.All(selector => selector.SelectedIndex == StudioLocalization.IndexOf(language))
					&& navigationButtons.Values.All(button => button.Right <= navigationPanel.ClientSize.Width)
					&& untranslated.Count == 0,
					$"{StudioLocalization.AvailableLanguages[StudioLocalization.IndexOf(language)].DisplayName} translates all normal interface text and fits the interface",
					untranslated.Count == 0 ? null : "Untranslated: " + string.Join(" | ", untranslated));
			}
			ApplyInterfaceLanguage("en-US");
			Record(navigationButtons["Start Here"].Text == "1  Start" && studioUpdateButton.Text == "Check for updates" && reviewProgressSteps[0].Title == "Preview"
				&& reviewActionDescriptionLabel.Text.StartsWith("Package Version", StringComparison.Ordinal)
				&& previewBox.Text.StartsWith("WHAT NEEDS ATTENTION", StringComparison.Ordinal),
				"Switching back to English restores the original interface text");
			testOptionalToolsCard.Visible = false;
			optionalToolsToggleButton.Text = "Show optional tools";
		}
		catch (Exception ex)
		{
			Record(false, "Full interface verification", ex.ToString());
		}

		report.Insert(0, $"Winget Manifest Studio UI verification: {passed} passed, {failed} failed");
		return report;
	}

	internal void RenderTabForVerification(string title, string outputPath)
	{
		SelectTab(title);
		Size = new Size(1280, 840);
		PerformLayout();
		workspaceTabs.PerformLayout();
		Application.DoEvents();
		using Bitmap bitmap = new(ClientSize.Width, ClientSize.Height);
		DrawToBitmap(bitmap, ClientRectangle);
		bitmap.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
	}

	internal void SetLanguageForVerification(string language)
	{
		if (!uiTestMode) throw new InvalidOperationException("Interface verification language changes are available only in safe UI test mode.");
		ApplyInterfaceLanguage(language);
		string displayName = StudioLocalization.AvailableLanguages[StudioLocalization.IndexOf(language)].DisplayName;
		SetStatus(string.Format(T("Interface language changed to {0}."), displayName));
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

	private void ChangeLanguage(StudioComboBox source)
	{
		if (applyingLanguage) return;
		if (uiTestMode) { SetStatus("TEST: Language selection changed without writing application settings."); return; }
		string language = StudioLocalization.CodeAt(source.SelectedIndex);
		StudioStateStore.SetLanguage(language);
		ApplyInterfaceLanguage(language);
		string displayName = StudioLocalization.AvailableLanguages[StudioLocalization.IndexOf(language)].DisplayName;
		SetStatus(string.Format(T("Interface language changed to {0}."), displayName));
	}

	private void ApplyInterfaceLanguage(string language)
	{
		if (!StudioLocalization.IsSupported(language)) language = "en-US";
		currentInterfaceLanguage = language;
		applyingLanguage = true;
		try
		{
			if (!originalInterfaceText.ContainsKey(modeLabel)) originalInterfaceText[modeLabel] = modeLabel.Text;
			if (!originalInterfaceText.ContainsKey(securityBadge)) originalInterfaceText[securityBadge] = securityBadge.Text;
			foreach (Control control in DescendantsAndSelf(this))
			{
				bool localizable = control is Button or TabPage or CheckBox or Label || ReferenceEquals(control, this);
				if (!localizable || ReferenceEquals(control, statusLabel) || ReferenceEquals(control, modeLabel)
					|| ReferenceEquals(control, securityBadge) || ReferenceEquals(control, readinessLabel)
					|| ReferenceEquals(control, testPlanLabel))
					continue;
				if (!originalInterfaceText.TryGetValue(control, out string? english))
				{
					english = control.Text;
					originalInterfaceText[control] = english;
				}
				control.Text = StudioLocalization.Translate(english, language);
			}
			foreach (StudioTextBox textBox in DescendantsAndSelf(this).OfType<StudioTextBox>())
			{
				if (string.IsNullOrWhiteSpace(textBox.PlaceholderText)) continue;
				if (!originalPlaceholderText.TryGetValue(textBox, out string? englishPlaceholder))
				{
					englishPlaceholder = textBox.PlaceholderText;
					originalPlaceholderText[textBox] = englishPlaceholder;
				}
				textBox.PlaceholderText = StudioLocalization.Translate(englishPlaceholder, language);
			}
			int languageIndex = StudioLocalization.IndexOf(language);
			foreach (StudioComboBox selector in languageBoxes)
				selector.SelectedIndex = languageIndex;
			string[] reviewTitles = ["Preview", "Save safely", "Validate", "Test & submit"];
			for (int index = 0; index < reviewProgressSteps.Length; index++) reviewProgressSteps[index].Title = T(reviewTitles[index]);
			string[] testTitles = ["Safe preflight", "Allow testing", "Test install", "Verify result"];
			for (int index = 0; index < testProgressSteps.Length; index++) testProgressSteps[index].Title = T(testTitles[index]);
			LocalizeInstallerGridHeaders();
			if (originalInterfaceText.TryGetValue(modeLabel, out string? modeEnglish)) modeLabel.Text = T(modeEnglish);
			if (originalInterfaceText.TryGetValue(securityBadge, out string? securityEnglish)) securityBadge.Text = T(securityEnglish);
			statusLabel.Text = T(currentStatusEnglish);
			if (IsDefaultLocalizedText(toolOutputBox.Text, DefaultOfficialToolOutput)) toolOutputBox.Text = T(DefaultOfficialToolOutput);
			if (IsDefaultLocalizedText(testOutputBox.Text, DefaultTestOutput)) testOutputBox.Text = T(DefaultTestOutput);
			UpdateNavigationState();
			RefreshStudioUpdateCard();
		}
		finally { applyingLanguage = false; }
		if (workspaceInitialized) RefreshReadiness();
	}

	private string T(string english) => StudioLocalization.Translate(english, currentInterfaceLanguage);

	private static bool IsDefaultLocalizedText(string current, string english) =>
		StudioLocalization.AvailableLanguages.Any(language =>
			current.Equals(StudioLocalization.Translate(english, language.Code), StringComparison.Ordinal));

	private void SetInterfaceText(Control control, string english)
	{
		originalInterfaceText[control] = english;
		control.Text = T(english);
		if (control is Button) control.AccessibleName = control.Text;
	}

	private void LocalizeDynamicControls(params Control[] controls)
	{
		foreach (Control control in controls)
			SetInterfaceText(control, control.Text);
	}

	private void SetLocalizedReadinessError(Control control, string error)
	{
		string simpleError = SimplifyReadinessError(error);
		string english = simpleError + ". The Studio will return you to the correct page.";
		originalInterfaceText[control] = english;
		control.Text = LocalizeReadinessMessage(error) + ". " + T("The Studio will return you to the correct page.");
	}

	private string LocalizeReadinessMessage(string error)
	{
		string simple = SimplifyReadinessError(error);
		string translated = T(simple);
		if (!translated.Equals(simple, StringComparison.Ordinal)) return translated;
		if (!currentInterfaceLanguage.Equals("es-ES", StringComparison.OrdinalIgnoreCase)) return simple;
		return simple
			.Replace("Installer ", "Instalador ", StringComparison.Ordinal)
			.Replace(" needs a valid public download URL", " necesita una URL pública de descarga válida", StringComparison.OrdinalIgnoreCase)
			.Replace(" needs a calculated 64-character SHA-256 hash", " necesita un hash SHA-256 calculado de 64 caracteres", StringComparison.OrdinalIgnoreCase)
			.Replace(" needs an architecture. Inspect its local file or choose x86, x64, arm, arm64, or neutral", " necesita una arquitectura. Inspecciona el archivo local o elige x86, x64, arm, arm64 o neutral", StringComparison.OrdinalIgnoreCase)
			.Replace(" needs an Installer Type. Inspect its local file or choose the correct type", " necesita un tipo de instalador. Inspecciona el archivo local o elige el tipo correcto", StringComparison.OrdinalIgnoreCase)
			.Replace(" failed public URL verification. The public download must match the attached local file", " no superó la verificación de la URL pública. La descarga debe coincidir con el archivo local adjunto", StringComparison.OrdinalIgnoreCase);
	}

	private void LocalizeInstallerGridHeaders()
	{
		if (installerGrid is null || installerGrid.IsDisposed) return;
		Dictionary<string, string> headers = new(StringComparer.Ordinal)
		{
			[nameof(InstallerArtifact.LocalFile)] = "LOCAL RELEASE FILE",
			[nameof(InstallerArtifact.InstallerUrl)] = "PUBLIC INSTALLER URL",
			[nameof(InstallerArtifact.Architecture)] = "ARCH",
			[nameof(InstallerArtifact.InstallerType)] = "TYPE",
			[nameof(InstallerArtifact.Scope)] = "SCOPE",
			[nameof(InstallerArtifact.VerificationStatus)] = "HASH SOURCE / STATUS",
			[nameof(InstallerArtifact.AnalysisSummary)] = "INSTALLER ANALYSIS",
			[nameof(InstallerArtifact.NestedInstallerType)] = "NESTED TYPE",
			[nameof(InstallerArtifact.NestedInstallerFiles)] = "ZIP CONTENTS",
			[nameof(InstallerArtifact.SignatureStatus)] = "DIGITAL SIGNATURE",
			[nameof(InstallerArtifact.SignerName)] = "SIGNER",
			[nameof(InstallerArtifact.Sha256)] = "SHA-256",
			[nameof(InstallerArtifact.ProductCode)] = "PRODUCT CODE",
			[nameof(InstallerArtifact.UpgradeCode)] = "UPGRADE CODE",
			[nameof(InstallerArtifact.SignatureSha256)] = "MSIX SIGNATURE SHA-256",
			[nameof(InstallerArtifact.AdvancedFieldsYaml)] = "ADDITIONAL ROW YAML"
		};
		foreach ((string property, string english) in headers)
			if (installerGrid.Columns[property] is DataGridViewColumn column) column.HeaderText = T(english);
	}

	private IReadOnlyList<string> FindUntranslatedInterfaceText(string language)
	{
		HashSet<string> missing = new(StringComparer.Ordinal);
		foreach (string english in originalInterfaceText.Values.Concat(originalPlaceholderText.Values))
		{
			if (string.IsNullOrWhiteSpace(english) || !System.Text.RegularExpressions.Regex.IsMatch(english, "[A-Za-z]{3,}")) continue;
			if (english.Equals("Winget Manifest Studio", StringComparison.Ordinal)
				|| english.StartsWith("Winget Manifest Studio ", StringComparison.Ordinal)
				|| english is "English" or "Español" or "SHA-256") continue;
			if (!StudioLocalization.HasCompleteTranslation(english, language)) missing.Add(english);
		}
		return missing.Order(StringComparer.Ordinal).ToArray();
	}

	private static IEnumerable<Control> DescendantsAndSelf(Control root)
	{
		yield return root;
		foreach (Control child in root.Controls)
			foreach (Control descendant in DescendantsAndSelf(child))
				yield return descendant;
	}

	private void SetStatus(string message)
	{
		currentStatusEnglish = message;
		statusLabel.Text = T(message);
	}

	private void SetModeText(string english)
	{
		originalInterfaceText[modeLabel] = english;
		modeLabel.Text = T(english);
	}

	private void SetSecurityText(string english)
	{
		originalInterfaceText[securityBadge] = english;
		securityBadge.Text = T(english);
	}
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
