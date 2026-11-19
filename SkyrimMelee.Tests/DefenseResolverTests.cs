using SkyrimMelee.Combat;

namespace SkyrimMelee.Tests;

public class DefenseResolverTests
{
	private static AttackDefinition Attack(string name) =>
		AttackCatalog.Create(SwordClips.Create()).Single(attack => attack.Name == name);

	private static FighterStats Attacker() => new();

	private static FighterStats Defender(float stamina = CombatTuning.MaxStamina) =>
		new(stamina) { Armor = 300f, BlockSkill = 50f };

	[Fact]
	public void CleanHitDoesNotSpendStamina()
	{
		var defender = Defender();
		var result = DefenseResolver.Resolve(
			Attack("SlashLeft"), Attacker(), defender, HitOutcome.Clean);

		Assert.Equal(HitOutcome.Clean, result.Outcome);
		Assert.Equal(DamageKind.Clean, result.Damage.Kind);
		Assert.Equal(16f, result.Damage.Final, 3);
		Assert.Equal(0f, result.StaminaSpent, 3);
		Assert.Equal(CombatTuning.MaxStamina, defender.Stamina, 3);
		Assert.False(result.GuardBroken);
	}

	[Fact]
	public void BlockSpendsBlockStamina()
	{
		var defender = Defender();
		var result = DefenseResolver.Resolve(
			Attack("SlashLeft"), Attacker(), defender, HitOutcome.Block);

		Assert.Equal(DamageKind.Block, result.Damage.Kind);
		Assert.Equal(8f, result.Damage.Final, 3);
		Assert.Equal(9.5232f, result.StaminaSpent, 3);
		Assert.Equal(90.4768f, defender.Stamina, 3);
		Assert.Equal(0f, result.AttackerStagger, 3);
		Assert.False(result.GuardBroken);
	}

	[Fact]
	public void TimedBlockSpendsHalfAndMicroStaggers()
	{
		var defender = Defender();
		var result = DefenseResolver.Resolve(
			Attack("SlashLeft"), Attacker(), defender, HitOutcome.TimedBlock);

		Assert.Equal(DamageKind.TimedBlock, result.Damage.Kind);
		Assert.Equal(5f, result.Damage.Final, 3);
		Assert.Equal(4.7616f, result.StaminaSpent, 3);
		Assert.Equal(95.2384f, defender.Stamina, 3);
		Assert.Equal(CombatTuning.StaggerMicroDuration, result.AttackerStagger, 3);
		Assert.False(result.GuardBroken);
	}

	[Fact]
	public void BlockBelowCostBreaksTheGuard()
	{
		var defender = Defender(stamina: 9f);
		var result = DefenseResolver.Resolve(
			Attack("SlashLeft"), Attacker(), defender, HitOutcome.Block);

		Assert.True(result.GuardBroken);
		Assert.Equal(DamageKind.GuardBreak, result.Damage.Kind);
		Assert.Equal(16f, result.Damage.Final, 3);
		Assert.Equal(0f, defender.Stamina, 3);
		Assert.Equal(CombatTuning.StaggerLargeDuration, result.DefenderStagger, 3);
	}

	[Fact]
	public void TimedBlockBelowCostAlsoBreaks()
	{
		var defender = Defender(stamina: 4f);
		var result = DefenseResolver.Resolve(
			Attack("SlashLeft"), Attacker(), defender, HitOutcome.TimedBlock);

		Assert.True(result.GuardBroken);
		Assert.Equal(0f, defender.Stamina, 3);
	}

	[Fact]
	public void GuardBreakLocksTheGuardUntilItExpires()
	{
		var defender = Defender(stamina: 5f);
		var result = DefenseResolver.Resolve(
			Attack("SlashLeft"), Attacker(), defender, HitOutcome.Block);

		var fsm = new CombatStateMachine();
		fsm.ApplyStagger(result.DefenderStagger);
		Assert.Equal(CombatState.Stagger, fsm.State);
		Assert.False(fsm.TryResumeGuard());
		Assert.False(fsm.TryStartGuard());

		fsm.Update(result.DefenderStagger);
		Assert.Equal(CombatState.Idle, fsm.State);
		Assert.True(fsm.TryResumeGuard());
	}

	[Fact]
	public void TimedBlockMicroStaggerLocksTheAttacker()
	{
		var defender = Defender();
		var result = DefenseResolver.Resolve(
			Attack("SlashLeft"), Attacker(), defender, HitOutcome.TimedBlock);

		var fsm = new CombatStateMachine();
		fsm.ApplyStagger(result.AttackerStagger);
		Assert.Equal(CombatState.Stagger, fsm.State);
		Assert.False(fsm.TryStartAttack(Attack("SlashLeft")));

		fsm.Update(result.AttackerStagger);
		Assert.Equal(CombatState.Idle, fsm.State);
	}
}
