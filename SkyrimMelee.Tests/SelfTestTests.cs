using SkyrimMelee.Combat;

namespace SkyrimMelee.Tests;

public class SelfTestTests
{
	[Fact]
	public void AllScenariosPass()
	{
		var results = SelfTestRunner.Run();

		Assert.Equal(14, results.Count);
		Assert.All(results, result =>
			Assert.True(result.Passed, $"{result.Name}: {result.Detail}"));
	}

	[Fact]
	public void ResultsAreIdenticalAcrossTenRuns()
	{
		var first = SelfTestRunner.Run().ToArray();

		for (var run = 0; run < 9; run++)
		{
			var next = SelfTestRunner.Run().ToArray();
			Assert.Equal(first, next);
		}
	}
}
