using SkyrimMelee.Combat;

namespace SkyrimMelee.Tests;

public class StaggerRulesTests
{
	private static AttackDefinition Attack(string name) =>
		AttackCatalog.Create(SwordClips.Create()).Single(attack => attack.Name == name);

	[Fact]
	public void LightHitsChipPoiseUntilItEmpties()
	{
		var slash = Attack("SlashLeft");
		var stats = new FighterStats();
		var result = default(StaggerResult);
		var hits = 0;

		while (!result.Staggered && hits < 20)
		{
			result = StaggerRules.Resolve(slash, stats);
			hits++;
		}

		Assert.True(result.Staggered);
		Assert.Equal(10, hits);
		Assert.Equal(CombatTuning.StaggerSmallDuration, result.Duration, 4);
		Assert.Equal(CombatTuning.MaxPoise, result.PoiseAfter, 4);
	}

	[Fact]
	public void PowerAttackAlwaysStaggers()
	{
		var stats = new FighterStats();
		var result = StaggerRules.Resolve(Attack("PowerSlash"), stats);

		Assert.True(result.Staggered);
		Assert.Equal(CombatTuning.StaggerLargeDuration, result.Duration, 4);
		Assert.Equal(CombatTuning.MaxPoise, result.PoiseAfter, 4);
	}

	[Fact]
	public void BashAlwaysStaggersForItsShortWindow()
	{
		var result = StaggerRules.Resolve(Attack("Bash"), new FighterStats());

		Assert.True(result.Staggered);
		Assert.Equal(CombatTuning.StaggerSmallDuration, result.Duration, 4);
		Assert.Equal(CombatTuning.MaxPoise, result.PoiseAfter, 4);
	}

	[Fact]
	public void NonBreakingLightHitDoesNotStagger()
	{
		var result = StaggerRules.Resolve(Attack("SlashLeft"), new FighterStats());

		Assert.False(result.Staggered);
		Assert.Equal(0f, result.Duration, 4);
		Assert.Equal(90f, result.PoiseAfter, 4);
	}

	[Fact]
	public void StaggerLocksTheFsmUntilItExpires()
	{
		var slash = Attack("SlashLeft");
		var fsm = new CombatStateMachine();
		fsm.ApplyStagger(CombatTuning.StaggerSmallDuration);

		Assert.Equal(CombatState.Stagger, fsm.State);
		Assert.False(fsm.TryStartAttack(slash));
		Assert.False(fsm.TryStartGuard());

		fsm.Update(CombatTuning.StaggerSmallDuration);

		Assert.Equal(CombatState.Idle, fsm.State);
		Assert.True(fsm.TryStartAttack(slash));
	}

	[Fact]
	public void StaggerCancelsWindup()
	{
		var slash = Attack("SlashLeft");
		var fsm = new CombatStateMachine();
		fsm.TryStartAttack(slash);
		fsm.Update(0.1f);

		fsm.ApplyStagger(CombatTuning.StaggerSmallDuration);

		Assert.Equal(CombatState.Stagger, fsm.State);
		Assert.Null(fsm.Attack);
	}

	[Fact]
	public void StaggerCancelsGuard()
	{
		var fsm = new CombatStateMachine();
		fsm.TryStartGuard();
		fsm.Update(CombatTuning.GuardRaiseDuration);
		Assert.Equal(CombatState.GuardHold, fsm.State);

		fsm.ApplyStagger(CombatTuning.StaggerSmallDuration);

		Assert.Equal(CombatState.Stagger, fsm.State);
	}

	[Fact]
	public void StaggerCannotBeRestaggered()
	{
		var fsm = new CombatStateMachine();
		fsm.ApplyStagger(CombatTuning.StaggerSmallDuration);
		fsm.ApplyStagger(CombatTuning.StaggerLargeDuration);

		Assert.Equal(CombatState.Stagger, fsm.State);
		Assert.Equal(CombatTuning.StaggerSmallDuration, fsm.StateDuration, 4);
	}
}
