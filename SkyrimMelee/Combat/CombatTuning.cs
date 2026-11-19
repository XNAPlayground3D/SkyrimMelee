namespace SkyrimMelee.Combat;

/// <summary>One attack's phase timings and combat values.</summary>
public readonly record struct AttackTuning(
	float Windup,
	float Active,
	float Recovery,
	float StaminaCost,
	float DamageMultiplier,
	float StaggerPower,
	bool RequiresStamina = false)
{
	public float Total => Windup + Active + Recovery;
}

/// <summary>Every gameplay number lives here; docs mirror this file.</summary>
public static class CombatTuning
{
	public const float CrossfadeDuration = 0.1f;
	public const float IdleDuration = 2.5f;
	public const float GuardRaiseDuration = 0.20f;
	public const float GuardHoldDuration = 2.0f;
	public const float GuardLowerDuration = 0.15f;

	public const float StaggerSmallDuration = 0.60f;
	public const float StaggerLargeDuration = 1.00f;
	public const float DeathDuration = 1.20f;
	public const float VictoryDuration = 1.60f;

	public const float WeaponBaseDamage = 20f;
	public const float BladeReach = 1.47f;

	public static readonly AttackTuning Slash =
		new(0.25f, 0.22f, 0.35f, 12f, 1.0f, 10f);

	public static readonly AttackTuning PowerSlash =
		new(0.60f, 0.16f, 0.70f, 30f, 1.8f, 45f, RequiresStamina: true);
}
