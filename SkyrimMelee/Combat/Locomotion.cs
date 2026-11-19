using Microsoft.Xna.Framework;
using SkyrimMelee.Tuning;
using SkyrimMelee.Attacks;
using SkyrimMelee.Physics;

namespace SkyrimMelee.Combat;

/// <summary>Pure movement helpers: arena clamp, opponent separation, facing.</summary>
public static class Locomotion
{
	public static Vector3 Clamp(
		Vector3 position, Vector3 opponentPosition, float arenaRadius, float separation)
	{
		var flat = new Vector2(position.X, position.Z);
		if (flat.LengthSquared() > arenaRadius * arenaRadius)
		{
			flat = Vector2.Normalize(flat) * arenaRadius;
			position.X = flat.X;
			position.Z = flat.Y;
		}

		var away = new Vector2(
			position.X - opponentPosition.X,
			position.Z - opponentPosition.Z);

		if (away.LengthSquared() < separation * separation)
		{
			away = away.LengthSquared() < 1e-6f
				? new Vector2(1f, 0f)
				: Vector2.Normalize(away);

			position.X = opponentPosition.X + away.X * separation;
			position.Z = opponentPosition.Z + away.Y * separation;
		}

		flat = new Vector2(position.X, position.Z);
		if (flat.LengthSquared() > arenaRadius * arenaRadius)
		{
			flat = Vector2.Normalize(flat) * arenaRadius;
			position.X = flat.X;
			position.Z = flat.Y;
		}

		return position;
	}

	/// <summary>
	/// Speed fraction for the current state; zero means the root is locked.
	/// A rooted action (e.g. bash) stops the fighter completely regardless of state.
	/// </summary>
	public static float MovementMultiplier(CombatState state, bool rootLocked = false)
	{
		if (rootLocked)
		{
			return 0f;
		}

		return state switch
		{
			CombatState.Idle or CombatState.Recovery => 1f,
			CombatState.Windup or CombatState.Active => CombatTuning.AttackMoveMultiplier,
			CombatState.GuardRaise or CombatState.GuardHold or CombatState.GuardLower =>
				CombatTuning.GuardMoveMultiplier,
			_ => 0f
		};
	}

	/// <summary>Yaw that turns local +X toward the given horizontal direction.</summary>
	public static float FacingFor(Vector3 direction) =>
		MathF.Atan2(-direction.Z, direction.X);
}
