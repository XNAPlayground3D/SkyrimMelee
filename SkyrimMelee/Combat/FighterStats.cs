namespace SkyrimMelee.Combat;

/// <summary>
/// One fighter's resources. All maximums live in <see cref="CombatTuning"/>;
/// stamina and poise regenerate after their respective delays.
/// </summary>
public sealed class FighterStats
{
	private float _staminaDelay;
	private float _poiseDelay;

	public FighterStats(float stamina = CombatTuning.MaxStamina)
	{
		Health = MaxHealth;
		Stamina = Math.Clamp(stamina, 0f, MaxStamina);
		Poise = MaxPoise;
	}

	public float MaxHealth { get; } = CombatTuning.MaxHealth;
	public float Health { get; private set; }
	public float MaxStamina { get; } = CombatTuning.MaxStamina;
	public float Stamina { get; private set; }
	public float MaxPoise { get; } = CombatTuning.MaxPoise;
	public float Poise { get; private set; }

	public float Armor { get; init; } = CombatTuning.DefaultArmor;
	public float TwoHandedSkill { get; init; } = CombatTuning.DefaultTwoHandedSkill;
	public float BlockSkill { get; init; } = CombatTuning.DefaultBlockSkill;

	public bool IsStaminaRegenerating => _staminaDelay <= 0f;

	public bool IsDead => Health <= 0f;

	public void ApplyDamage(float amount)
	{
		if (amount <= 0f)
		{
			return;
		}

		Health = MathF.Max(0f, Health - amount);
	}

	/// <summary>Power attacks need their full cost; light attacks are always allowed.</summary>
	public bool CanAfford(AttackDefinition attack) =>
		!attack.RequiresStamina || Stamina + 1e-4f >= attack.StaminaCost;

	/// <summary>Charges the attack at wind-up and restarts the regen delay.</summary>
	public void Spend(AttackDefinition attack)
	{
		Stamina = MathF.Max(0f, Stamina - attack.StaminaCost);
		_staminaDelay = CombatTuning.StaminaRegenDelay;
	}

	public void SetStamina(float value) => Stamina = Math.Clamp(value, 0f, MaxStamina);

	public void TakePoiseDamage(float amount)
	{
		Poise = MathF.Max(0f, Poise - MathF.Max(0f, amount));
		_poiseDelay = CombatTuning.PoiseRegenDelay;
	}

	public void RestorePoise()
	{
		Poise = MaxPoise;
		_poiseDelay = 0f;
	}

	public void Update(float deltaSeconds, bool blocking)
	{
		if (deltaSeconds <= 0f)
		{
			return;
		}

		if (blocking)
		{
			_staminaDelay = MathF.Max(_staminaDelay, CombatTuning.StaminaRegenDelay);
		}
		else
		{
			var remaining = deltaSeconds;
			if (_staminaDelay > 0f)
			{
				var consumed = MathF.Min(_staminaDelay, remaining);
				_staminaDelay -= consumed;
				remaining -= consumed;
			}

			if (remaining > 0f)
			{
				Stamina = MathF.Min(MaxStamina,
					Stamina + CombatTuning.StaminaRegenPerSecond * remaining);
			}
		}

		var poiseRemaining = deltaSeconds;
		if (_poiseDelay > 0f)
		{
			var consumed = MathF.Min(_poiseDelay, poiseRemaining);
			_poiseDelay -= consumed;
			poiseRemaining -= consumed;
		}

		if (poiseRemaining > 0f)
		{
			Poise = MathF.Min(MaxPoise,
				Poise + CombatTuning.PoiseRegenPerSecond * poiseRemaining);
		}
	}

	public void Reset()
	{
		Health = MaxHealth;
		Stamina = MaxStamina;
		Poise = MaxPoise;
		_staminaDelay = 0f;
		_poiseDelay = 0f;
	}
}
