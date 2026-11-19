namespace SkyrimMelee.Combat;

public enum DamageKind
{
	Clean,
	Block,
	TimedBlock,
	GuardBreak
}

/// <summary>Damage numbers before and after each mitigation step.</summary>
public readonly record struct DamageBreakdown(
	DamageKind Kind,
	float Raw,
	float Armored,
	float Final,
	float ReductionPercent,
	float StaminaCost);

/// <summary>
/// The single implementation of the documented damage formula. Pure math:
/// callers pass the stats and the mitigation kind, and get the breakdown.
/// </summary>
public static class DamageModel
{
	public static DamageBreakdown Compute(
		AttackDefinition attack,
		float twoHandedSkill,
		float armor,
		float blockSkill,
		DamageKind kind) =>
		Compute(
			CombatTuning.WeaponBaseDamage,
			attack.DamageMultiplier,
			twoHandedSkill,
			armor,
			blockSkill,
			kind);

	public static DamageBreakdown Compute(
		float weaponBaseDamage,
		float damageMultiplier,
		float twoHandedSkill,
		float armor,
		float blockSkill,
		DamageKind kind)
	{
		var skill = Math.Clamp(twoHandedSkill, 0f, 100f);
		var raw = weaponBaseDamage * damageMultiplier *
				  (1f + CombatTuning.TwoHandedDamagePerSkill * skill / 100f);

		var armorReduction = MathF.Min(
			CombatTuning.ArmorReductionCap,
			MathF.Max(0f, armor) * CombatTuning.ArmorReductionPerPoint);
		var armored = raw * (1f - armorReduction);

		var blockReduction = CombatTuning.BlockReductionBase +
							 CombatTuning.BlockReductionPerSkill *
							 Math.Clamp(blockSkill, 0f, 100f) / 100f;

		float final;
		float staminaCost;

		switch (kind)
		{
			case DamageKind.Block:
				final = armored * (1f - blockReduction);
				staminaCost = armored * CombatTuning.BlockStaminaMultiplier;
				break;
			case DamageKind.TimedBlock:
				final = armored * (1f - MathF.Min(
					CombatTuning.TimedBlockCap,
					blockReduction + CombatTuning.TimedBlockBonus));
				staminaCost = armored * CombatTuning.TimedBlockStaminaMultiplier;
				break;
			case DamageKind.GuardBreak:
				final = armored;
				staminaCost = armored * CombatTuning.BlockStaminaMultiplier;
				break;
			default:
				final = armored;
				staminaCost = 0f;
				break;
		}

		final = RoundDamage(final);
		var reduction = raw > 0f ? (1f - final / raw) * 100f : 0f;

		return new DamageBreakdown(kind, raw, armored, final, reduction, staminaCost);
	}

	/// <summary>Rounds away from zero and never goes below one on a connecting hit.</summary>
	public static float RoundDamage(float value) =>
		MathF.Max(CombatTuning.MinimumDamage,
			MathF.Round(value, MidpointRounding.AwayFromZero));
}
