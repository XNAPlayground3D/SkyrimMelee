namespace SkyrimMelee.Core;

/// <summary>Command-line switches for a launch.</summary>
public sealed record GameOptions
{
	public bool Debug { get; init; }
	public bool GodMode { get; init; }
	public bool SlowMotion { get; init; }
	public bool NoUi { get; init; }

	public const string Usage =
		"SkyrimMelee [--debug] [--god] [--slowmo] [--no-ui]\n" +
		"           [--self-test] [--help]";

	public static bool TryParse(string[] args, out GameOptions options, out string? error)
	{
		var result = new GameOptions();
		error = null;

		foreach (var arg in args)
		{
			switch (arg.ToLowerInvariant())
			{
				case "--debug":
					result = result with { Debug = true };
					break;
				case "--god":
					result = result with { GodMode = true };
					break;
				case "--slowmo":
					result = result with { SlowMotion = true };
					break;
				case "--no-ui":
					result = result with { NoUi = true };
					break;
				default:
					error = $"Unknown argument: {arg}";
					options = result;
					return false;
			}
		}

		options = result;
		return true;
	}
}
