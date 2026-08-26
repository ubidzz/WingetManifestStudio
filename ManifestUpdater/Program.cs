namespace ManifestUpdater;

internal static class Program
{
	[STAThread]
	private static async Task<int> Main(string[] args)
	{
		if (args.Any(argument => string.Equals(argument, "--self-test", StringComparison.OrdinalIgnoreCase)))
			return await SelfTestRunner.RunAsync(args);

		ApplicationConfiguration.Initialize();
		if (args.Any(argument => string.Equals(argument, "--ui-self-test", StringComparison.OrdinalIgnoreCase)))
			return await UiSelfTestRunner.RunAsync();
		Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
		Application.ThreadException += (_, eventArgs) => ShowRecoveredError(eventArgs.Exception, "Windows interface event");
		AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
		{
			if (eventArgs.ExceptionObject is Exception exception)
				CrashReporter.Report(exception, "Unhandled application error");
		};
		TaskScheduler.UnobservedTaskException += (_, eventArgs) =>
		{
			CrashReporter.Report(eventArgs.Exception, "Unobserved background task error");
			eventArgs.SetObserved();
		};
		Application.Run(new MainForm());
		return 0;
	}

	private static void ShowRecoveredError(Exception exception, string context)
	{
		string logPath = CrashReporter.Report(exception, context);
		string details = string.IsNullOrWhiteSpace(logPath)
			? exception.Message
			: $"{exception.Message}{Environment.NewLine}{Environment.NewLine}Technical details were saved to:{Environment.NewLine}{logPath}";
		MessageBox.Show(details, "Winget Manifest Studio recovered from a problem", MessageBoxButtons.OK, MessageBoxIcon.Error);
	}
}
