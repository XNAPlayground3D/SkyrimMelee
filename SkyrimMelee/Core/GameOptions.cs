using System.Globalization;

namespace SkyrimMelee.Core;

/// <summary>Command-line switches for a launch.</summary>
public sealed record GameOptions
{
	public bool Debug { get; init; }
	public bool GodMode { get; init; }
	public bool SlowMotion { get; init; }
	public bool NoUi { get; init; }
	public bool ScreenShake { get; init; } = true;

	/// <summary>Freeze the first sword clip at this time (seconds) for inspection.</summary>
	public float? ClipTime { get; init; }

	public const string Usage =
		"SkyrimMelee [--debug] [--god] [--slowmo] [--no-ui] [--no-shake] [--clip-time=<s>]\n" +
		"           [--self-test] [--help]";

	public static bool TryParse(string[] args, out GameOptions options, out string? error)
	{
		var result = new GameOptions();
		error = null;

		foreach (var arg in args)
		{
			if (arg.StartsWith("--clip-time=", StringComparison.OrdinalIgnoreCase))
			{
				var text = arg["--clip-time=".Length..];
				if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var time) ||
					time < 0f)
				{
					error = $"Invalid clip time: {text}";
					options = result;
					return false;
				}

				result = result with { ClipTime = time };
				continue;
			}

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
				case "--no-shake":
					result = result with { ScreenShake = false };
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
