namespace SkyrimMelee.Combat;

public readonly record struct StaggerResult(bool Staggered, float Duration, float PoiseAfter);

/// <summary>
/// Maps an attack's poise damage to a stagger decision. Light hits stagger only
/// when poise empties; power attacks always stagger. Entering stagger restores poise.
/// </summary>
public static class StaggerRules
{
	public static StaggerResult Resolve(AttackDefinition attack, FighterStats target)
	{
		target.TakePoiseDamage(attack.StaggerPower);

		if (attack.AlwaysStagger)
		{
			target.RestorePoise();
			return new StaggerResult(true, CombatTuning.StaggerLargeDuration, target.Poise);
		}

		if (target.Poise <= 0f)
		{
			target.RestorePoise();
			return new StaggerResult(true, CombatTuning.StaggerSmallDuration, target.Poise);
		}

		return new StaggerResult(false, 0f, target.Poise);
	}
}
