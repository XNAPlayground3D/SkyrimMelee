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
	public const float MaxHealth = 150f;
	public const float MaxStamina = 100f;
	public const float MaxPoise = 100f;
	public const float StaminaRegenPerSecond = 25f;
	public const float StaminaRegenDelay = 0.6f;
	public const float PoiseRegenPerSecond = 25f;
	public const float PoiseRegenDelay = 1.0f;
	public const float DefaultArmor = 0f;
	public const float DefaultTwoHandedSkill = 60f;
	public const float DefaultBlockSkill = 50f;

	public const float CrossfadeDuration = 0.1f;
	public const float IdleDuration = 2.5f;
	public const float GuardRaiseDuration = 0.20f;
	public const float GuardHoldDuration = 2.0f;
	public const float GuardLowerDuration = 0.15f;

	/// <summary>Half-angle of the defender's front cone that can block.</summary>
	public const float GuardConeDegrees = 60f;

	/// <summary>A guard younger than this is a timed block; older is a normal block.</summary>
	public const float GuardTimedBlockWindow = 0.25f;

	public const float StaggerSmallDuration = 0.60f;
	public const float StaggerLargeDuration = 1.00f;
	public const float DeathDuration = 1.20f;
	public const float VictoryDuration = 1.60f;

	/// <summary>How long a hit body part stays tinted.</summary>
	public const float HitFlashDuration = 0.18f;

	public const float WeaponBaseDamage = 20f;
	public const float BladeReach = 1.47f;

	public const float TwoHandedDamagePerSkill = 0.4f;
	public const float ArmorReductionPerPoint = 0.0012f;
	public const float ArmorReductionCap = 0.80f;
	public const float BlockReductionBase = 0.30f;
	public const float BlockReductionPerSkill = 0.45f;
	public const float TimedBlockBonus = 0.15f;
	public const float TimedBlockCap = 0.85f;
	public const float BlockStaminaMultiplier = 0.6f;
	public const float TimedBlockStaminaMultiplier = 0.3f;
	public const float MinimumDamage = 1f;

	public static readonly AttackTuning Slash =
		new(0.25f, 0.22f, 0.35f, 12f, 1.0f, 10f);

	public static readonly AttackTuning PowerSlash =
		new(0.60f, 0.16f, 0.70f, 30f, 1.8f, 45f, RequiresStamina: true);
}
