using Microsoft.Xna.Framework;
using SkyrimMelee.Tuning;
using SkyrimMelee.Animation;
using SkyrimMelee.Physics;

namespace SkyrimMelee.Attacks;

/// <summary>
/// A confirmed contact for damage and feedback. <see cref="ActiveProgress"/> is
/// 0 at the start of the active window and 1 at its end.
/// </summary>
public readonly record struct HitEvent(
	FighterId Attacker,
	FighterId Target,
	HurtboxKind Hurtbox,
	Vector3 Point,
	Vector3 Normal,
	AttackDefinition Attack,
	float ActiveProgress);
