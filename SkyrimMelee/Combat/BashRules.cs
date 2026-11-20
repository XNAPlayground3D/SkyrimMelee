using Microsoft.Xna.Framework;
using SkyrimMelee.Tuning;

namespace SkyrimMelee.Combat;

/// <summary>
/// Bash range: Skyrim's shove uses the weapon's melee reach, so it only
/// connects (and staggers) when the target is inside attack range and in front.
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
}
