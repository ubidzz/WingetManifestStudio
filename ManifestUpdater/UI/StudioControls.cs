using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Drawing2D;

namespace ManifestUpdater;

internal static class StudioPalette
{
	public static readonly Color Window = Color.FromArgb(5, 14, 27);
	public static readonly Color Header = Color.FromArgb(6, 17, 31);
	public static readonly Color Sidebar = Color.FromArgb(7, 20, 37);
	public static readonly Color Card = Color.FromArgb(16, 32, 54);
	public static readonly Color CardHover = Color.FromArgb(22, 46, 70);
	public static readonly Color Input = Color.FromArgb(7, 20, 37);
	public static readonly Color Border = Color.FromArgb(43, 68, 101);
	public static readonly Color Divider = Color.FromArgb(35, 57, 86);
	public static readonly Color Selection = Color.FromArgb(25, 69, 88);
	public static readonly Color Accent = Color.FromArgb(32, 215, 202);
	public static readonly Color AccentSoft = Color.FromArgb(9, 48, 58);
	public static readonly Color AccentHover = Color.FromArgb(76, 232, 219);
	public static readonly Color PrimaryText = Color.FromArgb(246, 249, 255);
	public static readonly Color Text = PrimaryText;
	public static readonly Color SecondaryText = Color.FromArgb(176, 201, 235);
	public static readonly Color MutedText = Color.FromArgb(126, 154, 194);
	public static readonly Color Muted = MutedText;
	public static readonly Color Success = Color.FromArgb(67, 231, 170);
	public static readonly Color Warning = Color.FromArgb(255, 190, 52);
}

internal static class StudioGeometry
{
	public static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
	{
		GraphicsPath path = new();
		int diameter = Math.Max(1, Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height)));
		if (diameter <= 2)
		{
			path.AddRectangle(bounds);
			return path;
		}

		Rectangle arc = new(bounds.Location, new Size(diameter, diameter));
		path.AddArc(arc, 180, 90);
		arc.X = bounds.Right - diameter;
		path.AddArc(arc, 270, 90);
		arc.Y = bounds.Bottom - diameter;
		path.AddArc(arc, 0, 90);
		arc.X = bounds.Left;
		path.AddArc(arc, 90, 90);
		path.CloseFigure();
		return path;
	}
}

internal sealed class StudioCard : TableLayoutPanel
{
	[DefaultValue(12)]
	public int CornerRadius { get; set; } = 12;

	public StudioCard()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
		BackColor = StudioPalette.Card;
	}

	protected override void OnPaintBackground(PaintEventArgs eventArgs)
	{
		eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		eventArgs.Graphics.Clear(Parent?.BackColor ?? StudioPalette.Window);
		Rectangle bounds = new(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
		using GraphicsPath path = StudioGeometry.RoundedRectangle(bounds, CornerRadius);
		using SolidBrush brush = new(BackColor);
		eventArgs.Graphics.FillPath(brush, path);
	}

	protected override void OnPaint(PaintEventArgs eventArgs)
	{
		base.OnPaint(eventArgs);
		eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		Rectangle bounds = new(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
		using GraphicsPath path = StudioGeometry.RoundedRectangle(bounds, CornerRadius);
		using Pen pen = new(StudioPalette.Border);
		eventArgs.Graphics.DrawPath(pen, path);
	}
}

internal enum StudioStepState
{
	Pending,
	Current,
	Complete,
	Problem
}

internal sealed class StudioTestProgressStep : Control
{
	private readonly Font titleFont = new("Segoe UI Semibold", 9F, FontStyle.Bold);
	private readonly Font statusFont = new("Segoe UI", 8F);
	private StudioStepState state;
	private string title = string.Empty;
	private string statusText = "Waiting";

	[DefaultValue(1)]
	public int StepNumber { get; set; } = 1;

	[DefaultValue(false)]
	public bool IsFirst { get; set; }

	[DefaultValue(false)]
	public bool IsLast { get; set; }

	[DefaultValue(StudioStepState.Pending)]
	public StudioStepState State
	{
		get => state;
		set { state = value; Invalidate(); }
	}

	[DefaultValue("")]
	public string Title
	{
		get => title;
		set { title = value ?? string.Empty; Invalidate(); }
	}

	[DefaultValue("Waiting")]
	public string StatusText
	{
		get => statusText;
		set { statusText = value ?? string.Empty; Invalidate(); }
	}

	public StudioTestProgressStep()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
		BackColor = StudioPalette.Card;
		MinimumSize = new Size(130, 72);
		AccessibleRole = AccessibleRole.Indicator;
	}

	protected override void OnPaint(PaintEventArgs eventArgs)
	{
		base.OnPaint(eventArgs);
		Graphics graphics = eventArgs.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.Clear(BackColor);

		int centerX = Width / 2;
		const int centerY = 22;
		const int radius = 13;
		Color completedLine = state == StudioStepState.Complete ? StudioPalette.Accent : StudioPalette.Divider;
		using Pen beforePen = new(state is StudioStepState.Complete or StudioStepState.Current ? StudioPalette.Accent : StudioPalette.Divider, 2F);
		using Pen afterPen = new(completedLine, 2F);
		if (!IsFirst) graphics.DrawLine(beforePen, 0, centerY, centerX - radius, centerY);
		if (!IsLast) graphics.DrawLine(afterPen, centerX + radius, centerY, Width, centerY);

		(Color fill, Color border, Color text) = state switch
		{
			StudioStepState.Complete => (StudioPalette.Accent, StudioPalette.Accent, Color.FromArgb(2, 24, 28)),
			StudioStepState.Current => (StudioPalette.AccentSoft, StudioPalette.Accent, StudioPalette.Accent),
			StudioStepState.Problem => (Color.FromArgb(62, 45, 18), StudioPalette.Warning, StudioPalette.Warning),
			_ => (StudioPalette.Input, StudioPalette.Divider, StudioPalette.MutedText)
		};
		Rectangle circle = new(centerX - radius, centerY - radius, radius * 2, radius * 2);
		using SolidBrush fillBrush = new(fill);
		using Pen borderPen = new(border, state == StudioStepState.Current ? 2F : 1F);
		graphics.FillEllipse(fillBrush, circle);
		graphics.DrawEllipse(borderPen, circle);

		string marker = state == StudioStepState.Complete ? "✓" : StepNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
		TextRenderer.DrawText(graphics, marker, titleFont, circle, text,
			TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
		Rectangle titleBounds = new(4, 42, Math.Max(1, Width - 8), 19);
		Rectangle statusBounds = new(4, 60, Math.Max(1, Width - 8), 18);
		TextRenderer.DrawText(graphics, title, titleFont, titleBounds, StudioPalette.PrimaryText,
			TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
		TextRenderer.DrawText(graphics, statusText, statusFont, statusBounds,
			state == StudioStepState.Problem ? StudioPalette.Warning : state == StudioStepState.Complete ? StudioPalette.Success : StudioPalette.MutedText,
			TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			titleFont.Dispose();
			statusFont.Dispose();
		}
		base.Dispose(disposing);
	}
}

internal sealed class StudioStatusPill : Control
{
	private StudioStepState state;

	[DefaultValue(StudioStepState.Pending)]
	public StudioStepState State
	{
		get => state;
		set { state = value; Invalidate(); }
	}

	public StudioStatusPill()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
		BackColor = StudioPalette.Card;
		ForeColor = StudioPalette.PrimaryText;
		Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold);
		MinimumSize = new Size(86, 26);
		Size = new Size(96, 26);
		AccessibleRole = AccessibleRole.StaticText;
	}

	protected override void OnPaint(PaintEventArgs eventArgs)
	{
		eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		eventArgs.Graphics.Clear(Parent?.BackColor ?? StudioPalette.Card);
		(Color fill, Color border, Color text) = state switch
		{
			StudioStepState.Complete => (Color.FromArgb(13, 61, 49), Color.FromArgb(42, 128, 99), StudioPalette.Success),
			StudioStepState.Current => (StudioPalette.AccentSoft, StudioPalette.Accent, StudioPalette.Accent),
			StudioStepState.Problem => (Color.FromArgb(62, 45, 18), Color.FromArgb(132, 94, 24), StudioPalette.Warning),
			_ => (StudioPalette.Input, StudioPalette.Divider, StudioPalette.MutedText)
		};
		Rectangle bounds = new(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
		using GraphicsPath path = StudioGeometry.RoundedRectangle(bounds, 7);
		using SolidBrush fillBrush = new(fill);
		using Pen borderPen = new(border);
		eventArgs.Graphics.FillPath(fillBrush, path);
		eventArgs.Graphics.DrawPath(borderPen, path);
		TextRenderer.DrawText(eventArgs.Graphics, Text, Font, bounds, text,
			TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
	}
}

internal enum StudioButtonKind
{
	Secondary,
	Primary,
	Title,
	Danger
}

internal class StudioButton : Button
{
	private bool hovered;
	private bool pressed;
	private StudioButtonKind buttonKind;

	[DefaultValue(StudioButtonKind.Secondary)]
	public StudioButtonKind ButtonKind
	{
		get => buttonKind;
		set { buttonKind = value; Invalidate(); }
	}

	[DefaultValue(9)]
	public int CornerRadius { get; set; } = 9;

	public StudioButton()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
		FlatStyle = FlatStyle.Flat;
		FlatAppearance.BorderSize = 0;
		UseVisualStyleBackColor = false;
		UseMnemonic = false;
		Cursor = Cursors.Hand;
		ForeColor = StudioPalette.PrimaryText;
		Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
		Height = 42;
		MinimumSize = new Size(112, 42);
		Padding = new Padding(13, 0, 13, 0);
	}

	protected override void OnMouseEnter(EventArgs eventArgs) { hovered = true; Invalidate(); base.OnMouseEnter(eventArgs); }
	protected override void OnMouseLeave(EventArgs eventArgs) { hovered = false; pressed = false; Invalidate(); base.OnMouseLeave(eventArgs); }
	protected override void OnMouseDown(MouseEventArgs eventArgs) { pressed = eventArgs.Button == MouseButtons.Left; Invalidate(); base.OnMouseDown(eventArgs); }
	protected override void OnMouseUp(MouseEventArgs eventArgs) { pressed = false; Invalidate(); base.OnMouseUp(eventArgs); }
	protected override void OnEnabledChanged(EventArgs eventArgs) { base.OnEnabledChanged(eventArgs); Invalidate(); }

	protected override void OnPaintBackground(PaintEventArgs eventArgs) => eventArgs.Graphics.Clear(Parent?.BackColor ?? StudioPalette.Window);

	protected override void OnPaint(PaintEventArgs eventArgs)
	{
		OnPaintBackground(eventArgs);
		Graphics graphics = eventArgs.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		Rectangle bounds = new(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));

		(Color fill, Color border, Color text) = ResolveColors();
		using GraphicsPath path = StudioGeometry.RoundedRectangle(bounds, CornerRadius);
		using SolidBrush fillBrush = new(fill);
		using Pen borderPen = new(border);
		graphics.FillPath(fillBrush, path);
		graphics.DrawPath(borderPen, path);

		Rectangle textBounds = Rectangle.Inflate(bounds, -Padding.Horizontal / 2, 0);
		TextRenderer.DrawText(graphics, Text, Font, textBounds, text,
			TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
	}

	private (Color fill, Color border, Color text) ResolveColors()
	{
		if (!Enabled) return (Color.FromArgb(18, 32, 49), Color.FromArgb(35, 53, 77), StudioPalette.MutedText);

		Color fill = buttonKind switch
		{
			StudioButtonKind.Primary => StudioPalette.Accent,
			StudioButtonKind.Title => Color.Transparent,
			StudioButtonKind.Danger => Color.FromArgb(91, 31, 47),
			_ => StudioPalette.Input
		};
		Color border = buttonKind switch
		{
			StudioButtonKind.Primary => StudioPalette.Accent,
			StudioButtonKind.Title => hovered ? StudioPalette.Border : Color.Transparent,
			StudioButtonKind.Danger => Color.FromArgb(143, 49, 67),
			_ => StudioPalette.Border
		};
		Color text = buttonKind == StudioButtonKind.Primary ? Color.FromArgb(2, 24, 28) : StudioPalette.PrimaryText;
		if (this is StudioNavButton navigation && navigation.Selected)
		{
			fill = StudioPalette.AccentSoft;
			border = StudioPalette.Border;
			text = StudioPalette.Accent;
		}

		if (hovered)
		{
			fill = buttonKind switch
			{
				StudioButtonKind.Primary => StudioPalette.AccentHover,
				StudioButtonKind.Title => StudioPalette.CardHover,
				_ => ControlPaint.Light(fill, 0.08F)
			};
			border = buttonKind == StudioButtonKind.Primary ? StudioPalette.AccentHover : ControlPaint.Light(border, 0.12F);
		}
		if (pressed) fill = ControlPaint.Dark(fill, 0.10F);
		return (fill, border, text);
	}
}

internal sealed class StudioWorkspaceTabs : TabControl
{
	private const int TcmAdjustRect = 0x1328;

	public StudioWorkspaceTabs()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
		Appearance = TabAppearance.FlatButtons;
		ItemSize = new Size(0, 1);
		SizeMode = TabSizeMode.Fixed;
		Multiline = true;
	}

	protected override void WndProc(ref Message message)
	{
		if (message.Msg == TcmAdjustRect && !DesignMode)
		{
			message.Result = (IntPtr)1;
			return;
		}
		base.WndProc(ref message);
	}

	protected override void OnPaint(PaintEventArgs eventArgs)
	{
		eventArgs.Graphics.Clear(StudioPalette.Window);
	}
}

internal sealed class StudioNavButton : StudioButton
{
	private bool selected;

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool Selected
	{
		get => selected;
		set { selected = value; Invalidate(); }
	}

	public StudioNavButton()
	{
		CornerRadius = 10;
		ButtonKind = StudioButtonKind.Title;
		Dock = DockStyle.Fill;
		Margin = new Padding(5, 8, 5, 8);
		Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
	}

	protected override void OnPaint(PaintEventArgs eventArgs)
	{
		base.OnPaint(eventArgs);
		if (!Selected) return;

		Graphics graphics = eventArgs.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		Rectangle highlight = new(8, Height - 5, Math.Max(1, Width - 16), 3);
		using GraphicsPath path = StudioGeometry.RoundedRectangle(highlight, 2);
		using SolidBrush brush = new(StudioPalette.Accent);
		graphics.FillPath(brush, path);
	}
}

internal sealed class StudioDataGridView : DataGridView
{
	public StudioDataGridView()
	{
		DoubleBuffered = true;
		AutoGenerateColumns = false;
		AllowUserToAddRows = false;
		AllowUserToDeleteRows = false;
		AllowUserToResizeRows = false;
		BackgroundColor = StudioPalette.Input;
		BorderStyle = BorderStyle.None;
		CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
		GridColor = StudioPalette.Divider;
		ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
		ColumnHeadersHeight = 44;
		ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
		EnableHeadersVisualStyles = false;
		RowHeadersVisible = false;
		RowTemplate.Height = 44;
		SelectionMode = DataGridViewSelectionMode.FullRowSelect;
		MultiSelect = false;
		AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
		EditMode = DataGridViewEditMode.EditOnEnter;
		EditingControlShowing += (_, eventArgs) =>
		{
			if (eventArgs.Control is not TextBox editor) return;
			editor.BorderStyle = BorderStyle.None;
			editor.BackColor = StudioPalette.CardHover;
			editor.ForeColor = StudioPalette.PrimaryText;
			editor.Font = new Font("Segoe UI", 9.25F);
		};

		ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
		{
			Alignment = DataGridViewContentAlignment.MiddleLeft,
			BackColor = StudioPalette.Sidebar,
			ForeColor = StudioPalette.SecondaryText,
			Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
			Padding = new Padding(10, 0, 6, 0),
			SelectionBackColor = StudioPalette.Sidebar,
			SelectionForeColor = StudioPalette.SecondaryText
		};
		DefaultCellStyle = new DataGridViewCellStyle
		{
			Alignment = DataGridViewContentAlignment.MiddleLeft,
			BackColor = StudioPalette.Input,
			ForeColor = StudioPalette.PrimaryText,
			Font = new Font("Segoe UI", 9.25F),
			Padding = new Padding(10, 0, 6, 0),
			SelectionBackColor = StudioPalette.Selection,
			SelectionForeColor = StudioPalette.PrimaryText
		};
		AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
		{
			BackColor = Color.FromArgb(10, 25, 43),
			ForeColor = StudioPalette.PrimaryText,
			SelectionBackColor = StudioPalette.Selection,
			SelectionForeColor = StudioPalette.PrimaryText
		};
	}

	protected override void OnCellPainting(DataGridViewCellPaintingEventArgs eventArgs)
	{
		if (eventArgs.RowIndex >= 0)
		{
			eventArgs.Paint(eventArgs.CellBounds, eventArgs.PaintParts & ~DataGridViewPaintParts.Focus);
			eventArgs.Handled = true;
			return;
		}
		base.OnCellPainting(eventArgs);
	}
}

internal sealed class StudioTextBox : UserControl
{
	private readonly TextBox editor = new();
	private bool focused;
	private int cornerRadius = 8;

	[DefaultValue(8)]
	public int CornerRadius
	{
		get => cornerRadius;
		set { cornerRadius = Math.Max(2, value); Invalidate(); }
	}

	[Browsable(true)]
	[EditorBrowsable(EditorBrowsableState.Always)]
	[AllowNull]
	public override string Text
	{
		get => editor.Text;
		set => editor.Text = value ?? string.Empty;
	}

	[DefaultValue("")]
	public string PlaceholderText
	{
		get => editor.PlaceholderText;
		set => editor.PlaceholderText = value ?? string.Empty;
	}

	[DefaultValue(false)]
	public bool Multiline
	{
		get => editor.Multiline;
		set
		{
			editor.Multiline = value;
			editor.AcceptsReturn = value;
			editor.ScrollBars = value ? ScrollBars.Vertical : ScrollBars.None;
			Padding = value ? new Padding(12, 10, 8, 10) : new Padding(12, 9, 12, 7);
		}
	}

	[DefaultValue(false)]
	public bool ReadOnly
	{
		get => editor.ReadOnly;
		set => editor.ReadOnly = value;
	}

	public StudioTextBox()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
		BackColor = StudioPalette.Window;
		ForeColor = StudioPalette.PrimaryText;
		Font = new Font("Segoe UI", 9.5F);
		MinimumSize = new Size(90, 38);
		Size = new Size(240, 38);
		Padding = new Padding(12, 9, 12, 7);
		TabStop = true;

		editor.BorderStyle = BorderStyle.None;
		editor.Dock = DockStyle.Fill;
		editor.BackColor = StudioPalette.Input;
		editor.ForeColor = StudioPalette.PrimaryText;
		editor.Font = Font;
		editor.TabStop = true;
		editor.TextChanged += (_, eventArgs) => base.OnTextChanged(eventArgs);
		editor.Enter += (_, _) => { focused = true; Invalidate(); };
		editor.Leave += (_, _) => { focused = false; Invalidate(); };
		Controls.Add(editor);
		Click += (_, _) => editor.Focus();
	}

	protected override void OnFontChanged(EventArgs eventArgs)
	{
		base.OnFontChanged(eventArgs);
		editor.Font = Font;
	}

	protected override void OnForeColorChanged(EventArgs eventArgs)
	{
		base.OnForeColorChanged(eventArgs);
		editor.ForeColor = ForeColor;
	}

	protected override void OnPaintBackground(PaintEventArgs eventArgs)
	{
		eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		eventArgs.Graphics.Clear(Parent?.BackColor ?? StudioPalette.Window);
		Rectangle bounds = new(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
		using GraphicsPath path = StudioGeometry.RoundedRectangle(bounds, cornerRadius);
		using SolidBrush fill = new(Enabled ? StudioPalette.Input : Color.FromArgb(11, 24, 40));
		using Pen border = new(focused ? StudioPalette.Accent : StudioPalette.Border, focused ? 1.5F : 1F);
		eventArgs.Graphics.FillPath(fill, path);
		eventArgs.Graphics.DrawPath(border, path);
	}

	protected override void OnEnter(EventArgs eventArgs)
	{
		base.OnEnter(eventArgs);
		editor.Focus();
	}

	protected override void OnEnabledChanged(EventArgs eventArgs)
	{
		base.OnEnabledChanged(eventArgs);
		editor.Enabled = Enabled;
		Invalidate();
	}

	public void SelectAll() => editor.SelectAll();
}

internal sealed class StudioComboBox : Control
{
	private readonly List<string> items = [];
	private int selectedIndex = -1;
	private bool hovered;
	private bool droppedDown;
	private ContextMenuStrip? dropDownMenu;

	public event EventHandler? SelectedIndexChanged;
	[DefaultValue(false)]
	public bool AllowEmptySelection { get; set; }

	[Browsable(false)]
	public IReadOnlyList<string> Items => items;

	[DefaultValue(-1)]
	public int SelectedIndex
	{
		get => selectedIndex;
		set
		{
			int normalized = value >= 0 && value < items.Count ? value : -1;
			if (selectedIndex == normalized) return;
			selectedIndex = normalized;
			base.Text = selectedIndex >= 0 ? items[selectedIndex] : string.Empty;
			SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
			Invalidate();
		}
	}

	[AllowNull]
	public override string Text
	{
		get => selectedIndex >= 0 ? items[selectedIndex] : base.Text;
		set
		{
			string normalized = value ?? string.Empty;
			int match = items.FindIndex(item => string.Equals(item, normalized, StringComparison.OrdinalIgnoreCase));
			if (match >= 0) SelectedIndex = match;
			else { selectedIndex = -1; base.Text = normalized; Invalidate(); }
		}
	}

	public StudioComboBox()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
		BackColor = StudioPalette.Input;
		ForeColor = StudioPalette.PrimaryText;
		Font = new Font("Segoe UI", 9.5F);
		Cursor = Cursors.Hand;
		MinimumSize = new Size(100, 38);
		Size = new Size(220, 38);
		TabStop = true;
	}

	public void SetItems(IEnumerable<string> values)
	{
		string current = Text;
		items.Clear();
		items.AddRange(values.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase));
		int match = items.FindIndex(item => string.Equals(item, current, StringComparison.OrdinalIgnoreCase));
		SelectedIndex = match;
		Invalidate();
	}

	protected override void OnMouseEnter(EventArgs eventArgs) { hovered = true; Invalidate(); base.OnMouseEnter(eventArgs); }
	protected override void OnMouseLeave(EventArgs eventArgs) { hovered = false; Invalidate(); base.OnMouseLeave(eventArgs); }
	protected override void OnClick(EventArgs eventArgs) { base.OnClick(eventArgs); Focus(); ShowDropDown(); }
	protected override void OnKeyDown(KeyEventArgs eventArgs)
	{
		base.OnKeyDown(eventArgs);
		if (eventArgs.KeyCode is Keys.Enter or Keys.Space || (eventArgs.Alt && eventArgs.KeyCode == Keys.Down))
		{
			ShowDropDown();
			eventArgs.Handled = true;
		}
		else if (eventArgs.KeyCode == Keys.Down && items.Count > 0)
		{
			SelectedIndex = Math.Min(items.Count - 1, selectedIndex + 1);
			eventArgs.Handled = true;
		}
		else if (eventArgs.KeyCode == Keys.Up && items.Count > 0)
		{
			SelectedIndex = Math.Max(0, selectedIndex <= 0 ? 0 : selectedIndex - 1);
			eventArgs.Handled = true;
		}
		else if (AllowEmptySelection && eventArgs.KeyCode is Keys.Delete or Keys.Back)
		{
			SelectedIndex = -1;
			eventArgs.Handled = true;
		}
	}

	private void ShowDropDown()
	{
		if (items.Count == 0 || droppedDown) return;
		dropDownMenu?.Dispose();
		ContextMenuStrip menu = new()
		{
			AutoSize = false,
			ShowImageMargin = false,
			ShowCheckMargin = false,
			BackColor = StudioPalette.Card,
			ForeColor = StudioPalette.PrimaryText,
			Font = Font,
			Padding = new Padding(4),
			Size = new Size(Width, Math.Min(320, (items.Count + (AllowEmptySelection ? 1 : 0)) * 36 + 8)),
			Renderer = new StudioMenuRenderer()
		};
		if (AllowEmptySelection)
		{
			ToolStripMenuItem blankItem = new("Leave blank")
			{
				AutoSize = false,
				Size = new Size(Math.Max(40, Width - 10), 34),
				Checked = selectedIndex < 0,
				CheckOnClick = false
			};
			blankItem.Click += (_, _) => SelectedIndex = -1;
			menu.Items.Add(blankItem);
		}
		for (int index = 0; index < items.Count; index++)
		{
			int capturedIndex = index;
			ToolStripMenuItem menuItem = new(items[index])
			{
				AutoSize = false,
				Size = new Size(Math.Max(40, Width - 10), 34),
				Checked = capturedIndex == selectedIndex,
				CheckOnClick = false
			};
			menuItem.Click += (_, _) => SelectedIndex = capturedIndex;
			menu.Items.Add(menuItem);
		}
		dropDownMenu = menu;
		menu.Closed += (_, _) => { droppedDown = false; Invalidate(); };
		menu.Disposed += (_, _) =>
		{
			if (ReferenceEquals(dropDownMenu, menu)) dropDownMenu = null;
		};
		droppedDown = true;
		menu.Show(this, new Point(0, Height + 2));
		Invalidate();
	}

	internal void ExerciseDropDownLifecycle()
	{
		ShowDropDown();
		Application.DoEvents();
		dropDownMenu?.Close();
		Application.DoEvents();
		ShowDropDown();
		Application.DoEvents();
		dropDownMenu?.Close();
		Application.DoEvents();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			dropDownMenu?.Dispose();
			dropDownMenu = null;
		}
		base.Dispose(disposing);
	}

	protected override void OnPaint(PaintEventArgs eventArgs)
	{
		eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		eventArgs.Graphics.Clear(Parent?.BackColor ?? StudioPalette.Window);
		Rectangle bounds = new(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
		using GraphicsPath path = StudioGeometry.RoundedRectangle(bounds, 8);
		using SolidBrush fill = new(hovered || Focused || droppedDown ? StudioPalette.CardHover : StudioPalette.Input);
		using Pen border = new(Focused || droppedDown ? StudioPalette.Accent : StudioPalette.Border, Focused || droppedDown ? 1.5F : 1F);
		eventArgs.Graphics.FillPath(fill, path);
		eventArgs.Graphics.DrawPath(border, path);

		Rectangle textBounds = new(13, 1, Math.Max(1, Width - 50), Height - 2);
		TextRenderer.DrawText(eventArgs.Graphics, Text, Font, textBounds, ForeColor,
			TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
		Point center = new(Width - 20, Height / 2);
		Point[] arrow = [new(center.X - 5, center.Y - 2), new(center.X + 5, center.Y - 2), new(center.X, center.Y + 4)];
		using SolidBrush arrowBrush = new(StudioPalette.Accent);
		eventArgs.Graphics.FillPolygon(arrowBrush, arrow);
	}
}

internal sealed class StudioMenuRenderer : ToolStripProfessionalRenderer
{
	public StudioMenuRenderer() : base(new StudioMenuColorTable()) { RoundedEdges = true; }

	protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs eventArgs)
	{
		Rectangle bounds = new(2, 1, Math.Max(1, eventArgs.Item.Width - 4), Math.Max(1, eventArgs.Item.Height - 2));
		using SolidBrush brush = new(eventArgs.Item.Selected ? StudioPalette.Selection : StudioPalette.Card);
		eventArgs.Graphics.FillRectangle(brush, bounds);
	}

	protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs eventArgs)
	{
		using SolidBrush brush = new(StudioPalette.Accent);
		eventArgs.Graphics.FillEllipse(brush, new Rectangle(7, eventArgs.Item.Height / 2 - 3, 6, 6));
	}
}

internal sealed class StudioMenuColorTable : ProfessionalColorTable
{
	public override Color ToolStripDropDownBackground => StudioPalette.Card;
	public override Color MenuBorder => StudioPalette.Border;
	public override Color MenuItemBorder => StudioPalette.Accent;
	public override Color MenuItemSelected => StudioPalette.Selection;
}

internal sealed class StudioCheckBox : CheckBox
{
	private bool hovered;

	public StudioCheckBox()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
		AutoSize = false;
		Size = new Size(180, 30);
		Cursor = Cursors.Hand;
		ForeColor = StudioPalette.SecondaryText;
		Font = new Font("Segoe UI Semibold", 9F);
		UseVisualStyleBackColor = false;
	}

	protected override void OnMouseEnter(EventArgs eventArgs) { hovered = true; Invalidate(); base.OnMouseEnter(eventArgs); }
	protected override void OnMouseLeave(EventArgs eventArgs) { hovered = false; Invalidate(); base.OnMouseLeave(eventArgs); }
	protected override void OnCheckedChanged(EventArgs eventArgs) { base.OnCheckedChanged(eventArgs); Invalidate(); }

	protected override void OnPaint(PaintEventArgs eventArgs)
	{
		eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		eventArgs.Graphics.Clear(Parent?.BackColor ?? StudioPalette.Window);
		Rectangle box = new(1, (Height - 20) / 2, 20, 20);
		using GraphicsPath path = StudioGeometry.RoundedRectangle(box, 5);
		using SolidBrush fill = new(Checked ? StudioPalette.Accent : hovered ? StudioPalette.CardHover : StudioPalette.Input);
		using Pen border = new(Checked || Focused ? StudioPalette.Accent : StudioPalette.Border, Focused ? 1.5F : 1F);
		eventArgs.Graphics.FillPath(fill, path);
		eventArgs.Graphics.DrawPath(border, path);
		if (Checked)
		{
			using Pen check = new(Color.FromArgb(2, 24, 28), 2.2F) { StartCap = LineCap.Round, EndCap = LineCap.Round };
			eventArgs.Graphics.DrawLines(check, [new Point(6, box.Top + 10), new Point(10, box.Top + 14), new Point(17, box.Top + 6)]);
		}
		Rectangle textBounds = new(30, 0, Math.Max(1, Width - 30), Height);
		TextRenderer.DrawText(eventArgs.Graphics, Text, Font, textBounds, Enabled ? ForeColor : StudioPalette.MutedText,
			TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
	}
}

internal sealed class StudioBusyIndicator : Control
{
	private readonly System.Windows.Forms.Timer timer = new() { Interval = 70 };
	private int angle;

	public StudioBusyIndicator()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
		Size = new Size(22, 22);
		timer.Tick += (_, _) => { angle = (angle + 24) % 360; Invalidate(); };
		VisibleChanged += (_, _) => { if (Visible) timer.Start(); else timer.Stop(); };
	}

	protected override void OnPaint(PaintEventArgs eventArgs)
	{
		eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		Rectangle bounds = new(4, 4, Math.Max(2, Width - 9), Math.Max(2, Height - 9));
		using Pen track = new(StudioPalette.Border, 3F);
		using Pen accent = new(StudioPalette.Accent, 3F) { StartCap = LineCap.Round, EndCap = LineCap.Round };
		eventArgs.Graphics.DrawArc(track, bounds, 0, 360);
		eventArgs.Graphics.DrawArc(accent, bounds, angle, 105);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing) timer.Dispose();
		base.Dispose(disposing);
	}
}

internal sealed class StudioLoadingBar : Control
{
	private readonly System.Windows.Forms.Timer timer = new() { Interval = 24 };
	private int position;

	public StudioLoadingBar()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
			ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
		Height = 3;
		timer.Tick += (_, _) =>
		{
			position = (position + 10) % Math.Max(1, Width + Math.Max(80, Width / 5));
			Invalidate();
		};
		VisibleChanged += (_, _) =>
		{
			if (Visible) timer.Start();
			else timer.Stop();
		};
	}

	protected override void OnPaint(PaintEventArgs eventArgs)
	{
		eventArgs.Graphics.Clear(StudioPalette.Border);
		int segmentWidth = Math.Max(80, Width / 5);
		int x = position - segmentWidth;
		using SolidBrush accent = new(StudioPalette.Accent);
		eventArgs.Graphics.FillRectangle(accent, x, 0, segmentWidth, Height);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing) timer.Dispose();
		base.Dispose(disposing);
	}
}
