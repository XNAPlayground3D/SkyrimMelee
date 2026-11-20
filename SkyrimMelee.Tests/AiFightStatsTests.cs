using SkyrimMelee.Ai;

namespace SkyrimMelee.Tests;

public class AiFightStatsTests
{
	[Fact]
	public void EmptyFightHasZeroRates()
	{
		var stats = new AiFightStats();

		Assert.Equal(0f, stats.HitRate);
		Assert.Equal(0f, stats.BlockRate);
		Assert.Equal(0, stats.Incoming);
	}

	[Fact]
	public void HitRateIsHitsOverAttacksThrown()
	{
		var stats = new AiFightStats();
		stats.RecordAttack();
		stats.RecordAttack();
		stats.RecordAttack();
		stats.RecordAttack();
		stats.RecordHit();

		Assert.Equal(0.25f, stats.HitRate, 4);
	}

	[Fact]
	public void BlockRateCountsBlocksBreaksAndCleanIncomingHits()
	{
		var stats = new AiFightStats();
		stats.RecordBlock();
		stats.RecordBlock();
		stats.RecordGuardBreak();
		stats.RecordIncomingHit();

		Assert.Equal(4, stats.Incoming);
		Assert.Equal(0.5f, stats.BlockRate, 4);
	}

	[Fact]
	public void ResetClearsEveryCounter()
	{
		var stats = new AiFightStats();
		stats.RecordAttack();
		stats.RecordHit();
		stats.RecordBlock();
		stats.RecordGuardBreak();
		stats.RecordIncomingHit();

		stats.Reset();

		Assert.Equal(0, stats.AttacksThrown);
		Assert.Equal(0, stats.HitsLanded);
		Assert.Equal(0, stats.Blocks);
		Assert.Equal(0, stats.GuardBreaks);
		Assert.Equal(0, stats.IncomingHits);
	}
}
