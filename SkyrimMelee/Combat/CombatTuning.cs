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
	public const float GuardRaiseDuration = 0.35f;

	public const float WeaponBaseDamage = 20f;
	public const float BladeReach = 1.47f;

	public static readonly AttackTuning Slash =
		new(0.25f, 0.22f, 0.35f, 12f, 1.0f, 10f);

	public static readonly AttackTuning Overhead =
		new(0.40f, 0.14f, 0.45f, 18f, 1.2f, 20f);

	public static readonly AttackTuning PowerSlash =
		new(0.60f, 0.16f, 0.70f, 30f, 1.8f, 45f, RequiresStamina: true);

	public static readonly AttackTuning PowerOverhead =
		new(0.75f, 0.18f, 0.85f, 40f, 2.2f, 70f, RequiresStamina: true);
}
