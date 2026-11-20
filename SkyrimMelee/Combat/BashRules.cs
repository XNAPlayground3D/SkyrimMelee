using Microsoft.Xna.Framework;
using SkyrimMelee.Tuning;

namespace SkyrimMelee.Combat;

/// <summary>
/// Bash range: Skyrim's shove uses the weapon's melee reach, so it only
/// connects when the target is inside attack range and in front.
/// </summary>
public static class BashRules
{
	public static bool InRange(
		Vector3 attackerPosition, Vector3 attackerForward, Vector3 targetPosition)
	{
		var offset = targetPosition - attackerPosition;
		offset.Y = 0f;
		if (offset.LengthSquared() > CombatTuning.BashReach * CombatTuning.BashReach)
		{
			return false;
		}

		return GuardResolver.InCone(
			attackerForward, attackerPosition, targetPosition,
			CombatTuning.BashConeDegrees);
	}

	/// <summary>
	/// A bash guarantees a stagger only when it interrupts a committed attack
	/// or connects with a held guard. Against an idle, recovering, or raising
	/// opponent it is an ordinary poise hit, so a bash cannot stun-lock.
	/// </summary>
	public static bool ForcesStagger(CombatState targetState, bool guarding) =>
		guarding || targetState is CombatState.Windup or CombatState.Active;
}
