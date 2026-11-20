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

				// The blade may graze the guard after the body; the cone still
				// decides when the body contact is inside the frontal block volume.
				if (InCone(defenderForward, defenderPosition, attackerPosition,
						CombatTuning.GuardBlockConeDegrees))
				{
					return guardAge < CombatTuning.GuardTimedBlockWindow
						? HitOutcome.TimedBlock
						: HitOutcome.Block;
				}

				return HitOutcome.Clean;
			}

			return guardAge < CombatTuning.GuardTimedBlockWindow
				? HitOutcome.TimedBlock
				: HitOutcome.Block;
		}

		// Skyrim blocks inside a frontal cone, not only where the weapon mesh is:
		// a body contact inside the cone counts even if the blade capsule missed.
		if (isGuarding &&
			bodyHit is not null &&
			InCone(defenderForward, defenderPosition, attackerPosition,
				CombatTuning.GuardBlockConeDegrees))
		{
			contact = bodyHit.Value;
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

	/// <summary>True when <paramref name="to"/> lies inside a horizontal cone around forward.</summary>
	public static bool InCone(
		Vector3 forward, Vector3 from, Vector3 to, float halfAngleDegrees)
	{
		var direction = to - from;
		direction.Y = 0f;
		forward.Y = 0f;
		if (direction.LengthSquared() < 1e-8f || forward.LengthSquared() < 1e-8f)
		{
			return true;
		}

		direction.Normalize();
		forward.Normalize();
		return Vector3.Dot(forward, direction) >=
			MathF.Cos(MathHelper.ToRadians(halfAngleDegrees));
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
