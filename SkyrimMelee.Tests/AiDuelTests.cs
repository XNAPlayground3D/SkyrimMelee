using SkyrimMelee.Ai;
using SkyrimMelee.SelfTest;

namespace SkyrimMelee.Tests;

public class AiDuelTests
{
	[Fact]
	public void MasterBeatsNoviceAtLeastEightOfTen()
	{
		var wins = 0;

		for (var seed = 1; seed <= 10; seed++)
		{
			var result = AiDuelSimulator.Run(AiProfile.Master, AiProfile.Novice, seed);
			if (result.Winner == "first")
			{
				wins++;
			}
		}

		Assert.True(wins >= 8, $"Master won {wins}/10 duels.");
	}


	[Fact]
	public void NoviceStillLandsHits()
	{
		var landed = Enumerable.Range(1, 10).Any(seed =>
			AiDuelSimulator.Run(AiProfile.Novice, AiProfile.Master, seed).FirstHits > 0);

		Assert.True(landed);
	}

	[Fact]
	public void DuelIsDeterministic()
	{
		var first = AiDuelSimulator.Run(AiProfile.Adept, AiProfile.Adept, seed: 7);
		var second = AiDuelSimulator.Run(AiProfile.Adept, AiProfile.Adept, seed: 7);

		Assert.Equal(first, second);
	}

	[Fact]
	public void AdeptFightsAndDealsDamage()
	{
		var result = AiDuelSimulator.Run(AiProfile.Adept, AiProfile.Adept, seed: 3);

		Assert.True(result.FirstDamage > 0f || result.SecondDamage > 0f);
	}
}
