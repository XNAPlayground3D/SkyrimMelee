using SkyrimMelee.Combat;
using SkyrimMelee.Core;

namespace SkyrimMelee;

internal static class Program
{
	[STAThread]
	private static int Main(string[] args)
	{
		if (HasFlag(args, "--self-test"))
		{
			return RunSelfTest();
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

	private static int RunSelfTest()
	{
		var results = SelfTestRunner.Run();
		var failed = 0;

		foreach (var result in results)
		{
			Console.WriteLine($"{(result.Passed ? "PASS" : "FAIL")}  {result.Name}  ({result.Detail})");
			if (!result.Passed)
			{
				failed++;
			}
		}

		Console.WriteLine($"{results.Count - failed}/{results.Count} scenarios passed");
		return failed == 0 ? 0 : 1;
	}

	private static bool HasFlag(string[] args, string flag) =>
		args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));
}
