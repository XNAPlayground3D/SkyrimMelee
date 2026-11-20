namespace SkyrimMelee.Validation;

public enum ValidationSeverity
{
	Warning,
	Error
}

/// <summary>One actionable validation finding.</summary>
public readonly record struct ValidationIssue(
	ValidationSeverity Severity,
	string File,
	string Location,
	string Message)
{
	public override string ToString() => $"{File}: {Location}: {Message}";
}

/// <summary>Collects validation findings for one or more models.</summary>
public sealed class ValidationReport
{
	private readonly List<ValidationIssue> _issues = [];

	public IReadOnlyList<ValidationIssue> Issues => _issues;

	public int ErrorCount => _issues.Count(issue => issue.Severity == ValidationSeverity.Error);

	public int WarningCount => _issues.Count(issue => issue.Severity == ValidationSeverity.Warning);

	public bool IsValid => ErrorCount == 0;

	public void Error(string file, string location, string message) =>
		_issues.Add(new ValidationIssue(ValidationSeverity.Error, file, location, message));

	public void Warning(string file, string location, string message) =>
		_issues.Add(new ValidationIssue(ValidationSeverity.Warning, file, location, message));

	public void Merge(ValidationReport other) => _issues.AddRange(other._issues);
}
