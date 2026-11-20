namespace SkyrimMelee.Validation;

/// <summary>CLI entry point for <c>--validate-models</c>.</summary>
public static class ModelValidationRunner
{
	public const string DefaultDirectory = "Content/Models";

	public static int Run(string modelsDirectory)
	{
		var report = new ValidationReport();
		var directory = Path.GetFullPath(modelsDirectory);

		foreach (var file in new[] { ModelSpec.FighterFile, ModelSpec.SwordFile })
		{
			var path = Path.Combine(directory, file);
			if (!File.Exists(path))
			{
				report.Error(file, "file", $"missing {path}");
			}
		}

		if (report.IsValid)
		{
			try
			{
				report.Merge(ModelValidationGame.Run(directory));
			}
			catch (Exception exception)
			{
				report.Error(
					directory,
					"graphics",
					$"could not create the validation window: {exception.GetBaseException().Message}");
			}
		}

		Print(report);
		return report.IsValid ? 0 : 1;
	}

	private static void Print(ValidationReport report)
	{
		foreach (var issue in report.Issues)
		{
			var label = issue.Severity == ValidationSeverity.Error ? "FAIL" : "WARN";
			Console.WriteLine($"{label}  {issue.File}: {issue.Location}: {issue.Message}");
		}

		if (report.IsValid)
		{
			Console.WriteLine(
				$"Model validation passed ({report.WarningCount} warning{(report.WarningCount == 1 ? string.Empty : "s")}).");
		}
		else
		{
			Console.WriteLine(
				$"Model validation failed: {report.ErrorCount} error{(report.ErrorCount == 1 ? string.Empty : "s")}, {report.WarningCount} warning{(report.WarningCount == 1 ? string.Empty : "s")}.");
		}
	}
}
