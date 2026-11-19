namespace SkyrimMelee.Combat;

public readonly record struct DefenseResult(
	HitOutcome Outcome,
	DamageBreakdown Damage,
	float StaminaSpent,
	float DefenderStagger,
	float AttackerStagger,
	bool GuardBroken);

/// <summary>
/// Resolves an incoming attack against a defender's guard and stamina. A block
/// or timed block spends its stamina cost; if that cost exceeds the remaining
/// stamina the guard breaks instead. A timed block micro-staggers the attacker.
/// </summary>
public static class DefenseResolver
{
	public static DefenseResult Resolve(
		AttackDefinition attack,
		FighterStats attacker,
		FighterStats defender,
		HitOutcome outcome)
	{
		var kind = outcome switch
		{
			HitOutcome.TimedBlock => DamageKind.TimedBlock,
			HitOutcome.Block => DamageKind.Block,
			_ => DamageKind.Clean
		};

		var damage = DamageModel.Compute(
			attack, attacker.TwoHandedSkill, defender.Armor, defender.BlockSkill, kind);

		if (outcome == HitOutcome.Clean)
		{
			return new DefenseResult(outcome, damage, 0f, 0f, 0f, false);
		}

		if (damage.StaminaCost > defender.Stamina + 1e-4f)
		{
			var broken = DamageModel.Compute(
				attack, attacker.TwoHandedSkill, defender.Armor,
				defender.BlockSkill, DamageKind.GuardBreak);

			defender.SetStamina(0f);
			return new DefenseResult(
				outcome, broken, 0f, CombatTuning.StaggerLargeDuration, 0f, true);
		}

		defender.SetStamina(defender.Stamina - damage.StaminaCost);
		var attackerStagger = outcome == HitOutcome.TimedBlock
			? CombatTuning.StaggerMicroDuration
			: 0f;

		return new DefenseResult(
			outcome, damage, damage.StaminaCost, 0f, attackerStagger, false);
	}
}
