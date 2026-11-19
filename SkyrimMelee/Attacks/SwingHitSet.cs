
using SkyrimMelee.Tuning;
using SkyrimMelee.Animation;
using SkyrimMelee.Physics;

namespace SkyrimMelee.Attacks;

/// <summary>
/// Per-swing hit bookkeeping. One set belongs to one attack; a hurtbox can
/// produce only its first contact for that swing, and the next attack starts
/// with a clean set.
/// </summary>
public sealed class SwingHitSet
{
	private readonly HashSet<(FighterId Fighter, HurtboxKind Kind)> _hit = new();
	private AttackDefinition? _attack;
	private bool _damagingHit;

	public AttackDefinition? Attack => _attack;

	public int Count => _hit.Count;

	public void Begin(AttackDefinition attack)
	{
		_attack = attack ?? throw new ArgumentNullException(nameof(attack));
		_hit.Clear();
		_damagingHit = false;
	}

	/// <summary>Starts a new swing, discarding every hurtbox from the previous one.</summary>
	public void Reset()
	{
		_hit.Clear();
		_attack = null;
		_damagingHit = false;
	}

	/// <summary>
	/// Claims the swing's single damage instance. Later hurtboxes in the same
	/// swing can still tint, but they do not deal damage.
	/// </summary>
	public bool TryMarkDamagingHit()
	{
		if (_damagingHit)
		{
			return false;
		}

		_damagingHit = true;
		return true;
	}

	/// <summary>
	/// Records the first contact with a hurtbox during this swing. Returns false
	/// for a repeated contact or when no attack is active.
	/// </summary>
	public bool TryRegister(BladeHit hit, FighterId attacker, float attackTime,
		out HitEvent hitEvent)
	{
		hitEvent = default;

		if (_attack is null || !_hit.Add((hit.Hurtbox.Fighter, hit.Hurtbox.Kind)))
		{
			return false;
		}

		var tuning = _attack.Tuning;
		var progress = Math.Clamp(
			(attackTime - tuning.Windup) / MathF.Max(tuning.Active, 1e-6f), 0f, 1f);

		hitEvent = new HitEvent(
			attacker,
			hit.Hurtbox.Fighter,
			hit.Hurtbox.Kind,
			hit.Point,
			hit.Normal,
			_attack,
			progress);
		return true;
	}
}
