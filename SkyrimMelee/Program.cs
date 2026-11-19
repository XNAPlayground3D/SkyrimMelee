using SkyrimMelee.Core;

namespace SkyrimMelee;

internal static class Program
{
	[STAThread]
	private static int Main(string[] args)
	{
		if (HasFlag(args, "--self-test"))
		{
			Console.WriteLine("self-test: no tests yet (combat harness arrives in T3.5)");
			return 0;
		}

		if (HasFlag(args, "--help") || HasFlag(args, "-h"))
		{
			Console.WriteLine(GameOptions.Usage);
			return 0;
		}

		if (!GameOptions.TryParse(args, out var options, out var error))
		{
			Console.Error.WriteLine(error);
			Console.WriteLine(GameOptions.Usage);
			return 2;
		}

		using var game = new SkyrimMeleeGame(options);
		game.Run();
		return 0;
	}

	private static bool HasFlag(string[] args, string flag) =>
		args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));
}
