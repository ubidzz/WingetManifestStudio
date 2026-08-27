namespace ManifestUpdater;

internal static class UiSelfTestRunner
{
	public static Task<int> RunStartupProbeAsync()
	{
		System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
		using MainForm form = new();
		form.ShowInTaskbar = false;
		form.Opacity = 0.01;
		form.StartPosition = FormStartPosition.Manual;
		form.Location = new Point(-32000, -32000);
		form.Shown += (_, _) =>
		{
			timer.Stop();
			File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "startup-probe.txt"), timer.ElapsedMilliseconds.ToString());
			form.Close();
		};
		Application.Run(form);
		return Task.FromResult(0);
	}

	public static Task<int> RunAsync()
	{
		List<string> report = [];
		int exitCode = 1;
		ThreadExceptionEventHandler threadExceptionHandler = (_, eventArgs) =>
		{
			report.Add("FAIL: An interface event crashed: " + eventArgs.Exception);
			exitCode = 1;
		};
		Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
		Application.ThreadException += threadExceptionHandler;
		using MainForm form = new(uiTestMode: true);
		form.Shown += async (_, _) =>
		{
			try
			{
				report.AddRange(await form.RunUiVerificationAsync());
				form.RenderTabForVerification("Installers & Hashes", Path.Combine(AppContext.BaseDirectory, "ui-installers.png"));
				form.RenderTabForVerification("Preview & Submit", Path.Combine(AppContext.BaseDirectory, "ui-review.png"));
				form.RenderTabForVerification("Test Center", Path.Combine(AppContext.BaseDirectory, "ui-test-center.png"));
				form.SetLanguageForVerification("es-ES");
				form.RenderTabForVerification("Installers & Hashes", Path.Combine(AppContext.BaseDirectory, "ui-installers-es.png"));
				form.RenderTabForVerification("Preview & Submit", Path.Combine(AppContext.BaseDirectory, "ui-review-es.png"));
				exitCode = report.Any(line => line.StartsWith("FAIL", StringComparison.Ordinal)) ? 1 : 0;
			}
			catch (Exception ex)
			{
				report.Add("FAIL: UI verification runner crashed: " + ex);
				exitCode = 1;
			}
			finally
			{
				string reportPath = Path.Combine(AppContext.BaseDirectory, "ui-self-test-report.txt");
				File.WriteAllLines(reportPath, report);
				Application.ThreadException -= threadExceptionHandler;
				form.Close();
			}
		};
		Application.Run(form);
		return Task.FromResult(exitCode);
	}
}
