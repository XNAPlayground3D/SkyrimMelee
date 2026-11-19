
using SkyrimMelee.Tuning;
using SkyrimMelee.Attacks;
using SkyrimMelee.Physics;

namespace SkyrimMelee.Combat;

/// <summary>
/// Skyrim's stamina gate: a stamina action may start while any stamina remains,
/// even if it costs more than the fighter has. At zero the action fails — a
/// power attack degrades to a normal swing instead.
/// </summary>
public static class StaminaRules
{
	public static bool Allows(FighterStats stats, AttackDefinition attack) =>
		!attack.RequiresStamina || stats.Stamina > 0f;

	/// <summary>Returns the attack that will actually play for the current stamina.</summary>
	public static AttackDefinition Resolve(
		AttackDefinition attack, FighterStats stats, AttackDefinition fallback) =>
		Allows(stats, attack) ? attack : fallback;
}
