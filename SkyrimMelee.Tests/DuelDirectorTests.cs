using SkyrimMelee.Combat;

namespace SkyrimMelee.Tests;

public class DuelDirectorTests
{
	[Fact]
	public void BestOfThreeEndsAtTwoWins()
	{
		var duel = new DuelDirector();

		Assert.True(duel.ReportDeath(FighterId.Opponent));
		Assert.False(duel.Update(2.9f));
		Assert.False(duel.ReportDeath(FighterId.Player));
		Assert.True(duel.Update(0.2f));
		Assert.Equal(2, duel.Round);

		Assert.True(duel.ReportDeath(FighterId.Player));
		Assert.True(duel.Update(3.1f));
		Assert.Equal(3, duel.Round);

		Assert.True(duel.ReportDeath(FighterId.Opponent));
		Assert.False(duel.Update(3.1f));
		Assert.Equal(DuelPhase.MatchOver, duel.Phase);
		Assert.Equal(FighterId.Player, duel.MatchWinner);
		Assert.Equal(2, duel.PlayerScore);
		Assert.Equal(1, duel.OpponentScore);
		Assert.False(duel.ReportDeath(FighterId.Opponent));
	}

	[Fact]
	public void DrawReplaysTheRoundWithoutScore()
	{
		var duel = new DuelDirector();

		Assert.True(duel.ReportDraw());
		Assert.True(duel.Update(3.1f));

		Assert.Equal(1, duel.Round);
		Assert.Equal(0, duel.PlayerScore);
		Assert.Equal(0, duel.OpponentScore);
		Assert.Equal(DuelPhase.Fighting, duel.Phase);
	}

	[Fact]
	public void ResetClearsTheMatch()
	{
		var duel = new DuelDirector();
		duel.ReportDeath(FighterId.Opponent);
		duel.Update(3.1f);
		duel.ReportDeath(FighterId.Opponent);

		duel.Reset();

		Assert.Equal(1, duel.Round);
		Assert.Equal(0, duel.PlayerScore);
		Assert.Equal(0, duel.OpponentScore);
		Assert.Equal(DuelPhase.Fighting, duel.Phase);
		Assert.Null(duel.RoundWinner);
		Assert.Null(duel.MatchWinner);
	}

	[Fact]
	public void PostDeathHoldIsThreeSeconds()
	{
		var duel = new DuelDirector();
		duel.ReportDeath(FighterId.Opponent);

		Assert.False(duel.Update(2.99f));
		Assert.Equal(DuelPhase.PostDeath, duel.Phase);
		Assert.True(duel.Update(0.02f));
		Assert.Equal(DuelPhase.Fighting, duel.Phase);
	}
}
