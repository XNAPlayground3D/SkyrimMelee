using SkyrimMelee.Ai;
using SkyrimMelee.Combat;

namespace SkyrimMelee.Tests;

public class DuelistAiTests
{
	private static AiProfile Profile(
		float reaction = 0.2f,
		float block = 1f,
		float turtle = 0f,
		float aggression = 1f,
		float power = 0f,
		float mistake = 0f,
		float timedBlock = 0f) =>
		new("Test", reaction, block, timedBlock, turtle, aggression, power, mistake);

	private static AiContext Context(
		CombatState player = CombatState.Idle,
		CombatState self = CombatState.Idle,
		float distance = 2f,
		float playerStamina = 100f,
		float selfStamina = 100f) =>
		new(player, playerStamina, self, selfStamina, distance);

	[Fact]
	public void GuardsOnlyAfterTheReactionDelay()
	{
		var ai = new DuelistAi(Profile(reaction: 0.5f, block: 1f), seed: 7);
		var windup = Context(player: CombatState.Windup);

		ai.Update(0.1f, windup);
		Assert.False(ai.GuardHeld);
		Assert.True(ai.ReactionRemaining > 0f);

		ai.Update(0.25f, windup);
		Assert.True(ai.GuardHeld);
		Assert.Equal("wind-up read", ai.LastDecision?.Reason);
		Assert.Equal("guard", ai.LastDecision?.Action);
	}

	[Fact]
	public void FailedBlockReadIsVisible()
	{
		var ai = new DuelistAi(Profile(reaction: 0.2f, block: 0f), seed: 7);
		var windup = Context(player: CombatState.Windup);

		ai.Update(0.016f, windup);
		ai.Update(0.3f, windup);

		Assert.False(ai.GuardHeld);
		Assert.Equal("hold", ai.LastDecision?.Action);
		Assert.Equal("block roll failed", ai.LastDecision?.Reason);
	}

	[Fact]
	public void PunishesRecovery()
	{
		var ai = new DuelistAi(Profile(aggression: 1f), seed: 7);

		ai.Update(0.016f, Context(player: CombatState.Recovery));

		Assert.NotNull(ai.Intent);
		Assert.Equal(ai.Intent.ToString(), ai.LastDecision?.Action);
		Assert.Equal("punish recovery", ai.LastDecision?.Reason);
	}

	[Fact]
	public void ResetClearsTelemetry()
	{
		var ai = new DuelistAi(Profile(aggression: 1f), seed: 7);
		ai.Update(0.016f, Context(player: CombatState.Recovery));
		Assert.True(ai.DecisionId > 0);

		ai.Reset();

		Assert.Equal(0, ai.DecisionId);
		Assert.Null(ai.LastDecision);
		Assert.Equal(-1f, ai.ReactionRemaining);
	}

	[Fact]
	public void DoesNotAttackWhileCommitted()
	{
		var ai = new DuelistAi(Profile(aggression: 1f), seed: 7);

		ai.Update(0.016f, Context(player: CombatState.Recovery, self: CombatState.Windup));

		Assert.Null(ai.Intent);
	}

	[Fact]
	public void ApproachesWhenOutOfReach()
	{
		var ai = new DuelistAi(Profile(), seed: 7);

		ai.Update(0.016f, Context(distance: 3f));

		Assert.Equal(1f, ai.Approach);
	}

	[Fact]
	public void GivesGroundWhenTooClose()
	{
		var ai = new DuelistAi(Profile(), seed: 7);

		ai.Update(0.016f, Context(distance: 1.2f));

		Assert.Equal(-1f, ai.Approach);
	}

	[Fact]
	public void BracesDuringACommittedWindup()
	{
		var ai = new DuelistAi(Profile(), seed: 7);

		ai.Update(0.016f, Context(player: CombatState.Windup, distance: 3f));

		Assert.Equal(0f, ai.Approach);
	}

	[Fact]
	public void SameSeedProducesTheSameFight()
	{
		var first = new DuelistAi(AiProfile.Adept, seed: 42);
		var second = new DuelistAi(AiProfile.Adept, seed: 42);

		for (var i = 0; i < 600; i++)
		{
			var state = ((i / 40) % 3) switch
			{
				0 => CombatState.Idle,
				1 => CombatState.Windup,
				_ => CombatState.Recovery
			};
			var ctx = Context(player: state, distance: 1.5f + i % 3);

			first.Update(1f / 60f, ctx);
			second.Update(1f / 60f, ctx);

			Assert.Equal(first.Intent, second.Intent);
			Assert.Equal(first.GuardHeld, second.GuardHeld);
			Assert.Equal(first.Approach, second.Approach);
		}
	}
}
