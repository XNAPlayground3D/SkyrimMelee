using Microsoft.Xna.Framework;
using SkyrimMelee.Tuning;
using SkyrimMelee.Attacks;
using SkyrimMelee.Physics;

namespace SkyrimMelee.Combat;

public enum HitOutcome
{
	Clean,
	Block,
	TimedBlock
}

/// <summary>
/// Classifies a swing's contacts against a defender. The guard blade must be
/// hit before (or level with) the body, the defender must be guarding and
/// facing the attacker, and the guard's age decides normal vs timed block.
/// </summary>
public static class GuardResolver
{
	public static HitOutcome Classify(
		IReadOnlyList<BladeHit> hits,
		FighterId defender,
		bool isGuarding,
		float guardAge,
		Vector3 defenderForward,
		Vector3 defenderPosition,
		Vector3 attackerPosition,
		out BladeHit contact)
	{
		contact = default;

		BladeHit? guardHit = null;
		BladeHit? bodyHit = null;

		foreach (var hit in hits)
		{
			if (hit.Hurtbox.Fighter != defender)
			{
				continue;
			}

			if (hit.Hurtbox.Kind == HurtboxKind.GuardBlade)
			{
				guardHit ??= hit;
			}
			else
			{
				bodyHit ??= hit;
			}
		}

		if (guardHit is not null)
		{
			contact = guardHit.Value;

			if (!isGuarding ||
				!InFront(defenderForward, defenderPosition, attackerPosition))
			{
				contact = bodyHit ?? guardHit.Value;
				return HitOutcome.Clean;
			}

			if (bodyHit is not null &&
				bodyHit.Value.TimeOfImpact < guardHit.Value.TimeOfImpact - 1e-4f)
			{
				contact = bodyHit.Value;
				return HitOutcome.Clean;
			}

			return guardAge < CombatTuning.GuardTimedBlockWindow
				? HitOutcome.TimedBlock
				: HitOutcome.Block;
		}

		if (bodyHit is not null)
		{
			contact = bodyHit.Value;
		}

		return HitOutcome.Clean;
	}

	/// <summary>True when the attacker lies inside the defender's front guard cone.</summary>
	public static bool InFront(
		Vector3 defenderForward, Vector3 defenderPosition, Vector3 attackerPosition)
	{
		var direction = attackerPosition - defenderPosition;
		direction.Y = 0f;
		if (direction.LengthSquared() < 1e-8f)
		{
			return true;
		}

		defenderForward.Y = 0f;
		if (defenderForward.LengthSquared() < 1e-8f)
		{
			return false;
		}

		direction.Normalize();
		defenderForward.Normalize();

		var limit = MathF.Cos(MathHelper.ToRadians(CombatTuning.GuardConeDegrees));
		return Vector3.Dot(defenderForward, direction) >= limit;
	}
}
