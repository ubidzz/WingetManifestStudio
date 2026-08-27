namespace ManifestUpdater;

internal sealed class StudioTextPromptDialog : Form
{
	private readonly StudioTextBox valueBox;

	private StudioTextPromptDialog(string title, string description, string fieldLabel, string placeholder, string initialValue)
	{
		Text = title;
		FormBorderStyle = FormBorderStyle.FixedDialog;
		StartPosition = FormStartPosition.CenterParent;
		ShowInTaskbar = false;
		MaximizeBox = false;
		MinimizeBox = false;
		ClientSize = new Size(600, 270);
		BackColor = StudioPalette.Window;
		ForeColor = Color.White;
		Font = new Font("Segoe UI", 9.5F);
		AutoScaleMode = AutoScaleMode.Dpi;

		TableLayoutPanel layout = new()
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 5,
			Padding = new Padding(26),
			BackColor = StudioPalette.Window
		};
		layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
		layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
		layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
		layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

		layout.Controls.Add(new Label
		{
			Text = description,
			Dock = DockStyle.Fill,
			ForeColor = StudioPalette.SecondaryText,
			Font = new Font("Segoe UI", 10F),
			AutoSize = false
		}, 0, 0);
		layout.Controls.Add(new Label
		{
			Text = fieldLabel,
			Dock = DockStyle.Fill,
			ForeColor = Color.White,
			Font = new Font("Segoe UI Semibold", 9F),
			TextAlign = ContentAlignment.BottomLeft
		}, 0, 1);
		valueBox = new StudioTextBox
		{
			Dock = DockStyle.Fill,
			Text = initialValue ?? string.Empty,
			PlaceholderText = placeholder,
			AccessibleName = fieldLabel,
			AccessibleDescription = description
		};
		layout.Controls.Add(valueBox, 0, 2);

		FlowLayoutPanel actions = new()
		{
			Dock = DockStyle.Fill,
			FlowDirection = FlowDirection.RightToLeft,
			WrapContents = false,
			BackColor = StudioPalette.Window,
			Padding = Padding.Empty
		};
		StudioButton continueButton = new()
		{
			Text = "Continue",
			DialogResult = DialogResult.OK,
			Width = 150,
			Height = 42,
			ButtonKind = StudioButtonKind.Primary,
			AccessibleName = "Continue"
		};
		StudioButton cancelButton = new()
		{
			Text = "Cancel",
			DialogResult = DialogResult.Cancel,
			Width = 120,
			Height = 42,
			Margin = new Padding(8, 0, 0, 0),
			AccessibleName = "Cancel"
		};
		actions.Controls.Add(continueButton);
		actions.Controls.Add(cancelButton);
		layout.Controls.Add(actions, 0, 4);
		Controls.Add(layout);
		AcceptButton = continueButton;
		CancelButton = cancelButton;
		Shown += (_, _) => { valueBox.Focus(); valueBox.SelectAll(); };
	}

	public static string? ShowPrompt(
		IWin32Window owner,
		string title,
		string description,
		string fieldLabel,
		string placeholder,
		string initialValue = "")
	{
		using StudioTextPromptDialog dialog = new(title, description, fieldLabel, placeholder, initialValue);
		return dialog.ShowDialog(owner) == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.valueBox.Text)
			? dialog.valueBox.Text.Trim()
			: null;
	}
}
