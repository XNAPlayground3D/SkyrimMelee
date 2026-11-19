namespace SkyrimMelee.Combat;

public enum AttackPhase
{
	Windup,
	Active,
	Recovery
}

/// <summary>
/// A named attack: its sword clip plus the phase windows and combat values.
/// The clip duration must equal the tuning total.
/// </summary>
public sealed class AttackDefinition
{
	public AttackDefinition(string name, SwordClip clip, AttackTuning tuning)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException("Attack name is required.", nameof(name));
		}

		if (MathF.Abs(clip.Duration - tuning.Total) > 0.001f)
		{
			throw new ArgumentException(
				$"Clip duration {clip.Duration:0.###}s does not match tuning total {tuning.Total:0.###}s for '{name}'.",
				nameof(clip));
		}

		Name = name;
		Clip = clip;
		Tuning = tuning;
	}

	public string Name { get; }
	public SwordClip Clip { get; }
	public AttackTuning Tuning { get; }

	public float WindupEnd => Tuning.Windup;
	public float ActiveEnd => Tuning.Windup + Tuning.Active;
	public float Total => Tuning.Total;
	public float StaminaCost => Tuning.StaminaCost;
	public float DamageMultiplier => Tuning.DamageMultiplier;
	public float StaggerPower => Tuning.StaggerPower;
	public bool RequiresStamina => Tuning.RequiresStamina;

	public bool IsActive(float timeSeconds) =>
		timeSeconds >= WindupEnd && timeSeconds < ActiveEnd;

	public AttackPhase GetPhase(float timeSeconds)
	{
		if (timeSeconds < WindupEnd)
		{
			return AttackPhase.Windup;
		}

		return timeSeconds < ActiveEnd ? AttackPhase.Active : AttackPhase.Recovery;
	}
}
